"""Production entry point for the reference-driven globe rework.
Uses the existing detailed globe generator and its original geographic dataset.
Both helpers and this entry point are embedded exactly in the editable .blend.
"""
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from reference_globe import run
if __name__ == "__main__":
    run()
