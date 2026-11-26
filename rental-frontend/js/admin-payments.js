function pagePayments() {
  shell('Thanh toán & doanh thu');
  const methodNames = {
    CASH: 'Tiền mặt', BANK_TRANSFER: 'Chuyển khoản', MOMO: 'MoMo',
    VNPAY: 'VNPay', CARD: 'Thẻ', OTHER: 'Khác'
  };
  const methods = Object.keys(methodNames);
  const statuses = Object.keys(PAYMENT_STATUS);
  const options = (values, name) => values.map(value =>
    '<option value="' + value + '">' + name(value) + '</option>').join('');
  const paymentsPath = '/api/admin/payments';
  const content = $('#adm-content');
  content.classList.add('compact-admin');
  content.innerHTML = `
    <div id="paymentStats" class="stats" aria-live="polite"></div>
    <section id="paymentRevenue" class="panel" aria-labelledby="revenueTitle"></section>
    <div class="panel">
      <div class="search-bar" id="paymentTabs">
        <button class="btn btn-outline" data-view="transfers">Yêu cầu xác nhận <span id="paymentRequestCount" class="badge orange" hidden style="display:none" aria-live="polite"></span></button>
        <button class="btn btn-outline" data-view="pending">Chờ thanh toán</button>
        <button class="btn btn-outline" data-view="paid">Đơn đã thanh toán</button>
        <button class="btn btn-outline" data-view="transactions">Giao dịch</button>
      </div>
      <form id="paymentFilters" class="search-bar"></form>
      <div id="paymentFeedback" role="status"></div>
      <div id="paymentRows"></div>
      <div id="paymentPager" class="search-bar"></div>
    </div>
    <dialog id="paymentDialog" class="panel" aria-labelledby="paymentDialogTitle"
      style="margin:auto;width:min(560px,92vw);max-height:90vh;overflow:auto"></dialog>`;
  const filters = $('#paymentFilters');
  const rows = $('#paymentRows');
  const pager = $('#paymentPager');
  const dialog = $('#paymentDialog');
  const refreshRevenue = mountPaymentRevenue($('#paymentRevenue'), paymentsPath, methodNames);
  let view = 'transfers';
  let page = 1;
  let generation = 0;
  let busy = false;
  let items = [];

  async function refreshSummary() {
    try {
      const stats = await request('GET', paymentsPath + '/summary', undefined, true);
      $('#paymentStats').innerHTML = [
        ['Doanh thu toàn thời gian', fmtMoney(stats.revenue)], ['Đã thanh toán', stats.paidOrders],
        ['Chờ xử lý', stats.awaitingReview + stats.awaitingPayment]
      ].map(([label, value]) => '<div class="stat"><span>' + label + '</span><strong>' + value + '</strong></div>').join('');
    } catch (error) {
      $('#paymentStats').innerHTML = errBox(error);
    }
  }

  const transactionView = () => view === 'paid' || view === 'transactions';
  const action = (name, id, label) =>
    '<button type="button" class="btn btn-outline btn-sm" data-action="' + name +
    '" data-id="' + esc(id) + '">' + label + '</button>';
  const vietnamTime = new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Ho_Chi_Minh'
  });
  const date = value => value ? vietnamTime.format(new Date(value.endsWith('Z') || /[+-]\d\d:\d\d$/.test(value)
    ? value : value + 'Z')) : '—';
  const dateBound = (value, nextDay) => {
    const instant = new Date(value + 'T00:00:00+07:00');
    if (nextDay) instant.setUTCDate(instant.getUTCDate() + 1);
    return instant.toISOString();
  };

  function selectView(next, search = '') {
    view = next;
    for (const button of $('#paymentTabs').querySelectorAll('button')) {
      const selected = button.dataset.view === view;
      button.classList.toggle('btn-coral', selected);
      button.setAttribute('aria-pressed', String(selected));
    }
    filters.innerHTML = `
      <input class="input" name="search" maxlength="100" aria-label="Tìm kiếm"
        placeholder="${transactionView() ? 'Mã giao dịch, mã đơn hoặc khách hàng' : 'Mã đơn hoặc khách hàng'}">
      ${transactionView() ? `
        <label>Từ ngày <input class="input" type="date" name="from"></label>
        <label>Đến ngày <input class="input" type="date" name="before"></label>
        ${view === 'transactions' ? `<select class="select" name="status" aria-label="Trạng thái">
          <option value="">Mọi trạng thái</option>${options(statuses, value => PAYMENT_STATUS[value][0])}</select>` : ''}
        <select class="select" name="method" aria-label="Phương thức">
          <option value="">Mọi phương thức</option>${options(methods, value => methodNames[value])}</select>` : ''}
      <button class="btn btn-outline" type="submit">Lọc</button>`;
    filters.elements.search.value = search;
    load(1);
  }

  async function load(nextPage) {
    const ticket = ++generation;
    page = nextPage;
    items = [];
    pager.replaceChildren();
    rows.innerHTML = '<div class="skeleton" aria-label="Đang tải dữ liệu"></div>';
    const params = new URLSearchParams();
    for (const [key, value] of new FormData(filters)) {
      if (value) params.set(key, ['from', 'before'].includes(key) ? dateBound(value, key === 'before') : value);
    }
    params.set('page', page);
    if (view === 'pending') params.set('awaitingProcessing', 'true');
    if (view === 'paid') params.set('status', 'PAID');

    try {
      const data = await request('GET', (view === 'transfers' ? paymentsPath + '/transfer-requests' : transactionView() ? paymentsPath : orderPath) + '?' + params, undefined, true);
      if (ticket !== generation) return;
      const pages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
      if (page > pages) return load(pages);
      items = data.items;
      rows.innerHTML = view === 'transfers' ? tableHtml([
        ['Mã đơn', item => esc(item.orderCode)],
        ['Khách hàng', item => esc(item.customerName)],
        ['Liên hệ', item => esc(item.customerPhone ?? '—') + '<br>' + esc(item.customerEmail ?? '—')],
        ['Tổng tiền', item => fmtMoney(item.totalAmount)],
        ['Trạng thái đơn', item => badge(RENTAL_STATUS, item.orderStatus)],
        ['Yêu cầu', item => esc(({ PENDING: 'Chờ xác nhận', APPROVED: 'Đã xác nhận',
          REJECTED: 'Đã từ chối', CANCELLED: 'Đã đóng' })[item.status])],
        ['Gửi lúc', item => date(item.submittedAt)],
        ['Xử lý lúc', item => date(item.reviewedAt)],
        ['Phương thức', item => esc(methodNames[item.paymentMethod])],
        ['', item => item.status === 'PENDING' ?
          (['PENDING', 'CONFIRMED'].includes(item.orderStatus) ? action('transfer-pay', item.id, 'Xác nhận thanh toán') : '') +
          ' ' + action('transfer-reject', item.id, 'Từ chối yêu cầu') : '']
      ], items) : transactionView() ? tableHtml([
        ['Mã đơn', item => esc(item.orderCode)],
        ['Khách hàng', item => esc(item.customerName)],
        ['Liên hệ', item => esc(item.customerPhone ?? '—') + '<br>' + esc(item.customerEmail ?? '—')],
        ['Mã giao dịch', item => esc(item.transactionCode)],
        ['Số tiền', item => fmtMoney(item.amount)],
        ['Phương thức', item => esc(methodNames[enumName(methods, item.paymentMethod)])],
        ['Trạng thái', item => badge(PAYMENT_STATUS, enumName(statuses, item.status))],
        ['Thời điểm', item => date(item.paidAt || item.createdAt)],
        ['', item => action('details', item.id, 'Chi tiết')]
      ], items) : tableHtml([
        ['Mã đơn', item => esc(item.orderCode)],
        ['Khách hàng', item => esc(item.customerName)],
        ['Liên hệ', item => esc(item.customerPhone ?? '—') + '<br>' + esc(item.customerEmail ?? '—')],
        ['Phương thức', item => esc(methodNames[item.pendingPaymentMethod] ?? '—')],
        ['Nhận xe', item => date(item.startDatetime)],
        ['Trả xe', item => date(item.endDatetime)],
        ['Tổng tiền', item => fmtMoney(item.totalAmount)],
        ['Trạng thái', item => badge(RENTAL_STATUS, enumName(Enum.rental, item.status))],
        ['', item => item.pendingPaymentRequestId ?
          action('confirm-pending', item.id, 'Xác nhận thanh toán') : '<span class="muted">Chưa gửi yêu cầu xác nhận</span>']
      ], items);
      renderPager(pager, page, pages, data.totalCount, load);
    } catch (error) {
      if (ticket === generation) rows.innerHTML = errBox(error);
    }
  }

  function show(title, body) {
    dialog.innerHTML = `<h2 id="paymentDialogTitle">${title}</h2>${body}
      <div id="paymentDialogError" role="alert"></div>
      <button type="button" class="btn btn-outline" id="paymentClose">Đóng</button>`;
    $('#paymentClose').onclick = () => { if (!busy) dialog.close(); };
    if (!dialog.open) dialog.showModal();
  }

  async function save(button, path, body, headers, message) {
    if (busy) return;
    busy = true;
    setLoading(button, true);
    $('#paymentClose').disabled = true;
    $('#paymentDialogError').replaceChildren();
    try {
      await request('POST', path, body, true, headers);
      dialog.close();
      $('#paymentFeedback').textContent = message;
      await Promise.all([refreshSummary(), refreshRevenue(), load(page), refreshAdminPaymentRequests()]);
    } catch (error) {
      $('#paymentDialogError').innerHTML = errBox(error);
    } finally {
      busy = false;
      setLoading(button, false);
      $('#paymentClose').disabled = false;
    }
  }

  function confirmPayment(order, transferRequestId) {
    const paymentMethod = order.paymentMethod || order.pendingPaymentMethod;
    if (!transferRequestId || !['CASH', 'BANK_TRANSFER'].includes(paymentMethod)) return;
    const key = crypto.randomUUID();
    show('Xác nhận thanh toán', `
      <p>Đơn ${esc(order.orderCode)} · ${esc(order.customerName)}</p>
      <p>Số tiền theo đơn <strong>${fmtMoney(order.totalAmount)}</strong></p>
      <p>Phương thức <strong>${methodNames[paymentMethod]}</strong></p>
      <form id="paymentEntry">
        <label class="payment-funds-check">
          <input name="fundsVerified" type="checkbox" required>
          <span>Tôi đã đối chiếu và nhận đủ tiền.</span>
        </label>
        <button class="btn btn-coral" type="submit" disabled>Xác nhận thanh toán</button>
      </form>`);
    const form = $('#paymentEntry');
    const fundsVerified = form.elements.fundsVerified;
    const commit = form.querySelector('button[type="submit"]');
    fundsVerified.onchange = () => {
      if (!busy) commit.disabled = !fundsVerified.checked;
    };
    form.onsubmit = event => {
      event.preventDefault();
      if (busy || !fundsVerified.checked) return;
      const body = { amount: order.totalAmount, paymentMethod, fundsVerified: true, transferRequestId };
      fundsVerified.disabled = true;
      save(commit, itemPath(orderPath, order.id) + '/payments/confirm-manual', body,
        { 'Idempotency-Key': key }, 'Đã ghi nhận thanh toán.').finally(() => {
        fundsVerified.disabled = false;
      });
    };
  }

  function rejectTransfer(item) {
    show('Từ chối yêu cầu xác nhận', '<p>Đơn ' + esc(item.orderCode) + '</p>' +
      '<form id="transferReject"><label>Lý do <textarea class="input" name="reason" required maxlength="500"></textarea></label>' +
      '<button class="btn btn-coral" type="submit">Từ chối yêu cầu</button></form>');
    $('#transferReject').onsubmit = event => {
      event.preventDefault();
      const reason = event.currentTarget.elements.reason.value.trim();
      if (!reason) {
        $('#paymentDialogError').textContent = 'Nhập lý do từ chối.';
        return;
      }
      save(event.submitter, paymentsPath + '/transfer-requests/' + item.id + '/reject',
        { reason }, {}, 'Đã từ chối yêu cầu xác nhận.');
    };
  }

  function showDetails(payment) {
    show('Chi tiết giao dịch', '<dl>' + [
      ['Mã giao dịch', payment.transactionCode], ['Mã đơn', payment.orderCode],
      ['Trạng thái đơn', RENTAL_STATUS[enumName(Enum.rental, payment.orderStatus)]?.[0]],
      ['Khách hàng', payment.customerName], ['Email', payment.customerEmail],
      ['Số tiền', fmtMoney(payment.amount)], ['Phương thức', methodNames[enumName(methods, payment.paymentMethod)]],
      ['Trạng thái giao dịch', PAYMENT_STATUS[enumName(statuses, payment.status)]?.[0]],
      ['Đã thanh toán lúc', date(payment.paidAt)], ['Tạo lúc', date(payment.createdAt)],
      ['Điện thoại', payment.customerPhone],
      ['Mã tham chiếu', payment.externalReference]
    ].map(([label, value]) => '<dt><strong>' + label + '</strong></dt><dd>' +
      esc(value ?? '—') + '</dd>').join('') + '</dl>');
  }

  dialog.addEventListener('cancel', event => { if (busy) event.preventDefault(); });
  rows.onclick = event => {
    const button = event.target.closest('[data-action]');
    if (!button || busy) return;
    const item = items.find(row => String(row.id) === button.dataset.id);
    if (!item) return;
    switch (button.dataset.action) {
      case 'transfer-pay': confirmPayment({ ...item, id: item.orderId }, item.id); break;
      case 'transfer-reject': rejectTransfer(item); break;
      case 'confirm-pending': confirmPayment(item, item.pendingPaymentRequestId); break;
      case 'details': showDetails(item); break;
    }
  };
  $('#paymentTabs').onclick = event => {
    const button = event.target.closest('[data-view]');
    if (button) selectView(button.dataset.view);
  };
  filters.onsubmit = event => { event.preventDefault(); load(1); };
  let lastRequestCount = null;
  const onRequestCount = event => {
    const count = event.detail;
    const badge = $('#paymentRequestCount');
    badge.textContent = count > 0 ? String(count) : '';
    badge.hidden = count === 0;
    // Avoid the .badge display rule overriding the hidden attribute.
    badge.style.display = count > 0 ? '' : 'none';
    if (lastRequestCount !== null && lastRequestCount !== count && !busy && !dialog.open &&
        (view === 'transfers' || view === 'pending')) load(page);
    lastRequestCount = count;
  };
  window.addEventListener('rentgo:payment-request-count', onRequestCount);
  selectView(new URLSearchParams(location.search).get('view') === 'transfers' ? 'transfers' : 'pending');
  refreshSummary();
  refreshAdminPaymentRequests();
}
