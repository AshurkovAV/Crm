document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    const loginButton = document.getElementById('loginBtn');       

    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            // Показываем индикатор загрузки
            loginButton.classList.add('loading');            
            loginButton.disabled = true;           

            try {
                const password = document.getElementById('passwordInput').value;
                const email = sessionStorage.getItem('pendingVerificationEmail');

                const jsonData = JSON.stringify({
                    Password: password,
                    Email: email
                });

                const response = await fetch("/Account/VerifyPassword", {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: jsonData
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message);
                }

                const data = await response.json();
                if (data.success) {
                    if (data.redirectUrl) {                       
                        // Перенаправляем на страницу верификации
                        window.location.href = data.redirectUrl;
                    } else {
                        // Обычный успешный вход
                        window.location.href = "/";
                    }
                } else {
                    throw new Error(data.message);
                }
            } catch (error) {
                loginButton.classList.remove('loading');                
                loginButton.disabled = false;
                handleLoginError(error.message);
            }
            finally {                
                loginButton.classList.remove('loading');                
                loginButton.disabled = false;
            }
        });
    }

    function handleLoginError(errorType) {
        let message = "Ошибка при авторизации";

        switch (errorType) {
            case 'db_connection':
                message = "Соединение с базой данных потеряно";
                break;
            case 'invalid_credentials':
                message = "Неверный логин или пароль";
                break;
            case 'not_email':
                message = "Пользователь не найден, либо не активен";
                break;
            case 'network_error':
                message = "Проблемы с соединением к серверу";
                break;
        }

        if (typeof DevExpress !== 'undefined') {
            DevExpress.ui.notify({
                message: message,
                type: "error",
                displayTime: 5000
            });
        } else {
            alert(message); // Fallback если DevExpress не загружен
        }
    }
});