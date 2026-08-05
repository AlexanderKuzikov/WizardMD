# WizardMD — CONTEXT

> Последнее обновление: 2026-08-05

## Статус
| Компонент | Статус | Версия/Заметка |
|-----------|--------|----------------|
| Окружение | готово | .NET SDK 8.0.423 (`%LOCALAPPDATA%\dotnet`, PATH user), WebView2 151.0.4129.59, WPF templates |
| Решение | создано | `WizardMD.sln`: WizardMD.Core (netstandard2.0), WizardMD.App (net8.0-windows WPF), WizardMD.Core.Tests (xUnit). Сборка зелёная, 0 ошибок |
| Ядро-парсер | не начато | Шаг 3 — приоритет. Структура пустая (Class1 удалён) |
| Рендерер | не начато | Шаг 2 — WPF + WebView2 |
| COM-превью | не начато | Шаг 1 — net48, IPreviewHandler |
| GitHub | подготовлен | Репо создаётся при первом коммите |

## Open-проблемы
| # | Priority | Описание |
|---|----------|----------|
| 1 | high | Ядро-парсер: реализовать BlockParser + InlineParser + AST + HtmlRenderer (подмножество CommonMark+GFM) |
| 2 | med | Spec-тесты: скачать `spec.json` (CommonMark), настроить прогон, целевой ориентир 80-90% |
| 3 | med | Golden-тесты на реальных .md (README, CONTEXT, knowledge-файлы) |
| 4 | low | Рендерер: подключить WebView2 NuGet, темы light/dark, подсветка синтаксиса |
| 5 | low | Ассоциация .md → WizardMD.App.exe (реестр, `--register`) |
| 6 | low | COM-хендлер: исследовать рендер без WebView2 (WebBrowser legacy vs GDI) — решение перед реализацией |

## Журнал работ
| Дата | Изменение |
|------|-----------|
| 2026-08-05 | Создание WizardMD: окружение (SDK 8.0.423 user install + PATH, WebView2 есть), решение (Core netstandard2.0 / App WPF net8.0 / Tests xUnit), сборка зелёная. ADR-001: стек. План: 3→2→1 |

## Структура проекта
```
WizardMD/
├── README.md
├── AGENTS.md
├── WizardMD.sln
├── src/
│   ├── WizardMD.Core/      # ядро-парсер, netstandard2.0
│   └── WizardMD.App/       # WPF + WebView2 рендерер, net8.0-windows
├── tests/
│   └── WizardMD.Core.Tests/  # xUnit
└── docs/
    ├── CONTEXT.md
    └── DECISIONS.md
```