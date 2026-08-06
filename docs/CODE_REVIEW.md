Ниже представлен подробный технический **Code Review** репозитория [WizardMD](https://github.com/AlexanderKuzikov/WizardMD).

---

# Code Review: WizardMD

### Общий обзор проекта

**WizardMD** — это WYSIWYG/Markdown-вьюер для Windows на .NET 8, состоящий из трех основных компонентов:

1. `WizardMD.Core` — кастомный AST-парсер Markdown и HTML-рендерер.
2. `WizardMD.App` — WPF-приложение для просмотра/редактирования Markdown с использованием `Microsoft.Web.WebView2`.
3. `WizardMD.Preview` — COM-компонент (Shell Preview Handler) для интеграции с панелью предпросмотра Проводника Windows (Windows Explorer) на базе WinForms `WebBrowser` (MSHTML).

---

## 1. Безопасность (Critical)

### 🔴 1.1 Отсутствие экранирования HTML (XSS Vulnerability)

* **Проблема**: В `HtmlRenderer.cs` текстовое содержимое узлов (Paragraph, Heading, List и т.д.) конкатенируется напрямую в итоговый HTML без применения HTML-экранирования (`HtmlEncode`).
* **Риск**: Если открыть файл `.md`, содержащий теги `<script>`, `<iframe>` или атрибуты `onerror` в тегах `<img>`, нефильтрованный HTML выполнится в контексте рендерера.
* В случае `WizardMD.Preview` код будет выполнен внутри процесса предпросмотра Проводника Windows (`prevhost.exe` / `explorer.exe`).


* **Решение**: Все текстовые узлы инлайнов и блоков должны экранироваться перед вставкой в `StringBuilder`:

```csharp
// Было
sb.Append(inlineText);

// Стало
sb.Append(System.Net.WebUtility.HtmlEncode(inlineText));

```

---

## 2. Windows Shell COM Integration & Multithreading (`WizardMD.Preview`)

### 🟠 2.1 Жизненный цикл STA-потока и утечки ресурсов в Проводнике

* **Проблема**: В `PreviewHandler.cs` для оборачивания WinForms `WebBrowser` запускается отдельный STA-поток (`_uiThread`). Проводник Windows при быстром переключении между файлами постоянно создает и уничтожает экземпляры `IPreviewHandler`.
* **Риск**: Если в методах `IPreviewHandler.Unload()` или `IUnknown.Release()` не вызывать принудительный вывод из цикла сообщений WinForms (`Application.ExitThread()`) и корректное закрытие формы `PreviewForm`, фоновые STA-потоки и хэндлы окон (`HWND`) останутся висеть в памяти `prevhost.exe`.
* **Решение**:
1. Реализовать интерфейс `IDisposable` для класса `PreviewHandler`.
2. В методе `Unload()` и `Dispose()` явно закрывать форму через `_form.BeginInvoke(new Action(() => _form.Close()))` и дожидаться завершения потока `_uiThread.Join(500)`.



### 🟡 2.2 Эмуляция IE7 в легаси-компоненте `WebBrowser` (MSHTML)

* **Проблема**: Элемент управления `System.Windows.Forms.WebBrowser` по умолчанию использует эмуляцию Internet Explorer 7 (Quirks Mode). Современный CSS (Flexbox, Grid, CSS Variables) в нем не отрендерится.
* **Решение**: В генерируемый HTML-шаблон (внутри `HtmlPage.cs` или аналогичного генератора обертки) обязательно должен внедряться мета-тег:

```html
<head>
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta charset="utf-8" />
</head>

```

---

## 3. Производительность и парсинг (`WizardMD.Core`)

### 🟡 3.1 Аллокации памяти при рендеринге HTML

* **Проблема**: В `HtmlRenderer.cs` создается `StringBuilder` без указания начальной емкости. При рендеринге крупных документов происходят постоянные переаллокации внутреннего буфера `StringBuilder`.
* **Решение**: Задавать начальный размер буфера на основе длины исходного текста Markdown:

```csharp
public static string Render(Document doc, int estimatedLength = 1024)
{
    var sb = new StringBuilder(estimatedLength * 2);
    foreach (var block in doc.Blocks) 
    {
        RenderBlock(block, sb);
    }
    return sb.ToString();
}

```

### 🟡 3.2 Соответствие спецификации CommonMark

* **Проблема**: Написание кастомного Markdown-парсера с нуля (`BlockParser.cs` и `InlineParser.cs`) сопряжено с рисками некорректной обработки сложных грамматик (вложенные списки, экранирование символов, сочетания курсива и жирного шрифта, ReDoS-уязвимости при поиске парных символов).
* **Решение**: Добавить в тестовый проект (`tests/`) автоматический запуск офциального [CommonMark Spec Test Suite](https://spec.commonmark.org/) для проверки полноты и корректности парсинга edge-cases.

---

## 4. Архитектура приложения (`WizardMD.App`)

### 🟢 4.1 Изоляция WebView2

* **Плюс**: Использование `WebView2` в WPF-приложении `WizardMD.App` и замена его на легкий MSHTML в `WizardMD.Preview` — архитектурно правильное решение. WebView2 требует сложного межпроцессного взаимодействия и не подходил бы для легкого хэндлера предпросмотра в Проводнике.
* **Рекомендация**: При передаче HTML в WebView2 использовать `NavigateToString()` или виртуальный mapping папок (`SetVirtualHostNameToFolderMapping`), чтобы избежать записи временных HTML-файлов на диск.

---

## Итоговый чек-лист улучшений

1. **[P0] Security**: Добавить `System.Net.WebUtility.HtmlEncode` для всех текстовых элементов в `HtmlRenderer`.
2. **[P1] Stability**: Проверить и протестировать корректное завершение STA-потоков в `PreviewHandler.Unload()` при быстром про протестировать корректное завершение STA-потоков в `PreviewHandler.Unload()` при быстром пролистывании файлов в Windows Explorer.
3. **[P1] Rendering**: Добавить мета-тег `<meta http-equiv="X-UA-Compatible" content="IE=edge" />` в HTML-контейнер для предпросмотра.
4. **[P2] Performance**: Задать начальную емкость для `StringBuilder` в `HtmlRenderer`.
5. **[P2] Testing**: Добавить интеграционное тестирование парсера на базе спецификации CommonMark.