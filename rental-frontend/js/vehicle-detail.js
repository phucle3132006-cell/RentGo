/* Chi tiết xe: xem thông tin, chọn thời gian, kiểm tra availability, thuê */
document.addEventListener('DOMContentLoaded', async () => {
  const id = getParam('id'); const root = $('#root');
  if (!id) { root.innerHTML = '<div class="empty"><h3>Thiếu mã xe</h3><a class="btn btn-purple" href="vehicles.html">Về danh sách</a></div>'; return; }
  let v;
  try { v = normVehicle(await Api.vehicle(id)); }
  catch (e) { root.innerHTML = `<div class="empty"><h3>Không tìm thấy xe</h3><p>${esc(e.message)}</p><a class="btn btn-purple" href="vehicles.html">Về danh sách</a></div>`; return; }
  document.title = v.name + ' – RentGo'; $('#title').textContent = v.name; $('#bc').textContent = v.name;
  const cat = CATEGORY[v.category] || { label: v.category };
  const range = defaultRange();
  for (const key of ['start', 'end']) {
    const value = getParam(key);
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)) range[key] = value;
  }
  const minNow = toLocalInput(new Date());
  window.__vehicles[v.id] = v;

  root.innerHTML = `<div class="detail-layout">
    <div>
      <div class="gallery-main"><img id="mainImg" src="${esc(v.images[0])}" alt="${esc(v.name)}"></div>
      <div class="thumbs">${v.images.length > 1 ? v.images.map((u, i) => `<img src="${esc(u)}" class="${i ? '' : 'active'}" alt="">`).join('') : ''}</div>
      <div class="panel"><h2>Thông tin xe</h2><div class="spec-table">
        <div><span>Loại xe</span><b>${cat.label}</b></div><div><span>Hãng</span><b>${esc(v.brand)}</b></div>
        <div><span>Dòng xe</span><b>${esc(v.model)}</b></div><div><span>Năm sản xuất</span><b>${v.year || '—'}</b></div>
        <div><span>Số chỗ</span><b>${v.seats || '—'}</b></div><div><span>Dung tích động cơ</span><b>${v.engine ? v.engine + ' cc' : 'Động cơ điện'}</b></div>
        <div><span>Biển số</span><b>${esc(v.plate)}</b></div><div><span>Màu sắc</span><b>${esc(v.color) || '—'}</b></div>
        <div><span>Tình trạng xe</span><b>${esc(v.condition) || '—'}</b></div><div><span>Trạng thái</span><b>${badge(VEHICLE_STATUS, v.status)}</b></div>
      </div></div>
      <div class="panel"><h2>Mô tả</h2><p>${esc(v.description) || 'Chưa có mô tả.'}</p></div>
    </div>
    <aside class="book-card">
      <div class="big-price">${fmtMoney(v.price)} <small>/ngày</small></div>
      <p class="muted" style="margin-bottom:16px">Giá cuối cùng do hệ thống tính khi tạo đơn.</p>
      <div class="field"><label>Nhận xe</label><input type="datetime-local" class="input" id="dStart" value="${range.start}" min="${minNow}"></div>
      <div class="field"><label>Trả xe</label><input type="datetime-local" class="input" id="dEnd" value="${range.end}" min="${minNow}"></div>
      <div class="sum-row"><span>Số ngày (ước tính)</span><b id="days">—</b></div>
      <div class="sum-row total"><span>Tạm tính</span><span id="est">—</span></div>
      <div id="availMsg"></div>
      <button class="btn btn-coral btn-block" id="btnRent" ${v.status === 'MAINTENANCE' ? 'disabled' : ''}>Thuê xe này</button>
      <button class="btn btn-outline btn-block" id="btnCart" style="margin-top:10px" ${v.status === 'MAINTENANCE' ? 'disabled' : ''}>${Cart.has(v.id) ? 'Đã trong giỏ ✓' : 'Thêm vào giỏ (thuê nhiều xe)'}</button>
      ${v.status !== 'AVAILABLE' ? '<div class="alert warn">Xe hiện không ở trạng thái sẵn sàng.</div>' : ''}
    </aside></div>
    <section class="section" style="padding-top:0"><h2>Xe cùng loại</h2><div class="vgrid" id="similar"></div></section>`;

  $$('.thumbs img').forEach(t => t.onclick = () => { $('#mainImg').src = t.src; $$('.thumbs img').forEach(x => x.classList.toggle('active', x === t)); });
  const calc = () => {
    const d = estimateDays($('#dStart').value, $('#dEnd').value);
    $('#days').textContent = d ? d + ' ngày' : '—'; $('#est').textContent = d ? fmtMoney(d * v.price) : '—'; $('#availMsg').innerHTML = '';
  };
  $('#dStart').onchange = $('#dEnd').onchange = calc; calc();

  if (!showRentalActions()) {
    $('#btnCart').remove();
    $('#btnRent').remove();
  } else {
    const addToBooking = async (button, goToBooking) => {
      const start = $('#dStart').value, end = $('#dEnd').value;
      if (!start || !end) { $('#availMsg').textContent = 'Chọn đủ ngày giờ nhận và trả xe.'; return; }
      if (!validateSearchRange(start, end, $('#availMsg'))) return;

      button.disabled = true;
      try {
        const vehicles = await Api.fleet({
          start: new Date(start).toISOString(), end: new Date(end).toISOString()
        });
        if (!vehicles.some(vehicle => vehicle.id === v.id)) {
          $('#availMsg').textContent = 'Xe không còn trống trong thời gian đã chọn.';
          return;
        }
        sessionStorage.setItem('rentRange', JSON.stringify({ start, end }));
        if (!Cart.add(v) || goToBooking) location.href = 'booking.html';
        else { toast('Đã thêm vào giỏ thuê', 'ok'); button.textContent = 'Đã trong giỏ ✓'; }
      } catch (error) { $('#availMsg').textContent = error.message; }
      finally { button.disabled = false; }
    };
    $('#btnCart').onclick = event => addToBooking(event.currentTarget, false);
    $('#btnRent').onclick = event => addToBooking(event.currentTarget, true);
  }

  try {
    const list = toList(await Api.vehicles({ category: v.category })).map(normVehicle).filter(x => x.id !== v.id && x.status === 'AVAILABLE').slice(0, 4);
    $('#similar').innerHTML = list.map(vehicle => vehicleCard(vehicle)).join('') || '<p class="muted">Chưa có xe tương tự.</p>';
  } catch (e) { $('#similar').textContent = e.message; }
});

