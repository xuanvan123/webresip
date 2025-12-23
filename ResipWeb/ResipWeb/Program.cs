using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResipWeb.Models;

var builder = WebApplication.CreateBuilder(args);

// =======================
// SERVICES (ĐĂNG KÝ DỊCH VỤ)
// =======================
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// --- Đăng ký dịch vụ mã hóa mật khẩu ---
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// 🔥 AUTHENTICATION (CẤU HÌNH ĐĂNG NHẬP)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)

    .AddCookie(options =>
    {
        // --- QUAN TRỌNG: Cập nhật đường dẫn về trang Login của Khách ---
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // CHẠY HTTP (Development)
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "ResipWebCookie"; // Đặt tên cookie để dễ debug
    });

var app = builder.Build();

// =======================
// MIDDLEWARE (CẤU HÌNH PIPELINE)
// =======================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // BẮT BUỘC: Xác thực danh tính
app.UseAuthorization();    // BẮT BUỘC: Phân quyền truy cập



// 1. Định nghĩa cho vùng Admin
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Định nghĩa cho Trang chủ khách hàng (BẮT BUỘC nằm dưới)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();