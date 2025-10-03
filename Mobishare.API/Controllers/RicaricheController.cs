using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RicaricheController(MobishareDbContext context, ILogger<RicaricheController> logger) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly ILogger<RicaricheController> _logger = logger;
        


        // GET: api/ricariche/{idUtente} - Storico ricariche utente
        [HttpGet("{idUtente}")]
        public async Task<ActionResult<SuccessResponse>> GetRicaricheUtente(int idUtente)
        {
            var utente = await _context.Utenti.FindAsync(idUtente)
                ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            var ricariche = await _context.Ricariche
                .Where(r => r.IdUtente == idUtente)
                .OrderByDescending(r => r.DataRicarica)
                .Select(r => new RicaricaResponseDTO
                {
                    Id = r.Id,
                    ImportoRicarica = r.ImportoRicarica,
                    DataRicarica = r.DataRicarica,
                    Tipo = r.Tipo.ToString(),
                    Stato = r.Stato.ToString()
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Storico ricariche utente",
                Dati = ricariche
            });
        }



        // POST: api/ricariche - Crea nuova ricarica partendo dal NuovaRicaricaDTO, valida importo e utente, simula pagamento e aggiorna credito utente
        //Inoltre gestisce utente sospeso/riattivato e salva il tutto 
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostRicarica([FromBody] NuovaRicaricaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validazioni dati input 
                var utente = await _context.Utenti.FindAsync(dto.IdUtente)
                     ?? throw new ElementoNonTrovatoException("Utente", dto.IdUtente);

                if (dto.ImportoRicarica <= 0)
                    throw new ImportoNonValidoException(dto.ImportoRicarica, "deve essere maggiore di zero");

                if (dto.ImportoRicarica > 500) // Limite massimo ricarica
                    throw new ImportoNonValidoException(dto.ImportoRicarica, "supera il massimo di 500€");

                // Crea ricarica in stato "InSospeso"
                var ricarica = new Ricarica
                {
                    IdUtente = dto.IdUtente,
                    ImportoRicarica = dto.ImportoRicarica,
                    DataRicarica = DateTime.UtcNow,
                    Tipo = dto.TipoRicarica,
                    Stato = StatoPagamento.InSospeso
                };

                _context.Ricariche.Add(ricarica);
                await _context.SaveChangesAsync();

                // Simula processamento pagamento
                var successoPagamento = await ProcessaPagamentoAsync(ricarica, dto);

                if (successoPagamento)
                {
                    // Pagamento riuscito - aggiorna credito utente
                    ricarica.Stato = StatoPagamento.Completato;
                    utente.Credito += dto.ImportoRicarica;

                    // Se utente era sospeso e ora ha credito positivo, riattivalo
                    if (utente.Sospeso && utente.Credito >= 0)
                    {
                        utente.Sospeso = false;
                        _logger.LogInformation("Utente {UserId} riattivato dopo ricarica. Nuovo credito: {Credito}€",
                            utente.Id, utente.Credito);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var payload = new RicaricaResponseDTO
                    {
                        Id = ricarica.Id,
                        ImportoRicarica = ricarica.ImportoRicarica,
                        DataRicarica = ricarica.DataRicarica,
                        Tipo = ricarica.Tipo.ToString(),
                        Stato = ricarica.Stato.ToString()
                    };

                    return CreatedAtAction(nameof(GetRicaricheUtente),
                        new { idUtente = dto.IdUtente },
                        new SuccessResponse
                        {
                            Messaggio = "Ricarica completata con successo",
                            Dati = payload
                        });
                }
                else
                {
                    // Pagamento fallito
                    ricarica.Stato = StatoPagamento.Fallito;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogWarning("Ricarica fallita: Utente {UserId}, Importo {Importo}€, Tipo {Tipo}",
                        dto.IdUtente, dto.ImportoRicarica, dto.TipoRicarica);

                    throw new PagamentoFallitoException("Pagamento rifiutato. Verifica i dati della carta o riprova.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Errore durante la ricarica per utente {UserId}", dto.IdUtente);
                return StatusCode(500, "Errore interno durante la ricarica");
            }
        }


        // POST: api/ricariche/{id}/conferma - Conferma ricarica (webhook)
        //Funziona come un webhook che in un sistema reale arriverebbe da stripe/paypal: controlla un TokenSicurezza e se successo == true allora aggiorna credito utente altrimenti "Fallito"
        [HttpPost("{id}/conferma")]
        public async Task<ActionResult<SuccessResponse>> ConfermaRicarica(int id, [FromBody] ConfermaRicaricaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ricarica = await _context.Ricariche
                    .Include(r => r.Utente)
                    .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new ElementoNonTrovatoException("Ricarica", id);

                if (ricarica.Stato != StatoPagamento.InSospeso)
                    throw new OperazioneNonConsentitaException("La ricarica non è in stato sospeso");

                // Verifica token sicurezza (in produzione usare chiavi API)
                if (dto.TokenSicurezza != "webhook_secret_key")
                    throw new UtenteNonAutorizzatoException("Conferma ricarica");

                if (dto.Successo)
                {
                    // Conferma pagamento riuscito
                    ricarica.Stato = StatoPagamento.Completato;
                    ricarica.Utente!.Credito += ricarica.ImportoRicarica;

                    // Riattiva utente se era sospeso
                    if (ricarica.Utente.Sospeso && ricarica.Utente.Credito >= 0)
                    {
                        ricarica.Utente.Sospeso = false;
                    }

                    _logger.LogInformation("Ricarica confermata via webhook: Id {RicaricaId}, Utente {UserId}",
                        id, ricarica.IdUtente);
                }
                else
                {
                    // Conferma pagamento fallito
                    ricarica.Stato = StatoPagamento.Fallito;

                    _logger.LogWarning("Ricarica rifiutata via webhook: Id {RicaricaId}, Motivo: {Motivo}",
                        id, dto.MotivoRifiuto);

                    throw new PagamentoFallitoException(dto.MotivoRifiuto ?? "Pagamento rifiutato");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new SuccessResponse
                {
                    Messaggio = "Ricarica aggiornata con successo",
                    Dati = new { id, stato = ricarica.Stato.ToString() }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Errore nella conferma ricarica {RicaricaId}", id);
                return StatusCode(500, "Errore interno");
            }
        }



        // GET: api/ricariche/utente/{idUtente}/saldo - Saldo attuale utente
        //Ritorna la vista utente: credito attuale, totale ricariche, spese, ultima ricarica
        [HttpGet("utente/{idUtente}/saldo")]
        public async Task<ActionResult<SuccessResponse>> GetSaldoUtente(int idUtente)
        {
            var utente = await _context.Utenti.FindAsync(idUtente)
             ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            // Calcola statistiche ricariche
            var totaleRicariche = await _context.Ricariche
                .Where(r => r.IdUtente == idUtente && r.Stato == StatoPagamento.Completato)
                .SumAsync(r => r.ImportoRicarica);

            var ricaricheInSospeso = await _context.Ricariche
                .Where(r => r.IdUtente == idUtente && r.Stato == StatoPagamento.InSospeso)
                .SumAsync(r => r.ImportoRicarica);

            // Calcola spese corse - totale 
            var totaleSpeseCorse = await _context.Corse
                .Where(c => c.IdUtente == idUtente && c.CostoFinale.HasValue)
                .SumAsync(c => c.CostoFinale!.Value);

            var payload = new SaldoResponseDTO
            {
                CreditoAttuale = utente.Credito,
                UtenteAttivo = !utente.Sospeso,
                TotaleRicariche = totaleRicariche,
                RicaricheInSospeso = ricaricheInSospeso,
                TotaleSpese = totaleSpeseCorse,
                UltimaRicarica = await _context.Ricariche
                    .Where(r => r.IdUtente == idUtente)
                    .OrderByDescending(r => r.DataRicarica)
                    .Select(r => r.DataRicarica)
                    .FirstOrDefaultAsync()
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Saldo utente",
                Dati = payload
            });
        }


        // Metodo privato per simulare processamento pagamento
        private async Task<bool> ProcessaPagamentoAsync(Ricarica ricarica, NuovaRicaricaDTO dto)
        {
            // Simula latenza processamento
            await Task.Delay(1000);

            // In produzione: integrazione con Stripe, PayPal, etc.
            // Per ora simula successo al 90%
            var random = new Random();
            var successo = random.Next(1, 101) <= 90;

            _logger.LogInformation("Processamento pagamento {TipoRicarica} di {Importo}€: {Risultato}",
                dto.TipoRicarica, dto.ImportoRicarica, successo ? "Successo" : "Fallito");

            return successo;
        }
    }
}