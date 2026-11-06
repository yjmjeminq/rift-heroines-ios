"""Collect the exact prompts for all twelve deployed character sheets."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ROOT / "Assets/Rift/ArtSources"
naruto = json.loads((SOURCES / "prompts-naruto-v0.2.0.json").read_text(encoding="utf-8"))
bleach = json.loads((SOURCES / "prompts-bleach-final-v0.3.0.json").read_text(encoding="utf-8"))
expansion = json.loads((SOURCES / "prompts-expansion-v0.4.0.json").read_text(encoding="utf-8"))
roster = json.loads((ROOT / "Assets/Rift/Resources/Rift/canonical-roster.json").read_text(encoding="utf-8"))
identities = {c["id"]: c for c in roster["characters"]}
legacy_refs = {"sakura": "kaen", "tsunade": "saya", "hinata": "fuka", "rangiku": "setsuna", "yoruichi": "raika", "unohana": "shion"}
by_id = {c["id"]: c for c in naruto["characters"] + bleach["characters"] + expansion["characters"]}
characters = [by_id[c["id"]] for c in roster["characters"]]
for character in characters:
    hero_id = character["id"]
    character["referenceAtGeneration"] = character.get("reference")
    if hero_id in legacy_refs:
        character["archivedReference"] = "ReferenceArchive/Art/v0.2.0-original-cast/" + legacy_refs[hero_id] + ".png"
    character["finalAsset"] = "Assets/Rift/Resources/Rift/Art/" + hero_id + ".png"
    character["canonicalName"] = identities[hero_id]["canonicalName"]
    character["franchise"] = identities[hero_id]["franchise"]
    character["sourceUrl"] = identities[hero_id]["sourceUrl"]
    character.pop("reference", None)

manifest = {
    "version": roster["version"],
    "tool": "built-in image_gen",
    "mode": "twelve canonical adult women, six per franchise, in eight-head-tall bikini battle outfits",
    "grid": {"columns": 3, "rows": 2, "cellWidth": 512, "cellHeight": 512},
    "characters": characters,
    "background": "Assets/Rift/Resources/Rift/Art/moonbridge.png (retained)",
    "backgroundPromptArchive": "ReferenceArchive/Art/v0.1.0/prompts.json",
    "chapterEnvironments": "Assets/Rift/ArtSources/environments-v0.7.0.json",
    "backgrounds": ["Assets/Rift/Resources/Rift/Art/moonbridge.png", "Assets/Rift/Resources/Rift/Stages/stormshrine.png", "Assets/Rift/Resources/Rift/Stages/eclipsecitadel.png"],
}
(SOURCES / "prompts.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
print("Recorded current prompts:", ", ".join(c["id"] for c in characters))
