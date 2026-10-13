/* =====================================================================
   Редактор страницы базы знаний (Tiptap, локальная сборка wwwroot/lib/tiptap).
   Сохраняет HTML (сервер его очищает санитайзером) + JSON документа.
   ===================================================================== */
import {
    Editor, Node, Extension, mergeAttributes,
    StarterKit, Underline, Link, Image, Table, TableRow, TableHeader, TableCell,
    TaskList, TaskItem, Placeholder, Suggestion, PluginKey
} from '../../lib/tiptap/tiptap.bundle.js';

const host = document.querySelector('[data-wiki-editor]');
if (host) init(host);

function init(host) {
    const W = window.WikiApi;
    const pageId = host.getAttribute('data-page-id');
    let baseVersion = parseInt(host.getAttribute('data-version'), 10);
    const titleInput = host.querySelector('[data-editor-title]');
    const statusEl = host.querySelector('[data-editor-status]');
    const saveBtn = host.querySelector('[data-editor-save]');
    const imageInput = host.querySelector('[data-editor-image-input]');
    const initialHtml = host.querySelector('[data-editor-initial]').innerHTML;
    const draftKey = 'wiki-draft-' + pageId;

    // ------------------------------------------------------------ узлы
    const Callout = Node.create({
        name: 'callout',
        group: 'block',
        content: 'block+',
        defining: true,
        addAttributes() {
            return {
                kind: {
                    default: 'info',
                    parseHTML: el => {
                        const k = el.getAttribute('data-callout');
                        return ['info', 'warning', 'tip'].includes(k) ? k : 'info';
                    },
                    renderHTML: attrs => ({ 'data-callout': attrs.kind }),
                },
            };
        },
        parseHTML() { return [{ tag: 'div[data-callout]' }]; },
        renderHTML({ HTMLAttributes }) { return ['div', mergeAttributes(HTMLAttributes), 0]; },
        addCommands() {
            return {
                toggleCallout: kind => ({ editor, commands }) => {
                    if (editor.isActive('callout', { kind })) return commands.lift('callout');
                    if (editor.isActive('callout')) return commands.updateAttributes('callout', { kind });
                    return commands.wrapIn('callout', { kind });
                },
            };
        },
    });

    const Video = Node.create({
        name: 'video',
        group: 'block',
        atom: true,
        draggable: true,
        addAttributes() {
            return { src: { default: null } };
        },
        parseHTML() {
            return [{
                tag: 'div[data-video]',
                getAttrs: el => {
                    const frame = el.querySelector('iframe');
                    return frame ? { src: frame.getAttribute('src') } : false;
                },
            }];
        },
        renderHTML({ node }) {
            return ['div', { 'data-video': '' }, ['iframe', { src: node.attrs.src, allowfullscreen: 'true', frameborder: '0' }]];
        },
        addCommands() {
            return {
                setVideo: src => ({ commands }) => commands.insertContent({ type: 'video', attrs: { src } }),
            };
        },
    });

    // Ссылка на страницу базы знаний: обычная ссылка с data-page-id и без target=_blank
    const WikiLink = Link.extend({
        addAttributes() {
            return {
                ...this.parent?.(),
                pageId: {
                    default: null,
                    parseHTML: el => el.getAttribute('data-page-id'),
                    renderHTML: attrs => (attrs.pageId ? { 'data-page-id': attrs.pageId } : {}),
                },
            };
        },
    });

    // ------------------------------------------------------------ «/»-меню
    const slashItems = [
        { title: 'Текст', hint: 'Обычный абзац', icon: 'fa-solid fa-paragraph', keys: 'текст абзац paragraph', run: c => c.setParagraph() },
        { title: 'Заголовок', hint: 'Крупный заголовок раздела', icon: 'fa-solid fa-heading', keys: 'заголовок h2 heading', run: c => c.setHeading({ level: 2 }) },
        { title: 'Подзаголовок', hint: 'Заголовок поменьше', icon: 'fa-solid fa-heading', keys: 'подзаголовок h3', run: c => c.setHeading({ level: 3 }) },
        { title: 'Маркированный список', hint: '• пункты', icon: 'fa-solid fa-list-ul', keys: 'список маркированный bullet', run: c => c.toggleBulletList() },
        { title: 'Нумерованный список', hint: '1. 2. 3.', icon: 'fa-solid fa-list-ol', keys: 'список нумерованный ordered', run: c => c.toggleOrderedList() },
        { title: 'Чек-лист', hint: 'Пункты с галочками', icon: 'fa-solid fa-square-check', keys: 'чеклист чек-лист задачи галочки todo', run: c => c.toggleTaskList() },
        { title: 'Таблица', hint: '3 × 3 с заголовком', icon: 'fa-solid fa-table', keys: 'таблица table', run: c => c.insertTable({ rows: 3, cols: 3, withHeaderRow: true }) },
        { title: 'Важно', hint: 'Синий блок-выноска', icon: 'fa-solid fa-circle-info', keys: 'важно инфо callout info', run: c => c.toggleCallout('info') },
        { title: 'Внимание', hint: 'Оранжевый блок-предупреждение', icon: 'fa-solid fa-triangle-exclamation', keys: 'внимание предупреждение warning', run: c => c.toggleCallout('warning') },
        { title: 'Совет', hint: 'Зелёный блок с подсказкой', icon: 'fa-solid fa-lightbulb', keys: 'совет подсказка tip', run: c => c.toggleCallout('tip') },
        { title: 'Цитата', hint: 'Выделенная цитата', icon: 'fa-solid fa-quote-left', keys: 'цитата quote', run: c => c.toggleBlockquote() },
        { title: 'Код', hint: 'Блок моноширинного текста', icon: 'fa-solid fa-terminal', keys: 'код code', run: c => c.toggleCodeBlock() },
        { title: 'Разделитель', hint: 'Горизонтальная линия', icon: 'fa-solid fa-minus', keys: 'разделитель линия hr', run: c => c.setHorizontalRule() },
        { title: 'Изображение', hint: 'Загрузить картинку', icon: 'fa-regular fa-image', keys: 'изображение картинка фото image', action: 'image' },
        { title: 'Видео', hint: 'YouTube, Rutube, VK Видео', icon: 'fa-brands fa-youtube', keys: 'видео youtube rutube vk video', action: 'video' },
        { title: 'Ссылка на страницу', hint: 'Другая статья базы знаний', icon: 'fa-solid fa-book', keys: 'ссылка страница статья link page', action: 'pageLink' },
    ];

    function slashRenderer() {
        let el = null;
        let items = [];
        let active = 0;
        let command = null;

        function draw() {
            if (!items.length) {
                el.innerHTML = '<div class="wk-slash-empty">Ничего не найдено</div>';
                return;
            }
            el.innerHTML = '<div class="wk-slash-title">Вставить блок</div>' + items.map((it, i) =>
                `<button type="button" class="${i === active ? 'is-active' : ''}" data-i="${i}">` +
                `<span class="wk-slash-ic"><i class="${it.icon}"></i></span><span><b>${W.esc(it.title)}</b><small>${W.esc(it.hint)}</small></span></button>`).join('');
            const btn = el.querySelector('.is-active');
            if (btn) btn.scrollIntoView({ block: 'nearest' });
        }

        function place(rect) {
            if (!rect) return;
            const width = 300;
            const left = Math.max(8, Math.min(rect.left, window.innerWidth - width - 8));
            let top = rect.bottom + 6;
            const height = Math.min(360, el.scrollHeight || 360);
            if (top + height > window.innerHeight - 8) top = Math.max(8, rect.top - height - 6);
            el.style.left = left + 'px';
            el.style.top = top + 'px';
        }

        return {
            onStart: props => {
                el = document.createElement('div');
                el.className = 'wk-slash';
                el.setAttribute('role', 'listbox');
                el.addEventListener('mousedown', e => e.preventDefault());
                el.addEventListener('click', e => {
                    const b = e.target.closest('[data-i]');
                    if (b && command) command(items[+b.getAttribute('data-i')]);
                });
                document.body.appendChild(el);
                items = props.items;
                command = props.command;
                active = 0;
                draw();
                place(props.clientRect && props.clientRect());
            },
            onUpdate: props => {
                items = props.items;
                command = props.command;
                active = 0;
                draw();
                place(props.clientRect && props.clientRect());
            },
            onKeyDown: ({ event }) => {
                if (!items.length) return false;
                if (event.key === 'ArrowDown') { active = (active + 1) % items.length; draw(); return true; }
                if (event.key === 'ArrowUp') { active = (active - 1 + items.length) % items.length; draw(); return true; }
                if (event.key === 'Enter') { command(items[active]); return true; }
                if (event.key === 'Escape') { el.remove(); return true; }
                return false;
            },
            onExit: () => { if (el) el.remove(); el = null; },
        };
    }

    const SlashCommand = Extension.create({
        name: 'slashCommand',
        addProseMirrorPlugins() {
            return [Suggestion({
                editor: this.editor,
                pluginKey: new PluginKey('wikiSlash'),
                char: '/',
                allowSpaces: false,
                items: ({ query }) => {
                    const q = (query || '').toLowerCase().trim();
                    return slashItems.filter(it => !q || it.title.toLowerCase().includes(q) || it.keys.includes(q)).slice(0, 16);
                },
                command: ({ editor, range, props }) => {
                    const chain = editor.chain().focus().deleteRange(range);
                    if (props.run) { props.run(chain).run(); return; }
                    chain.run();
                    runAction(props.action);
                },
                render: slashRenderer,
            })];
        },
    });

    // ------------------------------------------------------------ редактор
    const editor = new Editor({
        element: host.querySelector('[data-editor-mount]'),
        extensions: [
            StarterKit.configure({ heading: { levels: [2, 3, 4] } }),
            Underline,
            WikiLink.configure({ openOnClick: false, autolink: true, linkOnPaste: true, protocols: ['mailto', 'tel'] }),
            Image.configure({ inline: false, allowBase64: false }),
            Table.configure({ resizable: false }),
            TableRow, TableHeader, TableCell,
            TaskList,
            TaskItem.configure({ nested: true }),
            Placeholder.configure({ placeholder: 'Начните писать или нажмите «/», чтобы вставить блок…' }),
            Callout,
            Video,
            SlashCommand,
        ],
        content: initialHtml,
        editorProps: {
            attributes: { class: 'wk-prose', 'aria-label': 'Содержимое страницы', spellcheck: 'true' },
            handlePaste: (view, event) => {
                const files = imageFiles(event.clipboardData && event.clipboardData.files);
                if (!files.length) return false;
                event.preventDefault();
                files.forEach(f => uploadImage(f));
                return true;
            },
            handleDrop: (view, event, slice, moved) => {
                if (moved) return false;
                const files = imageFiles(event.dataTransfer && event.dataTransfer.files);
                if (!files.length) return false;
                event.preventDefault();
                const pos = view.posAtCoords({ left: event.clientX, top: event.clientY });
                if (pos) editor.chain().focus().setTextSelection(pos.pos).run();
                files.forEach(f => uploadImage(f));
                return true;
            },
        },
        onUpdate: () => { markDirty(); },
        onSelectionUpdate: () => updateToolbar(),
        onTransaction: () => updateToolbar(),
    });

    function imageFiles(list) {
        return Array.prototype.slice.call(list || []).filter(f => /^image\/(png|jpeg|gif|webp)$/.test(f.type));
    }

    // ------------------------------------------------------------ картинки
    function uploadImage(file) {
        if (file.size > 50 * 1024 * 1024) { W.toast('Картинка больше 50 МБ', true); return; }
        setStatus('Загружаю изображение «' + file.name + '»…');
        const fd = new FormData();
        fd.append('file', file);
        W.api('/Wiki/api/pages/' + pageId + '/attachments?inline=true', { body: fd })
            .then(data => {
                editor.chain().focus().setImage({ src: data.Url, alt: file.name.replace(/\.[^.]+$/, '') }).run();
                setStatus('Изображение добавлено');
            })
            .catch(err => { setStatus(err.message, true); W.toast(err.message, true); });
    }
    imageInput.addEventListener('change', () => {
        imageFiles(imageInput.files).forEach(f => uploadImage(f));
        imageInput.value = '';
    });

    // ------------------------------------------------------------ видео
    function toEmbedUrl(raw) {
        let url;
        try { url = new URL(raw.trim()); } catch (e) { return null; }
        const h = url.hostname.replace(/^www\.|^m\./, '');
        let m;
        if (h === 'youtu.be' || h === 'youtube.com' || h === 'youtube-nocookie.com') {
            let id = null;
            if (h === 'youtu.be') id = url.pathname.slice(1);
            else if (url.pathname === '/watch') id = url.searchParams.get('v');
            else if ((m = url.pathname.match(/^\/(embed|shorts|live)\/([^/?#]+)/))) id = m[2];
            if (!id || !/^[A-Za-z0-9_-]{6,20}$/.test(id)) return null;
            const t = url.searchParams.get('t') || url.searchParams.get('start');
            const seconds = t ? parseInt(String(t).replace(/s$/, ''), 10) : 0;
            return 'https://www.youtube.com/embed/' + id + (seconds > 0 ? '?start=' + seconds : '');
        }
        if (h === 'rutube.ru') {
            if ((m = url.pathname.match(/^\/(video|play\/embed)\/([A-Za-z0-9]{6,40})/))) return 'https://rutube.ru/play/embed/' + m[2];
            return null;
        }
        if (h === 'vk.com' || h === 'vkvideo.ru' || h === 'vk.ru') {
            if (url.pathname === '/video_ext.php') {
                const oid = url.searchParams.get('oid'), id = url.searchParams.get('id'), hash = url.searchParams.get('hash');
                if (!/^-?\d+$/.test(oid || '') || !/^\d+$/.test(id || '')) return null;
                return 'https://vk.com/video_ext.php?oid=' + oid + '&id=' + id + (hash && /^[A-Za-z0-9]+$/.test(hash) ? '&hash=' + hash : '');
            }
            const source = url.pathname + ' ' + (url.searchParams.get('z') || '');
            if ((m = source.match(/video(-?\d+)_(\d+)/))) return 'https://vk.com/video_ext.php?oid=' + m[1] + '&id=' + m[2];
        }
        return null;
    }

    // ------------------------------------------------------------ диалог ввода
    const promptDialog = document.getElementById('wkPromptDialog');
    function ask(title, label, hint, value) {
        return new Promise(resolve => {
            promptDialog.querySelector('[data-prompt-title]').textContent = title;
            promptDialog.querySelector('[data-prompt-label]').textContent = label;
            promptDialog.querySelector('[data-prompt-hint]').textContent = hint || '';
            const input = promptDialog.querySelector('[data-prompt-input]');
            input.value = value || '';
            const form = promptDialog.querySelector('[data-prompt-form]');
            let answered = false;
            form.onsubmit = e => { e.preventDefault(); answered = true; promptDialog.close(); resolve(input.value.trim()); };
            promptDialog.addEventListener('close', () => { if (!answered) resolve(null); }, { once: true });
            promptDialog.showModal();
            setTimeout(() => { input.focus(); input.select(); }, 30);
        });
    }

    function runAction(action) {
        if (action === 'image') { imageInput.click(); return; }
        if (action === 'video') {
            ask('Видео', 'Ссылка на видео', 'YouTube, Rutube или VK Видео — вставим проигрыватель.', '').then(value => {
                if (!value) return;
                const src = toEmbedUrl(value);
                if (!src) { W.toast('Поддерживаются только ссылки YouTube, Rutube и VK Видео', true); return; }
                editor.chain().focus().setVideo(src).run();
            });
            return;
        }
        if (action === 'pageLink') {
            const { from, to } = editor.state.selection;
            const selected = editor.state.doc.textBetween(from, to, ' ');
            window.WikiPicker.open('Ссылка на страницу', page => {
                const href = '/Wiki/Page/' + page.Id;
                if (selected) {
                    editor.chain().focus().extendMarkRange('link').setLink({ href, target: null, pageId: String(page.Id) }).run();
                } else {
                    editor.chain().focus().insertContent({
                        type: 'text',
                        text: page.Title,
                        marks: [{ type: 'link', attrs: { href, target: null, pageId: String(page.Id) } }],
                    }).insertContent(' ').run();
                }
            });
            return;
        }
        if (action === 'link') {
            const current = editor.getAttributes('link').href || '';
            ask('Ссылка', 'Адрес', 'https://…, mailto:… или tel:… Пустое поле — убрать ссылку.', current).then(value => {
                if (value === null) return;
                if (value === '') { editor.chain().focus().extendMarkRange('link').unsetLink().run(); return; }
                if (!/^(https?:\/\/|mailto:|tel:|\/)/i.test(value)) value = 'https://' + value;
                const { from, to } = editor.state.selection;
                if (from === to && !editor.isActive('link')) {
                    editor.chain().focus().insertContent({ type: 'text', text: value, marks: [{ type: 'link', attrs: { href: value } }] }).run();
                } else {
                    editor.chain().focus().extendMarkRange('link').setLink({ href: value }).run();
                }
            });
        }
    }

    // ------------------------------------------------------------ панель инструментов
    const commands = {
        paragraph: c => c.setParagraph(),
        h2: c => c.toggleHeading({ level: 2 }),
        h3: c => c.toggleHeading({ level: 3 }),
        bold: c => c.toggleBold(),
        italic: c => c.toggleItalic(),
        underline: c => c.toggleUnderline(),
        strike: c => c.toggleStrike(),
        code: c => c.toggleCode(),
        bulletList: c => c.toggleBulletList(),
        orderedList: c => c.toggleOrderedList(),
        taskList: c => c.toggleTaskList(),
        blockquote: c => c.toggleBlockquote(),
        codeBlock: c => c.toggleCodeBlock(),
        hr: c => c.setHorizontalRule(),
        table: c => c.insertTable({ rows: 3, cols: 3, withHeaderRow: true }),
        addRow: c => c.addRowAfter(),
        addCol: c => c.addColumnAfter(),
        delRow: c => c.deleteRow(),
        delCol: c => c.deleteColumn(),
        delTable: c => c.deleteTable(),
        'callout-info': c => c.toggleCallout('info'),
        'callout-warning': c => c.toggleCallout('warning'),
        'callout-tip': c => c.toggleCallout('tip'),
        undo: c => c.undo(),
        redo: c => c.redo(),
    };
    const actions = { link: 'link', pageLink: 'pageLink', image: 'image', video: 'video' };
    const activeChecks = {
        paragraph: () => editor.isActive('paragraph') && !editor.isActive('callout'),
        h2: () => editor.isActive('heading', { level: 2 }),
        h3: () => editor.isActive('heading', { level: 3 }),
        bold: () => editor.isActive('bold'),
        italic: () => editor.isActive('italic'),
        underline: () => editor.isActive('underline'),
        strike: () => editor.isActive('strike'),
        code: () => editor.isActive('code'),
        bulletList: () => editor.isActive('bulletList'),
        orderedList: () => editor.isActive('orderedList'),
        taskList: () => editor.isActive('taskList'),
        blockquote: () => editor.isActive('blockquote'),
        codeBlock: () => editor.isActive('codeBlock'),
        link: () => editor.isActive('link') && !editor.getAttributes('link').pageId,
        pageLink: () => editor.isActive('link') && !!editor.getAttributes('link').pageId,
        'callout-info': () => editor.isActive('callout', { kind: 'info' }),
        'callout-warning': () => editor.isActive('callout', { kind: 'warning' }),
        'callout-tip': () => editor.isActive('callout', { kind: 'tip' }),
    };

    const toolbar = host.querySelector('[data-editor-toolbar]');
    toolbar.addEventListener('mousedown', e => { if (e.target.closest('.wk-tb')) e.preventDefault(); });
    toolbar.addEventListener('click', e => {
        const b = e.target.closest('[data-cmd]');
        if (!b) return;
        const cmd = b.getAttribute('data-cmd');
        if (actions[cmd]) { runAction(actions[cmd]); return; }
        if (commands[cmd]) commands[cmd](editor.chain().focus()).run();
    });

    function updateToolbar() {
        toolbar.querySelectorAll('[data-cmd]').forEach(b => {
            const check = activeChecks[b.getAttribute('data-cmd')];
            if (check) b.classList.toggle('is-on', !!check());
        });
        const inTable = editor.isActive('table');
        toolbar.querySelectorAll('[data-table-tools]').forEach(el => { el.hidden = !inTable; });
    }
    updateToolbar();

    // ------------------------------------------------------------ состояние, черновик, сохранение
    let dirty = false;
    let saving = false;
    let draftTimer = null;

    function setStatus(text, isError) {
        statusEl.textContent = text;
        statusEl.classList.toggle('is-error', !!isError);
    }

    function markDirty() {
        dirty = true;
        clearTimeout(draftTimer);
        draftTimer = setTimeout(saveDraft, 1200);
    }
    titleInput.addEventListener('input', markDirty);

    function saveDraft() {
        try {
            localStorage.setItem(draftKey, JSON.stringify({ v: baseVersion, title: titleInput.value, html: editor.getHTML(), at: Date.now() }));
            const t = new Date();
            setStatus('Черновик сохранён на этом устройстве в ' + t.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }) + '. Не забудьте нажать «Сохранить».');
        } catch (e) { /* localStorage недоступен — не страшно */ }
    }

    function clearDraft() {
        try { localStorage.removeItem(draftKey); } catch (e) { }
    }

    // Предложить восстановить несохранённый черновик
    try {
        const raw = localStorage.getItem(draftKey);
        if (raw) {
            const draft = JSON.parse(raw);
            if (draft && draft.html && (draft.html !== editor.getHTML() || draft.title !== titleInput.value)) {
                const when = new Date(draft.at).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
                const note = draft.v === baseVersion ? '' : '\n\nВнимание: с тех пор страницу уже сохраняли — при сохранении появится предупреждение о конфликте.';
                if (confirm('Найден несохранённый черновик этой страницы от ' + when + '. Восстановить его?' + note)) {
                    editor.commands.setContent(draft.html, true);
                    titleInput.value = draft.title || titleInput.value;
                    if (draft.v !== baseVersion) baseVersion = draft.v;
                    dirty = true;
                } else {
                    clearDraft();
                }
            }
        }
    } catch (e) { }

    function save(force) {
        if (saving) return;
        const title = titleInput.value.trim();
        if (!title) { W.toast('Введите заголовок страницы', true); titleInput.focus(); return; }
        saving = true;
        saveBtn.disabled = true;
        setStatus('Сохраняю…');
        W.api('/Wiki/api/pages/' + pageId + '/save', {
            body: { BaseVersion: baseVersion, Force: !!force, Title: title, Html: editor.getHTML(), Json: editor.getJSON() },
        }).then(() => {
            dirty = false;
            clearDraft();
            location.href = '/Wiki/Page/' + pageId;
        }).catch(err => {
            saving = false;
            saveBtn.disabled = false;
            if (err.status === 409) {
                const d = err.data || {};
                document.querySelector('[data-conflict-text]').textContent =
                    'Пока вы редактировали, страницу сохранил' + (d.ModifiedByName ? 'а(а) ' + d.ModifiedByName : 'и') +
                    (d.ModifiedDate ? ' (' + d.ModifiedDate + ')' : '') + '. Сейчас это версия ' + d.Version + '.';
                setStatus('Конфликт: страницу уже изменили', true);
                W.openDialog('wkConflictDialog');
                return;
            }
            setStatus(err.message, true);
            W.toast(err.message, true);
        });
    }

    saveBtn.addEventListener('click', () => save(false));
    document.querySelector('[data-conflict-overwrite]').addEventListener('click', () => {
        document.getElementById('wkConflictDialog').close();
        save(true);
    });
    document.addEventListener('keydown', e => {
        if ((e.ctrlKey || e.metaKey) && (e.key === 's' || e.key === 'ы' || e.key === 'S')) { e.preventDefault(); save(false); }
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'л') && editor.isFocused) { e.preventDefault(); e.stopPropagation(); runAction('link'); }
    }, true);

    host.querySelector('[data-editor-cancel]').addEventListener('click', e => {
        if (dirty && !confirm('Выйти без сохранения? Черновик останется на этом устройстве.')) e.preventDefault();
    });
    window.addEventListener('beforeunload', e => {
        if (dirty && !saving) { saveDraft(); e.preventDefault(); e.returnValue = ''; }
    });

    window.__wikiEditor = editor; // для отладки
}
