"""Generates the gold line-art UI sprites for Spin The Globe.
All art is white on transparent so Unity can tint it (gold, dimmed, branch colours).
Frames are 9-slice sources (border sizes are set in UITheme.cs). Drawn at 4x and downsampled."""
import math, os, sys
from PIL import Image, ImageDraw

OUT = sys.argv[1]
S = 4
W = (255, 255, 255, 255)

def canvas(px):
    return Image.new("RGBA", (px * S, px * S), (0, 0, 0, 0))

def save(img, px, rel):
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.resize((px, px), Image.LANCZOS).save(path)

# ------------------------------------------------------------------ frames
def frame(ornate):
    px = 64
    im = canvas(px); d = ImageDraw.Draw(im)
    o, w1 = 2 * S, 2 * S             # outer line inset / width
    i, w2 = 7 * S, 1 * S              # inner line inset / width
    n = px * S - 1
    d.rectangle([o, o, n - o, n - o], outline=W, width=w1)
    if ornate:
        # inner line with notched corners + a small diamond in each corner
        c = 6 * S
        pts = [(i + c, i), (n - i - c, i), (n - i, i + c), (n - i, n - i - c), (n - i - c, n - i), (i + c, n - i), (i, n - i - c), (i, i + c), (i + c, i)]
        d.line(pts, fill=W, width=w2, joint="curve")
        for cx, cy in [(i + 1 * S, i + 1 * S), (n - i - 1 * S, i + 1 * S), (i + 1 * S, n - i - 1 * S), (n - i - 1 * S, n - i - 1 * S)]:
            r = 2.2 * S
            d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=W)
    else:
        d.rectangle([i, i, n - i, n - i], outline=(255, 255, 255, 170), width=w2)
    return im

save(frame(False), 64, "Frames/frame.png")
save(frame(True), 64, "Frames/frame_ornate.png")

# solid rounded fill (for backgrounds, 9-slice border 8)
im = canvas(32); d = ImageDraw.Draw(im)
d.rectangle([0, 0, 32 * S - 1, 32 * S - 1], fill=W)
save(im, 32, "Frames/fill.png")

# divider: thin line that fades out at both ends (vertical, 8x128)
div = Image.new("RGBA", (8, 128), (0, 0, 0, 0))
for y in range(128):
    t = 1 - abs((y - 63.5) / 63.5)
    a = int(255 * min(1, t * 2.2) ** 1.2)
    for x in range(3, 5):
        div.putpixel((x, y), (255, 255, 255, a))
os.makedirs(os.path.join(OUT, "Frames"), exist_ok=True)
div.save(os.path.join(OUT, "Frames/divider.png"))

# ------------------------------------------------------------------ icons (128 px, ~6 px lines)
PX = 128
LW = 6 * S
def P(x, y):  # icon coords are 0..128
    return (x * S, y * S)
def circle(d, cx, cy, r, w=LW, fill=None):
    d.ellipse([P(cx - r, cy - r), P(cx + r, cy + r)], outline=W if fill is None else None, width=w, fill=fill)
def line(d, pts, w=LW):
    d.line([P(*p) for p in pts], fill=W, width=w, joint="curve")
    for p in (pts[0], pts[-1]):
        x, y = P(*p); r = w / 2
        d.ellipse([x - r, y - r, x + r, y + r], fill=W)
def poly(d, pts, w=LW):
    line(d, list(pts) + [pts[0], pts[1]], w)
def arc(d, cx, cy, r, a0, a1, w=LW):
    d.arc([P(cx - r, cy - r), P(cx + r, cy + r)], a0, a1, fill=W, width=w)

def icon(name, draw):
    im = canvas(PX); d = ImageDraw.Draw(im); draw(d); save(im, PX, f"Icons/{name}.png")

def gear(d):
    cx = cy = 64; teeth = 8; ro, ri = 50, 38
    pts = []
    for k in range(teeth * 4):
        a = (k / (teeth * 4)) * 2 * math.pi + math.pi / (teeth * 4)
        r = ro if k % 4 in (1, 2) else ri
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    poly(d, pts)
    circle(d, cx, cy, 15)

def money(d):
    # stack of banknotes
    line(d, [(14, 88), (114, 88)])
    line(d, [(14, 100), (114, 100)])
    poly(d, [(14, 38), (114, 38), (114, 76), (14, 76)])
    circle(d, 64, 57, 11)
    circle(d, 30, 57, 2.5, fill=W); circle(d, 98, 57, 2.5, fill=W)

def chart(d):
    line(d, [(16, 16), (16, 112), (112, 112)])
    line(d, [(28, 92), (52, 66), (70, 80), (104, 40)])
    line(d, [(84, 40), (104, 40), (104, 60)])

def coin(d):
    circle(d, 64, 64, 48)
    circle(d, 64, 64, 36, w=3 * S)
    # small star
    pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5
        r = 18 if k % 2 == 0 else 7.5
        pts.append((64 + r * math.cos(a), 64 + r * math.sin(a)))
    d.polygon([P(*p) for p in pts], fill=W)

def tree(d):
    # skill-tree glyph: trunk branching into nodes
    line(d, [(64, 116), (64, 70)])
    line(d, [(64, 82), (36, 58), (36, 36)])
    line(d, [(64, 82), (92, 58), (92, 36)])
    line(d, [(64, 70), (64, 30)])
    line(d, [(36, 58), (18, 46)]); line(d, [(92, 58), (110, 46)])
    for (x, y, r) in [(64, 22, 9), (36, 28, 8), (92, 28, 8), (14, 42, 6), (114, 42, 6)]:
        circle(d, x, y, r, fill=W)
    line(d, [(44, 116), (84, 116)])

def pin(d):
    circle(d, 64, 42, 26)
    circle(d, 64, 42, 8, fill=W)
    line(d, [(64, 68), (64, 116)])

def folder(d):
    poly(d, [(14, 34), (48, 34), (58, 44), (114, 44), (114, 104), (14, 104)])
    line(d, [(14, 58), (114, 58)])
    line(d, [(34, 76), (78, 76)], w=4 * S); line(d, [(34, 88), (64, 88)], w=4 * S)

def glass(d):
    poly(d, [(28, 22), (100, 22), (92, 110), (36, 110)])
    line(d, [(31, 58), (97, 58)])
    poly(d, [(44, 70), (60, 66), (62, 82), (46, 86)], w=4 * S)
    poly(d, [(68, 74), (84, 70), (86, 86), (70, 90)], w=4 * S)

def box(d):
    poly(d, [(18, 46), (110, 46), (110, 110), (18, 110)])
    poly(d, [(12, 30), (116, 30), (116, 46), (12, 46)])
    line(d, [(64, 30), (64, 110)])
    # bow
    line(d, [(64, 30), (44, 12), (36, 24), (64, 30)], w=5 * S)
    line(d, [(64, 30), (84, 12), (92, 24), (64, 30)], w=5 * S)

def paper(d):
    circle(d, 64, 66, 44)
    line(d, [(30, 52), (56, 62), (52, 88)], w=4 * S)
    line(d, [(56, 62), (84, 44), (100, 58)], w=4 * S)
    line(d, [(84, 44), (78, 30)], w=4 * S)
    line(d, [(52, 88), (82, 100)], w=4 * S)

def statue(d):
    circle(d, 64, 34, 18)
    line(d, [(56, 52), (54, 62), (30, 72), (26, 96), (102, 96), (98, 72), (74, 62), (72, 52)])
    poly(d, [(20, 96), (108, 96), (108, 114), (20, 114)])

def close(d):
    line(d, [(30, 30), (98, 98)]); line(d, [(98, 30), (30, 98)])

def stamp(d):
    circle(d, 64, 24, 14)
    line(d, [(58, 38), (56, 60)]); line(d, [(70, 38), (72, 60)])
    poly(d, [(24, 60), (104, 60), (104, 82), (24, 82)])
    line(d, [(18, 106), (110, 106)])

def ice(d):
    poly(d, [(64, 14), (110, 38), (64, 62), (18, 38)])
    line(d, [(18, 38), (18, 88), (64, 114), (110, 88), (110, 38)])
    line(d, [(64, 62), (64, 114)])
    line(d, [(34, 60), (40, 70)], w=4 * S); line(d, [(84, 76), (94, 70)], w=4 * S)

for n, f in [("ice", ice), ("gear", gear), ("money", money), ("chart", chart), ("coin", coin), ("tree", tree), ("pin", pin), ("folder", folder),
             ("glass", glass), ("box", box), ("paper", paper), ("statue", statue), ("close", close), ("stamp", stamp)]:
    icon(n, f)
print("ok")
