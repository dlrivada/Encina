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

  function barHtml(pct, target, forceRed = false) {
    let color = 'var(--bar-red)';
    if (forceRed) { /* keep red */ }
    else if (pct >= target) color = 'var(--bar-green)';
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
    // If manifest has targets, use ONLY manifest to determine applicability.
    // Fall back to category only if no manifest targets exist.
    const applicable = perFlagTarget != null
      ? perFlagTarget[flag] != null
      : isApplicableFlag(catTests, flag);
    const pct = flagPct(perFlag, flag);

    if (!applicable) return '<td class="na" title="Not applicable">-</td>';
    if (pct === null) return '<td class="nodata" title="No coverage data (expected)">?</td>';

    const flagTarget = perFlagTarget?.[flag];
    const targetStr = flagTarget != null ? ` / ${flagTarget}%` : '';
    const title = `${Math.round(pct)}%${targetStr}`;

    if (pct === 0) return `<td class="zero" title="${title}">0%</td>`;

    // Color based on per-flag target (compare rounded to avoid 37.96% < 38% showing amber)
    if (flagTarget != null) {
      const rounded = Math.round(pct);
      if (rounded >= flagTarget) return `<td class="pct flag-pass" title="${title}">${rounded}%</td>`;
      if (rounded >= Math.round(flagTarget * 0.8)) return `<td class="pct flag-warn" title="${title}">${rounded}%</td>`;
      return `<td class="pct flag-fail" title="${title}">${rounded}%</td>`;
    }

    return `<td class="pct" title="${title}">${Math.round(pct)}%</td>`;
  }

  // Target cell for a specific flag — shows target% or lines depending on mode
  function flagTargetCell(perFlagTarget, flag, catTests, perFlag) {
    const applicable = perFlagTarget != null
      ? perFlagTarget[flag] != null
      : isApplicableFlag(catTests, flag);
    if (!applicable) return '<td class="na tgt-val">-</td>';
    const tgt = perFlagTarget?.[flag];
    if (tgt == null) return '<td class="tgt-val">-</td>';
    if (showLines) {
      const d = perFlag?.[flag];
      const applicableLines = d?.total ?? 0;
      const targetLines = Math.round(applicableLines * tgt / 100);
      return `<td class="tgt-val">${targetLines.toLocaleString()}</td>`;
    }
    return `<td class="tgt-val">${tgt}%</td>`;
  }

  // Applicable lines cell — shows total lines for this flag or '-' if not applicable
  function flagLinesCell(perFlag, flag, perFlagTarget) {
    const applicable = perFlagTarget != null ? perFlagTarget[flag] != null : false;
    if (!applicable) return '<td class="na tgt-val">-</td>';
    const d = perFlag?.[flag];
    if (!d || d.total === 0) return '<td class="tgt-val">0</td>';
    return `<td class="tgt-val">${d.total.toLocaleString()}</td>`;
  }

  // Show lines mode: coverage as "covered/total" instead of "%"
  let showLines = false;

  function flagCellValue(perFlag, flag, perFlagTarget) {
    const applicable = perFlagTarget != null ? perFlagTarget[flag] != null : false;
    if (!applicable) return '<td class="na">-</td>';
    const d = perFlag?.[flag];
    if (!d) return '<td class="nodata">?</td>';
    if (showLines) {
      const flagTarget = perFlagTarget?.[flag];
      let cls = 'num';
      if (flagTarget != null && d.total > 0) {
        const pct = d.coverage ?? (d.covered * 100 / d.total);
        const rounded = Math.round(pct);
        if (rounded >= flagTarget) cls = 'num flag-pass';
        else if (rounded >= Math.round(flagTarget * 0.8)) cls = 'num flag-warn';
        else cls = 'num flag-fail';
      } else if (d.covered === 0) {
        cls = 'zero';
      }
      return `<td class="${cls}">${d.covered.toLocaleString()}</td>`;
    }
    // Delegate to flagCell for % mode with colors
    return null; // signal to use flagCell
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
    ov.obligations
      ? `${(ov.met ?? 0).toLocaleString()} / ${ov.obligations.toLocaleString()} obligations met`
      : `${ov.lines.toLocaleString()} lines`;

  // Show link to download raw Cobertura XML artifacts if runId is available
  if (data.runId) {
    const rawLink = document.createElement('a');
    rawLink.href = `https://github.com/dlrivada/Encina/actions/runs/${data.runId}`;
    rawLink.textContent = 'Download raw coverage data';
    rawLink.target = '_blank';
    rawLink.style.cssText = 'display:block;margin-top:0.5em;font-size:0.85em;color:#58a6ff;';
    document.getElementById('overall-lines').parentElement?.appendChild(rawLink);
  }

  // ── Prefix-based filter buttons (derived from package names) ─────────
  const filtersDiv = document.querySelector('.filters');
  const allPackages = data.packages || [];

  // Derive group prefixes: Encina.ADO.* → ADO, Encina.Compliance.* → Compliance, etc.
  const prefixGroups = new Set();
  for (const pkg of allPackages) {
    const parts = pkg.name.replace('Encina.', '').split('.');
    if (parts.length > 0 && parts[0] !== 'Encina') prefixGroups.add(parts[0]);
  }
  for (const prefix of [...prefixGroups].sort()) {
    const btn = document.createElement('button');
    btn.className = 'filter-btn';
    btn.dataset.filter = prefix;
    btn.textContent = prefix;
    filtersDiv.appendChild(btn);
  }

  // ── Packages ────────────────────────────────────────────────────────
  const pkgBody = document.querySelector('#package-table tbody');
  let activeFilter = 'all';
  let searchTerm = '';

  // Sort state: { column: string|null, direction: 'asc'|'desc'|null }
  let sortState = { column: null, direction: null };

  // Extract sortable value from a package for a given column
  function sortValue(pkg, col) {
    if (col === 'name') return pkg.name.toLowerCase();
    if (col === 'lines') return pkg.lines;
    if (col === 'combined') return pkg.coverage;
    if (col === 'gap') {
      // Compute effective gap like renderPackages does
      if (!pkg.perFlagTarget || !pkg.perFlag) return 0;
      let worst = Infinity;
      for (const [flag, ft] of Object.entries(pkg.perFlagTarget)) {
        const d = pkg.perFlag?.[flag];
        const pct = d ? (d.total > 0 ? d.coverage : 0) : 0;
        const g = Math.round(pct) - ft;
        if (g < worst) worst = g;
      }
      return worst === Infinity ? 0 : worst;
    }
    // Flag columns: unit, guard, contract, property, integration
    const d = pkg.perFlag?.[col];
    if (!d) return -1; // no data sorts last
    return d.total > 0 ? d.coverage : 0;
  }

  function renderPackages() {
    pkgBody.innerHTML = '';
    const filtered = allPackages.filter(p => {
      if (activeFilter !== 'all' && !p.name.replace('Encina.', '').startsWith(activeFilter)) return false;
      if (searchTerm && !p.name.toLowerCase().includes(searchTerm)) return false;
      return true;
    });

    // Apply sort
    if (sortState.column && sortState.direction) {
      const col = sortState.column;
      const dir = sortState.direction === 'asc' ? 1 : -1;
      filtered.sort((a, b) => {
        const va = sortValue(a, col), vb = sortValue(b, col);
        if (typeof va === 'string') return va.localeCompare(vb) * dir;
        return (va - vb) * dir;
      });
    }

    for (const pkg of filtered) {
      const catTests = null;

      // Effective target = weighted by applicable lines per flag (obligations model)
      let effectiveTarget = 0;
      let allFlagsMeetTarget = true;
      let worstGapFlag = '';
      let worstGapValue = Infinity;
      if (pkg.perFlagTarget && pkg.perFlag) {
        let totalApplicable = 0, totalTargetLines = 0;
        for (const [flag, flagTarget] of Object.entries(pkg.perFlagTarget)) {
          const d = pkg.perFlag?.[flag];
          const flagApplicable = d?.total ?? 0;
          totalApplicable += flagApplicable;
          totalTargetLines += Math.round(flagApplicable * flagTarget / 100);
          const pct = d ? (d.total > 0 ? d.coverage : 0) : 0;
          const roundedPct = Math.round(pct);
          const flagGap = roundedPct - flagTarget;
          if (flagGap < worstGapValue) { worstGapValue = flagGap; worstGapFlag = flag; }
          if (roundedPct < flagTarget) { allFlagsMeetTarget = false; }
        }
        if (totalApplicable > 0) {
          effectiveTarget = Math.round(totalTargetLines * 100 / totalApplicable * 100) / 100;
        }
      } else {
        allFlagsMeetTarget = true; // no targets defined
        worstGapValue = 0;
      }

      // Gap shows worst flag gap — green only when ALL flags meet target
      const gapStr = allFlagsMeetTarget
        ? `+${Math.round(pkg.coverage - effectiveTarget)}%`
        : `${Math.round(worstGapValue)}%`;
      const gapClass = allFlagsMeetTarget ? 'gap-positive' : 'gap-negative';

      // Lines mode gap: covered - target (in lines)
      const coveredLines = Math.round(pkg.coverage * pkg.lines / 100);
      const targetLines = Math.round(effectiveTarget * pkg.lines / 100);
      const gapLinesVal = coveredLines - targetLines;
      const gapLines = allFlagsMeetTarget
        ? `+${gapLinesVal.toLocaleString()}`
        : `${gapLinesVal.toLocaleString()}`;

      // Render flag group: value + target + applicable lines
      function flagGroup(flag) {
        const linesCell = flagCellValue(pkg.perFlag, flag, pkg.perFlagTarget);
        const valueCell = linesCell ?? flagCell(pkg.perFlag, flag, catTests, pkg.perFlagTarget);
        return `${valueCell}${flagTargetCell(pkg.perFlagTarget, flag, catTests, pkg.perFlag)}${flagLinesCell(pkg.perFlag, flag, pkg.perFlagTarget)}`;
      }

      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td class="pkg-name" data-pkg="${pkg.name}">${pkg.name}</td>
        ${flagGroup('unit')}
        ${flagGroup('guard')}
        ${flagGroup('contract')}
        ${flagGroup('property')}
        ${flagGroup('integration')}
        <td class="pct ${showLines ? (allFlagsMeetTarget ? 'flag-pass' : 'flag-fail') : ''}">${showLines ? Math.round(pkg.coverage * pkg.lines / 100).toLocaleString() : pkg.coverage + '%'}</td>
        <td class="${gapClass}">${showLines ? gapLines : gapStr}</td>
        <td>${barHtml(pkg.coverage, effectiveTarget, !allFlagsMeetTarget)}</td>
        <td class="num">${pkg.lines.toLocaleString()}</td>`;
      pkgBody.appendChild(tr);
    }
  }

  renderPackages();

  // ── Column sorting ─────────────────────────────────────────────────
  function updateSortHeaders() {
    document.querySelectorAll('#package-table th[data-sort]').forEach(th => {
      th.classList.remove('sort-asc', 'sort-desc');
      if (th.dataset.sort === sortState.column) {
        if (sortState.direction === 'asc') th.classList.add('sort-asc');
        else if (sortState.direction === 'desc') th.classList.add('sort-desc');
      }
    });
  }

  document.querySelector('#package-table thead').addEventListener('click', e => {
    const th = e.target.closest('th[data-sort]');
    if (!th) return;
    const col = th.dataset.sort;
    if (sortState.column === col) {
      // Cycle: asc → desc → none
      if (sortState.direction === 'asc') sortState.direction = 'desc';
      else if (sortState.direction === 'desc') { sortState.column = null; sortState.direction = null; }
    } else {
      sortState.column = col;
      sortState.direction = 'asc';
    }
    updateSortHeaders();
    renderPackages();
  });

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

  // ── Toggle %/Lines mode ─────────────────────────────────────────────
  document.getElementById('toggle-mode').addEventListener('click', e => {
    showLines = !showLines;
    e.target.textContent = showLines ? 'Show %' : 'Show Lines';
    renderPackages();
  });

  // ── Copy to Excel (TSV) ────────────────────────────────────────────
  document.getElementById('copy-table').addEventListener('click', async e => {
    const btn = e.target;
    const headers = ['Package', 'Unit', 'U Tgt', 'U Lines', 'Guard', 'G Tgt', 'G Lines', 'Contract', 'C Tgt', 'C Lines', 'Property', 'P Tgt', 'P Lines', 'Integ', 'I Tgt', 'I Lines', 'Combined', 'Gap', 'Lines'];
    const rows = [headers.join('\t')];
    for (const pkg of allPackages) {
      const catTests = null;
      const gap = pkg.coverage - effectiveTarget;
      const ft = pkg.perFlagTarget || {};
      const fl = (flag) => pkg.perFlag?.[flag]?.total ?? '-';
      rows.push([
        pkg.name,
        flagCellText(pkg.perFlag, 'unit', catTests), ft.unit != null ? ft.unit + '%' : '-', fl('unit'),
        flagCellText(pkg.perFlag, 'guard', catTests), ft.guard != null ? ft.guard + '%' : '-', fl('guard'),
        flagCellText(pkg.perFlag, 'contract', catTests), ft.contract != null ? ft.contract + '%' : '-', fl('contract'),
        flagCellText(pkg.perFlag, 'property', catTests), ft.property != null ? ft.property + '%' : '-', fl('property'),
        flagCellText(pkg.perFlag, 'integration', catTests), ft.integration != null ? ft.integration + '%' : '-', fl('integration'),
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
        <td>${barHtml(f.percentage, 0)}</td>`;
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

    // Size canvas to fit its card — card height is set by the grid row (matching left column)
    const dpr = window.devicePixelRatio || 1;
    const card = canvas.parentElement;
    const togglesH = card.querySelector('.chart-toggles')?.offsetHeight ?? 0;
    const h2H = card.querySelector('h2')?.offsetHeight ?? 0;
    const padding = 40; // card padding + margins
    const availH = card.clientHeight - h2H - togglesH - padding;
    const availW = card.clientWidth - 40;
    const displaySize = Math.max(Math.min(availW, availH), 250);
    canvas.width = displaySize * dpr;
    canvas.height = displaySize * dpr;
    canvas.style.width = displaySize + 'px';
    canvas.style.height = displaySize + 'px';

    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);
    const cx = displaySize / 2;
    const cy = displaySize / 2;
    const outerR = displaySize * 0.45;
    const innerR = displaySize * 0.175;

    ctx.clearRect(0, 0, displaySize, displaySize);

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
      ovTotalC = ov.met ?? ov.covered ?? 0;
      ovTotalT = ov.obligations ?? ov.lines ?? 1;
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

    // Pre-compute effective target for each package (same logic as table)
    function pkgEffectiveTarget(pkg) {
      if (!pkg.perFlagTarget || !pkg.perFlag) return 50; // fallback
      let totalApplicable = 0, totalTargetLines = 0;
      for (const [flag, ft] of Object.entries(pkg.perFlagTarget)) {
        const d = pkg.perFlag?.[flag];
        const applicable = d?.total ?? 0;
        totalApplicable += applicable;
        totalTargetLines += Math.round(applicable * ft / 100);
      }
      return totalApplicable > 0 ? totalTargetLines * 100 / totalApplicable : 50;
    }

    // Check if all flags meet their individual targets
    function pkgAllFlagsMet(pkg) {
      if (!pkg.perFlagTarget) return false; // no targets = can't be green
      if (!pkg.perFlag) return false;
      for (const [flag, ft] of Object.entries(pkg.perFlagTarget)) {
        const d = pkg.perFlag?.[flag];
        const pct = d ? (d.total > 0 ? d.coverage : 0) : 0;
        if (Math.round(pct) < ft) return false;
      }
      return true;
    }

    let angle = -Math.PI / 2;
    for (const pkg of allPackages.sort((a, b) => b.lines - a.lines)) {
      const sweep = (pkg.lines / totalLines) * Math.PI * 2;
      const cov = pkgCoverage(pkg);
      // Color based on per-flag target compliance (consistent with table)
      const allMet = pkgAllFlagsMet(pkg);
      const effectiveT = pkgEffectiveTarget(pkg);
      // Force red when any flag misses target (same logic as BAR column)
      ctx.fillStyle = allMet ? '#238636' : '#da3633';
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
