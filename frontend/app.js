const ACCOUNT_API = 'http://localhost:5002/api/account';
const ANNOUNCEMENT_API = 'http://localhost:5003/api/announcements';
const LOST_FOUND_API = 'http://localhost:5004/api/items';
const ADMIN_KEY = 'Dragonborn:Fusrodah';

const role = sessionStorage.getItem('role') || '';
const student = JSON.parse(sessionStorage.getItem('student') || 'null');
const admin = JSON.parse(sessionStorage.getItem('admin') || 'null');
if (role !== 'Admin' && !student) location.href = 'Login/index.html';

const PROGRAMS = [
    'Bachelor of Science in Information Technology',
    'Bachelor of Science in Computer Science',
    'Bachelor of Science in Information Systems',
    'Bachelor of Science in Entertainment and Multimedia Computing',
    'Bachelor of Science in Computer Engineering',
    'Bachelor of Science in Data Science',
    'Bachelor of Science in Cybersecurity',
    'Bachelor of Science in Computer Technology',
    'Bachelor of Science in Information Management',
    'Bachelor of Science in Information Technology Management'
];

const studentId = student?.studentId || '';
const announceReadKey = `studentPortalAnnouncementsRead:${studentId || 'admin'}`;
let announcements = [];
let announceFilter = 'All';
let announcementMode = 'all';
let students = [];

function el(id) { return document.getElementById(id); }
function escapeHtml(value) { return String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
function adminHeaders() { return { 'Content-Type': 'application/json', 'X-Admin-Key': ADMIN_KEY }; }
function showStatus(id, text, error = false) { const box = el(id); if (!box) return; box.textContent = text; box.classList.toggle('error', error); }

function setupIdentity() {
    const displayName = role === 'Admin' ? (admin?.username || 'ADMINISTRATOR') : (student?.name || 'STUDENT');
    el('topName').textContent = displayName.toUpperCase();
    el('dashboardDate').textContent = new Date().toLocaleString('en-US', { weekday:'long', year:'numeric', month:'long', day:'numeric', hour:'numeric', minute:'2-digit' });

    if (role === 'Admin') {
        el('adminNav').classList.remove('hidden');
        el('student-portal').innerHTML = `<div class="panel placeholder"><h1>Administrator Mode</h1><p>Use the Admin Dashboard to manage students, classes, transactions, announcements, and Lost & Found.</p></div>`;
        el('pageTitle').textContent = 'Admin Dashboard';
        location.hash = '#admin-dashboard';
    } else {
        el('studentName').textContent = (student.name || 'Student').toUpperCase();
        el('studentProgram').textContent = (student.program || 'Program').toUpperCase();
        el('studentNumber').textContent = student.studentId || '—';
        el('studentEmail').textContent = student.email || '—';
        el('studentYear').textContent = student.yearLevel ? `Year Level: ${student.yearLevel}` : 'Year Level: —';
        el('avatar').textContent = (student.name || 'S').charAt(0).toUpperCase();
    }
}

el('collapseBtn').onclick = () => document.body.classList.toggle('collapsed');
el('logoutBtn').onclick = () => { sessionStorage.clear(); location.href = 'Login/index.html'; };

// ---------------- FEATURE 01: STUDENT ----------------
async function loadStudentRecords() {
    if (role !== 'Student') return;
    try {
        const [txResponse, gradeResponse] = await Promise.all([
            fetch(`${ACCOUNT_API}/transactions/student/${encodeURIComponent(studentId)}`),
            fetch(`${ACCOUNT_API}/grades/student/${encodeURIComponent(studentId)}`)
        ]);
        const tx = txResponse.ok ? await txResponse.json() : [];
        const grades = gradeResponse.ok ? await gradeResponse.json() : [];
        renderStudentTransactions(tx);
        renderStudentGrades(grades);
    } catch {
        renderStudentTransactions([]);
        renderStudentGrades([]);
    }
}

function renderStudentTransactions(records) {
    el('transactionRows').innerHTML = records.length ? records.map(t => `<tr><td>${escapeHtml(t.date)}</td><td>${escapeHtml(t.description)}</td><td>${escapeHtml(t.type)}</td><td>₱${Number(t.amount).toFixed(2)}</td><td>${escapeHtml(t.notes)}</td></tr>`).join('') : '<tr><td colspan="5" class="empty-cell">No transactions recorded.</td></tr>';
    const total = records.reduce((sum, t) => sum + Number(t.amount || 0), 0);
    el('transactionTotal').textContent = `₱${total.toFixed(2)}`;
}

function renderStudentGrades(records) {
    el('gradeRows').innerHTML = records.length ? records.map((g, i) => `<tr><td>${i+1}</td><td>${escapeHtml(g.courseCode)}</td><td>${escapeHtml(g.courseName)}</td><td>—</td><td>—</td><td>—</td><td>—</td><td>—</td><td>${Number(g.grade).toFixed(2)}</td></tr>`).join('') : '<tr><td colspan="9" class="empty-cell">No grades recorded.</td></tr>';
    el('gradeSummary').innerHTML = records.length ? `<span>Recorded Courses:</span><strong>${records.length}</strong>` : '';
}

// ---------------- FEATURE 02: ANNOUNCEMENTS ----------------
function loadReadAnnouncements() { try { return JSON.parse(localStorage.getItem(announceReadKey) || '[]'); } catch { return []; } }
function saveReadAnnouncements(items) { localStorage.setItem(announceReadKey, JSON.stringify(items)); }
function formatDate(date) { return new Date(`${date}T00:00:00`).toLocaleDateString('en-US', { month:'long', day:'numeric', year:'numeric' }); }

async function loadAnnouncements() {
    try {
        const params = role === 'Student' ? `?studentId=${encodeURIComponent(studentId)}&program=${encodeURIComponent(student.program || '')}` : '';
        const response = await fetch(ANNOUNCEMENT_API + params);
        const data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Failed');
        announcements = data.announcements || [];
        showStatus('announceStatus', '', false);
    } catch {
        announcements = [];
        showStatus('announceStatus', 'Could not load announcements. Start api/announcementapi with dotnet run.', true);
    }
    renderAnnouncements();
}

function renderAnnouncements() {
    const read = loadReadAnnouncements();
    const categories = ['All', 'Academic', 'Finance', 'Events', 'Reminders', 'Lost & Found', 'General'];
    el('announceFilters').innerHTML = categories.map(c => `<button type="button" class="chip${c === announceFilter ? ' active' : ''}" data-filter="${escapeHtml(c)}">${escapeHtml(c)}</button>`).join('');
    let visible = announcements.filter(a => announceFilter === 'All' || a.category === announceFilter);
    if (announcementMode === 'personal' && role === 'Student') visible = visible.filter(a => a.audienceType === 'Student' && a.audienceValue === studentId);
    const unread = announcements.filter(a => !read.includes(a.id)).length;
    el('announceCount').textContent = announcements.length ? `${unread} unread` : 'No announcements to show';
    el('announceList').innerHTML = visible.length ? visible.map(a => {
        const isRead = read.includes(a.id);
        const audience = a.audienceType === 'Everyone' ? 'Everyone' : `${a.audienceType}: ${a.audienceValue}`;
        return `<article class="announce-card${isRead ? '' : ' unread'}"><div class="announce-meta"><span class="type-badge">${escapeHtml(a.category)}</span><span>${formatDate(a.date)}</span><span>${escapeHtml(audience)}</span>${a.pinned ? '<span class="pin-tag">Pinned</span>' : ''}</div><h2>${escapeHtml(a.title)}</h2><p>${escapeHtml(a.body)}</p>${role === 'Student' ? `<button type="button" class="action-btn" data-toggle-read="${a.id}">${isRead ? 'Mark as unread' : 'Mark as read'}</button>` : ''}</article>`;
    }).join('') : '<p class="empty-cell">No announcements in this view.</p>';
}

el('announceFilters').onclick = e => { const b = e.target.closest('[data-filter]'); if (b) { announceFilter = b.dataset.filter; renderAnnouncements(); } };
el('announceList').onclick = e => { const b = e.target.closest('[data-toggle-read]'); if (!b) return; const id = Number(b.dataset.toggleRead); let read = loadReadAnnouncements(); read = read.includes(id) ? read.filter(x => x !== id) : [...read, id]; saveReadAnnouncements(read); renderAnnouncements(); };
el('markAllRead').onclick = () => { saveReadAnnouncements(announcements.map(a => a.id)); renderAnnouncements(); };
el('allAnnouncementsTab').onclick = () => { announcementMode = 'all'; el('allAnnouncementsTab').classList.add('active'); el('personalAnnouncementsTab').classList.remove('active'); renderAnnouncements(); };
el('personalAnnouncementsTab').onclick = () => { announcementMode = 'personal'; el('personalAnnouncementsTab').classList.add('active'); el('allAnnouncementsTab').classList.remove('active'); renderAnnouncements(); };

// ---------------- FEATURE 03: LOST & FOUND ----------------
async function loadLostFound() {
    try {
        const query = role === 'Student' ? `?studentId=${encodeURIComponent(studentId)}` : '';
        const response = await fetch(LOST_FOUND_API + query);
        const items = await response.json();
        if (!response.ok) throw new Error();
        if (role === 'Student') {
            el('lostFoundRows').innerHTML = items.length ? items.map(i => `<tr><td>${escapeHtml(i.itemName)}</td><td>${escapeHtml(i.type)}</td><td>${escapeHtml(i.category)}</td><td>${escapeHtml(i.location)}</td><td>${escapeHtml(i.status)}</td><td>${escapeHtml(i.dateReported)}</td></tr>`).join('') : '<tr><td colspan="6" class="empty-cell">No reports yet.</td></tr>';
        } else {
            renderAdminLostFound(items);
        }
    } catch {
        if (role === 'Student') showStatus('lfStatus', 'Could not load Lost & Found API. Start api/Feature03API with dotnet run.', true);
    }
}

el('lostFoundForm').onsubmit = async e => {
    e.preventDefault();
    const item = { itemName: el('lfItemName').value.trim(), description: el('lfDescription').value.trim(), category: el('lfCategory').value.trim(), type: el('lfType').value, location: el('lfLocation').value.trim(), status: 'Unclaimed', photoUrl: '', reportedByStudentId: studentId, program: student.program || '' };
    try {
        const response = await fetch(LOST_FOUND_API, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(item) });
        if (!response.ok) throw new Error();
        e.target.reset(); showStatus('lfStatus', 'Report submitted successfully.'); await loadLostFound();
    } catch { showStatus('lfStatus', 'Could not submit the report.', true); }
};

// ---------------- ADMIN ----------------
function populateProgramSelect(id) { el(id).innerHTML = PROGRAMS.map(p => `<option>${escapeHtml(p)}</option>`).join(''); }
function setupAdmin() {
    if (role !== 'Admin') return;
    populateProgramSelect('adminProgram'); populateProgramSelect('adminTxProgram');
    document.querySelectorAll('[data-admin-tab]').forEach(btn => btn.onclick = () => {
        document.querySelectorAll('[data-admin-tab]').forEach(x => x.classList.remove('active'));
        document.querySelectorAll('.admin-view').forEach(x => x.classList.remove('active-admin-view'));
        btn.classList.add('active'); el(`admin-${btn.dataset.adminTab}`).classList.add('active-admin-view');
        if (btn.dataset.adminTab === 'students') loadStudents();
        if (btn.dataset.adminTab === 'transactions') loadAdminTransactions();
        if (btn.dataset.adminTab === 'announcements') loadAdminAnnouncements();
        if (btn.dataset.adminTab === 'lostfound') loadLostFound();
    });
    el('adminTxDate').value = new Date().toISOString().slice(0,10);
    el('adminAnnAudience').onchange = () => { el('adminAnnAudienceValueWrap').classList.toggle('hidden', el('adminAnnAudience').value === 'Everyone'); };
    el('studentEditForm').onsubmit = updateStudent;
    el('adminTransactionForm').onsubmit = addAdminTransaction;
    el('adminAnnouncementForm').onsubmit = addAdminAnnouncement;
    loadStudents(); loadAdminTransactions(); loadAdminAnnouncements(); loadLostFound();
}

async function loadStudents() {
    try { const r = await fetch(`${ACCOUNT_API}/students`); students = await r.json(); renderStudents(); } catch { showStatus('adminStudentStatus', 'Could not load students.', true); }
}
function renderStudents() {
    el('adminStudentRows').innerHTML = students.length ? students.map(s => `<tr><td>${escapeHtml(s.studentId)}</td><td>${escapeHtml(s.name)}</td><td>${escapeHtml(s.program)}</td><td>${escapeHtml(s.yearLevel)}</td><td>${escapeHtml(s.classSection || '—')}</td><td><button class="action-btn" data-edit-student="${escapeHtml(s.studentId)}">Edit</button></td></tr>`).join('') : '<tr><td colspan="6" class="empty-cell">No students registered.</td></tr>';
    el('adminStudentRows').querySelectorAll('[data-edit-student]').forEach(b => b.onclick = () => {
        const s = students.find(x => x.studentId === b.dataset.editStudent); if (!s) return;
        el('adminStudentId').value = s.studentId; el('adminFirstName').value = s.name.split(',')[1]?.trim().split(' ')[0] || ''; el('adminLastName').value = s.name.split(',')[0] || ''; el('adminEmail').value = s.email; el('adminProgram').value = s.program; el('adminYear').value = s.yearLevel; el('adminClass').value = s.classSection || '';
    });
}
async function updateStudent(e) {
    e.preventDefault();
    const id = el('adminStudentId').value.trim();
    const payload = { firstName:el('adminFirstName').value.trim(), middleName:el('adminMiddleName').value.trim(), lastName:el('adminLastName').value.trim(), email:el('adminEmail').value.trim(), program:el('adminProgram').value, yearLevel:el('adminYear').value, classSection:el('adminClass').value.trim() };
    try { const r = await fetch(`${ACCOUNT_API}/students/${encodeURIComponent(id)}`, { method:'PUT', headers:adminHeaders(), body:JSON.stringify(payload) }); const d = await r.json(); if (!r.ok) throw new Error(d.message || 'Update failed'); showStatus('adminStudentStatus', d.message); await loadStudents(); } catch (err) { showStatus('adminStudentStatus', err.message, true); }
}
async function addAdminTransaction(e) {
    e.preventDefault();
    const payload = { studentId:el('adminTxStudentId').value.trim(), program:el('adminTxProgram').value, classSection:el('adminTxClass').value.trim(), description:el('adminTxDescription').value.trim(), amount:Number(el('adminTxAmount').value), type:el('adminTxType').value, date:el('adminTxDate').value, notes:el('adminTxNotes').value.trim() };
    try { const r = await fetch(`${ACCOUNT_API}/transactions`, { method:'POST', headers:adminHeaders(), body:JSON.stringify(payload) }); if (!r.ok) throw new Error('Could not add transaction.'); showStatus('adminTxStatus','Transaction added successfully.'); e.target.reset(); el('adminTxDate').value = new Date().toISOString().slice(0,10); await loadAdminTransactions(); } catch (err) { showStatus('adminTxStatus', err.message, true); }
}
async function loadAdminTransactions() {
    try { const r = await fetch(`${ACCOUNT_API}/transactions`); const data = await r.json(); el('adminTransactionRows').innerHTML = data.length ? data.map(t => `<tr><td>${escapeHtml(t.studentId)}</td><td>${escapeHtml(t.program)}</td><td>${escapeHtml(t.classSection)}</td><td>${escapeHtml(t.description)}</td><td>₱${Number(t.amount).toFixed(2)}</td><td>${escapeHtml(t.date)}</td></tr>`).join('') : '<tr><td colspan="6" class="empty-cell">No transactions recorded.</td></tr>'; } catch { el('adminTransactionRows').innerHTML = '<tr><td colspan="6" class="empty-cell">Transaction API unavailable.</td></tr>'; }
}
async function addAdminAnnouncement(e) {
    e.preventDefault();
    const audienceType = el('adminAnnAudience').value;
    const payload = { title:el('adminAnnTitle').value.trim(), body:el('adminAnnBody').value.trim(), category:el('adminAnnCategory').value, pinned:el('adminAnnPinned').checked, audienceType, audienceValue:audienceType === 'Everyone' ? '' : el('adminAnnAudienceValue').value.trim(), relatedLostFoundItemId:null };
    try { const r = await fetch(ANNOUNCEMENT_API, { method:'POST', headers:adminHeaders(), body:JSON.stringify(payload) }); if (!r.ok) throw new Error('Could not publish announcement.'); showStatus('adminAnnStatus','Announcement published.'); e.target.reset(); el('adminAnnAudienceValueWrap').classList.add('hidden'); await loadAdminAnnouncements(); } catch (err) { showStatus('adminAnnStatus', err.message, true); }
}
async function loadAdminAnnouncements() {
    try { const r = await fetch(ANNOUNCEMENT_API); const d = await r.json(); el('adminAnnouncementRows').innerHTML = d.announcements?.length ? d.announcements.map(a => `<tr><td>${escapeHtml(a.title)}</td><td>${escapeHtml(a.audienceType)}${a.audienceValue ? ': ' + escapeHtml(a.audienceValue) : ''}</td><td>${escapeHtml(a.category)}</td><td>${escapeHtml(a.date)}</td><td><button class="delete-btn" data-delete-ann="${a.id}">Delete</button></td></tr>`).join('') : '<tr><td colspan="5" class="empty-cell">No announcements.</td></tr>'; document.querySelectorAll('[data-delete-ann]').forEach(b => b.onclick = () => deleteAnnouncement(b.dataset.deleteAnn)); } catch { showStatus('adminAnnStatus','Announcement API unavailable.',true); }
}
async function deleteAnnouncement(id) { try { const r = await fetch(`${ANNOUNCEMENT_API}/${id}`, { method:'DELETE', headers:adminHeaders() }); if (!r.ok) throw new Error(); await loadAdminAnnouncements(); } catch { showStatus('adminAnnStatus','Could not delete announcement.',true); } }
function renderAdminLostFound(items) {
    el('adminLostFoundRows').innerHTML = items.length ? items.map(i => `<tr><td>${escapeHtml(i.itemName)}</td><td>${escapeHtml(i.reportedByStudentId || '—')}</td><td>${escapeHtml(i.program || '—')}</td><td>${escapeHtml(i.type)}</td><td>${escapeHtml(i.status)}</td><td><button class="action-btn" data-notify-item="${i.id}">Notify Student</button></td></tr>`).join('') : '<tr><td colspan="6" class="empty-cell">No Lost & Found reports.</td></tr>';
    document.querySelectorAll('[data-notify-item]').forEach(b => b.onclick = () => notifyMissingItem(Number(b.dataset.notifyItem)));
}
async function notifyMissingItem(id) {
    try {
        const r = await fetch(`${LOST_FOUND_API}/${id}/notify`, { method:'POST', headers:adminHeaders() });
        const item = await r.json(); if (!r.ok) throw new Error(item.message || 'Unable to notify.');
        const payload = { title:`Lost & Found: ${item.itemName || 'Missing item'}`, body:`A Lost & Found report associated with student ${item.studentId} has been updated. Please check the Lost & Found office or contact administration for details.`, category:'Lost & Found', pinned:true, audienceType:'Student', audienceValue:item.studentId, relatedLostFoundItemId:id };
        const announcement = await fetch(ANNOUNCEMENT_API, { method:'POST', headers:adminHeaders(), body:JSON.stringify(payload) });
        if (!announcement.ok) throw new Error('Lost & Found API responded, but the announcement could not be created.');
        alert('Student notification created in Personal Announcements.');
        await loadAdminAnnouncements();
    } catch (err) { alert(err.message); }
}

// ---------------- ROUTING ----------------
const titles = { '#student-portal':'Student Portal', '#announcements':'Announcements', '#feature-03':'Lost & Found', '#feature-04':'Library Access', '#admin-dashboard':'Admin Dashboard' };
function route() {
    let hash = titles[location.hash] ? location.hash : (role === 'Admin' ? '#admin-dashboard' : '#student-portal');
    document.querySelectorAll('.feature-section').forEach(s => s.classList.toggle('active-section', `#${s.id}` === hash));
    document.querySelectorAll('.sidebar nav a').forEach(a => a.classList.toggle('active', a.getAttribute('href') === hash));
    el('pageTitle').textContent = titles[hash];
    if (hash === '#announcements') loadAnnouncements();
    if (hash === '#feature-03') loadLostFound();
}
window.addEventListener('hashchange', route);

setupIdentity();
loadStudentRecords();
loadAnnouncements();
loadLostFound();
setupAdmin();
route();
