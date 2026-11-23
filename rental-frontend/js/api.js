class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
  }
}

const Auth = {
  token() {
    try {
      const token = sessionStorage.getItem('rentgo.token');
      if (token && Number(sessionStorage.getItem('rentgo.expires')) > Date.now()) return token;
    } catch {}
    Auth.clear();
    return null;
  },
  user() {
    if (!Auth.token()) return null;
    try { return JSON.parse(sessionStorage.getItem('rentgo.user')); } catch { return null; }
  },
  save({ accessToken, expiresAt, id, fullName, email, role }) {
    const expires = Date.parse(expiresAt);
    if (!accessToken || !(expires > Date.now())) throw new Error('Phiên đăng nhập không hợp lệ.');
    sessionStorage.setItem('rentgo.expires', String(expires));
    sessionStorage.setItem('rentgo.user', JSON.stringify({ id, fullName, email, role }));
    sessionStorage.setItem('rentgo.token', accessToken);
  },
  clear() {
    ['token', 'expires', 'user'].forEach(key => sessionStorage.removeItem('rentgo.' + key));
  },
  updateName(fullName) {
    const user = Auth.user();
    if (user) sessionStorage.setItem('rentgo.user', JSON.stringify({ ...user, fullName }));
  },
  isLoggedIn: () => !!Auth.user(),
  role: () => Auth.user()?.role
};
localStorage.removeItem('token');
localStorage.removeItem('user');

async function request(method, path, body, authenticated = false, extraHeaders = {}) {
  const headers = { Accept: 'application/json', ...extraHeaders };
  const token = authenticated ? Auth.token() : null;
  if (token) headers.Authorization = 'Bearer ' + token;
  const multipart = body instanceof FormData;
  if (body !== undefined && !multipart) headers['Content-Type'] = 'application/json';
  let response;
  try {
    response = await fetch(CONFIG.API_BASE_URL + path, {
      method, headers, body: multipart ? body : JSON.stringify(body),
      signal: AbortSignal.timeout(20000)
    });
  } catch {
    throw new ApiError('Không kết nối được API RentGo.', 0);
  }
  const data = response.headers.get('content-type')?.includes('json') ? await response.json() : null;
  if (!response.ok) {
    if (response.status === 401 && authenticated) {
      Auth.clear();
      const current = location.pathname.replace(/^\//, '') + location.search + location.hash;
      location.href = '/login.html?redirect=' + encodeURIComponent(current);
    }
    const fallback = {
      401: 'Thông tin đăng nhập sai hoặc phiên đã hết hạn.',
      403: 'Bạn không có quyền thực hiện.',
      404: 'Không tìm thấy dữ liệu.',
      409: 'Dữ liệu đã thay đổi hoặc xe không còn trống trong thời gian đã chọn.'
    };
    const validation = Object.values(data?.errors || {}).flat().join(' ');
    throw new ApiError(validation || data?.message || fallback[response.status] || 'Lỗi API (' + response.status + ').', response.status);
  }
  return data;
}

const Enum = {
  category: ['XE_SO', 'XE_GA', 'XE_DIEN', 'OTO'],
  vehicle: ['AVAILABLE', 'RENTED', 'MAINTENANCE'],
  rental: ['PENDING', 'CONFIRMED', 'PAID', 'RENTING', 'COMPLETED', 'CANCELLED'],
  detail: ['RESERVED', 'RENTING', 'RETURNED', 'CANCELLED']
};
const enumName = (values, value) => Number.isInteger(value) ? values[value] : value;
const vehiclePath = '/api/vehicles';
const modelPath = '/api/vehicle-models';
const orderPath = '/api/rental-orders';
const itemPath = (path, id) => path + '/' + encodeURIComponent(id);
const imagePath = id => itemPath(vehiclePath, id) + '/images';

const Api = {
  register: body => request('POST', '/api/auth/register', body),
  login: body => request('POST', '/api/auth/login', body),
  myProfile: () => request('GET', '/api/profile/me', undefined, true),
  updateMyProfile: body => request('PUT', '/api/profile/me', body, true),
  changeMyPassword: body => request('PUT', '/api/profile/me/password', body, true),
  vehicleModels: () => request('GET', modelPath),
  images: id => request('GET', imagePath(id)),
  fleet: (filters = {}) => request('GET', vehiclePath + '?' + new URLSearchParams(filters)),
  myPayments: page => request('GET', '/api/payments/my?page=' + page, undefined, true),
  myPayment: id => request('GET', itemPath('/api/payments/my', id), undefined, true),
  async vehicles(filters = {}) {
    const [vehicles, models] = await Promise.all([request('GET', vehiclePath + '?' + new URLSearchParams(filters)), Api.vehicleModels()]);
    const byId = new Map(models.map(model => [model.id, model]));
    const selected = vehicles.map(vehicle => ({ ...vehicle, vehicleModel: byId.get(vehicle.vehicleModelId) }));
    return Promise.all(selected.map(async vehicle => ({ ...vehicle, images: await Api.images(vehicle.id).catch(() => []) })));
  },
  async vehicle(id) {
    const vehicle = await request('GET', itemPath(vehiclePath, id));
    const [vehicleModel, images] = await Promise.all([
      request('GET', itemPath(modelPath, vehicle.vehicleModelId)),
      Api.images(vehicle.id)
    ]);
    return { ...vehicle, vehicleModel, images };
  },
  createRental: body => request('POST', orderPath, body, true),
  myRentals: (page = 1) => request('GET', orderPath + '/my?pageSize=20&page=' + page, undefined, true),
  rental: id => request('GET', itemPath(orderPath, id), undefined, true),
  cancelRental: id => request('PUT', itemPath(orderPath, id) + '/cancel', undefined, true),
  admin: {
    vehicles: () => Api.vehicles(),
    models: () => Api.vehicleModels(),
    createVehicle: body => request('POST', vehiclePath, body, true),
    updateVehicle: (id, body) => request('PUT', itemPath(vehiclePath, id), body, true),
    deleteVehicle: id => request('DELETE', itemPath(vehiclePath, id), undefined, true),
    createModel: body => request('POST', modelPath, body, true),
    updateModel: (id, body) => request('PUT', itemPath(modelPath, id), body, true),
    deleteModel: id => request('DELETE', itemPath(modelPath, id), undefined, true),
    uploadImage: (id, body) => request('POST', imagePath(id), body, true),
    primaryImage: (id, imageId) => request('PUT', imagePath(id) + '/primary', { imageId }, true),
    deleteImage: (id, imageId) => request('DELETE', itemPath(imagePath(id), imageId), undefined, true),
    updateRentalStatus: (id, status) => request('PUT', itemPath(orderPath, id) + '/status', { status: Enum.rental.indexOf(status) }, true)
  }
};
