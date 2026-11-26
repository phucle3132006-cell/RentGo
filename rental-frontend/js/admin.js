/* =========================================================
   ADMIN / STAFF / MANAGER – 1 file xử lý 6 trang (data-page)
   ========================================================= */
const ADMIN_NAV = [
  ['dashboard', 'index.html', 'Tổng quan', ['ADMIN']],
  ['vehicles', 'vehicles.html', 'Quản lý xe', ['ADMIN']],
  ['models', 'vehicle-models.html', 'Model xe', ['ADMIN']],
  ['rentals', 'rentals.html', 'Đơn thuê', ['ADMIN']],
  ['payments', 'payments.html', 'Thanh toán & doanh thu', ['ADMIN']],
  ['users', 'users.html', 'Người dùng', ['ADMIN']]
];

function shell(title, actions = '') {
  const page = document.body.dataset.page, u = Auth.user();
  $('#admin-root').innerHTML = `<div class="adm">
    <aside class="adm-side"><a class="logo" href="../index.html">rentgo<small>.vn</small></a>
      <nav>${ADMIN_NAV.filter(n => n[3].includes(u.role)).map(n => `<a href="${n[1]}" class="${n[0] === page ? 'active' : ''}">${n[2]}${n[0] === 'payments' ? '<span id="admPaymentRequestCount" class="badge orange" hidden style="display:none" aria-live="polite"></span>' : ''}</a>`).join('')}<a href="../index.html">← Về website</a></nav>
      <div class="me"><b>${esc(u.fullName || u.email)}</b><br>${ROLE_LABEL[u.role]}<a id="admLogout">Đăng xuất</a></div></aside>
    <main class="adm-main"><div class="adm-top"><h1>${title}</h1><div>${actions}</div></div><div id="adm-content"><div class="skeleton" style="min-height:160px"></div></div></main></div>`;
  $('#admLogout').onclick = doLogout;
  startAdminPaymentRequestPolling();
}

// Admin notifications come only from real, outstanding customer confirmation requests.
let adminPaymentPollTimer = null;
let adminPaymentPollBusy = false;
let adminPaymentRequestCount = null;
async function refreshAdminPaymentRequests() {
  if (adminPaymentPollBusy || document.hidden || Auth.role() !== 'ADMIN') return;
  adminPaymentPollBusy = true;
  try {
    const result = await request('GET', '/api/admin/payments/transfer-requests/pending-count', undefined, true);
    const count = result?.pendingCount;
    if (!Number.isSafeInteger(count) || count < 0) return;
    const navBadge = $('#admPaymentRequestCount');
    if (navBadge) {
      navBadge.textContent = count > 0 ? String(count) : '';
      navBadge.hidden = count === 0;
      navBadge.style.display = count > 0 ? '' : 'none';
      navBadge.setAttribute('aria-label', count + ' yêu cầu xác nhận thanh toán đang chờ');
      navBadge.parentElement.href = count > 0 ? 'payments.html?view=transfers' : 'payments.html';
    }
    if (adminPaymentRequestCount !== null && count > adminPaymentRequestCount)
      toast('Có yêu cầu xác nhận thanh toán mới.', 'ok');
    adminPaymentRequestCount = count;
    window.dispatchEvent(new CustomEvent('rentgo:payment-request-count', { detail: count }));
  } catch {
    // A temporary notification error must not disrupt other administration pages.
  } finally {
    adminPaymentPollBusy = false;
  }
}
function startAdminPaymentRequestPolling() {
  if (adminPaymentPollTimer !== null) return;
  refreshAdminPaymentRequests();
  adminPaymentPollTimer = setInterval(refreshAdminPaymentRequests, 10000);
  document.addEventListener('visibilitychange', refreshAdminPaymentRequests);
  window.addEventListener('pagehide', () => {
    clearInterval(adminPaymentPollTimer);
    adminPaymentPollTimer = null;
    document.removeEventListener('visibilitychange', refreshAdminPaymentRequests);
  }, { once: true });
}

const tableHtml = (cols, rows) => rows.length
  ? `<div class="table-wrap"><table class="tbl"><thead><tr>${cols.map(c => `<th>${c[0]}</th>`).join('')}</tr></thead><tbody>${rows.map(r => `<tr>${cols.map(c => `<td>${c[1](r)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`
  : '<div class="empty"><h3>Chưa có dữ liệu</h3></div>';
const errBox = e => `<div class="alert err">${esc(e.message)}</div>`;

/** Form modal: fields = [{name,label,type,options:[[v,l]],full,required}] */
function formModal(title, fields, values, onSubmit) {
  const m = openModal(`<h2>${title}</h2><div class="form-grid">${fields.map(f => {
    const v = values[f.name] ?? '';
    const ctl = f.type === 'select' ? `<select class="select" name="${f.name}" style="font-size:15px;padding:11px 14px">${f.options.map(o => `<option value="${esc(o[0])}" ${String(o[0]) === String(v) ? 'selected' : ''}>${esc(o[1])}</option>`).join('')}</select>`
      : f.type === 'textarea' ? `<textarea class="textarea" name="${f.name}">${esc(v)}</textarea>`
      : `<input class="input" name="${f.name}" type="${f.type || 'text'}" value="${esc(v)}" ${f.required ? 'required' : ''}>`;
    return `<div class="field ${f.full ? 'full' : ''}"><label>${f.label}</label>${ctl}</div>`;
  }).join('')}</div><div id="fmsg"></div><div class="modal-foot"><button class="btn btn-outline" id="fCancel">Huỷ</button><button class="btn btn-coral" id="fOk">Lưu</button></div>`);
  $('#fCancel', m).onclick = closeModal;
  $('#fOk', m).onclick = async () => {
    const data = {}; let bad = '';
    fields.forEach(f => { const el = m.querySelector(`[name=${f.name}]`); let val = el.value.trim(); if (f.required && !val) bad = `Vui lòng nhập "${f.label}"`; data[f.name] = f.type === 'number' ? (val === '' ? null : +val) : val; });
    if (bad) { $('#fmsg', m).innerHTML = `<div class="alert err">${bad}</div>`; return; }
    setLoading($('#fOk', m), true);
    try { await onSubmit(data); closeModal(); } catch (e) { $('#fmsg', m).innerHTML = errBox(e); setLoading($('#fOk', m), false); }
  };
}

/* ---------------- DASHBOARD ---------------- */
async function pageDashboard() {
  shell('Tổng quan');
  const content = $('#adm-content');
  try {
    const vehicles = toList(await Api.admin.vehicles()).map(normVehicle);
    content.innerHTML = `<div class="stats">
      <div class="stat"><span>Tổng số xe</span><strong>${vehicles.length}</strong></div>
      <div class="stat"><span>Xe sẵn sàng</span><strong>${vehicles.filter(vehicle => vehicle.status === 'AVAILABLE').length}</strong></div>
    </div><a class="btn btn-outline" href="rentals.html">Tra cứu đơn thuê</a>`;
  } catch (error) {
    content.innerHTML = errBox(error);
  }
}
/* ---------------- VEHICLES ---------------- */
const vehicleBody = data => ({ ...data, vehicleModelId: Number(data.vehicleModelId), status: Enum.vehicle.indexOf(data.status) });
const modelBody = data => ({ ...data, category: Enum.category.indexOf(data.category) });

async function pageVehicles() {
  shell('Quản lý xe', '<button class="btn btn-coral" id="btnNew">+ Thêm xe</button>');
  let models = [], list = [], q = '';
  const stOpts = Object.entries(VEHICLE_STATUS).map(([k, v]) => [k, v[0]]);
  const draw = () => {
    const rows = list.filter(v => !q || `${v.name} ${v.plate}`.toLowerCase().includes(q.toLowerCase()));
    $('#adm-content').innerHTML = `<div class="search-bar"><input class="input" id="q" placeholder="Tìm theo biển số / tên xe" value="${esc(q)}"></div>` + tableHtml([
      ['ID', v => v.id], ['Xe', v => `<b>${esc(v.name)}</b><br><small class="muted">${CATEGORY[v.category]?.label || ''} · ${v.year || ''}</small>`], ['Biển số', v => esc(v.plate)], ['Màu', v => esc(v.color)], ['Tình trạng', v => esc(v.condition)],
      ['Giá/ngày', v => fmtMoney(v.price)], ['Trạng thái', v => badge(VEHICLE_STATUS, v.status)],
      ['', v => `<div class="actions"><button class="btn btn-outline btn-sm" data-e="${v.id}">Sửa</button><button class="btn btn-outline btn-sm" data-images="${v.id}">Ảnh</button><button class="btn btn-danger btn-sm" data-d="${v.id}">Xoá</button></div>`]], rows);
    $('#q').oninput = e => { q = e.target.value; const pos = q.length; draw(); const i = $('#q'); i.focus(); i.setSelectionRange(pos, pos); };
  };
  const fields = () => [
    { name: 'vehicleModelId', label: 'Model xe', type: 'select', options: models.map(m => [m.id, `${m.brand} ${m.model} (${m.year})`]), full: true },
    { name: 'licensePlate', label: 'Biển số', required: true }, { name: 'color', label: 'Màu sắc', required: true },
    { name: 'condition', label: 'Tình trạng xe', required: true }, { name: 'status', label: 'Trạng thái', type: 'select', options: stOpts }];
  const load = async () => { try { list = toList(await Api.admin.vehicles()).map(normVehicle); draw(); } catch (e) { $('#adm-content').innerHTML = errBox(e); } };
  try { models = toList(await Api.admin.models()).map(normModel); }
  catch (e) { $('#adm-content').innerHTML = errBox(e); $('#btnNew').disabled = true; return; }
  $('#btnNew').disabled = !models.length;
  $('#btnNew').onclick = () => formModal('Thêm xe', fields(), { status: 'AVAILABLE', condition: 'Tốt' }, async d => { await Api.admin.createVehicle(vehicleBody(d)); toast('Đã thêm xe', 'ok'); load(); });
  $('#adm-content').onclick = e => {
    const ed = e.target.closest('[data-e]'), de = e.target.closest('[data-d]');
    const imageButton = e.target.closest('[data-images]');
    if (imageButton) manageImages(Number(imageButton.dataset.images), load);
    if (ed) { const v = list.find(x => x.id == ed.dataset.e); formModal('Sửa xe', fields(), { vehicleModelId: v.modelId, licensePlate: v.plate, color: v.color, condition: v.condition, status: v.status }, async d => { await Api.admin.updateVehicle(v.id, vehicleBody(d)); toast('Đã cập nhật', 'ok'); load(); }); }
    if (de && !de.disabled && confirm('Xoá xe này?')) {
      setLoading(de, true);
      Api.admin.deleteVehicle(de.dataset.d).then(() => { toast('Đã xoá', 'ok'); load(); })
        .catch(err => { toast(err.message, 'err'); setLoading(de, false); });
    }
  };
  load();
}

/* ---------------- VEHICLE MODELS ---------------- */
async function pageModels() {
  shell('Model xe', '<button class="btn btn-coral" id="btnNew">+ Thêm model</button>');
  let list = [];
  const fields = [
    { name: 'category', label: 'Loại xe', type: 'select', options: Object.entries(CATEGORY).map(([k, v]) => [k, v.label]) },
    { name: 'brand', label: 'Hãng', required: true }, { name: 'model', label: 'Dòng xe', required: true }, { name: 'year', label: 'Năm SX', type: 'number', required: true },
    { name: 'seats', label: 'Số chỗ', type: 'number', required: true }, { name: 'engineCapacity', label: 'Dung tích (cc) – để trống nếu xe điện', type: 'number' },
    { name: 'pricePerDay', label: 'Giá thuê / ngày (₫)', type: 'number', required: true, full: true }, { name: 'description', label: 'Mô tả', type: 'textarea', full: true }];
  const draw = () => $('#adm-content').innerHTML = tableHtml([
    ['ID', m => m.id], ['Loại', m => CATEGORY[m.category]?.label || m.category], ['Model', m => `<b>${esc(m.brand)} ${esc(m.model)}</b>`], ['Năm', m => m.year], ['Chỗ', m => m.seats], ['Động cơ', m => m.engine ? m.engine + 'cc' : 'Điện'], ['Giá/ngày', m => fmtMoney(m.price)],
    ['', m => `<div class="actions"><button class="btn btn-outline btn-sm" data-e="${m.id}">Sửa</button><button class="btn btn-danger btn-sm" data-d="${m.id}">Xoá</button></div>`]], list);
  const load = async () => { try { list = toList(await Api.admin.models()).map(normModel); draw(); } catch (e) { $('#adm-content').innerHTML = errBox(e); } };
  $('#btnNew').onclick = () => formModal('Thêm model', fields, { category: 'OTO', year: new Date().getFullYear() }, async d => { await Api.admin.createModel(modelBody(d)); toast('Đã thêm', 'ok'); load(); });
  $('#adm-content').onclick = e => {
    const ed = e.target.closest('[data-e]'), de = e.target.closest('[data-d]');
    if (ed) { const m = list.find(x => x.id == ed.dataset.e); formModal('Sửa model', fields, { ...m, engineCapacity: m.engine, pricePerDay: m.price }, async d => { await Api.admin.updateModel(m.id, modelBody(d)); toast('Đã cập nhật', 'ok'); load(); }); }
    if (de && !de.disabled && confirm('Xoá model này?')) {
      setLoading(de, true);
      Api.admin.deleteModel(de.dataset.d).then(() => { toast('Đã xoá', 'ok'); load(); })
        .catch(err => { toast(err.message, 'err'); setLoading(de, false); });
    }
  };
  load();
}

async function manageImages(id, refreshVehicles) {
  const modal = openModal(`<h2>Ảnh xe #${id}</h2>
    <div id="imageList" class="skeleton" aria-label="Đang tải ảnh"></div>
    <form id="imageForm" class="panel">
      <input name="file" type="file" accept="image/jpeg,image/png,image/webp" required>
      <label><input name="isPrimary" type="checkbox" value="true"> Đặt làm ảnh chính</label>
      <button class="btn btn-coral" type="submit">Tải ảnh lên</button>
    </form><div id="imageMessage" role="status"></div>`);
  const list = $('#imageList', modal);
  const message = $('#imageMessage', modal);
  const form = $('#imageForm', modal);
  let busy = false;
  async function load() {
    try {
      const images = await Api.images(id);
      list.classList.remove('skeleton');
      list.innerHTML = images.length ? images.map(image => `<div class="actions" style="margin-bottom:12px">
        <img style="width:100px;object-fit:cover" src="${esc(image.imageUrl)}" alt="Ảnh xe">
        ${image.isPrimary ? '<span>Ảnh chính</span>' : `<button class="btn btn-outline btn-sm" data-primary="${image.id}">Đặt ảnh chính</button>`}
        <button class="btn btn-danger btn-sm" data-delete="${image.id}">Xóa ảnh</button>
      </div>`).join('') : '<p>Xe chưa có ảnh.</p>';
    } catch (error) {
      list.classList.remove('skeleton');
      list.innerHTML = errBox(error);
    }
  }
  async function change(button, action) {
    if (busy) return;
    busy = true;
    setLoading(button, true);
    message.textContent = '';
    try { await action(); await load(); refreshVehicles(); }
    catch (error) { message.innerHTML = errBox(error); }
    finally { busy = false; setLoading(button, false); }
  }
  form.onsubmit = event => {
    event.preventDefault();
    change(event.submitter, async () => {
      await Api.admin.uploadImage(id, new FormData(form));
      form.reset();
    });
  };
  list.onclick = event => {
    const primary = event.target.closest('[data-primary]');
    const remove = event.target.closest('[data-delete]');
    if (busy) return;
    if (primary) change(primary, () => Api.admin.primaryImage(id, Number(primary.dataset.primary)));
    if (remove && confirm('Xóa ảnh xe này?')) change(remove, () => Api.admin.deleteImage(id, remove.dataset.delete));
  };
  await load();
}

function pageRentals() {
  shell('Quản lý đơn thuê');
  $('#adm-content').classList.add('compact-admin');
  $('#adm-content').innerHTML = `<form id="orderLookup" class="search-bar">
    <label for="orderId">ID đơn thuê (số)</label>
    <input class="input" id="orderId" inputmode="numeric" pattern="[1-9][0-9]*" required>
    <button class="btn btn-coral">Tra cứu</button></form>
    <div id="orderResult" aria-live="polite"></div>`;
  $('#orderLookup').insertAdjacentHTML('beforebegin', '<form id="orderFilters" class="search-bar">' +
    '<label for="orderSearch">Tìm đơn</label><input class="input" id="orderSearch" name="search" maxlength="100" placeholder="Mã đơn, ID hoặc tên khách">' +
    '<label for="orderStatusFilter">Trạng thái</label><select class="select" name="status" id="orderStatusFilter">' +
    '<option value="">Tất cả</option>' + Enum.rental.map(status => '<option value="' + status + '">' +
      esc(RENTAL_STATUS[status][0]) + '</option>').join('') +
    '</select><button class="btn btn-coral">Lọc</button></form><div id="orderList" aria-live="polite"></div>' +
    '<div id="orderPager" class="search-bar"></div>');
  const result = $('#orderResult');
  let busy = false;
  const list = $('#orderList');
  const pager = $('#orderPager');
  const filters = $('#orderFilters');
  let listRequest = 0;
  let currentPage = 1;
  let detail;

  async function loadOrders(page, quiet = false) {
    currentPage = page;
    const requestId = ++listRequest;
    if (!quiet) {
      pager.replaceChildren();
      list.innerHTML = '<div class="skeleton" aria-label="Đang tải danh sách đơn"></div>';
    }
    const params = new URLSearchParams(new FormData(filters));
    if (!params.get('status')) params.delete('status');
    params.set('page', page);
    try {
      const data = await request('GET', orderPath + '?' + params, undefined, true);
      if (requestId !== listRequest) return;
      const pages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
      if (page > pages) return loadOrders(pages);
      list.innerHTML = tableHtml([
        ['Mã đơn', o => '<a href="#orderResult" data-order="' + esc(o.id) + '">' + esc(o.code) + '</a>'],
        ['Khách hàng', o => esc(o.customer.name)],
        ['Nhận xe', o => fmtDateTime(o.start)],
        ['Trả xe', o => fmtDateTime(o.end)],
        ['Tổng tiền', o => fmtMoney(o.total)],
        ['Trạng thái', o => badge(RENTAL_STATUS, o.status)]
      ], data.items.map(normRental));
      renderPager(pager, page, pages, data.totalCount, loadOrders);
    } catch (error) {
      if (!quiet && requestId === listRequest) list.innerHTML = errBox(error);
    }
  }

  filters.onsubmit = event => { event.preventDefault(); loadOrders(1); };
  list.onclick = event => {
    const link = event.target.closest('[data-order]');
    if (!link || busy) return;
    $('#orderId').value = link.dataset.order;
    $('#orderLookup').requestSubmit($('#orderLookup button'));
  };
  loadOrders(1);
  function render(order) {
    detail = order;
    const action = {

      PAID: ['RENTING', 'Giao xe'],
      RENTING: ['COMPLETED', 'Hoàn tất']
    }[order.status];
    result.innerHTML = `<div class="panel"><h2>Đơn ${esc(order.code)} ${badge(RENTAL_STATUS, order.status)}</h2>
      <div class="order-meta"><span>Khách hàng ${esc(order.customer.name)}</span><span>Nhận ${fmtDateTime(order.start)}</span><span>Trả ${fmtDateTime(order.end)}</span></div>
      ${tableHtml([['Xe', i => esc(i.name)], ['Đơn giá', i => fmtMoney(i.price)], ['Số ngày', i => i.days], ['Thành tiền', i => fmtMoney(i.subtotal)], ['Trạng thái', i => badge(DETAIL_STATUS, i.status)]], order.items)}
      <div class="sum-row total"><span>Tổng cộng</span><span>${fmtMoney(order.total)}</span></div>
      ${order.note ? `<p>${esc(order.note)}</p>` : ''}
      ${order.status === 'CANCELLED' && order.rejectionReason ? `<p>Lý do từ chối ${esc(order.rejectionReason)}</p>` : ''}
      <div id="orderError"></div>${action ? `<button class="btn btn-coral" id="orderAction">${action[1]}</button>` : ''}</div>`;
    const button = $('#orderAction');
    if (button) button.onclick = async () => {
      if (busy || !confirm(action[1] + '?')) return;
      busy = true;
      setLoading(button, true);
      try {
        const updated = action[0] === 'CANCELLED' ? await Api.cancelRental(order.id) : await Api.admin.updateRentalStatus(order.id, action[0]);
        render(normRental(updated));
        loadOrders(currentPage);
      } catch (error) { $('#orderError').innerHTML = errBox(error); setLoading(button, false); }
      finally { busy = false; }
    };
  }
  $('#orderLookup').onsubmit = async event => {
    event.preventDefault();
    if (busy) return;
    busy = true;
    setLoading(event.submitter, true);
    detail = undefined;
    result.innerHTML = '<div class="skeleton" aria-label="Đang tải đơn"></div>';
    try { render(normRental(await Api.rental($('#orderId').value))); }
    catch (error) { result.innerHTML = errBox(error); }
    finally { busy = false; setLoading(event.submitter, false); }
  };

  async function refresh() {
    if (document.hidden || busy) return;
    const previous = detail;
    loadOrders(currentPage, true);
    if (!previous) return;
    try {
      const latest = normRental(await Api.rental(previous.id));
      if (!busy && previous === detail && JSON.stringify(previous) !== JSON.stringify(latest))
        render(latest);
    } catch { /* Preserve the current detail during a temporary fetch failure. */ }
  }
  const timer = setInterval(refresh, 10000);
  document.addEventListener('visibilitychange', refresh);
  window.addEventListener('pagehide', () => clearInterval(timer), { once: true });
}

function pageUsers() {
  shell('Quản lý người dùng');
  const path = '/api/admin/users';
  const content = $('#adm-content');
  content.classList.add('compact-admin');
  content.innerHTML = `
    <div class="panel">
      <div class="search-bar" id="userViews" role="group" aria-label="Danh sách người dùng">
        <button class="btn btn-outline btn-coral" type="button" data-view="users" aria-pressed="true">Người dùng</button>
        <button class="btn btn-outline" type="button" data-view="blacklist" aria-pressed="false">Danh sách chặn</button>
      </div>
      <form class="search-bar" id="userSearch">
        <label for="userTerm">Tìm kiếm</label>
        <input class="input" id="userTerm" maxlength="100" placeholder="Tên, email hoặc điện thoại">
        <button class="btn btn-coral" type="submit">Tìm</button>
      </form>
      <div id="userFeedback" role="status"></div>
      <div id="userRows"></div>
      <div id="userPager"></div>
    </div>
    <dialog class="panel user-dialog" id="userDialog" aria-labelledby="userTitle"></dialog>`;
  const rows = $('#userRows');
  const pager = $('#userPager');
  const term = $('#userTerm');
  const dialog = $('#userDialog');
  const feedback = $('#userFeedback');
  let page = 1;
  let ticket = 0;
  let busy = false;
  let blacklist = false;
  let users = [];
  const status = user => {
    if (user.status === 'LOCKED') return 'Đã chặn';
    return user.contactBlocked ? 'Bị chặn theo email hoặc SĐT' : 'Đang hoạt động';
  };
  const contact = user => esc(user.email) + '<br>' + esc(user.phone);
  const button = (user, action, label, style = 'btn-outline') =>
    '<button type="button" class="btn ' + style + ' btn-sm" data-user="' +
    esc(user.id) + '" data-user-action="' + action + '">' + label + '</button>';
  const actions = user => {
    const details = button(user, 'details', 'Chi tiết');
    if (user.status === 'LOCKED') return details + ' ' + button(user, 'unblock', 'Bỏ chặn');
    return user.role === 'CUSTOMER' ? details + ' ' + button(user, 'block', 'Chặn người dùng', 'btn-danger') : details;
  };

  async function load(next) {
    const current = ++ticket;
    page = next;
    pager.replaceChildren();
    rows.innerHTML = '<div class="skeleton" aria-label="Đang tải người dùng"></div>';
    const params = new URLSearchParams({ page, search: term.value.trim() });
    users = [];
    try {
      const data = await request('GET', path + (blacklist ? '/blacklist?' : '?') + params, undefined, true);
      if (current !== ticket) return;
      const pages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
      if (page > pages) return load(pages);
      users = data.items;
      rows.innerHTML = blacklist ? tableHtml([
        ['Họ tên', user => esc(user.fullName)],
        ['Email và SĐT bị chặn', contact],
        ['Lý do', user => esc(user.reason)],
        ['Người chặn', user => esc(user.blockedByName ?? '—')],
        ['Thời điểm', user => esc(fmtDateTime(user.blockedAt))],
        ['', user => button(user, 'unblock', 'Bỏ chặn')]
      ], users) : tableHtml([
        ['Họ tên', user => esc(user.fullName)],
        ['Liên hệ', contact],
        ['Vai trò', user => esc(ROLE_LABEL[user.role] ?? user.role)],
        ['Trạng thái', user => status(user)],
        ['', actions]
      ], users);
      renderPager(pager, page, pages, data.totalCount, load);
    } catch (error) {
      if (current === ticket) rows.innerHTML = errBox(error);
    }
  }

  function show(user) {
    const fields = [
      ['Email', user.email], ['Điện thoại', user.phone], ['Vai trò', ROLE_LABEL[user.role] ?? user.role],
      ['Trạng thái', status(user)], ['Ngày tạo', fmtDateTime(user.createdAt)]
    ];
    if (user.blockReason) fields.push(['Lý do chặn', user.blockReason]);
    const blocked = user.status === 'LOCKED';
    const statusButton = blocked || user.role === 'CUSTOMER' ?
      '<button type="button" class="btn ' + (blocked ? 'btn-coral' : 'btn-danger') + '" id="userToggle">' +
        (blocked ? 'Bỏ chặn' : 'Chặn người dùng') + '</button>' : '';
    dialog.innerHTML = '<h2 id="userTitle">' + esc(user.fullName) + '</h2><dl>' +
      fields.map(([label, value]) => '<dt>' + label + '</dt><dd>' + esc(value ?? '—') + '</dd>').join('') +
      '</dl><div class="search-bar"><button type="button" class="btn btn-outline" id="userClose">Đóng</button>' +
      statusButton + '</div>';
    $('#userClose').onclick = () => dialog.close();
    if (statusButton) $('#userToggle').onclick = () => confirmStatus(user, !blocked);
    dialog.showModal();
  }

  function confirmStatus(user, blocked) {
    const action = blocked ? 'Chặn người dùng' : 'Bỏ chặn';
    dialog.innerHTML = `
      <h2 id="userTitle">${action}</h2>
      <p><strong>${esc(user.fullName)}</strong><br>${contact(user)}</p>
      <form id="userStatusForm">
        ${blocked ? `
        <label for="userReason">Lý do chặn
          <input class="input" id="userReason" name="reason" list="blockReasons" required maxlength="500">
        </label>
        <datalist id="blockReasons">
          <option value="Vi phạm điều khoản thuê xe"></option>
          <option value="Có dấu hiệu gian lận"></option>
          <option value="Cung cấp thông tin sai"></option>
        </datalist>
        <p class="muted">Tài khoản sẽ bị đăng xuất. Email và số điện thoại này sẽ bị chặn khi đăng ký.</p>
        ` : '<p class="muted">Gỡ chặn cho tài khoản, email và số điện thoại này.</p>'}
        <div id="userError" role="alert"></div>
        <div class="search-bar">
          <button class="btn btn-outline" type="button" id="userClose">Hủy</button>
          <button class="btn ${blocked ? 'btn-danger' : 'btn-coral'}" type="submit">Xác nhận ${blocked ? 'chặn' : 'bỏ chặn'}</button>
        </div>
      </form>`;
    const close = $('#userClose');
    close.onclick = () => { if (!busy) dialog.close(); };
    $('#userStatusForm').onsubmit = async event => {
      event.preventDefault();
      if (busy) return;
      const reasonInput = event.currentTarget.elements.reason;
      const reason = reasonInput?.value.trim();
      if (blocked && !reason) {
        $('#userError').textContent = 'Vui lòng nhập lý do chặn.';
        reasonInput.focus();
        return;
      }
      const submit = event.submitter;
      busy = true;
      close.disabled = true;
      setLoading(submit, true);
      $('#userError').replaceChildren();
      try {
        const result = await request('PUT', itemPath(path, user.id) + '/status', { blocked, reason }, true);
        dialog.close();
        feedback.textContent = blocked ? 'Đã chặn ' + user.fullName + '.' :
          'Đã bỏ chặn ' + user.fullName + '.' + (result.contactBlocked ? ' Một thông tin liên hệ vẫn nằm trong mục chặn khác.' : '');
        await load(page);
      } catch (error) {
        $('#userError').innerHTML = errBox(error);
      } finally {
        setLoading(submit, false);
        close.disabled = false;
        busy = false;
      }
    };
    if (!dialog.open) dialog.showModal();
    ($('#userReason') ?? close).focus();
  }

  rows.onclick = async event => {
    const control = event.target.closest('[data-user]');
    if (!control || busy) return;
    const user = users.find(item => String(item.id) === control.dataset.user);
    if (!user) return;
    feedback.textContent = '';
    if (control.dataset.userAction !== 'details') {
      confirmStatus(user, control.dataset.userAction === 'block');
      return;
    }
    busy = true;
    setLoading(control, true);
    try { show(await request('GET', itemPath(path, user.id), undefined, true)); }
    catch (error) { toast(error.message, 'err'); }
    finally { busy = false; setLoading(control, false); }
  };
  dialog.addEventListener('cancel', event => { if (busy) event.preventDefault(); });
  $('#userViews').onclick = event => {
    const view = event.target.closest('[data-view]');
    if (!view || busy || view.getAttribute('aria-pressed') === 'true') return;
    blacklist = view.dataset.view === 'blacklist';
    for (const control of $('#userViews').querySelectorAll('[data-view]')) {
      const selected = control === view;
      control.classList.toggle('btn-coral', selected);
      control.setAttribute('aria-pressed', String(selected));
    }
    feedback.textContent = '';
    term.value = '';
    term.placeholder = blacklist ? 'Tên, email, điện thoại hoặc lý do' : 'Tên, email hoặc điện thoại';
    load(1);
  };
  $('#userSearch').onsubmit = event => { event.preventDefault(); load(1); };
  load(1);
}

document.addEventListener('DOMContentLoaded', () => {
  if (!requireRoles(['ADMIN'])) return;
  const pages = {
    dashboard: pageDashboard,
    vehicles: pageVehicles,
    models: pageModels,
    rentals: pageRentals,
    payments: () => pagePayments(),
    users: () => pageUsers()
  };
  pages[document.body.dataset.page]?.();
});

