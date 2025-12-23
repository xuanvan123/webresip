using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResipWeb.Models;
using System.Security.Claims;

namespace ResipWeb.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public CartSummaryViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            decimal totalMoney = 0;

            // 1. Kiểm tra người dùng đã đăng nhập chưa
            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    int userId = int.Parse(userIdStr);

                    // 2. Tính tổng tiền từ bảng GioHangs trong Database
                    totalMoney = await _context.GioHangs
                        .Where(x => x.UserId == userId)
                        .SumAsync(x => x.SoLuong * x.SanPham.GiaBan);
                }
            }

            // Trả về số tiền định dạng chuỗi (VD: 1.500.000)
            return View("Default", totalMoney.ToString("N0"));
        }
    }
}