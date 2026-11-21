"""Minimal Ollama client (native /api/chat, so structured outputs, `think` and `keep_alive` all work)."""
import base64
import io
import json
import time
import urllib.error
import urllib.request

_NUM = {"type": "number"}
_V3 = {"type": "array", "items": _NUM, "minItems": 3, "maxItems": 3}

MODEL_SCHEMA = {
    "type": "object",
    "properties": {
        "notes": {"type": "string"},
        "parts": {
            "type": "array",
            "minItems": 6,
            "maxItems": 64,
            "items": {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "shape": {"type": "string", "enum": ["cube", "sphere", "cylinder"]},
                    "pos": _V3, "rot": _V3, "size": _V3, "color": _V3,
                    "smooth": _NUM, "metal": _NUM,
                },
                "required": ["name", "shape", "pos", "rot", "size", "color", "smooth", "metal"],
            },
        },
    },
    "required": ["notes", "parts"],
}

CRITIC_SCHEMA = {
    "type": "object",
    "properties": {"score": {"type": "integer", "minimum": 1, "maximum": 10}, "problems": {"type": "string"}},
    "required": ["score", "problems"],
}


class Ollama:
    def __init__(self, host, model, think="medium", num_ctx=16384, keep_alive="2h", timeout=1800):
        self.host = host.rstrip("/")
        self.model = model
        self.think = think
        self.num_ctx = num_ctx  # keep constant: a different num_ctx forces Ollama to reload the model
        self.keep_alive = keep_alive
        self.timeout = timeout

    def _post(self, path, body, retries=4):
        data = json.dumps(body).encode("utf-8")
        for attempt in range(retries):
            try:
                req = urllib.request.Request(self.host + path, data, {"Content-Type": "application/json"})
                with urllib.request.urlopen(req, timeout=self.timeout) as r:
                    return json.load(r)
            except (urllib.error.URLError, TimeoutError, ConnectionError):
                if attempt == retries - 1:
                    raise
                time.sleep(5 * (attempt + 1))
        return None

    def chat_json(self, messages, schema, temperature=0.5, model=None, think=None):
        body = {
            "model": model or self.model,
            "messages": messages,
            "stream": False,
            "format": schema,
            "keep_alive": self.keep_alive,
            "options": {"temperature": temperature, "num_ctx": self.num_ctx},
        }
        t = think if think is not None else self.think
        if t:
            body["think"] = t
        r = self._post("/api/chat", body)
        content = r["message"]["content"]
        stats = {"eval": r.get("eval_count", 0), "prompt": r.get("prompt_eval_count", 0),
                 "secs": round(r.get("total_duration", 0) / 1e9, 1)}
        return json.loads(content), content, stats

    def ping(self):
        with urllib.request.urlopen(self.host + "/api/tags", timeout=10) as r:
            names = [m["name"] for m in json.load(r)["models"]]
        return names


def critique(client: Ollama, critic_model: str, image, item_line: str):
    """Asks a vision model whether the rendered preview reads as the item. Returns (score, problems)."""
    buf = io.BytesIO()
    image.save(buf, format="PNG")
    prompt = (
        "These are two renders (three-quarter view, and top-down view) of a low-poly game prop built from "
        "cubes, spheres and cylinders, lying on the floor. It is supposed to be:\n" + item_line + "\n"
        "Judge it as a stylised low-poly prop, not photorealism. Score 1-10 how clearly it reads as that item "
        "(silhouette, proportions, parts in the right places, sensible colours). In 'problems' list concrete, "
        "fixable geometric issues (e.g. 'the hammer head should cross the handle at a right angle', "
        "'sleeves are missing'). Say 'none' if it is good."
    )
    msgs = [{"role": "user", "content": prompt, "images": [base64.b64encode(buf.getvalue()).decode()]}]
    out, _, _ = client.chat_json(msgs, CRITIC_SCHEMA, temperature=0.2, model=critic_model, think=False)
    return int(out.get("score", 0)), str(out.get("problems", "")).strip()
