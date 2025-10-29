using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Models;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController(MobishareDbContext context) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;

        // POST: api/feedback
        [HttpPost]
        public async Task<IActionResult> PostFeedback([FromBody] FeedbackCreateDTO dto)
        {
            var utente = await _context.Utenti.FindAsync(dto.IdUtente);
            var corsa = await _context.Corse.FindAsync(dto.IdCorsa);

            if (utente == null || corsa == null)
                return NotFound("Utente o corsa non trovati");

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

            return Ok(new SuccessResponse
            {
                Messaggio = "Feedback ricevuto, grazie!",
                Dati = new { feedbackId = feedback.Id }
            });
        }

        // GET: api/feedback/utente/idutente
        [HttpGet("utente/{idUtente}")]
        public async Task<IActionResult> GetFeedbackPerUtente(int idUtente)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.IdUtente == idUtente)
                .Select(f => new
                {
                    f.Id,
                    f.IdUtente,
                    f.IdCorsa,
                    Valutazione = f.Valutazione.ToString(),
                    ValutazioneNumero = (int)f.Valutazione,
                    f.Commento,
                    f.DataFeedback
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = $"Feedback dell'utente {idUtente}",
                Dati = feedbacks
            });
        }

        // GET: api/feedback/recenti
        [HttpGet("recenti")]
        public async Task<IActionResult> GetFeedbackRecenti()
        {
            var recenti = await _context.Feedbacks
                .Include(f => f.Utente)
                .Include(f => f.Corsa)
                .OrderByDescending(f => f.DataFeedback)
                .Take(10)
                .Select(f => new
                {
                    f.Id,
                    Valutazione = f.Valutazione.ToString(),
                    ValutazioneNumero = (int)f.Valutazione,
                    f.Commento,
                    f.DataFeedback,
                    NomeUtente = f.Utente != null ? f.Utente.Nome : "Utente non disponibile",
                    f.IdCorsa,
                    MatricolaMezzo = f.Corsa != null ? f.Corsa.MatricolaMezzo : "N/A"
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Ultimi 10 feedback",
                Dati = recenti
            });
        }

        // GET: api/feedback/negativi
        [HttpGet("negativi")]
        public async Task<IActionResult> GetFeedbackNegativi()
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.Valutazione == ValutazioneFeedback.Pessimo ||
                           f.Valutazione == ValutazioneFeedback.Scarso)
                .Include(f => f.Utente)
                .Include(f => f.Corsa)
                .OrderByDescending(f => f.DataFeedback)
                .Select(f => new
                {
                    f.Id,
                    Valutazione = f.Valutazione.ToString(),
                    ValutazioneNumero = (int)f.Valutazione,
                    f.Commento,
                    f.DataFeedback,
                    NomeUtente = f.Utente != null ? f.Utente.Nome : "N/A",
                    f.IdCorsa,
                    MatricolaMezzo = f.Corsa != null ? f.Corsa.MatricolaMezzo : "N/A"
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Feedback negativi",
                Dati = new
                {
                    totaleFeedbackNegativi = feedbacks.Count,
                    feedbacks
                }
            });
        }

        // GET: api/feedback/statistiche
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

            var statistiche = new
            {
                TotaleFeedback = feedbacks.Count,
                MediaGenerale = Math.Round(feedbacks.Average(f => (int)f.Valutazione), 2),
                Distribuzione = new
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

        private static string GetDescrizioneMedia(double media)
        {
            return media switch
            {
                >= 4.5 => "Eccellente",
                >= 3.5 => "Buono",
                >= 2.5 => "Sufficiente",
                >= 1.5 => "Scarso",
                _ => "Pessimo"
            };
        }
    }
}