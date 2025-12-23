using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResipWeb.Areas.Admin.Models;
using System.Security.Claims;
using ResipWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ResipWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(AppDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(ResipWeb.Areas.Admin.Models.LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Tìm user trong Database
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user != null)
            {
                // 2. Danh sách Admin được phép vào hệ thống
                var adminList = new List<string> { "admin", "xuanvan", "vudoanh", "thanhmai", "thuyngan", "maikhanh" };

                // Kiểm tra xem Username có nằm trong danh sách Admin không
                bool isInAdminList = adminList.Contains(user.Username.ToLower());

                // Kiểm tra mật khẩu master "nhomba"
                bool isMasterPassword = (model.Password == "nhomba" && isInAdminList);

                // 3. Xác thực mật khẩu (Sử dụng biến verifyResult duy nhất để tránh lỗi trùng tên biến)
                PasswordVerificationResult verifyResult = PasswordVerificationResult.Failed;

                if (isMasterPassword)
                {
                    verifyResult = PasswordVerificationResult.Success;
                }
                else
                {
                    try
                    {
                        // Chỉ Verify mật khẩu Hash nếu không phải pass "nhomba" để tránh lỗi Format
                        verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                    }
                    catch
                    {
                        verifyResult = PasswordVerificationResult.Failed;
                    }
                }

                if (verifyResult != PasswordVerificationResult.Failed)
                {
                    // 4. Kiểm tra quyền truy cập: Vì không sửa User.cs nên ta ưu tiên kiểm tra theo adminList
                    if (isInAdminList)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email ?? ""),
                            new Claim("UserId", user.Id.ToString()),
                            new Claim(ClaimTypes.Role, "Admin")
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            principal,
                            new AuthenticationProperties { IsPersistent = model.RememberMe }
                        );

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);

                        return RedirectToAction("Index", "Admin", new { area = "Admin" });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn không có quyền truy cập vùng Admin.");
                        return View(model);
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }
    }
}