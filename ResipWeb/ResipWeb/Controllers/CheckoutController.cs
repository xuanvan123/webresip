using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResipWeb.Models;
using ResipWeb.Models.ViewModels;
using System.Security.Claims;

namespace ResipWeb.Controllers
{
    [Authorize] // Bắt buộc đăng nhập để thanh toán
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Checkout
        // Hiển thị trang thanh toán với danh sách sản phẩm từ giỏ hàng
        public async Task<IActionResult> Index()
        {
            var cartItems = await GetCartItems();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cartItems.Select(x => new CartItemViewModel
                {
                    TenSanPham = x.SanPham.TenSanPham,
                    DonGia = x.SanPham.GiaBan,
                    SoLuong = x.SoLuong,
                    HinhAnh = x.SanPham.AnhChinh
                }).ToList(),
                TongTienHang = cartItems.Sum(x => x.SanPham.GiaBan * x.SoLuong),
                PhiVanChuyen = 30000
            };

            return View(model);
        }

        // POST: /Checkout/PlaceOrder
        // Xử lý đặt hàng, trừ tồn kho và xóa giỏ hàng
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cartItems = await GetCartItems();

            if (ModelState.IsValid)
            {
                if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

                var userId = GetUserId();

                // 1. Khởi tạo đơn hàng mới
                var donHang = new DonHang
                {
                    UserId = userId.ToString(),
                    MaDonHang = "DH" + DateTime.Now.Ticks.ToString(), // Mã duy nhất dựa trên thời gian
                    HoTen = model.HoTen,
                    DienThoai = model.SoDienThoai,
                    DiaChi = $"{model.DiaChiCuThe}, {model.PhuongXa}, {model.TinhThanh}",
                    NgayTao = DateTime.Now,
                    TrangThai = "ChoXuLy",
                };
                decimal tongTien = 0;
                var gioHang = _context.GioHangs
    .Include(g => g.SanPham)
    .Where(g => g.UserId == userId)
    .ToList();

                if (!gioHang.Any())
                {
                    ModelState.AddModelError("", "Giỏ hàng trống");
                    return View("Index", model);
                }

                foreach (var item in gioHang)
                {
                    tongTien += item.SoLuong * item.SanPham.GiaBan;
                }

                donHang.TongTien = tongTien;

                _context.DonHangs.Add(donHang);
                // Lưu để lấy được donHang.Id cho bước sau
                await _context.SaveChangesAsync();

                // 2. Lưu chi tiết đơn hàng VÀ CẬP NHẬT TỒN KHO SẢN PHẨM
                var chiTietList = new List<ChiTietDonHang>();
                foreach (var item in cartItems)
                {
                    // --- LOGIC CẬP NHẬT TỒN KHO ---
                    var product = await _context.SanPhams.FindAsync(item.SanPhamId);
                    if (product != null)
                    {
                        // Trừ số lượng tồn kho dựa trên số lượng khách mua
                        product.StockQuantity -= item.SoLuong;
                        _context.SanPhams.Update(product);
                    }

                    // Thêm vào danh sách chi tiết đơn hàng
                    chiTietList.Add(new ChiTietDonHang
                    {
                        DonHangId = donHang.Id,
                        SanPhamId = item.SanPhamId,
                        SoLuong = item.SoLuong,
                        DonGia = item.SanPham.GiaBan,
                        TenSanPham = item.SanPham.TenSanPham,
                    });
                }
                _context.ChiTietDonHangs.AddRange(chiTietList);

                // 3. Xóa các sản phẩm trong giỏ hàng của người dùng này
                _context.GioHangs.RemoveRange(cartItems);

                // 4. Lưu tất cả thay đổi cuối cùng vào Database
                await _context.SaveChangesAsync();

                return RedirectToAction("OrderSuccess");
            }

            // Nếu dữ liệu (ModelState) không hợp lệ, tải lại dữ liệu giỏ hàng và hiển thị lỗi
            model.CartItems = cartItems.Select(x => new CartItemViewModel
            {
                TenSanPham = x.SanPham.TenSanPham,
                DonGia = x.SanPham.GiaBan,
                SoLuong = x.SoLuong,
                HinhAnh = x.SanPham.AnhChinh
            }).ToList();
            model.TongTienHang = cartItems.Sum(x => x.SanPham.GiaBan * x.SoLuong);
            model.PhiVanChuyen = 30000;

            return View("Index", model);
        }

        // Trang thông báo đặt hàng thành công
        public IActionResult OrderSuccess()
        {
            return View();
        }

        // Lấy ID người dùng đang đăng nhập
        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim) : 0;
        }

        // Lấy danh sách sản phẩm trong giỏ hàng kèm thông tin Sản phẩm
        private async Task<List<GioHang>> GetCartItems()
        {
            var userId = GetUserId();
            return await _context.GioHangs
                .Include(x => x.SanPham)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }
    }
}
