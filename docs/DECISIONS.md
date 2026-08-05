# WizardMD — DECISIONS

<!-- Append-only. Формат фиксирован. -->

## 2026-08-05: Стек — единый C#/.NET

**Контекст:** Нужен просмотрщик Markdown с дорожной картой от парсера до COM-превью-хендлера в Проводнике. Рассматривался Go (опыт в Go+WebView2, 6MB бинарники).

**Решение:** C#/.NET на всём пути:
- Ядро `WizardMD.Core` — `netstandard2.0` (переиспользуется и в .NET 8, и в .NET Framework 4.8)
- Рендерер `WizardMD.App` — .NET 8, WPF + WebView2
- COM-хендлер `WizardMD.Preview` — .NET Framework 4.8 (`net48`)

**Почему не Go:** COM-хендлеры загружаются в процесс Explorer (Win32 COM-контракт: `IPreviewHandler`, `IInitializeWithFile`). На Go это недели cgo-research вместо дней на C#. Единый язык — одно ядро на все уровни.

**Trade-off:** self-contained бинарник больше (~70MB vs 6MB у Go), но для десктоп-инструмента Windows это приемлемо. Расширяет портфолио (данные из обсуждения: «.NET почти нативно для Windows»).

## 2026-08-05: Рендер в App — WPF + WebView2

**Контекст:** Парсер отдаёт HTML, нужен контрол для отображения.

**Решение:** WPF + WebView2 (Edge Chromium, встроен в Win10/11). Парсер → HTML → `NavigateToString()`. Полный CSS-контроль вёрстки, подсветка синтаксиса, таблицы.

**Альтернатива:** RichTextBox — отклонён (слабый CSS-контроль, подсветка кода и таблицы — мучение).

## 2026-08-05: Объём парсера — подмножество CommonMark+GFM, архитектура под spec

**Контекст:** Полный CommonMark (~1500-2500 строк, недели на спек-тесты) vs лёгкое подмножество (глюки на краевых случаях).

**Решение:** Архитектура по CommonMark reference (блочный парсер + inline-процессор раздельно, AST, HtmlRenderer), реализация подмножества, прогон против `spec.json` с целевым ориентиром **80-90%** (не 100%). Остальные краевые — осознанно отложены.

**Trade-off:** не 100% соответствие спек-тестам, но корректная архитектура без переписывания ядра позже. Парсер — фундамент шагов 2 и 1, переписывать дорого.

## 2026-08-05: Тема — светлая по умолчанию, тёмная переключаемая

**Контекст:** Вопрос стиля рендерера от пользователя.

**Решение:** Светлая по умолчанию, переключатель на тёмную. Подсветка синтаксиса — обязательна с первого релиза рендерера.

## 2026-08-05: COM-превью — WebBrowser legacy на собственном STA-потоке, регистрация HKCU вручную

**Контекст:** Шаг 1 — IPreviewHandler в Проводнике. WebView2 в процессе Explorer опасен (AGENTS), нужен лёгкий рендер. .NET Framework 4.8-сборку нельзя поставить в GAC без админа, regasm пишет в HKLM.

**Решение:**
- Рендер — WinForms `WebBrowser` (MSHTML legacy) на **собственном STA-потоке** (скрытая форма + `Application.Run`), окно прицепляется к hwnd Explorer через `SetParent`/`MoveWindow`. COM-класс — тонкая обёртка: методы пересылают работу на UI-поток через `Control.Invoke` — Explorer не блокируется рендером.
- Регистрация — **ручная запись HKCU** (без админа, без regasm/GAC): `InprocServer32 = mscoree.dll` + `Assembly`/`Class`/`CodeBase` (file:// URI), `ThreadingModel = Both`; `.md\shellex\{IPreviewHandlerIID}`; `PreviewHandlers\{CLSID}`; ProgID\CLSID. Команды `--register-preview`/`--unregister-preview` в App. Идентификаторы (CLSID/ProgId/ClassName) — единый источник правды в Core `PreviewInfo`.
- `ClassInterface.AutoDispatch` — IDispatch для диагностики/скриптов (PowerShell smoke-тест), vtable интерфейсов — по ComImport-декларациям.
- ComRegisterFunction не делаем: regasm не нужен, HKCU-запись — единственный надёжный путь.

**Грабли:** ключи CLSID в реестре только в фигурных скобках (`CLSID\{GUID}`); `CLSIDFromProgID` не видит per-user ProgID — активация только по CLSID.

## 2026-08-06: COM-превью — AppID + DllSurrogate=prevhost.exe обязательны для активации

**Контекст:** CLSID (mscoree+CodeBase) резолвился (shellex-тесты), COM-активация из обычного процесса работала, но Explorer НЕ создавал объект (лог пуст, «Невозможно выполнить предварительный просмотр»).

**Решение:** Регистрация по образцу Edge-PDF-хендлера:
- CLSID + `AppID={GUID}` (новый, в Core `PreviewInfo.AppId`)
- `HKCU\Software\Classes\AppID\{GUID}` → `DllSurrogate = %SystemRoot%\system32\prevhost.exe` (хендлер выгружается в prevhost.exe, а не в explorer)
- `EnablePreviewHandler=1`, `AutomaticallyPreviewUntrustedFiles=1`

**Результат:** превью заработало. Похоже, Explorer отказывается создавать mscoree-Inproc объекты напрямую в своём процессе (или требует AppID для unmanaged surrogates); prevhost — штатный механизм изоляции preview-хендлеров.

**Побочный эффект:** DLL держится процессом prevhost.exe — перед пересборкой `taskkill /f /im prevhost.exe` (иначе MSB3021).

## 2026-08-06: WebBrowser legacy — повторный рендер в одном процессе

**Контекст:** первый рендер работает, повторные клики → «Переход на веб-страницу отменен» (MSHTML error page). Explorer переиспользует объект хендлера (ctor один раз), каждый раз создаётся новый PreviewForm.

**Решение (три фикса вместе):**
- `AllowNavigation = true` — `false` блокирует навигацию MSHTML после первой загрузки.
- Убрать `WebBrowser.Stop()` перед `DocumentText` — Stop отменял новую навигацию.
- В `Unload`: `SetParent(0)` + `Close` + `Dispose` формы — иначе MSHTML-COM-объекты копятся в процессе prevhost.exe.