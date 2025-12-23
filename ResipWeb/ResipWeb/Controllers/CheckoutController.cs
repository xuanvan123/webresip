using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResipWeb.Models;
using ResipWeb.Models.ViewModels; // Namespace chứa CheckoutViewModel
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace ResipWeb.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            var cartItems = await GetCartItems();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // --- KHỚP CODE VỚI MODEL SANPHAM ---
            // Gia -> GiaBan
            // HinhAnh -> AnhChinh
            var model = new CheckoutViewModel
            {
                CartItems = cartItems.Select(x => new CartItemViewModel
                {
                    TenSanPham = x.SanPham.TenSanPham,
                    DonGia = x.SanPham.GiaBan,  // Sửa: Lấy từ GiaBan
                    SoLuong = x.SoLuong,
                    HinhAnh = x.SanPham.AnhChinh // Sửa: Lấy từ AnhChinh
                }).ToList(),
                TongTienHang = cartItems.Sum(x => x.SanPham.GiaBan * x.SoLuong), // Sửa: GiaBan
                PhiVanChuyen = 30000
            };

            return View(model);
        }
        public string? MauSac { get; set; }
        public string? KichThuoc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ThanhTien { get; set; }

        public DonHang? DonHang { get; set; }
        public SanPham? SanPham { get; set; }
        // POST: /Checkout/PlaceOrder
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = GetUserId();
                var cartItems = await GetCartItems();

                if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

                // --- KHỚP CODE VỚI MODEL DONHANG ---
                var donHang = new DonHang
                {
                    UserId = userId.ToString(),
                    // Tạo mã đơn hàng tự động (VD: DH638384...)
                    MaDonHang = "DH" + DateTime.Now.Ticks.ToString(),

                    HoTen = model.HoTen, // Khớp với DonHang.cs

                    DienThoai = model.SoDienThoai, // Khớp: DonHang dùng 'DienThoai'

                    DiaChi = $"{model.DiaChiCuThe}, {model.PhuongXa}, {model.TinhThanh}",
                    NgayTao = DateTime.Now, // Khớp: DonHang dùng 'NgayTao' (không phải NgayDat)

                    TrangThai = "ChoXuLy", // Khớp: DonHang dùng 'TrangThai'

                    TongTien = model.TongThanhToan

                    // LƯU Ý: Đã bỏ 'GhiChu' và 'PhuongThucThanhToan' vì Model DonHang của bạn chưa có.
                };

                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync();

                // --- LƯU CHI TIẾT ĐƠN HÀNG ---
                var chiTietList = new List<ChiTietDonHang>();
                foreach (var item in cartItems)
                {
                    chiTietList.Add(new ChiTietDonHang
                    {
                        DonHangId = donHang.Id,
                        SanPhamId = item.SanPhamId,
                        SoLuong = item.SoLuong,
                        DonGia = item.SanPham.GiaBan, // Lưu giá bán tại thời điểm mua
                        TenSanPham = item.SanPham.TenSanPham,

                    });
                }
                _context.ChiTietDonHangs.AddRange(chiTietList);

                // Xóa giỏ hàng
                _context.GioHangs.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return RedirectToAction("OrderSuccess");
            }

            // Nếu lỗi form, load lại dữ liệu để không bị lỗi View
            var cart = await GetCartItems();
            model.CartItems = cart.Select(x => new CartItemViewModel
            {
                TenSanPham = x.SanPham.TenSanPham,
                DonGia = x.SanPham.GiaBan, // Sửa: GiaBan
                SoLuong = x.SoLuong,
                HinhAnh = x.SanPham.AnhChinh // Sửa: AnhChinh
            }).ToList();
            model.TongTienHang = cart.Sum(x => x.SanPham.GiaBan * x.SoLuong); // Sửa: GiaBan

            return View("Index", model);
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

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