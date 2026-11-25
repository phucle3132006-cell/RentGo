/* Đăng nhập / Đăng ký */
document.addEventListener('DOMContentLoaded', () => {
  const target = getParam('redirect') || '';
  const redirect = /^(?:[a-z-]+\.html|admin\/[a-z-]+\.html)(?:[?#].*)?$/i.test(target) ? target : '';
  const destination = role => {
    const home = role === 'ADMIN' ? 'admin/index.html' : 'index.html';
    if (!redirect) return home;
    const path = redirect.split(/[?#]/, 1)[0].toLowerCase();
    if (path.startsWith('admin/')) return role === 'ADMIN' ? redirect : home;
    if (['booking.html', 'my-orders.html', 'profile.html', 'payment.html', 'payment-history.html'].includes(path)) {
      return role === 'CUSTOMER' ? redirect : home;
    }
    return redirect;
  };
  const msg = (t, type = 'err') => $('#msg').innerHTML = t ? `<div class="alert ${type}">${esc(t)}</div>` : '';
  const isEmail = s => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(s);

  // giữ redirect khi chuyển giữa login <-> register
  if (redirect) { const a = $('#toReg') || $('#toLogin'); if (a) a.href += '?redirect=' + encodeURIComponent(redirect); }

  const btnLogin = $('#btnLogin');
  if (btnLogin) {
    if (getParam('passwordChanged') === '1') msg('Đã đổi mật khẩu. Hãy đăng nhập lại bằng mật khẩu mới.', 'ok');
    if (Auth.isLoggedIn()) location.href = destination(Auth.role());
    const submit = async () => {
      if (btnLogin.disabled) return;
      msg('');
      const email = $('#email').value.trim(), password = $('#password').value;
      if (!isEmail(email)) return msg('Email không hợp lệ');
      if (!password) return msg('Vui lòng nhập mật khẩu');
      setLoading(btnLogin, true);
      try {
        const r = await Api.login({ email, password });
        Auth.save(r);
        const role = r.role;
        location.href = destination(role);
      } catch (e) { msg(e.message); setLoading(btnLogin, false); }
    };
    btnLogin.onclick = submit;
    $('#password').onkeydown = e => { if (e.key === 'Enter') submit(); };
  }

  const btnReg = $('#btnReg');
  if (btnReg) btnReg.onclick = async () => {
    if (btnReg.disabled) return;
    msg('');
    const b = { fullName: $('#full_name').value.trim(), email: $('#email').value.trim(), phone: $('#phone').value.trim(), password: $('#password').value };
    if (!b.fullName) return msg('Vui lòng nhập họ tên');
    if (!isEmail(b.email)) return msg('Email không hợp lệ');
    if (!/^0\d{9,10}$/.test(b.phone)) return msg('Số điện thoại không hợp lệ (VD: 0901234567)');
    if (b.password.length < 6) return msg('Mật khẩu tối thiểu 6 ký tự');
    if (b.password !== $('#password2').value) return msg('Mật khẩu nhập lại không khớp');
    setLoading(btnReg, true);
    try {
      await Api.register({ ...b, confirmPassword: document.getElementById('password2').value });
      msg('Đăng ký thành công! Đang chuyển tới trang đăng nhập...', 'ok');
      setTimeout(() => location.href = 'login.html' + (redirect ? '?redirect=' + encodeURIComponent(redirect) : ''), 1200);
    } catch (e) { msg(e.message); setLoading(btnReg, false); }
  };
});



