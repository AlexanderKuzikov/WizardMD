# HANDOFF — WizardMD

> Создан: 2026-08-05
> Причина: плановый переход сессии после подготовки проекта

## Текущая задача

Реализовать **шаг 3** — ядро-парсер `WizardMD.Core` (CommonMark+GFM подмножество) с архитектурой под spec. Это фундамент для шага 2 (рендерер) и шага 1 (COM-превью).

## Что сделано в подготовке

- **Окружение:** .NET SDK 8.0.423 установлен user-scope в `%LOCALAPPDATA%\dotnet` (основной `dotnet` на C:\ не имел SDK, только runtime). PATH добавлен в User. WebView2 151.0.4129.59 уже был.
- **Решение:** `WizardMD.sln` — `src/WizardMD.Core` (netstandard2.0), `src/WizardMD.App` (net8.0-windows, WPF), `tests/WizardMD.Core.Tests` (xUnit). Сборка зелёная.
- **Доки:** README, AGENTS.md, docs/CONTEXT.md, docs/DECISIONS.md (4 ADR), LICENSE (Apache-2.0).
- Class1.cs из ядра удалён, UnitTest1.cs в тестах — удалить при первом реальном тесте.

## Что осталось сделать (шаг 3)

- [ ] Удалить `tests/WizardMD.Core.Tests/UnitTest1.cs` (заглушка)
- [ ] Скачать `spec.json` CommonMark (версию зафиксировать) → `tests/CommonMark/spec.json`
- [ ] `src/WizardMD.Core/Ast/` — `Document`, блочные узлы (Heading, Paragraph, List, ListItem, BlockQuote, CodeBlock, ThematicBreak, Table), inline-узлы (Text, Strong, Emphasis, Strikethrough, Code, Link, Image, AutoLink)
- [ ] `BlockParser.cs` — линейный проход, блоки: заголовки ATX (`#–######`), параграфы, списки (вложенность, task-листы `- [x]`), цитаты, hr (`---`, `***`, `___`), fenced code (~~~, ```, с языком), таблицы GFM (`| a | b |` + `|---|`)
- [ ] `InlineParser.cs` — рекурсивный: `**bold**`, `*italic*`, `~~strike~~`, `` `code` ``, `[text](url)`, `![alt](src)`, автоссылки, escape `\*`
- [ ] `HtmlRenderer.cs` — AST → HTML (переиспользуется в шаг 2 WebView2 и шаг 1 COM)
- [ ] Тесты: юнит на каждый блок/inline + прогон spec.json (цель 80-90%) + golden на реальные .md (README, CONTEXT, knowledge)
- [ ] Критерий успеха: без глюков на реальных файлах + зелёные тесты

## Ключевые файлы

- `src/WizardMD.Core/WizardMD.Core.csproj` — target `netstandard2.0` (для переиспользования в .NET Framework 4.8 на шаге 1)
- `docs/DECISIONS.md` — ADR-001 стек, ADR-002 WebView2, ADR-003 объём парсера, ADR-004 темы

## Контекст/грабли

- Телеметрия .NET: `DOTNET_CLI_TELEMETRY_OPTOUT=1` — можно выставить, чтобы не спамила
- `dotnet` из PATH — установленный нами SDK 8.0.423, НЕ системный
- WebView2 Nav занят: `NavigateToString` для HTML-строки, `Navigate` для URL
- Парсер — раздельно блоки и inline (не один проход), иначе краевые случаи CommonMark не сходятся
- Полный спек → 100% — не цель, 80-90% spec.json достаточно (ADR-003)
- Не грузить WebView2 в COM-хендлер (шаг 1) — в процессе Explorer это опасно

## Команды

```bash
dotnet build WizardMD.sln
dotnet test WizardMD.sln
dotnet run --project src/WizardMD.App -- <file.md>
```

## Следующий шаг

Реализовать ядро-парсер (см. чеклист). После зелёных тестов — шаг 2 (рендерер), затем шаг 1 (COM). После каждого шага — коммит в main + обновить docs/CONTEXT.md.