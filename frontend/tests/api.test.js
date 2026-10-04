import { test } from 'node:test';
import assert from 'node:assert/strict';

const storage = new Map();
globalThis.sessionStorage = { getItem: k => storage.get(k), setItem: (k, v) => storage.set(k, v), removeItem: k => storage.delete(k) };
globalThis.window = new EventTarget();
const { request, saveSession, getSession, query } = await import('../src/api.js');
const response = (status, data) => ({ status, ok: status < 400, json: async () => data });

test('requests send the selected interface language', async () => {
  const { setLanguage } = await import('../src/i18n.js');
  setLanguage('ar');
  globalThis.fetch = async (url, init) => {
    assert.equal(init.headers['Accept-Language'], 'ar');
    return response(200, {});
  };
  await request('/api/public/blogs');
  setLanguage('en');
});

test('expired concurrent requests share one refresh and retry with rotated token', async () => {
  saveSession({ accessToken: 'expired', refreshToken: 'old' });
  let refreshes = 0;
  globalThis.fetch = async (url, init) => {
    if (url.endsWith('/refresh')) { refreshes++; await new Promise(r => setTimeout(r, 10)); return response(200, { accessToken: 'new', refreshToken: 'rotated' }); }
    return init.headers.Authorization === 'Bearer new' ? response(200, { items: [] }) : response(401, {});
  };
  await Promise.all([request('/api/blogs'), request('/api/users')]);
  assert.equal(refreshes, 1);
  assert.equal(getSession().refreshToken, 'rotated');
});

test('failed refresh clears the session', async () => {
  saveSession({ accessToken: 'expired', refreshToken: 'invalid' });
  globalThis.fetch = async () => response(401, { error: 'Token expired' });
  await assert.rejects(request('/api/blogs'), /Token expired/);
  assert.equal(getSession(), null);
});

test('validation errors and empty responses are handled', async () => {
  globalThis.fetch = async () => response(400, { errors: { Title: ['Title is required.'] } });
  await assert.rejects(request('/api/blogs', 'POST', {}), /Title is required/);
  globalThis.fetch = async () => response(204);
  assert.equal(await request('/api/blogs/id', 'DELETE'), null);
  assert.equal(query({ status: 0, search: '', entity: null, page: 1 }), 'status=0&page=1');
});

test('multipart uploads keep the file body and let the browser set its boundary', async () => {
  saveSession({ accessToken: 'valid', refreshToken: 'refresh' });
  const form = new FormData();
  form.append('file', new Blob(['image'], { type: 'image/png' }), 'image.png');
  globalThis.fetch = async (url, init) => {
    assert.equal(init.body, form);
    assert.equal(init.headers['Content-Type'], undefined);
    assert.equal(init.headers.Authorization, 'Bearer valid');
    return response(200, { imageUrl: '/uploads/image.png' });
  };
  assert.equal((await request('/api/blogs/id/image', 'POST', form)).imageUrl, '/uploads/image.png');
});
