# WizardMD — CONTEXT

> Последнее обновление: 2026-08-06

## Статус
| Компонент | Статус | Версия/Заметка |
|-----------|--------|----------------|
| Окружение | готово | .NET SDK 8.0.423 (`%LOCALAPPDATA%\dotnet`, PATH user), WebView2 151.0.4129.59 |
| Решение | создано | WizardMD.sln: Core (netstandard2.0), App (net8.0-windows WPF), Tests (xUnit) |
| Ядро-парсер | **готово** | Шаг 3. BlockParser + InlineParser + AST + HtmlRenderer. **spec.json 0.30: 528/652 (81.0%)** — цель 80-90% достигнута |
| Рендерер | **готово** | Шаг 2. WPF + WebView2 (NuGet 1.0.2592.51), темы light/dark, подсветка синтаксиса (zero-dep JS, 20 языков), drag&drop, Ctrl+O, CLI `<file.md>` |
| COM-превью | **готово** | Шаг 1. `WizardMD.Preview` (net48, IPreviewHandler). WebBrowser legacy на собственном STA-потоке. Регистрация HKCU (`--register-preview`): mscoree + AppID + DllSurrogate=prevhost + EnablePreviewHandler. **Работает в Проводнике** (2026-08-06, повторные клики ок) |
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
| 3 | med | Ассоциация .md → WizardMD.App.exe (реестр, `--register`). Команды готовы, не применены — спросить пользователя (переопределит текущий редактор) |
| 4 | low | Подсветка: проверить визуально на реальных файлах, расширить языки при необходимости |

## Грабли (из сессии)
- Табы: offset-модель обязательна (SliceByColumn + PLine.Offset), иначе вложенные списки/цитаты ломаются. contentIndent для item = markerStartCol + markerLen + padding (padding по колонкам с откатом ≥5).
- Fenced code: содержимое со снятием min(fenceIndent, lineIndent) колонок (spec 0.30; python commonmark 0.29 не снимает — не ориентироваться).
- setext underline после lazy-строки — НЕ setext (флаг PLine.IsLazy).
- Reference definitions не прерывают параграф; после съедения ref строка ведёт себя как blank (_lastBlankLine=true), иначе вторая ref подряд не съедается.
- Пустой list item (`-`) не прерывает параграф.
- `IndentOf` — относительный (колонки пробелов в Text), `l.Offset + IndentOf` — абсолютный.
- SpecTests зацикливался: TryShortcutLink не двигал позицию → newPos обязателен.
- CLSID-ключи в реестре — **только в фигурных скобках** (`HKCU\Software\Classes\CLSID\{GUID}`), иначе CoCreateInstance → REGDB_E_CLASSNOTREG, хотя `reg query` показывает ключ.
- `CLSIDFromProgID` (GetTypeFromProgID) НЕ видит per-user ProgID из HKCU — активация только по CLSID; ProgID регистрируем для справки.
- net48 COM: InprocServer32 = mscoree.dll + значения Assembly/Class/CodeBase (file:// URI), ThreadingModel=Both — Explorer активирует по CLSID без regasm/GAC.
- MOTW-блок («файл небезопасен» в preview pane) — **глобальное поведение ОС** для untrusted-файлов: показывается даже без хендлера (проверено на .txt). Не диагностический сигнал!
- **COM-превью без AppID+prevhost не активируется в Explorer**: CLSID (mscoree+CodeBase) резолвится, но объект не создаётся. Решение — AppID + `DllSurrogate=%SystemRoot%\system32\prevhost.exe` (по образцу Edge-PDF) + `EnablePreviewHandler=1` + `AutomaticallyPreviewUntrustedFiles=1`.
- **WebBrowser legacy: повторный рендер в том же процессе → «Переход на веб-страницу отменен»** (MSHTML error page). Причины и фиксы: `AllowNavigation=false` блокирует навигацию после первой загрузки → `true`; `Stop()` перед `DocumentText` отменяет новую навигацию → убрать; форма только `Close` без `Dispose` копит MSHTML-объекты в prevhost → `SetParent(0)` + `Close` + `Dispose` в Unload.
- **prevhost.exe держит net48 DLL** (WizardMD.Preview и WizardMD.Core): пересборка падает с MSB3021 «file is being used» → `taskkill /f /im prevhost.exe` перед сборкой.
- **`DllSurrogate` — ТОЛЬКО REG_EXPAND_SZ** (`%SystemRoot%\system32\prevhost.exe`): `reg add /t REG_EXPAND_SZ` или `RegistryValueKind.ExpandString`. REG_SZ не раскрывает `%SystemRoot%` → prevhost не находится → «Невозможно выполнить предварительный просмотр». Registry.SetValue без типа пишет REG_SZ — грабля, из-за которой превью сломалось после перерегистрации кодом (2026-08-06).

## Журнал работ
| Дата | Изменение |
|------|-----------|
| 2026-08-06 | Регрессия превью после перерегистрации: `DllSurrogate` записан как REG_SZ (Registry.SetValue по умолчанию) → `%SystemRoot%` не раскрывается → «Невозможно выполнить предварительный просмотр». Фикс: `RegistryValueKind.ExpandString` в регистраторе. Диагностика: подстановка Edge-CLSID в shellex → «Нет данных» = резолв жив, проблема в активации CLSID |
| 2026-08-06 | **COM-превью починено в Проводнике** (open #7 закрыта): причина — неактивация CLSID в Explorer. Решение по образцу Edge-PDF: CLSID + AppID + `DllSurrogate=prevhost.exe` + `EnablePreviewHandler=1` + `AutomaticallyPreviewUntrustedFiles=1` (регистратор `--register-preview` дополнен, AppID в `PreviewInfo`). Повторный рендер «Переход на веб-страницу отменен»: `AllowNavigation=true`, убран `Stop()`, в Unload `SetParent(0)+Close+Dispose` (иначе MSHTML копится в prevhost). Диагностика: `[ModuleInitializer]` (net48-трюк) + `%TEMP%\wizardmd-preview.log`. Работает: повторные клики по .md, кириллица ок |
| 2026-08-05 | Шаг 1 (COM-превью): `WizardMD.Preview` (net48, IPreviewHandler + IInitializeWithFile + IObjectWithSite), WebBrowser legacy на собственном STA-потоке (PreviewForm + Application.Run, SetParent/MoveWindow в hwnd Explorer), регистрация HKCU через `--register-preview`/`--unregister-preview` (mscoree + Assembly/Class/CodeBase, shellex .md, PreviewHandlers), идентификаторы в Core `PreviewInfo`, ассоциация .md (`--register`/`--unregister`, OpenWithProgids без переопределения дефолта). Smoke: COM-активация по CLSID + Initialize + DoPreview + Unload OK. Explorer перезапущен. 87 тестов зелёные |
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
│   │   ├── PreviewInfo.cs  # CLSID/ProgId COM-превью (источник правды)
│   │   └── HtmlEntities.gen.cs
│   ├── WizardMD.App/       # WPF + WebView2 рендерер, net8.0-windows (--register* команды)
│   └── WizardMD.Preview/   # COM-превью net48: PreviewHandler, PreviewForm, Com/Interop
├── tests/
│   ├── CommonMark/spec.json   # спецификация 0.30 (652 примера)
│   └── WizardMD.Core.Tests/   # xUnit: SpecTests, BlockTests, InlineTests, GoldenTests
└── docs/
    ├── CONTEXT.md
    └── DECISIONS.md
```
