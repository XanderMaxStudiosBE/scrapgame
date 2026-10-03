"""Two original compact-yard surfaces; preserves all earlier maps/materials/GUIDs.

Run python3 ArtSource/build_compact_ground.py. Uses only standard-library painting.
Packed gravel is opaque; wheel-lane shoulders fade to zero alpha. Both tile along V.
"""
from pathlib import Path
import hashlib
import json
import math
import random
import struct
import uuid
import zlib
from texture_metadata import ensure_texture_metadata

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftWorld'
SIZE = 256


def grain(x, y, seed=91):
    return ((x * 73856093 ^ y * 19349663 ^ seed * 83492791) & 65535) / 65535


def broad(x, y):
    # Periodic broad dirt variation underneath the small stones, without a grid of decals.
    return (math.sin(x * math.tau / SIZE) * 3 +
            math.cos((x + y) * math.tau / SIZE) * 4 +
            math.sin((x - y * 2) * math.tau / SIZE) * 2)


def write_png(name, pixels):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
    rows = [bytes([0]) + bytes(v for pixel in row for v in pixel) for row in reversed(pixels)]
    raw = (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', SIZE, SIZE, 8, 6, 0, 0, 0)) +
           chunk(b'IDAT', zlib.compress(b''.join(rows), 9)) + chunk(b'IEND', b''))
    p = OUT / (name + '.png')
    p.write_bytes(raw)
    ensure_texture_metadata(p, repeat=True, max_size=256, alpha_transparency=name == 'WheelLane')
    return {'size': SIZE, 'sha256': hashlib.sha256(raw).hexdigest()}


def color(values):
    return tuple(max(0, min(255, round(v))) for v in values)


def paint():
    packed = [[color((137 + broad(x, y) + (grain(x, y) - .5) * 12,
                      130 + broad(x, y) + (grain(x, y) - .5) * 11,
                      112 + broad(x, y) + (grain(x, y) - .5) * 10, 255))
               for x in range(SIZE)] for y in range(SIZE)]
    rng = random.Random(1083)
    # Small flattened angular pebbles, with restrained directional highlights rather than white noise.
    for _ in range(1350):
        cx, cy = rng.randrange(SIZE), rng.randrange(SIZE)
        rx, ry = rng.choice([1, 1, 2, 3]), rng.choice([1, 1, 2])
        shade = rng.randint(-22, 21)
        for dy in range(-ry, ry + 1):
            for dx in range(-rx, rx + 1):
                if (dx / rx) ** 2 + (dy / ry) ** 2 > 1.12:
                    continue
                value = shade + (5 if dy < 0 else -4)
                x, y = (cx + dx) % SIZE, (cy + dy) % SIZE
                packed[y][x] = color((132 + value, 129 + value, 115 + value, 255))
    lane = []
    for y in range(SIZE):
        row = []
        for x in range(SIZE):
            u = x / (SIZE - 1) * 2 - 1
            shoulder = max(0, min(1, (1 - abs(u)) / .30))
            n = grain(x, y, 73)
            tyre = .48 < abs(u) < .67
            tread = tyre and (y // 6 + x // 9) % 3 != 0
            shade = broad(x, y) * .4 + (n - .5) * 10 - (11 if tread else 0)
            end = max(0, min(1, min(y, SIZE - 1 - y) / (SIZE * .085)))
            alpha = shoulder * end * (.38 + n * .11 + (.12 if tread else 0))
            row.append(color((113 + shade, 103 + shade, 84 + shade, alpha * 255)))
        lane.append(row)
    return packed, lane


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    packed, lane = paint()
    manifest = {'source': 'Original painted gravel/dust: ArtSource/build_compact_ground.py',
                'scope': 'PNG data/import metadata; not Unity visual verification',
                'textures': {'PackedGravel': write_png('PackedGravel', packed), 'WheelLane': write_png('WheelLane', lane)}}
    p = OUT / 'compact_ground_manifest.json'
    p.write_text(json.dumps(manifest, indent=2) + '\n')
    meta = Path(str(p) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    print(json.dumps(manifest))


if __name__ == '__main__':
    main()
