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
        private readonly PharmaDbContext _context;

        public HomeController(PharmaDbContext context, OpenFdaService fdaService)
        {
            _context = context;
            _fdaService = fdaService;
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
                    "⚠️ تحذير: تم اكتشاف تفاعل محتمل بين هذين الدوائين." :
                    "✅ لم يتم العثور على تداخل مباشر."
            };

            var userId = HttpContext.Session.GetInt32("UserId");
            int? doctorId = null;

            if (userId != null)
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                doctorId = doctor?.Id;
            }

            var search = new Search
            {
                Drug1Name = drug1.OpenFda?.BrandName?.FirstOrDefault() ?? drug1.OpenFda?.GenericName?.FirstOrDefault() ?? drugName1,
                Drug2Name = drug2.OpenFda?.BrandName?.FirstOrDefault() ?? drug2.OpenFda?.GenericName?.FirstOrDefault() ?? drugName2,
                InteractionFound = interactionFound,
                ResultMessage = analysisResult.AnalysisMessage,
                UserId = userId,
                DoctorId = doctorId
            };

            try
            {
                _context.Searches.Add(search);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ViewBag.Error = ex.InnerException?.Message ?? ex.Message;
                return View("Index", analysisResult);
            }

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
                        return true;
                }
            }
            return false;
        }

        [HttpGet]
        public IActionResult SignUp() => View();
        [HttpPost]
        public async Task<IActionResult> SignUp(User newUser)
        {
            if (ModelState.IsValid)
                return View(newUser);

            var existUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == newUser.Email.ToLower());
            if (existUser)
            {
                TempData["Message"] = "هذا البريد مسجل مسبقًا، الرجاء تسجيل الدخول";
                return RedirectToAction("Login", "Home");
            }

            // خليه مثل ما كان
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            TempData["Message"] = "تم التسجيل بنجاح، الرجاء تسجيل الدخول";
            return RedirectToAction("Login", "Home");
        }


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
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "الرجاء إدخال البريد وكلمة السر";
                return View();
            }

            var normalizedEmail = email.Trim().ToLower();
            var hashedPassword = HashPassword(password);

            // تحقق إذا يوجد مستخدم مطابق للبريد وكلمة السر
            bool userExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.PasswordHash == hashedPassword);

            if (!userExists)
            {
                ViewBag.Error = "البريد أو كلمة السر غير صحيحة";
                return View();
            }

            // جلب الـ UserId لتخزينه في الجلسة
            var userId = await _context.Users
                .Where(u => u.Email.ToLower() == normalizedEmail && u.PasswordHash == hashedPassword)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            HttpContext.Session.SetInt32("UserId", userId);

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult DoctorInfo()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Home");

            // إذا هذا المستخدم مسجل كدكتور مسبقاً → نرجعه للـ Index مباشرة
            bool isDoctor = _context.Doctors.Any(d => d.UserId == userId);
            if (isDoctor)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DoctorInfo(Doctor doctor)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Home");

            bool alreadyDoctor = await _context.Doctors.AnyAsync(d => d.UserId == userId);
            if (alreadyDoctor)
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View(doctor);

            doctor.UserId = userId.Value;
            doctor.IsVerified = false;

            await _context.Doctors.AddAsync(doctor);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

    }
}
