using DB.DBB;
using Digital_Pharma.Models;
using Digital_Pharma.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;



namespace DigitalPharma.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly OpenFdaService _fdaService;


        // هذا الـ constructor يقوم بحقن الخدمات الضرورية في الكونترولر:
        // 1. _context: هو الـ DbContext الخاص بالتطبيق للتعامل مع قاعدة البيانات (Users, Doctors, Searches)
        // 2. _fdaService: هو خدمة للتواصل مع قاعدة بيانات FDA لفحص تداخل الأدوية
        // ASP.NET Core يقوم تلقائيًا بإنشاء نسخة من هذه الخدمات من خلال Dependency Injection
        private readonly PharmaDbContext _context;
        public HomeController(PharmaDbContext context, OpenFdaService fdaService)
        {
            _context = context;      // تخزين DbContext لاستخدامه في باقي الـ methods
            _fdaService = fdaService; // تخزين خدمة FDA لاستخدامها عند فحص التداخل
        }
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> CheckInteraction(string drugName1, string drugName2)
        {
            if (string.IsNullOrWhiteSpace(drugName1) || string.IsNullOrWhiteSpace(drugName2))
            {
                ViewBag.Error = "⚠️ يرجى إدخال اسم الدواءين للفحص";
                return View("Index");
            }

            var task1 = _fdaService.GetDrugDataAsync(drugName1);
            var task2 = _fdaService.GetDrugDataAsync(drugName2);

            await Task.WhenAll(task1, task2);

            var res1 = await task1;
            var res2 = await task2;

            if (res1?.Results == null || !res1.Results.Any() || res2?.Results == null || !res2.Results.Any())
            {
                ViewBag.Error = "❌ تعذر العثور على أحد الأدوية في قاعدة بيانات FDA";
                return View("Index");
            }

            var drug1 = res1.Results[0];
            var drug2 = res2.Results[0];

            bool interactionFound = CheckForInteractions(drug1, drug2) || CheckForInteractions(drug2, drug1);

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

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(User newUser)
        {
            if (ModelState.IsValid) return View(newUser);
                {
                    var existUser = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == newUser.Email.ToLower());

                    if (existUser)
                    {
                        TempData["Message"] = "هذا البريد مسجل مسبقًا، الرجاء تسجيل الدخول";
                        return RedirectToAction("Login", "Home");
                    }

                    newUser.PasswordHash = HashPassword(newUser.Password);
                await _context.Users.AddAsync(newUser);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "تم التسجيل بنجاح، الرجاء تسجيل الدخول";
                    return RedirectToAction("Login", "Home");
                } }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            } 
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "الرجاء إدخال البريد وكلمة السر";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null || user.PasswordHash != HashPassword(password))
            {
                ViewBag.Error = "البريد أو كلمة السر غير صحيحة";
                return View();
            }

            // تسجيل الدخول ناجح → Redirect مباشرة للـ Index
            return RedirectToAction("Index", "Home");
        }
    }
    }
