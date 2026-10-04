import React, { useEffect, useId, useRef, useState } from 'react';
import { t, formatNumber } from './i18n.js';
import { mediaUrl } from './api.js';

export function Avatar({ url, name = 'User', large = false }) {
  return url ? <img className={`avatar ${large ? 'large' : ''}`} src={mediaUrl(url)} alt={t("{name}'s profile",{name})} /> : <span className={`avatar ${large ? 'large' : ''}`}>{name[0]?.toUpperCase()}</span>;
}

export default function ImagePicker({ label, current, file, onFile, remove, onRemove, disabled }) {
  const [preview, setPreview] = useState('');
  const [error, setError] = useState('');
  const [dragging, setDragging] = useState(false);
  const input = useRef(null);
  const id = useId();
  useEffect(() => {
    if (!file) { setPreview(''); return; }
    const url = URL.createObjectURL(file);
    setPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);
  const url = preview || (!remove && current ? mediaUrl(current) : '');
  function select(selected) {
    if (!selected || disabled) return;
    setError('');
    if (selected.size <= 0 || selected.size > 5 * 1024 * 1024 || !['image/png', 'image/jpeg', 'image/webp'].includes(selected.type)) {
      setError('Choose a PNG, JPEG, or WebP image smaller than 5 MB.');
      return;
    }
    onFile(selected);
  }
  return <div className="image-picker upload-card">
    <span id={id + '-label'} className="upload-label">{label}</span>
    <input ref={input} id={id} className="upload-input" type="file" accept="image/png,image/jpeg,image/webp"
      aria-label={label} aria-describedby={id + '-hint'} disabled={disabled}
      onChange={e => { select(e.target.files[0]); e.target.value = ''; }} />
    <div className={`upload-zone ${dragging ? 'dragging' : ''} ${url ? 'has-image' : ''}`}
      onDragOver={e => { e.preventDefault(); if (!disabled) setDragging(true); }}
      onDragLeave={e => { if (!e.currentTarget.contains(e.relatedTarget)) setDragging(false); }}
      onDrop={e => { e.preventDefault(); setDragging(false); select(e.dataTransfer.files[0]); }}>
      {url ? <img src={url} alt={t('{label} preview', {label})} className="upload-preview" />
        : <span className="upload-icon" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6"><rect x="3" y="3" width="18" height="18" rx="4"/><circle cx="8" cy="8" r="1.5"/><path d="m3 17 5-5 4 4 4-6 5 7"/></svg></span>}
      <div className="upload-copy"><strong>{t(dragging ? 'Drop your image here' : url ? 'Your image is ready' : 'Add a little personality')}</strong>
        <p>{t(url ? 'Choose another image to replace it.' : 'Drag an image here, or choose one from your device.')}</p>
        <button type="button" className="upload-browse" disabled={disabled} onClick={() => input.current?.click()}>{t(url ? 'Change image' : 'Choose image')} <span aria-hidden="true">↗</span></button>
      </div>
    </div>
    <div className="upload-details"><small id={id + '-hint'}>{t('PNG, JPEG, WebP · Maximum 5 MB')}</small>
      {(file || current && !remove) && <button type="button" className="upload-remove" disabled={disabled} onClick={() => { setError(''); onRemove(); }}>{t('Remove image')}</button>}
    </div>
    {file && <div className="upload-file" role="status"><span aria-hidden="true">✓</span><span>{file.name}</span><small>{formatNumber(Math.max(1, Math.round(file.size / 1024)))} {t('KB')}</small></div>}
    {error && <p className="upload-error" role="alert">{t(error)}</p>}
    {remove && <small role="status">{t('The existing image will be removed when you save.')}</small>}
    {file && <small>{t('Your image will be uploaded when you save.')}</small>}
  </div>;
}
