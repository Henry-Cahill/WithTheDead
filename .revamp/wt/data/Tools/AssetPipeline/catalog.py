"""Reads the CLONE-Z item catalogue straight out of the bundled INSERT scripts.

The game seeds its content DB from Assets/Resources/curated_items.txt and pz_items.txt, so those are
the source of truth here too. Executing them into an in-memory SQLite table avoids hand-parsing SQL.
"""
import re
import sqlite3
from dataclasses import dataclass
from pathlib import Path

# Mirrors ItemCategory, ClothingSlot, GearKind and PlayerSkills.Skill (enum order matters), phrased
# for the prompt.
CATEGORIES = ["Food", "Drink", "Medical", "Weapon", "Firearm", "Ammo", "Material", "Clothing", "Misc"]
CLOTHING_SLOTS = [
    "Clothing worn on the head", "Clothing worn on the torso", "Clothing worn on the legs",
    "Clothing worn on the feet", "Clothing worn on the hands", "Backpack worn on the back",
    "Chest rig (load-bearing vest with pouches)", "Thigh bag strapped to the leg",
    "Head-mounted eyewear or optics", "Mask worn over the face", "Body armour (vest or plate carrier)",
    "Small secure pouch",
]
GEAR_KINDS = ["", "Wearable container", "Night-vision device", "Flashlight", "Gas mask", "Battery",
              "Gas-mask filter canister"]
SKILLS = ["Blade", "Blunt"]

_ITEM_INSERT = re.compile(r"INSERT (?:OR \w+ )?INTO item \(([^)]*)\)")


@dataclass
class Item:
    id: str
    name: str
    category: str
    weight: float
    tint: tuple
    description: str
    traits: list

    def prompt_line(self) -> str:
        r, g, b = self.tint
        bits = [f"{self.name} (id {self.id})", f"Category: {self.category}", f"Weight {self.weight:g} kg",
                f"Tint RGB {r:g},{g:g},{b:g}"]
        bits += self.traits
        if self.description:
            bits.append(f"Description: {self.description}")
        return ". ".join(bits) + "."


def _create_table(db: sqlite3.Connection, scripts: list):
    """Creates the item table with every column any script inserts into. The scripts do not share one
    column list (the curated gear rows carry extra columns), so the first INSERT alone is not enough."""
    cols = []
    for sql in scripts:
        for m in _ITEM_INSERT.finditer(sql):
            for c in (c.strip() for c in m.group(1).split(",")):
                if c and c not in cols:
                    cols.append(c)
    if "id" not in cols:
        raise ValueError("No item INSERT found in the item scripts")
    db.execute(f"CREATE TABLE item ({', '.join(cols)}, PRIMARY KEY(id))")


def _num(v, default=0):
    """Column value with NULL read as the game's SQLite wrapper reads it (0)."""
    return default if v is None else v


def _traits(row: sqlite3.Row) -> list:
    t = []
    if row["isClothing"]:
        slot = _num(row["clothingSlot"])
        t.append(CLOTHING_SLOTS[slot] if 0 <= slot < len(CLOTHING_SLOTS) else "Clothing")
    gear = _num(row["gearKind"]) if "gearKind" in row.keys() else 0
    if 0 < gear < len(GEAR_KINDS):
        t.append(GEAR_KINDS[gear])
    if row["isWeapon"]:
        skill = _num(row["weaponSkill"])
        kind = SKILLS[skill].lower() if 0 <= skill < len(SKILLS) else "melee"
        t.append(f"Melee weapon ({kind}{', two-handed' if row['twoHanded'] else ''}, "
                 f"reach {_num(row['weaponRange']):g} m)")
    if row["isFirearm"]:
        t.append(f"Firearm (magazine {_num(row['magazineSize'])}"
                 f"{', shotgun' if _num(row['pelletsPerShot']) > 1 else ''})")
    if row["isCanned"]:
        t.append("Sealed tin can")
    if row["isWaterContainer"]:
        t.append("Water container" + (" (full)" if row["holdsWater"] else " (empty)"))
    if row["isSeed"]:
        t.append("Packet of seeds")
    if row["isCookedFood"]:
        t.append("Cooked dish")
    if row["stopsBleeding"]:
        t.append("Wound dressing")
    if row["maxStack"] and row["maxStack"] > 1:
        t.append(f"Stacks to {row['maxStack']}")
    return t


def load_items(resources: Path) -> list:
    paths = [resources / name for name in ("curated_items.txt", "pz_items.txt")]
    scripts = [p.read_text(encoding="utf-8") for p in paths if p.exists()]
    if not scripts:
        raise FileNotFoundError(f"No item scripts found under {resources}")

    db = sqlite3.connect(":memory:")
    db.row_factory = sqlite3.Row
    _create_table(db, scripts)
    # Curated rows go in first and win any id clash, mirroring ItemDatabase.EnsureInit.
    for sql in scripts:
        db.executescript(sql.replace("INSERT INTO item", "INSERT OR IGNORE INTO item"))

    items = []
    for row in db.execute("SELECT * FROM item ORDER BY rowid"):
        cat = _num(row["category"])
        items.append(Item(
            id=row["id"],
            name=row["displayName"] or row["id"],
            category=CATEGORIES[cat] if 0 <= cat < len(CATEGORIES) else "Misc",
            weight=float(_num(row["weight"])),
            tint=tuple(float(_num(row[c])) for c in ("tintR", "tintG", "tintB")),
            description=(row["description"] or "").strip(),
            traits=_traits(row),
        ))
    return items


def handmade_ids(item_models_cs: Path) -> set:
    """Ids that already have a bespoke C# model in ItemModels.Dispatch; those are never generated."""
    if not item_models_cs.exists():
        return set()
    src = item_models_cs.read_text(encoding="utf-8")
    start = src.find("switch (def.Id)")
    if start < 0:
        return set()
    end = src.find("default:", start)
    return set(re.findall(r'case "([^"]+)":', src[start:end if end > 0 else None]))
