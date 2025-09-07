document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('yandexAuthBtn').addEventListener('click', function () {
        // Весь ваш код здесь
        const width = 500;
        const height = 600;
        const left = (window.screen.width - width) / 2;
        const top = (window.screen.height - height) / 2;

        const authUrl = new URL('https://oauth.yandex.ru/authorize');

        const params = {
            response_type: 'code',
            client_id: '9f8dd7d3ac2b42228103fcbe8f11682f',
            redirect_uri: 'https://crm.biglv.ru/yandexauth/callback',
            scope: 'login:email login:info login:avatar',
            display: 'popup',
            origin: 'https://crm.biglv.ru',
            state: Math.random().toString(36).substring(2, 15),
            prompt: 'select_account',
            force_confirm: true
        };

        Object.keys(params).forEach(key => {
            authUrl.searchParams.append(key, params[key]);
        });

        const popup = window.open(
            authUrl.toString(),
            'yandex_oauth',
            `width=${width},height=${height},left=${left},top=${top},scrollbars=yes,resizable=yes`
        );

        if (!popup) {
            alert('Пожалуйста, разрешите всплывающие окна для этого сайта');
            return;
        }
    });
});

window.addEventListener('message', function (event) {
            // Проверяем origin сообщения для безопасности
            if (event.origin !== 'https://crm.biglv.ru') return;

    if (event.data.type === 'YANDEX_AUTH_SUCCESS') {
                        console.log('Авторизация успешна через Яндекс');
                        console.log('Email:', event.data.email);
                        console.log('Name:', event.data.name);

                        // Показываем уведомление об успехе
                        showAuthSuccessNotification();

                        // Перенаправляем на главную страницу
                        setTimeout(function () {
                            window.location.href = '/';
                                    }, 1500);
                            }
   }
   );

    // Функция показа уведомления об успешной авторизации
    function showAuthSuccessNotification() {
            const notification = document.createElement('div');
                notification.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: #28a745;
                color: white;
                padding: 15px 20px;
                border-radius: 5px;
                z-index: 10000;
                font-weight: bold;
                `;
    notification.textContent = 'Авторизация успешна!';
    document.body.appendChild(notification);

            setTimeout(() => {
        notification.remove();
            }, 3000);
        }

   

    // Функция для обработки callback с токеном (если нужно)
    function handleOAuthCallback() {
            const hash = window.location.hash.substring(1);
    const params = new URLSearchParams(hash);

    if (params.has('access_token')) {
                const accessToken = params.get('access_token');
    console.log('Access Token получен:', accessToken);
            }
        }

    // Проверяем, не вернулись ли мы с токеном
    handleOAuthCallback();