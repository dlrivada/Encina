// Encina Load Tests Dashboard — minimal, dependency-free.
// See ADR-025 and performance-measurement-methodology.md.

(function () {
  'use strict';

  const DATA_URL = '../data/latest.json';

  let state = { latest: null, areaFilter: 'all', searchTerm: '', sortField: 'area', sortDir: 'asc' };

  async function boot() {
    try {
      state.latest = await fetch(DATA_URL).then(r => r.ok ? r.json() : null);
      if (!state.latest) {
        document.getElementById('overall-subtitle').textContent = 'No data available yet.';
        return;
      }
      renderHeader();
      renderOverall();
      renderAreaFilters();
      renderTable();
      wireInteractions();
    } catch (err) {
      console.error('Load tests dashboard boot failed:', err);
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
    document.getElementById('overall-count').textContent = (o.totalScenarios ?? 0).toLocaleString();
    document.getElementById('overall-subtitle').textContent =
      `${o.totalScenarios ?? 0} scenarios (${o.passingScenarios ?? 0} pass, ${o.failingScenarios ?? 0} fail)`;

    const metricsEl = document.getElementById('overall-metrics');
    const parts = [];
    if (o.meanThroughputOps != null) parts.push(`Mean throughput: ${o.meanThroughputOps.toLocaleString()} ops/s`);
    if (o.meanP95Ms != null) parts.push(`Mean P95: ${o.meanP95Ms.toFixed(2)} ms`);
    if (o.meanErrorRate != null) parts.push(`Mean error rate: ${(o.meanErrorRate * 100).toFixed(2)}%`);
    metricsEl.textContent = parts.join(' · ');
  }

  function renderAreaFilters() {
    const container = document.getElementById('area-filters');
    container.innerHTML = '<button class="filter-btn active" data-filter="all">all</button>';
    (state.latest.modules || []).forEach(m => {
      const btn = document.createElement('button');
      btn.className = 'filter-btn';
      btn.dataset.filter = m.name;
      btn.textContent = m.name;
      container.appendChild(btn);
    });
  }

  function renderTable() {
    const tbody = document.querySelector('#scenario-table tbody');
    if (!tbody) return;

    const rows = [];
    for (const m of (state.latest.modules || [])) {
      for (const s of (m.scenarios || [])) {
        rows.push({
          area: m.name,
          name: s.name,
          rps: s.rps || 0,
          mean: s.meanMs || 0,
          p50: s.p50Ms || 0,
          p95: s.p95Ms || 0,
          p99: s.p99Ms || 0,
          errorRate: s.errorRate || 0,
          n: s.requestCount || 0
        });
      }
    }

    const filtered = rows.filter(r => {
      if (state.areaFilter !== 'all' && r.area !== state.areaFilter) return false;
      if (state.searchTerm) {
        const term = state.searchTerm.toLowerCase();
        if (!`${r.area} ${r.name}`.toLowerCase().includes(term)) return false;
      }
      return true;
    });

    filtered.sort((a, b) => {
      const f = state.sortField;
      const dir = state.sortDir === 'asc' ? 1 : -1;
      if (typeof a[f] === 'number') return (a[f] - b[f]) * dir;
      return String(a[f]).localeCompare(String(b[f])) * dir;
    });

    tbody.innerHTML = filtered.map(r => {
      const errClass = r.errorRate >= 0.01 ? 'status-fail' : 'status-pass';
      return `<tr>
        <td>${escape(r.area)}</td>
        <td>${escape(r.name)}</td>
        <td class="num">${r.rps.toLocaleString()}</td>
        <td class="num">${r.mean.toFixed(2)}</td>
        <td class="num">${r.p50.toFixed(2)}</td>
        <td class="num">${r.p95.toFixed(2)}</td>
        <td class="num">${r.p99.toFixed(2)}</td>
        <td class="num ${errClass}">${(r.errorRate * 100).toFixed(3)}%</td>
        <td class="num">${r.n.toLocaleString()}</td>
      </tr>`;
    }).join('');
  }

  function wireInteractions() {
    document.getElementById('scn-search').addEventListener('input', e => {
      state.searchTerm = e.target.value;
      renderTable();
    });

    document.getElementById('area-filters').addEventListener('click', e => {
      if (e.target.matches('.filter-btn')) {
        document.querySelectorAll('#area-filters .filter-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active');
        state.areaFilter = e.target.dataset.filter;
        renderTable();
      }
    });

    document.querySelectorAll('#scenario-table th[data-sort]').forEach(th => {
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

    document.getElementById('copy-table').addEventListener('click', () => {
      const header = ['Area', 'Scenario', 'RPS', 'Mean (ms)', 'P50', 'P95', 'P99', 'Error Rate', 'Requests'];
      const lines = [header.join('\t')];
      for (const m of (state.latest.modules || [])) {
        for (const s of (m.scenarios || [])) {
          lines.push([m.name, s.name, s.rps, s.meanMs, s.p50Ms, s.p95Ms, s.p99Ms, (s.errorRate * 100).toFixed(3) + '%', s.requestCount].join('\t'));
        }
      }
      navigator.clipboard.writeText(lines.join('\n')).then(() => {
        const btn = document.getElementById('copy-table');
        const old = btn.textContent;
        btn.textContent = 'Copied!';
        setTimeout(() => btn.textContent = old, 1500);
      });
    });
  }

  function escape(s) {
    if (s == null) return '';
    return String(s).replace(/[&<>"']/g, c => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  document.addEventListener('DOMContentLoaded', boot);
})();
