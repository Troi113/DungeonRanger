"""Converts a simple binary glTF (.glb) model to OBJ + MTL with flat material colors, so Unity
imports it without a glTF package. Applies node transforms. A base-color texture (e.g. a palette
atlas) is baked to one flat color per triangle, sampled at the triangle's UV center.
Usage: python3 -I glb_to_obj.py input.glb output_basename
No third-party dependencies (PNG decoding uses zlib)."""
import json, struct, sys, zlib, math

def read_glb(path):
    data = open(path, 'rb').read()
    length = struct.unpack('<I', data[12:16])[0]
    gltf = json.loads(data[20:20 + length])
    off = 20 + length
    blen = struct.unpack('<I', data[off:off + 4])[0]
    return gltf, data[off + 8:off + 8 + blen]

def accessor(gltf, binary, index):
    acc = gltf['accessors'][index]
    view = gltf['bufferViews'][acc['bufferView']]
    comps = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4}[acc['type']]
    fmt = {5126: 'f', 5123: 'H', 5125: 'I', 5121: 'B'}[acc['componentType']]
    size = struct.calcsize(fmt)
    stride = view.get('byteStride', size * comps)
    start = view.get('byteOffset', 0) + acc.get('byteOffset', 0)
    out = []
    for i in range(acc['count']):
        o = start + i * stride
        vals = struct.unpack('<' + fmt * comps, binary[o:o + size * comps])
        out.append(vals if comps > 1 else vals[0])
    return out

def decode_png(raw):
    assert raw[:8] == b'\x89PNG\r\n\x1a\n'
    pos, idat, palette = 8, b'', None
    while pos < len(raw):
        n = struct.unpack('>I', raw[pos:pos + 4])[0]
        kind = raw[pos + 4:pos + 8]
        body = raw[pos + 8:pos + 8 + n]
        if kind == b'IHDR':
            w, h, depth, ctype = struct.unpack('>IIBB', body[:10])
        elif kind == b'PLTE':
            palette = [tuple(body[i:i + 3]) for i in range(0, len(body), 3)]
        elif kind == b'IDAT':
            idat += body
        pos += 12 + n
    assert depth == 8, 'only 8-bit PNG supported'
    bpp = {2: 3, 6: 4, 3: 1, 0: 1, 4: 2}[ctype]
    data = zlib.decompress(idat)
    rows, prev, i = [], bytearray(w * bpp), 0
    for _ in range(h):
        f = data[i]; line = bytearray(data[i + 1:i + 1 + w * bpp]); i += 1 + w * bpp
        for x in range(len(line)):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]; c = prev[x - bpp] if x >= bpp else 0
            if f == 1: line[x] = (line[x] + a) & 255
            elif f == 2: line[x] = (line[x] + b) & 255
            elif f == 3: line[x] = (line[x] + (a + b) // 2) & 255
            elif f == 4:
                p = a + b - c; pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append(line); prev = line
    def pixel(x, y):
        r = rows[y]
        if ctype == 3: return palette[r[x]]
        if ctype in (0, 4): v = r[x * bpp]; return (v, v, v)
        return tuple(r[x * bpp:x * bpp + 3])
    return w, h, pixel

def quat_rotate(q, v):
    x, y, z, w = q
    ix = w * v[0] + y * v[2] - z * v[1]
    iy = w * v[1] + z * v[0] - x * v[2]
    iz = w * v[2] + x * v[1] - y * v[0]
    iw = -x * v[0] - y * v[1] - z * v[2]
    return (ix * w + iw * -x + iy * -z - iz * -y,
            iy * w + iw * -y + iz * -x - ix * -z,
            iz * w + iw * -z + ix * -y - iy * -x)

def transform(node, p):
    s = node.get('scale', [1, 1, 1]); t = node.get('translation', [0, 0, 0])
    p = (p[0] * s[0], p[1] * s[1], p[2] * s[2])
    p = quat_rotate(node.get('rotation', [0, 0, 0, 1]), p)
    return (p[0] + t[0], p[1] + t[1], p[2] + t[2])

def main(src, dst):
    gltf, binary = read_glb(src)
    images = {}
    def material_color(mi, uv_center):
        mat = gltf['materials'][mi] if mi is not None else {}
        pbr = mat.get('pbrMetallicRoughness', {})
        factor = pbr.get('baseColorFactor', [1, 1, 1, 1])
        if 'baseColorTexture' in pbr and uv_center is not None:
            src_img = gltf['textures'][pbr['baseColorTexture']['index']]['source']
            if src_img not in images:
                img = gltf['images'][src_img]; view = gltf['bufferViews'][img['bufferView']]
                o = view.get('byteOffset', 0)
                images[src_img] = decode_png(binary[o:o + view['byteLength']])
            w, h, pixel = images[src_img]
            u, v = uv_center
            px = pixel(min(w - 1, max(0, int((u % 1) * w))), min(h - 1, max(0, int((v % 1) * h))))
            # The texture is sRGB; OBJ Kd is treated as display color too, so keep it as is.
            return tuple(round(c / 255 * f, 3) for c, f in zip(px, factor[:3]))
        # baseColorFactor is linear; convert to display (sRGB) values for Kd.
        def srgb(c): return 12.92 * c if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055
        return tuple(round(srgb(c), 3) for c in factor[:3])

    verts, normals, faces = [], [], {}
    def walk(index, chain):
        node = gltf['nodes'][index]; chain = chain + [node]
        if 'mesh' in node:
            for prim in gltf['meshes'][node['mesh']]['primitives']:
                pos = accessor(gltf, binary, prim['attributes']['POSITION'])
                nor = accessor(gltf, binary, prim['attributes']['NORMAL']) if 'NORMAL' in prim['attributes'] else None
                uvs = accessor(gltf, binary, prim['attributes']['TEXCOORD_0']) if 'TEXCOORD_0' in prim['attributes'] else None
                idx = accessor(gltf, binary, prim['indices'])
                base = len(verts)
                for p in pos:
                    for n in reversed(chain): p = transform(n, p)
                    verts.append(p)
                if nor:
                    for n0 in nor:
                        n = n0
                        for nd in reversed(chain):
                            n = quat_rotate(nd.get('rotation', [0, 0, 0, 1]), n)
                        normals.append(n)
                for t in range(0, len(idx), 3):
                    tri = idx[t:t + 3]
                    center = None
                    if uvs:
                        center = (sum(uvs[i][0] for i in tri) / 3, sum(uvs[i][1] for i in tri) / 3)
                    color = material_color(prim.get('material'), center)
                    faces.setdefault(color, []).append([base + i for i in tri])
        for child in node.get('children', []): walk(child, chain)
    for root in gltf['scenes'][gltf.get('scene', 0)]['nodes']: walk(root, [])

    name = dst.split('/')[-1]
    with open(dst + '.mtl', 'w') as m:
        for k, color in enumerate(faces):
            m.write(f'newmtl color{k}\nKd {color[0]} {color[1]} {color[2]}\n\n')
    with open(dst + '.obj', 'w') as o:
        o.write(f'# Converted from {src.split("/")[-1]} by Tools/glb_to_obj.py\nmtllib {name}.mtl\n')
        for v in verts: o.write(f'v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n')
        for n in normals: o.write(f'vn {n[0]:.5f} {n[1]:.5f} {n[2]:.5f}\n')
        for k, (color, tris) in enumerate(faces.items()):
            o.write(f'usemtl color{k}\n')
            for tri in tris:
                if normals: o.write('f ' + ' '.join(f'{i + 1}//{i + 1}' for i in tri) + '\n')
                else: o.write('f ' + ' '.join(str(i + 1) for i in tri) + '\n')
    print(dst, len(verts), 'verts', len(faces), 'colors:', list(faces.keys()))

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
