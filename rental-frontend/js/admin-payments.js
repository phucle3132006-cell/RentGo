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
    <div class="panel">
      <div class="search-bar" id="paymentTabs">
        <button class="btn btn-outline" data-view="transfers">Yêu cầu xác nhận</button>
        <button class="btn btn-outline" data-view="pending">Chờ xử lý</button>
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
  let view = 'pending';
  let page = 1;
  let generation = 0;
  let busy = false;
  let items = [];

  async function refreshSummary() {
    try {
      const stats = await request('GET', paymentsPath + '/summary', undefined, true);
      $('#paymentStats').innerHTML = [
        ['Tổng doanh thu', fmtMoney(stats.revenue)], ['Đã thanh toán', stats.paidOrders],
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

  function selectView(next) {
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
          (item.orderStatus === 'CONFIRMED' ? action('transfer-pay', item.id, 'Xác nhận thanh toán') : '') +
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
        ['', item => enumName(Enum.rental, item.status) === 'PENDING' ? action('approve', item.id, 'Duyệt') + ' ' +
          action('reject', item.id, 'Từ chối') : action('pay', item.id, 'Xác nhận thanh toán')]
      ], items);
      pager.innerHTML = `<button type="button" class="btn btn-outline" id="paymentPrev">Trước</button>
        <span>Trang ${page} / ${pages}</span><span>${data.totalCount} kết quả</span>
        <button type="button" class="btn btn-outline" id="paymentNext">Sau</button>`;
      $('#paymentPrev').disabled = page <= 1;
      $('#paymentNext').disabled = page >= pages;
      $('#paymentPrev').onclick = () => load(page - 1);
      $('#paymentNext').onclick = () => load(page + 1);
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
      await Promise.all([refreshSummary(), load(page)]);
    } catch (error) {
      $('#paymentDialogError').innerHTML = errBox(error);
    } finally {
      busy = false;
      setLoading(button, false);
      $('#paymentClose').disabled = false;
    }
  }

  function review(order, approve) {
    show(approve ? 'Duyệt đơn' : 'Từ chối đơn', `
      <p>Đơn ${esc(order.orderCode)} · ${esc(order.customerName)} · ${fmtMoney(order.totalAmount)}</p>
      <form id="paymentReview">
        ${approve ? '<p>Đơn sẽ chuyển sang chờ thanh toán.</p>' :
          '<label>Lý do từ chối <textarea class="input" name="reason" required maxlength="500"></textarea></label>'}
        <button class="btn btn-coral" type="submit">${approve ? 'Duyệt đơn' : 'Từ chối đơn'}</button>
      </form>`);
    $('#paymentReview').onsubmit = event => {
      event.preventDefault();
      const reason = new FormData(event.currentTarget).get('reason')?.trim() || null;
      if (!approve && !reason) {
        $('#paymentDialogError').textContent = 'Nhập lý do từ chối.';
        return;
      }
      save(event.submitter, itemPath(orderPath, order.id) + '/review', { approve, reason }, {},
        approve ? 'Đã duyệt đơn.' : 'Đã từ chối đơn.');
    };
  }

  function confirmPayment(order, transferRequestId = null) {
    let body, key, previous;
    show('Xác nhận thanh toán', `
      <p>Đơn ${esc(order.orderCode)} · ${esc(order.customerName)}</p>
      <p>Số tiền theo đơn <strong>${fmtMoney(order.totalAmount)}</strong></p>
      <form id="paymentEntry">
        <label>Phương thức <select class="select" name="paymentMethod">
          ${options(order.paymentMethod || order.pendingPaymentMethod ? [order.paymentMethod || order.pendingPaymentMethod] : ['CASH', 'BANK_TRANSFER'], value => methodNames[value])}</select></label>
        <label><input name="fundsVerified" type="checkbox" required> Tôi đã đối chiếu và nhận đủ tiền.</label>
        <button class="btn btn-coral" type="submit">Kiểm tra thông tin</button>
      </form>
      <section id="paymentConfirm" hidden>
        <p id="paymentConfirmation"></p>
        <button type="button" class="btn btn-outline" id="paymentBack">Quay lại</button>
        <button type="button" class="btn btn-coral" id="paymentCommit">Ghi nhận đã thanh toán</button>
      </section>`);
    const form = $('#paymentEntry');
    const step = $('#paymentConfirm');
    const method = form.elements.paymentMethod;
    form.onsubmit = event => {
      event.preventDefault();
      const amount = order.totalAmount;
      body = { amount, paymentMethod: method.value,
        fundsVerified: form.elements.fundsVerified.checked, transferRequestId };
      const serialized = JSON.stringify(body);
      if (serialized !== previous) {
        key = crypto.randomUUID();
        previous = serialized;
      }
      $('#paymentConfirmation').textContent = 'Đã nhận ' + fmtMoney(amount) + ' qua ' +
        methodNames[body.paymentMethod] + '.';
      $('#paymentDialogError').replaceChildren();
      form.hidden = true;
      step.hidden = false;
      $('#paymentCommit').focus();
    };
    $('#paymentBack').onclick = () => {
      if (busy) return;
      step.hidden = true;
      form.hidden = false;
      $('#paymentDialogError').replaceChildren();
      method.focus();
    };
    $('#paymentCommit').onclick = event => save(event.currentTarget,
      itemPath(orderPath, order.id) + '/payments/confirm-manual', body,
      { 'Idempotency-Key': key }, 'Đã ghi nhận thanh toán.');
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
      case 'approve': review(item, true); break;
      case 'reject': review(item, false); break;
      case 'transfer-pay': confirmPayment({ ...item, id: item.orderId }, item.id); break;
      case 'transfer-reject': rejectTransfer(item); break;
      case 'pay': confirmPayment(item); break;
      case 'details': showDetails(item); break;
    }
  };
  $('#paymentTabs').onclick = event => {
    const button = event.target.closest('[data-view]');
    if (button) selectView(button.dataset.view);
  };
  filters.onsubmit = event => { event.preventDefault(); load(1); };
  selectView('pending');
  refreshSummary();
}
