using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.Services;
using Mobishare.Infrastructure.SignalRHubs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtentiController(MobishareDbContext context, PasswordService passwordService, IHubContext<NotificheHub> hubContext) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly PasswordService _passwordService = passwordService;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;

        //ZONA ADMIN 

        // GET: api/utenti
        [Authorize(Roles = "Gestore")]
        [HttpGet]
        public async Task<ActionResult<SuccessResponse>> GetUtenti()
        {
            var utenti = await _context.Utenti
                .Select(u => new UtenteDTO
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Ruolo = u.Ruolo.ToString(),
                    Credito = u.Credito,
                    Sospeso = u.Sospeso
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista utenti",
                Dati = utenti
            });
        }

        // SOLO GESTORE PUO' VEDERE UTENTI SOSPESI 
        // GET: api/utenti/sospesi
        [Authorize(Roles = "Gestore")]
        [HttpGet("sospesi")]
        public async Task<IActionResult> GetUtentiSospesi()
        {
            // prima corregge eventuali incoerenze: se credito > 0 ⇒ non deve essere sospeso
            var utenti = await _context.Utenti.ToListAsync();
            bool modifiche = false;

            foreach (var u in utenti)
            {
                if (u.Sospeso && u.Credito > 0)
                {
                    u.Sospeso = false;
                    modifiche = true;
                }
            }

            if (modifiche)
                await _context.SaveChangesAsync();

            // ora restituisce solo quelli effettivamente sospesi
            var sospesi = utenti.Where(u => u.Sospeso).Select(u => new UtenteDTO
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                Ruolo = u.Ruolo.ToString(),
                Credito = u.Credito,
                Sospeso = u.Sospeso
            }).ToList();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista utenti sospesi aggiornata",
                Dati = sospesi
            });
        }


        // PUT: api/utenti/{id}/riattiva
        [Authorize(Roles = "Gestore")]
        [HttpPut("{id}/riattiva")]
        public async Task<IActionResult> RiattivaUtente(int id)
        {
            var utente = await _context.Utenti
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new ElementoNonTrovatoException("Utente", id);

            if (!utente.Sospeso)
                return BadRequest(new ErrorResponse { Errore = "Utente già attivo" });

            if (utente.Credito <= 0)
                throw new OperazioneNonConsentitaException("Impossibile riattivare: credito insufficiente");

            utente.Sospeso = false;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"utenti:{utente.Id}")
                .SendAsync("UtenteRiattivato", new
                {
                    idUtente = utente.Id,
                    nome = utente.Nome,
                    messaggio = "Il tuo account è stato riattivato. Puoi nuovamente usare Mobishare!"
                });

            await _hubContext.Clients.Group("admin")
                .SendAsync("NotificaAdmin", new
                {
                    Titolo = "Utente riattivato",
                    Testo = $"Il gestore ha riattivato l’utente {utente.Nome} (ID {utente.Id})"
                });

            return Ok(new SuccessResponse
            {
                Messaggio = "Utente riattivato con successo",
                Dati = new { utente.Id, utente.Nome }
            });
        }





        //ZONA UTENTE
        //POST: api/utenti -> Registrazione di un nuovo utente
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostUtente([FromBody] RegisterDTO dto)
        {
            if (!ModelState.IsValid)
                throw new ValoreNonValidoException("Dati registrazione", "Modello non valido");

            if (await _context.Utenti.AnyAsync(u => u.Email == dto.Email))
                throw new ElementoDuplicatoException("Utente", dto.Email);

            var nuovoUtente = new Utente
            {
                Email = dto.Email,
                Nome = dto.Nome,
                Cognome = dto.Cognome,
                Password = _passwordService.Hash(dto.Password), // Usa il PasswordService per hashare la password
                Credito = 0,
                Sospeso = false,
                Ruolo = UserRole.Utente
            };

            _context.Utenti.Add(nuovoUtente);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group("admin")
                .SendAsync("NotificaAdmin", new {
                    Titolo = "Nuovo Utente Registrato",
                    Testo = $"L'utente {nuovoUtente.Nome} {nuovoUtente.Cognome} ({nuovoUtente.Email}) si è appena registrato"
                });
              
            return CreatedAtAction(nameof(GetUtente), new { id = nuovoUtente.Id }, new SuccessResponse
            {
                Messaggio = "Registrazione completata",
                Dati = new { nuovoUtente.Id, nuovoUtente.Nome, nuovoUtente.Email }
            });
        }

        //LOGIN 
        // POST: api/utenti/login -> login con email e password
        [HttpPost("login")]
        public async Task<ActionResult<SuccessResponse<LoginResponseDTO>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                throw new ValoreNonValidoException("Login", "Modello non valido");

            var utente = await _context.Utenti.FirstOrDefaultAsync(u => u.Email == request.Email)
                ?? throw new ElementoNonTrovatoException("Utente", request.Email);

            if (!_passwordService.Verify(request.Password, utente.Password))
                throw new OperazioneNonConsentitaException("Credenziali non valide");

            var jwtKey = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key non configurata");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)); 
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //info nei token, coppia chiave-valore es. "nome": "Mario" che rappresenta utente autenticato 
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, utente.Nome),
                new Claim(ClaimTypes.NameIdentifier, utente.Id.ToString()),
                new Claim(ClaimTypes.Role, utente.Ruolo.ToString())
            };

            //pensare al refresh token 
            var token = new JwtSecurityToken(
                issuer: "mobishare",
                audience: "mobishare-client",
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds,
                claims: claims
            );

            return Ok(new SuccessResponse<LoginResponseDTO>
            {
                Messaggio = "Login effettuato",
                Dati = new LoginResponseDTO
                {
                    Id = utente.Id,
                    Nome = utente.Nome,
                    Ruolo = utente.Ruolo.ToString(),
                    Credito = utente.Credito,
                    Sospeso = utente.Sospeso,
                    Token = new JwtSecurityTokenHandler().WriteToken(token)
                }
            });
        }

        //PUT: api/utenti/cambia-password
        [HttpPut("cambia-password")]
        [Authorize]
        public async Task<ActionResult<SuccessResponse>> CambiaPsw([FromBody] CambiaPswDTO dto)
        {
            //Estrai ID utente dal token JWT
            var idUtente = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var utente = await _context.Utenti.FindAsync(idUtente)
             ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            //Verifica password attuale
            if (!_passwordService.Verify(dto.VecchiaPassword, utente.Password))
                throw new OperazioneNonConsentitaException("La password attuale non è corretta");

            //Aggiorna -> la nuova password viene hashata 
            utente.Password = _passwordService.Hash(dto.NuovaPassword);
            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Password aggiornata con successo"
            });
        }

        // POST: api/utenti/request-password-reset
        [HttpPost("request-password-reset")]
        public async Task<ActionResult<SuccessResponse<string>>> RequestPasswordReset([FromBody] ResetPasswordRequestDTO dto)
        {
            var utente = await _context.Utenti.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (utente == null)
                return NotFound(new ErrorResponse { Errore = "Email non trovata" });

            // Genera token temporaneo (in produzione andrebbe salvato nel DB)
            var token = Guid.NewGuid().ToString("N");

            // Per progetto universitario: restituiamo direttamente il token
            return Ok(new SuccessResponse<string>
            {
                Messaggio = "Token generato",
                Dati = token
            });
        }

        // POST: api/utenti/reset-password
        [HttpPost("reset-password")]
        public async Task<ActionResult<SuccessResponse>> ResetPassword([FromBody] ResetPasswordRequestDTO dto)
        {
            //Validazione input
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.NewPassword) ||
                string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest(new ErrorResponse { Errore = "Richiesta non valida", Messaggio = "Compila tutti i campi richiesti." });
            }

            //Trova utente
            var utente = await _context.Utenti.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (utente == null)
            {
                return NotFound(new ErrorResponse { Errore = "Utente non trovato" });
            }

            //Aggiorna password
            utente.Password = _passwordService.Hash(dto.NewPassword);
            await _context.SaveChangesAsync();

            //Risposta positiva
            return Ok(new SuccessResponse
            {
                Messaggio = "Password aggiornata correttamente"
            });
        }


        //GET: api/utenti/{id} -> tira fuori info di un utente 
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<SuccessResponse>> GetUtente(int id)
        {

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Solo il proprietario dell'account o un gestore può vedere i dettagli
            if (currentUserRole != UserRole.Gestore.ToString() && currentUserId != id)
                throw new OperazioneNonConsentitaException("Non puoi accedere ai dati di altri utenti");

            // Trova l'utente e restituisci i dettagli
            var utente = await _context.Utenti.FindAsync(id)
                ?? throw new ElementoNonTrovatoException("Utente", id);

            return Ok(new SuccessResponse
            {
                Messaggio = "Dettaglio utente",
                Dati = new
                {
                    utente.Id,
                    utente.Nome,
                    utente.Email,
                    ruolo = utente.Ruolo.ToString(),
                    utente.Credito,
                    utente.Sospeso
                }
            });
        }


        //PUT: api/utenti/{id} -> Aggiorna profilo utente (solo Nome)
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<SuccessResponse>> UpdateInfoUtente(int id, [FromBody] UtenteDTO dto)
        {
            // Verifica coerenza ID
            if (id != dto.Id)
                throw new ValoreNonValidoException("Id", "non coincide con l'utente da aggiornare");

            // Verifica autorizzazione: solo l'utente stesso o un gestore può modificare
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole != UserRole.Gestore.ToString() && currentUserId != id)
                throw new OperazioneNonConsentitaException("Non puoi modificare i dati di altri utenti");

            var utente = await _context.Utenti.FindAsync(id)
                ?? throw new ElementoNonTrovatoException("Utente", id);

            // Aggiorna SOLO il nome (Email non si cambia, password ha endpoint dedicato)
            if (!string.IsNullOrWhiteSpace(dto.Nome))
                utente.Nome = dto.Nome;

            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Profilo aggiornato con successo",
                Dati = new UtenteDTO
                {
                    Id = utente.Id,
                    Nome = utente.Nome,
                    Email = utente.Email,
                    Ruolo = utente.Ruolo.ToString(),
                    Credito = utente.Credito,
                    Sospeso = utente.Sospeso
                }
            });
        }

        [Authorize]
        [HttpGet("profilo")]
        public async Task<ActionResult<SuccessResponse>> GetProfiloUtente()
        {
            var idUtente = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));

            var utente = await _context.Utenti
                .AsNoTracking()
                .Where(u => u.Id == idUtente)
                .Select(u => new ProfiloResponseDTO
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Cognome = u.Cognome,
                    Email = u.Email,
                    Ruolo = u.Ruolo.ToString(),
                    Credito = u.Credito,
                    PuntiBonus = u.PuntiBonus,
                    Sospeso = u.Sospeso
                })
                .FirstOrDefaultAsync()
                ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            return Ok(new SuccessResponse
            {
                Messaggio = "Profilo utente",
                Dati = utente
            });
        }

    }
}
