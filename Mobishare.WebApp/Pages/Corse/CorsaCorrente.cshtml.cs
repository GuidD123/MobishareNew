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
            return RedirectToPage("/Account/Login");
        }

        try
        {
            CorsaAttiva = await _apiService.GetCorsaAttivaAsync();

            if (CorsaAttiva != null)
            {
                var durata = DateTime.Now - CorsaAttiva.DataOraInizio;
                DurataMinuti = (int)durata.TotalMinutes;

                const decimal COSTO_BASE = 0.50m;

                decimal costoAlMinuto = CorsaAttiva.TipoMezzo switch
                {
                    "MonopattinoElettrico" => 0.25m,
                    "BiciElettrica" => 0.20m,
                    "BiciMuscolare" => 0.10m,
                    _ => 0.10m // fallback di sicurezza
                };


                if (DurataMinuti <= 30)
                {
                    CostoStimato = COSTO_BASE;
                }
                else
                {
                    var minutiExtra = DurataMinuti - 30;
                    CostoStimato = COSTO_BASE + (minutiExtra * costoAlMinuto);
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
            return RedirectToPage("/Account/Login");
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
                DataOraFineCorsa= DateTime.Now,
                IdParcheggioRilascio= InputFineCorsa.IdParcheggioRilascio,
                SegnalazioneProblema= InputFineCorsa.SegnalazioneProblema
            };

            var corsaTerminata = await _apiService.TerminaCorsaAsync(idCorsa, dto);

            if (corsaTerminata != null)
            {
                if (InputFineCorsa.SegnalazioneProblema)
                {
                    try
                    {
                        await _apiService.SegnalaGuastoAsync(corsaTerminata.MatricolaMezzo);
                    }
                    catch (Exception exGuasto)
                    {
                        // Non bloccare la terminazione corsa, ma registra l’errore
                        Console.WriteLine($"[WARNING] Errore segnalazione guasto mezzo {corsaTerminata.MatricolaMezzo}: {exGuasto.Message}");
                    }
                }

                TempData["SuccessMessage"] = InputFineCorsa.SegnalazioneProblema
                ? $"Corsa terminata. Mezzo segnalato come guasto. Costo finale: €{corsaTerminata.CostoFinale:0.00}"
                : $"Corsa terminata! Costo finale: €{corsaTerminata.CostoFinale:0.00}";

                return RedirectToPage("/Corse/LasciaFeedback", new { idCorsa = corsaTerminata.Id }); 
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