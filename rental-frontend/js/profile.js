document.addEventListener('DOMContentLoaded', async () => {
  if (!requireRoles(['CUSTOMER'])) return;

  const profileForm = $('#profileForm');
  const passwordForm = $('#passwordForm');
  const status = $('#profileStatus');

  function showResult(element, message, type) {
    element.textContent = message;
    element.className = 'alert ' + type;
  }

  function showProfile(profile) {
    $('#profileName').value = profile.fullName;
    $('#profilePhone').value = profile.phone;
    $('#profileEmail').value = profile.email;
    Auth.updateName(profile.fullName);
    renderHeader();
  }

  function handleSubmit(form, feedback, submit) {
    form.addEventListener('submit', async event => {
      event.preventDefault();
      const button = form.querySelector('button[type="submit"]');
      if (button.disabled) return;
      const values = Object.fromEntries(new FormData(form));
      feedback.textContent = '';
      feedback.className = '';
      setLoading(button, true);
      try { await submit(values); }
      catch (error) { showResult(feedback, error.message, 'err'); }
      finally { setLoading(button, false); }
    });
  }

  handleSubmit(profileForm, $('#profileResult'), async values => {
    showProfile(await Api.updateMyProfile(values));
    showResult($('#profileResult'), 'Đã lưu thông tin cá nhân.', 'ok');
  });

  handleSubmit(passwordForm, $('#passwordResult'), async values => {
    if (values.newPassword !== values.confirmPassword) throw new Error('Mật khẩu nhập lại không khớp.');
    await Api.changeMyPassword(values);
    passwordForm.reset();
    Auth.clear();
    location.replace('login.html?redirect=profile.html&passwordChanged=1');
  });

  try {
    showProfile(await Api.myProfile());
    status.hidden = true;
    $('#profileForms').classList.remove('hidden');
  } catch (error) {
    showResult(status, error.message, 'err');
  }
});
