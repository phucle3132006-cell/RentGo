document.addEventListener('DOMContentLoaded', async () => {
  if (!requireRoles(['CUSTOMER'])) return;

  const create = $('#btnCreate');
  const start = $('#dStart');
  const end = $('#dEnd');
  const message = $('#availMsg');
  let items = [];
  let submitting = false;
  let created = false;
  const show = (text, type = 'err') => {
    message.innerHTML = '<div class="alert ' + type + '">' + esc(text) + '</div>';
  };

  let saved;
  try { saved = JSON.parse(sessionStorage.getItem('rentRange')); } catch {}
  const range = saved || defaultRange();
  start.value = range.start;
  end.value = range.end;
  start.min = end.min = toLocalInput(new Date());

  function draw() {
    $('#items').innerHTML = items.length ? items.map(item => `<div class="cart-item">
      <img src="${esc(item.image)}" alt=""><div><h4>${esc(item.name)}</h4>
      <div class="muted">${esc(item.plate)} · ${badge(VEHICLE_STATUS, item.status)}</div>
      <strong>${fmtMoney(item.price)}</strong> /ngày</div>
      <button class="btn btn-danger btn-sm rm" data-rm="${item.id}">Bỏ xe</button></div>`).join('')
      : '<div class="empty"><h3>Giỏ thuê đang trống</h3><a href="vehicles.html">Chọn xe</a></div>';
    summarize();
  }

  function summarize() {
    if (submitting || created) return;
    const days = estimateDays(start.value, end.value);
    $('#sCount').textContent = items.length;
    $('#sDays').textContent = days ? days + ' ngày' : '—';
    $('#sTotal').textContent = days && items.length ? fmtMoney(items.reduce((sum, item) => sum + item.price * days, 0)) : '—';
    create.disabled = !items.length || items.some(item => item.status !== 'AVAILABLE');
  }

  $('#items').onclick = event => {
    const remove = event.target.closest('[data-rm]');
    if (!remove || submitting || created) return;
    const id = Number(remove.dataset.rm);
    Cart.remove(id);
    items = items.filter(item => item.id !== id);
    message.textContent = '';
    draw();
  };
  start.oninput = end.oninput = () => {
    if (submitting || created) return;
    sessionStorage.setItem('rentRange', JSON.stringify({ start: start.value, end: end.value }));
    message.textContent = '';
    summarize();
  };

  create.onclick = async () => {
    if (submitting || created || create.disabled) return;
    if (!estimateDays(start.value, end.value) || new Date(start.value) <= new Date()) {
      show('Chọn thời gian nhận trong tương lai và trả sau khi nhận.');
      return;
    }
    submitting = true;
    setLoading(create, true);
    try {
      const order = normRental(await Api.createRental({
        vehicleIds: items.map(item => item.id),
        startDatetime: new Date(start.value).toISOString(),
        endDatetime: new Date(end.value).toISOString(),
        note: $('#note').value.trim() || null
      }));
      created = true;
      Cart.clear();
      sessionStorage.removeItem('rentRange');
      $('#st1').className = $('#st2').className = 's done';
      location.assign('payment.html?orderId=' + encodeURIComponent(order.id));
    } catch (error) {
      show(error.status === 409 ? 'Xe không còn khả dụng hoặc lịch thuê bị trùng. ' + error.message : error.message);
    } finally {
      submitting = false;
      if (!created) { setLoading(create, false); summarize(); }
    }
  };

  $('#items').innerHTML = '<div class="skeleton" aria-label="Đang tải xe"></div>';
  const results = await Promise.allSettled(Cart.get().map(item => Api.vehicle(item.id).then(normVehicle)));
  items = results.filter(result => result.status === 'fulfilled').map(result => result.value);
  draw();
  if (results.some(result => result.status === 'rejected')) {
    create.disabled = true;
    show('Không tải được đầy đủ xe trong giỏ. Hãy tải lại trang hoặc xóa giỏ để chọn lại.');
    const reset = document.createElement('button');
    reset.className = 'btn btn-outline';
    reset.textContent = 'Xóa giỏ';
    reset.onclick = () => { Cart.clear(); location.reload(); };
    message.append(reset);
    create.onclick = null;
  }
});

