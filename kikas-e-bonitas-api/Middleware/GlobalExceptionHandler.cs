using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KikasEBonitas.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancelationToken)
    {
        // 1. Registar o erro nos logs do servidor
        _logger.LogError(exception, "Ocorreu uma exceção não tratada: {Message}", exception.Message);

        // 2. Montar a resposta padronizada (Problem Details)
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro Interno do Servidor",
            Detail = "Ocorreu um erro inesperado no servidor. Por favor, tente novamente mais tarde.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        // 3. Escrever o resultado JSON de volta na resposta HTTP
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancelationToken);

        // Retornar true indica ao .NET que o erro foi tratado com sucesso
        return true;
    }
}