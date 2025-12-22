using Digital_Pharma.Services;
using Microsoft.AspNetCore.Mvc;
using Digital_Pharma.Models;

namespace DigitalPharma.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly OpenFdaService _fdaService;

        public HomeController(OpenFdaService fdaService)
        {
            _fdaService = fdaService;
        }

        public IActionResult Index() => View();

        // يتم إضافة البارامترات هنا بين القوسين
        [HttpPost]
        public async Task<IActionResult> CheckInteraction(string drugName1, string drugName2)
        {
            // التحقق من أن المستخدم أدخل القيمتين
            if (string.IsNullOrWhiteSpace(drugName1) || string.IsNullOrWhiteSpace(drugName2))
            {
                ViewBag.Error = "⚠️ يرجى إدخال اسم الدواءين للفحص";
                return View("Index");
            }

            // طلب البيانات من الخدمة بشكل متوازي لتحسين السرعة
            var task1 = _fdaService.GetDrugDataAsync(drugName1);
            var task2 = _fdaService.GetDrugDataAsync(drugName2);

            await Task.WhenAll(task1, task2);

            var res1 = await task1;
            var res2 = await task2;

            // التأكد من وجود نتائج لكلا الدواءين
            if (res1?.Results == null || !res1.Results.Any() || res2?.Results == null || !res2.Results.Any())
            {
                ViewBag.Error = "❌ تعذر العثور على أحد الأدوية في قاعدة بيانات FDA";
                return View("Index");
            }

            var drug1 = res1.Results[0];
            var drug2 = res2.Results[0];

            // استدعاء منطق الفحص المتبادل
            bool interactionFound = CheckForInteractions(drug1, drug2) ||
                                   CheckForInteractions(drug2, drug1);

            var analysisResult = new InteractionAnalysis
            {
                Drug1 = drug1,
                Drug2 = drug2,
                InteractionFound = interactionFound,
                AnalysisMessage = interactionFound ?
                    "⚠️ تحذير: تم اكتشاف تفاعل محتمل بين هذين الدوائين بناءً على نصوص التحذيرات." :
                    "✅ لم يتم العثور على تداخل مباشر في نصوص التحذيرات المسجلة."
            };

            return View("Result", analysisResult);
        }

        // دالة منطق الفحص (Logic)
        private bool CheckForInteractions(OpenFdaResult sourceDrug, OpenFdaResult targetDrug)
        {
            var targetNames = new List<string>();
            if (targetDrug.OpenFda?.BrandName != null) targetNames.AddRange(targetDrug.OpenFda.BrandName);
            if (targetDrug.OpenFda?.GenericName != null) targetNames.AddRange(targetDrug.OpenFda.GenericName);

            if (sourceDrug.DrugInteractions == null) return false;

            foreach (var interactionText in sourceDrug.DrugInteractions)
            {
                foreach (var name in targetNames)
                {
                    if (interactionText.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}