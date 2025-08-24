document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    const loginButton = document.getElementById('loginButton');       

    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            // Показываем индикатор загрузки
            loginButton.classList.add('loading');            
            loginButton.disabled = true;

            const email = document.getElementById('email').value;           
            

            const jsonData = JSON.stringify({
                Email: email
                
            });

            try {
                const response = await fetch("/Authorization/Login", {
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
                    window.location.href = "/";
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