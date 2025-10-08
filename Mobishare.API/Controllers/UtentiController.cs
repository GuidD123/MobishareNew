using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mobishare.Core.Data;
using Mobishare.Core.Models;
using Mobishare.Core.Enums;
using Mobishare.Infrastructure.Services;
using Mobishare.Core.DTOs;
using Mobishare.Core.Exceptions; 

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtentiController(MobishareDbContext context, PasswordService passwordService) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly PasswordService _passwordService = passwordService;


        //ZONA ADMIN 

        // GET: api/utenti
        [Authorize(Roles = "Gestore")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Utente>>> GetUtenti()
        {
            // Solo gestori possono vedere lista utenti
            return await _context.Utenti.ToListAsync();
        }

        // SOLO GESTORE PUO' VEDERE UTENTI SOSPESI 
        // GET: api/utenti/sospesi?idUtente=99 ---> VISUALIZZA UTENTI SOSPESI
        [Authorize(Roles = "Gestore")]
        [HttpGet("sospesi")]
        public async Task<IActionResult> GetUtentiSospesi()
        {
            var sospesi = await _context.Utenti
                .Where(u => u.Sospeso)
                .AsNoTracking()
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista utenti sospesi",
                Dati = sospesi
            });
        }

        /// <summary>
        /// Riattiva un utente sospeso, se il chiamante è un gestore autorizzato
        /// </summary>
        /// <param name="id">ID utente da riattivare</param>
        /// <param name="idGestore">ID del gestore che effettua richiesta</param>
        /// <returns>CConferma riattivazione o messaggio di errore</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //POST: api/utenti/{id}/riattiva -> gestore può riattivare un utente sospeso
        [Authorize(Roles = "Gestore")]
        [HttpPost("{id}/riattiva")]
        public async Task<IActionResult> RiattivaUtente(int id)
        {
            var utente = await _context.Utenti
                .FirstOrDefaultAsync(u => u.Id == id && u.Sospeso)
                ?? throw new ElementoNonTrovatoException("Utente sospeso", id);

            utente.Sospeso = false;
            await _context.SaveChangesAsync();

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
            // 1. Estrai ID utente dal token JWT
            var idUtente = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var utente = await _context.Utenti.FindAsync(idUtente)
             ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            // 2. Verifica password attuale
            if (!_passwordService.Verify(dto.VecchiaPassword, utente.Password))
                throw new OperazioneNonConsentitaException("La password attuale non è corretta");

            if (dto.NuovaPassword.Length < 6)
                throw new ValoreNonValidoException("Password", "minimo 6 caratteri richiesti");

            // 3. Aggiorna -> la nuova password viene hashata 
            utente.Password = _passwordService.Hash(dto.NuovaPassword);
            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Password aggiornata con successo"
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

            //No esporre password (anche se hashata)
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


        /// <summary>
        /// Aggiorna le informazioni di un utente esistente: modifica nome e password.
        /// </summary>
        /// <param name="id">ID dell'utente da aggiornare</param>
        /// <param name="aggiornato">Oggetto Utente coi nuovi dati</param>
        /// <returns>Utente aggiornato o errore</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //PUT: api/utenti/{id} -> EndPoint per aggiornare profilo: modifica nome e psw
        [HttpPut("{id}")]
        public async Task<ActionResult<SuccessResponse>> UpdateInfoUtente(int id, [FromBody] Utente aggiornato)
        {
            if (id != aggiornato.Id)
                throw new ValoreNonValidoException("Id", "non coincide con l’utente da aggiornare");

            var utente = await _context.Utenti.FindAsync(id)
                ?? throw new ElementoNonTrovatoException("Utente", id);

            utente.Nome = aggiornato.Nome;
            utente.Password = _passwordService.Hash(aggiornato.Password);

            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Profilo aggiornato con successo",
                Dati = new { utente.Id, utente.Nome, utente.Email }
            });
        }
    }
}
