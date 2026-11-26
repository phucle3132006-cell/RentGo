function mountPaymentRevenue(root, path, methodNames) {
  root.innerHTML = `
    <h2 id="revenueTitle">Thống kê doanh thu</h2>
    <form class="search-bar">
      <label>Kỳ thống kê <select class="select" name="period">
        <option value="day">Theo ngày</option>
        <option value="month">Theo tháng</option>
        <option value="year">Theo năm</option>
        <option value="custom">Khoảng tùy chọn</option>
      </select></label>
      <span class="revenue-inputs"></span>
      <button class="btn btn-coral" type="submit">Thống kê</button>
      <button class="btn btn-outline" type="reset">Đặt lại</button>
    </form>
    <p class="revenue-status" role="status"></p>
    <div class="revenue-results"></div>`;

  const form = root.querySelector('form');
  const inputs = root.querySelector('.revenue-inputs');
  const status = root.querySelector('.revenue-status');
  const results = root.querySelector('.revenue-results');
  const period = form.elements.period;
  const todayParts = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit'
  }).formatToParts(new Date());
  const getPart = type => todayParts.find(part => part.type === type).value;
  const year = getPart('year');
  const month = getPart('month');
  const today = year + '-' + month + '-' + getPart('day');
  const money = new Intl.NumberFormat('vi-VN', {
    style: 'currency', currency: 'VND', maximumFractionDigits: 2
  });
  const percent = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });
  const localDate = iso => iso.split('-').reverse().join('/');
  const colors = ['#b74746', '#245d9c', '#17745e', '#805baa', '#9a5b14', '#52617c'];
  const months = Array.from({ length: 12 }, (_, index) => index + 1);
  let applied = new URLSearchParams({ period: 'day', date: today });
  let generation = 0;

  function displayFields() {
    const yearField = `<label>Năm <input class="input" name="year" type="number"
      min="3" max="9998" step="1" value="${year}" required></label>`;
    const dateField = (name, label, value) => `<label>${label}
      <input class="input" name="${name}" type="date" max="9999-12-30" value="${value}" required></label>`;
    switch (period.value) {
      case 'month':
        inputs.innerHTML = `<label>Tháng <select class="select" name="month">
          ${months.map(value => `<option value="${value}" ${value === Number(month) ? 'selected' : ''}>${value}</option>`).join('')}
        </select></label>` + yearField;
        break;
      case 'year': inputs.innerHTML = yearField; break;
      case 'custom': inputs.innerHTML = dateField('from', 'Từ ngày', year + '-' + month + '-01') +
        dateField('to', 'Đến ngày', today); break;
      default: inputs.innerHTML = dateField('date', 'Ngày', today);
    }
  }

  function render(data) {
    const total = data.totalRevenue;
    const cards = [
      ['Tổng doanh thu', money.format(total)],
      ['Đơn đã thanh toán', data.paidOrderCount],
      ['Giao dịch thành công', data.successfulPaymentCount],
      ['Tiền mặt', money.format(data.cashRevenue)],
      ['Chuyển khoản', money.format(data.bankTransferRevenue)]
    ];
    const growth = data.growthRate;
    const trend = growth === null ? 'Kỳ trước có doanh thu bằng 0' :
      growth > 0 ? 'Tăng ' + percent.format(growth) + '%' :
      growth < 0 ? 'Giảm ' + percent.format(-growth) + '%' : 'Không đổi (0%)';
    const peak = Math.max(0, ...data.revenueData.map(point => point.revenue));
    let cumulative = 0;
    const slices = data.paymentMethods.map((item, index) => {
      const start = cumulative;
      cumulative += total ? item.revenue / total * 100 : 0;
      return { ...item, start, end: cumulative, color: colors[index % colors.length] };
    });
    const gradient = total > 0 ? 'conic-gradient(' + slices.map(slice =>
      `${slice.color} ${slice.start}% ${slice.end}%`).join(',') + ')' : '#e7eaf0';
    results.innerHTML = `
      <div class="stats">${cards.map(([label, value]) =>
        `<div class="stat"><span>${label}</span><strong>${esc(value)}</strong></div>`).join('')}</div>
      <p>${localDate(data.startDate)} – ${localDate(data.endDate)} ·
        <strong class="${growth > 0 ? 'revenue-up' : growth < 0 ? 'revenue-down' : ''}">${trend}</strong>
        <span> · Kỳ trước (${localDate(data.previousStartDate)} – ${localDate(data.previousEndDate)}):
          ${money.format(data.previousRevenue)}</span></p>
      <div class="revenue-charts">
        <div><h3>Doanh thu theo thời gian</h3>
          <div class="revenue-scroll" tabindex="0" aria-label="Biểu đồ cột doanh thu">
            <ul class="revenue-bars">${data.revenueData.map(point => {
              const label = esc(point.label);
              const caption = label + ': ' + money.format(point.revenue);
              const height = peak ? Math.max(0, point.revenue / peak * 100) : 0;
              return `<li title="${caption}"><span class="revenue-bar-track" aria-hidden="true">
                <i style="height:${height}%"></i></span><span aria-hidden="true">${label}</span>
                <span class="revenue-sr">${caption}</span></li>`;
            }).join('')}</ul>
          </div>
        </div>
        <div><h3>Phương thức thanh toán</h3>
          <div class="revenue-donut" style="background:${gradient}" aria-hidden="true"></div>
          <ul class="revenue-legend">${slices.map(item =>
            `<li><i style="background:${item.color}" aria-hidden="true"></i>
              ${esc(methodNames[item.method] ?? item.method)} · ${money.format(item.revenue)}
              (${percent.format(item.end - item.start)}%)</li>`).join('')}</ul>
        </div>
      </div>`;
  }

  async function load() {
    const ticket = ++generation;
    status.textContent = 'Đang tải thống kê…';
    results.replaceChildren();
    try {
      const data = await request('GET', path + '/revenue?' + applied, undefined, true);
      if (ticket !== generation) return;
      render(data);
      status.textContent = data.successfulPaymentCount ? '' : 'Không có giao dịch thành công trong kỳ này.';
    } catch (error) {
      if (ticket === generation) status.textContent = 'Không tải được thống kê. ' + (error.message || '');
    }
  }

  period.addEventListener('change', displayFields);
  form.addEventListener('submit', event => {
    event.preventDefault();
    const params = new URLSearchParams(new FormData(form));
    if (period.value === 'custom' && params.get('from') > params.get('to')) {
      ++generation;
      results.replaceChildren();
      status.textContent = 'Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.';
      return;
    }
    applied = params;
    load();
  });
  form.addEventListener('reset', event => {
    event.preventDefault();
    period.value = 'day';
    displayFields();
    applied = new URLSearchParams({ period: 'day', date: today });
    load();
  });
  displayFields();
  load();
  return load;
}
