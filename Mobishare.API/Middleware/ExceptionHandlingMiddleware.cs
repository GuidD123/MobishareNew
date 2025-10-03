using System.Net;
using System.Text.Json;
using Mobishare.Core.Exceptions;
using Mobishare.Core.DTOs;

namespace Mobishare.API.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // esegue il prossimo middleware/controller
            }
            catch (Exception ex)
            {
                // Genera un TraceId univoco
                var traceId = Guid.NewGuid().ToString();

                _logger.LogError(ex, "Errore catturato dal middleware");

                context.Response.ContentType = "application/json";

                // Valori default
                var statusCode = (int)HttpStatusCode.InternalServerError;

                object response = new
                {
                    Errore = "Errore interno al server",
                    TraceId = traceId
                };

                // Mappa eccezioni custom in status code + messaggi
                switch (ex)
                {
                    case CreditoInsufficienteException cie:
                        statusCode = (int)HttpStatusCode.BadRequest;
                        response = new 
                        {
                            Errore = cie.Message,
                            cie.Saldo,
                            cie.ImportoRichiesto,
                            TraceId = traceId
                        };
                        break;

                    case MezzoNonDisponibileException mde:
                        statusCode = (int)HttpStatusCode.BadRequest;
                        response = new 
                        {
                            Errore = mde.Message,
                            mde.IdMezzo,
                            TraceId = traceId
                        };
                        break;

                    case UtenteSospesoException use:
                        statusCode = (int)HttpStatusCode.Forbidden;
                        response = new 
                        {
                            Errore = use.Message,
                            use.Email,
                            TraceId = traceId
                        };
                        break;

                    case OperazioneNonConsentitaException oce:
                        statusCode = (int)HttpStatusCode.Forbidden;
                        response = new
                        {
                            Errore = oce.Message,
                            TraceId = traceId
                        };
                        break;

                    case BatteriaTroppoBassaException bb:
                        statusCode = (int)HttpStatusCode.BadRequest;
                        response = new 
                        {
                            Errore = bb.Message,
                            TraceId = traceId
                        };
                        break;

                    case UtenteNonAutorizzatoException ua:
                        statusCode = (int)HttpStatusCode.Unauthorized;
                        response = new 
                        {
                            Errore = ua.Message,
                            TraceId = traceId
                        };
                        break;
                }


                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
