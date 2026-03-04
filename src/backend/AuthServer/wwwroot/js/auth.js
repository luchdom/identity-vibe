// Auth-specific JavaScript functionality

function fillCredentials() {
    const select = document.getElementById('credential-select');
    const emailInput = document.getElementById('email-input');
    const passwordInput = document.getElementById('password-input');

    if (!select || !emailInput || !passwordInput) {
        console.warn('Credential fill elements not found');
        return;
    }

    switch(select.value) {
        case 'admin':
            emailInput.value = 'admin@example.com';
            passwordInput.value = 'Admin123!';
            break;
        case 'user':
            emailInput.value = 'user@example.com';
            passwordInput.value = 'User123!';
            break;
        case 'custom':
        default:
            emailInput.value = '';
            passwordInput.value = '';
            break;
    }
}

function togglePasswordVisibility() {
    const passwordInput = document.getElementById('password-input');

    if (!passwordInput) {
        return;
    }

    const isPassword = passwordInput.getAttribute('type') === 'password';
    passwordInput.setAttribute('type', isPassword ? 'text' : 'password');
}
