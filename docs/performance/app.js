// Encina Unified Performance Dashboard — combines Benchmarks + Load Tests.
// Loads data from ../benchmarks/data/ and ../load-tests/data/ and renders
// them in a tabbed view. See ADR-025.

(function () {
  'use strict';

  const BENCH_DATA   = '../benchmarks/data/latest.json';
  const BENCH_HIST   = '../benchmarks/data/history.json';
  const BENCH_CITED  = '../benchmarks/data/cited-by.json';
  const LOAD_DATA    = '../load-tests/data/latest.json';

  let state = {
    bench: null, benchHistory: [], citedBy: {},
    load: null,
    // Bench table state
    bModuleFilter: 'all', bSearch: '', bShowUnstable: true, bSortField: 'module', bSortDir: 'asc',
    // Load table state
    lAreaFilter: 'all', lSearch: '', lSortField: 'area', lSortDir: 'asc'
  };

  async function boot() {
    const [bench, benchHist, citedBy, load] = await Promise.all([
      fetch(BENCH_DATA).then(r => r.ok ? r.json() : null).catch(() => null),
      fetch(BENCH_HIST).then(r => r.ok ? r.json() : []).catch(() => []),
      fetch(BENCH_CITED).then(r => r.ok ? r.json() : {}).catch(() => ({})),
      fetch(LOAD_DATA).then(r => r.ok ? r.json() : null).catch(() => null)
    ]);
    state.bench = bench;
    state.benchHistory = Array.isArray(benchHist) ? benchHist : [];
    state.citedBy = citedBy || {};
    state.load = load;

    renderTimestamp();
    renderBenchmarks();
    renderLoadTests();
    wireTabs();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // TABS
  // ═══════════════════════════════════════════════════════════════════════
  function wireTabs() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById('tab-' + btn.dataset.tab).classList.add('active');
      });
    });
  }

  function renderTimestamp() {
    const parts = [];
    if (state.bench?.timestamp) parts.push(`Benchmarks: ${new Date(state.bench.timestamp).toLocaleString()}`);
    if (state.load?.timestamp) parts.push(`Load tests: ${new Date(state.load.timestamp).toLocaleString()}`);
    document.getElementById('timestamp').innerHTML = parts.join(' · ') || 'No data available yet.';

    const benchRunId = state.bench?.runId || state.bench?.metadata?.runId;
    const loadRunId = state.load?.runId || state.load?.metadata?.runId;
    setRunLink('bench-run-link', benchRunId);
    setRunLink('load-run-link', loadRunId);
  }

  function setRunLink(id, runId) {
    const el = document.getElementById(id);
    if (el && runId) {
      el.href = `https://github.com/dlrivada/Encina/actions/runs/${runId}`;
      el.textContent = `#${runId}`;
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  // BENCHMARKS
  // ═══════════════════════════════════════════════════════════════════════
  function renderBenchmarks() {
    if (!state.bench) { document.getElementById('bench-subtitle').textContent = 'No benchmark data.'; return; }
    const o = state.bench.overall || {};

    document.getElementById('bench-count').textContent = (o.totalMethods ?? 0).toLocaleString();
    document.getElementById('bench-subtitle').textContent =
      `${o.totalModules ?? 0} modules, ${o.totalMethods ?? 0} methods (${o.stableMethods ?? 0} stable, ${o.unstableMethods ?? 0} unstable)`;
    const mp = [];
    if (o.meanLatencyNs != null) mp.push(`Mean latency: ${formatNs(o.meanLatencyNs)}`);
    if (o.meanAllocatedBytes != null) mp.push(`Mean alloc: ${formatBytes(o.meanAllocatedBytes)}`);
    document.getElementById('bench-metrics').textContent = mp.join(' · ');

    renderBenchTrend();
    renderBenchStability();
    renderBenchFilters();
    renderBenchTable();
    wireBenchInteractions();
  }

  function renderBenchTrend() {
    const canvas = document.getElementById('bench-trend-chart');
    if (!canvas || state.benchHistory.length === 0) return;
    const ctx = canvas.getContext('2d');
    canvas.width = canvas.offsetWidth * devicePixelRatio;
    canvas.height = canvas.offsetHeight * devicePixelRatio;
    ctx.scale(devicePixelRatio, devicePixelRatio);
    const w = canvas.offsetWidth, h = canvas.offsetHeight, px = 40, py = 20;
    const vals = state.benchHistory.map(e => e.meanLatencyNs || 0).filter(v => v > 0);
    if (!vals.length) return;
    const mx = Math.max(...vals), mn = Math.min(...vals), rng = mx - mn || 1;
    ctx.clearRect(0, 0, w, h);
    ctx.strokeStyle = '#444'; ctx.lineWidth = 1;
    ctx.beginPath(); ctx.moveTo(px, py); ctx.lineTo(px, h - py); ctx.lineTo(w - px, h - py); ctx.stroke();
    ctx.strokeStyle = '#3fb950'; ctx.lineWidth = 2; ctx.beginPath();
    vals.forEach((v, i) => {
      const x = px + (i * (w - 2 * px)) / Math.max(vals.length - 1, 1);
      const y = h - py - ((v - mn) / rng) * (h - 2 * py);
      i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
    });
    ctx.stroke();
    ctx.fillStyle = '#888'; ctx.font = '10px sans-serif';
    ctx.fillText(formatNs(mx), 4, py + 8); ctx.fillText(formatNs(mn), 4, h - py);
    document.getElementById('bench-trend-info').textContent = `${vals.length} runs. Range: ${formatNs(mn)} – ${formatNs(mx)}`;
  }

  function renderBenchStability() {
    const canvas = document.getElementById('bench-stability-chart');
    if (!canvas) return;
    const o = state.bench.overall || {};
    const s = o.stableMethods || 0, u = o.unstableMethods || 0, t = s + u;
    if (!t) return;
    canvas.width = canvas.offsetWidth * devicePixelRatio;
    canvas.height = canvas.offsetHeight * devicePixelRatio;
    const ctx = canvas.getContext('2d');
    ctx.scale(devicePixelRatio, devicePixelRatio);
    const w = canvas.offsetWidth, h = canvas.offsetHeight;
    const cx = w / 2, cy = h / 2, r = Math.min(w, h) / 2 - 10;
    const sa = (s / t) * Math.PI * 2;
    ctx.beginPath(); ctx.moveTo(cx, cy); ctx.arc(cx, cy, r, -Math.PI / 2, -Math.PI / 2 + sa); ctx.closePath(); ctx.fillStyle = '#3fb950'; ctx.fill();
    ctx.beginPath(); ctx.moveTo(cx, cy); ctx.arc(cx, cy, r, -Math.PI / 2 + sa, -Math.PI / 2 + Math.PI * 2); ctx.closePath(); ctx.fillStyle = '#d29922'; ctx.fill();
    ctx.fillStyle = '#fff'; ctx.font = 'bold 18px sans-serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(`${Math.round((s / t) * 100)}% stable`, cx, cy);
  }

  function renderBenchFilters() {
    const c = document.getElementById('bench-module-filters');
    if (!c) return;
    c.innerHTML = '<button class="filter-btn active" data-filter="all">all</button>';
    (state.bench.modules || []).forEach(m => {
      const btn = document.createElement('button');
      btn.className = 'filter-btn'; btn.dataset.filter = m.name;
      const s = m.stableMethods || 0, t = s + (m.unstableMethods || 0);
      btn.textContent = `${m.name} ${s}/${t}`;
      c.appendChild(btn);
    });
  }

  function flattenBench() {
    const rows = [];
    for (const m of (state.bench.modules || [])) {
      for (const b of (m.benchmarks || [])) {
        rows.push({ module: m.name, class: b.class, method: b.method, params: b.parameters || '',
          mean: b.meanNs || 0, median: b.medianNs || 0, stddev: b.stdDevNs || 0, cov: b.cov || 0,
          allocated: b.allocatedBytes || 0, n: b.n || 0, stable: !!b.stable, docRef: b.docRef || '' });
      }
    }
    return rows;
  }

  function renderBenchTable() {
    const tbody = document.querySelector('#bench-table tbody');
    if (!tbody) return;
    let rows = flattenBench().filter(r => {
      if (state.bModuleFilter !== 'all' && r.module !== state.bModuleFilter) return false;
      if (!state.bShowUnstable && !r.stable) return false;
      if (state.bSearch && !`${r.module} ${r.class} ${r.method} ${r.docRef}`.toLowerCase().includes(state.bSearch.toLowerCase())) return false;
      return true;
    });
    rows.sort((a, b) => { const f = state.bSortField, d = state.bSortDir === 'asc' ? 1 : -1;
      return typeof a[f] === 'number' ? (a[f] - b[f]) * d : String(a[f]).localeCompare(String(b[f])) * d; });
    tbody.innerHTML = rows.map(r => {
      const stCell = r.stable ? '<span class="status-pass">Stable</span>' : '<span class="status-warn">Unstable</span>';
      const drCell = r.docRef ? `<code class="docref">${esc(r.docRef)}</code>` : '<span class="muted">—</span>';
      const cites = r.docRef ? (state.citedBy[r.docRef] || []) : [];
      const citedCell = cites.length ? cites.map(l => { const f = l.split(':')[0].split('/').pop().replace('.md',''); return `<a href="../${l.split(':')[0]}" title="${esc(l)}">${esc(f)}</a>`; }).join(', ') : '<span class="muted">—</span>';
      return `<tr class="${r.stable ? '' : 'row-unstable'}">
        <td>${esc(r.module)}</td><td>${esc(r.class)}</td><td>${esc(r.method)}</td>
        <td class="num">${formatNs(r.mean)}</td><td class="num">${formatNs(r.median)}</td>
        <td class="num">${formatNs(r.stddev)}</td><td class="num">${(r.cov * 100).toFixed(2)}%</td>
        <td class="num">${formatBytes(r.allocated)}</td><td class="num">${r.n}</td>
        <td>${stCell}</td><td>${drCell}</td><td>${citedCell}</td></tr>`;
    }).join('');
  }

  function wireBenchInteractions() {
    const search = document.getElementById('bench-search');
    if (search) search.addEventListener('input', e => { state.bSearch = e.target.value; renderBenchTable(); });
    document.getElementById('bench-module-filters')?.addEventListener('click', e => {
      if (e.target.matches('.filter-btn')) {
        document.querySelectorAll('#bench-module-filters .filter-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active'); state.bModuleFilter = e.target.dataset.filter; renderBenchTable();
      }
    });
    document.getElementById('bench-show-unstable')?.addEventListener('click', e => {
      state.bShowUnstable = !state.bShowUnstable;
      e.target.textContent = state.bShowUnstable ? 'Hide Unstable' : 'Show Unstable'; renderBenchTable();
    });
    document.getElementById('bench-copy-table')?.addEventListener('click', () => {
      const h = ['Module','Class','Method','Mean(ns)','Median(ns)','StdDev(ns)','CoV','Alloc(B)','N','Stable','DocRef'];
      const lines = [h.join('\t')];
      for (const r of flattenBench()) lines.push([r.module,r.class,r.method,r.mean,r.median,r.stddev,(r.cov*100).toFixed(2)+'%',r.allocated,r.n,r.stable,r.docRef].join('\t'));
      navigator.clipboard.writeText(lines.join('\n')).then(() => {
        const b = document.getElementById('bench-copy-table'); const o = b.textContent; b.textContent = 'Copied!'; setTimeout(() => b.textContent = o, 1500);
      });
    });
    document.querySelectorAll('#bench-table th[data-sort]').forEach(th => {
      th.addEventListener('click', () => {
        const f = th.dataset.sort;
        if (state.bSortField === f) state.bSortDir = state.bSortDir === 'asc' ? 'desc' : 'asc';
        else { state.bSortField = f; state.bSortDir = 'asc'; }
        renderBenchTable();
      });
    });
  }

  // ═══════════════════════════════════════════════════════════════════════
  // LOAD TESTS
  // ═══════════════════════════════════════════════════════════════════════
  function renderLoadTests() {
    if (!state.load) { document.getElementById('load-subtitle').textContent = 'No load test data.'; return; }
    const o = state.load.overall || {};
    document.getElementById('load-count').textContent = (o.totalScenarios ?? 0).toLocaleString();
    document.getElementById('load-subtitle').textContent =
      `${o.totalScenarios ?? 0} scenarios (${o.passingScenarios ?? 0} pass, ${o.failingScenarios ?? 0} fail)`;
    const mp = [];
    if (o.meanThroughputOps != null) mp.push(`Mean throughput: ${o.meanThroughputOps.toLocaleString()} ops/s`);
    if (o.meanP95Ms != null) mp.push(`Mean P95: ${o.meanP95Ms.toFixed(2)} ms`);
    document.getElementById('load-metrics').textContent = mp.join(' · ');
    renderLoadFilters();
    renderLoadTable();
    wireLoadInteractions();
  }

  function flattenLoad() {
    const rows = [];
    for (const m of (state.load?.modules || []))
      for (const s of (m.scenarios || []))
        rows.push({ area: m.name, name: s.name, rps: s.rps || 0, mean: s.meanMs || 0,
          p50: s.p50Ms || 0, p95: s.p95Ms || 0, p99: s.p99Ms || 0, errorRate: s.errorRate || 0, n: s.requestCount || 0 });
    return rows;
  }

  function renderLoadFilters() {
    const c = document.getElementById('load-area-filters');
    if (!c) return;
    c.innerHTML = '<button class="filter-btn active" data-filter="all">all</button>';
    (state.load.modules || []).forEach(m => {
      const btn = document.createElement('button');
      btn.className = 'filter-btn'; btn.dataset.filter = m.name; btn.textContent = m.name;
      c.appendChild(btn);
    });
  }

  function renderLoadTable() {
    const tbody = document.querySelector('#load-table tbody');
    if (!tbody) return;
    let rows = flattenLoad().filter(r => {
      if (state.lAreaFilter !== 'all' && r.area !== state.lAreaFilter) return false;
      if (state.lSearch && !`${r.area} ${r.name}`.toLowerCase().includes(state.lSearch.toLowerCase())) return false;
      return true;
    });
    rows.sort((a, b) => { const f = state.lSortField, d = state.lSortDir === 'asc' ? 1 : -1;
      return typeof a[f] === 'number' ? (a[f] - b[f]) * d : String(a[f]).localeCompare(String(b[f])) * d; });
    tbody.innerHTML = rows.map(r => {
      const ec = r.errorRate >= 0.01 ? 'status-fail' : 'status-pass';
      return `<tr><td>${esc(r.area)}</td><td>${esc(r.name)}</td>
        <td class="num">${r.rps.toLocaleString()}</td><td class="num">${r.mean.toFixed(2)}</td>
        <td class="num">${r.p50.toFixed(2)}</td><td class="num">${r.p95.toFixed(2)}</td>
        <td class="num">${r.p99.toFixed(2)}</td><td class="num ${ec}">${(r.errorRate * 100).toFixed(3)}%</td>
        <td class="num">${r.n.toLocaleString()}</td></tr>`;
    }).join('');
  }

  function wireLoadInteractions() {
    document.getElementById('load-search')?.addEventListener('input', e => { state.lSearch = e.target.value; renderLoadTable(); });
    document.getElementById('load-area-filters')?.addEventListener('click', e => {
      if (e.target.matches('.filter-btn')) {
        document.querySelectorAll('#load-area-filters .filter-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active'); state.lAreaFilter = e.target.dataset.filter; renderLoadTable();
      }
    });
    document.getElementById('load-copy-table')?.addEventListener('click', () => {
      const h = ['Area','Scenario','RPS','Mean(ms)','P50','P95','P99','Error Rate','Requests'];
      const lines = [h.join('\t')];
      for (const r of flattenLoad()) lines.push([r.area,r.name,r.rps,r.mean,r.p50,r.p95,r.p99,(r.errorRate*100).toFixed(3)+'%',r.n].join('\t'));
      navigator.clipboard.writeText(lines.join('\n')).then(() => {
        const b = document.getElementById('load-copy-table'); const o = b.textContent; b.textContent = 'Copied!'; setTimeout(() => b.textContent = o, 1500);
      });
    });
    document.querySelectorAll('#load-table th[data-sort]').forEach(th => {
      th.addEventListener('click', () => {
        const f = th.dataset.sort;
        if (state.lSortField === f) state.lSortDir = state.lSortDir === 'asc' ? 'desc' : 'asc';
        else { state.lSortField = f; state.lSortDir = 'asc'; }
        renderLoadTable();
      });
    });
  }

  // ═══════════════════════════════════════════════════════════════════════
  // HELPERS
  // ═══════════════════════════════════════════════════════════════════════
  function formatNs(v) {
    if (v == null || isNaN(v) || v <= 0) return '—';
    if (v < 1000) return v.toFixed(2) + ' ns';
    if (v < 1e6) return (v / 1000).toFixed(2) + ' μs';
    if (v < 1e9) return (v / 1e6).toFixed(2) + ' ms';
    return (v / 1e9).toFixed(2) + ' s';
  }
  function formatBytes(v) {
    if (v == null || isNaN(v) || v <= 0) return '—';
    if (v < 1024) return v.toFixed(0) + ' B';
    if (v < 1024 * 1024) return (v / 1024).toFixed(2) + ' KB';
    return (v / (1024 * 1024)).toFixed(2) + ' MB';
  }
  function esc(s) { return s == null ? '' : String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }

  document.addEventListener('DOMContentLoaded', boot);
})();
