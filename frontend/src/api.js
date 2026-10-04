import { getLanguage, t } from './i18n.js';
const base = (import.meta.env?.VITE_API_URL || '').replace(/\/$/, '');
const key = 'journal.session';
let session = JSON.parse(sessionStorage.getItem(key) || 'null');
let refreshing;
export const getSession = () => session;
export const mediaUrl = path => path ? `${base}${path}` : '';
export async function uploadImage(path, file) {
  const form = new FormData();
  form.append('file', file);
  return request(path, 'POST', form);
}
export function saveSession(value) {
  session = value;
  if (value) sessionStorage.setItem(key, JSON.stringify(value));
  else sessionStorage.removeItem(key);
  window.dispatchEvent(new Event('sessionchange'));
}
export function query(values) {
  return new URLSearchParams(Object.entries(values).filter(([, v]) => v !== '' && v != null)).toString();
}
export async function request(path, method = 'GET', body, retry = true) {
  let response;
  try {
    response = await fetch(`${base}${path}`, {
      method, headers: { 'Accept-Language': getLanguage(), ...(body !== undefined && !(body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}), ...(session ? { Authorization: `Bearer ${session.accessToken}` } : {}) },
      ...(body !== undefined ? { body: body instanceof FormData ? body : JSON.stringify(body) } : {}),
    });
  } catch { throw new Error(t('The service is temporarily unavailable. Please try again later.')); }
  if (response.status === 401 && session && retry && !['/api/auth/login', '/api/auth/register', '/api/auth/refresh'].includes(path)) {
    await refreshSession();
    return request(path, method, body, false);
  }
  const data = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    const validation = data?.errors ? Object.values(data.errors).flat().join(' ') : null;
    throw new Error(validation || data?.detail || data?.error || data?.title || t('Request failed ({status}).', { status: response.status }));
  }
  return data;
}
export async function refreshSession() {
  if (!session) throw new Error(t('Please sign in again.'));
  if (!refreshing) refreshing = request('/api/auth/refresh', 'POST', { refreshToken: session.refreshToken }, false)
    .then(saveSession).catch(error => { saveSession(null); throw error; }).finally(() => { refreshing = null; });
  await refreshing;
}
