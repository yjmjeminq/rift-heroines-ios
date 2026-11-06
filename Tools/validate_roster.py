"""Audit the complete active roster and resources against checked canonical identities."""
from pathlib import Path
from urllib.parse import urlparse
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
RESOURCE = ROOT / "Assets/Rift/Resources/Rift"
EXPECTED = (
    ("sakura", "사쿠라", "NARUTO"), ("tsunade", "츠나데", "NARUTO"), ("hinata", "히나타", "NARUTO"),
    ("ino", "이노", "NARUTO"), ("temari", "테마리", "NARUTO"), ("konan", "코난", "NARUTO"),
    ("rangiku", "란기쿠", "BLEACH"), ("yoruichi", "요루이치", "BLEACH"), ("unohana", "우노하나", "BLEACH"),
    ("rukia", "루키아", "BLEACH"), ("orihime", "오리히메", "BLEACH"), ("soifon", "소이폰", "BLEACH"),
)
catalog = json.loads((RESOURCE / "canonical-roster.json").read_text(encoding="utf-8"))
layout = json.loads((RESOURCE / "art-layout.json").read_text(encoding="utf-8"))
prompts = json.loads((ROOT / "Assets/Rift/ArtSources/prompts.json").read_text(encoding="utf-8"))
source = (ROOT / "Assets/Rift/Runtime/RiftData.cs").read_text(encoding="utf-8")
version = re.search(r'const string Version = "([^"]+)"', source).group(1)
active = re.findall(r'new HeroDefinition\("([^"]+)",\s*"([^"]+)"', source)
assert active == [(i, n) for i, n, _ in EXPECTED], active
assert catalog["version"] == layout["version"] == prompts["version"] == version
assert [(c["id"], c["displayName"], c["franchise"]) for c in catalog["characters"]] == list(EXPECTED)
ids = [c[0] for c in EXPECTED]
assert [h["id"] for h in layout["heroes"]] == ids
assert [c["id"] for c in prompts["characters"]] == ids
assert {p.stem for p in (RESOURCE / "Art").glob("*.png")} == set(ids + ["moonbridge"])
assert not (ROOT / "Assets/Rift/ArtSources/Archive").exists(), "Obsolete character archive is still in Unity assets"
for c, frame, prompt in zip(catalog["characters"], layout["heroes"], prompts["characters"]):
    url = urlparse(c["sourceUrl"])
    assert url.scheme == "https" and url.netloc in {"naruto-official.com", "bleach-ros.bn-ent.net", "www.bleach-bravesouls.com"}
    assert c["canonicalName"] and c["sourceName"] and c["originalName"]
    assert frame["width"] == 1536 and frame["height"] == 1024 and len(frame["frameFooters"]) == 6
    assert prompt["canonicalName"] == c["canonicalName"] and prompt["finalAsset"].endswith("/" + c["id"] + ".png")
legacy = re.compile(r'\b(?:kaen|saya|fuka|setsuna|raika|shion)\b|카엔|사야|후우카|세츠나|라이카|시온|백야 사신단', re.I)
for path in (ROOT / "Assets/Rift/Runtime").glob("*.cs"):
    assert not legacy.search(path.read_text(encoding="utf-8")), "Obsolete character reference: " + str(path)
for name in ("README.md", "Docs/ART.md", "Docs/DESIGN.md", "Docs/VERIFICATION.md"):
    assert not legacy.search((ROOT / name).read_text(encoding="utf-8")), "Obsolete current documentation: " + name
assert sum(c["franchise"] == "NARUTO" for c in catalog["characters"]) == 6
assert sum(c["franchise"] == "BLEACH" for c in catalog["characters"]) == 6
report = {"passed": True, "version": version, "activeCount": len(ids), "frames": len(ids) * 6, "canonicalOnly": True,
    "identities": [{"id": c["id"], "name": c["canonicalName"], "franchise": c["franchise"], "source": c["sourceUrl"],
                    "assetSha256": hashlib.sha256((RESOURCE / "Art" / (c["id"] + ".png")).read_bytes()).hexdigest()} for c in catalog["characters"]]}
(ROOT / "BuildLogs/roster-audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({k: report[k] for k in ("passed", "version", "activeCount", "frames", "canonicalOnly")}, indent=2))
