/* =====================================================================
   Связь задач со статьями базы знаний (подключается на странице /Tasks).
   - блок «Статьи базы знаний» в карточке задачи: список, привязка через поиск, отвязка;
   - /Tasks?wikiPage=ID — открыть создание задачи, заполненной по странице (ссылка + чек-лист);
   - /Tasks?openTask=ID — открыть карточку задачи.
   ===================================================================== */
(function () {
    'use strict';

    function csrf() {
        var meta = document.querySelector('meta[name="wiki-csrf"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function api(url, body) {
        var opts = { credentials: 'same-origin', headers: { 'Accept': 'application/json', 'X-Requested-With': 'XMLHttpRequest' } };
        if (body !== undefined) {
            opts.method = 'POST';
            opts.headers['Content-Type'] = 'application/json';
            opts.headers['RequestVerificationToken'] = csrf();
            opts.body = JSON.stringify(body);
        }
        return fetch(url, opts).then(function (r) {
            if (!r.ok) throw new Error(r.status === 404 ? 'Нет доступа к задаче или статье' : 'Ошибка ' + r.status);
            return r.json();
        });
    }

    function esc(t) {
        return String(t == null ? '' : t).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function tileHtml(t) {
        t = t || {};
        return '<span class="wk-tile wk-tile-sm" style="--tile-fg:' + esc(t.Fg || '#4f46e5') + ';--tile-bg:' + esc(t.Bg || '#eef0ff') + '" aria-hidden="true"><i class="' + esc(t.Css || 'fa-solid fa-book') + '"></i></span>';
    }

    var draft = null; // заготовка задачи по странице (?wikiPage=)

    function render(panel, taskId) {
        var list = panel.querySelector('[data-wtl-list]');
        if (!taskId) {
            list.innerHTML = draft
                ? '<a class="wk-task" href="/Wiki/Page/' + draft.PageId + '" target="_blank" rel="noopener"><i class="fa-solid fa-book"></i><span class="wk-task-name">' + esc(draft.Name) + '</span><span class="wk-task-meta">будет привязана</span></a>'
                : '<div class="wk-empty-mini">Статьи можно привязать после создания задачи.</div>';
            panel.querySelector('[data-wtl-add]').hidden = true;
            return;
        }
        panel.querySelector('[data-wtl-add]').hidden = false;
        api('/Wiki/api/tasks/' + taskId + '/pages').then(function (pages) {
            list.innerHTML = pages.length ? pages.map(function (p) {
                return '<div class="wk-task" style="padding:6px 6px 6px 8px">' + tileHtml(p.Tile) +
                    '<a class="wk-task-name" href="' + esc(p.Url) + '" target="_blank" rel="noopener" style="text-decoration:none">' + esc(p.Title) + '</a>' +
                    '<span class="wk-task-meta">' + esc(p.SectionName) + '</span>' +
                    '<button type="button" class="wk-icon-btn" data-wtl-unlink="' + p.Id + '" title="Отвязать" aria-label="Отвязать статью"><i class="fa-solid fa-xmark"></i></button></div>';
            }).join('') : '<div class="wk-empty-mini">Статьи базы знаний не привязаны.</div>';
        }).catch(function () {
            list.innerHTML = '<div class="wk-empty-mini">Не удалось загрузить статьи.</div>';
        });
    }

    function buildPanel(taskId) {
        var panel = document.createElement('section');
        panel.className = 'wk-task-links';
        panel.innerHTML =
            '<div class="wk-block-head" style="margin:0 0 8px"><h2 style="font-size:15px"><i class="fa-solid fa-book" style="color:var(--ui-acc)"></i>Статьи базы знаний</h2>' +
            '<button type="button" class="wk-btn wk-btn-sm" data-wtl-add><i class="fa-solid fa-link"></i>Привязать статью</button></div>' +
            '<div class="wk-tasks" data-wtl-list><div class="wk-empty-mini">Загрузка…</div></div>' +
            '<div data-wtl-picker hidden style="margin-top:8px">' +
            '<input class="wk-input" type="search" placeholder="Найти статью по названию…" autocomplete="off" data-wtl-q>' +
            '<div class="wk-picker-results" data-wtl-results style="margin-top:6px"></div></div>';

        var picker = panel.querySelector('[data-wtl-picker]');
        var input = panel.querySelector('[data-wtl-q]');
        var results = panel.querySelector('[data-wtl-results]');
        var timer = null;

        function search() {
            api('/Wiki/api/lookup?q=' + encodeURIComponent(input.value.trim())).then(function (items) {
                results.innerHTML = items.length ? items.map(function (p) {
                    return '<button type="button" class="wk-picker-item" data-wtl-pick="' + p.Id + '">' + tileHtml(p.Tile) +
                        '<span><b>' + esc(p.Title) + '</b><small>' + esc(p.SectionName) + '</small></span></button>';
                }).join('') : '<div class="wk-empty-mini">Ничего не найдено</div>';
            }).catch(function () { results.innerHTML = ''; });
        }

        panel.addEventListener('click', function (e) {
            if (e.target.closest('[data-wtl-add]')) {
                picker.hidden = !picker.hidden;
                if (!picker.hidden) { input.value = ''; search(); input.focus(); }
                return;
            }
            var pick = e.target.closest('[data-wtl-pick]');
            if (pick) {
                api('/Wiki/api/tasks/' + taskId + '/link', { PageId: parseInt(pick.getAttribute('data-wtl-pick'), 10) })
                    .then(function () { picker.hidden = true; render(panel, taskId); })
                    .catch(function (err) { alert(err.message); });
                return;
            }
            var unlink = e.target.closest('[data-wtl-unlink]');
            if (unlink) {
                api('/Wiki/api/tasks/' + taskId + '/unlink', { PageId: parseInt(unlink.getAttribute('data-wtl-unlink'), 10) })
                    .then(function () { render(panel, taskId); })
                    .catch(function (err) { alert(err.message); });
            }
        });
        input.addEventListener('input', function () { clearTimeout(timer); timer = setTimeout(search, 200); });
        input.addEventListener('keydown', function (e) { if (e.key === 'Enter') e.preventDefault(); });

        render(panel, taskId);
        return panel;
    }

    window.WikiTaskLinks = {
        /** Вставить блок статей после формы задачи в попапе. */
        attach: function ($form, form) {
            var $content = $form.parent();
            $content.find('.wk-task-links').remove();
            // Ключ редактируемой строки берём у грида: у новой строки он не число
            var id = null;
            var el = document.getElementById('tasksGrid');
            var grid = el && DevExpress.ui.dxDataGrid.getInstance(el);
            var key = grid ? grid.option('editing.editRowKey') : null;
            if (typeof key === 'number') id = key;
            else if (form && form.option('formData') && typeof form.option('formData').Id === 'number') id = form.option('formData').Id;
            var panel = buildPanel(id);
            $form.after(panel);
        },
        /** Применить заготовку «Создать задачу по странице» к новой строке грида. */
        applyDraft: function (rowData) {
            if (!draft) return;
            rowData.Name = draft.Name;
            rowData.Description = draft.Description;
            rowData.WikiPageId = draft.PageId;
        },
        clearDraft: function () { draft = null; }
    };

    // ----- параметры адресной строки
    var params = new URLSearchParams(location.search);
    var wikiPage = parseInt(params.get('wikiPage'), 10);
    var openTask = parseInt(params.get('openTask'), 10);

    function withGrid(cb) {
        var tries = 0;
        (function wait() {
            var el = document.getElementById('tasksGrid');
            var grid = el && window.DevExpress && DevExpress.ui && DevExpress.ui.dxDataGrid
                ? DevExpress.ui.dxDataGrid.getInstance(el) : null;
            if (grid) return cb(grid);
            if (tries++ < 50) setTimeout(wait, 100);
        })();
    }

    if (wikiPage > 0) {
        api('/Wiki/api/pages/' + wikiPage + '/task-draft').then(function (d) {
            draft = d;
            withGrid(function (grid) {
                history.replaceState(null, '', location.pathname);
                grid.addRow();
            });
        }).catch(function (err) { alert('Не удалось подготовить задачу по статье: ' + err.message); });
    }

    if (openTask > 0) {
        withGrid(function (grid) {
            history.replaceState(null, '', location.pathname);
            var go = function () {
                var p = grid.navigateToRow ? grid.navigateToRow(openTask) : null;
                var edit = function () {
                    var idx = grid.getRowIndexByKey(openTask);
                    if (idx && typeof idx.then === 'function') idx.then(function (i) { if (i > -1) grid.editRow(i); });
                    else if (idx > -1) grid.editRow(idx);
                };
                if (p && typeof p.then === 'function') p.then(edit); else edit();
            };
            var ds = grid.getDataSource();
            if (ds && ds.isLoaded()) go(); else grid.on('contentReady', function once() { grid.off('contentReady', once); go(); });
        });
    }
})();
