const ACCOUNT_API = 'http://localhost:5002/api/account';

const loginForm = document.getElementById('loginForm');
const adminForm = document.getElementById('adminForm');
const createForm = document.getElementById('createForm');
const message = document.getElementById('message');
const loginTab = document.getElementById('loginTab');
const adminTab = document.getElementById('adminTab');
const createTab = document.getElementById('createTab');

loginTab.onclick = () => switchForm('login');
adminTab.onclick = () => switchForm('admin');
createTab.onclick = () => switchForm('create');

function switchForm(mode) {
    loginForm.classList.toggle('hidden', mode !== 'login');
    adminForm.classList.toggle('hidden', mode !== 'admin');
    createForm.classList.toggle('hidden', mode !== 'create');
    loginTab.classList.toggle('active', mode === 'login');
    adminTab.classList.toggle('active', mode === 'admin');
    createTab.classList.toggle('active', mode === 'create');
    message.textContent = '';
}

loginForm.onsubmit = async event => {
    event.preventDefault();
    await send('/login', {
        studentId: loginStudentId.value.trim(),
        password: loginPassword.value
    }, 'student');
};

adminForm.onsubmit = async event => {
    event.preventDefault();
    await send('/admin-login', {
        username: adminUsername.value.trim(),
        password: adminPassword.value
    }, 'admin');
};

createForm.onsubmit = async event => {
    event.preventDefault();
    await send('/create', {
        firstName: firstName.value.trim(),
        middleName: middleName.value.trim(),
        lastName: lastName.value.trim(),
        email: email.value.trim(),
        program: program.value,
        yearLevel: yearLevel.value,
        password: password.value
    }, 'student');
};

async function send(endpoint, body, role) {
    message.textContent = 'Please wait...';
    try {
        const response = await fetch(ACCOUNT_API + endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const data = await response.json();
        if (!response.ok) {
            message.textContent = data.message || 'Request failed.';
            return;
        }

        if (role === 'admin') {
            sessionStorage.setItem('role', 'Admin');
            sessionStorage.setItem('admin', JSON.stringify({ username: data.username, role: data.role }));
        } else {
            sessionStorage.setItem('role', 'Student');
            sessionStorage.setItem('student', JSON.stringify(data.student));
        }

        location.href = '../index.html';
    } catch {
        message.textContent = 'Account API is not running. Start api/accountapi with dotnet run.';
    }
}
