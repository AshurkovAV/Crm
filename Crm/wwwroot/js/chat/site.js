document.addEventListener('DOMContentLoaded', function () {
    if (window.location.pathname.toLowerCase() === '/online') {
        return;
    }

    // Добавляем в существующий скрипт
    const chatToggle = document.getElementById('chatToggle');
    const chatContainer = document.getElementById('chatContainer');
    let isChatView = false;

    chatToggle.addEventListener('click', function (event) {
        if (event.target.closest('a')) {
            return;
        }

        if (isChatView) {
            // Возвращаемся к предыдущему виду
            changeView(currentView);
        } else {
            // Сохраняем текущий вид и переключаем на чат
            currentView = document.querySelector('.tab.active')?.dataset.view || 'dashboard';
            showChatView();
        }
    });

    function showChatView() {
        isChatView = true;

        // Обновляем заголовок
        const titleElement = contentTopBar.querySelector('.content-title');
        titleElement.textContent = "Сообщения";
        titleElement.style.color = "#6f42c1";

        // Показываем чат
        contentArea.innerHTML = chatContainer.innerHTML;
        contentTopBar.className = 'content-top-bar communications-view';
        contentArea.className = 'communications-view';

        // Обновляем верхнее меню для чата
        updateTopMenu('communications');

        // Делаем активной вкладку коммуникаций
        tabs.forEach(tab => {
            if (tab.dataset.view === 'communications') {
                tab.classList.add('active');
            } else {
                tab.classList.remove('active');
            }
        });
    }

    // Добавляем переменную для отслеживания текущего вида
    let currentView = 'dashboard';

    // Обновляем функцию changeView
    function changeView(viewName) {
        isChatView = false;
        currentView = viewName;

        const view = views[viewName] || views.dashboard;

        // Обновляем заголовок
        const titleElement = contentTopBar.querySelector('.content-title');
        titleElement.textContent = view.title;
        titleElement.style.color = view.color;

        // Обновляем контент
        contentArea.innerHTML = view.content;

        // Обновляем классы для стилизации
        contentTopBar.className = 'content-top-bar ' + viewName + '-view';
        contentArea.className = viewName + '-view';

        // Обновляем верхнее меню
        updateTopMenu(viewName);

        // Обновляем активные вкладки
        tabs.forEach(tab => {
            if (tab.dataset.view === viewName) {
                tab.classList.add('active');
            } else {
                tab.classList.remove('active');
            }
        });
    }



    // Добавляем в существующий скрипт
    function openChat(chatId) {
        // Здесь можно загрузить конкретный чат через AJAX
        // Для примера используем статические данные
        const chatData = {
            id: chatId,
            name: "Иван Петров",
            isOnline: true
        };

        // Загружаем окно чата через AJAX или создаем из шаблона
        showChatWindow(chatData);
    }

    function showChatWindow(chatData) {
        const chatHTML = `
        <div class="chat-window" data-chat-id="${chatData.id}">
            <div class="chat-window-header">
                <button class="back-button" onclick="closeChatWindow()">
                    <i class="fas fa-arrow-left"></i>
                </button>
                <div class="chat-user-info">
                    <div class="chat-avatar">
                        <i class="fas fa-user"></i>
                        ${chatData.isOnline ? '<div class="online-indicator"></div>' : ''}
                    </div>
                    <div class="chat-user-details">
                        <div class="chat-user-name">${chatData.name}</div>
                        <div class="chat-user-status">${chatData.isOnline ? 'online' : 'offline'}</div>
                    </div>
                </div>
                <div class="chat-actions">
                    <button class="chat-btn"><i class="fas fa-phone"></i></button>
                    <button class="chat-btn"><i class="fas fa-video"></i></button>
                    <button class="chat-btn"><i class="fas fa-ellipsis-v"></i></button>
                </div>
            </div>

            <div class="chat-messages" id="chatMessages-${chatData.id}">
                <div class="message received">
                    <div class="message-content">
                        <div class="message-text">Привет! Как дела с проектом?</div>
                        <div class="message-time">12:30</div>
                    </div>
                </div>
                <div class="message sent">
                    <div class="message-content">
                        <div class="message-text">Всё отлично! Заканчиваю последние правки</div>
                        <div class="message-time">12:32</div>
                    </div>
                </div>
                <div class="message received">
                    <div class="message-content">
                        <div class="message-text">Отлично! Жду результат к концу дня</div>
                        <div class="message-time">12:35</div>
                    </div>
                </div>
            </div>

            <div class="chat-input-container">
                <div class="chat-input-actions">
                    <button class="input-btn"><i class="fas fa-paperclip"></i></button>
                    <button class="input-btn"><i class="fas fa-smile"></i></button>
                </div>
                <input type="text" class="chat-input" placeholder="Введите сообщение..." 
                       onkeypress="handleMessageInput(event, ${chatData.id})">
                <button class="send-btn" onclick="sendMessage(${chatData.id})">
                    <i class="fas fa-paper-plane"></i>
                </button>
            </div>
        </div>
    `;

        contentArea.innerHTML = chatHTML;

        // Прокручиваем к последнему сообщению
        setTimeout(() => {
            const messagesContainer = document.getElementById(`chatMessages-${chatData.id}`);
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }, 100);
    }

    function closeChatWindow() {
        // Возвращаемся к списку чатов
        showChatView();
    }

    function handleMessageInput(event, chatId) {
        if (event.key === 'Enter') {
            sendMessage(chatId);
        }
    }

    function sendMessage(chatId) {
        const input = document.querySelector(`.chat-input`);
        const messageText = input.value.trim();

        if (messageText) {
            const messagesContainer = document.getElementById(`chatMessages-${chatId}`);
            const currentTime = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

            const messageHTML = `
            <div class="message sent">
                <div class="message-content">
                    <div class="message-text">${messageText}</div>
                    <div class="message-time">${currentTime}</div>
                </div>
            </div>
        `;

            messagesContainer.innerHTML += messageHTML;
            input.value = '';

            // Прокручиваем к последнему сообщению
            messagesContainer.scrollTop = messagesContainer.scrollHeight;

            // Имитация ответа (можно удалить)
            setTimeout(() => {
                const responseHTML = `
                <div class="message received">
                    <div class="message-content">
                        <div class="message-text">Получил ваше сообщение!</div>
                        <div class="message-time">${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                    </div>
                </div>
            `;
                messagesContainer.innerHTML += responseHTML;
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            }, 1000);
        }
    }

    // Обновляем обработчик клика на чат
    document.addEventListener('click', function (e) {
        const chatItem = e.target.closest('.chat-item');
        if (chatItem) {
            const chatId = chatItem.dataset.chatId;
            openChat(parseInt(chatId));
        }
    });



});