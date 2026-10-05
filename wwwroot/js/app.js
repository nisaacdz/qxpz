// CampFix Frontend Client - Real-Time Dashboard

let currentIssues = [];
let currentFilterStatus = '';
let currentSelectedIssueId = null;
let metadata = {
  categories: [],
  priorities: [],
  statuses: [],
  buildings: []
};

// Initialize Application on DOM Ready
document.addEventListener('DOMContentLoaded', async () => {
  await loadMetadata();
  await Promise.all([fetchIssues(), fetchStats()]);
  initSignalR();
});

// 1. Load Metadata (Categories, Buildings, etc.)
async function loadMetadata() {
  try {
    const res = await fetch('/api/meta');
    if (!res.ok) throw new Error('Failed to fetch metadata');
    metadata = await res.json();

    // Populate building filters
    const buildingFilter = document.getElementById('buildingFilter');
    const modalBuildingSelect = document.getElementById('modalBuildingSelect');
    
    metadata.buildings.forEach(b => {
      buildingFilter.innerHTML += `<option value="${b}">${b}</option>`;
      modalBuildingSelect.innerHTML += `<option value="${b}">${b}</option>`;
    });

    // Populate category filters
    const categoryFilter = document.getElementById('categoryFilter');
    const modalCategorySelect = document.getElementById('modalCategorySelect');

    metadata.categories.forEach(c => {
      const label = formatEnum(c);
      categoryFilter.innerHTML += `<option value="${c}">${label}</option>`;
      modalCategorySelect.innerHTML += `<option value="${c}">${label}</option>`;
    });
  } catch (err) {
    console.error('Error loading metadata:', err);
  }
}

// 2. Fetch Issues with Active Filters
async function fetchIssues() {
  const search = document.getElementById('searchInput').value.trim();
  const building = document.getElementById('buildingFilter').value;
  const category = document.getElementById('categoryFilter').value;

  const params = new URLSearchParams();
  if (currentFilterStatus) params.append('status', currentFilterStatus);
  if (category) params.append('category', category);
  if (building) params.append('building', building);
  if (search) params.append('search', search);

  try {
    const res = await fetch(`/api/issues?${params.toString()}`);
    if (!res.ok) throw new Error('Failed to load issues');
    currentIssues = await res.json();
    renderIssues(currentIssues);
  } catch (err) {
    console.error('Error loading issues:', err);
    showToast('Failed to load issues', 'error');
  }
}

// 3. Fetch Analytics Stats
async function fetchStats() {
  try {
    const res = await fetch('/api/issues/stats');
    if (!res.ok) return;
    const stats = await res.json();

    document.getElementById('statTotal').innerText = stats.totalIssues;
    document.getElementById('statReported').innerText = stats.reportedCount;
    document.getElementById('statInProgress').innerText = stats.inProgressCount;
    document.getElementById('statResolved').innerText = stats.resolvedCount;
    document.getElementById('statCritical').innerText = stats.criticalCount;
  } catch (err) {
    console.error('Error fetching stats:', err);
  }
}

// 4. Render Issues Cards
function renderIssues(issues) {
  const container = document.getElementById('issuesContainer');
  const emptyState = document.getElementById('emptyState');
  const countBadge = document.getElementById('issueCountBadge');

  countBadge.innerText = issues.length;

  if (issues.length === 0) {
    container.innerHTML = '';
    emptyState.classList.remove('hidden');
    return;
  }

  emptyState.classList.add('hidden');
  container.innerHTML = issues.map(issue => createIssueCardHtml(issue)).join('');
}

function createIssueCardHtml(issue) {
  const priorityInfo = getPriorityBadge(issue.priority);
  const statusInfo = getStatusBadge(issue.status);
  const categoryIcon = getCategoryIcon(issue.category);
  const timeAgo = formatTimeAgo(new Date(issue.createdAt));

  return `
    <div id="issue-card-${issue.id}" class="issue-card bg-slate-900 border border-slate-800 hover:border-slate-700 rounded-xl p-5 flex flex-col justify-between shadow-lg ${issue.priority === 'Critical' ? 'glow-critical' : ''}">
      <div>
        <!-- Badges Row -->
        <div class="flex items-center justify-between gap-2 mb-3">
          <div class="flex items-center gap-1.5 flex-wrap">
            <span class="text-[11px] px-2 py-0.5 rounded-full font-bold uppercase ${priorityInfo.classes}">
              ${issue.priority}
            </span>
            <span class="text-[11px] px-2 py-0.5 rounded-full bg-slate-800 border border-slate-700 text-slate-300 flex items-center gap-1">
              <i class="${categoryIcon} text-slate-400"></i>
              <span>${formatEnum(issue.category)}</span>
            </span>
          </div>
          <span class="text-[11px] px-2.5 py-0.5 rounded-full font-medium ${statusInfo.classes}">
            ${formatEnum(issue.status)}
          </span>
        </div>

        <!-- Title & Location -->
        <h3 class="text-sm font-bold text-white line-clamp-2 hover:text-indigo-300 transition cursor-pointer" onclick="openIssueDetail(${issue.id})">
          ${escapeHtml(issue.title)}
        </h3>

        <div class="flex items-center gap-1.5 text-xs text-indigo-400 mt-2 font-medium">
          <i class="fa-solid fa-location-dot text-[11px]"></i>
          <span>${escapeHtml(issue.building)} &bull; ${escapeHtml(issue.room)}</span>
        </div>

        <p class="text-xs text-slate-400 mt-2.5 line-clamp-2 leading-relaxed">
          ${escapeHtml(issue.description)}
        </p>
      </div>

      <!-- Card Footer -->
      <div class="pt-4 mt-4 border-t border-slate-800/80 flex items-center justify-between text-xs text-slate-400">
        <div class="flex items-center gap-1.5">
          <span class="w-5 h-5 rounded-full bg-slate-800 flex items-center justify-center text-[10px] text-slate-300 font-bold">
            ${issue.reporterName ? issue.reporterName.charAt(0).toUpperCase() : 'U'}
          </span>
          <span class="truncate max-w-[100px] text-slate-400">${escapeHtml(issue.reporterName)}</span>
          <span class="text-slate-600">&bull;</span>
          <span class="text-[11px] text-slate-500">${timeAgo}</span>
        </div>

        <button onclick="openIssueDetail(${issue.id})" class="px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium transition flex items-center gap-1">
          <span>Manage</span>
          <i class="fa-solid fa-chevron-right text-[10px] text-slate-400"></i>
        </button>
      </div>
    </div>
  `;
}

// 5. Open Issue Details & Activity Audit Modal
async function openIssueDetail(id) {
  try {
    const res = await fetch(`/api/issues/${id}`);
    if (!res.ok) throw new Error('Issue not found');
    const issue = await res.json();
    currentSelectedIssueId = id;

    // Header info
    document.getElementById('detailTitle').innerText = issue.title;
    document.getElementById('detailLocation').innerHTML = `<i class="fa-solid fa-location-dot mr-1 text-indigo-400"></i> ${issue.building} &bull; ${issue.room}`;
    document.getElementById('detailDescription').innerText = issue.description;
    document.getElementById('detailReporter').innerText = `${issue.reporterName} (${issue.reporterEmail || 'No email provided'})`;
    document.getElementById('detailCreatedAt').innerText = new Date(issue.createdAt).toLocaleString();

    // Priority badge
    const pBadge = document.getElementById('detailPriorityBadge');
    const pInfo = getPriorityBadge(issue.priority);
    pBadge.className = `text-xs px-2.5 py-0.5 rounded-full font-bold uppercase tracking-wider ${pInfo.classes}`;
    pBadge.innerText = issue.priority;

    // Category badge
    const cBadge = document.getElementById('detailCategoryBadge');
    cBadge.innerText = formatEnum(issue.category);

    // Status badge
    const sBadge = document.getElementById('detailStatusBadge');
    const sInfo = getStatusBadge(issue.status);
    sBadge.className = `text-xs px-2.5 py-0.5 rounded-full font-semibold ${sInfo.classes}`;
    sBadge.innerText = formatEnum(issue.status);

    // Assigned
    const assignedContainer = document.getElementById('detailAssignedContainer');
    if (issue.assignedTo) {
      document.getElementById('detailAssigned').innerText = issue.assignedTo;
      assignedContainer.classList.remove('hidden');
    } else {
      assignedContainer.classList.add('hidden');
    }

    // Triage form pre-fill
    document.getElementById('statusIssueId').value = issue.id;
    document.getElementById('updateStatusSelect').value = issue.status;
    document.getElementById('updateAssignedTo').value = issue.assignedTo || '';
    document.getElementById('updateStatusNotes').value = '';

    // Render activities
    renderActivities(issue.activities || []);

    document.getElementById('issueDetailModal').classList.remove('hidden');
  } catch (err) {
    console.error('Error opening detail:', err);
    showToast('Failed to load issue details', 'error');
  }
}

function renderActivities(activities) {
  const container = document.getElementById('detailTimelineContainer');
  if (activities.length === 0) {
    container.innerHTML = '<p class="text-xs text-slate-500 italic py-2">No activity recorded yet.</p>';
    return;
  }

  container.innerHTML = activities.map(act => {
    let actionIcon = 'fa-circle-info text-blue-400';
    let actionBg = 'bg-blue-500/10 border-blue-500/20';

    if (act.action === 'Reported') {
      actionIcon = 'fa-flag text-indigo-400';
      actionBg = 'bg-indigo-500/10 border-indigo-500/20';
    } else if (act.action === 'StatusChanged' || act.action === 'Resolved') {
      actionIcon = 'fa-arrows-rotate text-amber-400';
      actionBg = 'bg-amber-500/10 border-amber-500/20';
    } else if (act.action === 'CommentAdded') {
      actionIcon = 'fa-comment text-slate-400';
      actionBg = 'bg-slate-800 border-slate-700';
    }

    return `
      <div class="p-2.5 rounded-lg border ${actionBg} text-xs space-y-1">
        <div class="flex items-center justify-between text-slate-400 text-[11px]">
          <span class="font-semibold text-slate-200 flex items-center gap-1.5">
            <i class="fa-solid ${actionIcon}"></i>
            <span>${escapeHtml(act.author)}</span>
            <span class="text-slate-500 font-normal">&bull; ${escapeHtml(act.action)}</span>
          </span>
          <span>${formatTimeAgo(new Date(act.createdAt))}</span>
        </div>
        <p class="text-slate-300 pl-4 leading-normal">${escapeHtml(act.details)}</p>
      </div>
    `;
  }).join('');
}

function closeIssueDetailModal() {
  document.getElementById('issueDetailModal').classList.add('hidden');
  currentSelectedIssueId = null;
}

// 6. Create Issue Handler
async function handleCreateIssue(e) {
  e.preventDefault();
  const form = e.target;
  const submitBtn = document.getElementById('submitIssueBtn');
  submitBtn.disabled = true;
  submitBtn.innerText = 'Submitting...';

  const payload = {
    title: form.title.value.trim(),
    description: form.description.value.trim(),
    building: form.building.value,
    room: form.room.value.trim(),
    category: form.category.value,
    priority: form.priority.value,
    reporterName: form.reporterName.value.trim(),
    reporterEmail: form.reporterEmail.value.trim()
  };

  try {
    const res = await fetch('/api/issues', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) throw new Error('Submission failed');

    form.reset();
    closeNewIssueModal();
    showToast('Incident reported successfully!', 'success');
  } catch (err) {
    console.error('Error creating issue:', err);
    showToast('Failed to submit report. Please check required fields.', 'error');
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerText = 'Submit Incident Report';
  }
}

// 7. Update Status & Staff Triage Handler
async function handleStatusUpdate(e) {
  e.preventDefault();
  const id = document.getElementById('statusIssueId').value;
  const status = document.getElementById('updateStatusSelect').value;
  const assignedTo = document.getElementById('updateAssignedTo').value.trim();
  const notes = document.getElementById('updateStatusNotes').value.trim();

  const payload = {
    status: status,
    changedBy: 'Campus Facilities Desk',
    notes: notes,
    assignedTo: assignedTo
  };

  try {
    const res = await fetch(`/api/issues/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) throw new Error('Status update failed');

    showToast(`Ticket #${id} status updated to ${formatEnum(status)}`, 'success');
    await openIssueDetail(id); // Reload modal details
  } catch (err) {
    console.error('Error updating status:', err);
    showToast('Failed to update ticket status', 'error');
  }
}

// 8. Add Comment Handler
async function handleAddComment(e) {
  e.preventDefault();
  if (!currentSelectedIssueId) return;

  const input = document.getElementById('commentTextInput');
  const comment = input.value.trim();
  if (!comment) return;

  try {
    const res = await fetch(`/api/issues/${currentSelectedIssueId}/comments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        author: 'Campus Technician',
        comment: comment
      })
    });

    if (!res.ok) throw new Error('Comment failed');

    input.value = '';
    await openIssueDetail(currentSelectedIssueId);
  } catch (err) {
    console.error('Error adding comment:', err);
    showToast('Failed to post comment', 'error');
  }
}

// 9. Delete Issue Handler
async function handleDeleteCurrentIssue() {
  if (!currentSelectedIssueId) return;
  if (!confirm(`Are you sure you want to delete Ticket #${currentSelectedIssueId}?`)) return;

  try {
    const res = await fetch(`/api/issues/${currentSelectedIssueId}`, {
      method: 'DELETE'
    });

    if (!res.ok) throw new Error('Deletion failed');

    closeIssueDetailModal();
    showToast('Ticket deleted successfully', 'info');
  } catch (err) {
    console.error('Error deleting issue:', err);
    showToast('Failed to delete ticket', 'error');
  }
}

// 10. Filter Tabs Handler
function setStatusFilter(status) {
  currentFilterStatus = status;

  document.querySelectorAll('.status-tab').forEach(tab => {
    tab.classList.remove('active-tab', 'bg-indigo-600/20', 'text-indigo-400', 'border', 'border-indigo-500/30');
    tab.classList.add('text-slate-400');
  });

  const activeBtn = event.target;
  activeBtn.classList.add('active-tab', 'bg-indigo-600/20', 'text-indigo-400', 'border', 'border-indigo-500/30');
  activeBtn.classList.remove('text-slate-400');

  fetchIssues();
}

let searchTimeout;
function handleSearch() {
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(() => {
    fetchIssues();
  }, 250);
}

function resetFilters() {
  document.getElementById('searchInput').value = '';
  document.getElementById('buildingFilter').value = '';
  document.getElementById('categoryFilter').value = '';
  setStatusFilter('');
}

function openNewIssueModal() {
  document.getElementById('newIssueModal').classList.remove('hidden');
}

function closeNewIssueModal() {
  document.getElementById('newIssueModal').classList.add('hidden');
}

// 11. SignalR Real-Time WebSocket Connection
function initSignalR() {
  const statusPill = document.getElementById('connectionStatus');

  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/issues')
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .build();

  // On receiving a new issue report
  connection.on('IssueCreated', (issue) => {
    showToast(`🔔 New Issue: ${issue.title} (${issue.building})`, 'info');
    fetchStats();
    fetchIssues();
  });

  // On receiving a status change update
  connection.on('IssueStatusUpdated', (issue) => {
    showToast(`🔄 Issue #${issue.id} transitioned to ${formatEnum(issue.status)}`, 'info');
    fetchStats();
    fetchIssues();

    if (currentSelectedIssueId === issue.id) {
      openIssueDetail(issue.id);
    }
  });

  // On receiving a comment
  connection.on('CommentAdded', (issueId, activity) => {
    if (currentSelectedIssueId === issueId) {
      openIssueDetail(issueId);
    }
  });

  // On issue deletion
  connection.on('IssueDeleted', (issueId) => {
    fetchStats();
    fetchIssues();
    if (currentSelectedIssueId === issueId) {
      closeIssueDetailModal();
    }
  });

  // Lifecycle events
  connection.onreconnecting(() => {
    statusPill.innerHTML = `
      <span class="w-2 h-2 rounded-full bg-amber-400 animate-ping"></span>
      <span>Reconnecting...</span>
    `;
    statusPill.className = 'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-amber-950/40 border border-amber-500/30 text-amber-400';
  });

  connection.onreconnected(() => {
    statusPill.innerHTML = `
      <span class="w-2 h-2 rounded-full bg-emerald-400"></span>
      <span>Live Sync</span>
    `;
    statusPill.className = 'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-950/40 border border-emerald-500/30 text-emerald-400';
  });

  connection.onclose(() => {
    statusPill.innerHTML = `
      <span class="w-2 h-2 rounded-full bg-rose-400"></span>
      <span>Offline</span>
    `;
    statusPill.className = 'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-rose-950/40 border border-rose-500/30 text-rose-400';
  });

  // Start Connection
  connection.start()
    .then(() => {
      statusPill.innerHTML = `
        <span class="w-2 h-2 rounded-full bg-emerald-400"></span>
        <span>Live Sync</span>
      `;
      statusPill.className = 'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-950/40 border border-emerald-500/30 text-emerald-400';
    })
    .catch(err => {
      console.error('SignalR Connection Error: ', err);
      statusPill.innerHTML = `
        <span class="w-2 h-2 rounded-full bg-slate-400"></span>
        <span>Polling Mode</span>
      `;
    });
}

// 12. Helper Utilities
function getPriorityBadge(priority) {
  switch (priority) {
    case 'Critical':
      return { classes: 'bg-rose-500/20 text-rose-400 border border-rose-500/30 animate-pulse-slow' };
    case 'High':
      return { classes: 'bg-orange-500/20 text-orange-400 border border-orange-500/30' };
    case 'Medium':
      return { classes: 'bg-amber-500/20 text-amber-400 border border-amber-500/30' };
    case 'Low':
    default:
      return { classes: 'bg-slate-700/40 text-slate-300 border border-slate-600/40' };
  }
}

function getStatusBadge(status) {
  switch (status) {
    case 'Reported':
      return { classes: 'bg-blue-500/20 text-blue-400 border border-blue-500/30' };
    case 'Investigating':
      return { classes: 'bg-purple-500/20 text-purple-400 border border-purple-500/30' };
    case 'InProgress':
      return { classes: 'bg-amber-500/20 text-amber-400 border border-amber-500/30' };
    case 'Resolved':
      return { classes: 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30' };
    case 'Closed':
      return { classes: 'bg-slate-700/40 text-slate-400 border border-slate-600/40' };
    default:
      return { classes: 'bg-slate-800 text-slate-400' };
  }
}

function getCategoryIcon(cat) {
  switch (cat) {
    case 'Electrical': return 'fa-solid fa-bolt';
    case 'HVAC': return 'fa-solid fa-snowflake';
    case 'IT_AV': return 'fa-solid fa-tv';
    case 'Plumbing': return 'fa-solid fa-faucet-drip';
    case 'Structural': return 'fa-solid fa-door-open';
    case 'Sanitation': return 'fa-solid fa-trash-can';
    default: return 'fa-solid fa-wrench';
  }
}

function formatEnum(val) {
  if (!val) return '';
  if (val === 'IT_AV') return 'IT / AV';
  if (val === 'InProgress') return 'In Progress';
  return val.replace(/([A-Z])/g, ' $1').trim();
}

function formatTimeAgo(date) {
  const seconds = Math.floor((new Date() - date) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

function escapeHtml(text) {
  if (!text) return '';
  const div = document.createElement('div');
  div.innerText = text;
  return div.innerHTML;
}

function showToast(message, type = 'info') {
  const container = document.getElementById('toastContainer');
  const toast = document.createElement('div');
  
  let bgClasses = 'bg-slate-900 border-slate-800 text-slate-200';
  if (type === 'success') bgClasses = 'bg-emerald-950/90 border-emerald-500/40 text-emerald-200';
  if (type === 'error') bgClasses = 'bg-rose-950/90 border-rose-500/40 text-rose-200';
  if (type === 'info') bgClasses = 'bg-indigo-950/90 border-indigo-500/40 text-indigo-200';

  toast.className = `pointer-events-auto px-4 py-2.5 rounded-xl border shadow-xl text-xs font-medium flex items-center gap-2 transform transition-all duration-300 translate-y-2 opacity-0 ${bgClasses}`;
  toast.innerHTML = `<span>${escapeHtml(message)}</span>`;

  container.appendChild(toast);
  setTimeout(() => {
    toast.classList.remove('translate-y-2', 'opacity-0');
  }, 10);

  setTimeout(() => {
    toast.classList.add('translate-y-2', 'opacity-0');
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}
