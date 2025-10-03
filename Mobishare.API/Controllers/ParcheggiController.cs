using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParcheggiController(MobishareDbContext context) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;

        // ZONA UTENTE 

        // GET: api/parcheggi
        [HttpGet]
        public async Task<ActionResult<SuccessResponse>> GetParcheggi()
        {
            var parcheggi = await _context.Parcheggi
                .Include(p => p.Mezzi)
                .AsNoTracking()
                .ToListAsync();

            // carico i mezzi separatamente
            var result = parcheggi.Select(p => new ParcheggioResponseDTO
            {
                Id = p.Id,
                Nome = p.Nome,
                Zona = p.Zona,
                Indirizzo = p.Indirizzo,
                Capienza = p.Capienza,
                Attivo = p.Attivo,
                Mezzi = p.Mezzi.Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = p.Nome
                }).ToList()
            }).ToList();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista parcheggi",
                Dati = result
            });
        }

       
        // GET: api/parcheggi/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SuccessResponse>> GetParcheggio(int id)
        {
            var parcheggio = await _context.Parcheggi
                .Include(p => p.Mezzi)   // carico anche i mezzi collegati
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parcheggio == null)
                throw new ElementoNonTrovatoException("Parcheggio", id);

            var dto = new ParcheggioResponseDTO
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Zona = parcheggio.Zona,
                Indirizzo = parcheggio.Indirizzo,
                Capienza = parcheggio.Capienza,
                Attivo = parcheggio.Attivo,
                Mezzi = parcheggio.Mezzi.Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = parcheggio.Nome
                }).ToList()
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Dettaglio parcheggio",
                Dati = dto
            });
        }

        // GET: api/parcheggi/{id}/disponibilita
        [HttpGet("{id}/disponibilita")]
        public async Task<ActionResult<SuccessResponse>> GetDisponibilitaParcheggio(int id)
        {
            var parcheggio = await _context.Parcheggi
                .Include(p => p.Mezzi)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parcheggio == null)
                throw new ElementoNonTrovatoException("Parcheggio", id);

            // mezzi che contano come "occupanti posti"
            var occupati = parcheggio.Mezzi.Count(m =>
                m.Stato != StatoMezzo.InUso &&
                m.Stato != StatoMezzo.Manutenzione);

            var liberi = parcheggio.Capienza - occupati;

            return Ok(new SuccessResponse
            {
                Messaggio = $"Disponibilità parcheggio {parcheggio.Nome}",
                Dati = new
                {
                    postiTotali = parcheggio.Capienza,
                    postiOccupati = occupati,
                    postiLiberi = liberi
                }
            });
        }




        // ZONA ADMIN

        // POST: api/parcheggi -> Aggiungi un nuovo parcheggio
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostParcheggio([FromBody] ParcheggioCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ValoreNonValidoException("Nome", "Il nome del parcheggio non può essere vuoto");

            if (dto.Capienza <= 0)
                throw new ValoreNonValidoException("Capienza", "La capienza deve essere maggiore di zero");

            if (await _context.Parcheggi.AnyAsync(p => p.Nome == dto.Nome))
                throw new ElementoDuplicatoException("Parcheggio", dto.Nome);

            var parcheggio = new Parcheggio
            {
                Nome = dto.Nome,
                Zona = dto.Zona,
                Indirizzo = dto.Indirizzo,
                Capienza = dto.Capienza,
                Attivo = dto.Attivo
            };

            _context.Parcheggi.Add(parcheggio);
            await _context.SaveChangesAsync();

            var response = new ParcheggioResponseDTO
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Zona = parcheggio.Zona,
                Indirizzo = parcheggio.Indirizzo,
                Capienza = parcheggio.Capienza,
                Attivo = parcheggio.Attivo,
                Mezzi = [] // nuovo parcheggio parte vuoto
            };

            return CreatedAtAction(nameof(GetParcheggio), new { id = parcheggio.Id }, new SuccessResponse
            {
                Messaggio = "Parcheggio creato correttamente",
                Dati = response
            });
        }

    }
}
