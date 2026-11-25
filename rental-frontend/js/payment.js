document.addEventListener('DOMContentLoaded', async () => {
  if (!requireRoles(['CUSTOMER'])) return;
  const box = $('#payBox');
  const id = new URLSearchParams(location.search).get('orderId');
  const back = '<a class="btn btn-outline" href="my-orders.html">Về đơn của tôi</a>';
  if (!id) { location.replace('payment-history.html'); return; }

  const path = itemPath(orderPath, id) + '/transfer-requests';
  let instructions;
  let method = "BANK_TRANSFER";
  let busy = false;
  let previous;
  const errorBox = document.createElement('p');
  errorBox.setAttribute('role', 'status');
  box.after(errorBox);

  async function refresh() {
    if (busy || document.hidden) return;
    busy = true;
    try {
      const [raw, { latest }] = await Promise.all([
        Api.rental(id), request('GET', path, undefined, true)
      ]);
      const order = normRental(raw);
      const state = JSON.stringify([order.status, order.total, order.canUseManualPayment, latest, method]);
      if (state === previous) return;
      const payable = order.canUseManualPayment;
      const waiting = latest?.status === 'PENDING';
      const selected = waiting ? latest.paymentMethod : method;
      const showTransfer = payable && selected === 'BANK_TRANSFER' && Number.isSafeInteger(order.total);
      if (showTransfer && instructions?.amount !== String(order.total))
        instructions = await request('GET', path + '/instructions', undefined, true);
      const { bank, account, holder, amount, reference, qrPng } = instructions || {};
      const status = {
        PENDING: 'Yêu cầu xác nhận đang chờ admin xử lý.',
        APPROVED: 'Admin đã xác nhận thanh toán.',
        REJECTED: 'Yêu cầu bị từ chối: ' + (latest?.reason || ''),
        CANCELLED: latest?.reason || 'Yêu cầu đã đóng.'
      }[latest?.status];
      const field = (label, value, key) => '<p><strong>' + label + '</strong> ' + esc(value) +
        ' <button type="button" class="btn btn-outline btn-sm" data-copy="' + key + '">Sao chép</button></p>';

      box.innerHTML = '<h2>Đơn ' + esc(order.code) + '</h2>' + badge(RENTAL_STATUS, order.status) +
        '<p>Nhận xe ' + fmtDateTime(order.start) + ' · Trả xe ' + fmtDateTime(order.end) + '</p>' +
        '<p><strong>Tổng tiền ' + fmtMoney(order.total) + '</strong></p>' +
        (status ? '<p role="status">' + esc(status) + '</p>' : '') +
        (waiting ? '<p>Đã gửi yêu cầu ' + esc(PAYMENT_METHOD[selected]) +
          '. Để xác nhận nhanh, <a href="https://zalo.me/0879542925">nhắn Zalo</a> hoặc ' +
          '<a href="tel:0879542925">gọi 0879542925</a> và cung cấp mã đơn.</p>' : '') +
        (payable && !waiting ? '<label>Phương thức thanh toán <select class="select" id="customerMethod">' +
          '<option value="BANK_TRANSFER"' + (method === 'BANK_TRANSFER' ? ' selected' : '') + '>Chuyển khoản</option>' +
          '<option value="CASH"' + (method === 'CASH' ? ' selected' : '') + '>Tiền mặt</option></select></label>' : '') +
        (payable && selected === 'CASH' ? '<p>Thanh toán tiền mặt cho RentGo khi nhận xe. ' +
          'Gửi yêu cầu để admin biết phương thức bạn đã chọn.</p>' : '') +
        (showTransfer ? '<p>' + esc(bank) + ' · ' + esc(holder) + '</p>' +
          '<img id="transferQr" width="240" height="240" alt="VietQR chuyển khoản MB Bank" referrerpolicy="no-referrer">' +
          field('Số tài khoản', account, 'account') + field('Số tiền', amount, 'amount') +
          field('Nội dung chuyển khoản', reference, 'reference') : '') +
        (payable && order.status === 'PENDING' && selected === 'BANK_TRANSFER' ?
          '<p>Đơn đang chờ duyệt. Hãy đợi RentGo duyệt đơn trước khi chuyển tiền.</p>' : '') +
        (payable && !waiting && (selected === 'CASH' || (showTransfer && order.status === 'CONFIRMED')) ?
          '<p><button type="button" class="btn btn-coral" id="sendTransfer">' +
          (selected === 'CASH' ? 'Gửi yêu cầu xác nhận tiền mặt' : 'Tôi đã chuyển khoản – Gửi yêu cầu xác nhận') +
          '</button></p>' : '') + back;
      previous = state;
      if (!payable) return;

      const image = $('#transferQr');
      if (image) {
        image.onerror = () => { image.replaceWith(document.createTextNode('Không tải được VietQR. Bạn có thể dùng thông tin chuyển khoản bên dưới.')); };
        image.src = 'data:image/png;base64,' + qrPng;
      }
      const methodSelect = $('#customerMethod');
      if (methodSelect) methodSelect.onchange = () => {
        method = methodSelect.value;
        refresh();
      };
      box.querySelectorAll('[data-copy]').forEach(button => {
        button.onclick = async () => {
          try {
            await navigator.clipboard.writeText({ account, amount, reference }[button.dataset.copy]);
            errorBox.textContent = 'Đã sao chép.';
          } catch {
            errorBox.textContent = 'Không thể sao chép tự động. Hãy chọn thông tin và sao chép thủ công.';
          }
        };
      });
      const send = $('#sendTransfer');
      if (send) {
        const storageKey = 'rentgo.transfer.' + Auth.user().id + '.' + id + '.' + (latest?.id ?? 'first') + '.' + selected;
        send.onclick = async () => {
          if (busy) return;
          busy = true;
          setLoading(send, true);
          errorBox.textContent = '';
          try {
            const key = sessionStorage.getItem(storageKey) || crypto.randomUUID();
            sessionStorage.setItem(storageKey, key);
            await request('POST', path, { paymentMethod: selected }, true, { 'Idempotency-Key': key });
          } catch (error) {
            errorBox.textContent = error.message;
          } finally {
            busy = false;
            setLoading(send, false);
            await refresh();
          }
        };
      }
    } catch (error) {
      errorBox.textContent = error.message;
      if (!previous) box.innerHTML = back;
    } finally {
      busy = false;
    }
  }

  await refresh();
  const timer = setInterval(refresh, 10000);
  window.addEventListener('pagehide', () => clearInterval(timer), { once: true });
  document.addEventListener('visibilitychange', refresh);
});
