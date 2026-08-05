# HANDOFF — WizardMD

> Создан: 2026-08-05
> Причина: переполнение контекста в середине диагностики COM-превью

## Текущая задача

**Починить COM-превью `WizardMD.Preview` в Проводнике.** Хендлер зарегистрирован (HKCU), реестр корректен, COM-активация работает из обычного процесса, но Explorer НЕ создаёт объект: preview pane показывает «Невозможно выполнить предварительный просмотр», лог `%TEMP%\wizardmd-preview.log` пуст (ни одна попытка превью не дошла до нашего кода).

## Что доказано диагностикой (НЕ перепроверять без нужды)

1. **Резолв по `HKCU\Software\Classes\.md\shellex\{8895B1C6-B41F-4C1C-A562-0D564250836F}` работает**: подстановка Edge-PDF-CLSID `{3A84F9C2-6164-485C-A7D9-4B27F8AC009E}` в shellex → Edge-хендлер ВЫЗВАЛСЯ (показал сырой текст .md). Значит проблема не в shellex и не в HKCU-схеме.
2. **HKCU-shellex читается**: тест на `.pdf` — запись HKCU-переопределения с несуществующим CLSID `{00000000-...}` сменила превью PDF с Edge-рендера на «Нет данных». Значит HKCU-запись приоритетнее/читается.
3. **COM-активация нашего CLSID работает из обычного 64-битного процесса**: PS 5.1: `GetTypeFromCLSID` + `Activator.CreateInstance` + QI на IPreviewHandler (hr=0) + Initialize/DoPreview/Unload — OK. mscoree + Assembly/Class/CodeBase читаются корректно.
4. **MOTW-блок («файл может нанести вред») — глобальное поведение ОС**, не связано с хендлером: `.txt` с Zone.Identifier=3 без всякого хендлера тоже показывает этот блок. Не использовать как диагностический сигнал.
5. `EnablePreviewHandler=1` и `AutomaticallyPreviewUntrustedFiles=1` добавлены в CLSID (по образцу Edge-PDF-хендлера). Эффект EnablePreviewHandler на резолв не изолирован (хронология запутана MOTW-файлами) — проверить при случае.
6. HKLM-запись невозможна из обычного процесса («Отказано в доступе»). Политик Explorer (HKLM\Software\Policies\Microsoft\Windows\Explorer) нет.
7. BHID_PreviewHandler (`IShellItem::BindToHandler`) НЕ резолвит даже рабочий PDF-хендлер на этой системе — **этот тест невалиден для диагностики** (механизм preview pane другой). Выбросить `%TEMP%\opencode\preview-resolve-test.cs`-подход.

## Текущее состояние реестра (проверено)

```
HKCU\Software\Classes\CLSID\{48A5B98A-BFE6-4E21-9CAA-876A31963DC2}\InprocServer32
    (Default)=C:\WINDOWS\system32\mscoree.dll
    Assembly=WizardMD.Preview, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
    Class=WizardMD.Preview.PreviewHandler
    CodeBase=file:///D:/GitHub/WizardMD/src/WizardMD.Preview/bin/Debug/net48/WizardMD.Preview.dll
    ThreadingModel=Both
    + EnablePreviewHandler=1, AutomaticallyPreviewUntrustedFiles=1, DisplayName
HKCU\Software\Classes\.md\shellex\{8895B1C6-...} = {48A5B98A-...}   ← ВЕРНУТО на наш CLSID
HKCU\Software\Microsoft\Windows\CurrentVersion\PreviewHandlers\{48A5B98A-...} = WizardMD Markdown Preview
HKCU\Software\Classes\WizardMD.Preview\CLSID = 48A5B98A-... (без скобок, ProgID для справки)
```
Тестовые файлы `%USERPROFILE%\Downloads\wmd-test.*` удалены, `HKCU\.pdf` и `HKCU\Markdown\shellex` переопределения удалены.

## План (приоритет сверху вниз)

1. **Вернуть лог-инструментацию**: DebugLog.cs уже в коде (закоммичен). Добавить `[ModuleInitializer]` (net48-совместимый трюк: объявить атрибут в `namespace System.Runtime.CompilerServices`) с записью «module loaded» + лог в конструкторе PreviewHandler. Пересборка → `taskkill /f /im explorer.exe` + `Start-Process explorer.exe` → пользователь превьюит локальный .md → читать лог:
   - «module loaded» есть, но методов нет → CLR грузится, класс не создаётся/CCW-проблема → смотреть исключение конструктора.
   - лог пуст → активация не вызывается вообще → шаг 2.
2. **Изоляция как у Edge-PDF**: у рабочих хендлеров есть `AppID` (у Edge-PDF: `{6d2b5079-2f0b-48dd-ab7f-97cec514d30b}`). Создать новый GUID AppID, `HKCU\Software\Classes\AppID\{GUID}` с `DllSurrogate = prevhost.exe`, в CLSID добавить `AppID` значение. Перезапуск Explorer → тест. (Проверить сначала наличие AppID/DllSurrogate у Word/Excel-хендлеров: `reg query HKLM\Software\Classes\CLSID\{84F66100-FF7C-4fb4-B0C0-02CD7FB668FE} /s` и `{00020827-0000-0000-C000-000000000046}`.)
3. **HKLM-регистрация** (план-Б, требует UAC-подтверждения пользователя): повторить CLSID + shellex в HKLM через elevated-процесс (Start-Process -Verb RunAs с командой reg add или мини-командой). Проверка гипотезы «shell preview resolution читает CLSID только из HKLM».
4. Если активация есть, но Explorer не показывает → проверить `DocumentText`/кодировку (WebBrowser legacy + кириллица), визуальный тест.
5. MOTW-файлы: проверить, снимает ли `AutomaticallyPreviewUntrustedFiles=1` блок (вероятно, для неподписанных хендлеров не сработает — задокументировать как ограничение ОС).
6. После починки: убрать DebugLog.cs (grep `DebugLog.` и `[DEBUG`), вернуть shellex, обновить CONTEXT (закрыть #7), ADR в DECISIONS, обновить `D:\GitHub\knowledge\wizardmd.md` (грабли: EnablePreviewHandler, AppID/DllSurrogate, MOTW, невалидность BHID-теста).

## Ключевые файлы

- `src/WizardMD.Preview/PreviewHandler.cs` — COM-класс: STA-поток, PreviewForm, SetParent/MoveWindow; сейчас с логами DebugLog
- `src/WizardMD.Preview/DebugLog.cs` — диагностический лог в `%TEMP%\wizardmd-preview.log` (удалить после починки)
- `src/WizardMD.Preview/PreviewForm.cs` — WebBrowser legacy, `DocumentText = HtmlPage.Build(...)`
- `src/WizardMD.App/PreviewRegistration.cs` — регистрация HKCU (`--register-preview` / `--unregister-preview`)
- `src/WizardMD.Core/PreviewInfo.cs` — CLSID `48A5B98A-BFE6-4E21-9CAA-876A31963DC2`, ProgId, ClassName
- `docs/CONTEXT.md` — статус: open-проблема #7 (high), грабли диагностики

## Команды

```bash
& "$env:LOCALAPPDATA\dotnet\dotnet.exe" build WizardMD.sln
& "$env:LOCALAPPDATA\dotnet\dotnet.exe" test WizardMD.sln
src\WizardMD.App\bin\Debug\net8.0-windows\WizardMD.exe --register-preview   # перерегистрация после пересборки
taskkill /f /im explorer.exe; Start-Sleep 2; Start-Process explorer.exe      # перезапуск Explorer
Get-Content "$env:TEMP\wizardmd-preview.log"                                 # лог хендлера
```

## Грабли

- CLSID-ключи реестра — только в фигурных скобках; `CLSIDFromProgID` не видит HKCU-ProgID.
- Preview pane: для локальных файлов «Невозможно» = хендлер не создан; MOTW-файлы всегда блокируются («файл небезопасен») независимо от хендлера.
- BHID_PreviewHandler-тест невалиден на этой системе.
- Пользователь коммитит сам (GitHub Desktop autocommit «.»); коммитить/пушить при явной просьбе или для HANDOFF.
- Дальнейшие шаги могут требовать UAC (elevated) — пользователь не возражал, просто предупреждать.
