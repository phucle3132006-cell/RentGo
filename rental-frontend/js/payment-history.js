document.addEventListener('DOMContentLoaded', async () => {
  if (!requireRoles(['CUSTOMER'])) return;

  const history = $('#paymentHistory');
  const status = $('#paymentStatus');
  const pages = $('#paymentPages');
  const id = getParam('id');
  const renderPayment = (raw, detail = false) => {
    const payment = normPayment(raw);
    return `<article class="panel">
      <h2>Đơn ${esc(payment.orderCode)}</h2>
      <div class="spec-table">
        <div><span>Số tiền</span><b>${fmtMoney(payment.amount)}</b></div>
        <div><span>Phương thức</span><b>${esc(PAYMENT_METHOD[payment.method] || payment.method)}</b></div>
        <div><span>Mã giao dịch</span><b>${esc(payment.code)}</b></div>
        <div><span>Thời gian thanh toán</span><b>${fmtDateTime(payment.paidAt)}</b></div>
        ${detail && raw.externalReference ? `<div><span>Mã tham chiếu</span><b>${esc(raw.externalReference)}</b></div>` : ''}
      </div>
      ${detail ? '' : `<a class="btn btn-outline btn-sm" href="?id=${encodeURIComponent(payment.id)}">Chi tiết giao dịch</a>`}
    </article>`;
  };

  if (id !== null) {
    history.innerHTML = '<a class="btn btn-outline" href="payment-history.html">Về lịch sử thanh toán</a>';
    status.textContent = 'Đang tải giao dịch…';
    try {
      history.insertAdjacentHTML('beforeend', renderPayment(await Api.myPayment(id), true));
      status.textContent = '';
    } catch (error) { status.textContent = error.message; }
    return;
  }

  async function load(page) {
    history.innerHTML = pages.innerHTML = '';
    status.textContent = 'Đang tải lịch sử…';
    try {
      const result = await Api.myPayments(page);
      status.textContent = result.items.length ? '' : 'Chưa có giao dịch thanh toán thành công.';
      history.innerHTML = result.items.map(payment => renderPayment(payment)).join('');
      renderPager(pages, page, result.hasNext ? null : page, null, load);
    } catch (error) { status.textContent = error.message; }
  }

  load(1);
});
