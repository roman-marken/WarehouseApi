# WarehouseApi — домашнє завдання 15.1

.NET 10, EF Core SQLite, Scalar та Pure CQRS. MediatR, dispatcher і сторонні CQRS-бібліотеки не використовуються.
Оскільки вихідний ProjectForStudents/WarehouseApi не був наданий, тут створено окремий самодостатній проєкт із базовими Products/StockTransactions та розширенням Suppliers.

## Запуск

Із папки рішення:

```powershell
dotnet run --project WarehouseApi
```

Відкрийте **https://localhost:7151/scalar/v1**.
OpenAPI: **https://localhost:7151/openapi/v1.json**.
База `WarehouseApi/warehouse.db` створюється та оновлюється автоматично через міграції. Спочатку каталог і список постачальників порожні.
Зупинка сервера — Ctrl+C. Локальний процес не потрібно запускати вдруге на тому самому порту.

## Архітектура Pure CQRS

```text
Common/Cqrs/
  ICommand<TResult>, ICommandHandler<TCommand, TResult>
  IQuery<TResult>, IQueryHandler<TQuery, TResult>
Domain/Entities/
  Product, Supplier, StockTransaction
Data/
  WarehouseDbContext, WarehouseDbContextFactory
Features/Products/
  DTOs/
  Queries/PureGetProductsList/, PureGetProductById/
  Commands/PureCreateProduct/, PureUpdateStock/, PureDeleteProduct/
Features/Suppliers/
  DTOs/
  Queries/GetSuppliersList/, GetSupplierById/
  Commands/CreateSupplier/, RestockProductFromSupplier/
Features/StockTransactions/
  Додатковий read-only запит історії
Controllers/
  PureCqrsDemoController, SuppliersController, StockTransactionsController
```

Запити читають через AsNoTracking і SQL-проекції в DTO; вони не викликають SaveChanges.
Команди перевіряють бізнес-правила та змінюють стан. Повернення DTO або bool відповідає контрактам завдання.
Кожен хендлер явно зареєстрований як AddScoped у Program.cs.
Конструктор контролера товарів має 5 хендлерів, постачальників — 4: це навмисна демонстрація Constructor Explosion перед вивченням MediatR.

## Перевірка сценарію 10 → 35 у Scalar

### 1. Створіть товар

`POST /api/pure-cqrs-demo`:

```json
{
  "name": "Ноутбук",
  "sku": "LAP-001",
  "category": "Електроніка",
  "description": "Навчальний приклад складського товару",
  "price": 35000,
  "stockQuantity": 10
}
```

Отримаєте 201 та ID товару. На свіжій базі це 1.
SKU нормалізується до верхнього регістру й має унікальний індекс.
Початковий ненульовий залишок також фіксується в історії.

### 2. Створіть постачальника

`POST /api/suppliers`:

```json
{
  "companyName": "ТОВ Електропостач",
  "edrpouCode": "41234567",
  "contactPerson": "Олена Коваль",
  "phone": "+380671234567",
  "email": "office@example.com",
  "address": "Київ, вул. Складська, 1"
}
```

Отримаєте 201 та ID постачальника. Повторне створення того самого коду поверне 409.
Код зберігається рядком: початкові нулі не губляться. Дозволено 8–10 цифр.
Новий постачальник завжди активний.

### 3. Оформіть поставку

`POST /api/suppliers/restock` — підставте ID з попередніх відповідей:

```json
{
  "supplierId": 1,
  "productId": 1,
  "quantity": 25,
  "purchasePrice": 125.50,
  "invoiceNumber": "INV-2026/09-44"
}
```

Результат: 200, `stockQuantity: 35`, оновлений UpdatedAt у UTC.
Закупівельна ціна потрапляє в опис руху; продажна Price товару не змінюється.

### 4. Перевірте товар та історію

- `GET /api/pure-cqrs-demo/1` — залишок 35.
- `GET /api/stock-transactions?productId=1` — початкові 10 одиниць та поставка +25 із назвою компанії, накладною й закупівельною ціною.

### 5. Ручна зміна залишку

`PATCH /api/pure-cqrs-demo/1/stock`:

```json
{ "quantityChange": -5, "reason": "Відвантаження клієнту" }
```

Результат: залишок 30 і ще один запис історії. Списання більше наявної кількості повертає 400 без зміни товару або історії.
Порожня причина, нульова зміна та переповнення int теж відхиляються.

## Ендпоінти та фільтри

| Метод | Маршрут | Дія |
| --- | --- | --- |
| GET | /api/pure-cqrs-demo | Список: searchTerm, category, onlyInStock |
| GET | /api/pure-cqrs-demo/{id} | Один товар |
| POST | /api/pure-cqrs-demo | Створення товару |
| PATCH | /api/pure-cqrs-demo/{id}/stock | Зміна залишку з причиною |
| DELETE | /api/pure-cqrs-demo/{id} | Видалення товару та пов’язаної історії, 204 |
| GET | /api/suppliers | Пошук: searchTerm; onlyActive=true типово |
| GET | /api/suppliers/{id} | Один постачальник |
| POST | /api/suppliers | Створення з перевіркою унікальності коду |
| POST | /api/suppliers/restock | Поставка й атомарне оновлення історії |
| GET | /api/stock-transactions?productId=1 | Додатковий запит історії для Scalar |

`onlyInStock=true` залишає StockQuantity > 0; false або відсутній параметр показує всі товари.
`onlyActive=false` показує активних і неактивних постачальників.
Пошук за назвою — підрядок із урахуванням регістру; SKU шукається незалежно від регістру. Категорія — точне значення.
Списки сортовані за CreatedAt спадно, а для однакового часу — за ID спадно.
Неактивний постачальник може бути прочитаний за ID, але оформити від нього поставку неможливо.
Статус IsActive передбачений для наявних даних; окремого ендпоінта редагування постачальника умова не вимагає.

## Узгодженість даних

- Залишок і StockTransaction зберігаються одним SaveChangesAsync — у спільній автоматичній транзакції EF Core.
- Якщо вставлення історії не вдається, оновлення залишку відкочується.
- Product.Version — concurrency token: застаріла паралельна команда отримує 409, а не перезаписує новий залишок. Її історія також не зберігається.
- Унікальні індекси захищають SKU та ЄДРПОУ не лише перевіркою перед вставленням, а й у самій БД.
- Дати створення, оновлення й руху передаються в UTC. Грошові значення — decimal; межі валідації не залежать від регіональних налаштувань Windows.
- Очікувані помилки повертаються як ProblemDetails: 400 для бізнес-правил, 404 для відсутніх ресурсів, 409 для дублікатів або конкурентної зміни.

## Міграції

У проєкті є `InitialWarehouse` і окрема `AddSuppliersTable`.
Для ручного застосування:

```powershell
dotnet tool restore
dotnet ef database update --project WarehouseApi
```

Для наступних змін:

```powershell
dotnet ef migrations add НазваЗміни --project WarehouseApi
```

## Тести

```powershell
dotnet test WarehouseApi.sln
```

Перевірено **26 тестових випадків**, 0 помилок і 0 пропусків:

- сценарій 10 + 25 = 35 та історія накладної;
- пошук, фільтри, сортування, CRUD і списання до нуля;
- дублікат SKU/ЄДРПОУ, формати кодів і початкові нулі;
- відсутній або неактивний постачальник, некоректні суми й кількість;
- відсутність tracked-сутностей після виконання Queries;
- реальний транзакційний відкат при помилці вставлення історії;
- захист від застарілої паралельної зміни залишку;
- застосування AddSuppliersTable зі збереженням наявних товарів та перевіркою унікального індексу.

Scalar та сценарій поставки також перевірені через справжній Kestrel і браузер.
Готові HTTP-запити — у `WarehouseApi/WarehouseApi.http`.
