// ==================== ACCOUNT API CONFIGURATION START ====================
const ACCOUNT_API = 'http://localhost:5002/api/account';
// ==================== ACCOUNT API CONFIGURATION END ======================

const loginForm = document.getElementById('loginForm');
const createForm = document.getElementById('createForm');
const message = document.getElementById('message');

loginTab.onclick = () => switchForm(true);
createTab.onclick = () => switchForm(false);
function switchForm(showLogin) {
    loginForm.classList.toggle('hidden', !showLogin);
    createForm.classList.toggle('hidden', showLogin);
    loginTab.classList.toggle('active', showLogin);
    createTab.classList.toggle('active', !showLogin);
    message.textContent = '';
}

loginForm.onsubmit = async (event) => {
    event.preventDefault();
    await send('/login', { studentId: loginStudentId.value.trim(), password: loginPassword.value });
};

createForm.onsubmit = async (event) => {
    event.preventDefault();
    await send('/create', {
        studentId: studentId.value.trim(), firstName: firstName.value.trim(), middleName: middleName.value.trim(),
        lastName: lastName.value.trim(), email: email.value.trim(), program: program.value.trim(),
        yearLevel: yearLevel.value.trim(), password: password.value
    });
};

async function send(endpoint, body) {
    message.textContent = 'Please wait...';
    try {
        const response = await fetch(ACCOUNT_API + endpoint, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
        const data = await response.json();
        if (!response.ok) { message.textContent = data.message || 'Request failed.'; return; }
        sessionStorage.setItem('student', JSON.stringify(data.student));
        location.href = '../index.html';
    } catch {
        message.textContent = 'Account API is not running. Start api/accountapi with dotnet run.';
    }
}
