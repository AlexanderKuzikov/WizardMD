# WizardMD — CONTEXT

> Последнее обновление: 2026-08-05

## Статус
| Компонент | Статус | Версия/Заметка |
|-----------|--------|----------------|
| Окружение | готово | .NET SDK 8.0.423 (`%LOCALAPPDATA%\dotnet`, PATH user), WebView2 151.0.4129.59 |
| Решение | создано | WizardMD.sln: Core (netstandard2.0), App (net8.0-windows WPF), Tests (xUnit) |
| Ядро-парсер | **готово** | Шаг 3. BlockParser + InlineParser + AST + HtmlRenderer. **spec.json 0.30: 528/652 (81.0%)** — цель 80-90% достигнута |
| Рендерер | **готово** | Шаг 2. WPF + WebView2 (NuGet 1.0.2592.51), темы light/dark, подсветка синтаксиса (zero-dep JS, 20 языков), drag&drop, Ctrl+O, CLI `<file.md>` |
| COM-превью | не начато | Шаг 1 — net48, IPreviewHandler |
| GitHub | синхронизирован | Коммиты в main (частично GitHub Desktop autocommit) |

## Что реализовано в ядре (шаг 3)

- **AST** (`src/WizardMD.Core/Ast/Nodes.cs`): Document, Paragraph, Heading, List/ListItem (task-листы), BlockQuote, CodeBlock (fenced/indented), ThematicBreak, Table (GFM, align), inline: Text, Strong, Emphasis, Strikethrough, Code, Link, Image, AutoLink, Soft/HardBreak.
- **BlockParser.cs**: линейный проход, PLine с offset-моделью (табы, вложенные контейнеры по commonmark reference), ATX/setext, цитаты (lazy-продолжения с флагом IsLazy), списки (вложенность, loose/tight, пустые items, правила прерывания параграфа), fenced/indented code, hr, таблицы GFM, reference definitions (multiline, первый wins, не прерывают параграф).
- **InlineParser.cs**: delimiter-based emphasis/strong/strike (flanking по спеку, начало/конец строки = whitespace), code spans (приоритет над link-скобками), links (inline/full/shortcut, вложенные скобки, escaped, «no links in links»), images (plain-alt), autolinks (URI+email по regex спекуля), entities (полный набор HTML5, 2125 шт.), escapes, hard/soft breaks, percent-encoding URL, trim whitespace строк параграфа.
- **HtmlRenderer.cs**: AST → HTML (переиспользуется в шаге 2 и 1).
- **HtmlEntities.gen.cs**: полный словарь HTML5 entities (сгенерирован из Python html.entities.html5).

## Open-проблемы
| # | Priority | Описание |
|---|----------|----------|
| 1 | med | Spec-тесты: остаток 124 примера — HTML blocks (44, вне подмножества), Raw HTML (12, вне), сложные emphasis/link/nested-list кейсы. Дальнейшее повышение — по желанию |
| 2 | med | Рендерер: относительные пути картинок не работают (NavigateToString, base about:blank). Решение — `SetVirtualHostNameToFolderMapping` на папку файла |
| 3 | med | Ассоциация .md → WizardMD.App.exe (реестр, `--register`) |
| 4 | low | Подсветка: проверить визуально на реальных файлах, расширить языки при необходимости |
| 5 | low | COM-хендлер: исследовать рендер без WebView2 (шаг 1) |

## Грабли (из сессии)
- Табы: offset-модель обязательна (SliceByColumn + PLine.Offset), иначе вложенные списки/цитаты ломаются. contentIndent для item = markerStartCol + markerLen + padding (padding по колонкам с откатом ≥5).
- Fenced code: содержимое со снятием min(fenceIndent, lineIndent) колонок (spec 0.30; python commonmark 0.29 не снимает — не ориентироваться).
- setext underline после lazy-строки — НЕ setext (флаг PLine.IsLazy).
- Reference definitions не прерывают параграф; после съедения ref строка ведёт себя как blank (_lastBlankLine=true), иначе вторая ref подряд не съедается.
- Пустой list item (`-`) не прерывает параграф.
- `IndentOf` — относительный (колонки пробелов в Text), `l.Offset + IndentOf` — абсолютный.
- SpecTests зацикливался: TryShortcutLink не двигал позицию → newPos обязателен.

## Журнал работ
| Дата | Изменение |
|------|-----------|
| 2026-08-05 | HTML-шаблон (MarkdownPage) перенесён в Core как `HtmlPage` — переиспользуется App и будущим Preview. Core.csproj + LangVersion latest |
| 2026-08-05 | Шаг 2 (рендерер): WPF + WebView2, `MarkdownPage` (темы light/dark через CSS-переменные, zero-dep JS-подсветка 20 языков), открытие файла (CLI-аргумент, Ctrl+O, drag&drop), smoke-тест старта. 87 тестов зелёные |
| 2026-08-05 | Ядро-парсер реализовано: AST, BlockParser (offset-модель), InlineParser (delimiter stack), HtmlRenderer, entities. Прогон spec.json 0.30 — **81.0%** (528/652). Юнит-тесты: Block/Inline/Golden (~70 кейсов) + SpecTests. Сборка зелёная, 84 теста |

## Структура проекта
```
WizardMD/
├── README.md
├── AGENTS.md
├── WizardMD.sln
├── src/
│   ├── WizardMD.Core/      # ядро-парсер, netstandard2.0
│   │   ├── Ast/Nodes.cs
│   │   ├── BlockParser.cs  # блоки + offset-модель
│   │   ├── InlineParser.cs # inline + delimiters
│   │   ├── HtmlRenderer.cs
│   │   ├── HtmlPage.cs     # HTML-шаблон: темы + подсветка (App и Preview)
│   │   ├── Markdown.cs     # фасад Parse/ToHtml
│   │   ├── MarkdownUtil.cs # normalize label, url, entities
│   │   └── HtmlEntities.gen.cs
│   └── WizardMD.App/       # WPF + WebView2 рендерер, net8.0-windows
├── tests/
│   ├── CommonMark/spec.json   # спецификация 0.30 (652 примера)
│   └── WizardMD.Core.Tests/   # xUnit: SpecTests, BlockTests, InlineTests, GoldenTests
└── docs/
    ├── CONTEXT.md
    └── DECISIONS.md
```
