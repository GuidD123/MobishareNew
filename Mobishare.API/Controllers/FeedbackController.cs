using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.SignalRHubs;
using System.Security.Claims;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController(MobishareDbContext context, IHubContext<NotificheHub> hubContext) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;

        // POST: api/feedback
        [Authorize(Roles = "Utente")]
        [HttpPost]
        public async Task<IActionResult> PostFeedback([FromBody] FeedbackCreateDTO dto)
        {
            // Estrai ID utente dal token JWT
            var utente = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));

            if (dto.IdUtente != utente)
                throw new OperazioneNonConsentitaException("Puoi lasciare feedback solo per le tue corse");

            var corsa = await _context.Corse.FindAsync(dto.IdCorsa)
                ?? throw new ElementoNonTrovatoException("Corsa", dto.IdCorsa);

            // Verifica che la corsa appartenga all'utente
            if (corsa.IdUtente != utente)
                throw new OperazioneNonConsentitaException("Questa corsa non ti appartiene");

            if (!corsa.DataOraFine.HasValue)
                return BadRequest("Non puoi lasciare un feedback su una corsa non ancora terminata");

            if (!Enum.IsDefined(typeof(ValutazioneFeedback), dto.Valutazione))
                return BadRequest("La valutazione deve essere tra Pessimo (1) e Ottimo (5)");

            var esisteGia = await _context.Feedbacks.AnyAsync(f => f.IdUtente == dto.IdUtente && f.IdCorsa == dto.IdCorsa);

            if (esisteGia)
                return Conflict("Hai già lasciato un feedback per questa corsa");

            var feedback = new Feedback
            {
                IdUtente = dto.IdUtente,
                IdCorsa = dto.IdCorsa,
                Valutazione = (ValutazioneFeedback)dto.Valutazione,
                Commento = dto.Commento,
                DataFeedback = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            if (feedback.Valutazione == ValutazioneFeedback.Pessimo || feedback.Valutazione == ValutazioneFeedback.Scarso)
            {
                var u = await _context.Utenti.FindAsync(utente)
                    ?? throw new ElementoNonTrovatoException("Utente", utente);

                await _hubContext.Clients.Group("admin")
                    .SendAsync("NotificaAdmin", new
                    {
                        Titolo = "Feedback negativo",
                        Testo = $"Corsa {feedback.IdCorsa}: valutazione {feedback.Valutazione} da {u.Nome} {u.Cognome} (ID {u.Id})."
                    });
            }

            return Ok(new SuccessResponse
            {
                Messaggio = "Feedback ricevuto, grazie!",
                Dati = new { feedbackId = feedback.Id }
            });
        }

        // GET: api/feedback/utente/idutente
        [Authorize]
        [HttpGet("utente/{idUtente}")]
        public async Task<IActionResult> GetFeedbackPerUtente(int idUtente)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var isGestore = User.IsInRole("Gestore");

            if (!isGestore && currentUserId != idUtente)
                throw new OperazioneNonConsentitaException("Non puoi vedere i feedback di altri utenti");

            var feedbackEntities = await _context.Feedbacks
                .Where(f => f.IdUtente == idUtente)
                .Include(f => f.Utente)
                .Include(f => f.Corsa)
                .ToListAsync();

            var feedbacks = feedbackEntities
                .Select(f => new FeedbackResponseDTO
                {
                    Id = f.Id,
                    IdUtente = f.IdUtente,
                    IdCorsa = f.IdCorsa,
                    NomeUtente = f.Utente != null ? $"{f.Utente.Nome} {f.Utente.Cognome}" : "N/A",
                    MatricolaMezzo = f.Corsa != null ? f.Corsa.MatricolaMezzo : "N/A",
                    Valutazione = f.Valutazione.ToString(),
                    ValutazioneNumero = (int)f.Valutazione,
                    Commento = f.Commento,
                    DataFeedback = f.DataFeedback
                })
                .ToList();

            return Ok(new SuccessResponse
            {
                Messaggio = $"Feedback dell'utente {idUtente}",
                Dati = feedbacks
            });
        }

        // GET: api/feedback/recenti
        [Authorize(Roles = "Gestore")]
        [HttpGet("recenti")]
        public async Task<IActionResult> GetFeedbackRecenti()
        {
            var feedbackEntities = await _context.Feedbacks
                .Include(f => f.Utente)
                .Include(f => f.Corsa)
                .OrderByDescending(f => f.DataFeedback)
                .Take(10)
                .ToListAsync();

            var recenti = feedbackEntities
                .Select(f => new FeedbackResponseDTO
                {
                    Id = f.Id,
                    IdUtente = f.IdUtente,
                    IdCorsa = f.IdCorsa,
                    NomeUtente = f.Utente != null ? $"{f.Utente.Nome} {f.Utente.Cognome}" : "N/A",
                    MatricolaMezzo = f.Corsa != null ? f.Corsa.MatricolaMezzo : "N/A",
                    Valutazione = f.Valutazione.ToString(),
                    ValutazioneNumero = (int)f.Valutazione,
                    Commento = f.Commento,
                    DataFeedback = f.DataFeedback
                })
                .ToList();

            return Ok(new SuccessResponse
            {
                Messaggio = "Ultimi 10 feedback",
                Dati = recenti
            });
        }

        // GET: api/feedback/negativi
        [Authorize(Roles = "Gestore")]
        [HttpGet("negativi")]
        public async Task<IActionResult> GetFeedbackNegativi()
        {
            var feedbackEntities = await _context.Feedbacks
                .Where(f => f.Valutazione == ValutazioneFeedback.Pessimo ||
                            f.Valutazione == ValutazioneFeedback.Scarso)
                .Include(f => f.Utente)
                .Include(f => f.Corsa)
                .OrderByDescending(f => f.DataFeedback)
                .ToListAsync();

            var feedbacks = feedbackEntities
                .Select(f => new FeedbackResponseDTO
                {
                    Id = f.Id,
                    IdUtente = f.IdUtente,
                    IdCorsa = f.IdCorsa,
                    NomeUtente = f.Utente != null ? $"{f.Utente.Nome} {f.Utente.Cognome}" : "N/A",
                    MatricolaMezzo = f.Corsa != null ? f.Corsa.MatricolaMezzo : "N/A",
                    Valutazione = f.Valutazione.ToString(),
                    ValutazioneNumero = (int)f.Valutazione,
                    Commento = f.Commento,
                    DataFeedback = f.DataFeedback
                })
                .ToList();

            return Ok(new SuccessResponse
            {
                Messaggio = "Feedback negativi",
                Dati = new FeedbackNegativiResponseDTO
                {
                    TotaleFeedbackNegativi = feedbacks.Count,
                    Feedbacks = feedbacks
                }
            });
        }

        // GET: api/feedback/statistiche
        [Authorize(Roles = "Gestore")]
        [HttpGet("statistiche")]
        public async Task<IActionResult> GetStatisticheFeedback()
        {
            var feedbacks = await _context.Feedbacks.ToListAsync();

            if (feedbacks.Count == 0)
            {
                return Ok(new SuccessResponse
                {
                    Messaggio = "Nessun feedback presente",
                    Dati = null
                });
            }

            var statistiche = new FeedbackStatisticheDTO
            {
                TotaleFeedback = feedbacks.Count,
                MediaGenerale = Math.Round(feedbacks.Average(f => (int)f.Valutazione), 2),
                Distribuzione = new FeedbackDistribuzioneDTO
                {
                    Pessimo = feedbacks.Count(f => f.Valutazione == ValutazioneFeedback.Pessimo),
                    Scarso = feedbacks.Count(f => f.Valutazione == ValutazioneFeedback.Scarso),
                    Sufficiente = feedbacks.Count(f => f.Valutazione == ValutazioneFeedback.Sufficiente),
                    Buono = feedbacks.Count(f => f.Valutazione == ValutazioneFeedback.Buono),
                    Ottimo = feedbacks.Count(f => f.Valutazione == ValutazioneFeedback.Ottimo)
                }
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Statistiche feedback",
                Dati = statistiche
            });
        }

        /*private static string GetDescrizioneMedia(double media)
        {
            return media switch
            {
                >= 4.5 => "Eccellente",
                >= 3.5 => "Buono",
                >= 2.5 => "Sufficiente",
                >= 1.5 => "Scarso",
                _ => "Pessimo"
            };
        }*/
    }
}