"""Fetches the CC0 Poly Haven assets the art direction samples are built from (Runeheir > Art > Render Style Samples).

Models come as 1k FBX with their textures; ground textures as 2k color, normal (GL) and roughness; the sky as a 2k HDR.
About 250 MB in all, written to Assets/_Runeheir/ThirdParty/PolyHaven (git-ignored). Everything on Poly Haven is CC0 1.0
(public domain): no attribution required, free for commercial use.

Usage (from Runeheir_EchoesOfFimbul/):  python Tools/ArtSamples/fetch_polyhaven.py
"""
import json
import os
import sys
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
DEST = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Runeheir", "ThirdParty", "PolyHaven"))
TEXTURES = ["leafy_grass", "forest_ground_04", "grass_path_2", "aerial_rocks_02"]
HDRIS = ["kloofendal_48d_partly_cloudy_puresky"]
MODELS = ["fir_sapling", "tree_small_02", "dead_tree_trunk", "tree_stump_01", "boulder_01", "rock_moss_set_01", "shrub_01", "shrub_02",
          "fern_02", "grass_medium_01", "moss_01", "stone_fire_pit", "wooden_crate_01", "Lantern_01", "treasure_chest"]
AGENT = {"User-Agent": "Runeheir-asset-fetch"}


def files(asset):
    request = urllib.request.Request("https://api.polyhaven.com/files/" + asset, headers=AGENT)
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def get(url, target):
    if not url.startswith("https://dl.polyhaven.org/"):
        raise SystemExit("refusing a download outside dl.polyhaven.org: " + url)
    os.makedirs(os.path.dirname(target), exist_ok=True)
    if os.path.exists(target):
        return 0
    with urllib.request.urlopen(urllib.request.Request(url, headers=AGENT), timeout=300) as response:
        data = response.read()
    with open(target, "wb") as out:
        out.write(data)
    return len(data)


def main():
    total = 0
    for asset in TEXTURES:
        d = files(asset)
        for key, suffix in (("Diffuse", "diff"), ("nor_gl", "nor_gl"), ("Rough", "rough")):
            total += get(d[key]["2k"]["jpg"]["url"], os.path.join(DEST, "Textures", asset, "%s_%s_2k.jpg" % (asset, suffix)))
        print("texture", asset, flush=True)
    for asset in HDRIS:
        total += get(files(asset)["hdri"]["2k"]["hdr"]["url"], os.path.join(DEST, "Sky", asset + "_2k.hdr"))
        print("sky", asset, flush=True)
    for asset in MODELS:
        entry = files(asset)["fbx"]["1k"]["fbx"]
        folder = os.path.join(DEST, "Models", asset)
        total += get(entry["url"], os.path.join(folder, asset + ".fbx"))
        for relative, include in entry.get("include", {}).items():
            total += get(include["url"], os.path.join(folder, relative.replace("/", os.sep)))
        print("model", asset, flush=True)
    with open(os.path.join(DEST, "LICENSE.txt"), "w", encoding="utf-8") as out:
        out.write("These assets come from Poly Haven (https://polyhaven.com) under CC0 1.0 (public domain). No attribution is required.\n")
    print("downloaded %.1f MB into %s" % (total / 1e6, DEST))
    return 0


if __name__ == "__main__":
    sys.exit(main())
