# CRM - Система управления взаимоотношениями с клиентами

Комплексное веб-приложение для управления деятельностью компании: клиентами, проектами, сделками, задачами, складом и коммуникациями.

## 📋 Основные возможности

### 👥 Управление пользователями и аккаунтами
- Регистрация и аутентификация через куки (7 дней)
- Интеграция с социальными сетями (VK, Yandex)
- Профили пользователей с настройками
- Система приглашений для новых пользователей
- Верификация устройств ("Запомнить устройство")

### 🏢 Управление компаниями
- Создание и редактирование компаний
- Управление контактами компаний
- Отслеживание данных клиентов

### 💼 Управление проектами и сделками
- Создание проектов с этапами
- Управление сделками (Sales)
- Отслеживание статусов и прогресса
- История изменений

### ✅ Управление задачами
- Создание и назначение задач
- Отслеживание сроков выполнения
- Приоритизация

### 📦 Управление складом
- Инвентаризация товаров
- Отслеживание запасов
- История движения товаров

### 📧 Email-коммуникации
- Отправка email-уведомлений
- История корреспонденции
- Шаблоны писем

### 🔄 Рабочие процессы
- Автоматизация бизнес-процессов
- Настройка workflows

### 📊 Аналитика и дашборд
- Личный дашборд пользователя
- Статистика по проектам и сделкам

### 🗑️ Управление архивом
- Корзина удаленных элементов
- Восстановление данных

---

## 🏗️ Архитектура проекта

### Многоуровневая структура

```
Crm.sln
├── Crm                    # 🖥️  Web приложение (MVC, Views, Controllers)
├── Crm.Application        # 🔌 Интеграция и прикладная логика
├── Crm.Core               # 💡 Основная бизнес-логика и сервисы
├── Crm.Domain             # 🎯 Доменные модели и интерфейсы
├── Crm.Entity             # 🗄️  Сущности БД и репозитории (EF)
└── Crm.Infrastructure     # ⚙️  Инфраструктурные сервисы
```

---

## 📁 Навигация по проекту

### **Crm/** — Веб-приложение
| Папка | Назначение |
|-------|-----------|
| `Controllers/` | MVC контроллеры для всех модулей |
| `Views/` | Razor представления для UI |
| `ViewComponents/` | Переиспользуемые компоненты |
| `Models/` | View-модели и DTO |
| `Services/` | Бизнес-сервисы |
| `Middleware/` | Кастомное ПО (сессии, куки) |
| `Extensions/` | Расширения для ASP.NET |
| `wwwroot/` | Статические файлы (CSS, JS, изображения) |

#### Контроллеры:
- **AccountController** → Аутентификация, вход/выход
- **HomeController** → Главная страница
- **CrmController** → Основной CRM функционал
- **DashboardController** → Личный дашборд
- **ProfileController** → Профиль пользователя
- **VkAuthController / YandexAuthController** → Социальная аутентификация
- **CrmSetupController** → Конфигурация системы

#### Подпапки контроллеров (модули):
- `Compan/` - Управление компаниями
- `Deals/` - Управление сделками
- `Email/` - Email-коммуникации
- `Nsi/` - Справочники
- `Online/` - Онлайн статусы
- `Project/` - Управление проектами
- `Sales/` - Продажи
- `Task/` - Управление задачами
- `Trash/` - Архив/корзина
- `User/` - Управление пользователями
- `Warehouse/` - Управление складом
- `Workflows/` - Автоматизация процессов

---

### **Crm.Application/** — Прикладной слой
| Файл | Назначение |
|------|-----------|
| `DependencyInjection.cs` | Регистрация сервисов в DI контейнер |
| `Interfaces/` | Контракты для интеграции |
| `Features/Accounts/` | Логика работы с аккаунтами |
| `Features/Email/` | Email-сервис |

---

### **Crm.Core/** — Ядро приложения
| Папка | Назначение |
|-------|-----------|
| `Features/` | Основная логика по модулям |
| `Interfaces/` | Контракты и интерфейсы |
| `Services/` | Бизнес-сервисы |
| `Implementations/` | Реализации сервисов |
| `ViewModels/` | DTO модели |
| `Infrastructure/` | Результаты операций, ошибки |

#### Ключевые сервисы:
- `IAuthenticationService` - Аутентификация
- `IUserContextService` - Контекст текущего пользователя
- `IEmailService` - Email отправка

---

### **Crm.Domain/** — Доменный слой
Содержит доменные модели и бизнес-правила (чистая архитектура).

---

### **Crm.Entity/** — ORM слой (Entity Framework)
| Папка | Назначение |
|-------|-----------|
| `Entities/` | Классы сущностей БД |
| `DTO/` | Трансфертные объекты |
| `Services/` | Репозитории и доступ к БД |
| `Infrastructure/` | Конфигурация EF |
| `ModelsCrm/` | Специфичные модели |

---

### **Crm.Infrastructure/** — Инфраструктура
Сервисы для работы с внешними системами, интеграции, логирования и т.д.

---

## 🔐 Ключевые технологии

- **Framework**: ASP.NET Core MVC
- **ORM**: Entity Framework Core
- **Authentication**: Cookie-based + Social (VK, Yandex)
- **Database**: SQL Server (по умолчанию)
- **Frontend**: Razor Views, CSS, JavaScript
- **Dependency Injection**: Built-in .NET DI

---

## ⚙️ Конфигурация и запуск

### Файлы конфигурации:
- `appsettings.json` - Основные настройки
- `appsettings.Development.json` - Настройки для разработки
- `launchSettings.json` - Профили запуска

### Запуск приложения:
```bash
dotnet run
```

Приложение запустится на http://localhost (порт зависит от launchSettings.json)

---

## 📝 Структура папок контроллеров

```
Controllers/
├── BaseController.cs           # Базовый класс с общей логикой
├── AccountController.cs        # Аутентификация
├── HomeController.cs           # Главная страница
├── DashboardController.cs      # Дашборд
├── ProfileController.cs        # Профиль пользователя
├── CrmController.cs            # Основной CRM
├── CrmSetupController.cs       # Настройки
├── TestController.cs           # Тестирование
├── VkAuthController.cs         # ВКонтакте
├── YandexAuthController.cs     # Яндекс
│
└── [Модули]
    ├── Compan/                 # Компании
    ├── Deals/                  # Сделки
    ├── Email/                  # Email
    ├── Nsi/                    # Справочники
    ├── Online/                 # Онлайн статусы
    ├── Project/                # Проекты
    ├── Sales/                  # Продажи
    ├── Task/                   # Задачи
    ├── Trash/                  # Корзина
    ├── User/                   # Пользователи
    ├── Warehouse/              # Склад
    └── Workflows/              # Workflow'ы
```

---

## 📋 Быстрая справка по файлам

### Старт приложения:
→ [Program.cs](Crm/Program.cs) - Конфигурация DI, аутентификация, middleware

### Главная страница:
→ [HomeController.cs](Crm/Controllers/HomeController.cs)
→ [Views/Home/](Crm/Views/Home/)

### Аутентификация:
→ [AccountController.cs](Crm/Controllers/AccountController.cs)
→ [AuthenticationService.cs](Crm.Core/Features/Account/Interfaces/IAuthenticationService.cs)
→ [Views/Account/](Crm/Views/Account/)

### Управление пользователями:
→ [UserService.cs](Crm/Services/UserService.cs)
→ [ApplicationUser.cs](Crm/Models/ApplicationUser.cs)
→ [Controllers/User/](Crm/Controllers/User/)

### Email:
→ [EmailService.cs](Crm.Core/Features/Email/Interfaces/IEmailService.cs)
→ [Controllers/Email/](Crm/Controllers/Email/)

### Компании:
→ [Controllers/Compan/](Crm/Controllers/Compan/)
→ [Views/Company/](Crm/Views/Company/)

### Проекты и сделки:
→ [Controllers/Project/](Crm/Controllers/Project/)
→ [Controllers/Deals/](Crm/Controllers/Deals/)
→ [Controllers/Sales/](Crm/Controllers/Sales/)

### Задачи:
→ [Controllers/Task/](Crm/Controllers/Task/)
→ [Views/Tasks/](Crm/Views/Tasks/)

### Склад:
→ [Controllers/Warehouse/](Crm/Controllers/Warehouse/)

---

## 🚀 Дальнейшее развитие

- [ ] Добавить тесты (Unit, Integration)
- [ ] Документация API
- [ ] Оптимизация производительности
- [ ] Кэширование
- [ ] Логирование всех операций
- [ ] Экспорт данных (Excel, PDF)

---

**Автор**: CRM Team  
**Версия**: 1.0  
**Дата обновления**: 01.09.2026
