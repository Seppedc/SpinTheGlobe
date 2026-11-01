"""DetectiveDesk PinCoffer starting geometry. Run using Blender background CLI.
Editable starter, NOT photorealistic final. Update this generator before rebuilding.
"""
from pathlib import Path
import sys
import bpy
HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'Tools/blender_shared.py').exists())
sys.path.insert(0,str(ROOT/'Tools'))
from blender_shared import save
if __name__=='__main__':
    raise SystemExit('Reference starter disabled: use ArtSource/PinCoffer/build_pincoffer.py and its production README.')
    print('Building PinCoffer')
    save('PinCoffer',HERE,__file__)
