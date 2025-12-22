using Digital_Pharma.Models;
using System.Text.Json;

namespace Digital_Pharma.Services
{
    public class OpenFdaService
    {
        private readonly HttpClient _httpClient;

        public OpenFdaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OpenFdaResponse?> GetDrugDataAsync(string drugName)
        {
            if (string.IsNullOrEmpty(drugName)) return null;

            string encodedDrugName = Uri.EscapeDataString(drugName);
            string url = $"https://api.fda.gov/drug/label.json?search=(openfda.brand_name:\"{encodedDrugName}\"+openfda.generic_name:\"{encodedDrugName}\")&limit=1";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<OpenFdaResponse>(jsonString, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching {drugName}: {ex.Message}");
            }
            return null;
        }
    }
}