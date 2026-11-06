'use strict';
// Tools/Server/Console/web/console.js: the release console's page. It reads /api/status every few seconds and a job's
// log as it grows, and sends every action as JSON (console.py takes nothing else). Everything shown goes in as text.

const $ = (id) => document.getElementById(id);
const SOURCES = { working: "this PC's working copy", main: "GitHub's main" };
let state = null;       // the last status
let chosen = null;      // the release the Deploy, Publish, Push and Check buttons act on (null: the newest)
let viewing = null;     // the job whose log is shown
let offset = 0;         // how much of its log is shown
let logTimer = 0;
let lastStatus = null;  // the viewed job's status, to notice it ending
let sourceSet = false;

function h(tag, attrs, ...kids) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v === null || v === undefined || v === false) continue;
    if (k === 'class') el.className = v;
    else if (k.startsWith('on')) el.addEventListener(k.slice(2), v);
    else el.setAttribute(k, v === true ? '' : v);
  }
  for (const kid of kids.flat(2)) {
    if (kid !== null && kid !== undefined && kid !== false) el.append(kid instanceof Node ? kid : String(kid));
  }
  return el;
}

async function api(path, body) {
  const init = body === undefined ? { cache: 'no-store' }
    : { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) };
  const r = await fetch('/api/' + path, init);
  let data = null;
  try { data = await r.json(); } catch (e) { /* not JSON */ }
  if (!r.ok) throw new Error((data && data.error) || `${r.status} ${r.statusText}`);
  return data;
}

let toastTimer = 0;
function toast(text, good) {
  const t = $('toast');
  t.textContent = text;
  t.className = 'toast' + (good ? ' good' : '');
  t.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { t.hidden = true; }, good ? 3500 : 9000);
}

const size = (bytes) => bytes == null ? '?' : bytes >= 2 ** 30 ? (bytes / 2 ** 30).toFixed(2) + ' GB' : Math.round(bytes / 2 ** 20) + ' MB';
const stampOf = (version) => { const m = /\+(.+)$/.exec(version || ''); return m ? m[1] : version || null; };
const hostOf = (url) => { try { return new URL(url).host; } catch (e) { return url; } };
function when(iso) {
  if (!iso) return '?';
  const d = new Date(iso);
  return isNaN(d) ? iso : d.toLocaleString(undefined, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
}
function took(job) {
  const s = Math.max(0, Math.round(((job.ended ? new Date(job.ended) : new Date()) - new Date(job.started)) / 1000));
  return s >= 60 ? `${Math.floor(s / 60)} min ${s % 60} s` : `${s} s`;
}

// --- Status ----------------------------------------------------------------------------------------------------------------

async function refresh(fresh) {
  try {
    state = await api('status' + (fresh ? '?fresh=1' : ''));
    render(state);
  } catch (e) {
    $('now').replaceChildren(h('div', { class: 'bad' }, "Can't reach the console: " + e.message));
  }
}

function render(s) {
  document.title = `WithTheDead release console \u00b7 ${s.console.host}`;
  $('host').textContent = s.console.host;
  for (const el of document.querySelectorAll('.hostname')) el.textContent = s.console.host + ' (this PC)';
  if (!sourceSet) {
    for (const r of document.querySelectorAll('input[name=source]')) r.checked = r.value === s.settings.source;
    sourceSet = true;
  }
  renderNow(s);
  renderSources(s);
  renderReleases(s);
  renderJobs(s);
  renderCode(s);
  renderButler(s);
  renderButtons(s);
  $('footer').replaceChildren('Logs, history and settings in ', h('code', {}, s.console.data), ' \u00b7 runs ',
    h('code', {}, s.console.project + '\\Tools\\Server\\Release.ps1'), ' \u00b7 see Tools/Server/README.md, \u201cRelease console\u201d');
}

function fact(label, value, cls, ...sub) {
  return h('div', { class: 'fact' }, h('div', { class: 'label' }, label), h('div', { class: 'value ' + (cls || '') }, value),
    sub.length ? h('div', { class: 'sub' }, sub) : null);
}

function renderNow(s) {
  const out = [];
  const sv = s.server || {};
  if (sv.error) out.push(fact('The server runs', 'unknown', 'bad', sv.error));
  else {
    out.push(fact('The server runs', sv.release || 'nothing', sv.state === 'running' ? '' : 'bad',
      sv.state === 'running' ? `running since ${when(sv.since)}` : `its container is ${sv.state}`,
      sv.status ? [h('br'), sv.status] : null));
  }
  const lan = s.site.lan || {}, pub = s.site.public || {};
  const page = stampOf(lan.version);
  if (lan.error) out.push(fact('The download page hands out', 'unknown', 'bad', lan.error));
  else {
    const same = pub.error ? [`${hostOf(pub.url || '')}: ${pub.error}`] : pub.version === lan.version
      ? [`${hostOf(pub.url)} hands out the same`] : [`but ${hostOf(pub.url)} hands out ${stampOf(pub.version)}`];
    out.push(fact('The download page hands out', page || 'nothing', sv.release && page !== sv.release ? 'warn' : '',
      `published ${when(lan.published)}`, h('br'), same,
      sv.release && page !== sv.release ? [h('br'), 'not the build the server runs'] : null));
  }
  const newest = (s.releases || [])[0];
  out.push(fact('Newest release on this PC', newest ? newest.stamp : 'none', '',
    newest ? (newest.stamp === sv.release ? 'the server runs it' : 'the server runs another') : 'build one'));
  const g = s.git || {};
  if (g.error) out.push(fact("GitHub's main", 'unknown', 'bad', g.error));
  else {
    out.push(fact("GitHub's main", g.main.commit, '', g.main.subject, h('br'),
      g.working.behind ? `this PC's copy is ${g.working.behind} commit${g.working.behind === 1 ? '' : 's'} behind` : "this PC's copy has it"));
  }
  $('now').replaceChildren(...out);
}

function renderSources(s) {
  const g = s.git || {};
  if (g.error) { $('src-working').textContent = $('src-main').textContent = ''; return; }
  const w = g.working;
  $('src-working').textContent = `${w.branch} at ${w.commit}, ${w.changed} changed file${w.changed === 1 ? '' : 's'}` +
    (w.behind ? `, ${w.behind} commit${w.behind === 1 ? '' : 's'} behind GitHub's main` : '');
  const c = g.checkout;
  $('src-main').textContent = `${g.main.commit} \u201c${g.main.subject}\u201d, ${when(g.main.date)}. Fetched first. ` +
    (c.exists ? `The checkout is ${c.path}.` : `The first such build makes the checkout, ${c.path}.`);
}

function pick(s) {
  const rows = s.releases || [];
  if (chosen && !rows.some((r) => r.stamp === chosen)) chosen = null;
  return chosen || (rows.find((r) => r.newest) || rows[0] || {}).stamp || null;
}

function renderReleases(s) {
  const rows = s.releases || [];
  const table = $('releases');
  if (s.releases && s.releases.error) { table.replaceChildren(h('tr', {}, h('td', { class: 'bad' }, s.releases.error))); return; }
  if (!rows.length) { table.replaceChildren(h('tr', {}, h('td', { class: 'sub' }, 'No releases yet: build one.'))); return; }
  const picked = pick(s);
  const page = stampOf((s.site.lan || {}).version);
  const yes = (v) => v ? h('span', { class: 'ok' }, '\u2713') : h('span', { class: 'bad' }, '\u2717');
  table.replaceChildren(
    h('thead', {}, h('tr', {}, h('th', {}, ''), h('th', {}, 'Release'), h('th', {}, 'Client'), h('th', {}, 'Server'),
      h('th', {}, 'Size'))),
    h('tbody', {}, rows.map((r) => h('tr', { class: r.stamp === picked ? 'chosen' : '', title: r.version || '',
      onclick: () => { chosen = r.stamp; renderReleases(state); renderButtons(state); } },
      h('td', {}, h('input', { type: 'radio', name: 'stamp', value: r.stamp, checked: r.stamp === picked, 'aria-label': r.stamp })),
      h('td', { class: 'release' }, h('b', {}, r.stamp),
        h('div', { class: 'sub' }, `its game joins ${r.joins || '?'}`),
        r.newest || r.stamp === (s.server || {}).release || r.stamp === page ? h('div', {},
          r.newest ? h('span', { class: 'tag' }, 'newest') : null,
          r.stamp === (s.server || {}).release ? h('span', { class: 'tag on' }, 'on the server') : null,
          r.stamp === page ? h('span', { class: 'tag on' }, 'on the page') : null) : null),
      h('td', {}, yes(r.client)), h('td', {}, yes(r.server)), h('td', {}, size(r.size))))));
}

function renderCode(s) {
  const g = s.git || {};
  if (g.error) { $('code').replaceChildren(h('div', { class: 'bad' }, g.error)); return; }
  const w = g.working, m = g.main, c = g.checkout;
  $('code').replaceChildren(
    h('div', {}, h('span', { class: 'label' }, "This PC's working copy"), h('b', {}, w.branch), ' at ', h('code', {}, w.commit),
      ' ', w.subject, h('div', { class: 'sub' }, `${w.changed} changed file${w.changed === 1 ? '' : 's'} not committed \u00b7 ` +
        `${w.behind} behind and ${w.ahead} ahead of GitHub's main \u00b7 ${w.path}`)),
    h('div', {}, h('span', { class: 'label' }, "GitHub's main"), h('code', {}, m.commit), ' ', m.subject,
      h('div', { class: 'sub' }, `committed ${when(m.date)} \u00b7 ` + (m.fetched ? `last fetched ${when(m.fetched)}` : 'never fetched'))),
    h('div', {}, h('span', { class: 'label' }, 'Clean checkout for GitHub-main builds'),
      c.exists ? [h('code', {}, c.commit || '?'), ' ', c.subject || c.error || ''] : 'not made yet: the first GitHub-main build makes it',
      h('div', { class: 'sub' }, c.path)));
}

function renderButler(s) {
  const b = s.butler || {};
  const st = s.settings || {};
  if (b.error) { $('butler').replaceChildren(h('div', { class: 'bad' }, b.error)); return; }
  const login = b.credentials ? h('span', { class: 'ok' }, 'logged in') : b.apiKey ? h('span', { class: 'ok' }, 'uses the API key saved here')
    : b.envKey ? h('span', { class: 'ok' }, 'uses BUTLER_API_KEY') : h('span', { class: 'warn' }, 'not logged in');
  $('butler').replaceChildren(
    h('div', {}, h('span', { class: 'label' }, 'butler'),
      b.installed ? [h('b', {}, b.version || '?')] : h('span', { class: 'warn' }, 'not installed'),
      h('div', { class: 'sub' }, b.installed ? b.path : `Install butler puts it in ${b.path}`)),
    h('div', {}, h('span', { class: 'label' }, 'itch.io login'), login,
      h('div', { class: 'sub' }, b.credentials ? `butler's login: ${b.creds}` : st.apiKey ? `API key in ${st.folder}` : `butler keeps its login in ${b.creds}`)),
    h('div', {}, h('span', { class: 'label' }, 'itch.io project'),
      st.itch ? h('b', {}, st.itch) : h('span', { class: 'warn' }, 'not set'),
      h('div', { class: 'sub' }, st.itch ? `pushes go to its windows channel (itch.io/${st.itch.split('/')[0]} \u2192 ${st.itch.split('/')[1]})`
        : 'Push to itch.io waits until it is set')));
  const input = $('itch-project');
  if (document.activeElement !== input && !input.dataset.edited) input.value = st.itch || '';
}

function renderButtons(s) {
  const busy = !!s.job;
  const rows = s.releases || [];
  const row = rows.find((r) => r.stamp === pick(s));
  const b = s.butler || {};
  const itch = (s.settings || {}).itch;
  const canPush = !!itch && !!b.loggedIn;
  for (const id of ['do-release', 'do-build', 'do-fetch', 'do-butler', 'do-login']) $(id).disabled = busy;
  $('do-deploy').disabled = busy || !row || !row.server;
  $('do-publish').disabled = busy || !row || !row.client;
  $('do-itch').disabled = busy || !row || !row.client || !canPush;
  $('do-check').disabled = busy || !row;
  $('do-butler').textContent = b.installed ? 'Upgrade butler' : 'Install butler';
  const also = $('also-itch');
  also.disabled = !canPush;
  if (!canPush) also.checked = false;
  $('itch-why').textContent = !itch ? '(set the itch.io project below first)' : !b.loggedIn ? '(log butler in first, below)'
    : `(${itch}, windows channel)`;
}

// --- Jobs ------------------------------------------------------------------------------------------------------------------

function renderJobs(s) {
  const jobs = s.jobs || [];
  const live = viewing && jobs.find((j) => j.id === viewing);
  // A running job takes the log over, unless another one that's running is shown.
  if (s.job && s.job.id !== viewing && !(live && (live.status === 'running' || live.status === 'cancelling'))) view(s.job.id);
  else if (!viewing && jobs.length) view(jobs[0].id);
  $('history').replaceChildren(...jobs.map((j) => h('li', { class: j.id === viewing ? 'viewing' : '', onclick: () => view(j.id) },
    h('span', { class: 'badge ' + j.status }, j.status), h('span', {}, j.title), h('span', { class: 'sub when' }, when(j.started)))));
  if (!jobs.length) $('history').replaceChildren(h('li', { class: 'sub' }, 'Nothing has run yet.'));
  renderJobHead();
}

function renderJobHead(status) {
  const job = state && (state.jobs || []).find((j) => j.id === viewing);
  const head = $('job-head');
  if (!job) { head.replaceChildren('No job yet. One runs at a time.'); $('do-cancel').hidden = true; return; }
  const st = status || job.status;
  const live = st === 'running' || st === 'cancelling';
  head.replaceChildren(h('span', { class: 'badge ' + st }, st), h('span', { class: 'title' }, job.title),
    h('span', {}, `started ${when(job.started)}, ${live ? 'running for' : 'took'} ${took(job)}` +
      (job.exit !== null && job.exit !== undefined && !live ? `, exit ${job.exit}` : '')));
  const cancel = $('do-cancel');
  cancel.hidden = !(state.job && state.job.id === job.id);
  cancel.disabled = st === 'cancelling';
}

function view(id) {
  if (viewing !== id) {
    viewing = id;
    offset = 0;
    lastStatus = null;
    $('log').textContent = '';
    for (const li of $('history').children) li.classList.remove('viewing');
  }
  pollLog();
}

async function pollLog() {
  clearTimeout(logTimer);
  const id = viewing;
  if (!id) return;
  try {
    const d = await api(`jobs/${id}/log?from=${offset}`);
    if (id !== viewing) return;
    const log = $('log');
    const atBottom = log.scrollHeight - log.scrollTop - log.clientHeight < 60;
    if (d.text) {
      log.append(d.text);
      if (atBottom) log.scrollTop = log.scrollHeight;
    }
    offset = d.next;
    const live = d.status === 'running' || d.status === 'cancelling';
    if (state) {
      const job = (state.jobs || []).find((j) => j.id === id);
      if (job) job.status = d.status;
      renderJobHead(d.status);
    }
    // It has just ended: what it changed (a release, butler, the server) shows at once.
    if (lastStatus && lastStatus !== d.status && !live) refresh(true);
    lastStatus = d.status;
    if (d.next < d.size) logTimer = setTimeout(pollLog, 30);
    else if (live) logTimer = setTimeout(pollLog, 1000);
  } catch (e) {
    logTimer = setTimeout(pollLog, 3000);
  }
}

async function start(body, question) {
  if (question && !confirm(question)) return;
  try {
    const d = await api('jobs', body);
    view(d.job.id);
    await refresh();
  } catch (e) {
    toast(e.message);
  }
}

const source = () => (document.querySelector('input[name=source]:checked') || {}).value || 'working';
const serverNow = () => {
  const sv = (state && state.server) || {};
  return sv.status ? `\n\nThe server says: ${sv.status}.` : '';
};

// --- Wiring ----------------------------------------------------------------------------------------------------------------

function wire() {
  $('do-refresh').addEventListener('click', () => refresh(true));
  $('do-release').addEventListener('click', () => {
    const itch = $('also-itch').checked;
    start({ action: 'release', source: source(), itch, confirm: true },
      `Release from ${SOURCES[source()]}?\n\nThis builds the game and its server, then restarts the server on hts-llm: ` +
      `a raid under way on it is lost.${serverNow()}\n\nThen it uploads the game (about 1.2 GB) to the download page` +
      (itch ? ` and pushes it to itch.io (${state.settings.itch}).` : '.'));
  });
  $('do-build').addEventListener('click', () => start({ action: 'build', source: source() }));
  $('do-deploy').addEventListener('click', () => {
    const stamp = pick(state);
    start({ action: 'deploy', stamp, confirm: true }, `Deploy ${stamp} to the server?\n\nThe server restarts: a raid ` +
      `under way on it is lost.${serverNow()}\n\nPlayers need this release's game to join it: publish it too.`);
  });
  $('do-publish').addEventListener('click', () => {
    const stamp = pick(state);
    start({ action: 'publish', stamp }, `Publish ${stamp} on the download page?\n\nIt uploads the game (about 1.2 GB), ` +
      'and players are told a new build is out.');
  });
  $('do-itch').addEventListener('click', () => {
    const stamp = pick(state);
    start({ action: 'itch', stamp }, `Push ${stamp} to itch.io (${state.settings.itch}, windows channel)?`);
  });
  $('do-check').addEventListener('click', () => start({ action: 'check', stamp: pick(state) }));
  $('do-fetch').addEventListener('click', () => start({ action: 'fetch' }));
  $('do-butler').addEventListener('click', () => start({ action: 'butler' }));
  $('do-login').addEventListener('click', () => start({ action: 'login' },
    `A browser opens on ${state.console.host}, this PC, for you to log in to itch.io there. The job waits until you ` +
    'have (Cancel stops it). Go ahead?'));
  $('do-cancel').addEventListener('click', async () => {
    const job = state && state.job;
    if (!job || !confirm(`Cancel \u201c${job.title}\u201d? Its processes are stopped where they are.`)) return;
    try { await api(`jobs/${job.id}/cancel`, {}); pollLog(); } catch (e) { toast(e.message); }
  });
  for (const r of document.querySelectorAll('input[name=source]')) {
    r.addEventListener('change', async () => {
      try { await api('settings', { source: r.value }); } catch (e) { toast(e.message); }
    });
  }
  const project = $('itch-project');
  project.addEventListener('input', () => { project.dataset.edited = '1'; });
  $('itch-form').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    try {
      await api('settings', { itch: project.value.trim() });
      delete project.dataset.edited;
      toast(project.value.trim() ? `The itch.io project is ${project.value.trim()}.` : 'The itch.io project is cleared.', true);
      await refresh(true);
    } catch (e) { toast(e.message); }
  });
  $('key-form').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const key = $('api-key');
    if (!key.value.trim()) return;
    try {
      await api('settings/key', { key: key.value.trim() });
      key.value = '';
      toast('The API key is saved on this PC.', true);
      await refresh(true);
    } catch (e) { toast(e.message); }
  });
  $('forget-key').addEventListener('click', async () => {
    if (!confirm('Forget the itch.io API key saved here?')) return;
    try { await api('settings/key', { key: '' }); toast('The API key is gone.', true); await refresh(true); } catch (e) { toast(e.message); }
  });
}

wire();
refresh();
setInterval(() => { if (!document.hidden) refresh(); }, 10000);
document.addEventListener('visibilitychange', () => { if (!document.hidden) { refresh(); pollLog(); } });
