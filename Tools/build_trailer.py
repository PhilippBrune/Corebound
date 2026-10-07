# Rough-cut trailer from autopilot frame sequences (Docs/STEAM_PAGE.md, section 3).
#
# 1. Record: write "trailer class catalyst arsenal god 1" into AutopilotRequest.txt; the run leaves
#    30 fps JPEG clips under Temp/Trailer/<time>/ (Unity deletes Temp when the Editor closes).
# 2. Build: python Tools/build_trailer.py [clip folder] [output.mp4] [music file name]
#    Defaults: the newest Temp/Trailer folder, Builds/Steam/corebound_trailer_roughcut.mp4 and
#    music_boss_stage5_loop.mp3. Needs Pillow, numpy and an ffmpeg (imageio-ffmpeg's, or one on PATH).
#
# Python composes every output frame (cuts, crossfades, text cards, stills) and pipes raw RGB to
# ffmpeg, which encodes H.264 and lays the game's own music underneath. Each clip is cut to its
# busiest stretch by frame-to-frame motion, and the music starts at its loudest stretch - measured,
# because nothing in the pipeline listens to it.
import glob, os, subprocess, sys, io
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

try:
    import imageio_ffmpeg
    FF = imageio_ffmpeg.get_ffmpeg_exe()
except ImportError:
    FF = 'ffmpeg'
PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace('\\', '/') + '/'
SHOTS = PROJECT + 'Temp/AutopilotShots/'
OUT = sys.argv[2] if len(sys.argv) > 2 else PROJECT + 'Builds/Steam/corebound_trailer_roughcut.mp4'
W, H, FPS = 1920, 1080, 30
XF = 8                       # crossfade frames between entries
FONT = 'C:/Windows/Fonts/bahnschrift.ttf'   # Windows 10/11; any bold sans works
CORE_BLUE = (120, 200, 255)
AMBER = (255, 176, 64)

root = sys.argv[1] if len(sys.argv) > 1 else sorted(glob.glob(PROJECT + 'Temp/Trailer/*'))[-1]
segments = {}
for folder in sorted(glob.glob(root + '/*')):
    tag = os.path.basename(folder).split('_', 1)[1]
    frames = sorted(glob.glob(folder + '/f*.jpg'))
    if frames:
        segments[tag] = frames
print('segments', {k: len(v) for k, v in segments.items()})

def font(size, bold=True):
    f = ImageFont.truetype(FONT, size)
    try:
        f.set_variation_by_name('Bold' if bold else 'Regular')
    except Exception:
        pass
    return f

def text_layer(lines, anchor='left', y=None):
    """A transparent layer with big labels and a soft dark glow behind them."""
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    y = y if y is not None else H - 300
    boxes = []
    for text, size, color in lines:
        f = font(size)
        w = d.textlength(text, font=f)
        x = 120 if anchor == 'left' else (W - w) / 2
        boxes.append((x, y, text, f, color))
        y += int(size * 1.15)
    glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for x, yy, text, f, color in boxes:
        gd.text((x, yy), text, font=f, fill=(0, 0, 0, 230))
    glow = glow.filter(ImageFilter.GaussianBlur(14))
    layer = Image.alpha_composite(layer, glow)
    d = ImageDraw.Draw(layer)
    for x, yy, text, f, color in boxes:
        d.text((x, yy), text, font=f, fill=color + (255,))
    return layer

def load(path):
    im = Image.open(path).convert('RGB')
    return im if im.size == (W, H) else im.resize((W, H), Image.LANCZOS)

logo = Image.open(PROJECT + 'Assets/Art/UI/MainMenu/corebound_logo.png').convert('RGBA')
menu_bg = Image.open(PROJECT + 'Assets/Art/UI/MainMenu/mainmenu_background.png').convert('RGB')

_motion = {}
def busiest_start(tag, n):
    """The window of n frames with the most frame-to-frame change: explosions, pulses, movement."""
    frames = segments[tag]
    if tag not in _motion:
        small = [np.asarray(Image.open(p).convert('L').resize((96, 54)), dtype=np.float32) for p in frames]
        _motion[tag] = np.array([0.0] + [float(np.abs(small[i] - small[i - 1]).mean()) for i in range(1, len(small))])
    m = _motion[tag]
    if len(m) <= n:
        return 0
    sums = np.convolve(m, np.ones(n), mode='valid')
    # A clip used twice (the cold open and the finale) must not show the same seconds twice.
    for a, b in _used.get(tag, []):
        sums[max(0, a - n + 1):b] = -1
    best = int(np.argmax(sums))
    _used.setdefault(tag, []).append((best, best + n))
    return best

_used = {}

def clip(tag, start, seconds, label=None):
    frames = segments.get(tag)
    if not frames:
        print('missing segment', tag)
        return None
    n = int(seconds * FPS)
    if start == 'auto':
        start = busiest_start(tag, n)
    start = min(start, max(0, len(frames) - n))
    picks = frames[start:start + n]
    return {'kind': 'clip', 'frames': picks, 'label': label}

def still(path, seconds, label=None, zoom=(1.0, 1.08)):
    return {'kind': 'still', 'image': load(path), 'n': int(seconds * FPS), 'label': label, 'zoom': zoom}

def card(kind, seconds):
    return {'kind': kind, 'n': int(seconds * FPS)}

def first_shot(pattern):
    hits = sorted(glob.glob(SHOTS + pattern))
    return hits[-1] if hits else None

# ---- the edit --------------------------------------------------------------------------------
edit = []
def add(entry):
    if entry is not None:
        edit.append(entry)

add(clip('s5_boss', 'auto', 3.0))                                              # cold open: the finale
add(card('title', 3.5))
add(clip('s1_combat1', 'auto', 3.2, [('FIGHT', 120, (255, 255, 255)), ('up to six weapons at once', 44, CORE_BLUE)]))
add(clip('s1_combat2', 'auto', 2.4))
add(clip('s2_combat1', 'auto', 3.2, [('BUILD', 120, (255, 255, 255)), ('towers that guard the Core', 44, CORE_BLUE)]))
add(clip('s2_combat2', 'auto', 2.4))
# A plain wave reward with weapon cards; a boss reward carries a voice line under the label.
reward = first_shot('stage1_wave3_reward.png') or first_shot('stage*_wave*_reward.png')
if reward:
    add(still(reward, 2.6, [('DRAFT', 120, (255, 255, 255)), ('a build, one card at a time', 44, CORE_BLUE)]))
add(clip('s3_combat1', 'auto', 3.2, [('ENGINEER', 120, (255, 255, 255)), ('a production chain that pays back', 44, CORE_BLUE)]))
add(clip('s3_combat2', 'auto', 2.4))
add(clip('s4_combat1', 'auto', 2.6, [('FIVE STAGES', 96, (255, 255, 255))]))
add(clip('s5_combat1', 'auto', 2.6))
classes = first_shot('class_select_*.png')
if classes:
    add(still(classes, 2.4, [('FIVE CLASSES', 96, (255, 255, 255))]))
add(clip('s1_boss', 'auto', 2.2, [('FIVE BOSSES', 96, AMBER)]))
add(clip('s2_boss', 'auto', 2.0))
add(clip('s3_boss', 'auto', 2.0))
add(clip('s4_boss', 'auto', 2.0))
add(clip('s5_boss', 'auto', 3.0, [('PROTECT THE CORE', 110, (255, 255, 255))]))
add(card('end', 5.0))

# ---- rendering -------------------------------------------------------------------------------
def title_frame(i, n):
    t = i / max(1, n - 1)
    s = 1.0 + 0.05 * t
    bw, bh = int(W * s), int(H * s)
    bg = menu_bg.resize((bw, bh), Image.LANCZOS).crop(((bw - W) // 2, (bh - H) // 2, (bw - W) // 2 + W, (bh - H) // 2 + H))
    bg = Image.blend(Image.new('RGB', (W, H)), bg, 0.55)
    a = min(1.0, i / (0.8 * FPS))
    lw = 980
    lg = logo.resize((lw, int(logo.height * lw / logo.width)), Image.LANCZOS)
    alpha = lg.split()[3].point(lambda v: int(v * a))
    lg.putalpha(alpha)
    frame = bg.convert('RGBA')
    frame.alpha_composite(lg, ((W - lw) // 2, (H - lg.height) // 2 - 60))
    if i > 0.9 * FPS:
        sub = text_layer([('Defend a living Core. Fight, build, draft, engineer.', 46, (235, 240, 250))], 'center', H // 2 + lg.height // 2 - 10)
        sub.putalpha(sub.split()[3].point(lambda v: int(v * min(1.0, (i - 0.9 * FPS) / (0.6 * FPS)))))
        frame.alpha_composite(sub)
    return frame.convert('RGB')

def end_frame(i, n):
    bg = Image.blend(Image.new('RGB', (W, H)), menu_bg.resize((W, H), Image.LANCZOS), 0.35).convert('RGBA')
    lw = 860
    lg = logo.resize((lw, int(logo.height * lw / logo.width)), Image.LANCZOS)
    bg.alpha_composite(lg, ((W - lw) // 2, 170))
    bg.alpha_composite(text_layer([('EARLY ACCESS', 64, AMBER), ('Wishlist on Steam', 52, (240, 244, 250))], 'center', 170 + lg.height + 40))
    return bg.convert('RGB')

def still_frame(entry, i):
    z0, z1 = entry['zoom']
    s = z0 + (z1 - z0) * i / max(1, entry['n'] - 1)
    im = entry['image']
    bw, bh = int(W * s), int(H * s)
    return im.resize((bw, bh), Image.BILINEAR).crop(((bw - W) // 2, (bh - H) // 2, (bw - W) // 2 + W, (bh - H) // 2 + H))

layers = {}
def with_label(frame, entry, i, n):
    if not entry.get('label'):
        return frame
    key = id(entry)
    if key not in layers:
        layers[key] = text_layer(entry['label'])
    a = min(1.0, i / 6.0, (n - i) / 6.0)
    lay = layers[key]
    if a < 1.0:
        lay = lay.copy()
        lay.putalpha(lay.split()[3].point(lambda v: int(v * a)))
    out = frame.convert('RGBA')
    out.alpha_composite(lay)
    return out.convert('RGB')

def frames_of(entry):
    if entry['kind'] == 'clip':
        n = len(entry['frames'])
        for i, p in enumerate(entry['frames']):
            yield with_label(load(p), entry, i, n)
    elif entry['kind'] == 'still':
        for i in range(entry['n']):
            yield with_label(still_frame(entry, i), entry, i, entry['n'])
    elif entry['kind'] == 'title':
        for i in range(entry['n']):
            yield title_frame(i, entry['n'])
    elif entry['kind'] == 'end':
        for i in range(entry['n']):
            yield end_frame(i, entry['n'])

total = sum((len(e['frames']) if e['kind'] == 'clip' else e['n']) for e in edit) - XF * (len(edit) - 1)
seconds = total / FPS
print('entries', len(edit), 'frames', total, 'seconds', round(seconds, 1))

# The loudest stretch of the music, measured, since nobody here can listen to it.
music = PROJECT + 'Assets/Audio/Music/' + (sys.argv[3] if len(sys.argv) > 3 else 'music_boss_stage5_loop.mp3')
pcm = subprocess.run([FF, '-v', 'error', '-i', music, '-ac', '1', '-ar', '8000', '-f', 's16le', '-'], capture_output=True).stdout
x = np.frombuffer(pcm, dtype=np.int16).astype(np.float32)
rms = np.sqrt(np.convolve(x * x, np.ones(8000) / 8000, mode='valid')[::8000])
span = int(np.ceil(seconds))
best = max(range(0, max(1, len(rms) - span)), key=lambda s0: rms[s0:s0 + span].mean() + 0.5 * rms[s0:s0 + 4].mean())
print('music', os.path.basename(music), 'start at', best, 's; mean rms', round(float(rms[best:best + span].mean())), 'track mean', round(float(rms.mean())))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
cmd = [FF, '-y', '-v', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', '%dx%d' % (W, H), '-r', str(FPS), '-i', '-',
       '-ss', str(best), '-t', '%.2f' % seconds, '-i', music,
       '-filter_complex', '[1:a]afade=t=in:d=0.6,afade=t=out:st=%.2f:d=2.5,volume=0.9[a]' % (seconds - 2.5),
       '-map', '0:v', '-map', '[a]', '-c:v', 'libx264', '-preset', 'slow', '-crf', '18', '-pix_fmt', 'yuv420p',
       '-profile:v', 'high', '-movflags', '+faststart', '-c:a', 'aac', '-b:a', '192k', '-shortest', OUT]
enc = subprocess.Popen(cmd, stdin=subprocess.PIPE)

tail = []          # the last XF frames of the previous entry, for the crossfade
written = 0
for k, entry in enumerate(edit):
    gen = frames_of(entry)
    if tail:
        for j in range(XF):
            nxt = next(gen)
            a = (j + 1) / (XF + 1)
            enc.stdin.write(Image.blend(tail[j], nxt, a).tobytes()); written += 1
    buf = []
    for fr in gen:
        buf.append(fr)
        if len(buf) > XF:
            enc.stdin.write(buf.pop(0).tobytes()); written += 1
    if k == len(edit) - 1:
        for fr in buf:
            enc.stdin.write(fr.tobytes()); written += 1
        tail = []
    else:
        tail = buf
enc.stdin.close()
enc.wait()
print('written', written, 'frames ->', OUT, round(os.path.getsize(OUT) / 1e6, 1), 'MB')
