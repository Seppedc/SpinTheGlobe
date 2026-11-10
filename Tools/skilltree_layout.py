"""Check the exported skill layout, including free headings and shared connectors."""
import json
import math
from pathlib import Path

SNAPSHOT = Path(__file__).with_name('economy_snapshot.json')
CELL = 160


def segment_distance(point, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = dx * dx + dy * dy
    if length == 0:
        return math.dist(point, a)
    t = max(0, min(1, ((point[0] - a[0]) * dx + (point[1] - a[1]) * dy) / length))
    return math.dist(point, (a[0] + t * dx, a[1] + t * dy))


def check(data):
    skills = {n['id']: n for n in data['skills']}
    sections = data.get('sections', [])
    positions = {n['id']: (n['position']['x'] / CELL, n['position']['y'] / CELL)
                 for n in [*skills.values(), *sections]}
    section_for = {n['id']: s for n in skills.values() if 'root' in (n['prerequisites'] or [])
                   for s in sections if n['branch'] in s['branches']}
    segments = []

    def edge(owner, a, b, *ignored):
        if math.dist(a, b) > 1e-9:
            segments.append((owner, a, b, set(ignored)))

    for section in sections:
        sid = section['id']
        x, y = positions[sid]
        bus_y = y + section['connectorOffset'] / CELL * (1 if section['upward'] else -1)
        entries = [nid for nid, s in section_for.items() if s['id'] == sid]
        edge(sid, positions['root'], (x, y), 'root', sid)
        if not section.get("sharedConnector", True):
            for nid in entries:
                edge(sid, (x, y), positions[nid], sid, nid)
        elif entries:
            xs = [x] + [positions[nid][0] for nid in entries]
            edge(sid, (x, y), (x, bus_y), sid)
            edge(sid, (min(xs), bus_y), (max(xs), bus_y), sid)
            for nid in entries:
                edge(sid, (positions[nid][0], bus_y), positions[nid], sid, nid)

    for nid, node in skills.items():
        for parent in node['prerequisites'] or []:
            if 'root' in node['prerequisites'] and parent != 'root':
                continue
            if parent == 'root' and nid in section_for:
                continue
            if parent != 'root' and skills[parent]['branch'] != node['branch']:
                continue
            edge(nid, positions[parent], positions[nid], parent, nid)

    errors = []
    ids = list(positions)
    for i, nid in enumerate(ids):
        for other in ids[i + 1:]:
            distance = math.dist(positions[nid], positions[other])
            if distance < .99:
                errors.append(('close', nid, other, round(distance, 2)))
    for owner, a, b, ignored in segments:
        for nid, point in positions.items():
            if nid in ignored:
                continue
            distance = segment_distance(point, a, b)
            if distance < .4:
                errors.append(('edge', owner, 'near', nid, round(distance, 2)))
    return positions, errors


if __name__ == '__main__':
    data = json.loads(SNAPSHOT.read_text(encoding='utf-8-sig'))
    positions, errors = check(data)
    print(f"{len(data['skills'])} skills, {len(data.get('sections', []))} free section headings")
    print(chr(10).join(map(str, errors)) if errors else 'OK')
    branch_codes = {0: 'R', 1: 'G', 2: 'P', 3: 'C', 4: 'W', 5: 'S', 6: 'L'}
    layout = {n['id']: [*positions[n['id']], n['prerequisites'] or [], branch_codes[n['branch']]] for n in data['skills']}
    Path('layout.json').write_text(json.dumps(layout), encoding='utf-8')
    raise SystemExit(1 if errors else 0)
