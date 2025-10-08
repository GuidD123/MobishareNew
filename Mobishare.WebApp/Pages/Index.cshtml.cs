using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages;

public class IndexModel(IMobishareApiService apiService) : PageModel
{
    private readonly IMobishareApiService _apiService = apiService;

    public bool IsLogged { get; set; }
    public string NomeUtente { get; set; } = string.Empty;
    public decimal Credito { get; set; }
    public decimal DebitoResiduo { get; set; }
    public bool Sospeso { get; set; }
    public bool HasCorsaInCorso { get; set; }

    public async Task OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId.HasValue)
        {
            IsLogged = true;
            NomeUtente = HttpContext.Session.GetString("UserName") ?? "Utente";

            try
            {
                // Carica dati utente
                var utente = await _apiService.GetUtenteAsync(userId.Value);
                if (utente != null)
                {
                    Credito = utente.Credito;
                    Sospeso = utente.Sospeso;
                }

                // Verifica corsa in corso
                var corse = await _apiService.GetStoricoCorseUtenteAsync(userId.Value);
                HasCorsaInCorso = corse.Any(c => c.DataOraFine == null);
            }
            catch
            {
                // Ignora errori, mostra homepage base
            }
        }
    }
}