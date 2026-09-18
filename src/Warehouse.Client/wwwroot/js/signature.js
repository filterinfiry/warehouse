// Pad de firma sobre <canvas>. Soporta ratón y táctil (tablet/móvil de almacén).
const pads = {};

export function init(canvas) {
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    ctx.lineWidth = 2.5;
    ctx.lineCap = 'round';
    ctx.strokeStyle = '#0f172a';

    let drawing = false, last = null, dirty = false;

    const pos = (e) => {
        const r = canvas.getBoundingClientRect();
        const p = e.touches ? e.touches[0] : e;
        return { x: (p.clientX - r.left) * (canvas.width / r.width),
                 y: (p.clientY - r.top) * (canvas.height / r.height) };
    };
    const start = (e) => { drawing = true; last = pos(e); e.preventDefault(); };
    const move = (e) => {
        if (!drawing) return;
        const p = pos(e);
        ctx.beginPath(); ctx.moveTo(last.x, last.y); ctx.lineTo(p.x, p.y); ctx.stroke();
        last = p; dirty = true; e.preventDefault();
    };
    const end = () => { drawing = false; };

    canvas.addEventListener('mousedown', start);
    canvas.addEventListener('mousemove', move);
    window.addEventListener('mouseup', end);
    canvas.addEventListener('touchstart', start, { passive: false });
    canvas.addEventListener('touchmove', move, { passive: false });
    canvas.addEventListener('touchend', end);

    pads[canvas.id] = {
        clear: () => { ctx.clearRect(0, 0, canvas.width, canvas.height); dirty = false; },
        isEmpty: () => !dirty,
        dataUrl: () => dirty ? canvas.toDataURL('image/png') : null
    };
}

export function clear(id) { pads[id]?.clear(); }
export function isEmpty(id) { return pads[id] ? pads[id].isEmpty() : true; }
export function dataUrl(id) { return pads[id] ? pads[id].dataUrl() : null; }
