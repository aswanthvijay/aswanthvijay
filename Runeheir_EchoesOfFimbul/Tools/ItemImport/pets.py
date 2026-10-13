"""Phase 8: XileRetro's pets (rAthena pet_db.yml) as Runeheir pets.

Each pet: the monster it's tamed from, the taming item, the egg, its food, its armor, how much a meal fills it, how
much a meal endears it, the capture rate, and the bonus it gives once Loyal (the pet script, read as if Loyal).
"""

import os

import ro_parse
import script_translate as st


def read_pets():
    path = os.path.join(ro_parse.SERVER_DB, "pet_db.yml")
    return ro_parse.read_yaml(path) if os.path.exists(path) else []


def build_pets(aegis_ids, monster_ids_by_aegis, resolve, extra=()):
    """
    C# PetDefinition initializers for every pet whose monster, egg and taming item made it into the game. extra: more
    pet_db-style entries (XileRetro's own pets, which have no taming item and no Loyal bonus).
    """
    out = []
    for entry in list(read_pets()) + list(extra):
        mob = monster_ids_by_aegis.get(str(entry.get("Mob")))
        egg = resolve(aegis_ids.get(str(entry.get("EggItem"))))
        tame = resolve(aegis_ids.get(str(entry.get("TameItem"))))
        if not (mob and egg):
            continue
        food = resolve(aegis_ids.get(str(entry.get("FoodItem"))))
        armor = resolve(aegis_ids.get(str(entry.get("EquipItem"))))
        script = str(entry.get("Script") or "")
        script = script.replace("getpetinfo(PETINFO_INTIMATE)", "1000").replace("PET_INTIMATE_LOYAL", "900")
        result = st.translate(script, False)
        effect = st.cs_effect(result["base"], buffs=st.BuffNamer(egg, "Pet"))
        bonus = st.describe(result)
        fields = [f'EggItemId = "{egg}", MonsterId = "{mob}"']
        if tame:
            fields.append(f'TameItemId = "{tame}"')
        if food:
            fields.append(f'FoodItemId = "{food}"')
        if armor:
            fields.append(f'ArmorItemId = "{armor}"')
        fields.append(f"CaptureRate = {st._float(int(entry.get('CaptureRate') or 1000) / 100)}, "
                      f"Fullness = {int(entry.get('Fullness') or 3)}, IntimacyFed = {int(entry.get('IntimacyFed') or 50)}")
        if effect:
            fields.append(f"LoyalBonus = {effect}")
        if bonus:
            fields.append(f"LoyalText = {st.cs_string(bonus)}")
        out.append((egg, tame, fields))
    return out
