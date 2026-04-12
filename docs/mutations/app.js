// Encina Mutation Testing Dashboard — reads data/latest.json + data/history.json
(async function () {
  'use strict';

  const LATEST_URL = 'data/latest.json';
  const HISTORY_URL = 'data/history.json';

  // ── Helpers ──────────────────────────────────────────────────────────
  const CSS = getComputedStyle(document.documentElement);
  const COL = {
    green: CSS.getPropertyValue('--green').trim(),
    yellow: CSS.getPropertyValue('--yellow').trim(),
    red: CSS.getPropertyValue('--red').trim(),
    blue: CSS.getPropertyValue('--blue').trim(),
    gray: CSS.getPropertyValue('--gray').trim(),
    text: CSS.getPropertyValue('--text').trim(),
    muted: CSS.getPropertyValue('--text-muted').trim(),
    border: CSS.getPropertyValue('--border').trim(),
    barBg: CSS.getPropertyValue('--bar-bg').trim(),
    barGreen: CSS.getPropertyValue('--bar-green').trim(),
    barYellow: CSS.getPropertyValue('--bar-yellow').trim(),
    barRed: CSS.getPropertyValue('--bar-red').trim(),
  };

  function scoreColor(pct, th) {
    if (pct >= th.high) return 'green';
    if (pct >= th.low) return 'yellow';
    return 'red';
  }

  function barHtml(pct, th) {
    let color = COL.barRed;
    if (pct >= th.high) color = COL.barGreen;
    else if (pct >= th.low) color = COL.barYellow;
    return `<div class="bar-container">
      <div class="bar-fill" style="width:${Math.min(pct, 100)}%;background:${color}"></div>
      <div class="bar-target" style="left:${Math.min(th.high, 100)}%"></div>
    </div>`;
  }

  function statusLabel(pct, th) {
    if (pct >= th.high) return '<span class="status-pass">PASS</span>';
    if (pct >= th.low) return '<span class="status-warn">WARN</span>';
    return '<span class="status-fail">FAIL</span>';
  }

  function fmt(n) { return n == null ? '-' : n.toLocaleString(); }
  function fmtPct(n) { return n == null ? '-' : n.toFixed(1) + '%'; }

  // ── Fetch data ──────────────────────────────────────────────────────
  let data, history;
  try {
    const res = await fetch(LATEST_URL);
    if (!res.ok) throw new Error(res.status);
    data = await res.json();
  } catch (e) {
    document.body.innerHTML = `<div class="error">Failed to load ${LATEST_URL}: ${e.message}</div>`;
    return;
  }

  try {
    const res = await fetch(HISTORY_URL);
    history = res.ok ? await res.json() : [];
  } catch { history = []; }

  const th = data.thresholds || { high: 85, low: 70 };
  const ov = data.overall || {};
  const modules = data.modules || [];
  const mutators = data.mutators || [];
  const gaps = data.gaps || {};

  // ── Header ──────────────────────────────────────────────────────────
  const ts = data.timestamp ? new Date(data.timestamp).toLocaleString() : 'Unknown';
  const scopeHtml = data.scope ? ` <span class="scope-badge">${data.scope}</span>` : '';
  document.getElementById('timestamp').innerHTML = ts + scopeHtml;

  // ── Overall card ────────────────────────────────────────────────────
  const score = ov.score ?? 0;
  const overallEl = document.getElementById('overall-pct');
  overallEl.textContent = fmtPct(score);
  overallEl.className = 'big-number ' + scoreColor(score, th);
  document.getElementById('overall-detail').textContent =
    `${fmt(ov.detected)} / ${fmt(ov.total)} mutants detected (${fmt(ov.killed)} killed, ${fmt(ov.survived)} survived)`;

  // ── Gap analysis ────────────────────────────────────────────────────
  document.getElementById('gap-summary').textContent =
    `${gaps.modulesInScope ?? 0} of ${gaps.modulesTotal ?? 0} modules in mutation scope (${gaps.coveragePercent ?? 0}% of files)`;

  const gapPct = gaps.modulesTotal > 0 ? (gaps.modulesInScope / gaps.modulesTotal) * 100 : 0;
  document.getElementById('gap-bar').innerHTML =
    `<div class="gap-bar-in" style="width:${gapPct}%"></div><div class="gap-bar-out" style="width:${100 - gapPct}%"></div>`;

  const outOfScope = modules.filter(m => m.score == null || m.total === 0).map(m => m.name);
  if (outOfScope.length > 0) {
    document.getElementById('gap-list').textContent = 'Out of scope: ' + outOfScope.join(', ');
  }

  // ── Donut chart ─────────────────────────────────────────────────────
  renderDonut(ov);

  // ── Trend chart ─────────────────────────────────────────────────────
  const inScopeModules = modules.filter(m => m.score != null && m.total > 0).map(m => m.name);
  setupTrendToggles(inScopeModules);
  renderTrend('overall');

  // ── Module table ────────────────────────────────────────────────────
  renderModuleTable(modules, th);

  // ── Mutator chart ───────────────────────────────────────────────────
  renderMutatorChart(mutators);

  // ── Filters ─────────────────────────────────────────────────────────
  const filterDiv = document.getElementById('filters');
  const allBtn = document.createElement('button');
  allBtn.className = 'filter-btn active';
  allBtn.textContent = 'All';
  allBtn.dataset.filter = '';
  filterDiv.appendChild(allBtn);

  const inScopeBtn = document.createElement('button');
  inScopeBtn.className = 'filter-btn';
  inScopeBtn.textContent = 'In Scope';
  inScopeBtn.dataset.filter = 'in-scope';
  filterDiv.appendChild(inScopeBtn);

  const outScopeBtn = document.createElement('button');
  outScopeBtn.className = 'filter-btn';
  outScopeBtn.textContent = 'Out of Scope';
  outScopeBtn.dataset.filter = 'out-scope';
  filterDiv.appendChild(outScopeBtn);

  filterDiv.addEventListener('click', e => {
    const btn = e.target.closest('.filter-btn');
    if (!btn) return;
    filterDiv.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    applyFilters();
  });

  // ── Search ──────────────────────────────────────────────────────────
  document.getElementById('search').addEventListener('input', () => applyFilters());

  function applyFilters() {
    const search = document.getElementById('search').value.toLowerCase();
    const activeFilter = filterDiv.querySelector('.filter-btn.active')?.dataset.filter || '';
    const rows = document.querySelectorAll('#module-tbody tr');
    rows.forEach(row => {
      const name = row.dataset.name?.toLowerCase() || '';
      const inScope = row.dataset.inScope === 'true';
      let show = name.includes(search);
      if (activeFilter === 'in-scope') show = show && inScope;
      if (activeFilter === 'out-scope') show = show && !inScope;
      row.style.display = show ? '' : 'none';
    });
  }

  // ── Sort ────────────────────────────────────────────────────────────
  document.querySelectorAll('#module-table th[data-sort]').forEach(th => {
    th.addEventListener('click', () => sortTable('module-table', 'module-tbody', th, modules));
  });

  // ── Copy TSV ────────────────────────────────────────────────────────
  document.getElementById('copy-tsv').addEventListener('click', () => {
    const rows = Array.from(document.querySelectorAll('#module-tbody tr'))
      .filter(r => r.style.display !== 'none');
    const header = 'Module\tScore\tKilled\tSurvived\tNoCoverage\tTotal';
    const lines = rows.map(r => {
      const cells = r.querySelectorAll('td');
      return [cells[0], cells[1], cells[2], cells[3], cells[4], cells[5]]
        .map(c => c.textContent.trim()).join('\t');
    });
    navigator.clipboard.writeText(header + '\n' + lines.join('\n'));
    const btn = document.getElementById('copy-tsv');
    btn.textContent = 'Copied!';
    btn.classList.add('copied');
    setTimeout(() => { btn.textContent = 'Copy TSV'; btn.classList.remove('copied'); }, 2000);
  });

  // ── Files detail ────────────────────────────────────────────────────
  document.getElementById('close-files').addEventListener('click', () => {
    document.getElementById('files-section').style.display = 'none';
  });

  // ==================================================================
  //  RENDERING FUNCTIONS
  // ==================================================================

  function renderModuleTable(mods, thresholds) {
    const tbody = document.getElementById('module-tbody');
    tbody.innerHTML = '';
    for (const m of mods) {
      const inScope = m.score != null && m.total > 0;
      const s = m.score ?? 0;
      const dimClass = inScope ? '' : ' module-dimmed';
      const tr = document.createElement('tr');
      tr.dataset.name = m.name;
      tr.dataset.inScope = inScope;
      tr.className = dimClass;

      tr.innerHTML = `
        <td class="module-name${dimClass}">${m.name}</td>
        <td class="pct${dimClass}">${inScope ? fmtPct(s) : '-'}</td>
        <td class="num${dimClass}">${inScope ? fmt(m.killed) : '-'}</td>
        <td class="num${dimClass}">${inScope ? fmt(m.survived) : '-'}</td>
        <td class="num${dimClass}">${inScope ? fmt(m.noCoverage) : '-'}</td>
        <td class="num${dimClass}">${inScope ? fmt(m.total) : '-'}</td>
        <td class="bar-col">${inScope ? barHtml(s, thresholds) : ''}</td>
        <td>${inScope ? statusLabel(s, thresholds) : '<span class="na">-</span>'}</td>`;
      tbody.appendChild(tr);

      // Click to expand files
      tr.querySelector('.module-name').addEventListener('click', () => {
        if (!inScope) return;
        showFiles(m, thresholds);
      });
    }
  }

  function showFiles(mod, thresholds) {
    const section = document.getElementById('files-section');
    section.style.display = '';
    document.getElementById('files-title').textContent = `Files in ${mod.name}`;
    const tbody = document.getElementById('files-tbody');
    tbody.innerHTML = '';
    for (const f of (mod.files || [])) {
      const s = f.score ?? 0;
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${f.path}</td>
        <td class="pct">${fmtPct(s)}</td>
        <td class="num">${fmt(f.killed)}</td>
        <td class="num">${fmt(f.survived)}</td>
        <td class="num">${fmt(f.noCoverage)}</td>
        <td class="num">${fmt(f.total)}</td>
        <td class="bar-col">${barHtml(s, thresholds)}</td>`;
      tbody.appendChild(tr);
    }
    section.scrollIntoView({ behavior: 'smooth' });
  }

  function sortTable(tableId, tbodyId, clickedTh) {
    const table = document.getElementById(tableId);
    const tbody = document.getElementById(tbodyId);
    const ths = table.querySelectorAll('th[data-sort]');
    const key = clickedTh.dataset.sort;

    // Toggle direction
    let dir = 'asc';
    if (clickedTh.classList.contains('sort-asc')) dir = 'desc';
    ths.forEach(t => t.classList.remove('sort-asc', 'sort-desc'));
    clickedTh.classList.add('sort-' + dir);

    const rows = Array.from(tbody.querySelectorAll('tr'));
    const idx = Array.from(ths).indexOf(clickedTh);

    rows.sort((a, b) => {
      const aVal = a.children[idx]?.textContent.trim().replace('%', '') || '';
      const bVal = b.children[idx]?.textContent.trim().replace('%', '') || '';
      const aNum = parseFloat(aVal);
      const bNum = parseFloat(bVal);
      const aIsNum = !isNaN(aNum) && aVal !== '-';
      const bIsNum = !isNaN(bNum) && bVal !== '-';

      if (aVal === '-' && bVal === '-') return 0;
      if (aVal === '-') return 1;
      if (bVal === '-') return -1;

      let cmp;
      if (aIsNum && bIsNum) cmp = aNum - bNum;
      else cmp = aVal.localeCompare(bVal);
      return dir === 'desc' ? -cmp : cmp;
    });
    rows.forEach(r => tbody.appendChild(r));
  }

  // ── Donut Chart ─────────────────────────────────────────────────────
  function renderDonut(overall) {
    const canvas = document.getElementById('donut-chart');
    const dpr = window.devicePixelRatio || 1;
    const size = 300;
    canvas.width = size * dpr;
    canvas.height = size * dpr;
    canvas.style.width = size + 'px';
    canvas.style.height = size + 'px';
    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);

    const segments = [
      { label: 'Killed', value: overall.killed || 0, color: COL.green },
      { label: 'Survived', value: overall.survived || 0, color: COL.red },
      { label: 'No Coverage', value: overall.noCoverage || 0, color: COL.yellow },
      { label: 'Timeout', value: overall.timeouts || 0, color: COL.blue },
      { label: 'Runtime Error', value: overall.runtimeErrors || 0, color: COL.gray },
    ].filter(s => s.value > 0);

    const total = segments.reduce((s, seg) => s + seg.value, 0);
    if (total === 0) {
      ctx.fillStyle = COL.muted;
      ctx.textAlign = 'center';
      ctx.font = '14px sans-serif';
      ctx.fillText('No mutation data', size / 2, size / 2);
      return;
    }

    const cx = size / 2, cy = size / 2, outer = 130, inner = 80;
    let angle = -Math.PI / 2;

    for (const seg of segments) {
      const sweep = (seg.value / total) * 2 * Math.PI;
      ctx.beginPath();
      ctx.arc(cx, cy, outer, angle, angle + sweep);
      ctx.arc(cx, cy, inner, angle + sweep, angle, true);
      ctx.closePath();
      ctx.fillStyle = seg.color;
      ctx.fill();
      angle += sweep;
    }

    // Center text
    ctx.fillStyle = COL.text;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.font = 'bold 28px sans-serif';
    ctx.fillText(fmtPct(score), cx, cy - 8);
    ctx.font = '13px sans-serif';
    ctx.fillStyle = COL.muted;
    ctx.fillText('mutation score', cx, cy + 16);

    // Legend below donut
    const legendY = cy + outer + 15;
    ctx.font = '11px sans-serif';
    let lx = 20;
    for (const seg of segments) {
      ctx.fillStyle = seg.color;
      ctx.fillRect(lx, legendY, 10, 10);
      ctx.fillStyle = COL.muted;
      ctx.fillText(`${seg.label} (${seg.value})`, lx + 14, legendY + 9);
      lx += ctx.measureText(`${seg.label} (${seg.value})`).width + 30;
    }
  }

  // ── Trend Chart ─────────────────────────────────────────────────────
  function setupTrendToggles(moduleNames) {
    const div = document.getElementById('trend-toggles');
    div.innerHTML = '';
    const overallBtn = document.createElement('button');
    overallBtn.className = 'toggle-btn active';
    overallBtn.dataset.module = 'overall';
    overallBtn.textContent = 'Overall';
    div.appendChild(overallBtn);

    for (const name of moduleNames) {
      const btn = document.createElement('button');
      btn.className = 'toggle-btn';
      btn.dataset.module = name;
      btn.textContent = name;
      div.appendChild(btn);
    }

    div.addEventListener('click', e => {
      const btn = e.target.closest('.toggle-btn');
      if (!btn) return;
      div.querySelectorAll('.toggle-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      renderTrend(btn.dataset.module);
    });
  }

  function renderTrend(moduleKey) {
    if (!history || history.length === 0) {
      document.getElementById('trend-info').textContent = 'No historical data yet';
      return;
    }

    const canvas = document.getElementById('trend-chart');
    const dpr = window.devicePixelRatio || 1;
    const W = canvas.parentElement.clientWidth - 40;
    const H = 200;
    canvas.width = W * dpr;
    canvas.height = H * dpr;
    canvas.style.width = W + 'px';
    canvas.style.height = H + 'px';
    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);

    const pad = { top: 20, right: 20, bottom: 30, left: 45 };
    const plotW = W - pad.left - pad.right;
    const plotH = H - pad.top - pad.bottom;

    // Extract data points
    const points = history.map(h => {
      let val;
      if (moduleKey === 'overall') val = h.score ?? 0;
      else val = h.perModule?.[moduleKey] ?? null;
      return { ts: new Date(h.timestamp), val, scope: h.scope || '' };
    }).filter(p => p.val !== null);

    if (points.length === 0) {
      ctx.fillStyle = COL.muted;
      ctx.textAlign = 'center';
      ctx.font = '13px sans-serif';
      ctx.fillText('No data for ' + moduleKey, W / 2, H / 2);
      return;
    }

    const minVal = 0, maxVal = 100;

    // Grid
    ctx.strokeStyle = COL.border;
    ctx.lineWidth = 0.5;
    for (let y = 0; y <= 100; y += 20) {
      const py = pad.top + plotH - (y / 100) * plotH;
      ctx.beginPath();
      ctx.moveTo(pad.left, py);
      ctx.lineTo(pad.left + plotW, py);
      ctx.stroke();
      ctx.fillStyle = COL.muted;
      ctx.font = '10px sans-serif';
      ctx.textAlign = 'right';
      ctx.fillText(y + '%', pad.left - 5, py + 3);
    }

    // Target line
    ctx.strokeStyle = COL.green;
    ctx.lineWidth = 1;
    ctx.setLineDash([4, 4]);
    const targetY = pad.top + plotH - (th.high / 100) * plotH;
    ctx.beginPath();
    ctx.moveTo(pad.left, targetY);
    ctx.lineTo(pad.left + plotW, targetY);
    ctx.stroke();
    ctx.setLineDash([]);

    // Data line
    ctx.strokeStyle = COL.blue;
    ctx.lineWidth = 2;
    ctx.beginPath();
    points.forEach((p, i) => {
      const x = pad.left + (points.length === 1 ? plotW / 2 : (i / (points.length - 1)) * plotW);
      const y = pad.top + plotH - (p.val / 100) * plotH;
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    });
    ctx.stroke();

    // Data dots
    points.forEach((p, i) => {
      const x = pad.left + (points.length === 1 ? plotW / 2 : (i / (points.length - 1)) * plotW);
      const y = pad.top + plotH - (p.val / 100) * plotH;
      ctx.beginPath();
      ctx.arc(x, y, 3, 0, Math.PI * 2);
      ctx.fillStyle = COL.blue;
      ctx.fill();
    });

    // X-axis labels
    ctx.fillStyle = COL.muted;
    ctx.font = '9px sans-serif';
    ctx.textAlign = 'center';
    const step = Math.max(1, Math.floor(points.length / 6));
    points.forEach((p, i) => {
      if (i % step !== 0 && i !== points.length - 1) return;
      const x = pad.left + (points.length === 1 ? plotW / 2 : (i / (points.length - 1)) * plotW);
      const label = p.ts.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
      ctx.fillText(label, x, H - 5);
    });

    const last = points[points.length - 1];
    document.getElementById('trend-info').textContent =
      `${points.length} data points | Latest: ${fmtPct(last.val)} on ${last.ts.toLocaleDateString()}`;
  }

  // ── Mutator Chart ───────────────────────────────────────────────────
  function renderMutatorChart(mutatorData) {
    if (!mutatorData || mutatorData.length === 0) return;

    const canvas = document.getElementById('mutator-chart');
    const dpr = window.devicePixelRatio || 1;
    const W = canvas.parentElement.clientWidth - 40;
    const barH = 22, gap = 4;
    const pad = { top: 10, right: 20, bottom: 10, left: 200 };
    const H = pad.top + mutatorData.length * (barH + gap) + pad.bottom;
    const plotW = W - pad.left - pad.right;

    canvas.width = W * dpr;
    canvas.height = H * dpr;
    canvas.style.width = W + 'px';
    canvas.style.height = H + 'px';
    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);

    const maxTotal = Math.max(...mutatorData.map(m => m.total), 1);

    mutatorData.forEach((m, i) => {
      const y = pad.top + i * (barH + gap);
      const totalW = (m.total / maxTotal) * plotW;
      const killedW = m.total > 0 ? (m.killed / m.total) * totalW : 0;
      const survivedW = totalW - killedW;

      // Killed portion (green)
      ctx.fillStyle = COL.barGreen;
      ctx.fillRect(pad.left, y, killedW, barH);
      // Survived portion (red)
      ctx.fillStyle = COL.barRed;
      ctx.fillRect(pad.left + killedW, y, survivedW, barH);

      // Label
      ctx.fillStyle = COL.text;
      ctx.font = '11px sans-serif';
      ctx.textAlign = 'right';
      ctx.textBaseline = 'middle';
      ctx.fillText(m.name, pad.left - 8, y + barH / 2);

      // Count
      ctx.textAlign = 'left';
      ctx.fillStyle = COL.muted;
      ctx.font = '10px sans-serif';
      const pct = m.total > 0 ? Math.round((m.killed / m.total) * 100) : 0;
      ctx.fillText(`${m.killed}/${m.total} (${pct}%)`, pad.left + totalW + 6, y + barH / 2);
    });
  }

})();
