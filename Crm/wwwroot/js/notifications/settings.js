/* Карточка «Уведомления» в профиле: переключатели каналов и подключение MAX. */
(function () {
    'use strict';
    var card = document.getElementById('notificationSettings');
    if (!card) return;
    var csrf = card.getAttribute('data-csrf');

    function post(url, body) {
        return fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': csrf },
            body: JSON.stringify(body || {})
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (data) {
                if (!r.ok) throw new Error(data.message || ('Ошибка ' + r.status));
                return data;
            });
        });
    }

    // MAX-переключатели недоступны, пока MAX не подключён
    function syncMaxPrefs() {
        var linked = card.getAttribute('data-max-state') === 'linked';
        card.querySelectorAll('[data-max-pref]').forEach(function (i) {
            i.disabled = !linked;
            i.closest('.nt-switch').title = linked ? '' : 'Сначала подключите MAX';
        });
    }
    syncMaxPrefs();

    var savedTimer = null;
    card.querySelectorAll('[data-pref]').forEach(function (input) {
        input.addEventListener('change', function () {
            var body = {};
            card.querySelectorAll('[data-pref]').forEach(function (i) { body[i.getAttribute('data-pref')] = i.checked; });
            post('/notifications/preferences', body).then(function () {
                var s = card.querySelector('[data-saved]');
                s.hidden = false;
                clearTimeout(savedTimer);
                savedTimer = setTimeout(function () { s.hidden = true; }, 2000);
            }).catch(function (e) { input.checked = !input.checked; alert(e.message); });
        });
    });

    var linkBox = card.querySelector('[data-max-link]');
    card.addEventListener('click', function (e) {
        var action = e.target.closest('[data-max-action]');
        if (action) {
            action.disabled = true;
            if (action.getAttribute('data-max-action') === 'unlink') {
                if (!confirm('Отключить уведомления в MAX?')) { action.disabled = false; return; }
                post('/notifications/max/unlink').then(function () { location.reload(); })
                    .catch(function (err) { action.disabled = false; alert(err.message); });
            } else {
                post('/notifications/max/link').then(function (data) {
                    action.disabled = false;
                    linkBox.hidden = false;
                    linkBox.querySelector('[data-max-open]').href = data.Url;
                    linkBox.querySelector('[data-max-minutes]').textContent = data.Minutes;
                    linkBox.setAttribute('data-url', data.Url);
                    window.open(data.Url, '_blank', 'noopener');
                }).catch(function (err) { action.disabled = false; alert(err.message); });
            }
            return;
        }
        if (e.target.closest('[data-max-copy]')) {
            var url = linkBox.getAttribute('data-url');
            (navigator.clipboard ? navigator.clipboard.writeText(url) : Promise.reject()).catch(function () { prompt('Ссылка для подключения MAX:', url); });
            return;
        }
        if (e.target.closest('[data-max-check]')) {
            fetch('/notifications/settings', { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
                .then(function (r) { return r.json(); })
                .then(function (s) {
                    if (s.MaxLinked) location.reload();
                    else alert('MAX пока не подключён. Откройте ссылку и нажмите «Начать» в чате с ботом.');
                });
        }
    });
})();
