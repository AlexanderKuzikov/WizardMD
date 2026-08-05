# WizardMD — Instructions for AI Agents

## Commands

- build: `dotnet build WizardMD.sln`
- test: `dotnet test WizardMD.sln`
- run app: `dotnet run --project src/WizardMD.App -- <file.md>`

## Conventions

- Стек — **C#, .NET 8** (WPF + WebView2). Ядро — `netstandard2.0` (переиспользуется в .NET Framework 4.8 для COM-хендлера).
- Парсер — архитектура по CommonMark reference: блочный парсер + inline-процессор раздельно, AST-иерархия, HtmlRenderer.
- Тесты — xUnit. Прогон против официального `spec.json` (CommonMark) — целевой ориентир 80-90%, не 100%.
- Подмножество фич: заголовки, параграфы, списки (вложенность + task-листы), цитаты, hr, код-блоки с языком, inline-код, жирный/курсив/зачеркнутый, ссылки, изображения, автоссылки, escape, таблицы GFM.
- Коммиты — прямо в `main`, повелительное наклонение, ≤72 символа.

## Structure

- `src/WizardMD.Core/` — ядро-парсер (блоки, inline, AST, HtmlRenderer)
- `src/WizardMD.App/` — WPF-рендерер + WebView2
- `tests/WizardMD.Core.Tests/` — xUnit-тесты ядра
- `docs/` — CONTEXT/DECISIONS

## Do NOT touch

- `spec.json` — скачивается из CommonMark repo, версию фиксировать
- Не грузить WebView2 в COM-хендлер (шаг 1) — в процессе Explorer это опасно, нужен лёгкий рендер

## Documentation rules

- После работы — обнови docs/CONTEXT.md
- Архитектурное решение — добавь в docs/DECISIONS.md
- НЕ создавай новых .md файлов без разрешения
- Грабли — в D:\GitHub\knowledge/ (wizardmd.md или в .NET-файл)