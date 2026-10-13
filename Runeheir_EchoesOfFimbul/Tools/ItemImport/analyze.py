"""Survey of the reference data: what XileRetro's client lists, and how much rAthena's pre-renewal database covers.

    "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" Tools/ItemImport/analyze.py
"""

import collections
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import ro_parse  # noqa: E402


def main():
    server = ro_parse.read_items()
    client_main = ro_parse.read_item_info(os.path.join(ro_parse.CLIENT_SYSTEM, "ItemInfo.lub"))
    client_alt = ro_parse.read_item_info(os.path.join(ro_parse.CLIENT_SYSTEM, "itemInfo_.lub"))
    print("rAthena pre-re items:", len(server))
    print("client ItemInfo.lub:", len(client_main), " itemInfo_.lub:", len(client_alt))
    a, b = set(client_main), set(client_alt)
    print("  in both:", len(a & b), " only ItemInfo:", len(a - b), " only itemInfo_:", len(b - a))
    for name, client in (("ItemInfo.lub", client_main), ("itemInfo_.lub", client_alt)):
        ids = set(client)
        print(f"\n{name}: with server stats {len(ids & set(server))}, client-only (custom) {len(ids - set(server))}, "
              f"costumes {sum(1 for v in client.values() if v['costume'])}")
        types = collections.Counter()
        for item_id, info in client.items():
            t = ro_parse.description_field(info["description"], "Type") or "?"
            types[t.split()[0] if t else "?"] += 1
        print("  description types:", types.most_common(14))
    # server-side types and weapon subtypes for the ids the main client lists
    kinds = collections.Counter()
    subs = collections.Counter()
    for item_id in client_main:
        entry = server.get(item_id)
        if entry is None:
            continue
        kinds[entry.get("Type", "Etc")] += 1
        if entry.get("Type") == "Weapon":
            subs[entry.get("SubType")] += 1
    print("\nserver types for ItemInfo ids:", kinds.most_common())
    print("weapon subtypes:", subs.most_common())
    sample = server.get(1201)
    print("\nsample 1201:", {k: sample[k] for k in list(sample)[:14]})
    scripts = [e for e in server.values() if e.get("Script")]
    print("items with scripts:", len(scripts))


if __name__ == "__main__":
    main()
