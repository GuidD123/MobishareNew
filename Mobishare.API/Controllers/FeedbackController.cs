using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.Models;
using Mobishare.Core.Enums;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController(MobishareDbContext context) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;

        // POST: api/feedback
        [HttpPost]
        public async Task<IActionResult> PostFeedback(Feedback feedback)
        {
            var utente = await _context.Utenti.FindAsync(feedback.IdUtente);
            var corsa = await _context.Corse.FindAsync(feedback.IdCorsa);

            if (utente == null || corsa == null)
                return NotFound("Utente o corsa non trovati");
       
            if (!corsa.DataOraFine.HasValue)
                return BadRequest("Non puoi lasciare un feedback su una corsa non ancora terminata");

            //Controllo che la valutazione sia tra 1 e 5
            if (!Enum.IsDefined(typeof(ValutazioneFeedback), feedback.Valutazione))
                return BadRequest("La valutazione deve essere tra Pessimo (1) e Ottimo (5)");

            //Controllo se l'utente ha già lasciato un feedback per questa corsa
            var esisteGia = await _context.Feedbacks.AnyAsync(f => f.IdUtente == feedback.IdUtente && f.IdCorsa == feedback.IdCorsa);

            if (esisteGia)
                return Conflict("Hai già lasciato un feedback per questa corsa");
           
            //Mi assicuro che la data sia impostata
            if (feedback.DataFeedback == default)
                feedback.DataFeedback = DateTime.UtcNow;

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { messaggio = "Feedback ricevuto, grazie!" });
        }

        // GET: api/feedback/corsa/idcorsa
        [HttpGet("corsa/{idCorsa}")]
        public async Task<IActionResult> GetFeedbackPerCorsa(int idCorsa)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.IdCorsa == idCorsa)
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

            return Ok(feedbacks);
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

            return Ok(feedbacks);
        }

        // GET: api/feedback/corsa/idcorsa/media
        [HttpGet("corsa/{idCorsa}/media")]
        public async Task<IActionResult> GetMediaVotiPerCorsa(int idCorsa)
        {
            var feedbacks = await _context.Feedbacks.Where(f => f.IdCorsa == idCorsa).ToListAsync();

            if (feedbacks.Count == 0)
                return Ok(new { media = 0.0, totaleFeedback = 0 });

            var media = feedbacks.Average(f => (int)f.Valutazione);

            return Ok(new
            {
                media = Math.Round(media, 2),
                totaleFeedback = feedbacks.Count,
                mediaDescrittiva = GetDescrizioneMedia(media)
            });
        }

        // GET: api/feedback/mezzo/matricola/media -> media voti per mezzo 
        [HttpGet("mezzo/{matricola}/media")]
        public async Task<IActionResult> GetMediaFeedbackPerMezzo(string matricola)
        {
            var feedbacks = await _context.Feedbacks
                .Join(_context.Corse,
                    f => f.IdCorsa,
                    c => c.Id,
                    (f, c) => new { Feedback = f, Corsa = c })
                .Where(fc => fc.Corsa.MatricolaMezzo == matricola)
                .Select(fc => fc.Feedback)
                .ToListAsync();

            if (feedbacks.Count == 0)
                return Ok(new
                {
                    media = 0.0,
                    totaleFeedback = 0,
                    messaggio = "Nessun feedback trovato per questo mezzo"
                });

            var media = feedbacks.Average(f => (int)f.Valutazione);

            return Ok(new
            {
                media = Math.Round(media, 2),
                totaleFeedback = feedbacks.Count,
                mediaDescrittiva = GetDescrizioneMedia(media)
            });
        }

        // GET: api/feedback/recenti
        [HttpGet("recenti")]
        public async Task<IActionResult> GetFeedbackRecenti()
        {
            var recenti = await _context.Feedbacks
                .Include(f => f.Utente) //include dati utente se necessari
                .Include(f => f.Corsa)  //Include dati corsa se necessari
                .OrderByDescending(f => f.DataFeedback)
                .Take(10)
                .Select(f => new //Projection per evitare over-fetching
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

            return Ok(recenti);
        }

        // GET: api/feedback/negativi - Gestione completa
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

            return Ok(new
            {
                totaleFeedbackNegativi = feedbacks.Count,
                feedbacks
            });
        }

        //Metodo helper per descrizione media
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

        // GET statistiche complete
        [HttpGet("statistiche")]
        public async Task<IActionResult> GetStatisticheFeedback()
        {
            var feedbacks = await _context.Feedbacks.ToListAsync();

            if (feedbacks.Count == 0)
                return Ok(new { messaggio = "Nessun feedback presente" });

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

            return Ok(statistiche);
        }
    }
}
