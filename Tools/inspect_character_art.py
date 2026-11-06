"""Inspect generated sheets and record sprite ground offsets without altering PNGs."""
from pathlib import Path
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/Rift/Resources/Rift/Art"
ROSTER = json.loads((ROOT / "Assets/Rift/Resources/Rift/canonical-roster.json").read_text(encoding="utf-8"))
heroes = []
def body_bounds(mask):
    """Ignore detached effects and any tiny neighboring-cell bleed when locating soles."""
    w, h = mask.size
    data = mask.tobytes()
    seen = bytearray(len(data))
    best_count, best_box = 0, None
    for start, alpha in enumerate(data):
        if alpha < 160 or seen[start]:
            continue
        pending = [start]
        seen[start] = 1
        count = 0
        left, top, right, bottom = w, h, 0, 0
        while pending:
            index = pending.pop()
            x, y = index % w, index // w
            count += 1
            left, top = min(left, x), min(top, y)
            right, bottom = max(right, x + 1), max(bottom, y + 1)
            candidates = []
            if x: candidates.append(index - 1)
            if x + 1 < w: candidates.append(index + 1)
            if y: candidates.append(index - w)
            if y + 1 < h: candidates.append(index + w)
            for neighbor in candidates:
                if not seen[neighbor] and data[neighbor] >= 160:
                    seen[neighbor] = 1
                    pending.append(neighbor)
        if count > best_count:
            best_count, best_box = count, (left, top, right, bottom)
    return best_box

for hero_id in (c["id"] for c in ROSTER["characters"]):
    image = Image.open(ART / f"{hero_id}.png")
    assert image.mode == "RGBA", f"{hero_id}: alpha channel missing"
    alpha = image.getchannel("A")
    assert alpha.getextrema()[0] == 0, f"{hero_id}: transparency missing"
    assert image.width % 3 == 0 and image.height % 2 == 0
    cell_w, cell_h = image.width // 3, image.height // 2
    assert cell_w == cell_h, f"{hero_id}: cells must be square"
    footers, bounds = [], []
    for frame in range(6):
        col, row = frame % 3, frame // 3
        cell = alpha.crop((col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h))
        box = body_bounds(cell)
        assert box and box[3] - box[1] > cell_h * .65, f"{hero_id}: incomplete frame {frame}"
        footers.append(round(max(0, cell_h - box[3] - 2) / cell_h, 7))
        bounds.append(list(box))
    heroes.append({"id": hero_id, "width": image.width, "height": image.height, "frameFooters": footers, "bounds": bounds})

layout = {"version": ROSTER["version"], "columns": 3, "rows": 2, "heroes": heroes}
(ROOT / "Assets/Rift/Resources/Rift/art-layout.json").write_text(json.dumps(layout, indent=2), encoding="utf-8")
print(json.dumps({"characters": len(heroes), "frames": len(heroes) * 6, "transparent": True, "ids": [h["id"] for h in heroes]}, indent=2))
