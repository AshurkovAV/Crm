/* =====================================================================
   База знаний: общие обработчики страниц /Wiki (кроме самого редактора — editor.js).
   Все POST-запросы отправляют antiforgery-токен в заголовке RequestVerificationToken.
   ===================================================================== */
(function () {
    'use strict';

    var root = document.querySelector('[data-wiki-root]');
    if (!root) return;

    // ---------------------------------------------------------------- утилиты
    function csrf() {
        var meta = document.querySelector('meta[name="wiki-csrf"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function api(url, options) {
        options = options || {};
        var headers = { 'Accept': 'application/json', 'X-Requested-With': 'XMLHttpRequest' };
        var body = options.body;
        var method = options.method || (body !== undefined ? 'POST' : 'GET');
        if (method !== 'GET') headers['RequestVerificationToken'] = csrf();
        if (body !== undefined && !(body instanceof FormData)) {
            headers['Content-Type'] = 'application/json';
            body = JSON.stringify(body);
        }
        return fetch(url, { method: method, headers: headers, body: body, credentials: 'same-origin' })
            .then(function (r) {
                return r.text().then(function (text) {
                    var data = null;
                    try { data = text ? JSON.parse(text) : null; } catch (e) { data = null; }
                    if (!r.ok) {
                        var err = new Error((data && (data.message || data.Message)) || httpMessage(r.status));
                        err.status = r.status;
                        err.data = data;
                        throw err;
                    }
                    return data;
                });
            });
    }

    function httpMessage(status) {
        if (status === 403) return 'Недостаточно прав для этого действия.';
        if (status === 404) return 'Не найдено или нет доступа.';
        if (status === 413) return 'Файл слишком большой.';
        if (status === 400) return 'Запрос отклонён (проверьте данные или обновите страницу).';
        return 'Ошибка сервера (' + status + ').';
    }

    var toastTimer = null;
    function toast(message, isError) {
        var el = document.querySelector('.wk-toast');
        if (!el) {
            el = document.createElement('div');
            el.className = 'wk-toast';
            el.setAttribute('role', 'status');
            document.body.appendChild(el);
        }
        el.textContent = message;
        el.classList.toggle('is-error', !!isError);
        el.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { el.hidden = true; }, 3500);
    }

    function esc(text) {
        return String(text == null ? '' : text)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function openDialog(id) {
        var d = document.getElementById(id);
        if (d && !d.open) d.showModal();
        return d;
    }

    document.addEventListener('click', function (e) {
        var closer = e.target.closest('[data-dialog-close]');
        if (closer) {
            var dlg = closer.closest('dialog');
            if (dlg) dlg.close();
        }
    });
    // Клик по подложке закрывает диалог
    document.querySelectorAll('dialog.wk-dialog').forEach(function (d) {
        d.addEventListener('click', function (e) { if (e.target === d) d.close(); });
    });

    window.WikiApi = { api: api, toast: toast, esc: esc, openDialog: openDialog, csrf: csrf };

    // ---------------------------------------------------------------- меню «…»
    function closeMenus(except) {
        document.querySelectorAll('.wk-menu').forEach(function (m) {
            if (m !== except && !m.hidden) {
                m.hidden = true;
                var t = m.parentElement.querySelector('[data-menu-toggle]');
                if (t) t.setAttribute('aria-expanded', 'false');
            }
        });
    }
    document.addEventListener('click', function (e) {
        var toggle = e.target.closest('[data-menu-toggle]');
        if (toggle) {
            e.preventDefault();
            e.stopPropagation();
            var menu = toggle.parentElement.querySelector('.wk-menu');
            var willOpen = menu.hidden;
            closeMenus();
            menu.hidden = !willOpen;
            toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
            if (willOpen) {
                // Не вылезать за правый/нижний край экрана
                menu.style.right = '0'; menu.style.left = 'auto';
                var rect = menu.getBoundingClientRect();
                if (rect.left < 8) { menu.style.left = '0'; menu.style.right = 'auto'; }
            }
            return;
        }
        if (!e.target.closest('.wk-menu')) closeMenus();
        else if (e.target.closest('.wk-menu button, .wk-menu a')) setTimeout(closeMenus, 0);
    });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeMenus(); });

    // ---------------------------------------------------------------- новая страница
    var newPageDialog = document.getElementById('wkNewPageDialog');
    if (newPageDialog) {
        var npForm = newPageDialog.querySelector('form');
        var npTemplate = 'blank';

        newPageDialog.querySelectorAll('[data-template]').forEach(function (b) {
            b.addEventListener('click', function () {
                npTemplate = b.getAttribute('data-template');
                newPageDialog.querySelectorAll('[data-template]').forEach(function (x) {
                    x.classList.toggle('is-on', x === b);
                    x.setAttribute('aria-checked', x === b ? 'true' : 'false');
                });
            });
        });

        document.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-wiki-new-page]');
            if (!btn) return;
            e.preventDefault();
            npForm.reset();
            var sectionId = btn.getAttribute('data-section-id');
            var parentId = btn.getAttribute('data-parent-id');
            var sectionField = npForm.querySelector('[data-section-field]');
            var select = npForm.elements.sectionId;
            if (sectionId) {
                select.value = sectionId;
                sectionField.hidden = true;
            } else {
                sectionField.hidden = select.options.length <= 1;
            }
            npForm.elements.parentId.value = parentId || '';
            var note = npForm.querySelector('[data-parent-note]');
            note.hidden = !parentId;
            if (parentId) note.querySelector('small').textContent = 'Подстраница для «' + (btn.getAttribute('data-parent-title') || '') + '»';
            openDialog('wkNewPageDialog');
            setTimeout(function () { npForm.elements.title.focus(); }, 30);
        });

        npForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var submit = npForm.querySelector('[type=submit]');
            submit.disabled = true;
            api('/Wiki/api/pages', {
                body: {
                    SectionId: parseInt(npForm.elements.sectionId.value, 10),
                    ParentId: npForm.elements.parentId.value ? parseInt(npForm.elements.parentId.value, 10) : null,
                    Title: npForm.elements.title.value,
                    Template: npTemplate
                }
            }).then(function (data) {
                location.href = data.Url;
            }).catch(function (err) {
                submit.disabled = false;
                toast(err.message, true);
            });
        });
    }

    // ---------------------------------------------------------------- разделы (руководители)
    var sectionDialog = document.getElementById('wkSectionDialog');
    if (sectionDialog) {
        var sForm = sectionDialog.querySelector('form');
        var emojiButtons = sectionDialog.querySelectorAll('[data-emoji]');
        function markEmoji(value) {
            emojiButtons.forEach(function (b) { b.classList.toggle('is-on', b.getAttribute('data-emoji') === value); });
        }
        emojiButtons.forEach(function (b) {
            b.addEventListener('click', function () {
                sForm.elements.icon.value = b.getAttribute('data-emoji');
                markEmoji(sForm.elements.icon.value);
            });
        });
        sForm.elements.icon.addEventListener('input', function () { markEmoji(sForm.elements.icon.value); });

        document.addEventListener('click', function (e) {
            var create = e.target.closest('[data-wiki-new-section]');
            var edit = e.target.closest('[data-wiki-edit-section]');
            if (!create && !edit) return;
            e.preventDefault();
            sForm.reset();
            sectionDialog.querySelector('h2').textContent = edit ? 'Изменить раздел' : 'Новый раздел';
            sForm.elements.id.value = edit ? edit.getAttribute('data-id') : '';
            sForm.elements.name.value = edit ? edit.getAttribute('data-name') : '';
            sForm.elements.icon.value = edit ? (edit.getAttribute('data-icon') || '') : '📘';
            sForm.elements.description.value = edit ? (edit.getAttribute('data-description') || '') : '';
            markEmoji(sForm.elements.icon.value);
            openDialog('wkSectionDialog');
            setTimeout(function () { sForm.elements.name.focus(); }, 30);
        });

        sForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var id = sForm.elements.id.value;
            var body = { Name: sForm.elements.name.value, Icon: sForm.elements.icon.value, Description: sForm.elements.description.value };
            api(id ? '/Wiki/api/sections/' + id : '/Wiki/api/sections', { body: body })
                .then(function (data) { location.href = id ? location.href : data.Url; })
                .catch(function (err) { toast(err.message, true); });
        });
    }

    document.addEventListener('click', function (e) {
        var del = e.target.closest('[data-wiki-delete-section]');
        if (del) {
            if (!confirm('Удалить раздел «' + del.getAttribute('data-name') + '»? Удалить можно только пустой раздел.')) return;
            api('/Wiki/api/sections/' + del.getAttribute('data-id') + '/delete', { body: {} })
                .then(function () { location.href = '/Wiki'; })
                .catch(function (err) { toast(err.message, true); });
            return;
        }
        var shift = e.target.closest('[data-wiki-shift-section]');
        if (shift) {
            api('/Wiki/api/sections/' + shift.getAttribute('data-id') + '/shift', { body: { Delta: parseInt(shift.getAttribute('data-wiki-shift-section'), 10) } })
                .then(function () { location.reload(); })
                .catch(function (err) { toast(err.message, true); });
        }
    });

    // ---------------------------------------------------------------- доступ и редакторы раздела
    var accessDialog = document.getElementById('wkAccessDialog');
    if (accessDialog) {
        var aForm = accessDialog.querySelector('form');

        function renderPeople(container, members, checkedIds, name, lockManagers) {
            container.innerHTML = members.map(function (m) {
                var checked = checkedIds.indexOf(m.UserId) >= 0 || (lockManagers && m.IsManager);
                return '<label class="wk-person"><input type="checkbox" name="' + name + '" value="' + m.UserId + '"' +
                    (checked ? ' checked' : '') + (lockManagers && m.IsManager ? ' disabled' : '') + '>' +
                    '<span class="wk-person-body"><span class="wk-person-name">' + esc(m.Name) + '</span>' +
                    '<span class="wk-person-mail">' + esc(m.Email || '') + '</span></span>' +
                    (m.IsManager ? '<span class="wk-chip wk-chip-acc">Руководитель</span>' : '') + '</label>';
            }).join('') || '<div class="wk-empty-mini" style="padding:8px">В компании пока нет сотрудников.</div>';
        }

        function syncAccessMode() {
            var selected = aForm.querySelector('input[name=accessMode]:checked').value === 'Selected';
            aForm.querySelector('[data-access-people]').hidden = !selected;
        }
        aForm.querySelectorAll('input[name=accessMode]').forEach(function (r) { r.addEventListener('change', syncAccessMode); });

        document.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-wiki-access]');
            if (!btn) return;
            e.preventDefault();
            var id = btn.getAttribute('data-id');
            api('/Wiki/api/sections/' + id + '/settings').then(function (data) {
                aForm.elements.id.value = id;
                accessDialog.querySelector('h2').textContent = 'Доступ: ' + data.Name;
                aForm.querySelector('input[name=accessMode][value="' + (data.AccessMode === 'Selected' ? 'Selected' : 'All') + '"]').checked = true;
                renderPeople(aForm.querySelector('[data-people=access]'), data.Members, data.AccessUserIds, 'access', true);
                renderPeople(aForm.querySelector('[data-people=editors]'), data.Members, data.EditorUserIds, 'editor', true);
                syncAccessMode();
                openDialog('wkAccessDialog');
            }).catch(function (err) { toast(err.message, true); });
        });

        aForm.addEventListener('submit', function (e) {
            e.preventDefault();
            function ids(name) {
                return Array.prototype.slice.call(aForm.querySelectorAll('input[name=' + name + ']:checked:not(:disabled)'))
                    .map(function (i) { return parseInt(i.value, 10); });
            }
            api('/Wiki/api/sections/' + aForm.elements.id.value + '/settings', {
                body: {
                    AccessMode: aForm.querySelector('input[name=accessMode]:checked').value,
                    AccessUserIds: ids('access'),
                    EditorUserIds: ids('editor')
                }
            }).then(function () {
                accessDialog.close();
                toast('Доступ сохранён');
                setTimeout(function () { location.reload(); }, 600);
            }).catch(function (err) { toast(err.message, true); });
        });
    }

    // ---------------------------------------------------------------- редакторы всей базы
    var editorsForm = document.querySelector('[data-wiki-editors-form]');
    if (editorsForm) {
        editorsForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var ids = Array.prototype.slice.call(editorsForm.querySelectorAll('input[name=userId]:checked:not(:disabled)'))
                .map(function (i) { return parseInt(i.value, 10); });
            api('/Wiki/api/editors', { body: { UserIds: ids } })
                .then(function () { toast('Редакторы сохранены'); })
                .catch(function (err) { toast(err.message, true); });
        });
    }

    // ---------------------------------------------------------------- выбор страницы (общий)
    var picker = document.getElementById('wkPickerDialog');
    var pickerCallback = null;
    var pickerTimer = null;
    var pickerItems = [];
    var pickerActive = 0;
    function renderPicker() {
        var box = picker.querySelector('[data-picker-results]');
        if (!pickerItems.length) {
            box.innerHTML = '<div class="wk-empty-mini" style="padding:10px">Ничего не найдено</div>';
            return;
        }
        box.innerHTML = pickerItems.map(function (p, i) {
            return '<button type="button" class="wk-picker-item' + (i === pickerActive ? ' is-active' : '') + '" role="option" data-index="' + i + '">' +
                '<span class="wk-list-ico">' + esc(p.SectionIcon) + '</span><span><b>' + esc(p.Title) + '</b><small>' + esc(p.SectionName) + '</small></span></button>';
        }).join('');
    }
    function loadPicker(q) {
        api('/Wiki/api/lookup?q=' + encodeURIComponent(q || '')).then(function (items) {
            pickerItems = items || [];
            pickerActive = 0;
            renderPicker();
        }).catch(function () { });
    }
    function choose(index) {
        var item = pickerItems[index];
        if (!item) return;
        picker.close();
        if (pickerCallback) pickerCallback(item);
    }
    if (picker) {
        var pInput = picker.querySelector('[data-picker-input]');
        pInput.addEventListener('input', function () {
            clearTimeout(pickerTimer);
            pickerTimer = setTimeout(function () { loadPicker(pInput.value.trim()); }, 200);
        });
        pInput.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown') { pickerActive = Math.min(pickerActive + 1, pickerItems.length - 1); renderPicker(); e.preventDefault(); }
            else if (e.key === 'ArrowUp') { pickerActive = Math.max(pickerActive - 1, 0); renderPicker(); e.preventDefault(); }
            else if (e.key === 'Enter') { e.preventDefault(); choose(pickerActive); }
        });
        picker.querySelector('[data-picker-results]').addEventListener('click', function (e) {
            var b = e.target.closest('[data-index]');
            if (b) choose(parseInt(b.getAttribute('data-index'), 10));
        });
    }
    window.WikiPicker = {
        open: function (title, callback) {
            if (!picker) return;
            pickerCallback = callback;
            picker.querySelector('h2').textContent = title || 'Выберите страницу';
            var input = picker.querySelector('[data-picker-input]');
            input.value = '';
            loadPicker('');
            picker.showModal();
            setTimeout(function () { input.focus(); }, 30);
        }
    };

    // ---------------------------------------------------------------- дерево страниц
    document.querySelectorAll('[data-wiki-tree]').forEach(function (tree) {
        tree.addEventListener('click', function (e) {
            var t = e.target.closest('.wk-tree-toggle');
            if (!t || t.classList.contains('is-leaf')) return;
            e.preventDefault();
            t.closest('li').classList.toggle('is-collapsed');
        });

        if (tree.getAttribute('data-can-edit') !== 'true') return;

        // Перетаскивание: верхняя четверть строки — «перед», нижняя — «после», середина — «внутрь»
        var dragLi = null;
        var dropInfo = null;

        function clearMarks() {
            tree.querySelectorAll('.is-drop-before, .is-drop-after, .is-drop-inside').forEach(function (el) {
                el.classList.remove('is-drop-before', 'is-drop-after', 'is-drop-inside');
            });
        }

        function subtreeHeight(li) {
            var max = 0;
            li.querySelectorAll(':scope > ul > li').forEach(function (c) { max = Math.max(max, subtreeHeight(c)); });
            return 1 + max;
        }

        tree.addEventListener('dragstart', function (e) {
            var row = e.target.closest('.wk-tree-row');
            if (!row) return;
            dragLi = row.parentElement;
            row.classList.add('is-dragging');
            e.dataTransfer.effectAllowed = 'move';
            try { e.dataTransfer.setData('text/plain', dragLi.getAttribute('data-page-id')); } catch (err) { }
        });
        tree.addEventListener('dragend', function () {
            if (dragLi) dragLi.querySelector('.wk-tree-row').classList.remove('is-dragging');
            dragLi = null;
            clearMarks();
        });
        tree.addEventListener('dragover', function (e) {
            if (!dragLi) return;
            var row = e.target.closest('.wk-tree-row');
            if (!row) return;
            var li = row.parentElement;
            if (li === dragLi || dragLi.contains(li)) { clearMarks(); dropInfo = null; return; }
            e.preventDefault();
            var rect = row.getBoundingClientRect();
            var y = (e.clientY - rect.top) / rect.height;
            var depth = parseInt(li.getAttribute('data-depth'), 10);
            var height = subtreeHeight(dragLi);
            var mode = y < 0.28 ? 'before' : y > 0.72 ? 'after' : 'inside';
            if (mode === 'inside' && depth + height > 3) mode = y < 0.5 ? 'before' : 'after';
            if (mode !== 'inside' && depth - 1 + height > 3) { clearMarks(); dropInfo = null; return; }
            clearMarks();
            row.classList.add('is-drop-' + mode);
            dropInfo = { li: li, mode: mode };
        });
        tree.addEventListener('dragleave', function (e) {
            if (!tree.contains(e.relatedTarget)) clearMarks();
        });
        tree.addEventListener('drop', function (e) {
            if (!dragLi || !dropInfo) return;
            e.preventDefault();
            var target = dropInfo.li;
            var pageId = dragLi.getAttribute('data-page-id');
            var parentId, index;
            if (dropInfo.mode === 'inside') {
                parentId = target.getAttribute('data-page-id');
                index = 100000; // в конец
            } else {
                var parentLi = target.parentElement.closest('li');
                parentId = parentLi ? parentLi.getAttribute('data-page-id') : null;
                var siblings = Array.prototype.slice.call(target.parentElement.children).filter(function (x) { return x !== dragLi; });
                index = siblings.indexOf(target) + (dropInfo.mode === 'after' ? 1 : 0);
            }
            clearMarks();
            api('/Wiki/api/pages/' + pageId + '/move', { body: { ParentId: parentId ? parseInt(parentId, 10) : null, Index: index } })
                .then(function () { location.reload(); })
                .catch(function (err) { toast(err.message, true); });
        });
    });

    // ---------------------------------------------------------------- страница
    var article = document.querySelector('[data-wiki-page]');
    if (article) {
        var pageId = article.getAttribute('data-wiki-page');
        var canEdit = article.getAttribute('data-can-edit') === 'true';

        // Широкие таблицы — с горизонтальной прокруткой
        article.querySelectorAll('[data-wiki-content] table').forEach(function (t) {
            if (t.parentElement.classList.contains('wk-table-wrap')) return;
            var wrap = document.createElement('div');
            wrap.className = 'wk-table-wrap';
            t.parentNode.insertBefore(wrap, t);
            wrap.appendChild(t);
        });

        var fav = article.querySelector('[data-wiki-favorite]');
        if (fav) fav.addEventListener('click', function () {
            api('/Wiki/api/pages/' + pageId + '/favorite', { body: {} }).then(function (data) {
                fav.classList.toggle('is-on', data.Favorite);
                fav.setAttribute('aria-pressed', data.Favorite ? 'true' : 'false');
                fav.title = data.Favorite ? 'Убрать из избранного' : 'В избранное';
                fav.querySelector('i').className = (data.Favorite ? 'fa-solid' : 'fa-regular') + ' fa-star';
                toast(data.Favorite ? 'Добавлено в избранное' : 'Убрано из избранного');
            }).catch(function (err) { toast(err.message, true); });
        });

        document.addEventListener('click', function (e) {
            if (e.target.closest('[data-wiki-print]')) { window.print(); return; }
            if (e.target.closest('[data-wiki-copy-link]')) {
                var url = location.origin + '/Wiki/Page/' + pageId;
                (navigator.clipboard ? navigator.clipboard.writeText(url) : Promise.reject())
                    .then(function () { toast('Ссылка скопирована'); })
                    .catch(function () { prompt('Ссылка на страницу:', url); });
                return;
            }
            var pin = e.target.closest('[data-wiki-pin]');
            if (pin) {
                api('/Wiki/api/pages/' + pageId + '/pin', { body: { Pinned: pin.getAttribute('data-wiki-pin') === 'true' } })
                    .then(function () { location.reload(); })
                    .catch(function (err) { toast(err.message, true); });
                return;
            }
            var shift = e.target.closest('[data-wiki-shift]');
            if (shift) {
                api('/Wiki/api/pages/' + pageId + '/shift', { body: { Delta: parseInt(shift.getAttribute('data-wiki-shift'), 10) } })
                    .then(function () { location.reload(); })
                    .catch(function (err) { toast(err.message, true); });
                return;
            }
            if (e.target.closest('[data-wiki-move]')) { openDialog('wkMoveDialog'); return; }
            if (e.target.closest('[data-wiki-delete-page]')) {
                if (!confirm('Удалить страницу «' + article.getAttribute('data-title') + '» вместе с подстраницами?')) return;
                api('/Wiki/api/pages/' + pageId + '/delete', { body: {} })
                    .then(function (data) { location.href = data.Url; })
                    .catch(function (err) { toast(err.message, true); });
                return;
            }
            if (e.target.closest('[data-wiki-review-settings]')) { openReviewDialog(); }
        });

        var moveForm = document.querySelector('[data-wiki-move-form]');
        if (moveForm) moveForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var v = moveForm.elements.parentId.value;
            api('/Wiki/api/pages/' + pageId + '/move', { body: { ParentId: v ? parseInt(v, 10) : null, Index: 100000 } })
                .then(function () { location.reload(); })
                .catch(function (err) { toast(err.message, true); });
        });

        var reviewForm = document.querySelector('[data-wiki-review-form]');
        function openReviewDialog() {
            var select = reviewForm.elements.ownerUserId;
            var selected = select.getAttribute('data-selected');
            api('/Wiki/api/members').then(function (members) {
                select.innerHTML = '<option value="">— не назначен —</option>' + members.map(function (m) {
                    return '<option value="' + m.UserId + '"' + (String(m.UserId) === selected ? ' selected' : '') + '>' + esc(m.Name) + '</option>';
                }).join('');
                openDialog('wkReviewDialog');
            }).catch(function (err) { toast(err.message, true); });
        }
        if (reviewForm) reviewForm.addEventListener('submit', function (e) {
            e.preventDefault();
            api('/Wiki/api/pages/' + pageId + '/review', {
                body: {
                    OwnerUserId: reviewForm.elements.ownerUserId.value ? parseInt(reviewForm.elements.ownerUserId.value, 10) : null,
                    ReviewDueDate: reviewForm.elements.reviewDueDate.value || null
                }
            }).then(function () { location.reload(); }).catch(function (err) { toast(err.message, true); });
        });

        // ----- вложения
        var filesBox = article.querySelector('[data-wiki-files]');
        var countChip = article.querySelector('[data-attachments-count]');

        function fileIcon(a) {
            if (a.Preview === 'image') return '<img src="' + esc(a.Url) + '" alt="" loading="lazy">';
            if (a.Preview === 'pdf') return '<i class="fa-regular fa-file-pdf"></i>';
            var ext = (a.FileName.split('.').pop() || '').toLowerCase();
            var map = { doc: 'word', docx: 'word', xls: 'excel', xlsx: 'excel', csv: 'excel', ppt: 'powerpoint', pptx: 'powerpoint', zip: 'zipper', rar: 'zipper', '7z': 'zipper', mp4: 'video', mov: 'video', mp3: 'audio', txt: 'lines' };
            return '<i class="fa-regular fa-file' + (map[ext] ? '-' + map[ext] : '') + '"></i>';
        }

        function loadAttachments() {
            return api('/Wiki/api/pages/' + pageId + '/attachments').then(function (list) {
                countChip.hidden = !list.length;
                countChip.textContent = list.length;
                if (!list.length) {
                    filesBox.innerHTML = '<div class="wk-empty-mini">' + (canEdit ? 'Файлов пока нет.' : 'К странице не прикреплено файлов.') + '</div>';
                    return;
                }
                filesBox.innerHTML = list.map(function (a) {
                    var preview = a.Preview ? ' data-preview="' + a.Preview + '" data-url="' + esc(a.Url) + '" data-download="' + esc(a.DownloadUrl) + '" data-name="' + esc(a.FileName) + '"' : '';
                    var hist = a.History && a.History.length
                        ? '<details class="wk-file-history"><summary>Предыдущие версии (' + a.History.length + ')</summary>' + a.History.map(function (h) {
                            return '<div>v' + h.VersionNumber + ' · <a href="' + esc(h.Url) + '">' + esc(h.FileName) + '</a> · ' + esc(h.Size) + ' · ' + esc(h.Uploaded) + (h.UploadedByName ? ' · ' + esc(h.UploadedByName) : '') + '</div>';
                        }).join('') + '</details>'
                        : '';
                    return '<div class="wk-file" data-attachment-id="' + a.Id + '">' +
                        '<button type="button" class="wk-file-thumb' + (a.Preview === 'pdf' ? ' is-pdf' : '') + '"' + preview + (a.Preview ? '' : ' data-open="' + esc(a.DownloadUrl) + '"') + ' aria-label="Открыть ' + esc(a.FileName) + '">' + fileIcon(a) + '</button>' +
                        '<div class="wk-file-body"><a class="wk-file-name" href="' + esc(a.Preview ? a.Url : a.DownloadUrl) + '"' + preview + ' title="' + esc(a.FileName) + '">' + esc(a.FileName) + '</a>' +
                        '<span class="wk-file-meta">' + esc(a.Size) + ' · ' + esc(a.Uploaded) + (a.VersionNumber > 1 ? ' · v' + a.VersionNumber : '') + (a.UploadedByName ? ' · ' + esc(a.UploadedByName) : '') + '</span></div>' +
                        '<div class="wk-file-actions">' +
                        '<a class="wk-icon-btn" href="' + esc(a.DownloadUrl) + '" title="Скачать" aria-label="Скачать"><i class="fa-solid fa-download"></i></a>' +
                        (canEdit ? '<button type="button" class="wk-icon-btn" data-replace title="Заменить новой версией" aria-label="Заменить"><i class="fa-solid fa-arrow-rotate-right"></i></button>' +
                            '<button type="button" class="wk-icon-btn" data-delete title="Удалить" aria-label="Удалить"><i class="fa-regular fa-trash-can"></i></button>' : '') +
                        '</div></div>' + hist;
                }).join('');
                // Файл с расширением картинки, но не картинка: сервер отдаёт его только на скачивание
                filesBox.querySelectorAll('.wk-file-thumb img').forEach(function (img) {
                    img.addEventListener('error', function () {
                        var card = img.closest('.wk-file');
                        card.querySelectorAll('[data-preview]').forEach(function (el) {
                            el.removeAttribute('data-preview');
                            if (el.classList.contains('wk-file-thumb')) el.setAttribute('data-open', el.getAttribute('data-download'));
                        });
                        img.replaceWith(Object.assign(document.createElement('i'), { className: 'fa-regular fa-file' }));
                    });
                });
            }).catch(function () {
                filesBox.innerHTML = '<div class="wk-empty-mini">Не удалось загрузить список файлов.</div>';
            });
        }

        filesBox.addEventListener('click', function (e) {
            var prev = e.target.closest('[data-preview]');
            if (prev) {
                e.preventDefault();
                openViewer(prev.getAttribute('data-preview'), prev.getAttribute('data-url'), prev.getAttribute('data-download'), prev.getAttribute('data-name'));
                return;
            }
            var open = e.target.closest('[data-open]');
            if (open) { location.href = open.getAttribute('data-open'); return; }
            var card = e.target.closest('[data-attachment-id]');
            if (!card) return;
            var id = card.getAttribute('data-attachment-id');
            if (e.target.closest('[data-delete]')) {
                if (!confirm('Удалить файл «' + card.querySelector('.wk-file-name').textContent + '»?')) return;
                api('/Wiki/api/attachments/' + id + '/delete', { body: {} })
                    .then(function () { toast('Файл удалён'); loadAttachments(); })
                    .catch(function (err) { toast(err.message, true); });
            } else if (e.target.closest('[data-replace]')) {
                var input = document.createElement('input');
                input.type = 'file';
                input.onchange = function () {
                    if (input.files[0]) upload(input.files[0], '/Wiki/api/attachments/' + id + '/replace');
                };
                input.click();
            }
        });

        function openViewer(kind, url, download, name) {
            var dlg = document.getElementById('wkViewerDialog');
            dlg.querySelector('h2').textContent = name;
            dlg.querySelector('[data-viewer-download]').href = download;
            var body = dlg.querySelector('[data-viewer-body]');
            body.innerHTML = kind === 'pdf'
                ? '<iframe src="' + esc(url) + '" title="' + esc(name) + '"></iframe>'
                : '<img src="' + esc(url) + '" alt="' + esc(name) + '">';
            dlg.showModal();
            dlg.addEventListener('close', function () { body.innerHTML = ''; }, { once: true });
        }

        // Картинки в тексте открываются в просмотрщике
        article.querySelectorAll('[data-wiki-content] img').forEach(function (img) {
            img.style.cursor = 'zoom-in';
            img.addEventListener('click', function () {
                openViewer('image', img.getAttribute('src'), img.getAttribute('src') + '?download=true', img.getAttribute('alt') || 'Изображение');
            });
        });

        var uploadsBox = article.querySelector('[data-wiki-uploads]');
        var maxBytes = 50 * 1024 * 1024;

        function upload(file, url) {
            var row = document.createElement('div');
            row.className = 'wk-upload';
            row.innerHTML = '<i class="fa-solid fa-arrow-up-from-bracket"></i><span style="min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:45%">' + esc(file.name) + '</span><progress max="100" value="0"></progress>';
            uploadsBox.appendChild(row);
            if (file.size > maxBytes) {
                row.classList.add('is-error');
                row.querySelector('progress').replaceWith(document.createTextNode('больше 50 МБ — не загружен'));
                setTimeout(function () { row.remove(); }, 6000);
                return Promise.resolve();
            }
            return new Promise(function (resolve) {
                var xhr = new XMLHttpRequest();
                xhr.open('POST', url || ('/Wiki/api/pages/' + pageId + '/attachments'));
                xhr.setRequestHeader('RequestVerificationToken', csrf());
                xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
                xhr.upload.onprogress = function (ev) {
                    if (ev.lengthComputable) row.querySelector('progress').value = Math.round(ev.loaded * 100 / ev.total);
                };
                xhr.onload = function () {
                    if (xhr.status >= 200 && xhr.status < 300) {
                        row.remove();
                    } else {
                        var msg = httpMessage(xhr.status);
                        try { msg = JSON.parse(xhr.responseText).message || msg; } catch (err) { }
                        row.classList.add('is-error');
                        row.querySelector('progress').replaceWith(document.createTextNode(msg));
                        setTimeout(function () { row.remove(); }, 6000);
                    }
                    resolve();
                };
                xhr.onerror = function () {
                    row.classList.add('is-error');
                    row.querySelector('progress').replaceWith(document.createTextNode('ошибка сети'));
                    resolve();
                };
                var fd = new FormData();
                fd.append('file', file);
                xhr.send(fd);
            });
        }

        function uploadMany(files) {
            var list = Array.prototype.slice.call(files || []);
            if (!list.length) return;
            // Не больше 3 загрузок одновременно
            var queue = list.slice();
            function next() {
                var f = queue.shift();
                return f ? upload(f).then(next) : Promise.resolve();
            }
            Promise.all([next(), next(), next()]).then(loadAttachments);
        }

        var drop = article.querySelector('[data-wiki-drop]');
        if (drop) {
            var fileInput = drop.querySelector('[data-wiki-file-input]');
            drop.addEventListener('click', function () { fileInput.click(); });
            drop.addEventListener('keydown', function (e) { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); fileInput.click(); } });
            fileInput.addEventListener('change', function () { uploadMany(fileInput.files); fileInput.value = ''; });
            ['dragenter', 'dragover'].forEach(function (t) {
                drop.addEventListener(t, function (e) { e.preventDefault(); drop.classList.add('is-over'); });
            });
            ['dragleave', 'drop'].forEach(function (t) {
                drop.addEventListener(t, function () { drop.classList.remove('is-over'); });
            });
            drop.addEventListener('drop', function (e) {
                e.preventDefault();
                uploadMany(e.dataTransfer.files);
            });
        }

        loadAttachments();

        // ----- связанные задачи
        var taskBox = article.querySelector('[data-wiki-task-list]');
        api('/Wiki/api/pages/' + pageId + '/tasks').then(function (data) {
            var items = data.Tasks || [];
            var html = items.map(function (t) {
                return '<a class="wk-task" href="' + esc(t.Url) + '"><i class="fa-solid fa-list-check"></i>' +
                    '<span class="wk-task-name">' + esc(t.Name) + '</span>' +
                    (t.StatusName ? '<span class="wk-chip">' + esc(t.StatusName) + '</span>' : '') +
                    '<span class="wk-task-meta">' + esc([t.AssigneeName, t.Deadline ? 'до ' + t.Deadline : null].filter(Boolean).join(' · ')) + '</span></a>';
            }).join('');
            if (data.Hidden > 0) html += '<div class="wk-empty-mini">И ещё ' + data.Hidden + ' задач(и), к которым у вас нет доступа.</div>';
            taskBox.innerHTML = html || '<div class="wk-empty-mini">Страница пока не привязана ни к одной задаче. Привязать можно в карточке задачи.</div>';
        }).catch(function () { taskBox.innerHTML = '<div class="wk-empty-mini">Не удалось загрузить задачи.</div>'; });
    }

    // ---------------------------------------------------------------- «Проверено» (страница и список)
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-wiki-reviewed]');
        if (!btn) return;
        var id = btn.getAttribute('data-page-id') || (article && article.getAttribute('data-wiki-page'));
        btn.disabled = true;
        api('/Wiki/api/pages/' + id + '/reviewed', { body: {} })
            .then(function () { toast('Отмечено как актуальное — следующая проверка через 6 месяцев'); setTimeout(function () { location.reload(); }, 900); })
            .catch(function (err) { btn.disabled = false; toast(err.message, true); });
    });

    // ---------------------------------------------------------------- история: восстановление
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-wiki-restore]');
        if (!btn) return;
        var v = btn.getAttribute('data-wiki-restore');
        if (!confirm('Восстановить версию ' + v + '? Текущий текст сохранится в истории.')) return;
        api('/Wiki/api/pages/' + btn.getAttribute('data-page-id') + '/restore/' + v, { body: {} })
            .then(function (data) { location.href = data.Url; })
            .catch(function (err) { toast(err.message, true); });
    });
})();
