"""
hit01.anim을 좌우 대칭(YZ plane mirror)하여 hit02.anim으로 저장
- m_RotationCurves: 쿼터니언 x, w 부호 반전 {x, y, z, w} → {-x, y, z, -w}
- m_PositionCurves: x 부호 반전
- path: _L <-> _R 교체
"""
import re, sys

def negate_str(s):
    """문자열 그대로 부호만 반전 (부동소수점 오차 없음)"""
    s = s.strip()
    if s in ('0', '0.0', '-0', '-0.0'):
        return '0'
    return s[1:] if s.startswith('-') else '-' + s

def mirror_quat(m):
    """쿼터니언 YZ-평면 미러: x, w 부호 반전"""
    return '{x: ' + negate_str(m.group(1)) + ', y: ' + m.group(2) + \
           ', z: ' + m.group(3) + ', w: ' + negate_str(m.group(4)) + '}'

def mirror_vec3(m):
    """위치벡터 YZ-평면 미러: x 부호 반전"""
    return '{x: ' + negate_str(m.group(1)) + ', y: ' + m.group(2) + \
           ', z: ' + m.group(3) + '}'

QUAT_RE = re.compile(r'\{x: ([^,}]+), y: ([^,}]+), z: ([^,}]+), w: ([^}]+)\}')
VEC3_RE = re.compile(r'\{x: ([^,}]+), y: ([^,}]+), z: ([^,}]+)\}')

def swap_lr(line):
    """_L <-> _R 교체 (임시 플레이스홀더 방식)"""
    line = re.sub(r'_L\b', '_TMPL_', line)
    line = re.sub(r'_R\b', '_TMPR_', line)
    line = line.replace('_TMPL_', '_R')
    line = line.replace('_TMPR_', '_L')
    return line

src = sys.argv[1]
dst = sys.argv[2]

with open(src, 'r', encoding='utf-8') as f:
    lines = f.readlines()

section = None
out = []

for line in lines:
    stripped = line.strip()

    # 섹션 감지
    if stripped.startswith('m_RotationCurves'):
        section = 'rot'
    elif stripped.startswith('m_CompressedRotationCurves') or stripped.startswith('m_EulerCurves'):
        section = None
    elif stripped.startswith('m_PositionCurves'):
        section = 'pos'
    elif stripped.startswith('m_ScaleCurves') or stripped.startswith('m_FloatCurves') or stripped.startswith('m_PPtrCurves'):
        section = None

    # named path 에서만 L/R 교체 (숫자 path 는 건드리지 않음)
    if 'path: ' in line and not re.search(r'path: \d', line):
        line = swap_lr(line)

    # 커브 데이터 미러
    if section == 'rot' and QUAT_RE.search(line):
        line = QUAT_RE.sub(mirror_quat, line)
    elif section == 'pos' and 'w:' not in line and VEC3_RE.search(line):
        line = VEC3_RE.sub(mirror_vec3, line)

    out.append(line)

result = ''.join(out)
# 이름만 hit02 로 변경
result = result.replace('  m_Name: hit01\n', '  m_Name: hit02\n', 1)

with open(dst, 'w', encoding='utf-8') as f:
    f.write(result)

print(f'완료: {src} → {dst}')
