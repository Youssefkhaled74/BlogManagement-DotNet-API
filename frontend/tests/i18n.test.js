import { test } from 'node:test';
import assert from 'node:assert/strict';
import { setLanguage, getLanguage, t, formatNumber, formatDate } from '../src/i18n.js';

test('language switching translates UI and preserves unknown user content', () => {
  setLanguage('ar');
  assert.equal(getLanguage(), 'ar');
  assert.equal(t('Categories'), 'التصنيفات');
  assert.equal(t('My own article title'), 'My own article title');
  assert.equal(t('users.read'), 'عرض المستخدمين');
  assert.match(formatNumber(123), /١٢٣/);
  assert.ok(formatDate('2026-10-04T12:00:00Z').length > 0);
  setLanguage('en');
  assert.equal(t('التصنيفات'), 'Categories');
  assert.equal(formatNumber(123), '123');
});
