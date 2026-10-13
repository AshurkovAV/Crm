// Точка входа сборки Tiptap для базы знаний (см. package.json). Экспортирует всё, что использует
// wwwroot/js/wiki/editor.js. Версии пакетов зафиксированы в package.json / package-lock.json.
export { Editor, Node, Mark, Extension, mergeAttributes, InputRule, textblockTypeInputRule, wrappingInputRule, nodeInputRule } from '@tiptap/core';
export { default as StarterKit } from '@tiptap/starter-kit';
export { default as Underline } from '@tiptap/extension-underline';
export { default as Link } from '@tiptap/extension-link';
export { default as Image } from '@tiptap/extension-image';
export { default as Table } from '@tiptap/extension-table';
export { default as TableRow } from '@tiptap/extension-table-row';
export { default as TableHeader } from '@tiptap/extension-table-header';
export { default as TableCell } from '@tiptap/extension-table-cell';
export { default as TaskList } from '@tiptap/extension-task-list';
export { default as TaskItem } from '@tiptap/extension-task-item';
export { default as Placeholder } from '@tiptap/extension-placeholder';
export { default as Suggestion } from '@tiptap/suggestion';
export { Plugin, PluginKey } from '@tiptap/pm/state';
