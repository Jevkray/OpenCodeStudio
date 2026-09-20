<p align="center">
  <img src="https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewImage.png" width="240" alt="OpenCode Studio" />
</p>

<h1 align="center">OpenCode Studio</h1>

<p align="center">
  <b>OpenCode-агент, встроенный прямо в Visual Studio.</b><br/>
  Быстрое, самодостаточное окно-инструмент с панелью статистики и мастером переноса настроек при первом запуске.
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio"><img src="https://img.shields.io/visual-studio-marketplace/v/jevkray.OpenCodeStudio?label=marketplace&color=7C3AED" alt="Marketplace"></a>
  <a href="https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio"><img src="https://img.shields.io/visual-studio-marketplace/i/jevkray.OpenCodeStudio?color=7C3AED" alt="Установки"></a>
  <img src="https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-7C3AED" alt="Visual Studio 2022 / 2026">
  <img src="https://img.shields.io/badge/license-MIT-7C3AED" alt="Лицензия: MIT">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.7.2-7C3AED" alt=".NET Framework 4.7.2">
</p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="README.ru.md">Русский</a>
</p>

---

```text
  ╭──────────────────────────────────────────────────────────────╮
  │                                                              │
  │    O P E N C O D E   S T U D I O                             │
  │                                                              │
  │    OpenCode прямо внутри Visual Studio                       │
  │    Тёмно-фиолетовый. Локально. Без настройки.                │
  │                                                              │
  ╰──────────────────────────────────────────────────────────────╯
```

## Обзор

**OpenCode Studio** встраивает полноценный веб-интерфейс [OpenCode](https://opencode.ai) в окно-инструмент Visual Studio через WebView2. В отличие от интеграции через прокси, расширение направляет браузер **напрямую на сервер OpenCode, который запускает само** — меньше движущихся частей, меньше ошибок и синхронность с каждым релизом OpenCode.

Всё работает **локально** на `127.0.0.1`. Никаких аккаунтов, телеметрии и передачи данных наружу.

## Возможности

| | Возможность | Описание |
|---|-------------|----------|
| 🧩 | **Нативный веб-UI** | Полный интерфейс OpenCode (проекты, сессии, вкладки, темы) отображается напрямую — без прослоек и внедряемых скриптов. |
| 🗂️ | **Сессии по проектам** | Определяет корень решения / Git и автоматически открывает нужную сессию OpenCode. |
| 📊 | **Панель статистики** | Отдельное окно с расходом, токенами, запросами и графиками в стиле OpenCode, с живым обновлением. |
| 📥 | **Мастер переноса при первом запуске** | Находит все установки OpenCode (CLI, Desktop, папка) и переносит настройки, темы, ключи и историю чатов в один клик. |
| 🔒 | **Изолированное окружение** | Работает в собственном окружении `%LOCALAPPDATA%\OpenCodeStudio` и не меняет ваши глобальные настройки CLI/Desktop. |
| 🔁 | **Авто-переподключение** | При обрыве связи окно показывает страницу прогресса и само восстанавливает сессию. |
| 🎨 | **Стиль Visual Studio** | Страницы загрузки и ошибок подстраиваются под цветовую тему Visual Studio. |
| 🧾 | **Логирование в файл** | Диагностика пишется в `%LOCALAPPDATA%\OpenCodeStudio\logs\opencode-studio.log`. |
| ✅ | **VS 2022 и 2026** | Один VSIX для обеих версий. |

## Установка

**Из Visual Studio Marketplace (рекомендуется)**

1. В Visual Studio откройте **Расширения → Управление расширениями**.
2. Найдите **OpenCode Studio** и установите.
3. Перезапустите Visual Studio.

**Из VSIX-файла**

1. Скачайте последний `OpenCodeStudio-x.y.z.vsix` из [Releases](https://github.com/Jevkray/OpenCodeStudio/releases).
2. Закройте все окна Visual Studio и дважды кликните по файлу.

> **Требование:** [OpenCode CLI](https://opencode.ai) должен быть установлен и доступен в `PATH`.

## Начало работы

```text
  Вид      → Другие окна → OpenCode Studio        (агент)
  Сервис   → OpenCode Studio Usage Statistics     (расход и токены)
  Сервис   → OpenCode Studio Import Settings...   (мастер настройки)
```

При **первом запуске** мастер настройки открывается автоматически. Он находит данные OpenCode на машине и предлагает перенести нужное:

```text
  ╭──────────────────────────────────────────────────────────────╮
  │  Импорт из                                                   │
  ├──────────────────────────────────────────────────────────────┤
  │  ■ OpenCode CLI / Desktop (global)   opencode.json, auth, db │
  │  ■ OpenCode Studio environment        текущее окружение       │
  │  ■ OpenCode Desktop                   настройки, черновики    │
  │  ■ Current project (.opencode)        конфиг проекта          │
  │  ■ Custom folder...                   бэкап или другой ПК     │
  ╰──────────────────────────────────────────────────────────────╯
```

Каждая перезапись сохраняется в бэкап с суффиксом `.bak-<время>`, поэтому ничего не теряется. Мастер можно снова открыть из меню **Сервис**.

## Статистика

Откройте **Сервис → OpenCode Studio Usage Statistics** — локальная панель на данных API OpenCode, без сторонних сервисов:

- **KPI-карточки** — общий расход, токены, сессии и запросы.
- **Расход по дням** — плавный график площади.
- **Токены по типам** — input / output / reasoning / cache в кольцевой диаграмме.
- **Расход по моделям** и **расход на запрос** для текущей сессии.
- **Таблица сессий** с моделью, агентом, токенами и стоимостью.

## Архитектура

Ядро OpenCode Studio намеренно компактное и разделённое.

```text
  ┌──────────────────────────────────────────────────────────────┐
  │  OpenCodeStudioPackage            пакет VS и команды         │
  ├──────────────────────────────────────────────────────────────┤
  │  ServerController                жизненный цикл сервера      │
  │    ├── OpenCodeServerService      запускает `opencode serve` │
  │    ├── OpenCodeSessionService     API /session, /project     │
  │    ├── ConnectionMonitor          периодические проверки     │
  │    └── ProcessBinding             завершается вместе с VS    │
  ├──────────────────────────────────────────────────────────────┤
  │  OpenCodeToolWindowControl        хост WebView2              │
  │    └── переход на http://127.0.0.1:<port>/{dir}/session      │
  ├──────────────────────────────────────────────────────────────┤
  │  UsageStatsWindow                 панель статистики          │
  │  FirstRunWindow                   мастер переноса            │
  │  ImportService / OpenCodeEnvironment / Log                   │
  └──────────────────────────────────────────────────────────────┘
```

**Принципы дизайна**

- **Без прокси.** WebView2 работает с реальным адресом сервера — интерфейс всегда настоящий и не ломается при обновлениях.
- **Изолированные данные.** OpenCode запускается со своими каталогами `XDG_*` в `%LOCALAPPDATA%\OpenCodeStudio\opencode`.
- **Чёткие границы сервисов.** Жизненный цикл сервера, API сессий, мониторинг и импорт — отдельные, тестируемые сервисы.
- **Локально по умолчанию.** Привязка к loopback, исходящих запросов нет, кроме настроенных вами провайдеров.

## Требования

| Компонент | Версия |
|-----------|--------|
| Visual Studio | 2022 (17.0) – 2026 |
| OpenCode CLI | установлен и в `PATH` |
| .NET SDK (сборка из исходников) | .NET 10 |
| WebView2 Runtime | входит в Windows 10/11 и VS |

## Сборка из исходников

```bash
git clone https://github.com/Jevkray/OpenCodeStudio.git
cd OpenCodeStudio
dotnet build vs/OpenCodeStudio.csproj -c Release
```

VSIX появится в `vs/bin/Release/net472/OpenCodeStudio.vsix`.
Для отладки откройте `OpenCodeStudio.sln` в Visual Studio и нажмите **F5** (экспериментальный экземпляр).

## Приватность

OpenCode Studio — **локальное** расширение:

- сервер слушает только `127.0.0.1`;
- ключи хранит сам OpenCode в вашем окружении;
- расширение не отправляет телеметрию и не содержит аналитики;
- панель статистики читает данные с локального сервера.

## Лицензия

Распространяется по лицензии [MIT](LICENSE).

<p align="center"><sub>Создано для сообщества OpenCode · Не связано с OpenCode или Microsoft.</sub></p>
