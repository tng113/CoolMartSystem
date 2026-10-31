using Microsoft.EntityFrameworkCore;
using CoolMart.API.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CoolMart.API.Data
{
    public class CoolMartDbContext : DbContext
    {
        public CoolMartDbContext(DbContextOptions<CoolMartDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Seed danh mục nhóm hàng đặc thù Cool Mart
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Kem Que & Kem Hộp", Description = "Kem ốc quế, kem ly, kem hộp các vị" },
                new Category { CategoryId = 2, CategoryName = "Nước Giải Khát Có Ga", Description = "Coca, Pepsi, 7Up lon và chai" },
                new Category { CategoryId = 3, CategoryName = "Trà Đóng Chai & Nước Trái Cây", Description = "Trà xanh, trà sữa, nước ép trái cây" },
                new Category { CategoryId = 4, CategoryName = "Nước Khoáng & Nước Tăng Lực", Description = "Lavie, Aquafina, RedBull, Sting" },
                new Category { CategoryId = 5, CategoryName = "Bánh Kẹo & Đồ Ăn Vặt Lạnh", Description = "Bánh ăn kèm kem, snack đồ khô" }
            );

            // 2. Seed 65 mặt hàng mẫu chuẩn thực tế khớp với hệ thống POS và Quản lý Kho
            modelBuilder.Entity<Product>().HasData(
                new Product { ProductId = 1, Barcode = "893456789001", ProductName = "Kem Ốc Quế Cornetto Socola", Price = 18000, StockQuantity = 120, CategoryId = 1 },
                new Product { ProductId = 2, Barcode = "893456789002", ProductName = "Kem Hộp Vinamilk Vani 450ml", Price = 48000, StockQuantity = 45, CategoryId = 1 },
                new Product { ProductId = 3, Barcode = "893456789003", ProductName = "Kem Que Merino Đậu Đỏ", Price = 12000, StockQuantity = 150, CategoryId = 1 },
                new Product { ProductId = 4, Barcode = "893456789004", ProductName = "Kem Que Celano Bánh Cá", Price = 22000, StockQuantity = 8, CategoryId = 1 },
                new Product { ProductId = 5, Barcode = "893456789005", ProductName = "Nước Ngọt Coca Cola Lon 320ml", Price = 10000, StockQuantity = 320, CategoryId = 2 },
                new Product { ProductId = 6, Barcode = "893456789006", ProductName = "Nước Ngọt Pepsi Không Calo Lon 320ml", Price = 10000, StockQuantity = 200, CategoryId = 2 },
                new Product { ProductId = 7, Barcode = "893456789007", ProductName = "Nước Ngọt Sprite Chai 390ml", Price = 9000, StockQuantity = 110, CategoryId = 2 },
                new Product { ProductId = 8, Barcode = "893456789008", ProductName = "Trà Xanh Không Độ Chai 455ml", Price = 11000, StockQuantity = 180, CategoryId = 3 },
                new Product { ProductId = 9, Barcode = "893456789009", ProductName = "Trà Ô Long Tea+ Chai 450ml", Price = 11000, StockQuantity = 160, CategoryId = 3 },
                new Product { ProductId = 10, Barcode = "893456789010", ProductName = "Nước Ép Cam Teppy Chai 327ml", Price = 13000, StockQuantity = 6, CategoryId = 3 },
                new Product { ProductId = 11, Barcode = "893456789011", ProductName = "Nước Tăng Lực Red Bull Lon 250ml", Price = 15000, StockQuantity = 140, CategoryId = 4 },
                new Product { ProductId = 12, Barcode = "893456789012", ProductName = "Nước Tăng Lực Sting Dâu Chai 330ml", Price = 11000, StockQuantity = 210, CategoryId = 4 },
                new Product { ProductId = 13, Barcode = "893456789013", ProductName = "Nước Khoáng Thiên Nhiên Lavie 500ml", Price = 6000, StockQuantity = 300, CategoryId = 4 },
                new Product { ProductId = 14, Barcode = "893456789014", ProductName = "Snack Khoai Tây O'Star 60g", Price = 12500, StockQuantity = 90, CategoryId = 5 },
                new Product { ProductId = 15, Barcode = "893456789015", ProductName = "Bánh Chocopie Orion Hộp 12 Cái", Price = 54000, StockQuantity = 50, CategoryId = 5 },

                // Mặt hàng que kem Merino thực tế bạn đang test
                new Product { ProductId = 16, Barcode = "893500110029", ProductName = "Kem Que Merino Đậu Xanh Nước Cốt Dừa 60g", Price = 10000, StockQuantity = 180, CategoryId = 1 },
                new Product { ProductId = 17, Barcode = "893456789017", ProductName = "Kem Ốc Quế Cornetto Dâu", Price = 18000, StockQuantity = 95, CategoryId = 1 },
                new Product { ProductId = 18, Barcode = "893456789018", ProductName = "Kem Hộp Vinamilk Socola 450ml", Price = 48000, StockQuantity = 40, CategoryId = 1 },
                new Product { ProductId = 19, Barcode = "893456789019", ProductName = "Kem Que Merino Sữa Chua Nếp Cẩm", Price = 12000, StockQuantity = 130, CategoryId = 1 },
                new Product { ProductId = 20, Barcode = "893456789020", ProductName = "Kem Celano Sôcôla Hạnh Nhân 70g", Price = 25000, StockQuantity = 65, CategoryId = 1 },
                new Product { ProductId = 21, Barcode = "893456789021", ProductName = "Kem Que Celano Trà Xanh 65g", Price = 22000, StockQuantity = 75, CategoryId = 1 },
                new Product { ProductId = 22, Barcode = "893456789022", ProductName = "Kem Hộp Merino Cốm Dừa 400ml", Price = 42000, StockQuantity = 50, CategoryId = 1 },
                new Product { ProductId = 23, Barcode = "893456789023", ProductName = "Kem Que Merino Khoai Môn", Price = 12000, StockQuantity = 110, CategoryId = 1 },

                new Product { ProductId = 24, Barcode = "893456789024", ProductName = "Nước Ngọt 7Up Lon 320ml", Price = 10000, StockQuantity = 220, CategoryId = 2 },
                new Product { ProductId = 25, Barcode = "893456789025", ProductName = "Nước Ngọt Mirinda Cam Lon 320ml", Price = 10000, StockQuantity = 190, CategoryId = 2 },
                new Product { ProductId = 26, Barcode = "893456789026", ProductName = "Nước Ngọt Mirinda Xá Xị Lon 320ml", Price = 10000, StockQuantity = 140, CategoryId = 2 },
                new Product { ProductId = 27, Barcode = "893456789027", ProductName = "Nước Ngọt Fanta Cam Chai 390ml", Price = 9000, StockQuantity = 130, CategoryId = 2 },
                new Product { ProductId = 28, Barcode = "893456789028", ProductName = "Nước Ngọt Coca Cola Chai 1.5L", Price = 22000, StockQuantity = 80, CategoryId = 2 },
                new Product { ProductId = 29, Barcode = "893456789029", ProductName = "Nước Ngọt Pepsi Chai 1.5L", Price = 22000, StockQuantity = 75, CategoryId = 2 },
                new Product { ProductId = 30, Barcode = "893456789030", ProductName = "Nước Ngọt Sprite Chai 1.5L", Price = 21000, StockQuantity = 60, CategoryId = 2 },

                new Product { ProductId = 31, Barcode = "893456789031", ProductName = "Trà Đào C2 Chai 455ml", Price = 10000, StockQuantity = 210, CategoryId = 3 },
                new Product { ProductId = 32, Barcode = "893456789032", ProductName = "Trà Chanh C2 Chai 455ml", Price = 10000, StockQuantity = 250, CategoryId = 3 },
                new Product { ProductId = 33, Barcode = "893456789033", ProductName = "Trà Sữa Macchiato Không Độ 268ml", Price = 14000, StockQuantity = 90, CategoryId = 3 },
                new Product { ProductId = 34, Barcode = "893456789034", ProductName = "Nước Ép Táo Vfresh Hộp 1L", Price = 42000, StockQuantity = 40, CategoryId = 3 },
                new Product { ProductId = 35, Barcode = "893456789035", ProductName = "Nước Ép Cam Vfresh Hộp 1L", Price = 45000, StockQuantity = 35, CategoryId = 3 },
                new Product { ProductId = 36, Barcode = "893456789036", ProductName = "Trà Bí Đao Wonderfarm Lon 310ml", Price = 9500, StockQuantity = 180, CategoryId = 3 },
                new Product { ProductId = 37, Barcode = "893456789037", ProductName = "Trà Lipton Chanh Chai 450ml", Price = 11000, StockQuantity = 150, CategoryId = 3 },
                new Product { ProductId = 38, Barcode = "893456789038", ProductName = "Nước Yến Ngân Nhĩ Tribeco Lon 240ml", Price = 12000, StockQuantity = 110, CategoryId = 3 },

                new Product { ProductId = 39, Barcode = "893456789039", ProductName = "Nước Khoáng Aquafina Chai 500ml", Price = 6000, StockQuantity = 400, CategoryId = 4 },
                new Product { ProductId = 40, Barcode = "893456789040", ProductName = "Nước Khoáng Dasani Chai 500ml", Price = 5500, StockQuantity = 320, CategoryId = 4 },
                new Product { ProductId = 41, Barcode = "893456789041", ProductName = "Nước Tăng Lực Wake Up 247 Chai 330ml", Price = 10000, StockQuantity = 260, CategoryId = 4 },
                new Product { ProductId = 42, Barcode = "893456789042", ProductName = "Nước Tăng Lực Monster Energy Lon 355ml", Price = 32000, StockQuantity = 85, CategoryId = 4 },
                new Product { ProductId = 43, Barcode = "893456789043", ProductName = "Nước Khoáng Vĩnh Hảo Chai 500ml", Price = 6500, StockQuantity = 200, CategoryId = 4 },
                new Product { ProductId = 44, Barcode = "893456789044", ProductName = "Nước Bù Khoáng Pocari Sweat Chai 500ml", Price = 15000, StockQuantity = 120, CategoryId = 4 },
                new Product { ProductId = 45, Barcode = "893456789045", ProductName = "Nước Bù Điện Giải Revive Chai 500ml", Price = 11000, StockQuantity = 175, CategoryId = 4 },
                new Product { ProductId = 46, Barcode = "893456789046", ProductName = "Nước Khoáng Thiên Nhiên Lavie Chai 1.5L", Price = 12000, StockQuantity = 130, CategoryId = 4 },

                new Product { ProductId = 47, Barcode = "893456789047", ProductName = "Snack Lay's Vị Tự Nhiên Gói 56g", Price = 13000, StockQuantity = 140, CategoryId = 5 },
                new Product { ProductId = 48, Barcode = "893456789048", ProductName = "Snack Lay's Vị Tảo Biển Gói 56g", Price = 13000, StockQuantity = 125, CategoryId = 5 },
                new Product { ProductId = 49, Barcode = "893456789049", ProductName = "Snack Mực Bento Thái Lan Màu Đỏ 20g", Price = 6500, StockQuantity = 160, CategoryId = 5 },
                new Product { ProductId = 50, Barcode = "893456789050", ProductName = "Snack Bắp Ngọt Oishi Gói 40g", Price = 6000, StockQuantity = 180, CategoryId = 5 },
                new Product { ProductId = 51, Barcode = "893456789051", ProductName = "Bánh Quy Bơ Danisa Hộp Thiếc 200g", Price = 58000, StockQuantity = 40, CategoryId = 5 },
                new Product { ProductId = 52, Barcode = "893456789052", ProductName = "Bánh Que Pocky Vị Dâu Hộp 38g", Price = 14000, StockQuantity = 110, CategoryId = 5 },
                new Product { ProductId = 53, Barcode = "893456789053", ProductName = "Bánh Que Pocky Sôcôla Hộp 40g", Price = 14000, StockQuantity = 120, CategoryId = 5 },
                new Product { ProductId = 54, Barcode = "893456789054", ProductName = "Bánh Xốp Nabati Phô Mai Hộp 160g", Price = 22000, StockQuantity = 95, CategoryId = 5 },
                new Product { ProductId = 55, Barcode = "893456789055", ProductName = "Kẹo Dẻo Chupa Chups Hương Trái Cây 90g", Price = 15000, StockQuantity = 130, CategoryId = 5 },
                new Product { ProductId = 56, Barcode = "893456789056", ProductName = "Kẹo Sing-gum Doublemint Hũ 58.4g", Price = 26000, StockQuantity = 80, CategoryId = 5 },
                new Product { ProductId = 57, Barcode = "893456789057", ProductName = "Hạt Điều Rang Muối Bazan Hộp 250g", Price = 75000, StockQuantity = 30, CategoryId = 5 },
                new Product { ProductId = 58, Barcode = "893456789058", ProductName = "Khô Bò Sấy Cay Thượng Hạng Gói 50g", Price = 42000, StockQuantity = 55, CategoryId = 5 },

                // Các mặt hàng nhu yếu phẩm & gia vị phổ biến
                new Product { ProductId = 59, Barcode = "893456789059", ProductName = "Mì Ăn Liền Hảo Hảo Tôm Chua Cay 75g", Price = 4500, StockQuantity = 600, CategoryId = 5 },
                new Product { ProductId = 60, Barcode = "893456789060", ProductName = "Mì Trộn Tương Đen Koreno Gói 100g", Price = 13000, StockQuantity = 150, CategoryId = 5 },
                new Product { ProductId = 61, Barcode = "893456789061", ProductName = "Sữa Tươi Tiệt Trùng Vinamilk 100% Hộp 1L", Price = 36000, StockQuantity = 85, CategoryId = 3 },
                new Product { ProductId = 62, Barcode = "893456789062", ProductName = "Sữa Tươi TH True MILK Ít Đường Hộp 1L", Price = 38000, StockQuantity = 90, CategoryId = 3 },
                new Product { ProductId = 63, Barcode = "893456789063", ProductName = "Sữa Chua Ăn Vinamilk Có Đường Hộp 100g", Price = 7500, StockQuantity = 200, CategoryId = 1 },
                new Product { ProductId = 64, Barcode = "893456789064", ProductName = "Khăn Giấy Lụa Pulppy Hộp 180 Tờ", Price = 25000, StockQuantity = 110, CategoryId = 5 },
                new Product { ProductId = 65, Barcode = "893456789065", ProductName = "Khăn Giấy Ướt Baby Care Gói 80 Tờ", Price = 28000, StockQuantity = 95, CategoryId = 5 }
            );
            // 3. Seed tài khoản người dùng đủ 3 vai trò
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin01", Password = "123", FullName = "Nguyễn Quản Trị", Role = "Admin", IsActive = true },
                new User { Id = 2, Username = "cashier01", Password = "123", FullName = "Lê Thu Ngân", Role = "Cashier", IsActive = true },
                new User { Id = 3, Username = "ware01", Password = "123", FullName = "Trần Thủ Kho", Role = "Warehouse", IsActive = true }
            );

            // 4. Seed khách hàng thân thiết có điểm tích lũy
            modelBuilder.Entity<Customer>().HasData(
    new Customer { CustomerId = 1, Address = "Quận 1, TP.HCM", CustomerName = "Nguyễn Văn An", MembershipRank = "Vàng", PhoneNumber = "0901234567", RewardPoints = 50 },
    new Customer { CustomerId = 2, Address = "Bình Thạnh, TP.HCM", CustomerName = "Trần Thị Bình", MembershipRank = "Bạc", PhoneNumber = "0912345678", RewardPoints = 20 },
    new Customer { CustomerId = 3, Address = "Hải Châu, Đà Nẵng", CustomerName = "Khách Vãng Lai", MembershipRank = "Chuẩn", PhoneNumber = "0000000000", RewardPoints = 0 },
    new Customer { CustomerId = 4, Address = "Quận 3, TP.HCM", CustomerName = "Nguyễn Văn A", MembershipRank = "VIP", PhoneNumber = "0919700001", RewardPoints = 500 },
    new Customer { CustomerId = 5, Address = "Thủ Dầu Một, Bình Dương", CustomerName = "Lê Thị B", MembershipRank = "Thành viên", PhoneNumber = "0907725032", RewardPoints = 120 },
    new Customer { CustomerId = 6, Address = "Biên Hòa, Đồng Nai", CustomerName = "Phạm Văn VIP", MembershipRank = "VIP", PhoneNumber = "0933445566", RewardPoints = 1500 },
    new Customer { CustomerId = 7, Address = "Cầu Giấy, Hà Nội", CustomerName = "Hoàng Minh Tuấn", MembershipRank = "Vàng", PhoneNumber = "0987654321", RewardPoints = 85 },
    new Customer { CustomerId = 8, Address = "Đống Đa, Hà Nội", CustomerName = "Đỗ Mai Phương", MembershipRank = "Thành viên", PhoneNumber = "0978123456", RewardPoints = 110 },
    new Customer { CustomerId = 9, Address = "Ninh Kiều, Cần Thơ", CustomerName = "Võ Quốc Cường", MembershipRank = "Chuẩn", PhoneNumber = "0939112233", RewardPoints = 15 },
    new Customer { CustomerId = 10, Address = "Thủ Đức, TP.HCM", CustomerName = "Bùi Tuyết Mai", MembershipRank = "VIP", PhoneNumber = "0908889977", RewardPoints = 650 },
    new Customer { CustomerId = 11, Address = "Quận 7, TP.HCM", CustomerName = "Đặng Đình Khang", MembershipRank = "Bạc", PhoneNumber = "0918776655", RewardPoints = 40 },
    new Customer { CustomerId = 12, Address = "Tân Bình, TP.HCM", CustomerName = "Ngô Bích Thủy", MembershipRank = "Thành viên", PhoneNumber = "0903332211", RewardPoints = 180 },
    new Customer { CustomerId = 13, Address = "Thanh Xuân, Hà Nội", CustomerName = "Dương Gia Huy", MembershipRank = "VIP", PhoneNumber = "0988665544", RewardPoints = 820 },
    new Customer { CustomerId = 14, Address = "Quận 10, TP.HCM", CustomerName = "Lý Ánh Nguyệt", MembershipRank = "Chuẩn", PhoneNumber = "0938990011", RewardPoints = 5 },
    new Customer { CustomerId = 15, Address = "Sơn Trà, Đà Nẵng", CustomerName = "Phan Thanh Tùng", MembershipRank = "Vàng", PhoneNumber = "0905123789", RewardPoints = 95 },
    new Customer { CustomerId = 16, Address = "Quận 5, TP.HCM", CustomerName = "Lâm Quốc Bảo", MembershipRank = "Thành viên", PhoneNumber = "0909654321", RewardPoints = 210 },
    new Customer { CustomerId = 17, Address = "Bình Tân, TP.HCM", CustomerName = "Vũ Thùy Linh", MembershipRank = "Bạc", PhoneNumber = "0913456789", RewardPoints = 35 },
    new Customer { CustomerId = 18, Address = "Hà Đông, Hà Nội", CustomerName = "Trịnh Kim Ngân", MembershipRank = "VIP", PhoneNumber = "0989001122", RewardPoints = 1200 },
    new Customer { CustomerId = 19, Address = "Nha Trang, Khánh Hòa", CustomerName = "Trương Bá Thắng", MembershipRank = "Chuẩn", PhoneNumber = "0935112244", RewardPoints = 0 },
    new Customer { CustomerId = 20, Address = "Phan Thiết, Bình Thuận", CustomerName = "Tạ Nhật Nam", MembershipRank = "Thành viên", PhoneNumber = "0906223344", RewardPoints = 130 },
    new Customer { CustomerId = 21, Address = "Vũng Tàu, BR-VT", CustomerName = "Hồ Ngọc Hà", MembershipRank = "Vàng", PhoneNumber = "0914556677", RewardPoints = 75 },
    new Customer { CustomerId = 22, Address = "Quận 12, TP.HCM", CustomerName = "Lê Văn Đạt", MembershipRank = "Chuẩn", PhoneNumber = "0937889900", RewardPoints = 25 },
    new Customer { CustomerId = 23, Address = "Hoàn Kiếm, Hà Nội", CustomerName = "Mai Phương Thúy", MembershipRank = "VIP", PhoneNumber = "0982334455", RewardPoints = 940 },
    new Customer { CustomerId = 24, Address = "Long Xuyên, An Giang", CustomerName = "Nguyễn Tấn Phát", MembershipRank = "Thành viên", PhoneNumber = "0907119988", RewardPoints = 160 },
    new Customer { CustomerId = 25, Address = "Huế, Thừa Thiên Huế", CustomerName = "Trần Như Quỳnh", MembershipRank = "Bạc", PhoneNumber = "0916223388", RewardPoints = 45 },
    new Customer { CustomerId = 26, Address = "Rạch Giá, Kiên Giang", CustomerName = "Đinh Trọng Hưng", MembershipRank = "Chuẩn", PhoneNumber = "0934556611", RewardPoints = 10 },
    new Customer { CustomerId = 27, Address = "Buôn Ma Thuột, Đắk Lắk", CustomerName = "Cao Thái Sơn", MembershipRank = "Vàng", PhoneNumber = "0902998877", RewardPoints = 90 },
    new Customer { CustomerId = 28, Address = "Quận 4, TP.HCM", CustomerName = "Văn Thị Mỹ", MembershipRank = "Thành viên", PhoneNumber = "0917334411", RewardPoints = 140 },
    new Customer { CustomerId = 29, Address = "Quận 8, TP.HCM", CustomerName = "Tô Văn Hùng", MembershipRank = "VIP", PhoneNumber = "0981223344", RewardPoints = 1100 },
    new Customer { CustomerId = 30, Address = "Gò Vấp, TP.HCM", CustomerName = "Chu Thanh Huyền", MembershipRank = "Bạc", PhoneNumber = "0904887766", RewardPoints = 30 },
    new Customer { CustomerId = 31, Address = "Bắc Ninh", CustomerName = "Phùng Quý Đôn", MembershipRank = "Chuẩn", PhoneNumber = "0932445566", RewardPoints = 0 },
    new Customer { CustomerId = 32, Address = "Hải Phòng", CustomerName = "Lê Bảo Ngọc", MembershipRank = "Vàng", PhoneNumber = "0915667788", RewardPoints = 65 },
    new Customer { CustomerId = 33, Address = "Nam Định", CustomerName = "Trần Đình Trọng", MembershipRank = "Thành viên", PhoneNumber = "0984556677", RewardPoints = 190 },
    new Customer { CustomerId = 34, Address = "Thái Nguyên", CustomerName = "Nguyễn Kiều Oanh", MembershipRank = "VIP", PhoneNumber = "0909114477", RewardPoints = 780 },
    new Customer { CustomerId = 35, Address = "Phú Nhuận, TP.HCM", CustomerName = "Lê Quang Liêm", MembershipRank = "Vàng", PhoneNumber = "0918225588", RewardPoints = 80 }
);
        }
    }
}