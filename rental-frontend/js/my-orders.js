/* Đơn thuê của tôi */
document.addEventListener('DOMContentLoaded', async () => {
  if (!requireRoles(['CUSTOMER'])) return;
  if (location.hash === '#profile') { location.replace('profile.html'); return; }

  let orders = [], filter = '';

  function renderFilter() {
    const keys = ['', ...Object.keys(RENTAL_STATUS)];
    $('#statusFilter').innerHTML = keys.map(k => `<button class="chip" style="${filter === k ? 'background:var(--teal);color:#fff' : ''}" data-s="${k}">${k ? RENTAL_STATUS[k][0] : 'Tất cả'} (${k ? orders.filter(o => o.status === k).length : orders.length})</button>`).join('');
  }
  $('#statusFilter').onclick = e => { const b = e.target.closest('[data-s]'); if (b) { filter = b.dataset.s; renderFilter(); render(); } };

  function render() {
    const l = orders.filter(o => !filter || o.status === filter);
    $('#list').innerHTML = l.length ? l.map(o => `<div class="order-card">
      <header><h3>Đơn ${esc(o.code)}</h3>${badge(RENTAL_STATUS, o.status)}</header>
      <div class="order-meta"><span>Nhận xe: <b>${fmtDateTime(o.start)}</b></span><span>Trả xe: <b>${fmtDateTime(o.end)}</b></span><span>Tạo lúc: <b>${fmtDateTime(o.createdAt)}</b></span></div>
      <div class="order-items">${o.items.map(i => `<span>${esc(i.name)}${i.plate ? ' · ' + esc(i.plate) : ''}</span>`).join('')}</div>
      ${o.note ? `<p class="muted" style="margin-bottom:10px">Ghi chú: ${esc(o.note)}</p>` : ''}
      ${o.status === 'CANCELLED' && o.rejectionReason ? `<p class="muted">Lý do từ chối: ${esc(o.rejectionReason)}</p>` : ''}
      <div class="order-foot"><div class="total">${fmtMoney(o.total)}</div><div style="display:flex;gap:8px;flex-wrap:wrap">
        <button class="btn btn-outline btn-sm" data-detail="${o.id}">Chi tiết</button>
        ${o.canUseManualPayment ? `<a class="btn btn-outline btn-sm" href="payment.html?orderId=${encodeURIComponent(o.id)}">Thanh toán</a>` : ''}
        ${['PENDING', 'CONFIRMED'].includes(o.status) ? `<button class="btn btn-danger btn-sm" data-cancel="${o.id}">Huỷ đơn</button>` : ''}
      </div></div></div>`).join('') : '<div class="empty"><h3>Chưa có đơn thuê nào</h3><p>Hãy chọn xe và đặt lịch ngay.</p><a class="btn btn-coral" href="vehicles.html">Thuê xe</a></div>';
  }

  let page = 1;
  const pager = document.createElement('div');
  pager.className = 'pager';
  $('#list').after(pager);
  let generation = 0;
  async function load(quiet = false) {
    const ticket = ++generation;
    if (!quiet) {
      pager.replaceChildren();
      $('#list').innerHTML = '<div class="skeleton" aria-label="Đang tải đơn"></div>';
    }
    try {
      const result = await Api.myRentals(page);
      if (ticket !== generation) return;
      const nextOrders = result.items.map(normRental);
      if (quiet && JSON.stringify(nextOrders) === JSON.stringify(orders)) return;
      orders = nextOrders;
      pager.replaceChildren();
      if (!orders.length && page > 1) { page--; return load(); }
      renderFilter();
      render();
      renderPager(pager, page, Math.max(1, Math.ceil(result.totalCount / result.pageSize)), result.totalCount, next => { page = next; load(); });
    } catch (e) { if (!quiet && ticket === generation) $('#list').innerHTML = `<div class="alert err">${esc(e.message)}</div>`; }
  }
  $('#list').onclick = async e => {
    const d = e.target.closest('[data-detail]'), c = e.target.closest('[data-cancel]');
    if (d) {
      try {
        const r = normRental(await Api.rental(d.dataset.detail));
        openModal(`<h2>Đơn ${esc(r.code)} ${badge(RENTAL_STATUS, r.status)}</h2>
          <div class="order-meta"><span>Nhận: <b>${fmtDateTime(r.start)}</b></span><span>Trả: <b>${fmtDateTime(r.end)}</b></span></div>
          <div class="table-wrap"><table class="tbl"><thead><tr><th>Xe</th><th>Đơn giá</th><th>Ngày</th><th>Thành tiền</th><th>Trạng thái</th></tr></thead><tbody>
          ${r.items.map(i => `<tr><td><b>${esc(i.name)}</b><br><small class="muted">${esc(i.plate)}</small></td><td>${fmtMoney(i.price)}</td><td>${i.days ?? '—'}</td><td>${fmtMoney(i.subtotal)}</td><td>${badge(DETAIL_STATUS, i.status)}</td></tr>`).join('')}</tbody></table></div>
          <div class="sum-row total"><span>Tổng cộng</span><span>${fmtMoney(r.total)}</span></div>
          ${r.note ? `<p class="muted">Ghi chú: ${esc(r.note)}</p>` : ''}`, { width: '760px' });
      } catch (err) { toast(err.message, 'err'); }
    }
    if (c && confirm('Bạn chắc chắn muốn huỷ đơn này?')) {
      setLoading(c, true);
      try { await Api.cancelRental(c.dataset.cancel); toast('Đã huỷ đơn', 'ok'); await load(); }
      catch (err) { toast(err.message, 'err'); setLoading(c, false); }
    }
  };

  const refresh = () => { if (!document.hidden) load(true); };
  const timer = setInterval(refresh, 10000);
  document.addEventListener('visibilitychange', refresh);
  window.addEventListener('pagehide', () => clearInterval(timer), { once: true });
  load();
});

