/**
 * hit01.anim → hit02.anim 좌우 미러링
 * - m_RotationCurves: 쿼터니언 x, w 부호 반전
 * - m_PositionCurves: x 부호 반전
 * - path: _L <-> _R 교체
 */
const fs = require('fs');
const [,, src, dst] = process.argv;

function negateStr(s) {
    s = s.trim();
    if (s === '0' || s === '0.0') return s;
    return s.startsWith('-') ? s.slice(1) : '-' + s;
}

const QUAT_RE = /\{x: ([^,}]+), y: ([^,}]+), z: ([^,}]+), w: ([^}]+)\}/g;
const VEC3_RE = /\{x: ([^,}]+), y: ([^,}]+), z: ([^,}]+)\}/g;

function mirrorQuat(_, x, y, z, w) {
    return `{x: ${negateStr(x)}, y: ${y}, z: ${z}, w: ${negateStr(w)}}`;
}
function mirrorVec3(_, x, y, z) {
    return `{x: ${negateStr(x)}, y: ${y}, z: ${z}}`;
}

function swapLR(line) {
    line = line.replace(/_L\b/g, '_TMPL_');
    line = line.replace(/_R\b/g, '_TMPR_');
    line = line.replace(/_TMPL_/g, '_R');
    line = line.replace(/_TMPR_/g, '_L');
    return line;
}

const lines = fs.readFileSync(src, 'utf8').split('\n');
let section = null;
const out = [];

for (let line of lines) {
    const stripped = line.trim();

    // 섹션 감지
    if (stripped.startsWith('m_RotationCurves'))               section = 'rot';
    else if (stripped.startsWith('m_CompressedRotationCurves') ||
             stripped.startsWith('m_EulerCurves'))             section = null;
    else if (stripped.startsWith('m_PositionCurves'))          section = 'pos';
    else if (stripped.startsWith('m_ScaleCurves')  ||
             stripped.startsWith('m_FloatCurves')  ||
             stripped.startsWith('m_PPtrCurves'))              section = null;

    // named path 에서만 L/R 교체 (숫자 path 는 건드리지 않음)
    if (line.includes('path: ') && !/path: \d/.test(line)) {
        line = swapLR(line);
    }

    // 커브 데이터 미러
    if (section === 'rot' && QUAT_RE.test(line)) {
        QUAT_RE.lastIndex = 0;
        line = line.replace(QUAT_RE, mirrorQuat);
    } else if (section === 'pos' && !line.includes('w:') && VEC3_RE.test(line)) {
        VEC3_RE.lastIndex = 0;
        line = line.replace(VEC3_RE, mirrorVec3);
    }
    QUAT_RE.lastIndex = 0;
    VEC3_RE.lastIndex = 0;

    out.push(line);
}

let result = out.join('\n');
result = result.replace('  m_Name: hit01\n', '  m_Name: hit02\n');

fs.writeFileSync(dst, result, 'utf8');
console.log(`완료: ${src} → ${dst}`);
