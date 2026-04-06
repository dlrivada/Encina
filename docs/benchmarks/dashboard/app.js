// Encina Benchmarks Dashboard — minimal, dependency-free client.
// Adapted from docs/coverage/app.js. See ADR-025 and the methodology doc.
//
// Data source: ../data/latest.json (emitted by .github/scripts/perf-report.cs)
// History:     ../data/history.json (emitted by .github/scripts/perf-history.cs)

(function () {
  'use strict';

  const DATA_URL = '../data/latest.json';
  const HISTORY_URL = '../data/history.json';
  const DOCREF_URL = '../data/docref-index.json';
  const CITED_BY_URL = '../data/cited-by.json';

  let state = {
    latest: null,
    history: [],
    docrefIndex: {},
    citedBy: {},
    moduleFilter: 'all',
    searchTerm: '',
    showUnstable: true,
    sortField: 'module',
    sortDir: 'asc'
  };

  async function boot() {
    try {
      const [latest, history, docref, citedBy] = await Promise.all([
        fetch(DATA_URL).then(r => r.ok ? r.json() : null).catch(() => null),
        fetch(HISTORY_URL).then(r => r.ok ? r.json() : []).catch(() => []),
        fetch(DOCREF_URL).then(r => r.ok ? r.json() : {}).catch(() => ({})),
        fetch(CITED_BY_URL).then(r => r.ok ? r.json() : {}).catch(() => ({}))
      ]);

      if (!latest) {
        document.getElementById('overall-subtitle').textContent = 'No data available yet.';
        return;
      }

      state.latest = latest;
      state.history = Array.isArray(history) ? history : [];
      state.docrefIndex = docref || {};
      state.citedBy = citedBy || {};

      renderHeader();
      renderOverall();
      renderTrend();
      renderStability();
      renderModuleFilters();
      renderTable();
      wireInteractions();
    } catch (err) {
      console.error('Dashboard boot failed:', err);
      document.getElementById('overall-subtitle').textContent = 'Error loading data.';
    }
  }

  function renderHeader() {
    const ts = state.latest.timestamp;
    const sha = state.latest.sha || '';
    const runId = state.latest.runId || state.latest.metadata?.runId;
    let html = ts ? `Generated ${new Date(ts).toLocaleString()}` : '';
    if (sha) html += ` &middot; <code>${sha.substring(0, 7)}</code>`;
    document.getElementById('timestamp').innerHTML = html;

    const runLink = document.getElementById('run-link');
    if (runLink && runId) {
      runLink.href = `https://github.com/dlrivada/Encina/actions/runs/${runId}`;
      runLink.textContent = `CI run #${runId}`;
    }
  }

  function renderOverall() {
    const o = state.latest.overall || {};
    document.getElementById('overall-count').textContent = (o.totalMethods ?? 0).toLocaleString();
    document.getElementById('overall-subtitle').textContent =
      `${o.totalModules ?? 0} modules, ${o.totalMethods ?? 0} methods ` +
      `(${o.stableMethods ?? 0} stable, ${o.unstableMethods ?? 0} unstable)`;

    const metricsEl = document.getElementById('overall-metrics');
    const parts = [];
    if (o.meanLatencyNs != null) parts.push(`Mean latency: ${formatNs(o.meanLatencyNs)}`);
    if (o.meanAllocatedBytes != null) parts.push(`Mean alloc: ${formatBytes(o.meanAllocatedBytes)}`);
    metricsEl.textContent = parts.join(' · ');
  }

  function renderTrend() {
    const canvas = document.getElementById('trend-chart');
    if (!canvas || state.history.length === 0) return;

    const ctx = canvas.getContext('2d');
    canvas.width = canvas.offsetWidth * window.devicePixelRatio;
    canvas.height = canvas.offsetHeight * window.devicePixelRatio;
    ctx.scale(window.devicePixelRatio, window.devicePixelRatio);

    const w = canvas.offsetWidth;
    const h = canvas.offsetHeight;
    const padX = 40, padY = 20;

    const values = state.history.map(e => e.meanLatencyNs || 0).filter(v => v > 0);
    if (values.length === 0) return;

    const maxV = Math.max(...values);
    const minV = Math.min(...values);
    const range = maxV - minV || 1;

    ctx.clearRect(0, 0, w, h);

    // Axis
    ctx.strokeStyle = '#444';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(padX, padY);
    ctx.lineTo(padX, h - padY);
    ctx.lineTo(w - padX, h - padY);
    ctx.stroke();

    // Line
    ctx.strokeStyle = '#3fb950';
    ctx.lineWidth = 2;
    ctx.beginPath();
    values.forEach((v, i) => {
      const x = padX + (i * (w - 2 * padX)) / Math.max(values.length - 1, 1);
      const y = h - padY - ((v - minV) / range) * (h - 2 * padY);
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    });
    ctx.stroke();

    // Min/max labels
    ctx.fillStyle = '#888';
    ctx.font = '10px sans-serif';
    ctx.fillText(formatNs(maxV), 4, padY + 8);
    ctx.fillText(formatNs(minV), 4, h - padY);

    document.getElementById('trend-info').textContent =
      `Showing ${values.length} runs. Range: ${formatNs(minV)} – ${formatNs(maxV)}`;
  }

  function renderStability() {
    const canvas = document.getElementById('stability-chart');
    if (!canvas) return;
    const o = state.latest.overall || {};
    const stable = o.stableMethods || 0;
    const unstable = o.unstableMethods || 0;
    const total = stable + unstable;
    if (total === 0) return;

    canvas.width = canvas.offsetWidth * window.devicePixelRatio;
    canvas.height = canvas.offsetHeight * window.devicePixelRatio;
    const ctx = canvas.getContext('2d');
    ctx.scale(window.devicePixelRatio, window.devicePixelRatio);

    const w = canvas.offsetWidth;
    const h = canvas.offsetHeight;
    const cx = w / 2, cy = h / 2, r = Math.min(w, h) / 2 - 10;

    const stableAngle = (stable / total) * Math.PI * 2;

    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, r, -Math.PI / 2, -Math.PI / 2 + stableAngle);
    ctx.closePath();
    ctx.fillStyle = '#3fb950';
    ctx.fill();

    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, r, -Math.PI / 2 + stableAngle, -Math.PI / 2 + Math.PI * 2);
    ctx.closePath();
    ctx.fillStyle = '#d29922';
    ctx.fill();

    ctx.fillStyle = '#fff';
    ctx.font = 'bold 18px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(`${Math.round((stable / total) * 100)}% stable`, cx, cy);
  }

  function renderModuleFilters() {
    const container = document.getElementById('module-filters');
    if (!container) return;
    container.innerHTML = '<button class="filter-btn active" data-filter="all">all</button>';
    (state.latest.modules || []).forEach(m => {
      const btn = document.createElement('button');
      btn.className = 'filter-btn';
      btn.dataset.filter = m.name;
      // Phase 3.3 — show stability indicator + mean CoV in module filter button
      const stable = m.stableMethods || 0;
      const total = stable + (m.unstableMethods || 0);
      const cov = m.meanCov != null ? ` (CoV ${(m.meanCov * 100).toFixed(1)}%)` : '';
      const carried = m.carriedForward ? ' [cf]' : '';
      btn.textContent = `${m.name} ${stable}/${total}${cov}${carried}`;
      btn.title = `${m.name}: ${stable} stable / ${total} total methods, mean CoV = ${((m.meanCov || 0) * 100).toFixed(2)}%${m.carriedForward ? ' (carried forward from previous run)' : ''}`;
      container.appendChild(btn);
    });
  }

  function renderTable() {
    const tbody = document.querySelector('#benchmark-table tbody');
    if (!tbody) return;

    const rows = flatten(state.latest.modules || []);
    const filtered = rows.filter(r => {
      if (state.moduleFilter !== 'all' && r.module !== state.moduleFilter) return false;
      if (!state.showUnstable && !r.stable) return false;
      if (state.searchTerm) {
        const term = state.searchTerm.toLowerCase();
        const hay = `${r.module} ${r.class} ${r.method} ${r.docRef || ''}`.toLowerCase();
        if (!hay.includes(term)) return false;
      }
      return true;
    });

    filtered.sort(compareRows);

    tbody.innerHTML = filtered.map(renderRow).join('');
  }

  function flatten(modules) {
    const rows = [];
    for (const m of modules) {
      for (const b of (m.benchmarks || [])) {
        rows.push({
          module: m.name,
          class: b.class,
          method: b.method,
          params: b.parameters || '',
          mean: b.meanNs || 0,
          median: b.medianNs || 0,
          stddev: b.stdDevNs || 0,
          cov: b.cov || 0,
          ci99: `${formatNs(b.ci99LowerNs || 0)} – ${formatNs(b.ci99UpperNs || 0)}`,
          ci99_lower: b.ci99LowerNs || 0,
          allocated: b.allocatedBytes || 0,
          n: b.n || 0,
          stable: !!b.stable,
          docRef: b.docRef || ''
        });
      }
    }
    return rows;
  }

  function renderRow(r) {
    const stableCell = r.stable
      ? '<span class="status-pass">Stable</span>'
      : '<span class="status-warn">Unstable</span>';
    const docRefCell = r.docRef
      ? `<code class="docref">${escape(r.docRef)}</code>`
      : '<span class="muted">—</span>';
    // Phase 4.1 — "Cited in" column: show which docs reference this DocRef
    const citations = r.docRef ? (state.citedBy[r.docRef] || []) : [];
    const citedInCell = citations.length > 0
      ? citations.map(loc => {
          const [file] = loc.split(':');
          const shortName = file.split('/').pop().replace('.md', '');
          return `<a href="../../${file}" title="${escape(loc)}">${escape(shortName)}</a>`;
        }).join(', ')
      : '<span class="muted">—</span>';
    return `<tr class="${r.stable ? '' : 'row-unstable'}">
      <td>${escape(r.module)}</td>
      <td>${escape(r.class)}</td>
      <td>${escape(r.method)}</td>
      <td>${escape(r.params)}</td>
      <td class="num">${formatNs(r.mean)}</td>
      <td class="num">${formatNs(r.median)}</td>
      <td class="num">${formatNs(r.stddev)}</td>
      <td class="num">${(r.cov * 100).toFixed(2)}%</td>
      <td class="num">${r.ci99}</td>
      <td class="num">${formatBytes(r.allocated)}</td>
      <td class="num">${r.n}</td>
      <td>${stableCell}</td>
      <td>${docRefCell}</td>
      <td>${citedInCell}</td>
    </tr>`;
  }

  function compareRows(a, b) {
    const f = state.sortField;
    const dir = state.sortDir === 'asc' ? 1 : -1;
    const va = a[f];
    const vb = b[f];
    if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir;
    return String(va).localeCompare(String(vb)) * dir;
  }

  function wireInteractions() {
    document.getElementById('bench-search').addEventListener('input', e => {
      state.searchTerm = e.target.value;
      renderTable();
    });

    document.getElementById('module-filters').addEventListener('click', e => {
      if (e.target.matches('.filter-btn')) {
        document.querySelectorAll('#module-filters .filter-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active');
        state.moduleFilter = e.target.dataset.filter;
        renderTable();
      }
    });

    document.getElementById('show-unstable').addEventListener('click', e => {
      state.showUnstable = !state.showUnstable;
      e.target.textContent = state.showUnstable ? 'Hide Unstable' : 'Show Unstable';
      renderTable();
    });

    document.getElementById('copy-table').addEventListener('click', () => {
      const rows = flatten(state.latest.modules || []);
      const header = ['Module', 'Class', 'Method', 'Params', 'Mean (ns)', 'Median (ns)', 'StdDev (ns)', 'CoV', 'Allocated (B)', 'N', 'Stable', 'DocRef', 'Cited in'];
      const lines = [header.join('\t')];
      for (const r of rows) {
        const citations = r.docRef ? (state.citedBy[r.docRef] || []).map(l => l.split(':')[0]).join('; ') : '';
        lines.push([r.module, r.class, r.method, r.params, r.mean, r.median, r.stddev, (r.cov * 100).toFixed(2) + '%', r.allocated, r.n, r.stable, r.docRef, citations].join('\t'));
      }
      navigator.clipboard.writeText(lines.join('\n')).then(() => {
        const btn = document.getElementById('copy-table');
        const old = btn.textContent;
        btn.textContent = 'Copied!';
        setTimeout(() => btn.textContent = old, 1500);
      });
    });

    document.querySelectorAll('#benchmark-table th[data-sort]').forEach(th => {
      th.addEventListener('click', () => {
        const field = th.dataset.sort;
        if (state.sortField === field) {
          state.sortDir = state.sortDir === 'asc' ? 'desc' : 'asc';
        } else {
          state.sortField = field;
          state.sortDir = 'asc';
        }
        renderTable();
      });
    });
  }

  function formatNs(v) {
    if (v == null || isNaN(v)) return '—';
    if (v < 1000) return v.toFixed(2) + ' ns';
    if (v < 1e6) return (v / 1000).toFixed(2) + ' μs';
    if (v < 1e9) return (v / 1e6).toFixed(2) + ' ms';
    return (v / 1e9).toFixed(2) + ' s';
  }

  function formatBytes(v) {
    if (v == null || isNaN(v)) return '—';
    if (v < 1024) return v.toFixed(0) + ' B';
    if (v < 1024 * 1024) return (v / 1024).toFixed(2) + ' KB';
    return (v / (1024 * 1024)).toFixed(2) + ' MB';
  }

  function escape(s) {
    if (s == null) return '';
    return String(s).replace(/[&<>"']/g, c => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  document.addEventListener('DOMContentLoaded', boot);
})();
