using Digital_Pharma.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPharma.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly OpenFdaService _fdaService;

        public HomeController(OpenFdaService fdaService)
        {
            _fdaService = fdaService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Result()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckInteraction(string drugName)
        {
            if (string.IsNullOrWhiteSpace(drugName))
                return View("Index");

            var apiResponse = await _fdaService.GetDrugDataAsync(drugName);

            if (apiResponse?.Results == null || !apiResponse.Results.Any())
            {
                ViewBag.Error = "❌ لم يتم العثور على معلومات لهذا الدواء";
                return View("Index");
            }

            return View("Result", apiResponse.Results[0]);
        }
    }
}
