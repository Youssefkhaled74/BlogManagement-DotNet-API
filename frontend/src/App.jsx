import React, { useEffect, useState } from 'react';
import { getSession, saveSession, request, query, mediaUrl, uploadImage } from './api.js';
import { t, useLanguage, setLanguage, formatDate, formatNumber } from './i18n.js';
import ImagePicker, { Avatar } from './ImagePicker.jsx';

const statuses = ['Draft', 'PendingApproval', 'Approved', 'Published', 'Rejected'];
const actions = ['Create', 'Update', 'Delete', 'Publish', 'Unpublish', 'Approve', 'Reject', 'Submit', 'Activate', 'Deactivate', 'Assign'];
const statusName = value => typeof value === 'number' ? statuses[value] : value;
const date = formatDate;
function activityDetails(data) {
  try {
    const fields = Object.entries(JSON.parse(data || '{}')).filter(([key, value]) => !/ids?$|token|password/i.test(key) && value != null && typeof value !== 'object');
    return fields.length ? fields.map(([key, value]) => t(key[0].toLowerCase() + key.slice(1)) + ': ' + (typeof value === 'boolean' ? t(value ? 'Yes' : 'No') : t(String(value)))).join(' · ') : t('Changes recorded');
  } catch { return t('Changes recorded'); }
}
const nav = [['public', 'Published stories', null], ['blogs', 'Articles', 'blogs.read'], ['categories', 'Categories', 'categories.read'], ['users', 'Users', 'users.read'], ['employees', 'Employees', 'employees.read'], ['roles', 'Roles & permissions', 'roles.read'], ['audit-logs', 'Activity log', 'audit.read'], ['account', 'My account', null]];
const schemas = {
  blogs: [['title', 'Title', 'text', true, 3, 250], ['slug', 'Slug', 'text', false, 0, 250], ['categoryId', 'Category', 'category', true], ['content', 'Content', 'textarea', true, 10]],
  categories: [['name', 'Name', 'text', true, 2, 120], ['description', 'Description', 'textarea', false, 0, 500]],
  roles: [['name', 'Name', 'text', true, 2, 80], ['description', 'Description', 'textarea', false, 0, 300]],
  users: [['username', 'Username', 'text', true, 2, 80], ['email', 'Email', 'email', true], ['displayName', 'Display name', 'text', false, 0, 120]],
  employees: [['firstName', 'First name', 'text', true, 1, 100], ['lastName', 'Last name', 'text', true, 1, 100], ['employeeNumber', 'Employee number', 'text', false, 0, 50], ['department', 'Department', 'text', false, 0, 100], ['jobTitle', 'Job title', 'text', false, 0, 100], ['userId', 'Linked user account (optional)', 'user']],
};

function Fields({ schema, values, set, categories = [] }) {
  return schema.map(([key, label, type, required, minLength, maxLength]) => <label key={key}>{t(label)}{type === 'user' ? <select aria-label={t(label)} disabled={!categories.length} value={values[key] || ''} onChange={e => set({ ...values, [key]: e.target.value })}><option value="">{t('No linked account')}</option>{values[key] && !categories.some(user => user.id === values[key]) && <option value={values[key]}>{t('Linked account')}</option>}{categories.map(user => <option key={user.id} value={user.id}>{user.displayName || user.username} · {user.email}</option>)}</select> : type === 'category' ? <select aria-label={t(label)} required value={values[key] || ''} onChange={e => set({ ...values, [key]: e.target.value })}><option value="">{t("Choose a category")}</option>{categories.map(c => <option key={c.id} value={c.id}>{t(c.name)}</option>)}</select> : React.createElement(type === 'textarea' ? 'textarea' : 'input', { 'aria-label': t(label), dir: ['email','password'].includes(type) ? 'ltr' : 'auto', onInvalid: e => e.target.setCustomValidity(t('Please enter a valid value for {field}.', {field:t(label)})), onInput: e => e.target.setCustomValidity(''), type: type === 'textarea' ? undefined : type, required, minLength, maxLength, value: values[key] ?? '', onChange: e => set({ ...values, [key]: e.target.value }) })}</label>);
}
function Checks({ options, value, onChange }) {
  return <div className="checks">{options.map(o => <label key={o.value}><input type="checkbox" checked={value.includes(o.value)} onChange={e => onChange(e.target.checked ? [...value, o.value] : value.filter(x => x !== o.value))} />{t(o.label)}</label>)}</div>;
}

export default function App() {
  const language = useLanguage();
  const [session, setSession] = useState(getSession);
  const [me, setMe] = useState(null);
  const [page, setPage] = useState('public');
  const [authMode, setAuthMode] = useState(null);
  const [authError, setAuthError] = useState('');
  const [auth, setAuth] = useState({});
  const [rows, setRows] = useState([]);
  const [paging, setPaging] = useState({ page: 1, totalPages: 1, totalCount: 0 });
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState('');
  const [entity, setEntity] = useState('');
  const [version, setVersion] = useState(0);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [modal, setModal] = useState(null);
  const [values, setValues] = useState({});
  const [options, setOptions] = useState([]);
  const [profile, setProfile] = useState({});
  const [password, setPassword] = useState({});
  const [articleImage, setArticleImage] = useState(null);
  const [removeArticleImage, setRemoveArticleImage] = useState(false);
  const [profileImage, setProfileImage] = useState(null);
  const [removeProfileImage, setRemoveProfileImage] = useState(false);
  const can = permission => Boolean(me?.permissions?.includes(permission));
  useEffect(() => { const sync = () => setSession(getSession()); window.addEventListener('sessionchange', sync); return () => window.removeEventListener('sessionchange', sync); }, []);
  useEffect(() => {
    let active = true;
    setMe(null);
    if (session) request('/api/auth/me').then(data => { if (active) { setMe(data); setProfile({ username: data.username, email: data.email, displayName: data.displayName || '' }); } }).catch(e => { if (active) setError(e.message); });
    else if (page !== 'public') setPage('public');
    return () => { active = false; };
  }, [session]);
  useEffect(() => {
    if (page === 'account' || (page !== 'public' && !me)) return;
    let active = true;
    setLoading(true); setError('');
    const endpoint = page === 'public' ? 'public/blogs' : page;
    request(`/api/${endpoint}?${query({ page: paging.page, pageSize: 12, search: ['public', 'blogs', 'users', 'employees'].includes(page) ? search : '', status: page === 'blogs' ? filter : '', action: page === 'audit-logs' ? filter : '', entityName: page === 'audit-logs' ? entity : '' })}`)
      .then(data => { if (active) { setRows(Array.isArray(data) ? data : data.items); setPaging(p => ({ ...p, totalPages: data.totalPages || 1, totalCount: data.totalCount ?? data.length })); } })
      .catch(e => { if (active) { setError(e.message); setRows([]); } }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [page, me, paging.page, search, filter, entity, version]);

  async function run(work, message = 'Changes saved.', reportError = setError) {
    setBusy(true); setError(''); setNotice('');
    try { await work(); setNotice(message); setVersion(v => v + 1); return true; }
    catch (e) { reportError(e.message); return false; }
    finally { setBusy(false); }
  }
  function navigate(next) { setPage(next); setSearch(''); setFilter(''); setEntity(''); setRows([]); setPaging({ page: 1, totalPages: 1, totalCount: 0 }); setError(''); setNotice(''); }
  async function open(kind, row = null) {
    setArticleImage(null); setRemoveArticleImage(false);
    await run(async () => {
      let fresh = row;
      if (kind === 'userRoles') {
        const userId = page === 'employees' ? row.userId : row.id;
        if (!userId) throw new Error('This employee has no linked user account. Link an account first.');
        fresh = await request(`/api/users/${userId}`);
      } else if (row && ['edit', 'detail', 'permissions'].includes(kind)) fresh = await request(`/api/${page === 'public' ? 'public/blogs' : page}/${row.id}`);
      let choices = [];
      if (['edit', 'create'].includes(kind) && page === 'blogs') choices = await request('/api/categories');
      if (['edit', 'create'].includes(kind) && page === 'employees' && can('users.read')) {
        let result = await request('/api/users?pageSize=100');
        choices = result.items;
        for (let next = 2; next <= result.totalPages; next++)
          choices = choices.concat((await request(`/api/users?pageSize=100&page=${next}`)).items);
      }
      if (kind === 'permissions' || (kind === 'create' && page === 'roles')) choices = await request('/api/roles/permissions');
      if (kind === 'userRoles') choices = await request('/api/roles');
      if (kind === 'history') choices = await request(`/api/blogs/${row.id}/history`);
      setOptions(choices); setValues(kind === 'userRoles' ? { roleIds: choices.filter(r => fresh.roles.includes(r.name)).map(r => r.id) } : { ...fresh, permissions: fresh?.permissions || [], approved: true, comment: '' });
      setModal({ kind, row: fresh });
    }, '');
  }
  async function save(e) {
    e.preventDefault();
    const ok = await run(async () => {
      const { kind, row } = modal;
      const path = `/api/${page}${row ? `/${row.id}` : ''}`;
      if (kind === 'review') await request(`${path}/review`, 'POST', { approved: values.approved, comment: values.comment || null });
      else if (kind === 'permissions') await request(`${path}/permissions`, 'PUT', { permissions: values.permissions });
      else if (kind === 'userRoles') await request(`/api/users/${row.id}/roles`, 'PUT', { roleIds: values.roleIds });
      else {
        const body = Object.fromEntries(schemas[page].map(([key]) => [key, values[key] || null]));
        if (page === 'employees' && row) body.isActive = values.isActive;
        if (page === 'roles' && !row) body.permissions = values.permissions;
        const saved = await request(path, row ? 'PUT' : 'POST', body);
        if (page === 'blogs') {
          // Keep the saved ID if the upload fails, so retrying cannot create another article.
          setModal({ kind: 'edit', row: saved });
          if (articleImage) await uploadImage(`/api/blogs/${saved.id}/image`, articleImage);
          else if (removeArticleImage && saved.imageUrl) await request(`/api/blogs/${saved.id}/image`, 'DELETE');
        }
      }
      setModal(null);
    });
    return ok;
  }
  async function remove(row) { if (window.confirm(t('Delete {name}? This cannot be undone.', {name:row.title || row.name || row.firstName}))) await run(() => request(`/api/${page}/${row.id}`, 'DELETE'), 'Deleted.'); }
  const manager = can(`${page}.manage`);
  const isBlog = page === 'blogs' || page === 'public';
  const title = nav.find(n => n[0] === page)?.[1];
  const actionButton = (label, work, style = '') => <button disabled={busy} className={`small ${style}`} onClick={work}>{t(label)}</button>;

  return <div className="layout">
    <aside><a className="brand" href="#" onClick={e => { e.preventDefault(); navigate('public'); }}><span className="brand-icon">J.</span> journal<span className="brand-dot">®</span></a><p className="nav-label">{t("YOUR WORKSPACE")}</p><nav>{nav.filter(([id, , permission]) => id === 'public' || (session && (permission ? can(permission) : true))).map(([id, label]) => <button className={page === id ? 'active' : ''} key={id} onClick={() => navigate(id)}><span>{({ public: '◈', blogs: '▤', categories: '▦', users: '◎', employees: '◇', roles: '⚿', 'audit-logs': '◷', account: '○' })[id]}</span>{t(label)}</button>)}</nav><div className="aside-bottom"><p>{t("Ideas deserve")}<br /><strong>{t("a place to grow.")}</strong></p>{session ? <><div className="identity"><Avatar url={me?.profileImageUrl} name={me?.username || 'User'} /><div><strong>{me?.username || t('Loading…')}</strong><small>{me?.roles?.map(role => t(role)).join('، ')}</small></div></div><button className="ghost" disabled={busy} onClick={() => run(async () => { await request('/api/auth/logout', 'POST', { refreshToken: session.refreshToken }); saveSession(null); navigate('public'); }, 'Signed out.')}>{t("Sign out")}</button></> : <button onClick={() => { setAuth({}); setAuthError(''); setAuthMode('login'); }}>{t("Sign in to workspace ↗")}</button>}</div></aside>
    <main><header><span>{t("THE EDITORIAL WORKSPACE")}</span><div className="header-controls"><button className="language-switch" aria-label={t("Language")} onClick={() => setLanguage(language === "en" ? "ar" : "en")}>{language === "en" ? "العربية" : "English"}</button></div></header><section className="page-heading"><div><p className="eyebrow">{t(page === 'public' ? 'READ. DISCOVER. GET INSPIRED.' : 'MAKE SOMETHING WORTH READING.')}</p><h1>{t(title)}<span>.</span></h1><p>{t(page === 'public' ? 'Fresh perspectives, thoughtful stories, and ideas from our community.' : 'Everything you need to keep your editorial workspace moving.')}</p></div>{(manager && ['categories', 'roles', 'employees'].includes(page) || page === 'blogs' && can('blogs.create')) && <button className="primary" disabled={busy} onClick={() => open('create')}>＋ {t(({blogs:'New article',categories:'New category',roles:'New role',employees:'New employee'})[page])}</button>}</section>
      {error && <div className="alert error" role="alert">{t(error)}<button onClick={() => setError('')}>×</button></div>}{notice && <div className="alert success" role="status">{t(notice)}<button onClick={() => setNotice('')}>×</button></div>}
      {page === 'account' ? <div className="account-grid"><form className="panel" onSubmit={e => { e.preventDefault(); run(async () => { let user = await request('/api/auth/profile', 'PUT', profile); if (profileImage) user = await uploadImage('/api/auth/profile/image', profileImage); else if (removeProfileImage) user = await request('/api/auth/profile/image', 'DELETE'); setProfileImage(null); setRemoveProfileImage(false); saveSession({ ...getSession(), user }); setProfile({ username: user.username, email: user.email, displayName: user.displayName || '' }); }); }}><h2>{t("Your profile")}</h2><Avatar url={me?.profileImageUrl} name={me?.username || 'User'} large /><ImagePicker label={t("Profile image")} current={me?.profileImageUrl} file={profileImage} onFile={file => { setProfileImage(file); setRemoveProfileImage(false); }} remove={removeProfileImage} onRemove={() => { setProfileImage(null); setRemoveProfileImage(Boolean(me?.profileImageUrl)); }} disabled={busy} /><Fields schema={schemas.users} values={profile} set={setProfile} /><button className="primary" disabled={busy}>{t("Save profile")}</button></form><form className="panel" onSubmit={e => { e.preventDefault(); run(async () => { await request('/api/auth/change-password', 'POST', password); setPassword({}); }, 'Password changed.'); }}><h2>{t("Security")}</h2><Fields schema={ [['currentPassword', 'Current password', 'password', true], ['newPassword', 'New password', 'password', true, 8, 100]] } values={password} set={setPassword} /><button className="primary" disabled={busy}>{t("Change password")}</button></form></div> : <>
        <div className="toolbar"><div className="count"><strong>{formatNumber(paging.totalCount)}</strong> {t(isBlog ? 'stories' : 'records')}<span className="muted"> / {page === 'public' ? t('Published collection') : t('Workspace')}</span></div><div className="filters">{['public', 'blogs', 'users', 'employees'].includes(page) && <input aria-label={t("Search")} placeholder={t("Search…")} value={search} onChange={e => { setSearch(e.target.value); setPaging(p => ({ ...p, page: 1 })); }} />}{['blogs', 'audit-logs'].includes(page) && <select aria-label={t("Filter status or action")} value={filter} onChange={e => { setFilter(e.target.value); setPaging(p => ({ ...p, page: 1 })); }}><option value="">{t(page === 'blogs' ? 'All statuses' : 'All actions')}</option>{(page === 'blogs' ? statuses : actions).map((s, i) => <option key={s} value={i}>{t(s)}</option>)}</select>}{page === 'audit-logs' && <input aria-label={t("Entity name")} placeholder={t("Entity name…")} value={entity} onChange={e => { setEntity(e.target.value); setPaging(p => ({ ...p, page: 1 })); }} />}<button className="ghost" onClick={() => setVersion(v => v + 1)}>{t("↻ Refresh")}</button></div></div>
        {loading ? <div className="empty" role="status">{t("Loading your workspace…")}</div> : rows.length === 0 ? <div className="empty"><span>✧</span><h2>{error ? t('Unable to load records') : t('A little room for something new')}</h2><p>{error ? t('Please refresh the page and try again.') : t('No records match this view. Try another search or create your first record.')}</p></div> : isBlog ? <div className="cards">{rows.map((row, i) => { const status = statusName(row.status); const owns = row.authorId === me?.id; const editable = (owns || can('blogs.review')) && !['PendingApproval', 'Published'].includes(status); return <article className="story" key={row.id}><div className={`story-art art-${i % 4}`}>{row.imageUrl && <img className="story-cover" src={mediaUrl(row.imageUrl)} alt={row.title} loading="lazy" />}<span>{t(row.categoryName)}</span>{!row.imageUrl && <div>{['Aa', '✳', '〰', '↗'][i % 4]}</div>}</div><div className="story-body"><div className="story-meta"><span className={`badge ${status}`}>{t(status === 'PendingApproval' ? 'Pending review' : status)}</span><small>{formatDate(row.publishedAt || row.createdAt, true)}</small></div><h2><button className="title-link" dir="auto" onClick={() => open('detail', row)}>{row.title}</button></h2><p className="excerpt" dir="auto">{row.content.slice(0, 150)}{row.content.length > 150 ? '…' : ''}</p><div className="byline"><Avatar url={row.authorImageUrl} name={row.authorName} />{row.authorName}</div><div className="row-actions">{actionButton('Read ↗', () => open('detail', row))}{page === 'blogs' && <>{can('blogs.update') && editable && actionButton('Edit', () => open('edit', row))}{can('blogs.delete') && editable && actionButton('Delete', () => remove(row), 'danger')}{can('blogs.submit') && owns && ['Draft', 'Rejected'].includes(status) && actionButton('Submit', () => run(() => request(`/api/blogs/${row.id}/submit`, 'POST')))}{can('blogs.review') && status === 'PendingApproval' && actionButton('Review', () => open('review', row))}{can('blogs.publish') && ['Approved', 'Published'].includes(status) && actionButton(status === 'Published' ? 'Unpublish' : 'Publish', () => run(() => request(`/api/blogs/${row.id}/${status === 'Published' ? 'unpublish' : 'publish'}`, 'POST')))}{can('blogs.review') && actionButton('History', () => open('history', row))}</>}</div></div></article>; })}</div> : <div className="table-wrap"><table><thead><tr><th>{t(page === 'audit-logs' ? 'Action / entity' : 'Name')}</th><th>{t("Details")}</th><th>{t(page === 'audit-logs' ? 'When' : 'Actions')}</th></tr></thead><tbody>{rows.map(row => <tr key={row.id}><td><strong>{row.name || row.displayName || row.username || `${row.firstName || ''} ${row.lastName || ''}`.trim() || `${t(actions[row.action] || row.action)} · ${t(row.entityName)}`}</strong><small>{row.email || row.employeeNumber || ''}</small></td><td>{page === 'categories' ? <>{row.description || t('No description')}<small>{formatNumber(row.blogsCount)} {t('articles')}</small></> : page === 'roles' ? <>{row.description}<small>{formatNumber(row.permissions.length)} {t('permissions')}</small></> : page === 'users' ? <>{row.roles.map(tRole => t(tRole)).join('، ')}<small>{t(row.isActive ? 'Active' : 'Inactive')}</small></> : page === 'employees' ? <>{row.jobTitle || '—'} · {row.department || '—'}<small>{t(row.isActive ? 'Active' : 'Inactive')}</small></> : <>{row.performedBy || t('System')}<small>{activityDetails(row.data)}</small></>}</td><td><div className="row-actions">{page === 'audit-logs' ? date(row.timestamp) : <>{actionButton('View', () => open('detail', row))}{manager && actionButton('Edit', () => open('edit', row))}{page === 'employees' && can('users.manage') && can('users.read') && can('roles.read') && (row.userId ? <button type="button" disabled={busy || row.userId === me?.id} title={row.userId === me?.id ? t('You cannot change your own roles.') : undefined} onClick={() => open('userRoles', row)}>{t('Assign roles')}</button> : <small title={t('This employee has no linked user account. Link an account first.')}>{t('No linked account')}</small>)}{manager && page !== 'users' && actionButton('Delete', () => remove(row), 'danger')}{manager && page === 'roles' && actionButton('Permissions', () => open('permissions', row))}{manager && page === 'users' && <>{actionButton(row.isActive ? 'Deactivate' : 'Activate', () => run(() => request(`/api/users/${row.id}/status`, 'PATCH', { isActive: !row.isActive })))}{can('roles.read') && actionButton('Assign roles', () => open('userRoles', row))}</>}</>}</div></td></tr>)}</tbody></table></div>}
        <div className="pagination"><span>{t('Page {page} of {total}', {page:formatNumber(paging.page),total:formatNumber(paging.totalPages)})}</span><button disabled={loading || paging.page <= 1} onClick={() => setPaging(p => ({ ...p, page: p.page - 1 }))}>{t("← Previous")}</button><button disabled={loading || paging.page >= paging.totalPages} onClick={() => setPaging(p => ({ ...p, page: p.page + 1 }))}>{t("Next →")}</button></div>
      </>}<footer>{t("JOURNAL / A SPACE FOR YOUR IDEAS")}<span>{t("Built for thoughtful publishing.")}</span></footer>
    </main>
    {authMode && <div className="overlay"><section className="modal auth-modal" role="dialog" aria-modal="true" aria-label={t("Authentication")}><button aria-label={t("Close")} className="close" onClick={() => setAuthMode(null)}>×</button><p className="eyebrow">{t("WELCOME TO JOURNAL")}</p><h2>{authMode === 'login' ? t('Good to see you again.') : t('Start your next chapter.')}</h2><p>{t("Sign in to write, review, and manage your workspace.")}</p>{authError && <div role="alert" className="alert error">{t(authError)}</div>}<form onSubmit={e => { e.preventDefault(); run(async () => { const result = await request(`/api/auth/${authMode}`, 'POST', auth); saveSession(result); setAuthMode(null); navigate('public'); }, t('Welcome to your workspace.'), setAuthError); }}><Fields schema={[...(authMode === 'register' ? [['username', t('Username'), 'text', true, 2, 80]] : []), ['email', t('Email'), 'email', true], ['password', t('Password'), 'password', true, authMode === 'register' ? 8 : undefined, 100]]} values={auth} set={setAuth} /><button className="primary" disabled={busy}>{busy ? t('Please wait…') : authMode === 'login' ? t('Sign in ↗') : t('Create account ↗')}</button></form><button className="ghost" onClick={() => { setAuth({}); setAuthError(''); setAuthMode(authMode === 'login' ? 'register' : 'login'); }}>{t(authMode === 'login' ? 'New here? Create an account' : 'Already registered? Sign in')}</button></section></div>}
    {modal && <div className="overlay"><section className="modal" role="dialog" aria-modal="true" aria-label={t("JOURNAL WORKSPACE")}><button aria-label={t("Close")} className="close" disabled={busy} onClick={() => setModal(null)}>×</button><p className="eyebrow">{t("JOURNAL WORKSPACE")}</p><h2>{modal.kind === 'detail' ? modal.row.title || modal.row.name || modal.row.username || `${modal.row.firstName} ${modal.row.lastName}` : ({ create: t('Create a record'), edit: t('Edit record'), review: t('Review article'), history: t('Approval history'), permissions: t('Role permissions'), userRoles: t('Assign user roles') })[modal.kind]}</h2>{error && <div className="alert error" role="alert">{t(error)}</div>}{modal.kind === 'detail' ? isBlog ? <><div className="story-meta">{modal.row.authorName} · {t(modal.row.categoryName)} · {t(statusName(modal.row.status))}</div>{modal.row.imageUrl && <img className="article-cover" src={mediaUrl(modal.row.imageUrl)} alt={modal.row.title} />}<div className="article-content" dir="auto">{modal.row.content}</div>{modal.row.reviewComment && <blockquote>{modal.row.reviewComment}</blockquote>}</> : <dl>{Object.entries(modal.row).filter(([key]) => !/id$|imageurl$/i.test(key)).map(([key, value]) => <React.Fragment key={key}><dt>{t(key)}</dt><dd>{Array.isArray(value) ? value.map(item => t(item)).join('، ') : typeof value === 'boolean' ? t(value ? 'Yes' : 'No') : String(value ?? '—')}</dd></React.Fragment>)}</dl> : modal.kind === 'history' ? options.length ? options.map(o => <div className="history" key={o.id}><strong>{t(statusName(o.decision))} · {o.reviewedBy}</strong><small>{date(o.timestamp)}</small><p>{o.comment || t('No comment')}</p></div>) : <p>{t("No reviews yet.")}</p> : <form onSubmit={save}>{['create', 'edit'].includes(modal.kind) && <><Fields schema={schemas[page]} values={values} set={setValues} categories={options} />{page === 'blogs' && <ImagePicker label={t("Article image")} current={modal.row?.imageUrl} file={articleImage} onFile={file => { setArticleImage(file); setRemoveArticleImage(false); }} remove={removeArticleImage} onRemove={() => { setArticleImage(null); setRemoveArticleImage(Boolean(modal.row?.imageUrl)); }} disabled={busy} />}{page === 'employees' && modal.row && <label className="check"><input type="checkbox" checked={values.isActive} onChange={e => setValues({ ...values, isActive: e.target.checked })} />{t("Active employee")}</label>}</>}{(modal.kind === 'permissions' || page === 'roles' && modal.kind === 'create') && <><p>{t("Select permissions")}</p><Checks options={options.map(o => ({ value: o.name, label: o.name }))} value={values.permissions} onChange={permissions => setValues({ ...values, permissions })} /></>}{modal.kind === 'userRoles' && <><p>{t("Role changes take effect the next time the user signs in.")}</p><Checks options={options.map(o => ({ value: o.id, label: o.name }))} value={values.roleIds} onChange={roleIds => setValues({ ...values, roleIds })} /></>}{modal.kind === 'review' && <><label>{t("Decision")}<select value={String(values.approved)} onChange={e => setValues({ ...values, approved: e.target.value === 'true' })}><option value="true">{t("Approve")}</option><option value="false">{t("Reject")}</option></select></label><Fields schema={ [['comment', t('Review comment'), 'textarea', false, 0, 1000]] } values={values} set={setValues} /></>}<div className="modal-actions"><button type="button" disabled={busy} onClick={() => setModal(null)}>{t("Cancel")}</button><button className="primary" disabled={busy}>{busy ? t('Saving…') : t('Save changes ↗')}</button></div></form>}</section></div>}
  </div>;
}
