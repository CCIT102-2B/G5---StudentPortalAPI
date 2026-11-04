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
// ==================== SHARED PORTAL / SESSION CODE END ======================

// ==================== FEATURE 02: TRANSACTIONS JS START =====================
// ============================================================
// PART A: NEW BLOCK
// Paste this block into app.js right ABOVE the
// "FEATURE ROUTING START" line.
// ============================================================

// ==================== FEATURE 02: ANNOUNCEMENTS JS START ====================
// Talks to api/announcementapi. Start it with: dotnet run (port 5003).
const ANNOUNCEMENT_API = 'http://localhost:5003/api/announcements';

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

function escapeHtml(value){
    return String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
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
        showAnnounceStatus('Could not load announcements. Start api/announcementapi with dotnet run.', true);
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


// ============================================================
// PART B: ROUTING TITLES
// In the routing block, replace the `const titles=...` line with:
// ============================================================
// const titles={'#student-portal':'Student Portal','#feature-02':'Announcements','#feature-03':'Feature 03','#feature-04':'Feature 04'};


// ============================================================
// PART C (optional): RENAME TWO COMMENTS
// Your Transactions code belongs to Feature 01, not Feature 02, so rename
// "FEATURE 02: TRANSACTIONS JS START/END" to "STUDENT PORTAL: TRANSACTIONS JS START/END".
// ============================================================
// ==================== FEATURE 02: TRANSACTIONS JS END =======================

// ==================== STUDENT PORTAL: GRADES JS START =======================
// Fictional showcase data only. These subjects were created for this demo and
// are not copied from the visual reference. Replace this object with API data later.
const gradeData = {
 first:[
  ['CS101','Introduction to Computing','A. Rivera',3,91,93,94,93],
  ['MATH110','College Algebra','L. Mendoza',3,88,90,92,90],
  ['ENG105','Academic Communication','C. Santos',3,94,92,95,94],
  ['HUM120','Art and Society','R. Flores',3,90,91,93,91],
  ['PE101','Movement and Wellness','D. Cruz',2,95,96,94,95]
 ],
 second:[
  ['CS120','Programming Fundamentals','M. Villanueva',3,92,94,95,94],
  ['STAT115','Introductory Statistics','E. Garcia',3,89,91,93,91],
  ['ENG120','Professional Communication','N. Reyes',3,93,94,94,94],
  ['SOC110','Culture and Community','P. Lim',3,91,90,92,91],
  ['PE102','Fitness and Recreation','D. Cruz',2,96,95,97,96]
 ]
};
function renderGrades(){
 const rows=gradeData[document.getElementById('semesterSelect').value];
 document.getElementById('gradeRows').innerHTML=rows.map((r,i)=>`<tr><td>${i+1}</td><td>${r[0]}</td><td>${r[1]}</td><td>${r[2]}</td><td>${Number(r[3]).toFixed(2)}</td><td>${r[4]}</td><td>${r[5]}</td><td>${r[6]}</td><td>${r[7]}</td></tr>`).join('');
 const units=rows.reduce((sum,row)=>sum+row[3],0);
 const weighted=(column)=>(rows.reduce((sum,row)=>sum+row[column]*row[3],0)/units).toFixed(2);
 document.getElementById('gradeSummary').innerHTML=`<span>Total Units:</span><strong>${units.toFixed(2)}</strong><span>Prelim Weighted Average:</span><strong>${weighted(4)}</strong><span>Midterm Weighted Average:</span><strong>${weighted(5)}</strong><span>Final Weighted Average:</span><strong>${weighted(7)}</strong>`;
}
document.getElementById('semesterSelect').onchange=renderGrades;
renderGrades();
// ==================== STUDENT PORTAL: GRADES JS END =========================

// ==================== FEATURE ROUTING START =================================
const titles={'#student-portal':'Student Portal','#announcements':'Announcements','#feature-03':'Feature 03','#feature-04':'Feature 04'};
function route(){ const hash=titles[location.hash]?location.hash:'#student-portal'; document.querySelectorAll('.feature-section').forEach(s=>s.classList.toggle('active-section','#'+s.id===hash)); document.querySelectorAll('.sidebar nav a').forEach(a=>a.classList.toggle('active',a.getAttribute('href')===hash)); document.getElementById('pageTitle').textContent=titles[hash]; }
window.addEventListener('hashchange',route); route();
// ==================== FEATURE ROUTING END ===================================
