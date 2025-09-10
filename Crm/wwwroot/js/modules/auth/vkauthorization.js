// Ждем полной загрузки DOM
document.addEventListener('DOMContentLoaded', function () {
    const vkAuthBtn = document.getElementById('vkAuthBtn');
    const container = document.getElementById('vkid-container');

    if (!vkAuthBtn) {
        console.error('Элемент с id "vkAuthBtn" не найден');
        return;
    }

    if (!container) {
        console.error('Элемент с id "vkid-container" не найден');
        return;
    }

    vkAuthBtn.addEventListener('click', function () {
        if ('VKIDSDK' in window) {
            const VKID = window.VKIDSDK;

            VKID.Config.init({
                app: 54112877,
                redirectUrl: 'https://crm.biglv.ru/vkauth/callback',
                responseMode: VKID.ConfigResponseMode.Callback,
                
                scope: 'email', // Заполните нужными доступами по необходимости
            });

            const oneTap = new VKID.OneTap();

            oneTap.render({
                container: document.currentScript.parentElement,
                scheme: 'dark',
                showAlternativeLogin: true
            })
                .on(VKID.WidgetEvents.ERROR, vkidOnError)
                .on(VKID.OneTapInternalEvents.LOGIN_SUCCESS, function (payload) {
                    const code = payload.code;
                    const deviceId = payload.device_id;

                    VKID.Auth.exchangeCode(code, deviceId)
                        .then(vkidOnSuccess)
                        .catch(vkidOnError);
                });

            function vkidOnSuccess(data) {
                // Обработка полученного результата
            }

            function vkidOnError(error) {
                // Обработка ошибки
            }
        
        } else {
            console.error('VKID SDK не доступен');
        }
    });

    function sendToServer(authData) {
        fetch('/vkauth/callback', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                access_token: authData.access_token,
                user_id: authData.user_id,
                expires_in: authData.expires_in,
                email: authData.email || ''
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Ошибка сервера: ' + response.status);
                }
                return response.json();
            })
            .then(serverData => {
                console.log('Сервер ответил:', serverData);
                container.style.display = 'none';
                updateUI(serverData);
            })
            .catch(error => {
                console.error('Ошибка отправки:', error);
                container.style.display = 'none';
            });
    }

    function updateUI(userData) {
        vkAuthBtn.style.display = 'none';
        const userInfo = document.createElement('div');
        userInfo.innerHTML = `
            <h3>Добро пожаловать!</h3>
            <p>User ID: ${userData.user_id}</p>
            <button onclick="location.reload()">Выйти</button>
        `;
        document.body.appendChild(userInfo);
    }
});