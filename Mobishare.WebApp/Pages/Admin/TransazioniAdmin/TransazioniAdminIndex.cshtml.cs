using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.Core.DTOs;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Admin.TransazioniAdmin
{
    public class TransazioniAdminIndexModel(IMobishareApiService api) : PageModel
    {
        private readonly IMobishareApiService _api = api;

        public List<TransazioneResponseDTO> Transazioni { get; set; } = new();
        public UtenteDTO? UtenteSelezionato { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdUtente { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInizio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFine { get; set; }

        public decimal TotaleEntrate { get; set; }
        public decimal TotaleUscite { get; set; }

        public IEnumerable<TransazioneResponseDTO> TransazioniFiltrate
        {
            get
            {
                var query = Transazioni.AsEnumerable();

                if (!string.IsNullOrEmpty(TipoFiltro))
                {
                    query = query.Where(t => t.Tipo == TipoFiltro);
                }

                if (DataInizio.HasValue)
                {
                    query = query.Where(t => t.DataTransazione.Date >= DataInizio.Value.Date);
                }

                if (DataFine.HasValue)
                {
                    query = query.Where(t => t.DataTransazione.Date <= DataFine.Value.Date);
                }

                return query;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            //Controllo autenticazione
            var token = HttpContext.Session.GetString("JwtToken");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(token) || userRole != "Gestore")
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                // Carica transazioni SOLO se c'è un utente selezionato
                if (IdUtente.HasValue)
                {
                    UtenteSelezionato = await _api.GetUtenteAsync(IdUtente.Value);
                    Transazioni = await _api.GetTransazioniByUtenteAsync(IdUtente.Value);

                    // Calcola statistiche sulle transazioni filtrate
                    var transazioniFiltrate = TransazioniFiltrate.ToList();

                    TotaleEntrate = transazioniFiltrate
                        .Where(t => t.Importo > 0 && t.Stato == "Completato")
                        .Sum(t => t.Importo);

                    TotaleUscite = Math.Abs(transazioniFiltrate
                        .Where(t => t.Importo < 0 && t.Stato == "Completato")
                        .Sum(t => t.Importo));
                }
                //Altrimenti lascia la lista vuota (mostrerà il messaggio nella view)
            }
            catch (Exception ex)
            {
                ErrorMessage = "Errore nel caricamento delle transazioni: " + ex.Message;
            }

            // Messaggi da TempData
            if (TempData.ContainsKey("SuccessMessage"))
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            if (TempData.ContainsKey("ErrorMessage"))
                ErrorMessage = TempData["ErrorMessage"]?.ToString();

            return Page();
        }
    }
}