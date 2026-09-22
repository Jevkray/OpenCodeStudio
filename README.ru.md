<div align="center">

<img src="https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewImage.png" width="240" alt="OpenCode Studio" />

# 🟣 OpenCode Studio

**OpenCode-агент — прямо внутри Visual Studio.**

*Быстрое, самодостаточное окно-инструмент с панелью статистики, мастером переноса при первом запуске и общей историей чатов.*

[![Marketplace](https://img.shields.io/visual-studio-marketplace/v/jevkray.OpenCodeStudio?label=marketplace&color=7C3AED)](https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio)
[![Установки](https://img.shields.io/visual-studio-marketplace/i/jevkray.OpenCodeStudio?color=7C3AED)](https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio)
[![Рейтинг](https://img.shields.io/visual-studio-marketplace/r/jevkray.OpenCodeStudio?color=7C3AED)](https://marketplace.visualstudio.com/items?itemName=jevkray.OpenCodeStudio)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-7C3AED)
![Лицензия](https://img.shields.io/badge/license-MIT-7C3AED)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.7.2-7C3AED)

**[English](README.md) · [Русский](README.ru.md)**

</div>

---

> [!TIP]
> **Одно окружение — везде.** OpenCode Studio использует те же данные, что OpenCode CLI и desktop-приложение — провайдеры, настройки, ключи и **все чаты** уже на месте. Никакой настройки и дублирования.

## 🆕 Что нового

- **1.1.4** - Вход надёжно возвращает в приложение (событийный захват cookie).
- **1.1.3** - Вход возвращает в приложение и автоматически открывает панель использования.
- **1.1.2** - Поповер не выходит за границы; вход завершается в панели (OAuth внутри встроенного браузера).
- **1.1.1** - Кнопка использования в футере OpenCode с поповером (вход, загрузка, инфографика).
- **1.1.0** - Оверлей использования: остаток лимитов Go и расход за сегодня прямо в панели OpenCode, плюс окно инфографики.

## ✨ Почему OpenCode Studio?

| | |
|---|---|
| 🧠 | **Настоящий OpenCode, а не клон** — полный официальный веб-UI прямо в Visual Studio. |
| 🔄 | **Общая история** — те же чаты, что в CLI и desktop, во всех окнах Visual Studio. |
| 🗂️ | **Видны все чаты** — открывается главная веб-UI со списком всех проектов и сессий. |
| 📊 | **Панель статистики** — расход, токены и запросы с графиками в стиле OpenCode. |
| 📥 | **Мастер при первом запуске** — импорт настроек, тем, ключей и истории в один клик. |
| 🔁 | **Авто-переподключение** — само восстанавливает сессию при обрыве связи. |
| 🔒 | **Локально** — только loopback, ноль телеметрии. |
| 🪶 | **Лёгкий** — один VSIX для Visual Studio 2022 и 2026. |

## 📸 Превью

![OpenCode Studio прямо внутри Visual Studio](https://raw.githubusercontent.com/Jevkray/OpenCodeStudio/main/vs/Resources/PreviewWork.jpg)

> 💬 *Чат с агентом рядом с решением — и живая панель сессии с моделью, провайдером, расходом токенов, стоимостью, последней активностью и разбивкой контекста.*

## 🚀 Быстрый старт

```text
  Вид      →  Другие окна          →  OpenCode Studio        🔹 агент
  Сервис   →  OpenCode Studio Usage Statistics               📊 расход и токены
  Сервис   →  OpenCode Studio Import Settings...             📥 мастер настройки
```

> [!NOTE]
> **Требование:** [OpenCode CLI](https://opencode.ai) должен быть установлен и доступен в `PATH`.

<details>
<summary><b>📦 Установка</b></summary>

**Из Visual Studio Marketplace (рекомендуется)**

1. В Visual Studio откройте **Расширения → Управление расширениями**.
2. Найдите **OpenCode Studio** и установите.
3. Перезапустите Visual Studio.

**Из VSIX-файла**

1. Скачайте последний `OpenCodeStudio-x.y.z.vsix` из [Releases](https://github.com/Jevkray/OpenCodeStudio/releases).
2. Закройте все окна Visual Studio и дважды кликните по файлу.

</details>

## 🔗 Синхронизация истории чатов

OpenCode хранит каждую сессию **в привязке к каталогу проекта**, в котором она создана. Поскольку OpenCode Studio использует те же данные, что CLI и desktop, все чаты уже доступны — но сгруппированы по пути проекта.

> [!IMPORTANT]
> Если проект находится по **другому абсолютному пути**, чем при создании чатов (другой диск, другое имя пользователя, перемещённая или переименованная папка), сессии останутся привязаны к исходному пути. **Приведите пути проектов в соответствие — и всё синхронизируется.**

1. Открывайте каждый проект по **тому же абсолютному пути**, что использовался изначально — например `C:\Users\<вы>\source\repos\MyApp`.
2. Если путь изменился — верните проект на место либо перенесите старые данные через **Сервис → OpenCode Studio Import Settings...**, указав папку с исходным `opencode.db`.
3. Переоткройте окно-инструмент — сессии появятся у нужного проекта.

Как только пути совпадают, **все чаты синхронизируются** между CLI, desktop-приложением и всеми окнами Visual Studio. 🎉

## 📊 Статистика

> [!NOTE]
> **Сервис → OpenCode Studio Usage Statistics** открывает **локальную** панель на данных API OpenCode — без сторонних сервисов.

| 📈 Панель | Что показывает |
|---|---|
| 💰 KPI-карточки | Общий расход, токены, сессии, запросы |
| 🗓️ Расход по дням | Плавный график площади |
| 🍩 Токены по типам | Input / output / reasoning / cache |
| 🤖 Расход по моделям | Затраты по каждой модели |
| 🔍 Расход на запрос | Для текущей сессии |
| 📋 Таблица сессий | Модель, агент, токены и стоимость |

## 🧱 Архитектура

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
 │    └── открывает http://127.0.0.1:<port>/ (главная веб-UI)   │
 ├──────────────────────────────────────────────────────────────┤
 │  UsageStatsWindow                 панель статистики          │
 │  FirstRunWindow                   мастер переноса            │
 │  ImportService / OpenCodeEnvironment / Log                   │
 └──────────────────────────────────────────────────────────────┘
```

> [!TIP]
> **Принципы дизайна**
> - **Без прокси** — WebView2 работает с реальным адресом сервера, интерфейс истинный и не ломается при обновлениях.
> - **Общие данные** — запуск в вашем стандартном окружении, синхронно с CLI и desktop.
> - **Один сервер на машину** — все окна Visual Studio используют один сервер OpenCode.
> - **Локально по умолчанию** — только loopback, исходящих запросов нет, кроме ваших провайдеров.

## 🧩 Требования

| Компонент | Версия |
|---|---|
| 🖥️ Visual Studio | 2022 (17.0) – 2026 |
| ⚡ OpenCode CLI | установлен и в `PATH` |
| 🛠️ .NET SDK *(сборка из исходников)* | .NET 10 |
| 🌐 WebView2 Runtime | входит в Windows 10/11 и VS |

## 🔨 Сборка из исходников

```bash
git clone https://github.com/Jevkray/OpenCodeStudio.git
cd OpenCodeStudio
dotnet build vs/OpenCodeStudio.csproj -c Release
```

VSIX появится в `vs/bin/Release/net472/OpenCodeStudio.vsix`. Для отладки откройте `OpenCodeStudio.sln` и нажмите **F5** (экспериментальный экземпляр). 🧪

## 🛡️ Приватность

- 🖧 сервер слушает только `127.0.0.1`
- 🔑 ключи хранит сам OpenCode в вашем окружении
- 🚫 нет телеметрии и аналитики
- 📊 панель статистики читает данные с локального сервера
- 📈 оверлей использования входит в OpenCode Console и читает расход и лимиты с `opencode.ai` по вашей сессионной cookie (хранится зашифрованной через DPAPI) — другие данные наружу не уходят.

## 💬 Контакты и поддержка

<div align="center">

[![Telegram](https://img.shields.io/badge/Telegram-@eugenekray-2CA5E0?logo=telegram&logoColor=white)](https://t.me/eugenekray)
[![Почта](https://img.shields.io/badge/Email-krasovskyworks@gmail.com-EA4335?logo=gmail&logoColor=white)](mailto:krasovskyworks@gmail.com)
[![Boosty](https://img.shields.io/badge/Поддержать-Boosty-FF6A00?logo=boosty&logoColor=white)](https://boosty.to/jevkray)
[![Issues](https://img.shields.io/badge/Issues-GitHub-181717?logo=github&logoColor=white)](https://github.com/Jevkray/OpenCodeStudio/issues)

</div>

## 📜 Лицензия

Распространяется по лицензии [MIT](LICENSE).

<div align="center"><sub>Создано для сообщества OpenCode · Не связано с OpenCode или Microsoft.</sub></div>
