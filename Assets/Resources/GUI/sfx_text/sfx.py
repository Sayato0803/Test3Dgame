import math, random
from PIL import Image, ImageDraw, ImageFont, ImageChops

FONT = "/usr/share/fonts/opentype/inter/Inter-BlackItalic.otf"
OUT = "/tmp/claude-0/-home-claude/ee7dd7b3-968c-5d58-a838-9ff5e37c8fed/scratchpad/sfx/"
S = 1024  # canvas
BLACK = (20, 20, 20, 255)

# word, text color, halftone dot color, burst fill, burst style
WORDS = [
    ("BONK!",  (230, 40, 35),  (170, 20, 20),  (255, 200, 30), "spiky"),
    ("ZAP!",   (255, 255, 255), (60, 60, 60),  (255, 230, 60), "spiky"),
    ("POW!",   (255, 220, 40), (230, 120, 20), (230, 40, 35),  "spiky"),
    ("SHOCK!", (40, 40, 40),   (90, 90, 90),   (255, 255, 255), "spiky"),
    ("WHOOSH", (255, 205, 30), (240, 130, 20), (255, 255, 255), "text"),
    ("K.O.",   (255, 255, 255), (200, 200, 200), (230, 40, 35), "spiky"),
    ("CRIT!",  (255, 120, 20), (200, 60, 10),  (255, 230, 60), "spiky"),
    ("WIN!",   (255, 220, 40), (230, 140, 20), (60, 170, 255), "spiky"),
    ("LOSE",   (200, 200, 210), (120, 120, 140), (70, 70, 90), "cloud"),
    ("SWEEP!", (120, 225, 255), (40, 140, 220), (255, 255, 255), "spiky"),
]

def burst(cx, cy, r_out, r_in, n, seed):
    rnd = random.Random(seed)
    pts = []
    for i in range(n * 2):
        a = math.pi * i / n + rnd.uniform(-0.08, 0.08)
        r = (r_out * rnd.uniform(0.85, 1.1)) if i % 2 == 0 else r_in * rnd.uniform(0.9, 1.05)
        pts.append((cx + r * math.cos(a) * 1.25, cy + r * math.sin(a) * 0.8))
    return pts

def cloud(cx, cy, rx, ry, n, seed):
    rnd = random.Random(seed)
    pts = []
    for i in range(n * 8):
        a = 2 * math.pi * i / (n * 8)
        bump = abs(math.sin(a * n / 2)) ** 0.5
        k = 0.82 + 0.18 * bump + rnd.uniform(-0.01, 0.01)
        pts.append((cx + rx * k * math.cos(a), cy + ry * k * math.sin(a)))
    return pts

def halftone(size, color, step=14, maxr=4.5):
    im = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    w, h = size
    for y in range(0, h, step):
        for x in range(0, w, step):
            off = step / 2 if (y // step) % 2 else 0
            t = y / h  # dots bigger toward bottom
            r = maxr * (0.35 + 0.65 * t)
            d.ellipse([x + off - r, y - r, x + off + r, y + r], fill=color + (255,))
    return im

def text_mask(word, font, size):
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    bb = d.textbbox((0, 0), word, font=font)
    tw, th = bb[2] - bb[0], bb[3] - bb[1]
    x = (size[0] - tw) / 2 - bb[0]
    y = (size[1] - th) / 2 - bb[1]
    return m, (x, y), font

def fit_font(word, maxw):
    sz = 400
    while sz > 40:
        f = ImageFont.truetype(FONT, sz)
        bb = f.getbbox(word)
        if bb[2] - bb[0] <= maxw:
            return f
        sz -= 10
    return ImageFont.truetype(FONT, sz)

def render(word, tcol, dcol, bcol, style, idx):
    canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    cx, cy = S / 2, S / 2
    # burst shape: black outline + fill ("text" style = chỉ chữ + viền, không nền)
    if style != "text":
        if style == "spiky":
            poly = burst(cx, cy, 400, 270, 14, idx)
        else:
            poly = cloud(cx, cy, 470, 300, 9, idx)
        shadow = [(x + 18, y + 18) for x, y in poly]
        d.polygon(shadow, fill=BLACK)
        d.polygon(poly, fill=BLACK)
        inner = [(cx + (x - cx) * 0.95, cy + (y - cy) * 0.93) for x, y in poly]
        d.polygon(inner, fill=bcol + (255,))
        # light halftone on burst
        burst_mask = Image.new("L", (S, S), 0)
        ImageDraw.Draw(burst_mask).polygon(inner, fill=90)
        ht = halftone((S, S), tuple(max(0, c - 40) for c in bcol), step=18, maxr=3.5)
        ht.putalpha(ImageChops.multiply(ht.getchannel("A"), burst_mask))
        canvas.alpha_composite(ht)

    # text
    font = fit_font(word, 760 if style == "spiky" else 820)
    d = ImageDraw.Draw(canvas)
    bb = d.textbbox((0, 0), word, font=font)
    x = (S - (bb[2] - bb[0])) / 2 - bb[0]
    y = (S - (bb[3] - bb[1])) / 2 - bb[1]
    stroke = max(14, font.size // 12)
    # drop shadow layer (3D extrude)
    for k in range(1, 15):
        d.text((x + k, y + k), word, font=font, fill=BLACK, stroke_width=stroke, stroke_fill=BLACK)
    d.text((x, y), word, font=font, fill=BLACK, stroke_width=stroke, stroke_fill=BLACK)
    # fill with text color + halftone gradient
    tm = Image.new("L", (S, S), 0)
    ImageDraw.Draw(tm).text((x, y), word, font=font, fill=255)
    fill = Image.new("RGBA", (S, S), tcol + (255,))
    fill.alpha_composite(halftone((S, S), dcol, step=12, maxr=4.2))
    fill.putalpha(tm)
    canvas.alpha_composite(fill)
    # highlight line on top of letters
    hl = Image.new("L", (S, S), 0)
    ImageDraw.Draw(hl).text((x - 4, y - 5), word, font=font, fill=255)
    hl = ImageChops.subtract(tm, ImageChops.offset(tm, 6, 8))
    white = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    white.putalpha(hl.point(lambda v: int(v * 0.55)))
    canvas.alpha_composite(white)

    bbox = canvas.getbbox()
    canvas = canvas.crop(bbox)
    name = word.strip("!.").replace(".", "").lower()
    canvas.save(OUT + f"sfx_{name}.png")

if __name__ == "__main__":
    import os
    os.makedirs(OUT, exist_ok=True)
    for i, w in enumerate(WORDS):
        render(*w, i)
