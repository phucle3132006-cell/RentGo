/* Trang chủ */
document.addEventListener('DOMContentLoaded', async () => {
  let category = '';
  const refreshSignup = () => document.querySelector("#signupCta").classList.toggle("hidden", Auth.isLoggedIn());
  refreshSignup();
  window.addEventListener('pageshow', refreshSignup);
  const pills = [['', 'Tất cả', ''], ['OTO', 'Ô tô', 'car'], ['XE_SO', 'Xe số', 'bike'], ['XE_GA', 'Xe ga', 'bike'], ['XE_DIEN', 'Xe điện', 'bolt']];
  $('#catPills').innerHTML = pills.map(p => `<button class="pill ${p[0] === '' ? 'active' : ''}" data-c="${p[0]}">${p[2] ? icon(p[2]) : ''}${p[1]}</button>`).join('');
  $('#catPills').onclick = e => {
    const b = e.target.closest('.pill'); if (!b) return;
    category = b.dataset.c; $$('#catPills .pill').forEach(x => x.classList.toggle('active', x === b)); fillOptions();
  };
  $$('.tab-btn').forEach(b => b.onclick = () => {
    $$('.tab-btn').forEach(x => x.classList.toggle('active', x === b));
    ['t1', 't2', 't3'].forEach(id => $('#' + id).classList.toggle('hidden', id !== b.dataset.tab));
  });

  $('#catTiles').innerHTML = Object.entries(CATEGORY).map(([k, c]) => `<a class="tile" href="vehicles.html?category=${k}"><span class="ic">${icon(c.icon, 'icon-lg')}</span>${c.label}</a>`).join('');
  const popular = [
    ['Ô tô', 'car', 'category=OTO'], ['Xe số', 'bike', 'category=XE_SO'], ['Xe ga', 'bike', 'category=XE_GA'], ['Xe điện', 'bolt', 'category=XE_DIEN'], ['Dưới 200.000₫', 'tag', 'maxPrice=200000'],
    ['Ô tô 5 chỗ', 'car', 'category=OTO&seats=5'], ['Ô tô 7 chỗ', 'car', 'category=OTO&seats=7'], ['Giá tốt nhất', 'tag', 'sort=price_asc'], ['Dưới 500.000₫', 'tag', 'maxPrice=500000'], ['Sẵn sàng ngay', 'check', 'status=AVAILABLE']
  ];
  $('#popular').innerHTML = popular.map(p => `<a class="tile" href="vehicles.html?${p[2]}"><span class="ic">${icon(p[1], 'icon-lg')}</span>${p[0]}</a>`).join('');

  let fleet = [], models = [];
  function fillOptions() {
    const cars = category === 'OTO';
    $('#brandField').classList.remove('hidden');
    $('#modelField').classList.remove('hidden');
    $('#seatsField').classList.toggle('hidden', !cars);
    const list = models.filter(model => !category || model.category === category);
    $('#selSeats').innerHTML = options('Mọi số chỗ', list.filter(model => model.category === 'OTO').map(model => model.seats), (a, b) => a - b);
    const selectedBrand = $('#selBrand').value;
    $('#selBrand').innerHTML = options('Tất cả hãng', list.map(model => model.brand));
    if ([...$('#selBrand').options].some(option => option.value === selectedBrand)) $('#selBrand').value = selectedBrand;
    fillModels();
  }
  function fillModels() {
    const brand = $('#selBrand').value;
    $('#selModel').disabled = !brand;
    const matchingModels = models.filter(model =>
      (!category || model.category === category) && model.brand === brand);
    const availableModels = new Set(fleet.filter(vehicle => vehicle.brand === brand).map(vehicle => vehicle.model));
    $('#selModel').innerHTML = '<option value="">Mọi dòng xe</option>' + matchingModels
      .map(model => `<option value="${esc(model.model)}">${esc(model.model)}${availableModels.has(model.model) ? '' : ' (chưa có xe)'}</option>`)
      .filter((option, index, options) => options.indexOf(option) === index).join('');
  }
  $('#selBrand').onchange = fillModels;
  $('#vehicleSearch').onsubmit = event => {
    event.preventDefault();
    const start = $('#searchStart').value, end = $('#searchEnd').value;
    if (!validateSearchRange(start, end, $('#searchError'))) return;
    const params = new URLSearchParams();
    const fields = category === 'OTO'
      ? { brand: $('#selBrand').value, model: $('#selModel').value, seats: $('#selSeats').value }
      : { brand: $('#selBrand').value, model: $('#selModel').value };
    for (const [key, value] of Object.entries({ category, ...fields, start, end, maxPrice: $('#selPrice').value })) {
      if (value) params.set(key, value);
    }
    location.href = 'vehicles.html?' + params;
  };

  try {
    [fleet, models] = await Promise.all([searchFleet(), Api.vehicleModels().then(toList).then(items => items.map(normModel))]);
    fillOptions();
    const brands = [...new Set(models.map(model => model.brand).filter(Boolean))].sort();
    $('#brandChips').innerHTML = brands.map(brand =>
      `<a class="chip" href="vehicles.html?brand=${encodeURIComponent(brand)}">${esc(brand)}</a>`).join('');
  } catch (e) { toast(e.message, 'err'); }

  try {
    const list = toList(await Api.vehicles({ status: 'AVAILABLE' })).map(normVehicle).slice(0, 8);
    $('#featured').innerHTML = list.length ? list.map(vehicle => vehicleCard(vehicle)).join('') : '<div class="empty" style="grid-column:1/-1"><h3>Chưa có xe nào</h3></div>';
  } catch (e) { $('#featured').innerHTML = `<div class="empty" style="grid-column:1/-1"><h3>Không tải được danh sách xe</h3><p>${esc(e.message)}</p></div>`; }
});
