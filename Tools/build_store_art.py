# Steam store and library art at Steamworks' exact sizes (Docs/Steam/STEAMWORKS_GUIDE.md, step 4).
#
#   python Tools/build_store_art.py [landscape key art] [portrait key art]
#
# Without arguments it builds DRAFTS from the main-menu art (1376 x 768) and the logo already in
# the game - good enough for a "Coming soon" page, not for launch: the library hero is upscaled
# almost 3x. Once Assets 114 (landscape key art, 3840 x 2160) and 115 (portrait, 1800 x 2700) are
# delivered, pass them and every capsule is rebuilt from them.
#
# Writes Docs/Steam/store_art/*.png. Steam rules applied: capsules show artwork, the game's name and
# nothing else; the library hero has no text or logo; the library logo is a transparent PNG.
import os, sys
import numpy as np
from PIL import Image, ImageFilter

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace('\\', '/') + '/'
OUT = PROJECT + 'Docs/Steam/store_art/'
MENU_ART = PROJECT + 'Assets/Art/UI/MainMenu/mainmenu_background.png'
LOGO = PROJECT + 'Assets/Art/UI/MainMenu/corebound_logo.png'

landscape_path = sys.argv[1] if len(sys.argv) > 1 else MENU_ART
portrait_path = sys.argv[2] if len(sys.argv) > 2 else None
os.makedirs(OUT, exist_ok=True)


def remove_menu_watermark(image):
    """The main-menu art carries a translucent four-point star (an image generator's mark) at about
    (1256, 648); it must not reach a store capsule. Painted out by inpainting what is brighter than
    its surroundings in that one spot."""
    try:
        import cv2
    except ImportError:
        print('cv2 missing - the watermark stays; pip install opencv-python')
        return image
    rgb = np.asarray(image.convert('RGB'))
    x0, y0, x1, y1 = 1215, 605, 1300, 695
    region = rgb[y0:y1, x0:x1].astype(np.float32).mean(axis=2)
    local = cv2.medianBlur(region.astype(np.uint8), 31).astype(np.float32)
    mask = np.zeros(rgb.shape[:2], np.uint8)
    mask[y0:y1, x0:x1] = ((region - local) > 10).astype(np.uint8) * 255
    mask = cv2.dilate(mask, np.ones((5, 5), np.uint8))
    cleaned = cv2.inpaint(rgb, mask, 6, cv2.INPAINT_TELEA)
    print('watermark pixels painted out:', int((mask > 0).sum()))
    return Image.fromarray(cleaned)


def cover(image, width, height, focus=(0.5, 0.5)):
    """Scale to cover width x height, then crop around the focus point (fractions of the image)."""
    scale = max(width / image.width, height / image.height)
    resized = image.resize((max(width, round(image.width * scale)), max(height, round(image.height * scale))), Image.LANCZOS)
    fx, fy = focus[0] * resized.width, focus[1] * resized.height
    left = int(min(max(0, fx - width / 2), resized.width - width))
    top = int(min(max(0, fy - height / 2), resized.height - height))
    return resized.crop((left, top, left + width, top + height))


logo = Image.open(LOGO).convert('RGBA')
logo = logo.crop(logo.getbbox())


def with_logo(art, logo_width_fraction, anchor=(0.5, 0.5), shadow=True):
    """The logo over the art, with a soft dark halo so it reads on any part of the picture."""
    art = art.convert('RGBA')
    width = int(art.width * logo_width_fraction)
    mark = logo.resize((width, int(logo.height * width / logo.width)), Image.LANCZOS)
    x = int(anchor[0] * art.width - mark.width / 2)
    y = int(anchor[1] * art.height - mark.height / 2)
    if shadow:
        halo = Image.new('RGBA', art.size, (0, 0, 0, 0))
        alpha = mark.split()[3].point(lambda v: int(v * 0.75))
        black = Image.new('RGBA', mark.size, (8, 10, 16, 255))
        black.putalpha(alpha)
        halo.alpha_composite(black, (x, y))
        halo = halo.filter(ImageFilter.GaussianBlur(max(4, width // 40)))
        art = Image.alpha_composite(art, halo)
    art.alpha_composite(mark, (x, y))
    return art.convert('RGB')


landscape = Image.open(landscape_path).convert('RGB')
if landscape_path == MENU_ART:
    landscape = remove_menu_watermark(landscape)

# The Core stands a little right of centre in the menu art; capsules crop around it. On the two
# portrait capsules the logo sits at the bottom, over the rocks: a crop of the wide art has no sky
# above the Core to put it in.
core = (0.62, 0.42) if landscape_path == MENU_ART else (0.5, 0.45)
portrait = Image.open(portrait_path).convert('RGB') if portrait_path else None

outputs = {
    # Store
    'header_capsule_920x430.png': with_logo(cover(landscape, 920, 430, (0.5, core[1])), 0.46, (0.27, 0.5)),
    'small_capsule_462x174.png': with_logo(cover(landscape, 462, 174, core), 0.62, (0.5, 0.5)),
    'main_capsule_1232x706.png': with_logo(cover(landscape, 1232, 706, (0.5, core[1])), 0.38, (0.24, 0.24)),
    'vertical_capsule_748x896.png': with_logo(cover(portrait or landscape, 748, 896, core), 0.8, (0.5, 0.84)),
    'page_background_1438x810.png': Image.blend(Image.new('RGB', (1438, 810), (14, 18, 26)), cover(landscape, 1438, 810), 0.35),
    # Library
    'library_capsule_600x900.png': with_logo(cover(portrait or landscape, 600, 900, core), 0.82, (0.5, 0.85)),
    'library_hero_3840x1240.png': cover(landscape, 3840, 1240, (0.5, core[1])),
}

for name, image in outputs.items():
    image.save(OUT + name, optimize=True)
    print(name, image.size, round(os.path.getsize(OUT + name) / 1e3), 'KB')

# Library logo: the logo alone on transparency, at most 1280 x 720 (Steam centres it over the hero).
canvas = Image.new('RGBA', (1280, 720), (0, 0, 0, 0))
mark = logo.copy()
mark.thumbnail((1180, 640), Image.LANCZOS)
canvas.alpha_composite(mark, ((1280 - mark.width) // 2, (720 - mark.height) // 2))
canvas.save(OUT + 'library_logo_1280x720.png', optimize=True)
print('library_logo_1280x720.png', canvas.size)

# Icons, until Asset 116 exists: the Core crystal from the logo's centre.
crystal = logo.crop((int(logo.width * 0.37), int(logo.height * 0.08), int(logo.width * 0.63), int(logo.height * 0.56)))

# App icon (Steamworks > Client images > "Anwendungssymbol", also used as the community icon):
# 184 x 184 JPG, no transparency, so on the game's dark slate.
icon = Image.new('RGBA', (184, 184), (20, 26, 38, 255))
mark = crystal.copy()
mark.thumbnail((168, 168), Image.LANCZOS)
icon.alpha_composite(mark, ((184 - mark.width) // 2, (184 - mark.height) // 2))
icon.convert('RGB').save(OUT + 'app_icon_184x184.jpg', quality=95)
print('app_icon_184x184.jpg (184, 184)')

# Shortcut icon ("Verknüpfungssymbol"): a 256 x 256 PNG with transparency, and an ICO with the
# smaller sizes Windows uses for desktop shortcuts.
shortcut = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
mark = crystal.copy()
mark.thumbnail((240, 240), Image.LANCZOS)
shortcut.alpha_composite(mark, ((256 - mark.width) // 2, (256 - mark.height) // 2))
shortcut.save(OUT + 'shortcut_icon_256x256.png', optimize=True)
shortcut.save(OUT + 'shortcut_icon.ico', sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
print('shortcut_icon_256x256.png, shortcut_icon.ico')
