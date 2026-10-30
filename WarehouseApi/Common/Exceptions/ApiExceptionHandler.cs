using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace WarehouseApi.Common.Exceptions;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (404, "Ресурс не знайдено"),
            ConflictException => (409, "Конфлікт даних"),
            DbUpdateConcurrencyException => (409, "Залишок уже змінено іншим запитом"),
            InvalidOperationException or ArgumentException => (400, "Порушено бізнес-правило"),
            _ => (500, "Внутрішня помилка сервера")
        };
        if (status == 500) logger.LogError(exception, "Не вдалося обробити {Path}", context.Request.Path);
        string detail = exception is DbUpdateConcurrencyException
            ? "Оновіть дані товару та повторіть операцію. Зміни не збережено."
            : status == 500 ? "Не вдалося зберегти зміни. Спробуйте пізніше." : exception.Message;
        await Results.Problem(statusCode: status, title: title, detail: detail, instance: context.Request.Path).ExecuteAsync(context);
        return true;
    }
}
