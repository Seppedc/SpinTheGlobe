"""Production entry point for the reference-driven PinLauncher revision 2.
The original spring-tray generator is backed up under ProductionLogs/PinLauncherRework.
Detailed geometry and PBR texture authoring live alongside this entry point;
both scripts are embedded in the editable .blend and copied to every build.
"""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent))
from pinlauncher_detail import build
if __name__ == "__main__":
    build()
