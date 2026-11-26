/* =========================================================
   COMMON – tiện ích dùng chung, header/footer, giỏ thuê, chuẩn hoá dữ liệu
   ========================================================= */
const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];
const BASE = document.body.dataset.base || '';

/* ---------- Nhãn & màu trạng thái ---------- */
const CATEGORY = {
  OTO: { label: 'Ô tô', icon: 'car' },
  XE_SO: { label: 'Xe số', icon: 'bike' },
  XE_GA: { label: 'Xe ga', icon: 'bike' },
  XE_DIEN: { label: 'Xe điện', icon: 'bolt' }
};
const VEHICLE_STATUS = { AVAILABLE: ['Sẵn sàng', 'green'], RENTED: ['Đang được thuê', 'orange'], MAINTENANCE: ['Bảo trì', 'gray'] };
const RENTAL_STATUS = { PENDING: ['Chờ thanh toán', 'yellow'], CONFIRMED: ['Chờ thanh toán', 'blue'], PAID: ['Đã thanh toán', 'teal'], RENTING: ['Đang thuê', 'purple'], COMPLETED: ['Hoàn tất', 'green'], CANCELLED: ['Đã huỷ', 'red'] };
const DETAIL_STATUS = { RESERVED: ['Đã giữ xe', 'blue'], RENTING: ['Đang thuê', 'purple'], RETURNED: ['Đã trả', 'green'], CANCELLED: ['Đã huỷ', 'red'] };
const PAYMENT_STATUS = { PENDING: ['Chờ thanh toán', 'yellow'], PAID: ['Đã thanh toán', 'green'], FAILED: ['Thất bại', 'red'], REFUNDED: ['Đã hoàn tiền', 'gray'] };
const PAYMENT_METHOD = { CASH: 'Tiền mặt', BANK_TRANSFER: 'Chuyển khoản', MOMO: 'MoMo', VNPAY: 'VNPay', CARD: 'Thẻ', OTHER: 'Khác' };
const ROLE_LABEL = { CUSTOMER: 'Khách hàng', STAFF: 'Nhân viên', MANAGER: 'Quản lý', ADMIN: 'Quản trị viên' };

const badge = (map, key) => { const [t, c] = map[key] || [key || '—', '']; return `<span class="badge ${c}">${esc(t)}</span>`; };

/* ---------- Icons ---------- */
const Icons = {
  car: '<path d="M3 14l1.8-4.6A2 2 0 0 1 6.7 8h10.6a2 2 0 0 1 1.9 1.4L21 14v4h-2.5M3 14v4h2.5M3 14h18M8.5 18h7"/><circle cx="7" cy="17" r="1.8"/><circle cx="17" cy="17" r="1.8"/>',
  bike: '<circle cx="5.5" cy="16.5" r="3"/><circle cx="18.5" cy="16.5" r="3"/><path d="M5.5 16.5L9 9h4l3 7.5M9 9L8 7H6M13 9l2-3h2"/>',
  bolt: '<path d="M13 3L5 14h6l-1 7 8-11h-6z"/>',
  search: '<circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/>',
  chevron: '<path d="M6 9l6 6 6-6"/>',
  cart: '<path d="M3 4h2l2.4 11h10.2l2-8H6.2"/><circle cx="9" cy="19" r="1.4"/><circle cx="17" cy="19" r="1.4"/>',
  user: '<circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 3.6-6 8-6s8 2 8 6"/>',
  seat: '<path d="M7 4v8a2 2 0 0 0 2 2h7l2 6M7 14l-2 6M9 7h3"/>',
  engine: '<path d="M4 9h3l2-2h6l2 2h3v6h-3l-2 2H9l-2-2H4z"/>',
  palette: '<circle cx="12" cy="12" r="9"/><circle cx="8" cy="10" r="1"/><circle cx="12" cy="7.5" r="1"/><circle cx="16" cy="10" r="1"/>',
  tag: '<path d="M3 12V4h8l10 10-8 8z"/><circle cx="7.5" cy="8.5" r="1.2"/>',
  calendar: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
  check: '<path d="M5 12l5 5 9-10"/>',
  menu: '<path d="M4 7h16M4 12h16M4 17h16"/>',
  card: '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18"/>',
  shield: '<path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z"/><path d="M9 12l2 2 4-4"/>'
};
const icon = (n, cls = '') => `<svg class="icon ${cls}" viewBox="0 0 24 24" aria-hidden="true">${Icons[n] || ''}</svg>`;

/* ---------- Helpers ---------- */
function esc(s) { return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }
const fmtMoney = n => (n === null || n === undefined || isNaN(n)) ? '—' : new Intl.NumberFormat('vi-VN').format(Math.round(n)) + ' ₫';
const fmtDateTime = s => s ? new Date(/(?:Z|[+-]\d\d:\d\d)$/.test(s) ? s : s + 'Z').toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—';
const fmtDate = s => s ? new Date(s).toLocaleDateString('vi-VN') : '—';
const pick = (o, ...keys) => { for (const k of keys) if (o && o[k] !== undefined && o[k] !== null) return o[k]; return undefined; };
const toList = r => Array.isArray(r) ? r : (r && (r.data || r.items || r.results || r.$values)) || [];
const getParam = k => new URLSearchParams(location.search).get(k);

/** Số ngày ước tính CHỈ để hiển thị – số tiền chính thức do backend tính. */
function estimateDays(start, end) {
  const ms = new Date(end) - new Date(start);
  if (!(ms > 0)) return 0;
  return Math.max(1, Math.ceil(ms / 86400000));
}
function toLocalInput(d) { const p = n => String(n).padStart(2, '0'); return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`; }
function defaultRange() {
  const s = new Date(); s.setDate(s.getDate() + 1); s.setHours(8, 0, 0, 0);
  const e = new Date(s); e.setDate(e.getDate() + 2); e.setHours(18, 0, 0, 0);
  return { start: toLocalInput(s), end: toLocalInput(e) };
}

/* ---------- Ảnh placeholder (khi xe chưa có ảnh) ---------- */
function placeholderImg(category, label = '') {
  const glyph = {
    OTO: '<path d="M60 150l14-38a14 14 0 0 1 13-9h86a14 14 0 0 1 13 9l14 38v26h-14v-12H74v12H60z" fill="#fff" fill-opacity=".92"/><circle cx="92" cy="156" r="14" fill="#2b1b49"/><circle cx="188" cy="156" r="14" fill="#2b1b49"/><path d="M84 112h112l8 24H76z" fill="#9fd8d8"/>',
    XE_SO: '<circle cx="90" cy="160" r="26" fill="none" stroke="#fff" stroke-width="9"/><circle cx="190" cy="160" r="26" fill="none" stroke="#fff" stroke-width="9"/><path d="M90 160l30-52h40l30 52M120 108l-8-14H96M160 108l14-22h18" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" stroke-linejoin="round"/>',
    XE_GA: '<circle cx="90" cy="164" r="22" fill="none" stroke="#fff" stroke-width="9"/><circle cx="196" cy="164" r="22" fill="none" stroke="#fff" stroke-width="9"/><path d="M90 164h30c10 0 12-30 30-30h22l24 30M170 134l8-40h16" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" stroke-linejoin="round"/>',
    XE_DIEN: '<circle cx="90" cy="164" r="22" fill="none" stroke="#fff" stroke-width="9"/><circle cx="196" cy="164" r="22" fill="none" stroke="#fff" stroke-width="9"/><path d="M90 164h30c10 0 12-30 30-30h22l24 30M170 134l8-40h16" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round"/><path d="M142 70l-16 26h14l-6 24 24-32h-14l8-18z" fill="#f6c453"/>'
  }[category] || '';
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 280 200"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#5b3d82"/><stop offset="1" stop-color="#3f9b9b"/></linearGradient></defs><rect width="280" height="200" fill="url(#g)"/>${glyph}<text x="140" y="192" font-family="Arial" font-size="12" font-weight="700" fill="#fff" fill-opacity=".8" text-anchor="middle">${esc(label)}</text></svg>`;
  return 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
}

/* ---------- Chuẩn hoá dữ liệu từ backend ----------
   Backend có thể trả snake_case (theo tài liệu) hoặc camelCase (mặc định .NET).
   Nếu cấu trúc JSON thật khác, CHỈ cần sửa các hàm norm* dưới đây. */
function normVehicle(v) {
  if (!v) return null;
  const nested = pick(v, 'vehicle_model', 'vehicleModel');
  const mo = (nested && typeof nested === 'object') ? nested : v;
  const category = enumName(Enum.category, mo.category);
  const brand = pick(mo, 'brand') || '';
  const modelName = typeof mo.model === 'string' ? mo.model : (pick(mo, 'model_name', 'modelName') || '');
  const imgsRaw = pick(v, 'images', 'vehicle_images', 'vehicleImages') || [];
  let images = toList(imgsRaw).map(i => typeof i === 'string' ? { url: i, primary: false } : { url: pick(i, 'image_url', 'imageUrl', 'url'), primary: !!pick(i, 'is_primary', 'isPrimary') }).filter(i => i.url);
  const single = pick(v, 'image_url', 'imageUrl', 'primary_image', 'primaryImage');
  if (!images.length && single) images = [{ url: single, primary: true }];
  images.sort((a, b) => (b.primary ? 1 : 0) - (a.primary ? 1 : 0));
  const name = `${brand} ${modelName}`.trim();
  const ph = placeholderImg(category, name);
  return {
    id: v.id,
    modelId: pick(v, 'vehicle_model_id', 'vehicleModelId') ?? mo.id,
    category, brand, model: modelName, name,
    year: pick(mo, 'year'), seats: pick(mo, 'seats'),
    engine: pick(mo, 'engine_capacity', 'engineCapacity'),
    description: pick(mo, 'description') || '',
    price: pick(mo, 'price_per_day', 'pricePerDay') ?? 0,
    plate: pick(v, 'license_plate', 'licensePlate') || '',
    color: pick(v, 'color') || '', condition: pick(v, 'condition') || '',
    status: enumName(Enum.vehicle, v.status),
    images: images.length ? images.map(i => i.url) : [ph],
    image: images.length ? images[0].url : ph
  };
}
function normModel(m) {
  return { id: m.id, category: enumName(Enum.category, m.category), brand: m.brand, model: m.model, year: m.year, seats: m.seats, engine: pick(m, 'engine_capacity', 'engineCapacity'), description: m.description || '', price: pick(m, 'price_per_day', 'pricePerDay') };
}
function normRental(r) {
  if (!r) return null;
  const detailsRaw = toList(pick(r, 'details', 'rental_order_details', 'rentalOrderDetails', 'items', 'vehicles') || []);
  const items = detailsRaw.map(d => {
    const veh = normVehicle(pick(d, 'vehicle')) || {};
    return {
      id: d.id, vehicleId: pick(d, 'vehicle_id', 'vehicleId') ?? veh.id,
      name: veh.name || pick(d, 'vehicle_name', 'vehicleName') || ('Xe #' + (pick(d, 'vehicle_id', 'vehicleId') ?? '')),
      plate: veh.plate || pick(d, 'license_plate', 'licensePlate') || '', image: veh.image,
      price: pick(d, 'price_per_day', 'pricePerDay'), days: pick(d, 'rental_days', 'rentalDays'),
      subtotal: d.subtotal, status: enumName(Enum.detail, d.status)
    };
  });
  const cust = pick(r, 'customer');
  return {
    id: r.id, code: pick(r, 'order_code', 'orderCode') || ('#' + r.id),
    start: pick(r, 'start_datetime', 'startDatetime'), end: pick(r, 'end_datetime', 'endDatetime'),
    total: pick(r, 'total_amount', 'totalAmount'), status: enumName(Enum.rental, r.status), note: r.note || '',
    rejectionReason: r.rejectionReason,
    canUseManualPayment: r.canUseManualPayment === true,
    createdAt: pick(r, 'created_at', 'createdAt'),
    customer: cust ? { name: pick(cust, 'full_name', 'fullName'), email: cust.email, phone: cust.phone } : { name: pick(r, 'customer_name', 'customerName') || ('#' + r.customerId) },
    items, payments: toList(pick(r, 'payments') || []).map(normPayment)
  };
}
function normPayment(p) {
  return { id: p.id, orderId: pick(p, 'rental_order_id', 'rentalOrderId'), orderCode: pick(p, 'order_code', 'orderCode'), amount: p.amount, method: pick(p, 'payment_method', 'paymentMethod'), code: pick(p, 'transaction_code', 'transactionCode'), status: p.status, paidAt: pick(p, 'paid_at', 'paidAt'), createdAt: pick(p, 'created_at', 'createdAt') };
}
function normUser(u) { return { id: u.id, name: pick(u, 'full_name', 'fullName'), email: u.email, phone: u.phone, role: u.role, status: u.status, createdAt: pick(u, 'created_at', 'createdAt') }; }

/* ---------- Giỏ thuê (nhiều xe trong 1 đơn) ---------- */
const Cart = {
  get() { try { return JSON.parse(localStorage.getItem('rentgo.cart') || '[]'); } catch { return []; } },
  set(a) { localStorage.setItem('rentgo.cart', JSON.stringify(a)); Cart.refresh(); },
  has: id => Cart.get().some(x => x.id === id),
  add(v) {
    const c = Cart.get(); if (c.some(x => x.id === v.id)) return false;
    c.push({ id: v.id, name: v.name, image: v.image, price: v.price, category: v.category, plate: v.plate, seats: v.seats });
    Cart.set(c); return true;
  },
  remove(id) { Cart.set(Cart.get().filter(x => x.id !== id)); },
  clear() { Cart.set([]); },
  count: () => Cart.get().length,
  refresh() { $$('[data-cart-count]').forEach(e => { const n = Cart.count(); e.textContent = n; e.classList.toggle('hidden', !n); }); }
};

/* ---------- Toast & Modal ---------- */
function toast(msg, type = '') {
  let box = $('#toasts'); if (!box) { box = document.createElement('div'); box.id = 'toasts'; document.body.appendChild(box); }
  const t = document.createElement('div'); t.className = 'toast ' + type; t.textContent = msg; box.appendChild(t);
  setTimeout(() => t.remove(), 3800);
}
function openModal(html, opts = {}) {
  closeModal();
  const bg = document.createElement('div'); bg.className = 'modal-bg'; bg.id = 'modal-bg';
  bg.innerHTML = `<div class="modal" style="${opts.width ? 'max-width:' + opts.width : ''}"><button class="x" aria-label="Đóng">×</button>${html}</div>`;
  bg.addEventListener('mousedown', e => { if (e.target === bg) closeModal(); });
  bg.querySelector('.x').onclick = closeModal;
  document.body.appendChild(bg);
  return bg.querySelector('.modal');
}
function closeModal() { const m = $('#modal-bg'); if (m) m.remove(); }
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeModal(); });

function setLoading(btn, on, text) {
  if (!btn) return;
  if (on) { btn.dataset.t = btn.innerHTML; btn.disabled = true; btn.innerHTML = '<span class="spinner"></span>' + (text ? ' ' + text : ''); }
  else { btn.disabled = false; if (btn.dataset.t) btn.innerHTML = btn.dataset.t; }
}

/* ---------- Auth guard ---------- */
function requireRoles(roles) {
  if (!Auth.isLoggedIn()) { location.href = BASE + 'login.html?redirect=' + encodeURIComponent(location.pathname.replace(/^\//, '') + location.search + location.hash); return false; }
  if (!roles.includes(Auth.role())) { location.href = BASE + 'index.html'; return false; }
  return true;
}
async function doLogout() {

  Auth.clear(); location.href = BASE + 'index.html';
}

/* ---------- Vehicle card dùng chung ---------- */
window.__vehicles = {};
function vehicleCard(v, range) {
  window.__vehicles[v.id] = v;
  const cat = CATEGORY[v.category] || { label: v.category, icon: 'car' };
  const can = range || v.status === 'AVAILABLE';
  const dateQuery = range ? '&' + new URLSearchParams(range) : '';
  const inCart = Cart.has(v.id);
  return `<article class="vcard">
    <a class="vcard-img" href="${BASE}vehicle-detail.html?id=${v.id}${dateQuery}">
      <img src="${esc(v.image)}" alt="${esc(v.name)}" loading="lazy">
      <span class="tag">${icon(cat.icon)} ${cat.label}</span>
      <span style="position:absolute;right:12px;top:12px">${badge(VEHICLE_STATUS, v.status)}</span>
    </a>
    <div class="vcard-body">
      <h3>${esc(v.name)}</h3>
      <p class="sub">${v.year ? 'Đời ' + v.year + ' · ' : ''}${esc(v.plate)}</p>
      <ul class="specs">
        ${v.seats ? `<li>${icon('seat')} ${v.seats} chỗ</li>` : ''}
        ${v.engine ? `<li>${icon('engine')} ${v.engine}cc</li>` : (v.category === 'XE_DIEN' || v.category === 'OTO' ? `<li>${icon('bolt')} Điện</li>` : '')}
        ${v.color ? `<li>${icon('palette')} ${esc(v.color)}</li>` : ''}
      </ul>
      <div class="vcard-foot">
        <div class="price"><span>Chỉ từ</span><strong>${fmtMoney(v.price)}</strong><small>/ngày</small></div>
        ${showRentalActions() ? `<button class="btn ${inCart ? 'btn-outline' : 'btn-coral'} btn-sm" data-add="${v.id}"${range ? ` data-start="${esc(range.start)}" data-end="${esc(range.end)}"` : ""} ${can ? '' : 'disabled'}>${inCart ? 'Đã chọn ✓' : 'Chọn thuê'}</button>` : ""}
      </div>
    </div>
  </article>`;
}
document.addEventListener('click', e => {
  const b = e.target.closest('[data-add]'); if (!b) return;
  const v = window.__vehicles[b.dataset.add] || window.__vehicles[+b.dataset.add];
  if (!v || !showRentalActions()) return;
  if (b.dataset.start) sessionStorage.setItem('rentRange', JSON.stringify({ start: b.dataset.start, end: b.dataset.end }));
  if (Cart.has(v.id)) { location.href = BASE + 'booking.html'; return; }
  Cart.add(v); toast('Đã thêm "' + v.name + '" vào giỏ thuê', 'ok');
  b.className = 'btn btn-outline btn-sm'; b.textContent = 'Đã chọn ✓';
});

/* ---------- Header / Footer ---------- */
function renderHeader() {
  const el = $('#app-header'); if (!el) return;
  const u = Auth.user(); const nav = document.body.dataset.nav || '';
  const catLinks = c => `${BASE}vehicles.html?category=${c}`;
  el.innerHTML = `
  <header class="site-header"><div class="container">
    <div class="logo-wrap">
      <a class="logo" href="${BASE}index.html">rentgo<small>.vn</small></a>
      <div class="rating"><span class="stars">★★★★★</span><span>Dịch vụ thuê xe tin cậy</span></div>
    </div>
    <nav class="main-nav" id="mainNav">
      <div class="nav-item"><a class="nav-link ${nav === 'cars' ? 'active' : ''}" href="${catLinks('OTO')}">Ô tô ${icon('chevron')}</a>
        <div class="dropdown"><span class="dd-head">Số chỗ</span>
          <a href="${BASE}vehicles.html?category=OTO&seats=5">Xe 5 chỗ</a><a href="${BASE}vehicles.html?category=OTO&seats=7">Xe 7 chỗ</a></div></div>
      <div class="nav-item"><a class="nav-link ${nav === 'bikes' ? 'active' : ''}" href="${BASE}vehicles.html?group=bike">Xe máy ${icon('chevron')}</a>
        <div class="dropdown"><a href="${catLinks('XE_SO')}">${icon('bike')} Xe số</a><a href="${catLinks('XE_GA')}">${icon('bike')} Xe ga</a><a href="${catLinks('XE_DIEN')}">${icon('bolt')} Xe điện</a></div></div>
      <div class="nav-item"><a class="nav-link ${nav === 'all' ? 'active' : ''}" href="${BASE}vehicles.html">Tất cả xe</a></div>
      ${showRentalActions() ? `<div class="nav-item"><a class="nav-link ${nav === 'orders' ? 'active' : ''}" href="${BASE}my-orders.html">Đơn của tôi</a></div>` : ""}
      <div class="nav-item"><a class="nav-link" href="${BASE}index.html#how">Cách thuê xe <span class="try-me">MỚI</span></a></div>
    </nav>
    <div class="hdr-actions">
      ${showRentalActions() ? `<a class="hdr-btn" href="${BASE}booking.html" title="Giỏ thuê xe" aria-label="Giỏ thuê xe">${icon('cart', 'icon-lg')}<span class="count hidden" data-cart-count></span></a>` : ""}
      <div class="nav-item">
        <button class="hdr-btn user" aria-label="Tài khoản">${icon('user', 'icon-lg')}${icon('chevron')}</button>
        <div class="dropdown right">${u ? `
          <span class="dd-head">${esc(u.fullName || u.email)} · ${ROLE_LABEL[u.role] || u.role}</span>
          ${u.role === 'CUSTOMER' ? `
            <a href="${BASE}my-orders.html">Đơn thuê của tôi</a>
            <a href="${BASE}payment-history.html">Lịch sử thanh toán</a>
            <a href="${BASE}profile.html"${nav === 'profile' ? ' aria-current="page"' : ''}>Hồ sơ cá nhân</a>` : ''}
          ${u.role === 'ADMIN' ? `<a href="${BASE}admin/index.html">Trang quản trị</a>
            <a href="${BASE}admin/payments.html">Quản lý thanh toán</a>` : ''}
          <a href="#" id="btnLogout">Đăng xuất</a>` : `
          <a href="${BASE}login.html">Đăng nhập</a><a href="${BASE}register.html">Đăng ký</a>`}
        </div>
      </div>
      <button class="hamburger" id="hamb" aria-label="Menu">${icon('menu', 'icon-lg')}</button>
    </div>
  </div></header>
  <div class="usp-strip"><div class="container"><span>Thủ tục nhanh – nhận xe trong ngày</span><span>Giá minh bạch, không phí ẩn</span><span>Ô tô &amp; xe máy đa dạng, bảo dưỡng định kỳ</span></div></div>`;
  $('#hamb').onclick = () => $('#mainNav').classList.toggle('open');
  const lo = $('#btnLogout'); if (lo) lo.onclick = e => { e.preventDefault(); doLogout(); };
  Cart.refresh();
}

function renderFooter() {
  const el = $('#app-footer'); if (!el) return;
  el.innerHTML = `<footer class="site-footer"><div class="container">
    <div class="footer-grid">
      <div><a class="logo" href="${BASE}index.html">rentgo<small>.vn</small></a><p style="margin-top:12px;max-width:300px">Hệ thống quản lý &amp; cho thuê xe máy, ô tô trực tuyến. Chọn xe – đặt lịch – thanh toán chỉ vài phút.</p></div>
      <div><h4>Thuê xe</h4><ul><li><a href="${BASE}vehicles.html?category=OTO">Ô tô</a></li><li><a href="${BASE}vehicles.html?category=XE_SO">Xe số</a></li><li><a href="${BASE}vehicles.html?category=XE_GA">Xe ga</a></li><li><a href="${BASE}vehicles.html?category=XE_DIEN">Xe điện</a></li></ul></div>
      <div><h4>Tài khoản</h4><ul>${Auth.role() === 'ADMIN' ?
        `<li><a href="${BASE}admin/index.html">Trang quản trị</a></li><li><a href="${BASE}admin/payments.html">Quản lý thanh toán</a></li>` :
        Auth.role() === 'CUSTOMER' ?
        `<li><a href="${BASE}my-orders.html">Đơn của tôi</a></li><li><a href="${BASE}payment-history.html">Lịch sử thanh toán</a></li><li><a href="${BASE}profile.html">Hồ sơ cá nhân</a></li>` :
        !Auth.isLoggedIn() ? `<li><a href="${BASE}login.html">Đăng nhập</a></li><li><a href="${BASE}register.html">Đăng ký</a></li>` : ''}
      </ul></div>
      <div><h4>Liên hệ</h4><ul><li>Hotline: 1900 0000</li><li>support@rentgo.vn</li><li>08:00 – 21:00 hằng ngày</li></ul></div>
    </div>
    <div class="copy">© ${new Date().getFullYear()} RentGo. Giao diện tham khảo phong cách leasing.com.</div>
  </div></footer>`;
}

document.addEventListener('DOMContentLoaded', () => { renderHeader(); renderFooter(); });


async function searchFleet() {
  const [vehicles, models] = await Promise.all([Api.fleet(), Api.vehicleModels()]);
  const byId = new Map(models.map(model => [model.id, normModel(model)]));
  return vehicles.map(vehicle => byId.get(vehicle.vehicleModelId)).filter(Boolean);
}

function options(label, values, compare) {
  return `<option value="">${esc(label)}</option>` +
    [...new Set(values.filter(Boolean))].sort(compare).map(value => `<option>${esc(value)}</option>`).join('');
}

function validateSearchRange(start, end, error) {
  const valid = (!start && !end) ||
    (start && end && new Date(start) >= new Date() && new Date(end) > new Date(start));
  error.textContent = valid ? '' : 'Chọn đủ ngày và giờ. Giờ nhận xe phải ở tương lai và giờ trả phải sau giờ nhận.';
  return valid;
}

function showRentalActions() {
  return !Auth.isLoggedIn() || Auth.role() === 'CUSTOMER';
}

function renderPager(el, page, pages, total, onPage) {
  el.className = 'rg-pager';
  el.innerHTML = `<span class="rg-count">${total == null ? '' : total + ' kết quả'}</span><div class="rg-page-controls"><button type="button" aria-label="Trang trước" ${page <= 1 ? 'disabled' : ''}>‹</button><span>Trang ${page}${pages == null ? '' : ' / ' + pages}</span><button type="button" aria-label="Trang sau" ${pages != null && page >= pages ? 'disabled' : ''}>›</button></div>`;
  const [prev, next] = el.querySelectorAll('button');
  prev.onclick = () => onPage(page - 1);
  next.onclick = () => onPage(page + 1);
}
