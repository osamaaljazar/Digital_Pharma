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
            string encodedDrugName = Uri.EscapeDataString(drugName);
            string url = $"https://api.fda.gov/drug/label.json?search=openfda.brand_name:\"{encodedDrugName}\"&limit=1";

            try
            {
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine("Status Code: " + response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("JSON FROM FDA:\n" + jsonString);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<OpenFdaResponse>(jsonString, options);
                }
                else
                {
                    Console.WriteLine("Request failed: " + response.ReasonPhrase);
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine("HTTP Error: " + httpEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Error: " + ex.Message);
            }

            return null;
        }
    }
}
