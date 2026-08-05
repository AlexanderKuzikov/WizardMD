# HANDOFF — WizardMD

> Создан: 2026-08-05
> Причина: переполнение контекста после шагов 3 и 2

## Текущая задача

Шаг 1 — COM-превью `WizardMD.Preview` (net48, `IPreviewHandler`) в Проводнике. Рендер БЕЗ WebView2 (ADR-002/AGENTS: в Explorer опасно) — WebBrowser legacy (MSHTML) + свой STA-поток с message loop.

## Что сделано в этой сессии

- **Шаг 3 — ядро готово**: BlockParser (offset-модель PLine.Offset + SliceByColumn), InlineParser (delimiter stack, flanking), AST, HtmlRenderer, HtmlEntities.gen.cs (2125 entities). **spec.json 0.30: 528/652 = 81.0%** (цель 80-90%).
- **Шаг 2 — рендерер готов**: WPF + WebView2 (NuGet 1.0.2592.51), темы light/dark (CSS-переменные), zero-dep JS-подсветка (20 языков, файл `HtmlPage.cs`), CLI `<file.md>`, Ctrl+O, drag&drop. Smoke-тест: стартует, README рендерится.
- **Только что (НЕ закоммичено)**: `MarkdownPage` из App перенесён в Core как `HtmlPage` (namespace `WizardMD.Core`), App использует `Core.HtmlPage.Build`. `Core.csproj` + `<LangVersion>latest</LangVersion>` (raw strings). Сборка зелёная.
- 87 тестов зелёные (SpecTests + BlockTests + InlineTests + GoldenTests + RendererPageTests).

## Что осталось сделать

- [ ] **Закоммитить и запушить** текущие незакоммиченные изменения (HtmlPage в Core)
- [ ] **Шаг 1 — COM-превью**: проект `src/WizardMD.Preview` (net48, WinForms):
  - [ ] `Microsoft.NETFramework.ReferenceAssemblies` NuGet (сборка net48 без установленного Framework SDK)
  - [ ] COM-интерфейсы: `IPreviewHandler` (8895b1c6-b41f-4c1c-a562-0d564250836f), `IInitializeWithFile` (b7d14566-0509-4cce-a71f-0a554233bd9b), `IObjectWithSite` (fc4801a3-2ba9-11cf-a229-00aa003d7352), RECT/MSG
  - [ ] STA-поток: скрытая CommonForm + Application.Run, Invoke для операций с контролами
  - [ ] `WebBrowser` (AllowNavigation=false, ScriptErrorsSuppressed=true, Dock=Fill) + `DocumentText = HtmlPage.Build(md, dark:false)`
  - [ ] Регистрация: ComRegisterFunction/ComUnregisterFunction в **HKCU** (без админа): CLSID\InprocServer32 ThreadingModel=Apartment, `.md\shellex\{8895...}`, PreviewHandlers
  - [ ] `--register-preview`/`--unregister-preview` в App (ручная запись реестра HKCU)
  - [ ] Добавить в sln; после регистрации — перезапуск Explorer
- [ ] Ассоциация `.md` → WizardMD.App.exe: `--register`/`--unregister` (HKCU\Software\Classes\.md, OpenWithProgids, DefaultIcon)
- [ ] Относительные пути картинок: `SetVirtualHostNameToFolderMapping("wmd.local", папка файла)` + `<base href="https://wmd.local/">`
- [ ] Визуально проверить рендерер (темы, подсветку) на реальных .md

## Ключевые файлы

- `src/WizardMD.Core/HtmlPage.cs` — HTML-шаблон: CSS light/dark + JS-подсветка (zero-dep). Переиспользуется App и Preview
- `src/WizardMD.Core/Markdown.cs` — фасад `Parse`/`ToHtml`
- `src/WizardMD.Core/BlockParser.cs` — offset-модель (PLine), важные грабли ниже
- `src/WizardMD.App/MainWindow.xaml.cs` — рендерер, точки для `--register*` команд
- `docs/CONTEXT.md`, `docs/DECISIONS.md` (ADR-001..004)

## Контекст / грабли

- Табы: только offset-модель (`SliceByColumn(line, absoluteColumn)`, `IndentOf` относительный, `l.Offset + IndentOf` — абсолютный)
- contentIndent item = markerStartCol + markerLen + padding (padding по колонкам, откат при ≥5 — как commonmark)
- Fenced code: снимать `min(fenceIndent, lineIndent)` колонок (spec 0.30; python commonmark 0.29 не снимает — не ориентироваться!)
- setext underline после lazy-строки — НЕ setext (флаг `PLine.IsLazy`)
- Reference definitions: не прерывают параграф; после съедения — `_lastBlankLine = true` (иначе вторая ref подряд не съедается)
- Пустой item (`-`) не прерывает параграф; blank не входит в пустой item
- `TryShortcutLink` обязан двигать позицию (был бесконечный цикл на spec#567)
- webView2 Nav: `NavigateToString` для HTML-строки; base about:blank → относительные картинки не резолвятся
- WebView2 151.0.4129.59 установлен; SDK 8.0.423 в `%LOCALAPPDATA%\dotnet` (полный путь: `& "$env:LOCALAPPDATA\dotnet\dotnet.exe"`)
- НЕ грузить WebView2 в COM-хендлер (шаг 1) — в процессе Explorer опасно, только WebBrowser legacy
- Пользователь коммитит сам (сообщения «.» — GitHub Desktop autocommit); коммитить/пушить при явной просьбе или для HANDOFF

## Команды

```bash
& "$env:LOCALAPPDATA\dotnet\dotnet.exe" build WizardMD.sln
& "$env:LOCALAPPDATA\dotnet\dotnet.exe" test WizardMD.sln
& "$env:LOCALAPPDATA\dotnet\dotnet.exe" run --project src/WizardMD.App -- README.md
```

## Следующий шаг

1. Закоммитить текущие изменения (HtmlPage в Core).
2. Создать `src/WizardMD.Preview` по чеклисту (net48 + ReferenceAssemblies NuGet → COM-интерфейсы → STA-поток → WebBrowser → регистрация HKCU → `--register-preview` в App → тест: перезапуск Explorer, превью .md).
