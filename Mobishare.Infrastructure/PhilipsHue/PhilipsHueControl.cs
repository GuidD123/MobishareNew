using Microsoft.Extensions.Logging;
using Mobishare.Core.Enums;
using System.Text;

namespace Mobishare.Infrastructure.PhilipsHue
{
    public class PhilipsHueControl
    {
        private readonly HttpClient _client;
        private readonly ILogger<PhilipsHueControl> _logger;

        // Mapping Matricola → Light ID numerico nell'emulatore
        // Configura questo in base alle luci che hai creato nell'emulatore
        private readonly Dictionary<string, string> _matricolaToLightId = new()
        {
            { "BE002", "1" },
            { "BE005", "2" },
            { "BE008", "3" },
            { "BE009", "4" },
            { "BE013", "5" },
            { "BE016", "6" },
            { "BE017", "7" },
            { "BM001", "8" },
            { "BM004", "9" },
            { "BM007", "10" },
            { "BM011", "11" },
            { "BM015", "12" },
            { "ME003", "13" },
            { "ME006", "14" },
            { "ME010", "15" },
            { "ME012", "16" },
            { "ME014", "17" },
            { "ME018", "18" }
        };

        public PhilipsHueControl(IHttpClientFactory factory, ILogger<PhilipsHueControl> logger)
        {
            //usa il client registrato in Program.cs
            _client = factory.CreateClient("PhilipsHue");
            _logger = logger;
        }

        public async Task ChangeLightState(string lightId, string requestBody)
        {
            // Converti matricola → light ID numerico se esiste mapping
            var actualLightId = _matricolaToLightId.TryGetValue(lightId, out var mappedId) 
                ? mappedId 
                : lightId;

            var url = $"lights/{actualLightId}/state";
            _logger.LogInformation("Cambiando lo stato della luce {LightId} (matricola: {Matricola}) con body: {Body}", 
                actualLightId, lightId, requestBody);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PutAsync(url, content);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Stato della luce {LightId} cambiato con successo.", actualLightId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il cambio dello stato per la luce {LightId}.", actualLightId);
                throw; // Rilancia l'eccezione
            }
        }
        public async Task SetSpiaColor(string lightId, ColoreSpia colore)
        {
            string body = colore switch
            {
                ColoreSpia.Rosso => "{\"on\": true, \"hue\": 0, \"sat\": 254, \"bri\": 254}",
                ColoreSpia.Verde => "{\"on\": true, \"hue\": 25500, \"sat\": 254, \"bri\": 254}",
                ColoreSpia.Blu => "{\"on\": true, \"hue\": 46920, \"sat\": 254, \"bri\": 254}",
                ColoreSpia.Giallo => "{\"on\": true, \"hue\": 12750, \"sat\": 254, \"bri\": 254}",
                ColoreSpia.Spenta => "{\"on\": false}",
                _ => "{\"on\": true}"
            };

            await ChangeLightState(lightId, body);
        }
    }
}
