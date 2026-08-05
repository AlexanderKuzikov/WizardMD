<p align="center">
  <a href="#"><img alt="C#" src="https://img.shields.io/badge/C%23-.NET_8-512BD4?logo=dotnet&logoColor=white"></a>
  <a href="LICENSE"><img alt="License" src="https://img.shields.io/badge/License-Apache_2.0-blue.svg"></a>
</p>

<h1 align="center">WizardMD</h1>
<p align="center">WYSIWYG-просмотрщик Markdown для Windows: рендерер, Explorer-интеграция, COM-превью</p>

---

Просмотрщик Markdown со своей дорожной картой: от парсера-ядра до превью-панели в Проводнике Windows (как у PDF). Стек — C#/.NET единый: ядро на netstandard2.0, рендерер на WPF+WebView2, COM-хендлер на .NET Framework 4.8.

- **Парсер-ядро** — CommonMark+GFM, архитектура под официальный spec, переиспользуется всеми уровнями
- **Рендерер** — WPF + WebView2, светлая/тёмная темы, подсветка синтаксиса
- **Ассоциация .md** — двойной клик по файлу → мгновенный рендер
- **COM-превью** — панель предпросмотра в Проводнике (цель, шаг 3)

## Дорожная карта

3. **Ядро-парсер** `WizardMD.Core` — блоки + inline + AST + HtmlRenderer + spec-тесты (в работе)
2. **Рендерер** `WizardMD.App` — WPF + WebView2, темы, подсветка
1. **COM-превью** `WizardMD.Preview` — IPreviewHandler в Проводнике

## Быстрый старт

```bash
dotnet build WizardMD.sln
dotnet test WizardMD.sln
```

## Документация

- [docs/CONTEXT.md](docs/CONTEXT.md) — состояние проекта
- [docs/DECISIONS.md](docs/DECISIONS.md) — архитектурные решения

## Статус

**v0.1.0** — каркас решения, окружение готово (SDK 8.0.423, WebView2 151). Начало шага 3.

## Лицензия

[Apache-2.0](LICENSE) © Alexander Kuzikov