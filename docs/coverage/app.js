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
    const color = pct >= target ? 'var(--bar-green)' : pct >= target * 0.8 ? 'var(--bar-yellow)' : 'var(--bar-red)';
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
    if (!perFlag || !perFlag[flag]) return null;
    const d = perFlag[flag];
    return d.total > 0 ? d.coverage : 0;
  }

  function flagCell(perFlag, flag, applicable) {
    if (!applicable) return '<td class="na">-</td>';
    const pct = flagPct(perFlag, flag);
    if (pct === null) return '<td class="pct">0%</td>';
    return `<td class="pct">${Math.round(pct)}%</td>`;
  }

  function isApplicable(tests, flag) {
    if (!tests) return false;
    return tests.toLowerCase().includes(flag.charAt(0));
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
    `big-number ${ov.coverage >= 80 ? 'green' : ov.coverage >= 60 ? 'yellow' : 'red'}`;
  document.getElementById('overall-lines').textContent =
    `${ov.covered.toLocaleString()} / ${ov.lines.toLocaleString()} applicable lines covered`;

  // ── Categories ──────────────────────────────────────────────────────
  const catBody = document.querySelector('#category-table tbody');
  const categoryNames = new Set();

  for (const cat of data.categories) {
    categoryNames.add(cat.name);
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
      const tests = pkg.tests || '';
      const gap = pkg.coverage - pkg.target;
      const gapStr = gap >= 0 ? `+${Math.round(gap)}%` : `${Math.round(gap)}%`;
      const gapClass = gap >= 0 ? 'gap-positive' : 'gap-negative';

      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td class="pkg-name" data-pkg="${pkg.name}">${pkg.name}</td>
        <td>${pkg.category}</td>
        ${flagCell(pkg.perFlag, 'unit', isApplicable(tests, 'u'))}
        ${flagCell(pkg.perFlag, 'guard', isApplicable(tests, 'g'))}
        ${flagCell(pkg.perFlag, 'contract', isApplicable(tests, 'c'))}
        ${flagCell(pkg.perFlag, 'property', isApplicable(tests, 'p'))}
        ${flagCell(pkg.perFlag, 'integration', isApplicable(tests, 'i'))}
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
        <td class="num">${f.coveredLines}</td>
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
      ctx.fillStyle = col === 'green' ? '#238636' : col === 'yellow' ? '#9e6a03' : '#da3633';
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
})();
