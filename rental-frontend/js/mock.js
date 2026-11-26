/* =========================================================
   MOCK BACKEND – giả lập API (snake_case đúng tài liệu).
   Dữ liệu lưu trong localStorage ('mock_db_v1') để giữ giữa các trang.
   Xoá localStorage để reset. Tắt bằng CONFIG.USE_MOCK = false.
   Tài khoản demo:  customer@demo.com / 123456   |   admin@demo.com / 123456   |   staff@demo.com / 123456
   ========================================================= */
const Mock = (() => {
  const KEY = 'mock_db_v1';
  const now = () => new Date().toISOString();
  const err = (status, message) => new ApiError(message, status, { message });

  function seed() {
    const models = [
      [1, 'OTO', 'Toyota', 'Vios', 2025, 5, 1500, 'Sedan 5 chỗ tiết kiệm nhiên liệu, phù hợp đi phố và đường dài.', 800000],
      [2, 'OTO', 'Toyota', 'Innova', 2024, 7, 2000, 'MPV 7 chỗ rộng rãi cho gia đình và nhóm bạn.', 1100000],
      [3, 'OTO', 'Mazda', 'CX-5', 2025, 5, 2000, 'SUV 5 chỗ êm ái, trang bị an toàn hiện đại.', 1200000],
      [4, 'OTO', 'VinFast', 'VF 8', 2025, 5, null, 'SUV điện 5 chỗ, vận hành êm, không tiếng ồn.', 1300000],
      [5, 'OTO', 'Hyundai', 'Accent', 2024, 5, 1400, 'Sedan hạng B nhỏ gọn, dễ lái.', 750000],
      [6, 'XE_SO', 'Honda', 'Wave Alpha', 2024, 2, 110, 'Xe số bền bỉ, tiết kiệm xăng.', 120000],
      [7, 'XE_SO', 'Yamaha', 'Sirius', 2024, 2, 115, 'Xe số phổ thông, dễ điều khiển.', 130000],
      [8, 'XE_GA', 'Honda', 'Vision', 2025, 2, 110, 'Xe ga nhỏ gọn cho đi lại trong thành phố.', 150000],
      [9, 'XE_GA', 'Honda', 'SH Mode', 2025, 2, 125, 'Xe ga cao cấp, thiết kế sang trọng.', 250000],
      [10, 'XE_GA', 'Yamaha', 'Grande', 2025, 2, 125, 'Xe ga thời trang, cốp rộng.', 200000],
      [11, 'XE_DIEN', 'VinFast', 'Feliz S', 2025, 2, null, 'Xe máy điện chạy êm, đi được ~130km mỗi lần sạc.', 180000],
      [12, 'XE_DIEN', 'Yadea', 'Ulike', 2024, 2, null, 'Xe điện nhỏ gọn, giá thuê rẻ.', 140000]
    ].map(a => ({ id: a[0], category: a[1], brand: a[2], model: a[3], year: a[4], seats: a[5], engine_capacity: a[6], description: a[7], price_per_day: a[8], created_at: now(), updated_at: now() }));

    const colors = ['Đen', 'Trắng', 'Xám', 'Đỏ', 'Xanh'];
    const conds = ['Rất tốt', 'Tốt', 'Tốt', 'Khá'];
    const vehicles = [];
    models.forEach((m, i) => {
      const n = i % 3 === 0 ? 2 : (i % 2 ? 1 : 2);
      for (let k = 0; k < n; k++) {
        const id = vehicles.length + 1;
        const plate = m.category === 'OTO' ? `51K-${String(100 + id * 7).padStart(3, '0')}.${String(10 + id * 3).padStart(2, '0')}` : `59X${k + 1}-${String(10000 + id * 137).slice(0, 5)}`;
        vehicles.push({
          id, vehicle_model_id: m.id, license_plate: plate, color: colors[(id + k) % 5], condition: conds[id % 4],
          status: id === 4 ? 'MAINTENANCE' : (id === 7 ? 'RENTED' : 'AVAILABLE'), created_at: now(), updated_at: now()
        });
      }
    });

    const users = [
      { id: 1, full_name: 'Quản trị viên', email: 'admin@demo.com', phone: '0900000001', role: 'ADMIN', status: 'ACTIVE', created_at: now(), updated_at: now() },
      { id: 2, full_name: 'Nhân viên Lan', email: 'staff@demo.com', phone: '0900000002', role: 'STAFF', status: 'ACTIVE', created_at: now(), updated_at: now() },
      { id: 3, full_name: 'Nguyễn Văn A', email: 'customer@demo.com', phone: '0901234567', role: 'CUSTOMER', status: 'ACTIVE', created_at: now(), updated_at: now() },
      { id: 4, full_name: 'Trần Thị B', email: 'b@demo.com', phone: '0912345678', role: 'CUSTOMER', status: 'LOCKED', created_at: now(), updated_at: now() }
    ];
    const passwords = { 1: '123456', 2: '123456', 3: '123456', 4: '123456' };

    const d = (days, h) => { const x = new Date(); x.setDate(x.getDate() + days); x.setHours(h, 0, 0, 0); return x.toISOString().slice(0, 16); };
    const rentals = [
      { id: 1, order_code: 'RT-0001', customer_id: 3, start_datetime: d(-10, 8), end_datetime: d(-7, 18), total_amount: 2400000, status: 'COMPLETED', note: '', created_at: now(), updated_at: now(),
        details: [{ id: 1, vehicle_id: 1, price_per_day: 800000, rental_days: 3, subtotal: 2400000, status: 'RETURNED' }] },
      { id: 2, order_code: 'RT-0002', customer_id: 3, start_datetime: d(3, 8), end_datetime: d(5, 18), total_amount: 900000, status: 'PENDING', note: 'Nhận xe buổi sáng', created_at: now(), updated_at: now(),
        details: [{ id: 2, vehicle_id: 9, price_per_day: 150000, rental_days: 3, subtotal: 450000, status: 'RESERVED' }, { id: 3, vehicle_id: 10, price_per_day: 150000, rental_days: 3, subtotal: 450000, status: 'RESERVED' }] }
    ];
    const payments = [
      { id: 1, rental_order_id: 1, amount: 2400000, payment_method: 'VNPAY', transaction_code: 'TXN-A1B2C3', status: 'PAID', paid_at: now(), created_at: now() }
    ];
    return { models, vehicles, users, passwords, rentals, payments, seq: { user: 5, vehicle: 100, model: 100, rental: 3, detail: 10, payment: 2 } };
  }

  function load() {
    try { const s = localStorage.getItem(KEY); if (s) return JSON.parse(s); } catch (e) {}
    const db = seed(); save(db); return db;
  }
  function save(db) { localStorage.setItem(KEY, JSON.stringify(db)); }

  /* ---------- builders ---------- */
  const vehicleOut = (db, v) => ({
    ...v,
    vehicle_model: db.models.find(m => m.id === v.vehicle_model_id) || null,
    images: [] // chưa có ảnh -> frontend dùng ảnh placeholder
  });
  const userOut = (u) => { const { password_hash, ...rest } = u; return rest; };
  const rentalOut = (db, r) => {
    const c = db.users.find(u => u.id === r.customer_id);
    return {
      ...r,
      customer: c ? { id: c.id, full_name: c.full_name, email: c.email, phone: c.phone } : null,
      details: r.details.map(dt => ({ ...dt, vehicle: vehicleOut(db, db.vehicles.find(v => v.id === dt.vehicle_id)) })),
      payments: db.payments.filter(p => p.rental_order_id === r.id)
    };
  };
  const calcDays = (s, e) => Math.max(1, Math.ceil((new Date(e) - new Date(s)) / 86400000));

  function isFree(db, vehicleId, start, end, ignoreRentalId) {
    const v = db.vehicles.find(x => x.id === vehicleId);
    if (!v) return { ok: false, reason: 'Xe không tồn tại' };
    if (v.status === 'MAINTENANCE') return { ok: false, reason: 'Xe đang bảo trì' };
    const s = new Date(start), e = new Date(end);
    for (const r of db.rentals) {
      if (r.id === ignoreRentalId || ['CANCELLED', 'COMPLETED'].includes(r.status)) continue;
      if (!r.details.some(d => d.vehicle_id === vehicleId && d.status !== 'CANCELLED')) continue;
      if (s < new Date(r.end_datetime) && e > new Date(r.start_datetime)) return { ok: false, reason: 'Trùng lịch thuê' };
    }
    return { ok: true };
  }

  function validateRentalBody(db, b, ignoreId) {
    if (!b.vehicle_ids || !b.vehicle_ids.length) throw err(400, 'Chưa chọn xe');
    if (new Set(b.vehicle_ids).size !== b.vehicle_ids.length) throw err(400, 'Một xe không được xuất hiện 2 lần trong cùng đơn');
    if (!b.start_datetime || !b.end_datetime || new Date(b.end_datetime) <= new Date(b.start_datetime)) throw err(400, 'Thời gian thuê không hợp lệ');
    for (const id of b.vehicle_ids) {
      const f = isFree(db, id, b.start_datetime, b.end_datetime, ignoreId);
      if (!f.ok) throw err(400, `Xe #${id} không khả dụng: ${f.reason}`);
    }
  }
  function buildDetails(db, b) {
    const days = calcDays(b.start_datetime, b.end_datetime);
    return b.vehicle_ids.map(vid => {
      const v = db.vehicles.find(x => x.id === vid); const m = db.models.find(x => x.id === v.vehicle_model_id);
      return { id: db.seq.detail++, vehicle_id: vid, price_per_day: m.price_per_day, rental_days: days, subtotal: m.price_per_day * days, status: 'RESERVED' };
    });
  }

  /* ---------- router ---------- */
  async function handle(method, fullPath, body, token) {
    await new Promise(r => setTimeout(r, 180));
    const db = load();
    const [p, qs] = fullPath.split('?');
    const q = Object.fromEntries(new URLSearchParams(qs || ''));
    const me = token && token.startsWith('mock-') ? db.users.find(u => u.id == token.slice(5)) : null;
    const need = (roles) => {
      if (!me) throw err(401, 'Bạn cần đăng nhập');
      if (me.status === 'LOCKED') throw err(403, 'Tài khoản đã bị khoá');
      if (roles && !roles.includes(me.role)) throw err(403, 'Bạn không có quyền thực hiện');
    };
    const R = (meth, re) => (method === meth ? p.match(re) : null);
    let m;

    /* AUTH */
    if (R('POST', /^\/api\/auth\/register$/)) {
      if (!body.email || !body.password || !body.full_name) throw err(400, 'Vui lòng nhập đủ thông tin');
      if (db.users.some(u => u.email.toLowerCase() === body.email.toLowerCase())) throw err(400, 'Email đã được đăng ký');
      const u = { id: db.seq.user++, full_name: body.full_name, email: body.email, phone: body.phone || '', role: 'CUSTOMER', status: 'ACTIVE', created_at: now(), updated_at: now() };
      db.users.push(u); db.passwords[u.id] = body.password; save(db);
      return { message: 'Register successful' };
    }
    if (R('POST', /^\/api\/auth\/login$/)) {
      const u = db.users.find(x => x.email.toLowerCase() === (body.email || '').toLowerCase());
      if (!u || db.passwords[u.id] !== body.password) throw err(401, 'Email hoặc mật khẩu không đúng');
      if (u.status === 'LOCKED') throw err(403, 'Tài khoản đã bị khoá');
      return { token: 'mock-' + u.id, user: { id: u.id, full_name: u.full_name, email: u.email, phone: u.phone, role: u.role } };
    }
    if (R('POST', /^\/api\/auth\/logout$/)) return { message: 'Logout successful' };
    if (R('GET', /^\/api\/auth\/me$/)) { need(); return userOut(me); }

    /* PUBLIC VEHICLES */
    if (R('GET', /^\/api\/vehicles$/)) {
      let list = db.vehicles.map(v => vehicleOut(db, v));
      if (q.category) list = list.filter(v => v.vehicle_model.category === q.category);
      if (q.brand) list = list.filter(v => v.vehicle_model.brand.toLowerCase() === q.brand.toLowerCase());
      if (q.status) list = list.filter(v => v.status === q.status);
      return list;
    }
    if ((m = R('GET', /^\/api\/vehicles\/(\d+)$/))) {
      const v = db.vehicles.find(x => x.id == m[1]); if (!v) throw err(404, 'Không tìm thấy xe');
      return vehicleOut(db, v);
    }
    if (R('GET', /^\/api\/vehicle-models$/)) return db.models;
    if ((m = R('GET', /^\/api\/vehicle-models\/(\d+)$/))) {
      const x = db.models.find(y => y.id == m[1]); if (!x) throw err(404, 'Không tìm thấy model'); return x;
    }

    /* AVAILABILITY */
    if (R('POST', /^\/api\/rentals\/check-availability$/)) {
      if (!body.start_datetime || !body.end_datetime || new Date(body.end_datetime) <= new Date(body.start_datetime)) throw err(400, 'Thời gian thuê không hợp lệ');
      const vs = (body.vehicle_ids || []).map(id => ({ vehicle_id: id, available: isFree(db, id, body.start_datetime, body.end_datetime).ok }));
      return { available: vs.every(x => x.available), vehicles: vs };
    }

    /* CUSTOMER RENTALS */
    if (R('POST', /^\/api\/rentals$/)) {
      need(['CUSTOMER', 'STAFF', 'MANAGER', 'ADMIN']);
      validateRentalBody(db, body);
      const details = buildDetails(db, body);
      const id = db.seq.rental++;
      const r = { id, order_code: 'RT-' + String(id).padStart(4, '0'), customer_id: me.id, start_datetime: body.start_datetime, end_datetime: body.end_datetime,
        total_amount: details.reduce((s, d) => s + d.subtotal, 0), status: 'PENDING', note: body.note || '', created_at: now(), updated_at: now(), details };
      db.rentals.push(r); save(db); return rentalOut(db, r);
    }
    if (R('GET', /^\/api\/rentals\/my$/)) { need(); return db.rentals.filter(r => r.customer_id === me.id).sort((a, b) => b.id - a.id).map(r => rentalOut(db, r)); }
    if ((m = R('GET', /^\/api\/rentals\/(\d+)$/))) {
      need(); const r = db.rentals.find(x => x.id == m[1]);
      if (!r || (r.customer_id !== me.id && me.role === 'CUSTOMER')) throw err(404, 'Không tìm thấy đơn thuê');
      return rentalOut(db, r);
    }
    if ((m = R('PUT', /^\/api\/rentals\/(\d+)$/))) {
      need(); const r = db.rentals.find(x => x.id == m[1]);
      if (!r || r.customer_id !== me.id) throw err(404, 'Không tìm thấy đơn thuê');
      if (!['PENDING', 'CONFIRMED'].includes(r.status)) throw err(400, 'Chỉ sửa được đơn đang chờ xử lý');
      validateRentalBody(db, body, r.id);
      r.start_datetime = body.start_datetime; r.end_datetime = body.end_datetime; r.note = body.note || '';
      r.details = buildDetails(db, body); r.total_amount = r.details.reduce((s, d) => s + d.subtotal, 0); r.updated_at = now();
      save(db); return rentalOut(db, r);
    }
    if ((m = R('PATCH', /^\/api\/rentals\/(\d+)\/cancel$/))) {
      need(); const r = db.rentals.find(x => x.id == m[1]);
      if (!r || r.customer_id !== me.id) throw err(404, 'Không tìm thấy đơn thuê');
      if (!['PENDING', 'CONFIRMED'].includes(r.status)) throw err(400, 'Không thể huỷ đơn ở trạng thái này');
      r.status = 'CANCELLED'; r.details.forEach(d => d.status = 'CANCELLED'); r.updated_at = now(); save(db);
      return { message: 'Cancel successful' };
    }

    /* PAYMENTS (customer) */
    if (R('POST', /^\/api\/payments$/)) {
      need(); const r = db.rentals.find(x => x.id == body.rental_order_id);
      if (!r || r.customer_id !== me.id) throw err(404, 'Không tìm thấy đơn thuê');
      if (!['PENDING', 'CONFIRMED'].includes(r.status)) throw err(400, 'Đơn này không thể thanh toán');
      const pay = { id: db.seq.payment++, rental_order_id: r.id, amount: r.total_amount, payment_method: body.payment_method, transaction_code: 'TXN-' + Math.random().toString(36).slice(2, 8).toUpperCase(), status: 'PAID', paid_at: now(), created_at: now() };
      db.payments.push(pay); r.status = 'PAID'; save(db); return pay;
    }
    if (R('GET', /^\/api\/payments\/my$/)) {
      need(); const ids = db.rentals.filter(r => r.customer_id === me.id).map(r => r.id);
      return db.payments.filter(p => ids.includes(p.rental_order_id)).map(p => ({ ...p, order_code: db.rentals.find(r => r.id === p.rental_order_id).order_code }));
    }
    if ((m = R('GET', /^\/api\/payments\/order\/(\d+)$/))) { need(); return db.payments.filter(p => p.rental_order_id == m[1]); }

    /* ADMIN – vehicles */
    if (R('GET', /^\/api\/admin\/vehicles$/)) { need(['STAFF', 'MANAGER', 'ADMIN']); return db.vehicles.map(v => vehicleOut(db, v)); }
    if ((m = R('GET', /^\/api\/admin\/vehicles\/(\d+)$/))) { need(['STAFF', 'MANAGER', 'ADMIN']); return vehicleOut(db, db.vehicles.find(v => v.id == m[1])); }
    if (R('POST', /^\/api\/admin\/vehicles$/)) {
      need(['STAFF', 'MANAGER', 'ADMIN']);
      if (db.vehicles.some(v => v.license_plate === body.license_plate)) throw err(400, 'Biển số đã tồn tại');
      const v = { id: db.seq.vehicle++, ...body, vehicle_model_id: +body.vehicle_model_id, created_at: now(), updated_at: now() };
      db.vehicles.push(v); save(db); return vehicleOut(db, v);
    }
    if ((m = R('PUT', /^\/api\/admin\/vehicles\/(\d+)$/))) {
      need(['STAFF', 'MANAGER', 'ADMIN']); const v = db.vehicles.find(x => x.id == m[1]); if (!v) throw err(404, 'Không tìm thấy xe');
      Object.assign(v, body, { vehicle_model_id: +body.vehicle_model_id, updated_at: now() }); save(db); return vehicleOut(db, v);
    }
    if ((m = R('DELETE', /^\/api\/admin\/vehicles\/(\d+)$/))) {
      need(['STAFF', 'MANAGER', 'ADMIN']);
      if (db.rentals.some(r => r.details.some(d => d.vehicle_id == m[1]))) throw err(400, 'Xe đã có lịch sử thuê, không thể xoá');
      db.vehicles = db.vehicles.filter(v => v.id != m[1]); save(db); return { message: 'Deleted' };
    }

    /* ADMIN – models */
    if (R('GET', /^\/api\/admin\/vehicle-models$/)) { need(['STAFF', 'MANAGER', 'ADMIN']); return db.models; }
    if (R('POST', /^\/api\/admin\/vehicle-models$/)) {
      need(['STAFF', 'MANAGER', 'ADMIN']);
      const x = { id: db.seq.model++, ...body, created_at: now(), updated_at: now() }; db.models.push(x); save(db); return x;
    }
    if ((m = R('PUT', /^\/api\/admin\/vehicle-models\/(\d+)$/))) {
      need(['STAFF', 'MANAGER', 'ADMIN']); const x = db.models.find(y => y.id == m[1]); if (!x) throw err(404, 'Không tìm thấy model');
      Object.assign(x, body, { updated_at: now() }); save(db); return x;
    }
    if ((m = R('DELETE', /^\/api\/admin\/vehicle-models\/(\d+)$/))) {
      need(['STAFF', 'MANAGER', 'ADMIN']);
      if (db.vehicles.some(v => v.vehicle_model_id == m[1])) throw err(400, 'Model đang có xe, không thể xoá');
      db.models = db.models.filter(x => x.id != m[1]); save(db); return { message: 'Deleted' };
    }

    /* ADMIN – users */
    if (R('GET', /^\/api\/admin\/users$/)) { need(['ADMIN']); return db.users.map(userOut); }
    if ((m = R('GET', /^\/api\/admin\/users\/(\d+)$/))) { need(['ADMIN']); return userOut(db.users.find(u => u.id == m[1])); }
    if ((m = R('PUT', /^\/api\/admin\/users\/(\d+)$/))) {
      need(['ADMIN']); const u = db.users.find(x => x.id == m[1]); if (!u) throw err(404, 'Không tìm thấy người dùng');
      Object.assign(u, body, { updated_at: now() }); save(db); return userOut(u);
    }

    /* ADMIN – rentals */
    if (R('GET', /^\/api\/admin\/rentals$/)) { need(['STAFF', 'MANAGER', 'ADMIN']); return [...db.rentals].sort((a, b) => b.id - a.id).map(r => rentalOut(db, r)); }
    if ((m = R('GET', /^\/api\/admin\/rentals\/(\d+)$/))) { need(['STAFF', 'MANAGER', 'ADMIN']); return rentalOut(db, db.rentals.find(r => r.id == m[1])); }
    if ((m = R('PATCH', /^\/api\/admin\/rentals\/(\d+)\/status$/))) {
      need(['STAFF', 'MANAGER', 'ADMIN']); const r = db.rentals.find(x => x.id == m[1]); if (!r) throw err(404, 'Không tìm thấy đơn');
      const st = body.status; r.status = st; r.updated_at = now();
      r.details.forEach(d => {
        const v = db.vehicles.find(x => x.id === d.vehicle_id);
        if (st === 'RENTING') { d.status = 'RENTING'; v.status = 'RENTED'; }
        if (st === 'COMPLETED') { d.status = 'RETURNED'; v.status = 'AVAILABLE'; }
        if (st === 'CANCELLED') { d.status = 'CANCELLED'; if (v.status === 'RENTED') v.status = 'AVAILABLE'; }
      });
      save(db); return rentalOut(db, r);
    }

    /* ADMIN – payments */
    if (R('GET', /^\/api\/admin\/payments\/revenue$/)) {
      need(['MANAGER', 'ADMIN']); const paid = db.payments.filter(p => p.status === 'PAID');
      return { total_revenue: paid.reduce((s, p) => s + p.amount, 0), total_transactions: paid.length };
    }
    if (R('GET', /^\/api\/admin\/payments$/)) {
      need(['STAFF', 'MANAGER', 'ADMIN']);
      return db.payments.map(p => ({ ...p, order_code: (db.rentals.find(r => r.id === p.rental_order_id) || {}).order_code }));
    }
    if ((m = R('GET', /^\/api\/admin\/payments\/(\d+)$/))) { need(['STAFF', 'MANAGER', 'ADMIN']); return db.payments.find(p => p.id == m[1]); }
    if ((m = R('PATCH', /^\/api\/admin\/payments\/(\d+)\/status$/))) {
      need(['MANAGER', 'ADMIN']); const pay = db.payments.find(p => p.id == m[1]); if (!pay) throw err(404, 'Không tìm thấy thanh toán');
      pay.status = body.status; if (body.status === 'PAID') pay.paid_at = now(); save(db); return pay;
    }

    throw err(404, `Mock chưa hỗ trợ: ${method} ${p}`);
  }

  return { handle, reset() { localStorage.removeItem(KEY); } };
})();
