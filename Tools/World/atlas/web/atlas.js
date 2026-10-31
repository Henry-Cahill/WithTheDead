// The Kentucky Atlas in a browser: the world store's buildings over Kentucky's own aerial photographs, each one's outline
// and kind checked against them, and what was found (with the corrections the game applies) saved in the store's
// world/validation folder through atlas.py. The editor's Kentucky Atlas shares the same records.
// Map data (c) OpenStreetMap contributors (ODbL); imagery KyFromAbove, Commonwealth of Kentucky.
"use strict";

(async function () {
  const $ = (id) => document.getElementById(id);
  const BUILDINGS_ZOOM = 15;   // outlines from this zoom on
  const MAX_REGIONS_IN_VIEW = 9;
  const KEEP_REGIONS = 16;     // regions kept in memory
  const MAX_DRAWN = 15000;     // outlines drawn at once
  const COLOURS = { unchecked: "#ffffff", partial: "#f5b342", ok: "#38c172", bad: "#ff4d4f" };
  const FIXED = "#4da3ff", SELECTED = "#00e5ff";
  const KY = "https://kygisserver.ky.gov/arcgis/rest/services/WGS84WM_Services/";
  const START = [37.8092, -85.4669];   // Bardstown, where the raid starts

  let vocab, cur;
  try {
    [vocab, cur] = await Promise.all([getJSON("vocab.json"), getJSON("api/current")]);
  } catch (e) {
    document.body.textContent = "The atlas can't read the world store: " + e.message;
    return;
  }
  const CHECKS = vocab.checks.map((c) => c[0]);
  const REGION = cur.region;
  const buildingsIn = new Map(cur.regions.map(([rx, rz, n]) => [rx + "_" + rz, n]));
  $("build").textContent = "build " + cur.build;
  $("credit").textContent = cur.attribution;

  // --- The map --------------------------------------------------------------------------------------------------------
  const map = L.map("map", { maxZoom: 22, zoomSnap: 0.5, preferCanvas: true });
  const renderer = L.canvas({ padding: 0.25, tolerance: 3 });
  const tiles = (url, native, attribution) => L.tileLayer(url, { maxNativeZoom: native, maxZoom: 22, attribution });
  const kyCredit = "Imagery: KyFromAbove, Commonwealth of Kentucky";
  const bases = {
    "Kentucky aerial, 3 in (2022-24)": tiles(KY + "Ky_Imagery_Phase3_3IN_WGS84WM/MapServer/tile/{z}/{y}/{x}", 21, kyCredit),
    "Kentucky aerial, 6 in (phase 2)": tiles(KY + "Ky_Imagery_Phase2_6IN_WGS84WM/MapServer/tile/{z}/{y}/{x}", 21, kyCredit),
    "OpenStreetMap": tiles("https://tile.openstreetmap.org/{z}/{x}/{y}.png", 19,
      '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'),
  };
  bases["Kentucky aerial, 3 in (2022-24)"].addTo(map);
  L.control.layers(bases, null, { position: "topright" }).addTo(map);
  L.control.scale({ imperial: true }).addTo(map);
  map.attributionControl.addAttribution(cur.attribution);
  map.on("baselayerchange", (e) => {
    COLOURS.unchecked = e.name === "OpenStreetMap" ? "#4a4f57" : "#ffffff";
    restyleDrawn();
  });

  const toLatLng = (x, z) => {
    const [lon, lat] = Ky3088.toLonLat(x, z);
    return [lat, lon];
  };

  // --- The state, region by region --------------------------------------------------------------------------------------
  const regions = new Map();   // "rx_rz" -> { rx, rz, key, buildings, byId, checks (id -> record), loaded, ready, used }
  const summary = new Map();   // "rx_rz" -> [records, ok, partial, bad, corrected]
  const drawn = L.layerGroup().addTo(map);
  const drawnSet = new Set();
  let selected = null, edit = null, base = "", dirty = false;

  function region(rx, rz) {
    const key = rx + "_" + rz;
    let r = regions.get(key);
    if (r) {
      r.used = Date.now();
      return r.ready;
    }
    r = { rx, rz, key, buildings: [], byId: new Map(), checks: new Map(), loaded: false, used: Date.now() };
    regions.set(key, r);
    r.ready = Promise.all([getJSON(`api/region/${key}?build=${encodeURIComponent(cur.build)}`), getJSON(`api/checks/${key}`)])
      .then(([data, checks]) => {
        index(r, data);
        setChecks(r, checks);
        r.loaded = true;
        return r;
      })
      .catch((e) => {
        regions.delete(key);
        throw e;
      });
    return r.ready;
  }

  // A region's buildings, their outlines on the map (the grid's decimetres from the region's corner, projected).
  function index(r, data) {
    const ox = r.rx * REGION, oz = r.rz * REGION;
    (data.buildings || []).forEach((d, i) => {
      const p = d.pts;
      if (!p || p.length < 6) return;
      const n = p.length >> 1, ll = new Array(n);
      let sx = 0, sz = 0, area = 0;
      for (let k = 0; k < n; k++) {
        const x = ox + p[2 * k] / 10, z = oz + p[2 * k + 1] / 10;
        sx += x;
        sz += z;
        ll[k] = toLatLng(x, z);
        const j = (k + 1) % n;
        area += p[2 * k] * p[2 * j + 1] - p[2 * j] * p[2 * k + 1];
      }
      const b = { r, i, id: d.id, d, ll, gx: sx / n, gz: sz / n, area: Math.abs(area) / 200, layer: null };
      b.bounds = L.latLngBounds(ll);
      r.buildings.push(b);
      const same = r.byId.get(b.id);
      if (same) same.push(b);
      else r.byId.set(b.id, [b]);
    });
  }

  function setChecks(r, file) {
    r.checks = new Map((file.buildings || []).map((rec) => [rec.id, rec]));
  }

  function setRecord(r, id, rec) {
    if (rec) r.checks.set(id, rec);
    else r.checks.delete(id);
    for (const b of r.byId.get(id) || []) restyle(b);
    progress();
  }

  async function refreshChecks(r) {
    try {
      setChecks(r, await getJSON(`api/checks/${r.key}`));
      for (const b of r.buildings) if (b.layer && drawnSet.has(b)) b.layer.setStyle(styleOf(b));
    } catch (e) {
      // keep what we have; the next save says if the store is gone
    }
  }

  // --- Status and style ---------------------------------------------------------------------------------------------------
  const checksOf = (b) => ((b.r.checks.get(b.id) || {}).checks) || {};

  function statusOf(rec) {
    const c = (rec && rec.checks) || {};
    const values = CHECKS.map((name) => c[name] || "");
    if (values.includes("bad")) return "bad";
    const done = values.filter((v) => v === "ok" || v === "na").length;
    return done === 0 ? "unchecked" : done === CHECKS.length ? "ok" : "partial";
  }

  const hasFix = (f) => !!f && Object.values(f).some((v) => v);
  // What the browser can check: a building whose outline or kind hasn't been.
  const open = (b) => {
    const c = checksOf(b);
    return !c.footprint || !c.kind;
  };

  function styleOf(b) {
    const rec = b.r.checks.get(b.id), st = statusOf(rec), fix = rec && rec.fix, fixed = hasFix(fix);
    const sel = !!selected && selected.r === b.r && selected.id === b.id;
    return {
      renderer, color: sel ? SELECTED : fixed ? FIXED : COLOURS[st], weight: sel ? 3.5 : st === "unchecked" && !fixed ? 1.2 : 2,
      opacity: 0.95, fillColor: COLOURS[st], fillOpacity: st === "unchecked" ? (sel ? 0.12 : 0.03) : 0.28,
      dashArray: fix && fix.hide ? "2 5" : fixed ? "7 4" : null,
    };
  }

  function restyle(b) {
    if (!b) return;
    for (const same of b.r.byId.get(b.id) || [b]) {
      if (!same.layer) continue;
      same.layer.setStyle(styleOf(same));
      if (selected && selected.id === same.id) same.layer.bringToFront();
    }
  }

  function restyleDrawn() {
    for (const b of drawnSet) b.layer.setStyle(styleOf(b));
  }

  function layerOf(b) {
    if (!b.layer) {
      b.layer = L.polygon(b.ll, styleOf(b));
      b.layer.on("click", (e) => {
        L.DomEvent.stopPropagation(e);
        select(b);
      });
      b.layer.bindTooltip(() => esc(describe(b)), { sticky: true, direction: "top", opacity: 0.9 });
    }
    return b.layer;
  }

  function describe(b) {
    const d = b.d, rec = b.r.checks.get(b.id), st = statusOf(rec);
    return [d.name, d.kind + (d.use ? " (" + d.use + ")" : ""), d.address].filter(Boolean).join(" · ")
      + (st === "unchecked" ? "" : " · " + st) + (rec && rec.fix && rec.fix.hide ? " · not there" : "");
  }

  // --- The overview: the build's regions, shaded by how far their checking has got ----------------------------------------
  const overview = L.layerGroup();
  const shapes = new Map();
  for (const [rx, rz] of cur.regions) {
    const corners = [[rx, rz], [rx + 1, rz], [rx + 1, rz + 1], [rx, rz + 1]].map(([x, z]) => toLatLng(x * REGION, z * REGION));
    const shape = L.polygon(corners, { renderer, weight: 1, color: "#8fb3d9", opacity: 0.4, fillOpacity: 0 });
    shape.on("click", () => map.setView(toLatLng((rx + 0.5) * REGION, (rz + 0.5) * REGION), BUILDINGS_ZOOM));
    shape.bindTooltip(() => regionLabel(rx, rz), { sticky: true });
    shapes.set(rx + "_" + rz, shape);
    overview.addLayer(shape);
  }

  function regionLabel(rx, rz) {
    const key = rx + "_" + rz, s = summary.get(key), n = buildingsIn.get(key) || 0;
    const checked = s ? `${s[0]} checked (${s[1]} good, ${s[2]} partly, ${s[3]} wrong, ${s[4]} corrected)` : "none checked";
    return `Region ${key}: ${n.toLocaleString()} buildings, ${checked}`;
  }

  function shadeOverview() {
    for (const [key, shape] of shapes) {
      const s = summary.get(key), n = buildingsIn.get(key) || 1;
      if (!s) shape.setStyle({ fillOpacity: 0, color: "#8fb3d9", opacity: 0.4 });
      else {
        const colour = s[3] > 0 ? COLOURS.bad : s[2] > 0 ? COLOURS.partial : COLOURS.ok;
        shape.setStyle({ fillColor: colour, fillOpacity: 0.15 + 0.6 * Math.min(1, s[0] / n), color: colour, opacity: 0.8 });
      }
    }
  }

  async function refreshSummary() {
    try {
      const s = await getJSON("api/checks");
      summary.clear();
      for (const [rx, rz, ...counts] of s.regions) summary.set(rx + "_" + rz, counts);
      shadeOverview();
      progress();
    } catch (e) {
      // the overview stays as it was
    }
  }

  // --- Place names (the aerial photographs have none) ----------------------------------------------------------------------
  let places = [];
  const placeLayer = L.layerGroup().addTo(map);
  getJSON(`api/places?build=${encodeURIComponent(cur.build)}`).then((p) => {
    places = (p.places || []).filter((pl) => pl.name).map((pl) => ({ ...pl, ll: toLatLng(pl.x, pl.z) }));
    const list = $("placeNames");
    for (const pl of places.slice(0, 1500)) {
      const o = document.createElement("option");
      o.value = pl.name;
      list.appendChild(o);
    }
    drawPlaces();
  }).catch(() => {});

  function drawPlaces() {
    placeLayer.clearLayers();
    const z = map.getZoom(), view = map.getBounds();
    const kinds = z < 9 ? ["city"] : z < 11 ? ["city", "town"] : z < 13 ? ["city", "town", "village"]
      : ["city", "town", "village", "hamlet", "suburb", "neighbourhood", "locality"];
    let n = 0;
    for (const pl of places) {
      if (!kinds.includes(pl.kind) || !view.contains(pl.ll)) continue;
      placeLayer.addLayer(L.marker(pl.ll, { interactive: false, keyboard: false,
        icon: L.divIcon({ className: "place", html: esc(pl.name), iconSize: null }) }));
      if (++n >= 150) break;
    }
  }

  // --- Drawing what's in view -----------------------------------------------------------------------------------------------
  function regionsInView() {
    const v = map.getBounds();
    const corners = [v.getSouthWest(), v.getNorthWest(), v.getNorthEast(), v.getSouthEast()].map((c) => Ky3088.toGrid(c.lng, c.lat));
    const xs = corners.map((c) => c[0]), zs = corners.map((c) => c[1]);
    const out = [];
    for (let rx = Math.floor(Math.min(...xs) / REGION); rx <= Math.floor(Math.max(...xs) / REGION); rx++)
      for (let rz = Math.floor(Math.min(...zs) / REGION); rz <= Math.floor(Math.max(...zs) / REGION); rz++)
        if (buildingsIn.has(rx + "_" + rz)) out.push([rx, rz]);
    return out;
  }

  let refreshes = 0;
  async function refresh() {
    const token = ++refreshes;
    const z = map.getZoom();
    saveHash();
    drawPlaces();
    if (z < BUILDINGS_ZOOM) {
      if (!map.hasLayer(overview)) overview.addTo(map);
      undraw();
      progress(`Zoom in to a town to see its buildings (zoom ${BUILDINGS_ZOOM}+).`);
      return;
    }
    if (map.hasLayer(overview)) map.removeLayer(overview);
    const want = regionsInView();
    if (want.length > MAX_REGIONS_IN_VIEW) {
      undraw();
      progress("Zoom in a little further.");
      return;
    }
    if (want.some(([rx, rz]) => !(regions.get(rx + "_" + rz) || {}).loaded)) progress("Loading the buildings...");
    try {
      await Promise.all(want.map(([rx, rz]) => region(rx, rz)));
    } catch (e) {
      toast("Couldn't load the buildings: " + e.message, true);
    }
    if (token !== refreshes) return;   // the map moved on meanwhile
    evict(want);
    draw();
  }

  function draw() {
    const view = map.getBounds().pad(0.15), only = $("onlyOpen").checked;
    const keep = new Set();
    let capped = false;
    outer: for (const r of regions.values()) {
      if (!r.loaded) continue;
      for (const b of r.buildings) {
        if (!view.intersects(b.bounds) || (only && !open(b) && !(selected && selected.id === b.id))) continue;
        if (keep.size >= MAX_DRAWN) {
          capped = true;
          break outer;
        }
        keep.add(b);
      }
    }
    for (const b of [...drawnSet]) if (!keep.has(b)) {
      drawn.removeLayer(b.layer);
      drawnSet.delete(b);
    }
    for (const b of keep) if (!drawnSet.has(b)) {
      drawn.addLayer(layerOf(b).setStyle(styleOf(b)));
      drawnSet.add(b);
    }
    if (selected) restyle(selected);
    progress(capped ? "Too many buildings to draw them all: zoom in." : null);
  }

  function undraw() {
    drawn.clearLayers();
    drawnSet.clear();
  }

  function evict(want) {
    if (regions.size <= KEEP_REGIONS) return;
    const wanted = new Set(want.map(([rx, rz]) => rx + "_" + rz));
    const old = [...regions.values()].filter((r) => r.loaded && !wanted.has(r.key) && !(selected && selected.r === r))
      .sort((a, b) => a.used - b.used);
    while (regions.size > KEEP_REGIONS && old.length) {
      const r = old.shift();
      for (const b of r.buildings) if (drawnSet.has(b)) {
        drawn.removeLayer(b.layer);
        drawnSet.delete(b);
      }
      regions.delete(r.key);
    }
  }

  function progress(note) {
    let total = 0, checked = 0, ok = 0, bad = 0;
    for (const r of regions.values()) {
      if (!r.loaded) continue;
      total += r.buildings.length;
      for (const rec of r.checks.values()) {
        const st = statusOf(rec);
        if (st !== "unchecked") checked++;
        if (st === "ok") ok++;
        if (st === "bad") bad++;
      }
    }
    let state = 0;
    for (const s of summary.values()) state += s[0];
    const here = map.getZoom() >= BUILDINGS_ZOOM && total
      ? `${checked.toLocaleString()} of ${total.toLocaleString()} buildings here checked (${ok} all good, ${bad} wrong). ` : "";
    $("progress").textContent = (note ? note + " " : "") + here + `Statewide: ${state.toLocaleString()} checked.`;
  }

  // --- Choosing a building ---------------------------------------------------------------------------------------------------
  async function select(b, opts = {}) {
    if (selected && dirty && !(await save())) return;   // keep an edit that couldn't be saved
    const was = selected;
    selected = b;
    restyle(was);
    restyle(b);
    await refreshChecks(b.r);   // the editor may have saved it since
    if (selected !== b) return;
    const rec = b.r.checks.get(b.id);
    edit = copy(rec, b.id);
    base = rec ? rec.updated : "";
    dirty = false;
    if (opts.fly) {
      if (map.getZoom() < 18) map.fitBounds(b.bounds, { maxZoom: 19, padding: [80, 80] });
      else map.panInside(b.bounds.getCenter(), { padding: [120, 120] });
    }
    restyle(b);
    showPanel();
    saveHash();
  }

  async function deselect() {
    if (dirty && !(await save())) return;
    const was = selected;
    selected = edit = null;
    dirty = false;
    restyle(was);
    showPanel();
    saveHash();
  }

  // The nearest building whose outline or kind isn't checked yet, in what's loaded.
  function next() {
    const c = map.getCenter();
    const [fx, fz] = selected ? [selected.gx, selected.gz] : Ky3088.toGrid(c.lng, c.lat);
    let best = null, bestD = Infinity;
    for (const r of regions.values()) {
      if (!r.loaded) continue;
      for (const b of r.buildings) {
        if ((selected && b.id === selected.id) || !open(b)) continue;
        const d = (b.gx - fx) ** 2 + (b.gz - fz) ** 2;
        if (d < bestD) {
          bestD = d;
          best = b;
        }
      }
    }
    if (best) select(best, { fly: true });
    else toast("Every building loaded here has its outline and kind checked: move the map on.");
  }

  async function openBuilding(spec) {
    const m = /^(-?\d+)_(-?\d+)_(\d+)$/.exec(spec);
    if (!m) return false;
    try {
      const r = await region(+m[1], +m[2]);
      const b = (r.byId.get(+m[3]) || [])[0];
      if (!b) {
        toast(`Region ${m[1]}_${m[2]} has no building ${m[3]}`, true);
        return false;
      }
      await select(b, { fly: true });
      return true;
    } catch (e) {
      toast("Couldn't load that building: " + e.message, true);
      return false;
    }
  }

  // --- The panel ---------------------------------------------------------------------------------------------------------------
  const checkButtons = new Map();
  for (const [name, label, help] of vocab.checks) {
    const row = document.createElement("div");
    row.className = "check";
    const caption = document.createElement("span");
    caption.textContent = label;
    caption.title = help;
    row.appendChild(caption);
    const buttons = [];
    for (const [value, text, tip] of [["ok", "Good", "Right"], ["bad", "Wrong", "Wrong: say what in the note, or correct it"],
      ["na", "n/a", "Doesn't apply (a shed's inside)"]]) {
      const btn = document.createElement("button");
      btn.textContent = text;
      btn.title = tip;
      btn.className = value;
      btn.addEventListener("click", () => setCheck(name, edit.checks[name] === value ? "" : value));
      row.appendChild(btn);
      buttons.push([value, btn]);
    }
    checkButtons.set(name, buttons);
    $("checks").appendChild(row);
  }

  const fixFields = { kind: "fixKind", arch: "fixArch", style: "fixStyle", roof: "fixRoof", material: "fixMaterial", facing: "fixFacing" };
  fillSelect("fixKind", vocab.kinds, "as mapped");
  fillSelect("fixArch", vocab.archetypes, "game's choice");
  fillSelect("fixStyle", vocab.styles, "game's choice");
  fillSelect("fixRoof", vocab.roofs, "as mapped");
  fillSelect("fixMaterial", vocab.materials, "as mapped");
  fillSelect("fixFacing", vocab.facings, "game's choice");
  for (const u of vocab.uses) {
    const o = document.createElement("option");
    o.value = u;
    $("uses").appendChild(o);
  }
  for (const [field, id] of Object.entries(fixFields)) $(id).addEventListener("change", (e) => changeFix(field, e.target.value));
  // Typed fields keep what's typed (atlas.py trims what's saved); only their "changed" mark follows.
  $("fixUse").addEventListener("input", (e) => changeFix("use", e.target.value, false));
  $("fixName").addEventListener("input", (e) => changeFix("name", e.target.value, false));
  $("fixStoreys").addEventListener("input", (e) => changeFix("storeys", Math.max(0, Math.min(40, parseInt(e.target.value, 10) || 0)), false));
  $("fixHide").addEventListener("change", (e) => changeFix("hide", e.target.checked));
  $("note").addEventListener("input", (e) => {
    edit.note = e.target.value;
    touched();
  });
  $("save").addEventListener("click", () => save());
  $("saveNext").addEventListener("click", async () => {
    if (await save()) next();
  });
  $("skip").addEventListener("click", () => next());
  $("clear").addEventListener("click", async () => {
    if (!selected || !confirm("Forget every check, the note and the corrections for this building?")) return;
    edit = copy(null, selected.id);
    touched();
    await save();
  });

  function fillSelect(id, values, none) {
    const s = $(id);
    s.appendChild(new Option("(" + none + ")", ""));
    for (const v of values) s.appendChild(new Option(v, v));
  }

  function setCheck(name, value) {
    if (!edit) return;
    edit.checks[name] = value;
    touched();
    showChecks();
  }

  function changeFix(field, value, rerender = true) {
    if (!edit) return;
    edit.fix[field] = value;
    touched();
    if (rerender) showFix();
    else markChanged();
  }

  function touched() {
    dirty = true;
    setState("Not saved yet", "dirty");
  }

  let shownKey = null;
  function showPanel() {
    $("empty").hidden = !!selected;
    $("detail").hidden = !selected;
    if (!selected) {
      shownKey = null;
      return;
    }
    const b = selected, d = b.d, rec = b.r.checks.get(b.id);
    if (shownKey !== b.r.key + "_" + b.id) {
      // A different building: its details from the top, its corrections open when it has some.
      shownKey = b.r.key + "_" + b.id;
      $("detail").scrollTop = 0;
      $("fixBox").open = hasFix(edit.fix);
    }
    const osm = b.id % 2 === 0 ? ["way", b.id / 2] : ["relation", (b.id - 1) / 2];
    $("title").textContent = d.name || (d.address ? d.address : cap(d.kind));
    $("sub").textContent = `${cap(d.kind)}${d.use ? " (" + d.use + ")" : ""} · building ${b.id} (OSM ${osm[0]} ${osm[1]})`
      + ((b.r.byId.get(b.id) || []).length > 1 ? " · one of its outlines" : "");
    $("links").innerHTML = `<a href="https://www.openstreetmap.org/${osm[0]}/${osm[1]}" target="_blank" rel="noopener">OpenStreetMap ↗</a>`
      + `<a href="https://www.openstreetmap.org/edit?${osm[0]}=${osm[1]}" target="_blank" rel="noopener">Edit the map ↗</a>`
      + `<a href="#" id="copyLink" title="A link to this building, for the editor or a note">Copy link</a>`;
    $("copyLink").addEventListener("click", (e) => {
      e.preventDefault();
      navigator.clipboard.writeText(location.href).then(() => toast("Link copied"));
    });
    const rows = [
      ["Kind", d.kind], ["Use", d.use], ["Name", d.name], ["Address", d.address],
      ["Storeys", d.levels + (d.levels === defaultLevels(d.kind) ? " (none mapped)" : "")],
      ["Height", d.height ? d.height + " m" : ""], ["Roof", d.roof], ["Walls", d.material],
      ["Colours", [d.colour, d.roofColour].filter((c) => /^#[0-9a-fA-F]{6}$/.test(c || ""))
        .map((c) => `<i class="swatch" style="background:${c}"></i>${c}`).join(" "), true],
      ["Built", d.year || ""], ["Footprint", `${Math.round(b.area).toLocaleString()} m², ${b.ll.length} corners`],
      ["Where", `region ${b.r.key}, grid ${Math.round(b.gx)} E ${Math.round(b.gz)} N`],
    ];
    $("data").innerHTML = rows.filter((row) => row[1] !== undefined && row[1] !== null && row[1] !== "")
      .map(([k, v, html]) => `<tr><td>${k}</td><td>${html ? v : esc(String(v))}</td></tr>`).join("");
    showGame(rec);
    $("note").value = edit.note || "";
    showChecks();
    showFix();
    if (rec) setState(`Saved ${when(rec.updated)} by ${rec.by}` + (rec.build && rec.build !== cur.build ? ` (against build ${rec.build})` : ""), "");
    else setState("Not checked yet", "");
  }

  function showGame(rec) {
    const g = rec && rec.game;
    if (!g || !g.seen) {
      $("game").innerHTML = "<b>In the game:</b> not built in the editor yet. Outside, windows, doors and inside are checked there "
        + "(Unity: <i>WithTheDead ▸ Kentucky Atlas</i>, this building, <i>Build in scene</i>).";
      return;
    }
    const inside = g.furnished ? `${esc(g.use)}: ${esc(g.rooms)}; ${g.containers} containers, ${g.lights} lights` : "not furnished";
    $("game").innerHTML = `<b>In the game</b> (built in the editor ${esc(when(g.seen))}): ${esc(g.arch)}${g.style ? "/" + esc(g.style) : ""}, `
      + `${g.storeys} storey${g.storeys === 1 ? "" : "s"}, ${esc(g.roof)} roof, ${esc(g.wall)}; ${g.enterable ? "enterable" : "shut"}; `
      + `${g.doors} door${g.doors === 1 ? "" : "s"}${g.blockedDoors ? ` (${g.blockedDoors} blocked)` : ""}, ${g.windows} windows; inside: ${inside}.`
      + (g.problems ? `<div class="problems">${esc(g.problems)}</div>` : "");
  }

  function showChecks() {
    for (const [name, buttons] of checkButtons)
      for (const [value, btn] of buttons) btn.classList.toggle("on", !!edit && edit.checks[name] === value);
  }

  function showFix() {
    const f = edit.fix;
    for (const [field, id] of Object.entries(fixFields)) $(id).value = f[field] || "";
    $("fixUse").value = f.use || "";
    $("fixName").value = f.name || "";
    $("fixStoreys").value = f.storeys ? f.storeys : "";
    $("fixHide").checked = !!f.hide;
    markChanged();
  }

  function markChanged() {
    for (const id of [...Object.values(fixFields), "fixUse", "fixName", "fixStoreys"]) $(id).classList.toggle("changed", !!$(id).value);
    $("fixBox").classList.toggle("has", hasFix(edit.fix));
  }

  function setState(text, kind) {
    $("state").textContent = text;
    $("state").className = kind || "";
  }

  // --- Saving --------------------------------------------------------------------------------------------------------------------
  async function save() {
    if (!selected || !edit) return true;
    const b = selected, r = b.r;
    let res, out;
    try {
      res = await fetch(`api/checks/${r.key}`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: b.id, base, checks: edit.checks, note: edit.note, fix: edit.fix }),
      });
      out = await res.json();
    } catch (e) {
      setState("Not saved: " + e.message, "error");
      return false;
    }
    if (res.status === 409) {
      setRecord(r, b.id, out.current);
      if (selected === b) {
        edit = copy(out.current, b.id);
        base = out.current ? out.current.updated : "";
        dirty = false;
        showPanel();
      }
      toast(out.error + ". It's been reloaded: make your changes again.", true);
      return false;
    }
    if (!res.ok) {
      setState("Not saved: " + (out.error || res.status), "error");
      return false;
    }
    setRecord(r, b.id, out.record);
    if (selected === b) {
      edit = copy(out.record, b.id);
      base = out.record ? out.record.updated : "";
      dirty = false;
      showPanel();
      if (!out.record) setState("Cleared", "");
    }
    refreshSummary();
    return true;
  }

  function copy(rec, id) {
    const blank = { id, checks: {}, note: "", fix: { hide: false, kind: "", use: "", name: "", arch: "", style: "", roof: "", material: "", facing: "", storeys: 0 } };
    for (const c of CHECKS) blank.checks[c] = "";
    if (!rec) return blank;
    const r = JSON.parse(JSON.stringify(rec));
    return { id, checks: { ...blank.checks, ...(r.checks || {}) }, note: r.note || "", fix: { ...blank.fix, ...(r.fix || {}) } };
  }

  // --- Finding things -------------------------------------------------------------------------------------------------------------
  $("find").addEventListener("submit", async (e) => {
    e.preventDefault();
    const q = $("q").value.trim();
    if (!q) return;
    let m;
    if ((m = /^(-?\d{1,3}(?:\.\d+)?)\s*[, ]\s*(-?\d{1,3}(?:\.\d+)?)$/.exec(q))) return map.setView([+m[1], +m[2]], 18);
    if (/^-?\d+_-?\d+_\d+$/.test(q)) return openBuilding(q);
    if ((m = /^(?:osm\s*)?(way|relation)\s*[/ ]?\s*(\d+)$/i.exec(q)) || /^\d+$/.test(q)) {
      const id = m ? (m[1].toLowerCase() === "way" ? 2 * +m[2] : 2 * +m[2] + 1) : +q;
      for (const r of regions.values()) {
        const b = r.loaded && (r.byId.get(id) || [])[0];
        if (b) return select(b, { fly: true });
      }
      return toast(`Building ${id} isn't in the loaded area: go to its town first`, true);
    }
    const low = q.toLowerCase();
    const place = places.find((p) => p.name.toLowerCase() === low) || places.find((p) => p.name.toLowerCase().startsWith(low));
    if (place) return map.setView(place.ll, place.kind === "city" ? 15 : 16);
    toast("Nothing called " + q, true);
  });

  $("onlyOpen").addEventListener("change", () => draw());

  document.addEventListener("keydown", (e) => {
    const t = e.target;
    if (t && /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName)) {
      if (e.key === "Escape") t.blur();
      else if (e.key === "Enter" && (e.ctrlKey || e.metaKey) && t.id !== "q" && selected) {
        e.preventDefault();
        save().then((ok) => ok && next());
      }
      return;
    }
    if (e.ctrlKey || e.metaKey || e.altKey) return;
    const k = e.key.toLowerCase();
    if (k === "escape") deselect();
    else if (k === "n") next();
    else if (!selected || !edit) return;
    else if (k === "f") setCheck("footprint", e.shiftKey ? "bad" : "ok");
    else if (k === "k") setCheck("kind", e.shiftKey ? "bad" : "ok");
    else if (k === "h") {
      $("fixBox").open = true;
      changeFix("hide", !edit.fix.hide);
    }
    else if (k === "enter") save().then((ok) => ok && next());
    else if (k === "s") save();
    else return;
    e.preventDefault();
  });

  window.addEventListener("focus", () => {
    for (const r of regions.values()) if (r.loaded) refreshChecks(r);
    refreshSummary();
  });
  window.addEventListener("beforeunload", (e) => {
    if (!dirty) return;
    e.preventDefault();
    e.returnValue = "";
  });

  // --- Where the map is, in the address (a link opens the same view and building) -------------------------------------------------
  function saveHash() {
    const c = map.getCenter();
    const h = `map=${map.getZoom()}/${c.lat.toFixed(5)}/${c.lng.toFixed(5)}` + (selected ? `&b=${selected.r.key}_${selected.id}` : "");
    history.replaceState(null, "", "#" + h);
  }

  const hash = new URLSearchParams(location.hash.slice(1));
  const at = (hash.get("map") || "").split("/").map(Number);
  if (at.length === 3 && at.every(Number.isFinite)) map.setView([at[1], at[2]], at[0]);
  else map.setView(START, 16);
  map.on("moveend", debounce(refresh, 120));
  // A link pasted into this tab (the map's own moves replace the address without this firing).
  window.addEventListener("hashchange", () => {
    const h = new URLSearchParams(location.hash.slice(1));
    const v = (h.get("map") || "").split("/").map(Number);
    if (h.get("b") && (!selected || h.get("b") !== selected.r.key + "_" + selected.id)) openBuilding(h.get("b"));
    else if (v.length === 3 && v.every(Number.isFinite)) map.setView([v[1], v[2]], v[0]);
  });
  refreshSummary();
  await refresh();
  if (hash.get("b")) openBuilding(hash.get("b"));

  // --- Little helpers ----------------------------------------------------------------------------------------------------------------
  function defaultLevels(kind) {
    return ["apartments", "hotel", "hospital"].includes(kind) ? 3 : ["office", "school", "civic"].includes(kind) ? 2 : 1;
  }

  function cap(s) {
    return s ? s.charAt(0).toUpperCase() + s.slice(1) : "";
  }

  function when(iso) {
    const t = Date.parse(iso);
    return Number.isFinite(t) ? new Date(t).toLocaleString() : iso || "";
  }

  function debounce(fn, ms) {
    let timer = 0;
    return () => {
      clearTimeout(timer);
      timer = setTimeout(fn, ms);
    };
  }

  function toast(text, error) {
    const el = $("toast");
    el.textContent = text;
    el.className = error ? "error" : "";
    el.hidden = false;
    clearTimeout(toast.timer);
    toast.timer = setTimeout(() => (el.hidden = true), error ? 6000 : 3000);
  }
})();

async function getJSON(url) {
  const res = await fetch(url);
  let body = null;
  try {
    body = await res.json();
  } catch (e) {
    // not JSON: say what the server said below
  }
  if (!res.ok) throw new Error((body && body.error) || `${res.status} ${res.statusText}`);
  return body;
}

function esc(s) {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}
