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
// Demo-only browser storage. Replace these localStorage operations with fetch()
// calls when the Transactions API is implemented.
const txKey = `studentPortalTransactions:${studentId}`;
const exampleTransactions = [
    {id:1,date:'2026-08-12',description:'First tuition installment',type:'Tuition Fee',amount:12500,notes:'Official receipt recorded'},
    {id:2,date:'2026-09-03',description:'Student organization shirt',type:'Non-School Fee',amount:450,notes:'Optional activity'},
    {id:3,date:'2026-10-15',description:'Midterm tuition payment',type:'Tuition Fee',amount:8000,notes:''}
];
let transactions = JSON.parse(localStorage.getItem(txKey) || 'null') || exampleTransactions;

function saveTransactions(){ localStorage.setItem(txKey, JSON.stringify(transactions)); }
function money(value){ return new Intl.NumberFormat('en-PH',{style:'currency',currency:'PHP'}).format(value); }
function renderTransactions(){
    const rows = document.getElementById('transactionRows');
    rows.innerHTML = transactions.length ? transactions.map(tx => `<tr><td>${tx.date}</td><td>${escapeHtml(tx.description)}</td><td><span class="type-badge">${escapeHtml(tx.type)}</span></td><td>${money(tx.amount)}</td><td>${escapeHtml(tx.notes || '—')}</td><td><button class="delete-btn" data-delete-tx="${tx.id}">Delete</button></td></tr>`).join('') : '<tr><td colspan="6" class="empty-cell">No transactions recorded.</td></tr>';
    document.getElementById('transactionTotal').textContent = money(transactions.reduce((sum,tx)=>sum+Number(tx.amount),0));
    document.querySelectorAll('[data-delete-tx]').forEach(button => button.onclick = () => { transactions = transactions.filter(tx => String(tx.id) !== button.dataset.deleteTx); saveTransactions(); renderTransactions(); });
}
function escapeHtml(value){ const div=document.createElement('div'); div.textContent=String(value); return div.innerHTML; }
document.getElementById('txDate').value = new Date().toISOString().slice(0,10);
document.getElementById('transactionForm').onsubmit = event => {
    event.preventDefault();
    transactions.push({id:Date.now(),description:document.getElementById('txDescription').value.trim(),amount:Number(document.getElementById('txAmount').value),type:document.getElementById('txType').value,date:document.getElementById('txDate').value,notes:document.getElementById('txNotes').value.trim()});
    saveTransactions(); event.target.reset(); document.getElementById('txDate').value = new Date().toISOString().slice(0,10); renderTransactions();
};
renderTransactions();
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
const titles={'#student-portal':'Student Portal','#feature-02':'Feature 02','#feature-03':'Feature 03','#feature-04':'Feature 04'};
function route(){ const hash=titles[location.hash]?location.hash:'#student-portal'; document.querySelectorAll('.feature-section').forEach(s=>s.classList.toggle('active-section','#'+s.id===hash)); document.querySelectorAll('.sidebar nav a').forEach(a=>a.classList.toggle('active',a.getAttribute('href')===hash)); document.getElementById('pageTitle').textContent=titles[hash]; }
window.addEventListener('hashchange',route); route();
// ==================== FEATURE ROUTING END ===================================
