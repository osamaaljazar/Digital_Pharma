using DB.DBB;
using Digital_Pharma.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// تسجيل الخدمات
builder.Services.AddScoped<OpenFdaService>();
builder.Services.AddHttpClient<OpenFdaService>();

builder.Services.AddControllersWithViews();

// إضافة DbContext
builder.Services.AddDbContext<PharmaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DigitalPharmaDB")));

// تفعيل الجلسة
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// تفعيل الجلسة قبل Authorization
app.UseSession();

app.UseAuthorization();

// Route افتراضي بدون .WithStaticAssets()
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();