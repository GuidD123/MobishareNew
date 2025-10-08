using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs; 

namespace Mobishare.WebApp.Pages.Corse;

public class CorsaCorrenteModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public CorsaCorrenteModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public CorsaResponseDTO? CorsaAttiva { get; set; }
    public List<ParcheggioResponseDTO> ParcheggiDisponibili { get; set; } = new();
    public int DurataMinuti { get; set; }
    public decimal CostoStimato { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public FineCorsaInput InputFineCorsa { get; set; } = new();

    public class FineCorsaInput
    {
        [Required(ErrorMessage = "Seleziona il parcheggio di rilascio")]
        [Display(Name = "Parcheggio di Rilascio")]
        public int IdParcheggioRilascio { get; set; }

        [Display(Name = "Segnalazione Problema")]
        public bool SegnalazioneProblema { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        try
        {
            var tutteCorse = await _apiService.GetCorseAsync(idUtente: userId);
            CorsaAttiva = tutteCorse.FirstOrDefault(c => c.DataOraFine == null);

            if (CorsaAttiva != null)
            {
                var durata = DateTime.UtcNow - CorsaAttiva.DataOraInizio;
                DurataMinuti = (int)durata.TotalMinutes;

                const decimal COSTO_BASE = 1.00m;
                const decimal COSTO_AL_MINUTO = 0.10m;

                if (DurataMinuti <= 30)
                {
                    CostoStimato = COSTO_BASE;
                }
                else
                {
                    var minutiExtra = DurataMinuti - 30;
                    CostoStimato = COSTO_BASE + (minutiExtra * COSTO_AL_MINUTO);
                }

                ParcheggiDisponibili = await _apiService.GetParcheggiAsync();
                ParcheggiDisponibili = ParcheggiDisponibili.Where(p => p.Attivo).ToList();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento della corsa: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int idCorsa)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            var dto = new FineCorsaDTO
            {
                DataOraFineCorsa= DateTime.UtcNow,
                IdParcheggioRilascio= InputFineCorsa.IdParcheggioRilascio,
                SegnalazioneProblema= InputFineCorsa.SegnalazioneProblema
            };

            var corsaTerminata = await _apiService.TerminaCorsaAsync(idCorsa, dto);

            if (corsaTerminata != null)
            {
                TempData["SuccessMessage"] = $"Corsa terminata! Costo finale: €{corsaTerminata.CostoFinale:0.00}";
                return RedirectToPage("/Corse/Index");
            }
            else
            {
                ErrorMessage = "Impossibile terminare la corsa. Riprova.";
                await OnGetAsync();
                return Page();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore durante la terminazione della corsa: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }
}