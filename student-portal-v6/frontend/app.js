// ==================== SHARED PORTAL / SESSION CODE START ====================
const student = JSON.parse(sessionStorage.getItem('student') || 'null');
if (!student) location.href = 'Login/index.html';

const name = student?.name || 'Student';
const program = student?.program || 'Bachelor of Science in Information Technology';
const studentId = student?.studentId || '—';
const initial = name.charAt(0).toUpperCase();

['topName'].forEach(id => document.getElementById(id).textContent = name.toUpperCase());
document.getElementById('studentName').textContent = name.toUpperCase();
document.getElementById('studentProgram').textContent = program.toUpperCase();
document.getElementById('studentNumber').textContent = studentId;
document.getElementById('studentEmail').textContent = student?.email || '—';
document.getElementById('studentYear').textContent = student?.yearLevel ? `Year Level: ${student.yearLevel}` : 'Year Level: —';
document.getElementById('avatar').textContent = initial;
document.getElementById('dashboardDate').textContent = new Date().toLocaleString('en-US', {weekday:'long', year:'numeric', month:'long', day:'numeric', hour:'numeric', minute:'2-digit'});

document.getElementById('collapseBtn').onclick = () => document.body.classList.toggle('collapsed');
document.getElementById('logoutBtn').onclick = () => { sessionStorage.clear(); location.href = 'Login/index.html'; };

// Shared helpers used by more than one feature.
const PORTAL_API = 'http://localhost:5002/api'; // api/accountapi (login, transactions, grades)
function escapeHtml(value){
    return String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
}
function formatDate(isoDate){
    return new Date(isoDate + 'T00:00:00').toLocaleDateString('en-US', {month:'short', day:'numeric', year:'numeric'});
}
// Creates a small message line (same look as the announcements status) right before `target`.
function addStatusLine(id, target){
    const p = document.createElement('p');
    p.id = id; p.className = 'form-message'; p.setAttribute('role', 'status');
    target.before(p);
    return (text, isError) => { p.textContent = text; p.classList.toggle('error', Boolean(isError)); };
}
// ==================== SHARED PORTAL / SESSION CODE END ======================

// ==================== STUDENT PORTAL: TRANSACTIONS JS START =================
// Saved in the Transactions table through api/accountapi (port 5002).
const TX_API = `${PORTAL_API}/transactions`;
let transactions = [];
const txForm = document.getElementById('transactionForm');
const showTxStatus = addStatusLine('txStatus', txForm);

const peso = amount => '₱' + Number(amount).toLocaleString('en-PH', {minimumFractionDigits:2, maximumFractionDigits:2});
const todayIso = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; };
document.getElementById('txDate').value = todayIso();

async function loadTransactions(){
    try {
        const response = await fetch(`${TX_API}?studentId=${encodeURIComponent(studentId)}`);
        const data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Request failed.');
        transactions = data.transactions;
        showTxStatus('', false);
    } catch (error) {
        console.error(error);
        transactions = [];
        showTxStatus('Could not load transactions. Make sure api/accountapi is running (dotnet run).', true);
    }
    renderTransactions();
}

function renderTransactions(){
    document.getElementById('transactionRows').innerHTML = transactions.length
        ? transactions.map(t => `<tr><td>${formatDate(t.date)}</td><td>${escapeHtml(t.description)}</td><td>${escapeHtml(t.type)}</td><td>${peso(t.amount)}</td><td>${t.notes ? escapeHtml(t.notes) : '—'}</td><td><button type="button" class="action-btn" data-delete-tx="${t.id}">Delete</button></td></tr>`).join('')
        : '<tr><td colspan="6" class="empty-cell">No transactions yet.</td></tr>';
    document.getElementById('transactionTotal').textContent = peso(transactions.reduce((sum, t) => sum + Number(t.amount), 0));
}

txForm.onsubmit = async event => {
    event.preventDefault();
    const button = txForm.querySelector('button[type="submit"]');
    const body = {
        studentId,
        description: document.getElementById('txDescription').value.trim(),
        amount: Number(document.getElementById('txAmount').value),
        type: document.getElementById('txType').value,
        date: document.getElementById('txDate').value,
        notes: document.getElementById('txNotes').value.trim() || null
    };
    button.disabled = true;
    try {
        const response = await fetch(TX_API, {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body)});
        const data = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(data.message || 'Could not save the transaction.');
        txForm.reset();
        document.getElementById('txDate').value = todayIso();
        await loadTransactions();
        showTxStatus('Transaction added.', false);
    } catch (error) {
        console.error(error);
        showTxStatus(error.message === 'Failed to fetch'
            ? 'Could not reach the server. Make sure api/accountapi is running (dotnet run).'
            : error.message, true);
    } finally {
        button.disabled = false;
    }
};

document.getElementById('transactionRows').onclick = async event => {
    const button = event.target.closest('[data-delete-tx]');
    if (!button || !confirm('Delete this transaction?')) return;
    try {
        const response = await fetch(`${TX_API}/${button.dataset.deleteTx}?studentId=${encodeURIComponent(studentId)}`, {method:'DELETE'});
        const data = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(data.message || 'Could not delete the transaction.');
        await loadTransactions();
        showTxStatus('Transaction deleted.', false);
    } catch (error) {
        console.error(error);
        showTxStatus(error.message, true);
    }
};

loadTransactions();
// ==================== STUDENT PORTAL: TRANSACTIONS JS END ===================

// ==================== FEATURE 02: ANNOUNCEMENTS JS START ====================
// Talks to api/announcementapi. Start it with: dotnet run (port 5003).
const ANNOUNCEMENT_API = `${PORTAL_API}/announcements`;

// Which announcements this student has read stays in the browser, per student.
const announceReadKey = `studentPortalAnnouncementsRead:${studentId}`;
const announceCategories = ['All', 'Academic', 'Finance', 'Events', 'Reminders'];
let announcements = [];
let announceFilter = 'All';
let readAnnouncements = loadReadAnnouncements();

function loadReadAnnouncements(){
    try {
        const stored = JSON.parse(localStorage.getItem(announceReadKey) || '[]');
        return Array.isArray(stored) ? stored : [];
    } catch { return []; }
}
function saveReadAnnouncements(){ localStorage.setItem(announceReadKey, JSON.stringify(readAnnouncements)); }
function formatAnnouncementDate(isoDate){
    return new Date(isoDate + 'T00:00:00').toLocaleDateString('en-US', {month:'long', day:'numeric', year:'numeric'});
}
function showAnnounceStatus(text, isError){
    const box = document.getElementById('announceStatus');
    box.textContent = text;
    box.classList.toggle('error', Boolean(isError));
}

async function loadAnnouncements(){
    showAnnounceStatus('Loading announcements…', false);
    try {
        const response = await fetch(ANNOUNCEMENT_API);
        const data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Request failed.');
        announcements = data.announcements;
        showAnnounceStatus('', false);
    } catch (error) {
        console.error(error);
        announcements = [];
        showAnnounceStatus('Could not load announcements. Make sure api/accountapi is running (dotnet run).', true);
    }
    renderAnnouncements();
}

function renderAnnouncements(){
    document.getElementById('announceFilters').innerHTML = announceCategories.map(c =>
        `<button type="button" class="chip${c === announceFilter ? ' active' : ''}" data-filter="${c}" aria-pressed="${c === announceFilter}">${c}</button>`).join('');

    const unread = announcements.filter(a => !readAnnouncements.includes(a.id)).length;
    document.getElementById('announceCount').textContent = announcements.length === 0
        ? 'No announcements to show'
        : (unread === 0 ? 'You are all caught up' : `${unread} unread`);

    const visible = announcements
        .filter(a => announceFilter === 'All' || a.category === announceFilter)
        .sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.date.localeCompare(a.date));

    document.getElementById('announceList').innerHTML = visible.length
        ? visible.map(a => {
            const isRead = readAnnouncements.includes(a.id);
            return `<article class="announce-card${isRead ? '' : ' unread'}"><div class="announce-meta"><span class="type-badge">${escapeHtml(a.category)}</span><span>${formatAnnouncementDate(a.date)}</span>${a.pinned ? '<span class="pin-tag">Pinned</span>' : ''}</div><h2>${escapeHtml(a.title)}</h2><p>${escapeHtml(a.body)}</p><button type="button" class="action-btn" data-toggle-read="${a.id}">${isRead ? 'Mark as unread' : 'Mark as read'}</button></article>`;
        }).join('')
        : '<p class="empty-cell">No announcements in this category.</p>';
}

document.getElementById('announceFilters').onclick = event => {
    const chip = event.target.closest('[data-filter]');
    if (chip){ announceFilter = chip.dataset.filter; renderAnnouncements(); }
};
document.getElementById('announceList').onclick = event => {
    const button = event.target.closest('[data-toggle-read]');
    if (!button) return;
    const id = Number(button.dataset.toggleRead);
    readAnnouncements = readAnnouncements.includes(id) ? readAnnouncements.filter(x => x !== id) : [...readAnnouncements, id];
    saveReadAnnouncements();
    renderAnnouncements();
};
document.getElementById('markAllRead').onclick = () => {
    readAnnouncements = announcements.map(a => a.id);
    saveReadAnnouncements();
    renderAnnouncements();
};

// Load on page open, and again each time the student opens the Announcements page
// so newly posted announcements show up without a full refresh.
window.addEventListener('hashchange', () => { if (location.hash === '#announcements') loadAnnouncements(); });
loadAnnouncements();
// ==================== FEATURE 02: ANNOUNCEMENTS JS END ======================

// ==================== STUDENT PORTAL: GRADES JS START =======================
// Loaded from the Grades table through api/accountapi (port 5002).
const GRADES_API = `${PORTAL_API}/grades`;
let gradeRecords = [];
const semesterSelect = document.getElementById('semesterSelect');
const showGradeStatus = addStatusLine('gradeStatus', semesterSelect.closest('.grades-section').querySelector('.table-wrap'));

async function loadGrades(){
    try {
        const response = await fetch(`${GRADES_API}?studentId=${encodeURIComponent(studentId)}`);
        const data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Request failed.');
        gradeRecords = data.grades;
        showGradeStatus('', false);
    } catch (error) {
        console.error(error);
        gradeRecords = [];
        showGradeStatus('Could not load grades. Make sure api/accountapi is running (dotnet run).', true);
    }
    renderGrades();
}

function renderGrades(){
    const rows = gradeRecords.filter(g => g.semester === semesterSelect.value);
    const cell = value => value == null ? '—' : value;
    document.getElementById('gradeRows').innerHTML = rows.length
        ? rows.map((g, i) => `<tr><td>${i+1}</td><td>${escapeHtml(g.subjectCode)}</td><td>${escapeHtml(g.subjectName)}</td><td>${escapeHtml(g.instructor)}</td><td>${Number(g.units).toFixed(2)}</td><td>${cell(g.prelim)}</td><td>${cell(g.midterm)}</td><td>${cell(g.finals)}</td><td>${cell(g.finalRating)}</td></tr>`).join('')
        : '<tr><td colspan="9" class="empty-cell">No grades recorded for this semester.</td></tr>';

    const units = rows.reduce((sum, g) => sum + g.units, 0);
    // Weighted by units, counting only subjects that already have that grade.
    const weighted = key => {
        const graded = rows.filter(g => g[key] != null);
        const u = graded.reduce((sum, g) => sum + g.units, 0);
        return u ? (graded.reduce((sum, g) => sum + g[key] * g.units, 0) / u).toFixed(2) : '—';
    };
    document.getElementById('gradeSummary').innerHTML = `<span>Total Units:</span><strong>${units.toFixed(2)}</strong><span>Prelim Weighted Average:</span><strong>${weighted('prelim')}</strong><span>Midterm Weighted Average:</span><strong>${weighted('midterm')}</strong><span>Final Weighted Average:</span><strong>${weighted('finalRating')}</strong>`;
}

semesterSelect.onchange = renderGrades;
loadGrades();
// ==================== STUDENT PORTAL: GRADES JS END =========================

// ==================== FEATURE 03: CLASS SCHEDULE JS START ====================
// Talks to api/accountapi (port 5002). Uses helpers defined in app.js.
(function () {
    const SCHEDULE_API = `${PORTAL_API}/schedule`;
    const DAYS = [['Mon', 'Monday'], ['Tue', 'Tuesday'], ['Wed', 'Wednesday'], ['Thu', 'Thursday'], ['Fri', 'Friday'], ['Sat', 'Saturday']];
    let entries = [];

    const form = document.getElementById('scheduleForm');
    const showStatus = addStatusLine('scheduleStatus', form);

    const minutes = value => { const [h, m] = value.split(':').map(Number); return h * 60 + m; };
    const time12 = value => {
        const [h, m] = value.split(':').map(Number);
        return `${((h + 11) % 12) + 1}:${String(m).padStart(2, '0')} ${h >= 12 ? 'PM' : 'AM'}`;
    };

    async function loadSchedule() {
        try {
            const response = await fetch(`${SCHEDULE_API}?studentId=${encodeURIComponent(studentId)}`);
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Request failed.');
            entries = data.entries;
            showStatus('', false);
        } catch (error) {
            console.error(error);
            entries = [];
            showStatus('Could not load your schedule. Make sure api/accountapi is running (dotnet run).', true);
        }
        renderSchedule();
    }

    function renderSchedule() {
        document.getElementById('weekBoard').innerHTML = DAYS.map(([key, label]) => {
            const items = entries.filter(e => e.day === key);
            const cards = items.length
                ? items.map(e => `<article class="class-card">
                        <strong>${escapeHtml(e.subjectCode)}</strong>
                        <span>${escapeHtml(e.subjectName)}</span>
                        <span class="class-time">${time12(e.start)} – ${time12(e.end)}</span>
                        ${e.room ? `<span>Room: ${escapeHtml(e.room)}</span>` : ''}
                        ${e.instructor ? `<span>${escapeHtml(e.instructor)}</span>` : ''}
                        <button type="button" class="action-btn" data-delete-class="${e.id}">Remove</button>
                    </article>`).join('')
                : '<p class="empty-day">No classes</p>';
            return `<div class="day-col"><h3>${label}</h3>${cards}</div>`;
        }).join('');

        const totalMinutes = entries.reduce((sum, e) => sum + (minutes(e.end) - minutes(e.start)), 0);
        document.getElementById('scheduleSummary').innerHTML =
            `<span>Class meetings per week:</span><strong>${entries.length}</strong>` +
            `<span>Total hours per week:</span><strong>${(totalMinutes / 60).toFixed(1)}</strong>`;
    }

    form.onsubmit = async event => {
        event.preventDefault();
        const start = document.getElementById('scStart').value;
        const end = document.getElementById('scEnd').value;
        if (minutes(end) <= minutes(start)) {
            showStatus('End time must be after the start time.', true);
            return;
        }
        const button = form.querySelector('button[type="submit"]');
        const body = {
            studentId,
            subjectCode: document.getElementById('scCode').value.trim(),
            subjectName: document.getElementById('scName').value.trim(),
            instructor: document.getElementById('scInstructor').value.trim(),
            room: document.getElementById('scRoom').value.trim(),
            day: document.getElementById('scDay').value,
            start,
            end
        };
        button.disabled = true;
        try {
            const response = await fetch(SCHEDULE_API, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
            const data = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(data.message || 'Could not save the class.');
            form.reset();
            await loadSchedule();
            showStatus('Class added.', false);
        } catch (error) {
            console.error(error);
            showStatus(error.message === 'Failed to fetch'
                ? 'Could not reach the server. Make sure api/accountapi is running (dotnet run).'
                : error.message, true);
        } finally {
            button.disabled = false;
        }
    };

    document.getElementById('weekBoard').onclick = async event => {
        const button = event.target.closest('[data-delete-class]');
        if (!button || !confirm('Remove this class from your schedule?')) return;
        try {
            const response = await fetch(`${SCHEDULE_API}/${button.dataset.deleteClass}?studentId=${encodeURIComponent(studentId)}`, { method: 'DELETE' });
            const data = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(data.message || 'Could not remove the class.');
            await loadSchedule();
            showStatus('Class removed.', false);
        } catch (error) {
            console.error(error);
            showStatus(error.message, true);
        }
    };

    window.addEventListener('hashchange', () => { if (location.hash === '#schedule') loadSchedule(); });
    loadSchedule();
})();
// ==================== FEATURE 03: CLASS SCHEDULE JS END ======================

// ==================== FEATURE 04: STATEMENT OF ACCOUNT JS START ==============
// Talks to api/accountapi (port 5002). Uses helpers defined in app.js.
(function () {
    const STATEMENT_API = `${PORTAL_API}/statement`;
    const status = document.getElementById('stStatus');

    function showStatus(text, isError) {
        status.textContent = text;
        status.classList.toggle('error', Boolean(isError));
    }

    document.getElementById('printStudent').textContent = `${String(student.name || 'Student').toUpperCase()} · ${studentId}`;

    async function loadStatement() {
        showStatus('Loading statement…', false);
        try {
            const response = await fetch(`${STATEMENT_API}?studentId=${encodeURIComponent(studentId)}`);
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Request failed.');
            showStatus('', false);
            renderStatement(data);
        } catch (error) {
            console.error(error);
            document.getElementById('statementCards').innerHTML = '';
            document.getElementById('statementRows').innerHTML = '<tr><td colspan="6" class="empty-cell">No statement to show.</td></tr>';
            showStatus('Could not load the statement. Make sure api/accountapi is running (dotnet run).', true);
        }
    }

    function renderStatement(data) {
        const t = data.totals;
        const state = t.balance > 0 ? 'due' : (t.balance < 0 ? 'credit' : 'clear');
        const label = t.balance > 0 ? 'Balance due' : (t.balance < 0 ? 'Overpaid' : 'Fully paid');

        document.getElementById('statementCards').innerHTML =
            `<div class="stat-card"><span>Total assessed</span><strong>${peso(t.assessed)}</strong></div>` +
            `<div class="stat-card"><span>Tuition payments</span><strong>${peso(t.paid)}</strong></div>` +
            `<div class="stat-card balance-${state}"><span>${label}</span><strong>${peso(Math.abs(t.balance))}</strong></div>` +
            `<div class="stat-card"><span>Non-school payments</span><strong>${peso(t.nonSchool)}</strong></div>`;

        document.getElementById('statementRows').innerHTML = data.ledger.length
            ? data.ledger.map(l => `<tr>
                    <td>${formatDate(l.date)}</td>
                    <td>${escapeHtml(l.description)}</td>
                    <td><span class="type-badge">${escapeHtml(l.kind)}</span></td>
                    <td>${l.charge ? peso(l.charge) : '—'}</td>
                    <td>${l.payment ? peso(l.payment) : '—'}</td>
                    <td>${peso(l.balance)}</td>
                </tr>`).join('')
            : '<tr><td colspan="6" class="empty-cell">No charges or payments yet.</td></tr>';
    }

    document.getElementById('printStatement').onclick = () => window.print();

    window.addEventListener('hashchange', () => { if (location.hash === '#statement-of-account') loadStatement(); });
    loadStatement();
})();
// ==================== FEATURE 04: STATEMENT OF ACCOUNT JS END ================

// ==================== FEATURE ROUTING START =================================
const titles={'#student-portal':'Student Portal','#announcements':'Announcements','#schedule':'Class Schedule','#statement-of-account':'Statement of Account'};function route(){ const hash=titles[location.hash]?location.hash:'#student-portal'; document.querySelectorAll('.feature-section').forEach(s=>s.classList.toggle('active-section','#'+s.id===hash)); document.querySelectorAll('.sidebar nav a').forEach(a=>a.classList.toggle('active',a.getAttribute('href')===hash)); document.getElementById('pageTitle').textContent=titles[hash]; }
window.addEventListener('hashchange',route); route();
// ==================== FEATURE ROUTING END ===================================