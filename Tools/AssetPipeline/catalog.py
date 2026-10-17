"""Reads the CLONE-Z item catalogue straight out of the bundled INSERT scripts.

The game seeds its content DB from Assets/Resources/curated_items.txt and pz_items.txt, so those are
the source of truth here too. Executing them into an in-memory SQLite table avoids hand-parsing SQL.
"""
import re
import sqlite3
from dataclasses import dataclass
from pathlib import Path

# Mirrors ItemCategory, ClothingSlot and PlayerSkills.Skill (enum order matters).
CATEGORIES = ["Food", "Drink", "Medical", "Weapon", "Firearm", "Ammo", "Material", "Clothing", "Misc"]
CLOTHING_SLOTS = ["Head", "Torso", "Legs", "Feet", "Hands"]
SKILLS = ["Blade", "Blunt"]


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


def _load_sql(db: sqlite3.Connection, path: Path, created: bool) -> bool:
    sql = path.read_text(encoding="utf-8")
    if not created:
        m = re.search(r"INSERT (?:OR IGNORE )?INTO item \(([^)]*)\)", sql)
        if not m:
            raise ValueError(f"No item INSERT found in {path}")
        db.execute(f"CREATE TABLE item ({m.group(1)}, PRIMARY KEY(id))")
    # Curated rows go in first and win any id clash, mirroring ItemDatabase.EnsureInit.
    db.executescript(sql.replace("INSERT INTO item", "INSERT OR IGNORE INTO item"))
    return True


def _traits(row: sqlite3.Row) -> list:
    t = []
    if row["isClothing"]:
        slot = row["clothingSlot"] or 0
        t.append(f"Clothing worn on the {CLOTHING_SLOTS[slot].lower() if slot < len(CLOTHING_SLOTS) else 'body'}")
    if row["isWeapon"]:
        skill = row["weaponSkill"] or 0
        kind = SKILLS[skill].lower() if skill < len(SKILLS) else "melee"
        t.append(f"Melee weapon ({kind}{', two-handed' if row['twoHanded'] else ''}, reach {row['weaponRange']:g} m)")
    if row["isFirearm"]:
        t.append(f"Firearm (magazine {row['magazineSize']}{', shotgun' if row['pelletsPerShot'] > 1 else ''})")
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
    db = sqlite3.connect(":memory:")
    db.row_factory = sqlite3.Row
    created = False
    for name in ("curated_items.txt", "pz_items.txt"):
        p = resources / name
        if p.exists():
            created = _load_sql(db, p, created)
    if not created:
        raise FileNotFoundError(f"No item scripts found under {resources}")

    items = []
    for row in db.execute("SELECT * FROM item ORDER BY rowid"):
        cat = row["category"] or 0
        items.append(Item(
            id=row["id"],
            name=row["displayName"] or row["id"],
            category=CATEGORIES[cat] if 0 <= cat < len(CATEGORIES) else "Misc",
            weight=float(row["weight"] or 0),
            tint=(float(row["tintR"] or 0.5), float(row["tintG"] or 0.5), float(row["tintB"] or 0.5)),
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
