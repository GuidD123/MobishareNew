using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;

namespace Mobishare.WebApp.Pages.Ricariche;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public IndexModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public SaldoResponseDTO? Saldo { get; set; }
    public List<RicaricaResponseDTO> Ricariche { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public NuovaRicaricaInput InputRicarica { get; set; } = new();

    public class NuovaRicaricaInput
    {
        [Required(ErrorMessage = "L'importo è obbligatorio")]
        [Range(5, 500, ErrorMessage = "L'importo deve essere tra €5 e €500")]
        [Display(Name = "Importo Ricarica")]
        public decimal ImportoRicarica { get; set; }

        [Required(ErrorMessage = "Seleziona un metodo di pagamento")]
        [Display(Name = "Metodo di Pagamento")]
        public TipoRicarica TipoRicarica { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        await CaricaDatiAsync(userId.Value);

        // Messaggio da TempData (se presente)
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        // Ricarica dati per visualizzazione in caso di errore
        await CaricaDatiAsync(userId.Value);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new NuovaRicaricaDTO
            {
                IdUtente = userId.Value,
                ImportoRicarica = InputRicarica.ImportoRicarica,
                TipoRicarica = InputRicarica.TipoRicarica
            };

            var success = await _apiService.NuovaRicaricaAsync(dto);

            if (success)
            {
                SuccessMessage = $"Ricarica di €{InputRicarica.ImportoRicarica:0.00} completata con successo!";

                // Ricarica i dati aggiornati
                await CaricaDatiAsync(userId.Value);

                // Reset form
                ModelState.Clear();
                InputRicarica = new();
            }
            else
            {
                ErrorMessage = "Ricarica fallita. Riprova o contatta il supporto.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore durante la ricarica: {ex.Message}";
        }

        return Page();
    }

    private async Task CaricaDatiAsync(int userId)
    {
        try
        {
            // Carica saldo
            Saldo = await _apiService.GetSaldoUtenteAsync(userId);

            // Carica storico ricariche
            Ricariche = await _apiService.GetRicaricheUtenteAsync(userId);

            // Ordina per data decrescente (più recenti prima)
            Ricariche = Ricariche.OrderByDescending(r => r.DataRicarica).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento dei dati: {ex.Message}";
        }
    }
}