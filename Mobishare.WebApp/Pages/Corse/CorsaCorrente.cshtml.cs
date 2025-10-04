using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Corse
{
    public class CorsaCorrenteModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public CorsaDto? CorsaAttiva { get; set; }

        public CorsaCorrenteModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MobishareApi");
        }

        public async Task OnGetAsync(int idUtente)
        {
            CorsaAttiva = await _httpClient.GetFromJsonAsync<CorsaDto>($"api/corse?idUtente={idUtente}");
        }
    }

}
