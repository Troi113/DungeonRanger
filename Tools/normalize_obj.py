"""Normalizes an OBJ model in place for the game: optional rotation so its long axis is +Y,
then scaled so its longest side is 1, centered on x/z, and resting on y = 0.
Usage: python3 -I normalize_obj.py model.obj [none|z_to_y|x_to_y]"""
import sys

ROTATIONS = {
    'none': lambda x, y, z: (x, y, z),
    'z_to_y': lambda x, y, z: (x, z, -y),   # rotate -90 deg about X
    'x_to_y': lambda x, y, z: (-y, x, z),   # rotate +90 deg about Z
}

def main(path, mode):
    rot = ROTATIONS[mode]
    lines = open(path).read().splitlines()
    verts = []
    for i, l in enumerate(lines):
        p = l.split()
        if p and p[0] == 'v':
            verts.append((i, rot(*map(float, p[1:4]))))
    mn = [min(v[k] for _, v in verts) for k in range(3)]
    mx = [max(v[k] for _, v in verts) for k in range(3)]
    size = max(mx[k] - mn[k] for k in range(3))
    cx, cz = (mn[0] + mx[0]) / 2, (mn[2] + mx[2]) / 2
    for i, v in verts:
        lines[i] = 'v %.6f %.6f %.6f' % ((v[0] - cx) / size, (v[1] - mn[1]) / size, (v[2] - cz) / size)
    for i, l in enumerate(lines):
        p = l.split()
        if p and p[0] == 'vn':
            lines[i] = 'vn %.5f %.5f %.5f' % rot(*map(float, p[1:4]))
    open(path, 'w').write('\n'.join(lines) + '\n')
    print(path, mode, 'done')

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else 'none')
