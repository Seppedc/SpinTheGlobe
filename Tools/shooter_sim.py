"""Run: python Tools/shooter_sim.py. Isolates firing, flight and meridian payouts.

Reads the current assets. Excludes payout upgrades, whiskey, Twin Barrel
and cases. Uses seeded random meridian arrival and simulates rendered frames.
"""
import heapq
import math
from pathlib import Path
import random
import re
from economy_model import Economy

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'SpinTheGlobe/Assets/DetectiveDesk/Data'
balance = (DATA / 'Balance/GameBalance.asset').read_text(encoding='utf-8')
tree = (DATA / 'SkillTree/SkillTree.asset').read_text(encoding='utf-8')
reload_node = tree.split('  - id: shooter_cooldown\n', 1)[1].split('  - id:', 1)[0]


def field(text, name):
    return float(re.search(r'^\s*' + name + r': ([\d.eE+-]+)', text, re.M)[1])


base_reload = float(re.search(r'- stat: 201\s+value: ([\d.]+)', balance)[1])
payout = field(balance, 'pinBasePayout')
reward = field(balance, 'revolutionReward')
ranks = int(field(reload_node, 'maxRank'))
factor = field(reload_node, 'valuePerRank')
source = (ROOT / 'SpinTheGlobe/Assets/DetectiveDesk/Scripts/Runtime/Progression/GameBalance.cs').read_text(encoding='utf-8')
flight = float(re.search(r'pinFlightSeconds = ([\d.]+)', source)[1])
model = Economy()
speed_ranks = [0] * len(model.nodes)
speed_index = model.index['globe_max_speed']
speed_ranks[speed_index] = model.nodes[speed_index]['maxRank']
max_rpm = model.sheet(tuple(speed_ranks), (), 0)[0][100] / 6


def simulate(pins, passes, rpm, reload, per_pass, fair, fps=60, seconds=120):
    rng = random.Random(7)
    events = []
    busy = set()
    used = set()
    next_pin = 0
    remaining = 0.0
    shots = paid = 0
    for frame in range(fps * seconds):
        now = (frame + 1) / fps
        while events and events[0][0] <= now:
            at, index, left = heapq.heappop(events)
            paid += 1
            if left > 1:
                heapq.heappush(events, (at + 60 / rpm, index, left - 1))
            else:
                busy.remove(index)
        remaining -= 1 / fps
        for _ in range(12 if fair else 1):
            if remaining > 0:
                break
            ready = next(((next_pin + i) % pins for i in range(pins)
                          if (next_pin + i) % pins not in busy), None)
            if ready is None:
                break
            busy.add(ready)
            used.add(ready)
            if fair:
                next_pin = (ready + 1) % pins
            shots += 1
            heapq.heappush(events, (now + flight + rng.random() * 60 / rpm, ready, passes))
            remaining = remaining + reload if fair else reload
        remaining = max(0, remaining)
    return shots / seconds, paid * per_pass / seconds, len(used)


if __name__ == '__main__':
    start_rpm = model.sheet(tuple(0 for _ in model.nodes), (), 0)[0][100] / 6
    print('Early reload at %.0f RPM, one-pass pins, ready-pin firing:' % start_rpm)
    print('pins rank reload  shots/min')
    for pins in (1, 2, 3):
        for rank in (0, 1, 5, 10):
            reload = base_reload * factor ** rank
            shots, _, _ = simulate(pins, 1, start_rpm, reload, payout, True, seconds=600)
            print(f'{pins:4} {rank:4} {reload:6.3f}s {shots * 60:9.1f}')
    print('%.0f RPM, all locations, no payout upgrades; globe alone = EUR %.2f/s' % (max_rpm, max_rpm / 60 * reward))
    print('curve        pins passes reload    shots/s   pin EUR/s used')
    for label, reload, pay, fair in [('old reload', .4, 3, False)] + [
            (f'new rank {rank}', base_reload * factor ** rank, payout, True)
            for rank in (0, 15, 30, ranks)]:
        for passes in (1, 6):
            for pins in (3, 12):
                rate, income, used = simulate(pins, passes, max_rpm, reload, pay, fair)
                print(f'{label:12} {pins:4} {passes:6} {reload:6.3f}s {rate:9.2f} {income:11.2f} {used:4}')
    for fps in (30, 60, 144):
        print(f'{fps} FPS, 12 one-pass pins, max reload:', simulate(12, 1, max_rpm, base_reload * factor ** ranks, payout, True, fps))
    old_total = sum(math.ceil(15 * 1.45 ** i) for i in range(15))
    new_total = sum(math.ceil(field(reload_node, 'baseCost') * field(reload_node, 'growth') ** i) for i in range(ranks))
    print(f'Full reload upgrade cost: old 15 ranks EUR {old_total}; new {ranks} ranks EUR {new_total}')
