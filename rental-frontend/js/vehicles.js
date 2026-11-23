document.addEventListener('DOMContentLoaded', async () => {
  const PER_PAGE = 9;
  let all = [], fleet = [], page = 1, requestId = 0, range;
  const url = new URLSearchParams(location.search);
  const fields = ['category', 'brand', 'model', 'seats', 'maxPrice', 'start', 'end', 'status', 'q', 'sort', 'group'];
  const state = Object.fromEntries(fields.map(key => [key, url.get(key) || '']));

  function fillOptions() {
    const list = fleet.filter(vehicle =>
      (!state.category || vehicle.category === state.category) &&
      (state.group !== 'bike' || vehicle.category !== 'OTO'));
    const brands = list.map(vehicle => vehicle.brand);
    const seats = list.filter(vehicle => vehicle.category === 'OTO').map(vehicle => vehicle.seats);
    $('#fBrand').innerHTML = options('Tất cả hãng', brands);
    $('#fModel').disabled = !state.brand;
    $('#fModel').innerHTML = options(state.brand ? 'Tất cả dòng xe' : 'Chọn hãng trước',
      list.filter(vehicle => vehicle.brand === state.brand).map(vehicle => vehicle.model));
    $('#fModel').value = state.model;
    $('#fSeats').innerHTML = options('Mọi số chỗ', [...seats, Number(state.seats)], (a, b) => a - b);
    $('#fBrand').value = state.brand;
    $('#fSeats').value = state.seats;
    $('#fSeatsGroup').classList.toggle('hidden', !!state.category && state.category !== 'OTO' || state.group === 'bike');
  }

  function syncInputs() {
    $('#fQ').value = state.q;
    $('#fPrice').value = state.maxPrice;
    $('#fStart').value = state.start;
    $('#fEnd').value = state.end;
    $('#fAvail').checked = state.status === 'AVAILABLE';
    $('#fSort').value = state.sort;
    $$('input[name=cat]').forEach(radio => radio.checked = radio.value === state.category);
    fillOptions();
  }

  function filtered() {
    const term = state.q.toLowerCase();
    const list = all.filter(vehicle =>
      (state.group !== 'bike' || vehicle.category !== 'OTO') &&
      `${vehicle.name} ${vehicle.plate} ${vehicle.color}`.toLowerCase().includes(term));
    if (state.sort === 'price_asc') list.sort((a, b) => a.price - b.price);
    if (state.sort === 'price_desc') list.sort((a, b) => b.price - a.price);
    if (state.sort === 'year_desc') list.sort((a, b) => (b.year || 0) - (a.year || 0));
    return list;
  }

  function render() {
    const l = filtered(); const pages = Math.max(1, Math.ceil(l.length / PER_PAGE)); if (page > pages) page = pages;
    $('#count').textContent = l.length + ' xe phù hợp';
    const t = state.category ? (CATEGORY[state.category]?.label || 'xe') : (state.group === 'bike' ? 'Xe máy' : 'xe');
    $('#pageTitle').textContent = (state.category || state.group ? 'Thuê ' : 'Tất cả ') + (state.category || state.group ? t.toLowerCase() : 'xe cho thuê');
    $('#grid').innerHTML = l.length ? l.slice((page - 1) * PER_PAGE, page * PER_PAGE).map(vehicle => vehicleCard(vehicle, range)).join('')
      : '<div class="empty" style="grid-column:1/-1"><h3>Không tìm thấy xe phù hợp</h3><p>Hãy thử thay đổi hoặc xoá bớt bộ lọc.</p></div>';
    $('#pager').innerHTML = pages > 1 ? Array.from({ length: pages }, (_, i) => `<button class="${i + 1 === page ? 'active' : ''}" data-p="${i + 1}">${i + 1}</button>`).join('') : '';
  }
  async function load() {
    const id = ++requestId;
    all = [];
    range = undefined;
    $('#count').textContent = '';
    $('#pager').innerHTML = '';
    $('#grid').innerHTML = '';
    if (!validateSearchRange(state.start, state.end, $('#filterError'))) return;

    const query = new URLSearchParams();
    for (const key of fields) if (state[key]) query.set(key, state[key]);
    history.replaceState(null, '', location.pathname + (query.size ? '?' + query : ''));
    const filters = Object.fromEntries(['category', 'brand', 'model', 'seats', 'maxPrice', 'status']
      .filter(key => state[key]).map(key => [key, state[key]]));
    if (state.start) {
      filters.start = new Date(state.start).toISOString();
      filters.end = new Date(state.end).toISOString();
    }
    $('#grid').innerHTML = '<div class="skeleton"></div><div class="skeleton"></div><div class="skeleton"></div>';
    try {
      const result = await Api.vehicles(filters);
      if (id !== requestId) return;
      all = result.map(normVehicle);
      if (filters.start) range = { start: state.start, end: state.end };
      page = 1;
      render();
    } catch (error) {
      if (id !== requestId) return;
      $('#grid').innerHTML = `<div class="empty"><h3>Không tải được dữ liệu</h3><p>${esc(error.message)}</p></div>`;
    }
  }

  function apply() {
    state.start = $('#fStart').value;
    state.end = $('#fEnd').value;
    load();
  }

  $('#fApply').onclick = apply;
  $('#fCat').onchange = event => {
    Object.assign(state, { category: event.target.value, group: '', brand: '', model: '', seats: '' });
    fillOptions();
    apply();
  };
  $('#fBrand').onchange = event => { state.model = ''; state.brand = event.target.value; apply(); };
  $('#fModel').onchange = event => { state.model = event.target.value; apply(); };
  $('#fSeats').onchange = event => { state.seats = event.target.value; apply(); };
  $('#fPrice').onchange = event => { state.maxPrice = event.target.value; apply(); };
  $('#fAvail').onchange = event => { state.status = event.target.checked ? 'AVAILABLE' : ''; apply(); };
  $('#fSort').onchange = event => { state.sort = event.target.value; apply(); };
  $('#fQ').oninput = event => { state.q = event.target.value.trim(); apply(); };
  $('#fReset').onclick = () => {
    fields.forEach(key => { state[key] = ''; });
    syncInputs();
    load();
  };
  $('#pager').onclick = event => {
    const button = event.target.closest('[data-p]');
    if (button) { page = Number(button.dataset.p); render(); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  };

  syncInputs();
  load();
  try { fleet = await searchFleet(); fillOptions(); }
  catch (error) { toast(error.message, 'err'); }
});
