document.addEventListener('DOMContentLoaded', function () {
    const progressBar = document.getElementById('progressBar');
    const statusText = document.getElementById('statusText');
    const completionMessage = document.getElementById('completionMessage');
    const errorMessage = document.getElementById('errorMessage');

    // Функция обновления статуса (оставлена, но не используется для отображения)
    function updateStatus(message) {
    }

    // Функция завершения процесса
    function completeProcess() {
        progressBar.style.width = '100%';
        progressBar.classList.remove('running');
        statusText.style.display = 'none';
        completionMessage.style.display = 'block';

        window.location.href = '/';
    }

    // Функция обработки ошибки
    function handleError(error) {
        progressBar.classList.remove('running');
        statusText.style.display = 'none';
        errorMessage.textContent = `Ошибка: ${error}`;
        errorMessage.style.display = 'block';
    }

    async function startProcess() {

        try {
            // Отправляем запрос на сервер для начала процесса
            const response = await fetch('/CrmSetup/start', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Ошибка подключения к серверу');
            }

            const data = await response.json();
            // Сообщение о создании CRM больше не отображается

            // Запускаем анимацию прогресс-бара
            progressBar.classList.add('running');

            // Запускаем опрос статуса
            pollStatus(data.processId);

        } catch (error) {
            handleError("Не удалось подключиться к серверу");
        }
    }

    // Функция опроса статуса
    async function pollStatus(processId) {
        try {
            const response = await fetch(`/CrmSetup/status/${processId}`);

            if (!response.ok) {
                throw new Error('Ошибка получения статуса');
            }

            const status = await response.json();

            if (status.completed) {
                completeProcess();
            } else if (status.failed) {
                handleError(status.error || "Процесс завершился с ошибкой");
            } else {
                // Плавно увеличиваем прогресс
                const currentWidth = parseInt(progressBar.style.width || '0');
                const newWidth = Math.min(currentWidth + 5, 95);
                progressBar.style.width = `${newWidth}%`;

                // Продолжаем опрос через 1.5 секунды
                setTimeout(() => pollStatus(processId), 1500);
            }

        } catch (error) {
            handleError("Ошибка при проверке статуса");
        }
    }

    // Запускаем процесс автоматически после загрузки страницы
    setTimeout(startProcess, 1000);
});