import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';

const base = process.env.TEST_BASE_URL || 'http://127.0.0.1:5173';
const suffix = randomUUID().slice(0, 8);
const created = { users: [], roles: [], categories: [], employees: [], blogs: [] };
let admin;
async function call(path, method = 'GET', body, token = admin, expected = 200, language = 'en') {
  const response = await fetch(base + path, {
    method, headers: { 'Accept-Language': language, ...(token ? { Authorization: 'Bearer ' + token } : {}),
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}) },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {})
  });
  const data = response.status === 204 ? null : await response.json();
  assert.equal(response.status, expected, method + ' ' + path + ': ' + JSON.stringify(data));
  return data;
}
async function create(kind, body) {
  const data = await call('/api/' + kind, 'POST', body, admin, 201);
  created[kind].push(data.id);
  return data;
}
try {
  const session = await call('/api/auth/login', 'POST', {
    email: process.env.TEST_ADMIN_EMAIL || 'admin.demo@example.test',
    password: process.env.TEST_ADMIN_PASSWORD || 'DemoPass123!'
  }, null);
  admin = session.accessToken;
  const me = await call('/api/auth/me');
  assert.ok(me.permissions.includes('roles.manage'));
  for (const path of ['/api/blogs', '/api/categories', '/api/users', '/api/roles',
    '/api/roles/permissions', '/api/employees', '/api/audit-logs', '/api/public/blogs'])
    await call(path);
  const permissions = await call('/api/roles/permissions');
  const roles = await call('/api/roles');
  const role = await create('roles', { name: 'QA role ' + suffix, permissions: ['blogs.read', 'blogs.create'] });
  for (const selection of [role.permissions, ['blogs.read'], [], ['blogs.read', 'blogs.create']]) {
    const result = await call('/api/roles/' + role.id + '/permissions', 'PUT', { permissions: selection });
    assert.deepEqual([...result.permissions].sort(), [...selection].sort());
  }
  await call('/api/roles/' + role.id + '/permissions', 'PUT', { permissions: ['bad.permission'] }, admin, 400, 'ar');
  assert.equal((await call('/api/roles/' + role.id)).permissions.length, 2);
  const adminRole = roles.find(r => r.name === 'Admin');
  await call('/api/roles/' + adminRole.id + '/permissions', 'PUT', { permissions: [] }, admin, 400);
  await call('/api/roles/' + role.id, 'PUT', { name: 'QA updated ' + suffix, description: 'اختبار' });
  const userPassword = 'QApass123!' + suffix;
  const user = await call('/api/auth/register', 'POST', { username: 'qa_' + suffix,
    email: 'qa_' + suffix + '@example.invalid', password: userPassword }, null);
  created.users.push(user.user.id);
  const author = user.accessToken;
  for (const ids of [[role.id], [], [roles.find(r => r.name === 'Author').id]]) {
    const result = await call('/api/users/' + user.user.id + '/roles', 'PUT', { roleIds: ids });
    assert.equal(result.roles.length, ids.length);
  }
  await call('/api/users/' + me.id + '/roles', 'PUT', { roleIds: [] }, admin, 400);
  await call('/api/users/' + user.user.id, 'PUT', { username: user.user.username,
    email: user.user.email, displayName: 'مستخدم اختبار' });
  await call('/api/users/' + user.user.id + '/status', 'PATCH', { isActive: false });
  await call('/api/auth/login', 'POST', { email: user.user.email, password: userPassword }, null, 401);
  await call('/api/users/' + user.user.id + '/status', 'PATCH', { isActive: true });
  await call('/api/users', 'GET', undefined, author, 403);
  const category = await create('categories', { name: 'تصنيف اختبار ' + suffix, description: 'اختبار باللغتين' });
  await call('/api/categories/' + category.id, 'PUT', { name: category.name, description: 'Updated category' });
  const employee = await create('employees', { firstName: 'QA', lastName: suffix,
    employeeNumber: 'QA-' + suffix, department: 'Test', jobTitle: 'Tester', userId: user.user.id });
  await call('/api/employees/' + employee.id, 'PUT', { ...employee, department: 'اختبار', isActive: false });
  const article = await call('/api/blogs', 'POST', { title: 'مقال اختبار ' + suffix,
    content: 'Arabic and English test article content.', categoryId: category.id }, author, 201);
  created.blogs.push(article.id);
  await call('/api/blogs/' + article.id, 'PUT', { title: article.title,
    content: 'Updated article محتوى عربي للتجربة.', categoryId: category.id }, author);
  await call('/api/categories/' + category.id, 'DELETE', undefined, admin, 400);
  await call('/api/blogs/' + article.id + '/publish', 'POST', undefined, admin, 400);
  await call('/api/blogs/' + article.id + '/submit', 'POST', undefined, author);
  await call('/api/blogs/' + article.id + '/review', 'POST', { approved: false, comment: 'Needs changes' });
  await call('/api/blogs/' + article.id + '/submit', 'POST', undefined, author);
  await call('/api/blogs/' + article.id + '/review', 'POST', { approved: true, comment: 'Approved' });
  await call('/api/blogs/' + article.id + '/publish', 'POST');
  await call('/api/public/blogs/' + article.id, 'GET', undefined, null);
  assert.equal((await call('/api/blogs/' + article.id + '/history')).length, 2);
  await call('/api/blogs/' + article.id + '/unpublish', 'POST');
  await call('/api/public/blogs/' + article.id, 'GET', undefined, null, 404);
  await call('/api/auth/profile', 'PUT', { username: user.user.username,
    email: user.user.email, displayName: 'Test profile' }, author);
  await call('/api/auth/change-password', 'POST', { currentPassword: userPassword, newPassword: userPassword + 'New' }, author, 204);
  const updatedSession = await call('/api/auth/login', 'POST', { email: user.user.email, password: userPassword + 'New' }, null);
  const renewed = await call('/api/auth/refresh', 'POST', { refreshToken: updatedSession.refreshToken }, null);
  await call('/api/auth/logout', 'POST', { refreshToken: renewed.refreshToken }, renewed.accessToken, 204);
  await call('/api/auth/refresh', 'POST', { refreshToken: renewed.refreshToken }, null, 401);
  for (const language of ['en', 'ar']) {
    const bad = await call('/api/auth/login', 'POST', { email: 'unknown@example.invalid', password: 'wrongpassword' }, null, 401, language);
    assert.equal(/[\u0600-\u06ff]/.test(bad.error), language === 'ar');
    await call('/api/auth/register', 'POST', {}, null, 400, language);
  }
  const audit = await call('/api/audit-logs?entityName=Role&pageSize=100');
  assert.ok(audit.items.some(item => item.entityId === role.id));
  console.log('PASS: lists, role permissions, role assignment, users/status, categories, employees, article workflow, ownership/authorization, profile, password, refresh/logout, audit and bilingual validation.');
} finally {
  for (const kind of ['blogs', 'employees', 'roles', 'categories'])
    for (const id of created[kind]) await call('/api/' + kind + '/' + id, 'DELETE', undefined, admin, 204);
  for (const id of created.users) {
    assert.match(id, /^[a-f0-9-]{36}$/i);
    execFileSync('sqlcmd', ['-S', '(localdb)\\mssqllocaldb', '-d', 'BlogManagementDb', '-E', '-I', '-b', '-Q',
      `SET XACT_ABORT ON; BEGIN TRANSACTION; DELETE FROM dbo.AuditLogs WHERE PerformedByUserId='${id}'; DELETE FROM dbo.RefreshTokens WHERE UserId='${id}'; DELETE FROM dbo.RoleUser WHERE UsersId='${id}'; DELETE FROM dbo.Users WHERE Id='${id}'; COMMIT TRANSACTION;`], { stdio: 'pipe' });
  }
  console.log('Temporary test records removed; existing accounts, articles and permissions preserved.');
}
