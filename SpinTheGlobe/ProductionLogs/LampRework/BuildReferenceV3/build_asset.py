"""Production entry point for the reference-driven banker-lamp detail rework.
The original generator is retained in ProductionLogs/LampRework/Checkpoint.
This entry point and the asset-local detailed helper are embedded in the blend.
"""
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from lamp_detail import build
if __name__ == "__main__":
    build()
