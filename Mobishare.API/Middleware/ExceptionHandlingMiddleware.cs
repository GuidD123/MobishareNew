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
                var traceId = context.TraceIdentifier;
                _logger.LogError(ex, "Errore catturato dal middleware. TraceId={TraceId}", traceId);

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
                    //400 Bad Request 
                    case ValoreNonValidoException vnv: // ArgumentException custom
                        statusCode = (int)HttpStatusCode.BadRequest;
                        response = new
                        {
                            Errore = vnv.Message,
                            Campo = vnv.Data,
                            TraceId = traceId
                        };
                        break;

                    case ImportoNonValidoException inv:
                        statusCode = (int)HttpStatusCode.BadRequest;
                        response = new 
                        {
                            Errore = inv.Message,
                            inv.Importo,
                            TraceId = traceId
                        };
                        break;

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

                    case BatteriaTroppoBassaException bb:
                        statusCode = (int)HttpStatusCode.BadRequest;
                        response = new
                        {
                            Errore = bb.Message,
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



                    //401 Unauthorized

                    case UtenteNonAutorizzatoException ua:
                        statusCode = (int)HttpStatusCode.Unauthorized;
                        response = new
                        {
                            Errore = ua.Message,
                            TraceId = traceId
                        };
                        break;

                    case OperazioneNonConsentitaException oce:
                        statusCode = (int)HttpStatusCode.Unauthorized;
                        response = new
                        {
                            Errore = oce.Message,
                            TraceId = traceId
                        };
                        break;

                    //402 Payment Required 
                    case PagamentoFallitoException pfe:
                        statusCode = 402;
                        response = new
                        {
                            Errore = pfe.Message,
                            pfe.Motivo,
                            TraceId = traceId
                        };
                        break; 


                    //403 Forbidden

                    case UtenteSospesoException use:
                        statusCode = (int)HttpStatusCode.Forbidden;
                        response = new
                        {
                            Errore = use.Message,
                            use.Email,
                            TraceId = traceId
                        };
                        break;

                    //404 NotFound
                    case CorsaNonTrovataException cnt:
                        statusCode = (int)HttpStatusCode.NotFound;
                        response = new
                        {
                            Errore = cnt.Message,
                            TraceId = traceId
                        };
                        break;

                    case ElementoNonTrovatoException ent:
                        statusCode = (int)HttpStatusCode.NotFound;
                        response = new
                        {
                            Errore = ent.Message,
                            TraceId = traceId
                        };
                        break;

                    //409 Conflict 
                    case ElementoDuplicatoException dup:
                        statusCode = (int)HttpStatusCode.Conflict;
                        response = new 
                        { 
                            Errore = dup.Message,
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
