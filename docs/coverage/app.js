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

  // 3-state cell with per-flag target coloring
  // perFlagTarget: { "unit": 85, "guard": 70, ... } or null
  function flagCell(perFlag, flag, catTests, perFlagTarget) {
    const applicable = isApplicableFlag(catTests, flag);
    const pct = flagPct(perFlag, flag);

    if (!applicable) return '<td class="na" title="Not applicable for this category">-</td>';
    if (pct === null) return '<td class="nodata" title="No coverage data (expected)">?</td>';

    const flagTarget = perFlagTarget?.[flag];
    const targetStr = flagTarget != null ? ` / ${flagTarget}%` : '';
    const title = `${Math.round(pct)}%${targetStr}`;

    if (pct === 0) return `<td class="zero" title="${title}">0%</td>`;

    // Color based on per-flag target
    if (flagTarget != null) {
      if (pct >= flagTarget) return `<td class="pct flag-pass" title="${title}">${Math.round(pct)}%</td>`;
      if (pct >= flagTarget * 0.8) return `<td class="pct flag-warn" title="${title}">${Math.round(pct)}%</td>`;
      return `<td class="pct flag-fail" title="${title}">${Math.round(pct)}%</td>`;
    }

    return `<td class="pct" title="${title}">${Math.round(pct)}%</td>`;
  }

  // Target cell for a specific flag — shows target% or '-' if not applicable
  function flagTargetCell(perFlagTarget, flag, catTests) {
    const applicable = isApplicableFlag(catTests, flag);
    if (!applicable) return '<td class="na tgt-val">-</td>';
    const tgt = perFlagTarget?.[flag];
    if (tgt == null) return '<td class="tgt-val">-</td>';
    return `<td class="tgt-val">${tgt}%</td>`;
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

      // Package is green only when ALL flags meet their individual target
      let allFlagsMeetTarget = true;
      if (pkg.perFlagTarget) {
        for (const [flag, flagTarget] of Object.entries(pkg.perFlagTarget)) {
          const pct = flagPct(pkg.perFlag, flag);
          if (pct === null || pct < flagTarget) { allFlagsMeetTarget = false; break; }
        }
      } else {
        allFlagsMeetTarget = gap >= 0;
      }
      const gapClass = allFlagsMeetTarget ? 'gap-positive' : 'gap-negative';

      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td class="pkg-name" data-pkg="${pkg.name}">${pkg.name}</td>
        <td>${pkg.category}</td>
        ${flagCell(pkg.perFlag, 'unit', catTests, pkg.perFlagTarget)}
        ${flagTargetCell(pkg.perFlagTarget, 'unit', catTests)}
        ${flagCell(pkg.perFlag, 'guard', catTests, pkg.perFlagTarget)}
        ${flagTargetCell(pkg.perFlagTarget, 'guard', catTests)}
        ${flagCell(pkg.perFlag, 'contract', catTests, pkg.perFlagTarget)}
        ${flagTargetCell(pkg.perFlagTarget, 'contract', catTests)}
        ${flagCell(pkg.perFlag, 'property', catTests, pkg.perFlagTarget)}
        ${flagTargetCell(pkg.perFlagTarget, 'property', catTests)}
        ${flagCell(pkg.perFlag, 'integration', catTests, pkg.perFlagTarget)}
        ${flagTargetCell(pkg.perFlagTarget, 'integration', catTests)}
        <td class="pct">${pkg.coverage}%</td>
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
    const headers = ['Package', 'Category', 'Unit', 'U Tgt', 'Guard', 'G Tgt', 'Contract', 'C Tgt', 'Property', 'P Tgt', 'Integ', 'I Tgt', 'Combined', 'Gap', 'Lines'];
    const rows = [headers.join('\t')];
    for (const pkg of allPackages) {
      const catTests = catTestsMap[pkg.category] || '';
      const gap = pkg.coverage - pkg.target;
      const ft = pkg.perFlagTarget || {};
      rows.push([
        pkg.name, pkg.category,
        flagCellText(pkg.perFlag, 'unit', catTests), ft.unit != null ? ft.unit + '%' : '-',
        flagCellText(pkg.perFlag, 'guard', catTests), ft.guard != null ? ft.guard + '%' : '-',
        flagCellText(pkg.perFlag, 'contract', catTests), ft.contract != null ? ft.contract + '%' : '-',
        flagCellText(pkg.perFlag, 'property', catTests), ft.property != null ? ft.property + '%' : '-',
        flagCellText(pkg.perFlag, 'integration', catTests), ft.integration != null ? ft.integration + '%' : '-',
        Math.round(pkg.coverage) + '%',
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

  // ── Flag colors (consistent across charts) ────────────────────────
  const FLAG_COLORS = {
    combined: '#58a6ff',
    unit: '#8b5cf6',
    guard: '#f97316',
    contract: '#06b6d4',
    property: '#84cc16',
    integration: '#ec4899'
  };

  // ── Toggle button logic ────────────────────────────────────────────
  // Combined = radio (deselects all others). Others = checkboxes (deselect Combined).
  function setupToggles(containerId, onChangeCallback) {
    const container = document.getElementById(containerId);
    if (!container) return;
    const buttons = container.querySelectorAll('.toggle-btn');

    buttons.forEach(btn => {
      btn.addEventListener('click', () => {
        const flag = btn.dataset.flag;
        if (flag === 'combined') {
          // Radio: activate Combined, deactivate all others
          buttons.forEach(b => b.classList.toggle('active', b.dataset.flag === 'combined'));
        } else {
          // Checkbox: toggle this flag, deactivate Combined
          btn.classList.toggle('active');
          container.querySelector('[data-flag="combined"]')?.classList.remove('active');
          // If nothing selected, reactivate Combined
          const anyActive = [...buttons].some(b => b.dataset.flag !== 'combined' && b.classList.contains('active'));
          if (!anyActive) {
            container.querySelector('[data-flag="combined"]')?.classList.add('active');
          }
        }
        const selected = [...buttons].filter(b => b.classList.contains('active')).map(b => b.dataset.flag);
        onChangeCallback(selected);
      });
    });

    return [...buttons].filter(b => b.classList.contains('active')).map(b => b.dataset.flag);
  }

  // ── Sunburst chart ─────────────────────────────────────────────────
  function renderSunburst(selectedFlags) {
    const canvas = document.getElementById('sunburst');
    if (!canvas || allPackages.length === 0) return;
    const ctx = canvas.getContext('2d');
    const cx = canvas.width / 2;
    const cy = canvas.height / 2;
    const outerR = 180;
    const innerR = 70;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const isCombined = selectedFlags.includes('combined');
    const totalLines = allPackages.reduce((s, p) => s + p.lines, 0);

    // Compute coverage for each package based on selected flags
    function pkgCoverage(pkg) {
      if (isCombined) return pkg.coverage;
      if (!pkg.perFlag) return 0;
      let totalT = 0, totalC = 0;
      for (const flag of selectedFlags) {
        const d = pkg.perFlag[flag];
        if (d && d.total > 0) { totalT += d.total; totalC += d.covered; }
      }
      return totalT > 0 ? Math.round(totalC * 100 / totalT * 100) / 100 : 0;
    }

    // Compute overall for center text
    let ovTotalT = 0, ovTotalC = 0;
    if (isCombined) {
      ovTotalC = ov.covered; ovTotalT = ov.lines;
    } else {
      for (const pkg of allPackages) {
        if (!pkg.perFlag) continue;
        for (const flag of selectedFlags) {
          const d = pkg.perFlag[flag];
          if (d && d.total > 0) { ovTotalT += d.total; ovTotalC += d.covered; }
        }
      }
    }
    const overallPctForCenter = ovTotalT > 0 ? Math.round(ovTotalC * 100 / ovTotalT * 100) / 100 : 0;

    let angle = -Math.PI / 2;
    for (const pkg of allPackages.sort((a, b) => b.lines - a.lines)) {
      const sweep = (pkg.lines / totalLines) * Math.PI * 2;
      const cov = pkgCoverage(pkg);
      const col = pctColor(cov, pkg.target);
      ctx.fillStyle = col === 'green' ? '#238636' : (col === 'yellow' ? '#9e6a03' : '#da3633');
      ctx.globalAlpha = 0.6 + (cov / 100) * 0.4;
      ctx.beginPath();
      ctx.arc(cx, cy, outerR, angle, angle + sweep);
      ctx.arc(cx, cy, innerR, angle + sweep, angle, true);
      ctx.closePath();
      ctx.fill();

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
    ctx.fillText(`${overallPctForCenter}%`, cx, cy - 8);
    ctx.font = '11px sans-serif';
    ctx.fillStyle = '#8b949e';
    const label = isCombined ? 'weighted' : selectedFlags.join('+');
    ctx.fillText(label, cx, cy + 12);
  }

  // Initial sunburst render
  renderSunburst(['combined']);
  setupToggles('sunburst-toggles', renderSunburst);

  // ── Trend chart (coverage over time) ───────────────────────────────
  let historyData = null;

  try {
    const histResp = await fetch('data/history.json');
    if (histResp.ok) {
      historyData = await histResp.json();
      if (historyData.length > 0) {
        renderTrendChart(historyData, ['combined']);
      } else {
        document.getElementById('trend-info').textContent = 'No history data yet — will populate after the next CI run.';
      }
    }
  } catch { /* history.json not available yet */ }

  setupToggles('trend-toggles', (selected) => {
    if (historyData && historyData.length > 0) renderTrendChart(historyData, selected);
  });

  function renderTrendChart(history, selectedFlags) {
    const canvas = document.getElementById('trend-chart');
    if (!canvas) return;
    const container = canvas.parentElement;
    const dpr = window.devicePixelRatio || 1;
    const displayW = container.clientWidth - 40;
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

    const isCombined = selectedFlags.includes('combined');

    // Build series: one per selected flag
    const series = [];
    if (isCombined) {
      series.push({
        flag: 'combined',
        color: FLAG_COLORS.combined,
        points: history.map(h => ({ t: new Date(h.timestamp), v: h.coverage ?? 0 }))
      });
    } else {
      for (const flag of selectedFlags) {
        series.push({
          flag,
          color: FLAG_COLORS[flag] || '#888',
          points: history.map(h => ({
            t: new Date(h.timestamp),
            v: h.perFlag?.[flag] ?? 0
          }))
        });
      }
    }

    // Compute Y range across all series
    const allValues = series.flatMap(s => s.points.map(p => p.v));
    const minV = Math.max(0, Math.min(...allValues) - 5);
    const maxV = Math.min(100, Math.max(...allValues) + 5);
    const timestamps = series[0].points.map(p => p.t);
    const minT = timestamps[0].getTime();
    const maxT = timestamps[timestamps.length - 1].getTime();
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

    // Draw each series
    for (const s of series) {
      // Line
      ctx.strokeStyle = s.color;
      ctx.lineWidth = 2;
      ctx.beginPath();
      s.points.forEach((p, i) => {
        const px = x(p.t), py = y(p.v);
        if (i === 0) ctx.moveTo(px, py); else ctx.lineTo(px, py);
      });
      ctx.stroke();

      // Fill area (only for single series)
      if (series.length === 1) {
        ctx.globalAlpha = 0.1;
        ctx.fillStyle = s.color;
        ctx.lineTo(x(s.points[s.points.length - 1].t), pad.top + plotH);
        ctx.lineTo(x(s.points[0].t), pad.top + plotH);
        ctx.closePath();
        ctx.fill();
        ctx.globalAlpha = 1;
      }

      // Dots
      for (const p of s.points) {
        const px = x(p.t), py = y(p.v);
        ctx.fillStyle = s.color;
        ctx.beginPath();
        ctx.arc(px, py, 3, 0, Math.PI * 2);
        ctx.fill();
      }
    }

    // Legend (when multiple series)
    if (series.length > 1) {
      let lx = pad.left + 5;
      ctx.font = '10px sans-serif';
      ctx.textBaseline = 'top';
      ctx.textAlign = 'left';
      for (const s of series) {
        ctx.fillStyle = s.color;
        ctx.fillRect(lx, 4, 12, 12);
        ctx.fillStyle = '#e6edf3';
        ctx.fillText(s.flag, lx + 16, 5);
        lx += ctx.measureText(s.flag).width + 30;
      }
    }

    // X-axis labels
    ctx.fillStyle = '#8b949e';
    ctx.font = '9px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';
    const step = Math.max(1, Math.floor(timestamps.length / 8));
    for (let i = 0; i < timestamps.length; i += step) {
      const t = timestamps[i];
      ctx.fillText(t.toLocaleDateString('en', { month: 'short', day: 'numeric' }), x(t), pad.top + plotH + 6);
    }
    if (timestamps.length > 1) {
      const last = timestamps[timestamps.length - 1];
      ctx.fillText(last.toLocaleDateString('en', { month: 'short', day: 'numeric' }), x(last), pad.top + plotH + 6);
    }

    // Info text
    const mainSeries = series[0];
    const latest = mainSeries.points[mainSeries.points.length - 1];
    const first = mainSeries.points[0];
    const delta = latest.v - first.v;
    const arrow = delta > 0 ? '\u2191' : (delta < 0 ? '\u2193' : '\u2192');
    const flagLabel = isCombined ? '' : ` (${selectedFlags.join('+')})`;
    document.getElementById('trend-info').textContent =
      `${mainSeries.points.length} data points${flagLabel} \u2014 ${first.t.toLocaleDateString()} to ${latest.t.toLocaleDateString()} \u2014 ${arrow} ${delta > 0 ? '+' : ''}${delta}% change`;
  }
})();
