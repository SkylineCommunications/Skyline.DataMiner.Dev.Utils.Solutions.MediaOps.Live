var allSources = [], allDestinations = [];
var selectedSource = null, selectedDestination = null;
// Console is driven by the destination's levels. Each row: { dstLevel, srcVsg, srcLevel }
// dstLevel = { Id, Name, Number } (fixed per row), srcVsg = { Id, Name } | null, srcLevel = { Id, Name, Number } | null
var consoleRows = [];
var activeRowIndex = null; // row index waiting for source assignment
var selectedDiscLevels = new Set();
var dragData = null;
var vsgLevelsCache = {};

async function loadData() {
    try {
        var r = await Promise.all([fetch('/api/controlsurface/sources'), fetch('/api/controlsurface/destinations')]);
        if (!r[0].ok || !r[1].ok) throw new Error('Server error');
        allSources = await r[0].json();
        allDestinations = await r[1].json();
        applyFilters();
        setStatus('Ready');
    } catch (e) {
        setStatus('Error loading data: ' + e.message);
    }
}

var activeSrcFilters = new Set();
var activeDstFilters = new Set();

function initFilterChips() {
    document.querySelectorAll('#srcFilterBar .filter-chip').forEach(function(chip) {
        chip.onclick = function() {
            var f = chip.dataset.filter;
            if (activeSrcFilters.has(f)) { activeSrcFilters.delete(f); chip.classList.remove('active'); }
            else { activeSrcFilters.add(f); chip.classList.add('active'); }
            applyFilters();
        };
    });
    document.querySelectorAll('#dstFilterBar .filter-chip').forEach(function(chip) {
        chip.onclick = function() {
            var f = chip.dataset.filter;
            if (activeDstFilters.has(f)) { activeDstFilters.delete(f); chip.classList.remove('active'); }
            else { activeDstFilters.add(f); chip.classList.add('active'); }
            applyFilters();
        };
    });
}

function applyFilters() {
    var sf = document.getElementById('sourceFilter').value.toLowerCase();
    var df = document.getElementById('destFilter').value.toLowerCase();

    var sources = allSources.filter(function(s) {
        if (sf && s.Name.toLowerCase().indexOf(sf) < 0) return false;
        var isConnected = s.ConnectedDestinations && s.ConnectedDestinations.length > 0;
        if (activeSrcFilters.has('src-connected') && !isConnected) return false;
        if (activeSrcFilters.has('src-unconnected') && isConnected) return false;
        return true;
    });

    var destinations = allDestinations.filter(function(d) {
        if (df && d.Name.toLowerCase().indexOf(df) < 0) return false;
        var hasMapping = d.ConnectedLevelMappings && d.ConnectedLevelMappings.length > 0;
        var isConnected = d.ConnectedSourceName != null || hasMapping;
        var totalLevels = d.LevelNames ? d.LevelNames.length : 0;
        var connectedLevels = hasMapping ? d.ConnectedLevelMappings.length : 0;
        var isPartial = isConnected && totalLevels > 0 && connectedLevels < totalLevels;
        var isUnconnected = !isConnected;
        var isShuffled = hasMapping && d.ConnectedLevelMappings.some(function(m) { return m.SourceLevel !== m.DestinationLevel; });
        var isMultiSource = hasMapping && (function() {
            var srcs = {}; d.ConnectedLevelMappings.forEach(function(m) { srcs[m.SourceVsgName] = true; });
            return Object.keys(srcs).length > 1;
        })();
        if (activeDstFilters.has('dst-connected') && !isConnected) return false;
        if (activeDstFilters.has('dst-unconnected') && !isUnconnected) return false;
        if (activeDstFilters.has('dst-partial') && !isPartial) return false;
        if (activeDstFilters.has('dst-shuffled') && !isShuffled) return false;
        if (activeDstFilters.has('dst-multisrc') && !isMultiSource) return false;
        return true;
    });

    renderSources(sources);
    renderDestinations(destinations);
}

function esc(str) {
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

async function getVsgLevels(vsgId) {
    if (vsgLevelsCache[vsgId]) return vsgLevelsCache[vsgId];
    var res = await fetch('/api/controlsurface/vsgs/' + vsgId + '/levels');
    var levels = await res.json();
    levels.sort(function(a,b){ return a.Number - b.Number; });
    vsgLevelsCache[vsgId] = levels;
    return levels;
}

function renderSources(sources) {
    var list = document.getElementById('sourceList');
    list.innerHTML = '';
    if (!sources.length) { list.innerHTML = '<li class="no-results">No sources found</li>'; return; }
    sources.forEach(function(s) {
        var li = document.createElement('li');
        li.className = 'vsg-item' + (selectedSource && selectedSource.Id === s.Id ? ' selected' : '');
        li.dataset.id = s.Id;
        li.innerHTML = esc(s.Name);
        if (s.ConnectedDestinations && s.ConnectedDestinations.length > 0) {
            var badge = document.createElement('div');
            badge.className = 'src-connected-badge';
            var text = document.createElement('span');
            text.className = 'src-conn-text';
            text.innerHTML = '\u2713 ' + s.ConnectedDestinations.join(', ');
            badge.appendChild(text);
            var icon = document.createElement('div');
            icon.className = 'src-conn-icon';
            icon.innerHTML = '\u2699';
            icon.title = 'Manage connections';
            icon.onclick = function(e) { e.stopPropagation(); showSourceConnectionsModal(s); };
            badge.appendChild(icon);
            li.appendChild(badge);
        }
        if (s.PendingDisconnectDestinations && s.PendingDisconnectDestinations.length > 0) {
            var pendingBadge = document.createElement('div');
            pendingBadge.className = 'pending-disconnect-info';
            pendingBadge.innerHTML = '\u23f3 Releasing: ' + s.PendingDisconnectDestinations.join(', ');
            li.appendChild(pendingBadge);
        }
        li.draggable = true;
        li.ondragstart = function(e) {
            dragData = { id: s.Id, name: s.Name };
            li.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'copy';
            e.dataTransfer.setData('text/plain', s.Id);
        };
        li.ondragend = function() { li.classList.remove('dragging'); dragData = null; };
        li.onclick = function() { handleSourceClick(s); };
        list.appendChild(li);
    });
}

function renderDestinations(destinations) {
    var list = document.getElementById('destList');
    list.innerHTML = '';
    if (!destinations.length) { list.innerHTML = '<li class="no-results">No destinations found</li>'; return; }
    destinations.forEach(function(d) {
        var li = document.createElement('li');
        li.className = 'vsg-item' + (selectedDestination && selectedDestination.Id === d.Id ? ' selected' : '');
        li.dataset.id = d.Id;
        li.innerHTML = esc(d.Name);
        if (d.ConnectedSourceName) li.innerHTML += '<div class="connected-info">\u2713 ' + esc(d.ConnectedSourceName) + '</div>';
        if (d.ConnectedLevelMappings && d.ConnectedLevelMappings.length) {
            var hasShuffled = d.ConnectedLevelMappings.some(function(m){ return m.SourceLevel !== m.DestinationLevel; });
            var multipleSourceVsgs = new Set(d.ConnectedLevelMappings.map(function(m){ return m.SourceVsgName; }).filter(Boolean)).size > 1;
            var icon = multipleSourceVsgs ? '\ud83d\udd00 Multi-source' : (hasShuffled ? '\u26a0 Shuffled' : '\ud83d\udd17 Mapped');
            var pills = d.ConnectedLevelMappings.map(function(m){
                var shuffled = m.SourceLevel !== m.DestinationLevel;
                var label = m.SourceVsgName && multipleSourceVsgs
                    ? esc(m.SourceVsgName) + '/' + esc(m.SourceLevel)
                    : esc(m.SourceLevel);
                return '<span class="mapping-pill' + (shuffled ? ' shuffled' : '') + '">' + label + ' \u2192 ' + esc(m.DestinationLevel) + '</span>';
            }).join('');
            li.innerHTML += '<div class="mapping-info"><span class="mapping-icon">' + icon + '</span>' + pills + '</div>';
        }
        if (d.PendingConnectedSourceName) li.innerHTML += '<div class="pending-info">\u23f3 ' + esc(d.PendingConnectedSourceName) + '</div>';
        if (d.PendingDisconnects && d.PendingDisconnects.length > 0) {
            var pendingText = d.PendingDisconnects.map(function(pd) { return esc(pd.SourceVsgName) + ': ' + pd.LevelNames.map(esc).join(', '); }).join('; ');
            li.innerHTML += '<div class="pending-disconnect-info">\u23f3 Releasing: ' + pendingText + '</div>';
        }
        // Show unconnected levels indicator
        if (d.LevelNames && d.LevelNames.length) {
            var connectedDstLevels = (d.ConnectedLevelMappings || []).map(function(m){ return m.DestinationLevel; });
            var unconnected = d.LevelNames.filter(function(n){ return connectedDstLevels.indexOf(n) < 0; });
            if (unconnected.length > 0 && connectedDstLevels.length > 0) {
                li.innerHTML += '<div class="partial-info">' + connectedDstLevels.length + '/' + d.LevelNames.length + ' levels connected</div>';
                li.innerHTML += '<div class="unconnected-info">\u2716 ' + unconnected.map(esc).join(', ') + '</div>';
            } else if (unconnected.length === d.LevelNames.length && !d.ConnectedSourceName && !d.PendingConnectedSourceName) {
                li.innerHTML += '<div class="unconnected-info">\u25CB ' + d.LevelNames.length + ' levels &mdash; not connected</div>';
            }
        }
        li.onclick = function() { handleDestinationClick(d); };
        list.appendChild(li);
    });
}

// --- Click handlers ---
function handleSourceClick(s) {
    // If a specific row is active, assign this source to that row only
    if (activeRowIndex !== null) {
        assignSourceToSingleRow(activeRowIndex, s);
        activeRowIndex = null;
        renderConsole();
        updateButtons();
        return;
    }
    // Otherwise, select this source and fill all matching levels
    selectedSource = s;
    document.querySelectorAll('#sourceList .vsg-item').forEach(function(e) { e.classList.remove('selected'); });
    var el = document.querySelector('#sourceList .vsg-item[data-id="' + s.Id + '"]');
    if (el) el.classList.add('selected');
    fillSourceIntoConsole(s);
}

function handleDestinationClick(d) {
    selectedDestination = d;
    document.querySelectorAll('#destList .vsg-item').forEach(function(e) { e.classList.remove('selected'); });
    var el = document.querySelector('#destList .vsg-item[data-id="' + d.Id + '"]');
    if (el) el.classList.add('selected');
    activeRowIndex = null;
    populateConsoleFromDestination(d);
}

async function populateConsoleFromDestination(d) {
    setStatus('Loading destination levels...');
    try {
        var levels = await getVsgLevels(d.Id);
        // Reset console: one row per destination level, fixed
        consoleRows = levels.map(function(l) {
            return { dstLevel: l, srcVsg: null, srcLevel: null };
        });
        // If a source is already selected, auto-fill matching
        if (selectedSource) {
            await fillSourceIntoConsole(selectedSource);
        } else {
            renderConsole();
            updateButtons();
        }
        document.getElementById('consoleTitle').textContent = d.Name;
        setStatus('Ready');
    } catch(e) { setStatus('Error: ' + e.message); }
}

async function fillSourceIntoConsole(s) {
    if (!consoleRows.length) return; // need a destination first
    try {
        var srcLevels = await getVsgLevels(s.Id);
        // Match source levels to destination levels by name (straight mapping default)
        var srcByName = {};
        srcLevels.forEach(function(l) { srcByName[l.Name.toLowerCase()] = l; });
        consoleRows.forEach(function(row) {
            var match = srcByName[row.dstLevel.Name.toLowerCase()];
            if (match) {
                row.srcVsg = { Id: s.Id, Name: s.Name };
                row.srcLevel = match;
            }
        });
        renderConsole();
        updateButtons();
    } catch(e) { setStatus('Error loading source levels: ' + e.message); }
}

async function assignSourceToSingleRow(rowIndex, s) {
    try {
        var srcLevels = await getVsgLevels(s.Id);
        var row = consoleRows[rowIndex];
        // Default: pick the source level matching the destination level name, or first available
        var match = srcLevels.find(function(l) { return l.Name.toLowerCase() === row.dstLevel.Name.toLowerCase(); });
        row.srcVsg = { Id: s.Id, Name: s.Name };
        row.srcLevel = match || srcLevels[0] || null;
        renderConsole();
        updateButtons();
    } catch(e) { setStatus('Error: ' + e.message); }
}

// --- Console rendering ---
function showLevelPicker(rowIndex, anchorEl) {
    // Remove any existing picker
    var existing = document.querySelector('.level-picker');
    if (existing) existing.remove();

    var row = consoleRows[rowIndex];
    if (!row.srcVsg) return;
    var levels = vsgLevelsCache[row.srcVsg.Id];
    if (!levels || levels.length < 2) return;

    var picker = document.createElement('div');
    picker.className = 'level-picker';
    levels.forEach(function(l) {
        var chip = document.createElement('span');
        chip.className = 'lp-chip' + (row.srcLevel && row.srcLevel.Id === l.Id ? ' active' : '');
        chip.textContent = l.Name;
        chip.onmousedown = function(e) {
            e.stopPropagation();
            e.preventDefault();
            row.srcLevel = l;
            picker.remove();
            renderConsole();
            updateButtons();
        };
        picker.appendChild(chip);
    });

    // Position above the anchor element
    document.body.appendChild(picker);
    var rect = anchorEl.getBoundingClientRect();
    picker.style.left = rect.left + 'px';
    picker.style.top = (rect.top - picker.offsetHeight - 4) + 'px';

    // Close on outside click
    setTimeout(function() {
        function handler(e) {
            if (picker.contains(e.target)) return;
            picker.remove();
            document.removeEventListener('mousedown', handler);
        }
        document.addEventListener('mousedown', handler);
    }, 50);
}

function renderConsole() {
    var table = document.getElementById('levelTable');
    if (!consoleRows.length) {
        table.innerHTML = '<div class="center-empty">Select a <strong>destination</strong> to populate the level console.<br><span class="hint">Then select or drag sources to assign them.</span></div>';
        return;
    }
    table.innerHTML = '';
    consoleRows.forEach(function(row, idx) {
        var rowEl = document.createElement('div');
        rowEl.className = 'level-row';

        // Source cell (left)
        var srcCell = document.createElement('div');
        srcCell.className = 'level-cell src-cell ' + (row.srcVsg && row.srcLevel ? 'filled' : 'empty');
        if (activeRowIndex === idx) srcCell.classList.add('active');
        if (row.srcVsg && row.srcLevel) {
            var vsgSpan = document.createElement('span');
            vsgSpan.className = 'vsg-name';
            vsgSpan.textContent = row.srcVsg.Name;
            srcCell.appendChild(vsgSpan);
            var levelBtn = document.createElement('span');
            levelBtn.className = 'level-name level-cycle';
            levelBtn.textContent = row.srcLevel.Name + ' \u25BE';
            levelBtn.title = 'Click to pick source level';
            levelBtn.onclick = function(e) {
                e.stopPropagation();
                e.preventDefault();
                showLevelPicker(idx, srcCell);
            };
            srcCell.appendChild(levelBtn);
        } else {
            srcCell.textContent = 'assign source';
        }
        srcCell.onclick = function(e) {
            if (e.target.closest && e.target.closest('.level-cycle')) return;
            if (e.target.classList && e.target.classList.contains('level-cycle')) return;
            if (document.querySelector('.level-picker')) return;
            activeRowIndex = (activeRowIndex === idx) ? null : idx;
            renderConsole();
        };
        // Drop target
        srcCell.ondragover = function(e) { if (dragData) { e.preventDefault(); srcCell.classList.add('drop-target'); } };
        srcCell.ondragleave = function() { srcCell.classList.remove('drop-target'); };
        srcCell.ondrop = function(e) {
            e.preventDefault(); srcCell.classList.remove('drop-target');
            if (!dragData) return;
            var vsg = allSources.find(function(s) { return s.Id === dragData.id; });
            if (vsg) { assignSourceToSingleRow(idx, vsg); activeRowIndex = null; }
        };

        // Arrow
        var arrow = document.createElement('div');
        arrow.className = 'arrow';
        arrow.textContent = '\u2192';

        // Destination cell (right) - fixed, shows level name
        var dstCell = document.createElement('div');
        dstCell.className = 'level-cell dst-cell filled';
        dstCell.innerHTML = '<span class="level-name">' + esc(row.dstLevel.Name) + '</span>';

        rowEl.appendChild(srcCell);
        rowEl.appendChild(arrow);
        rowEl.appendChild(dstCell);
        table.appendChild(rowEl);
    });
}

// --- Buttons ---
function updateButtons() {
    var canConnect = consoleRows.some(function(r) { return r.srcVsg && r.srcLevel; });
    document.getElementById('btnConnect').disabled = !canConnect;
    document.getElementById('btnDisconnect').disabled = !selectedDestination;
}

document.getElementById('btnConnect').onclick = async function() {
    // Group by source VSG and build level mappings
    var requests = {};
    consoleRows.forEach(function(r) {
        if (!r.srcVsg || !r.srcLevel) return;
        var dstVsgId = selectedDestination.Id;
        var key = r.srcVsg.Id + '|' + dstVsgId;
        if (!requests[key]) requests[key] = { SourceId: r.srcVsg.Id, DestinationId: dstVsgId, LevelMappings: [] };
        requests[key].LevelMappings.push({ SourceLevelId: r.srcLevel.Id, DestinationLevelId: r.dstLevel.Id });
    });
    var reqs = Object.values(requests);
    if (!reqs.length) return;

    // Optimistically update all affected destinations to show pending connects
    reqs.forEach(function(req) {
        var src = allSources.find(function(s) { return s.Id === req.SourceId; });
        var dst = allDestinations.find(function(d) { return d.Id === req.DestinationId; });
        if (src && dst) {
            dst.PendingConnectedSourceName = src.Name;
            // Also optimistically add the levels to pending connections for sources
            if (!src.PendingDisconnectDestinations) src.PendingDisconnectDestinations = [];
            if (src.PendingDisconnectDestinations.indexOf(dst.Name) < 0) {
                src.PendingDisconnectDestinations.push(dst.Name);
            }
        }
    });
    applyFilters();

    setStatus('Taking...');
    try {
        for (var i = 0; i < reqs.length; i++) {
            var res = await fetch('/api/controlsurface/connect', {
                method: 'POST', headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(reqs[i])
            });
            if (!res.ok) {
                var errText = await res.text();
                throw new Error(errText || 'Take failed (HTTP ' + res.status + ')');
            }
        }
        setStatus('Take initiated');
        // Wait for SSE event to update UI
    } catch(e) { setStatus('Take failed'); showError(e.message); }
};

document.getElementById('btnDisconnect').onclick = async function() {
    if (!selectedDestination) return;
    setStatus('Loading levels...');
    try {
        var levels = await getVsgLevels(selectedDestination.Id);
        showDisconnectModal(levels);
        setStatus('Ready');
    } catch(e) { setStatus('Error: ' + e.message); }
};

// --- Disconnect modal ---
function showDisconnectModal(levels) {
    document.getElementById('disconnectModalTitle').textContent = selectedDestination.Name;
    var list = document.getElementById('discLevelList');
    list.innerHTML = '';
    selectedDiscLevels = new Set();
    levels.forEach(function(l) {
        selectedDiscLevels.add(l.Id);
        var chip = document.createElement('div');
        chip.className = 'disc-level-chip selected';
        chip.textContent = l.Name;
        chip.onclick = function() {
            if (selectedDiscLevels.has(l.Id)) { selectedDiscLevels.delete(l.Id); chip.classList.remove('selected'); }
            else { selectedDiscLevels.add(l.Id); chip.classList.add('selected'); }
        };
        list.appendChild(chip);
    });
    document.getElementById('disconnectModal').classList.add('visible');
}

document.getElementById('btnDisconnectCancel').onclick = function() {
    document.getElementById('disconnectModal').classList.remove('visible');
};

document.getElementById('btnDoDisconnectLevels').onclick = async function() {
    if (!selectedDiscLevels.size) { setStatus('Select at least one level.'); return; }

    // Optimistically update all affected sources to show pending disconnects
    var dst = selectedDestination;
    if (dst && dst.ConnectedLevelMappings) {
        dst.ConnectedLevelMappings.forEach(function(mapping) {
            // Check if this level is being disconnected
            if (selectedDiscLevels.has(mapping.DestinationLevelId)) {
                if (!dst.PendingDisconnects) dst.PendingDisconnects = [];
                var existingPending = dst.PendingDisconnects.find(function(pd) { return pd.SourceVsgName === mapping.SourceVsgName; });
                if (!existingPending) {
                    existingPending = { SourceVsgName: mapping.SourceVsgName, LevelNames: [] };
                    dst.PendingDisconnects.push(existingPending);
                }
                if (existingPending.LevelNames.indexOf(mapping.DestinationLevel) < 0) {
                    existingPending.LevelNames.push(mapping.DestinationLevel);
                }
            }
        });
    }
    applyFilters();

    setStatus('Releasing...');
    try {
        var body = { DestinationId: selectedDestination.Id, LevelIds: Array.from(selectedDiscLevels) };
        var res = await fetch('/api/controlsurface/disconnect', {
            method: 'POST', headers: {'Content-Type': 'application/json'},
            body: JSON.stringify(body)
        });
        if (!res.ok) {
            var errText = await res.text();
            throw new Error(errText || 'Release failed (HTTP ' + res.status + ')');
        }
        setStatus('Release initiated');
        document.getElementById('disconnectModal').classList.remove('visible');
        // Wait for SSE event to update UI
    } catch(e) { setStatus('Release failed'); showError(e.message); }
};

function setStatus(msg) { document.getElementById('status').textContent = msg; }

function showError(msg) {
    document.getElementById('errorModalMessage').textContent = msg;
    document.getElementById('errorModal').classList.add('visible');
}

document.getElementById('btnErrorClose').onclick = function() {
    document.getElementById('errorModal').classList.remove('visible');
};

var currentSourceModal = null; // Track which source is being viewed in the modal

function showSourceConnectionsModal(source) {
    currentSourceModal = source;
    document.getElementById('srcConnTitle').textContent = esc(source.Name);
    var list = document.getElementById('srcConnList');
    list.innerHTML = '';
    if (!source.ConnectedDestinations || source.ConnectedDestinations.length === 0) {
        list.innerHTML = '<p style="color:var(--text-dim);font-size:11px;">No connections</p>';
        document.getElementById('sourceConnectionsModal').classList.add('visible');
        return;
    }
    // Fetch detailed connection info for each destination
    source.ConnectedDestinations.forEach(function(dstName) {
        var dst = allDestinations.find(function(d) { return d.Name === dstName; });
        if (!dst) return;
        var card = document.createElement('div');
        card.className = 'src-conn-card';
        var header = document.createElement('div');
        header.className = 'src-conn-card-header';
        header.innerHTML = '<span>' + esc(dst.Name) + '</span>';
        card.appendChild(header);
        // Get levels connected from this source to this destination
        var mappings = dst.ConnectedLevelMappings ? dst.ConnectedLevelMappings.filter(function(m) { return m.SourceVsgName === source.Name; }) : [];
        // Get levels being disconnected from this source to this destination
        var pendingDisconnects = dst.PendingDisconnects ? dst.PendingDisconnects.filter(function(pd) { return pd.SourceVsgName === source.Name; }) : [];

        if (mappings.length > 0) {
            var levelsDiv = document.createElement('div');
            levelsDiv.className = 'src-conn-card-levels';
            mappings.forEach(function(m) {
                var level = document.createElement('span');
                level.className = 'src-conn-level';
                level.textContent = esc(m.SourceLevel) + ' \u2192 ' + esc(m.DestinationLevel);
                levelsDiv.appendChild(level);
            });
            card.appendChild(levelsDiv);
        }

        // Show pending disconnects if any
        if (pendingDisconnects.length > 0) {
            var pendingDiv = document.createElement('div');
            pendingDiv.style.cssText = 'font-size:10px;color:#ff9800;padding-top:4px;border-top:1px solid var(--border);margin-top:4px;padding-top:4px;';
            pendingDiv.innerHTML = '\u23f3 Releasing: ' + pendingDisconnects[0].LevelNames.map(esc).join(', ');
            card.appendChild(pendingDiv);
        }

        // Disconnect button
        var actions = document.createElement('div');
        actions.className = 'src-conn-card-actions';
        var btn = document.createElement('button');
        btn.className = 'src-conn-disconnect-btn';
        btn.textContent = 'Disconnect';
        btn.onclick = function() { performSourceDisconnect(source.Id, dst.Id, mappings); };
        actions.appendChild(btn);
        card.appendChild(actions);
        list.appendChild(card);
    });
    document.getElementById('sourceConnectionsModal').classList.add('visible');
}

document.getElementById('btnSrcConnClose').onclick = function() {
    document.getElementById('sourceConnectionsModal').classList.remove('visible');
};

async function performSourceDisconnect(sourceId, destinationId, mappings) {
    if (mappings.length === 0) return;
    setStatus('Releasing...');

    // Optimistically update the destination's pending disconnect immediately
    var dst = allDestinations.find(function(d) { return d.Id === destinationId; });
    if (dst && currentSourceModal) {
        // Create optimistic pending disconnect entry
        if (!dst.PendingDisconnects) dst.PendingDisconnects = [];
        var existingPending = dst.PendingDisconnects.find(function(pd) { return pd.SourceVsgName === currentSourceModal.Name; });
        if (!existingPending) {
            existingPending = { SourceVsgName: currentSourceModal.Name, LevelNames: [] };
            dst.PendingDisconnects.push(existingPending);
        }
        // Add the levels being disconnected
        mappings.forEach(function(m) {
            if (existingPending.LevelNames.indexOf(m.DestinationLevel) < 0) {
                existingPending.LevelNames.push(m.DestinationLevel);
            }
        });
        // Refresh the modal display immediately
        showSourceConnectionsModal(currentSourceModal);
    }

    try {
        var levelIds = mappings.map(function(m) { return m.DestinationLevelId; });
        var body = { DestinationId: destinationId, LevelIds: levelIds };
        var res = await fetch('/api/controlsurface/disconnect', {
            method: 'POST', headers: {'Content-Type': 'application/json'},
            body: JSON.stringify(body)
        });
        if (!res.ok) {
            var errText = await res.text();
            throw new Error(errText || 'Release failed (HTTP ' + res.status + ')');
        }
        setStatus('Release initiated');
        // Modal will auto-close when SSE event updates the source data (if no more connections)
    } catch(e) { setStatus('Release failed'); showError(e.message); }
}

async function refreshVsgs(vsgIds) {
    // Refresh sources and destinations based on the affected VSG IDs from the SSE event
    try {
        var [srcRes, dstRes] = await Promise.all([
            fetch('/api/controlsurface/sources'),
            fetch('/api/controlsurface/destinations')
        ]);
        if (srcRes.ok) {
            allSources = await srcRes.json();
        }
        if (dstRes.ok) {
            allDestinations = await dstRes.json();
        }
    } catch(e) { /* skip */ }
    applyFilters();

    // If the source modal is open, check if the source still has connections
    if (currentSourceModal && document.getElementById('sourceConnectionsModal').classList.contains('visible')) {
        var updatedSource = allSources.find(function(s) { return s.Id === currentSourceModal.Id; });
        if (updatedSource && (!updatedSource.ConnectedDestinations || updatedSource.ConnectedDestinations.length === 0)) {
            // Last connection was removed, close the modal
            document.getElementById('sourceConnectionsModal').classList.remove('visible');
            currentSourceModal = null;
        } else if (updatedSource) {
            // Connections still exist, refresh the modal to show current state
            showSourceConnectionsModal(updatedSource);
        }
    }
}

function connectSse() {
    var es = new EventSource('/api/controlsurface/events');
    es.onmessage = async function(e) {
        if (e.data === 'connected') return;
        if (e.data === 'all') {
            await loadData();
        } else {
            // e.data is comma-separated VSG IDs that were affected
            var ids = e.data.split(',');
            await refreshVsgs(ids);
        }
    };
    es.onerror = function() { es.close(); setTimeout(connectSse, 5000); };
}

initFilterChips();

loadData().then(connectSse);