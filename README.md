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

1. **COM-превью** `WizardMD.Preview` — IPreviewHandler в Проводнике (**готово**, работает через prevhost.exe)
2. **Рендерер** `WizardMD.App` — WPF + WebView2, темы, подсветка (**готово**)
3. **Ядро-парсер** `WizardMD.Core` — блоки + inline + AST + HtmlRenderer + spec-тесты (**готово**, 81.0% CommonMark 0.30)

## Быстрый старт

```bash
dotnet build WizardMD.sln
dotnet test WizardMD.sln
```

## Документация

- [docs/CONTEXT.md](docs/CONTEXT.md) — состояние проекта
- [docs/DECISIONS.md](docs/DECISIONS.md) — архитектурные решения

## Статус

**v0.1.0** — все три шага готовы: ядро-парсер (CommonMark 81%), рендерер WPF+WebView2, COM-превью в Проводнике. Сборка зелёная, 87 тестов.

## Лицензия

[Apache-2.0](LICENSE) © Alexander Kuzikov