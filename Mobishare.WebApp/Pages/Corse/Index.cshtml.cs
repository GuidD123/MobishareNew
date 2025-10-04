using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Corse
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public List<CorsaDto> Corse { get; set; } = new();

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MobishareApi");
        }

        public async Task OnGetAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<CorsaDto>>("api/corse");
            Corse = response ?? [];
        }
    }
}
