// Encina Coverage Dashboard — reads data/latest.json
(async function () {
  'use strict';

  const DATA_URL = 'data/latest.json';

  // ── Helpers ──────────────────────────────────────────────────────────
  function pctColor(pct, target) {
    if (pct >= target) return 'green';
    if (pct >= target * 0.8) return 'yellow';
    return 'red';
  }

  function barHtml(pct, target) {
    let color = 'var(--bar-red)';
    if (pct >= target) color = 'var(--bar-green)';
    else if (pct >= target * 0.8) color = 'var(--bar-yellow)';
    const targetLeft = Math.min(target, 100);
    return `<div class="bar-container">
      <div class="bar-fill" style="width:${Math.min(pct, 100)}%;background:${color}"></div>
      ${target > 0 ? `<div class="bar-target" style="left:${targetLeft}%"></div>` : ''}
    </div>`;
  }

  function statusIcon(pct, target) {
    if (pct >= target) return '<span class="status-pass">PASS</span>';
    if (pct >= target * 0.8) return '<span class="status-warn">WARN</span>';
    return '<span class="status-fail">FAIL</span>';
  }

  function flagPct(perFlag, flag) {
    const d = perFlag?.[flag];
    if (!d) return null;
    return d.total > 0 ? d.coverage : 0;
  }

  // Map category short codes to flag names
  const FLAG_SHORT = { U: 'unit', G: 'guard', C: 'contract', P: 'property', I: 'integration' };

  function isApplicableFlag(catTests, flag) {
    if (!catTests) return false;
    const short = Object.entries(FLAG_SHORT).find(([, v]) => v === flag);
    return short ? catTests.includes(short[0]) : false;
  }

  // 3-state cell: applicable+data, applicable+nodata, not-applicable
  function flagCell(perFlag, flag, catTests) {
    const applicable = isApplicableFlag(catTests, flag);
    const pct = flagPct(perFlag, flag);

    if (!applicable) return '<td class="na" title="Not applicable for this category">-</td>';
    if (pct === null) return '<td class="nodata" title="No coverage data (expected)">?</td>';
    if (pct === 0) return '<td class="zero" title="Has data but 0% covered">0%</td>';
    return `<td class="pct">${Math.round(pct)}%</td>`;
  }

  // For TSV export
  function flagCellText(perFlag, flag, catTests) {
    const applicable = isApplicableFlag(catTests, flag);
    const pct = flagPct(perFlag, flag);
    if (!applicable) return '-';
    if (pct === null) return '?';
    return Math.round(pct) + '%';
  }

  // ── Fetch data ──────────────────────────────────────────────────────
  let data;
  try {
    const resp = await fetch(DATA_URL);
    if (!resp.ok) throw new Error(`HTTP ${resp.status}: ${resp.statusText}`);
    data = await resp.json();
  } catch (err) {
    document.getElementById('timestamp').textContent = '';
    document.getElementById('overall-pct').textContent = 'No data';
    document.getElementById('overall-pct').className = 'big-number red';
    document.getElementById('overall-lines').innerHTML =
      `<span class="error">Could not load <code>${DATA_URL}</code>: ${err.message}<br>
       The CI pipeline needs to complete at least once to generate coverage data.</span>`;
    return;
  }

  // ── Overall ─────────────────────────────────────────────────────────
  const ts = new Date(data.timestamp);
  document.getElementById('timestamp').textContent =
    `Generated: ${ts.toISOString().replace('T', ' ').slice(0, 19)} UTC`;

  const ov = data.overall;
  document.getElementById('overall-pct').textContent = `${ov.coverage}%`;
  document.getElementById('overall-pct').className =
    `big-number ${ov.coverage >= 80 ? 'green' : (ov.coverage >= 60 ? 'yellow' : 'red')}`;
  document.getElementById('overall-lines').textContent =
    `${typeof ov.covered === 'number' ? ov.covered.toLocaleString(undefined, {maximumFractionDigits: 1}) : ov.covered} / ${ov.lines.toLocaleString()} weighted lines covered`;

  // Show link to download raw Cobertura XML artifacts if runId is available
  if (data.runId) {
    const rawLink = document.createElement('a');
    rawLink.href = `https://github.com/dlrivada/Encina/actions/runs/${data.runId}`;
    rawLink.textContent = 'Download raw coverage data';
    rawLink.target = '_blank';
    rawLink.style.cssText = 'display:block;margin-top:0.5em;font-size:0.85em;color:#58a6ff;';
    document.getElementById('overall-lines').parentElement?.appendChild(rawLink);
  }

  // ── Categories ──────────────────────────────────────────────────────
  const catBody = document.querySelector('#category-table tbody');
  const categoryNames = new Set();
  const catTestsMap = {}; // category name → "U+G+C+P+I"

  for (const cat of data.categories) {
    categoryNames.add(cat.name);
    catTestsMap[cat.name] = cat.tests || '';
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${cat.name}</td>
      <td>${cat.tests}</td>
      <td class="pct">${cat.coverage}%</td>
      <td class="pct">${cat.target}%</td>
      <td>${barHtml(cat.coverage, cat.target)}</td>
      <td>${statusIcon(cat.coverage, cat.target)}</td>`;
    catBody.appendChild(tr);
  }

  // ── Category filter buttons ─────────────────────────────────────────
  const filtersDiv = document.querySelector('.filters');
  for (const name of categoryNames) {
    const btn = document.createElement('button');
    btn.className = 'filter-btn';
    btn.dataset.filter = name;
    btn.textContent = name;
    filtersDiv.appendChild(btn);
  }

  // ── Packages ────────────────────────────────────────────────────────
  const pkgBody = document.querySelector('#package-table tbody');
  const allPackages = data.packages || [];
  let activeFilter = 'all';
  let searchTerm = '';

  function renderPackages() {
    pkgBody.innerHTML = '';
    const filtered = allPackages.filter(p => {
      if (activeFilter !== 'all' && p.category !== activeFilter) return false;
      if (searchTerm && !p.name.toLowerCase().includes(searchTerm)) return false;
      return true;
    });

    for (const pkg of filtered) {
      const catTests = catTestsMap[pkg.category] || '';
      const gap = pkg.coverage - pkg.target;
      const gapStr = gap >= 0 ? `+${Math.round(gap)}%` : `${Math.round(gap)}%`;
      const gapClass = gap >= 0 ? 'gap-positive' : 'gap-negative';

      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td class="pkg-name" data-pkg="${pkg.name}">${pkg.name}</td>
        <td>${pkg.category}</td>
        ${flagCell(pkg.perFlag, 'unit', catTests)}
        ${flagCell(pkg.perFlag, 'guard', catTests)}
        ${flagCell(pkg.perFlag, 'contract', catTests)}
        ${flagCell(pkg.perFlag, 'property', catTests)}
        ${flagCell(pkg.perFlag, 'integration', catTests)}
        <td class="pct">${pkg.coverage}%</td>
        <td class="pct">${pkg.target}%</td>
        <td class="${gapClass}">${gapStr}</td>
        <td>${barHtml(pkg.coverage, pkg.target)}</td>
        <td class="num">${pkg.lines.toLocaleString()}</td>`;
      pkgBody.appendChild(tr);
    }
  }

  renderPackages();

  // ── Filter buttons ──────────────────────────────────────────────────
  filtersDiv.addEventListener('click', e => {
    if (!e.target.classList.contains('filter-btn')) return;
    document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
    e.target.classList.add('active');
    activeFilter = e.target.dataset.filter;
    renderPackages();
  });

  // ── Search ──────────────────────────────────────────────────────────
  document.getElementById('pkg-search').addEventListener('input', e => {
    searchTerm = e.target.value.toLowerCase();
    renderPackages();
  });

  // ── Copy to Excel (TSV) ────────────────────────────────────────────
  document.getElementById('copy-table').addEventListener('click', async e => {
    const btn = e.target;
    const headers = ['Package', 'Category', 'Unit', 'Guard', 'Contract', 'Property', 'Integ', 'Combined', 'Target', 'Gap', 'Lines'];
    const rows = [headers.join('\t')];
    for (const pkg of allPackages) {
      const catTests = catTestsMap[pkg.category] || '';
      const gap = pkg.coverage - pkg.target;
      rows.push([
        pkg.name, pkg.category,
        flagCellText(pkg.perFlag, 'unit', catTests),
        flagCellText(pkg.perFlag, 'guard', catTests),
        flagCellText(pkg.perFlag, 'contract', catTests),
        flagCellText(pkg.perFlag, 'property', catTests),
        flagCellText(pkg.perFlag, 'integration', catTests),
        Math.round(pkg.coverage) + '%',
        pkg.target + '%',
        (gap >= 0 ? '+' : '') + Math.round(gap) + '%',
        pkg.lines
      ].join('\t'));
    }
    await navigator.clipboard.writeText(rows.join('\n'));
    btn.textContent = 'Copied!';
    btn.classList.add('copied');
    setTimeout(() => { btn.textContent = 'Copy to Excel'; btn.classList.remove('copied'); }, 2000);
  });

  // ── File detail (click package name) ────────────────────────────────
  pkgBody.addEventListener('click', e => {
    const td = e.target.closest('.pkg-name');
    if (!td) return;
    const pkgName = td.dataset.pkg;
    const pkg = allPackages.find(p => p.name === pkgName);
    if (!pkg || !pkg.files || pkg.files.length === 0) return;

    document.getElementById('files-pkg-name').textContent = pkgName;
    const filesBody = document.querySelector('#files-table tbody');
    filesBody.innerHTML = '';

    for (const f of pkg.files) {
      const shortName = f.relativePath
        ? f.relativePath.replace(`src/${pkgName}/`, '')
        : f.file || f.name || 'unknown';
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${shortName}</td>
        <td class="num">${f.totalLines}</td>
        <td class="num">${typeof f.covered === 'number' ? f.covered.toFixed(1) : (f.coveredLines ?? 0)}</td>
        <td class="pct">${f.percentage}%</td>
        <td>${barHtml(f.percentage, pkg.target)}</td>`;
      filesBody.appendChild(tr);
    }

    document.getElementById('files-section').style.display = '';
    document.getElementById('files-section').scrollIntoView({ behavior: 'smooth' });
  });

  document.getElementById('files-close').addEventListener('click', () => {
    document.getElementById('files-section').style.display = 'none';
  });

  // ── Sunburst chart (simple canvas) ──────────────────────────────────
  const canvas = document.getElementById('sunburst');
  if (canvas && allPackages.length > 0) {
    const ctx = canvas.getContext('2d');
    const cx = canvas.width / 2;
    const cy = canvas.height / 2;
    const outerR = 180;
    const innerR = 70;
    const totalLines = allPackages.reduce((s, p) => s + p.lines, 0);

    let angle = -Math.PI / 2;
    for (const pkg of allPackages.sort((a, b) => b.lines - a.lines)) {
      const sweep = (pkg.lines / totalLines) * Math.PI * 2;
      const col = pctColor(pkg.coverage, pkg.target);
      ctx.fillStyle = col === 'green' ? '#238636' : (col === 'yellow' ? '#9e6a03' : '#da3633');
      ctx.globalAlpha = 0.6 + (pkg.coverage / 100) * 0.4;
      ctx.beginPath();
      ctx.arc(cx, cy, outerR, angle, angle + sweep);
      ctx.arc(cx, cy, innerR, angle + sweep, angle, true);
      ctx.closePath();
      ctx.fill();

      // Label for large segments
      if (sweep > 0.15) {
        const mid = angle + sweep / 2;
        const labelR = (outerR + innerR) / 2;
        const lx = cx + Math.cos(mid) * labelR;
        const ly = cy + Math.sin(mid) * labelR;
        ctx.globalAlpha = 1;
        ctx.fillStyle = '#e6edf3';
        ctx.font = '9px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        const shortName = pkg.name.replace('Encina.', '').replace('Compliance.', 'C.');
        ctx.fillText(shortName, lx, ly);
      }

      angle += sweep;
    }

    // Center text
    ctx.globalAlpha = 1;
    ctx.fillStyle = '#e6edf3';
    ctx.font = 'bold 24px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(`${ov.coverage}%`, cx, cy - 8);
    ctx.font = '11px sans-serif';
    ctx.fillStyle = '#8b949e';
    ctx.fillText('weighted', cx, cy + 12);
  }

  // ── Trend chart (coverage over time) ───────────────────────────────
  try {
    const histResp = await fetch('data/history.json');
    if (histResp.ok) {
      const history = await histResp.json();
      if (history.length > 0) {
        renderTrendChart(history);
      } else {
        document.getElementById('trend-info').textContent = 'No history data yet — will populate after the next CI run.';
      }
    }
  } catch { /* history.json not available yet */ }

  function renderTrendChart(history) {
    const canvas = document.getElementById('trend-chart');
    if (!canvas) return;
    // Size canvas to fill container width
    const container = canvas.parentElement;
    const dpr = window.devicePixelRatio || 1;
    const displayW = container.clientWidth - 40; // subtract card padding
    const displayH = 250;
    canvas.width = displayW * dpr;
    canvas.height = displayH * dpr;
    canvas.style.width = displayW + 'px';
    canvas.style.height = displayH + 'px';
    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);
    const W = displayW, H = displayH;
    const pad = { top: 20, right: 20, bottom: 40, left: 50 };
    const plotW = W - pad.left - pad.right;
    const plotH = H - pad.top - pad.bottom;

    // Data points
    const points = history.map(h => ({
      t: new Date(h.timestamp),
      v: h.coverage
    }));

    const minV = Math.max(0, Math.min(...points.map(p => p.v)) - 5);
    const maxV = Math.min(100, Math.max(...points.map(p => p.v)) + 5);
    const minT = points[0].t.getTime();
    const maxT = points[points.length - 1].t.getTime();
    const rangeT = maxT - minT || 1;
    const rangeV = maxV - minV || 1;

    function x(t) { return pad.left + ((t.getTime() - minT) / rangeT) * plotW; }
    function y(v) { return pad.top + plotH - ((v - minV) / rangeV) * plotH; }

    // Background
    ctx.fillStyle = '#0d1117';
    ctx.fillRect(0, 0, W, H);

    // Grid lines
    ctx.strokeStyle = '#21262d';
    ctx.lineWidth = 1;
    for (let v = Math.ceil(minV / 10) * 10; v <= maxV; v += 10) {
      ctx.beginPath();
      ctx.moveTo(pad.left, y(v));
      ctx.lineTo(W - pad.right, y(v));
      ctx.stroke();
      ctx.fillStyle = '#8b949e';
      ctx.font = '10px sans-serif';
      ctx.textAlign = 'right';
      ctx.textBaseline = 'middle';
      ctx.fillText(v + '%', pad.left - 6, y(v));
    }

    // Target line at 85%
    if (minV <= 85 && maxV >= 85) {
      ctx.strokeStyle = '#9e6a03';
      ctx.lineWidth = 1;
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(pad.left, y(85));
      ctx.lineTo(W - pad.right, y(85));
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = '#9e6a03';
      ctx.font = '9px sans-serif';
      ctx.textAlign = 'left';
      ctx.fillText('target 85%', W - pad.right - 55, y(85) - 6);
    }

    // Line
    ctx.strokeStyle = '#58a6ff';
    ctx.lineWidth = 2;
    ctx.beginPath();
    points.forEach((p, i) => {
      const px = x(p.t), py = y(p.v);
      if (i === 0) ctx.moveTo(px, py);
      else ctx.lineTo(px, py);
    });
    ctx.stroke();

    // Fill area under line
    ctx.globalAlpha = 0.1;
    ctx.fillStyle = '#58a6ff';
    ctx.lineTo(x(points[points.length - 1].t), pad.top + plotH);
    ctx.lineTo(x(points[0].t), pad.top + plotH);
    ctx.closePath();
    ctx.fill();
    ctx.globalAlpha = 1;

    // Dots
    for (const p of points) {
      const px = x(p.t), py = y(p.v);
      ctx.fillStyle = p.v >= 85 ? '#238636' : (p.v >= 68 ? '#9e6a03' : '#da3633');
      ctx.beginPath();
      ctx.arc(px, py, 3, 0, Math.PI * 2);
      ctx.fill();
    }

    // X-axis labels (dates)
    ctx.fillStyle = '#8b949e';
    ctx.font = '9px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';
    const step = Math.max(1, Math.floor(points.length / 8));
    for (let i = 0; i < points.length; i += step) {
      const p = points[i];
      const label = p.t.toLocaleDateString('en', { month: 'short', day: 'numeric' });
      ctx.fillText(label, x(p.t), pad.top + plotH + 6);
    }
    // Always show last date
    if (points.length > 1) {
      const last = points[points.length - 1];
      ctx.fillText(last.t.toLocaleDateString('en', { month: 'short', day: 'numeric' }), x(last.t), pad.top + plotH + 6);
    }

    // Info text
    const latest = points[points.length - 1];
    const first = points[0];
    const delta = latest.v - first.v;
    const arrow = delta > 0 ? '\u2191' : (delta < 0 ? '\u2193' : '\u2192');
    document.getElementById('trend-info').textContent =
      `${points.length} data points \u2014 ${first.t.toLocaleDateString()} to ${latest.t.toLocaleDateString()} \u2014 ${arrow} ${delta > 0 ? '+' : ''}${delta}% change`;
  }
})();
