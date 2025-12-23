using System;
using System.Collections.Generic;

namespace ResipWeb.Models
{
    public class DonHang
    {
        public int Id { get; set; }
        public string MaDonHang { get; set; }
        public string HoTen { get; set; }
        public string? DienThoai { get; set; }
        public string DiaChi { get; set; }
        public decimal? TongTien { get; set; }
        public string TrangThai { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? UserId { get; set; }
        // Danh sách các món trong đơn hàng
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
    = new List<ChiTietDonHang>();

    }
}