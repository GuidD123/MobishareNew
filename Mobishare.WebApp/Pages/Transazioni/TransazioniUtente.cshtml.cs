using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.Core.DTOs;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Transazioni
{
    public class TransazioniUtenteModel(IMobishareApiService api) : PageModel
    {
        private readonly IMobishareApiService _api = api;

        public List<TransazioneResponseDTO> Transazioni { get; set; } = new();
        public string? ErrorMessage { get; set; }

        // Filtri
        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatoFiltro { get; set; }

        // Statistiche
        public decimal TotaleRicariche { get; set; }
        public decimal TotaleSpese { get; set; }

        public IEnumerable<TransazioneResponseDTO> TransazioniFiltrate
        {
            get
            {
                var query = Transazioni.AsEnumerable();

                if (!string.IsNullOrEmpty(TipoFiltro))
                {
                    query = query.Where(t => t.Tipo == TipoFiltro);
                }

                if (!string.IsNullOrEmpty(StatoFiltro))
                {
                    query = query.Where(t => t.Stato == StatoFiltro);
                }

                return query;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                Transazioni = await _api.GetMieTransazioniAsync();

                // Calcola statistiche
                TotaleRicariche = Transazioni
                    .Where(t => t.Importo > 0 && t.Stato == "Completato")
                    .Sum(t => t.Importo);

                TotaleSpese = Math.Abs(Transazioni
                    .Where(t => t.Importo < 0 && t.Stato == "Completato")
                    .Sum(t => t.Importo));

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Errore nel caricamento delle transazioni: " + ex.Message;
                return Page();
            }
        }
    }
}