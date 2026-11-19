# MyLab.Opensearch

Ознакомьтесь с последними изменениями в [журнале изменений](/CHANGELOG.md).

[![NuGet](https://img.shields.io/nuget/v/MyLab.Opensearch.svg)](https://www.nuget.org/packages/MyLab.Opensearch/)

## Обзор

Библиотека для `.NET 10`. Подключает клиент [OpenSearch.Client](https://github.com/opensearch-project/opensearch-net) к приложению и проверяет доступность узлов.

После регистрации в контейнере зависимостей доступен `IOpenSearchClient`. Это клиент OpenSearch: через него выполняются запросы к кластеру.

## Интеграция

`AddOpenSearch` регистрирует `IOpenSearchClient` как одиночный сервис. Клиент создаётся при первом получении из контейнера.

```csharp
services
    .AddOpenSearch()
    .ConfigureOpenSearch(Configuration);
```

Настройки по умолчанию читаются из секции `OpenSearch`. Имя секции можно заменить:

```csharp
services.ConfigureOpenSearch(Configuration, "Search");
```

Те же параметры задаются в коде:

```csharp
services.ConfigureOpenSearch(options =>
{
    options.Host = "http://localhost:9200";
});
```

Пример использования:

```csharp
public class OrderSearch(IOpenSearchClient client)
{
    public async Task<IReadOnlyCollection<Order>> FindAsync(string text, CancellationToken cancellationToken)
    {
        var response = await client.SearchAsync<Order>(search => search
            .Index("orders")
            .Query(query => query
                .Match(match => match
                    .Field(order => order.Title)
                    .Query(text))),
            cancellationToken);

        return response.Documents;
    }
}
```

Если адрес подключения не задан или не разбирается как URI, создание клиента завершается ошибкой. Когда в контейнере зарегистрирован `ILogger`, ошибка записывается в журнал.

## Конфигурация

```json
{
  "OpenSearch": {
    "Host": "http://localhost:9200",
    "HostPool": [
      "http://node-a:9200",
      "http://node-b:9200"
    ]
  }
}
```

Секция по умолчанию называется `OpenSearch`. Должен быть указан один из параметров подключения: `Host` или `HostPool` (игнорируется, если указан `Host`). В противном случае возникнет ошибка при попытке получить `IOpenSearchClient`.

### `Host`

Абсолютный URI одного узла, например `http://localhost:9200`. Заданное значение выбирает пул с одним узлом.

Если заданы и `Host`, и `HostPool`, используется `Host`.

### `HostPool`

Список абсолютных URI узлов. Используется, когда `Host` не задан, и содержит хотя бы один адрес. Узлы образуют статический пул соединений.

## Проверка работоспособности

Проверка называется `opensearch`. Она получает `IOpenSearchClient` и опрашивает каждый узел пула. Опрос одного узла ждёт ответ до 5 секунд и не повторяется.

Результат успешный, когда ответили все узлы. Результат деградировавший, когда ответила только часть узлов: в описании указано, сколько узлов доступно из общего числа. Результат неуспешный, когда не ответил ни один узел.

При одном узле в `Host` возможны только успешный и неуспешный результаты. Деградация появляется при пуле из нескольких узлов.

```csharp
services.AddHealthChecks().AddOpenSearch();
```
