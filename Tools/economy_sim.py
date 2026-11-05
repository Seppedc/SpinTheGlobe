"""Run after editor automation calls EconomySnapshotMenu.Export()."""
import argparse
from economy_model import Economy

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--activity', type=float, default=.5)
    parser.add_argument('--no-loot', action='store_true')
    args = parser.parse_args()
    model = Economy(activity=args.activity, loot=not args.no_loot)
    result = model.run()
    print(f'Activity {args.activity:.0%}; loot spending {not args.no_loot}; retirement reached {result["reached"]}')
    for key in ('root', 'globe_auto', 'pin_01', 'pin_03', 'pin_06', 'pin_autoloader', 'retire_available', 'globe_max_speed_max', 'pin_12'):
        if key in result['milestones']:
            print(f'{key:24} {result["milestones"][key]/60:6.1f} min')
    print(f'Earned EUR {result["earned"]:,.0f}; gold {result["gold"]:.0f}')
