using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoolMart.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoolMartDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RewardPoints = table.Column<int>(type: "int", nullable: false),
                    MembershipRank = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CashierUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RewardPoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailId);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "Description" },
                values: new object[,]
                {
                    { 1, "Kem Que & Kem Hộp", "Kem ốc quế, kem ly, kem hộp các vị" },
                    { 2, "Nước Giải Khát Có Ga", "Coca, Pepsi, 7Up lon và chai" },
                    { 3, "Trà Đóng Chai & Nước Trái Cây", "Trà xanh, trà sữa, nước ép trái cây" },
                    { 4, "Nước Khoáng & Nước Tăng Lực", "Lavie, Aquafina, RedBull, Sting" },
                    { 5, "Bánh Kẹo & Đồ Ăn Vặt Lạnh", "Bánh ăn kèm kem, snack đồ khô" }
                });

            migrationBuilder.InsertData(
                 table: "Customers",
                 columns: new[] { "CustomerId", "Address", "CustomerName", "MembershipRank", "PhoneNumber", "RewardPoints" },
                 values: new object[,]
                 {
                    { 1, "Quận 1, TP.HCM", "Nguyễn Văn An", "Vàng", "0901234567", 50 },
                    { 2, "Bình Thạnh, TP.HCM", "Trần Thị Bình", "Bạc", "0912345678", 20 },
                    { 3, "Hải Châu, Đà Nẵng", "Khách Vãng Lai", "Chuẩn", "0000000000", 0 },
                    { 4, "Quận 3, TP.HCM", "Nguyễn Văn A", "VIP", "0919700001", 500 },
                    { 5, "Thủ Dầu Một, Bình Dương", "Lê Thị B", "Thành viên", "0907725032", 120 },
                    { 6, "Biên Hòa, Đồng Nai", "Phạm Văn VIP", "VIP", "0933445566", 1500 },
                    { 7, "Cầu Giấy, Hà Nội", "Hoàng Minh Tuấn", "Vàng", "0987654321", 85 },
                    { 8, "Đống Đa, Hà Nội", "Đỗ Mai Phương", "Thành viên", "0978123456", 110 },
                    { 9, "Ninh Kiều, Cần Thơ", "Võ Quốc Cường", "Chuẩn", "0939112233", 15 },
                    { 10, "Thủ Đức, TP.HCM", "Bùi Tuyết Mai", "VIP", "0908889977", 650 },
                    { 11, "Quận 7, TP.HCM", "Đặng Đình Khang", "Bạc", "0918776655", 40 },
                    { 12, "Tân Bình, TP.HCM", "Ngô Bích Thủy", "Thành viên", "0903332211", 180 },
                    { 13, "Thanh Xuân, Hà Nội", "Dương Gia Huy", "VIP", "0988665544", 820 },
                    { 14, "Quận 10, TP.HCM", "Lý Ánh Nguyệt", "Chuẩn", "0938990011", 5 },
                    { 15, "Sơn Trà, Đà Nẵng", "Phan Thanh Tùng", "Vàng", "0905123789", 95 },
                    { 16, "Quận 5, TP.HCM", "Lâm Quốc Bảo", "Thành viên", "0909654321", 210 },
                    { 17, "Bình Tân, TP.HCM", "Vũ Thùy Linh", "Bạc", "0913456789", 35 },
                    { 18, "Hà Đông, Hà Nội", "Trịnh Kim Ngân", "VIP", "0989001122", 1200 },
                    { 19, "Nha Trang, Khánh Hòa", "Trương Bá Thắng", "Chuẩn", "0935112244", 0 },
                    { 20, "Phan Thiết, Bình Thuận", "Tạ Nhật Nam", "Thành viên", "0906223344", 130 },
                    { 21, "Vũng Tàu, BR-VT", "Hồ Ngọc Hà", "Vàng", "0914556677", 75 },
                    { 22, "Quận 12, TP.HCM", "Lê Văn Đạt", "Chuẩn", "0937889900", 25 },
                    { 23, "Hoàn Kiếm, Hà Nội", "Mai Phương Thúy", "VIP", "0982334455", 940 },
                    { 24, "Long Xuyên, An Giang", "Nguyễn Tấn Phát", "Thành viên", "0907119988", 160 },
                    { 25, "Huế, Thừa Thiên Huế", "Trần Như Quỳnh", "Bạc", "0916223388", 45 },
                    { 26, "Rạch Giá, Kiên Giang", "Đinh Trọng Hưng", "Chuẩn", "0934556611", 10 },
                    { 27, "Buôn Ma Thuột, Đắk Lắk", "Cao Thái Sơn", "Vàng", "0902998877", 90 },
                    { 28, "Quận 4, TP.HCM", "Văn Thị Mỹ", "Thành viên", "0917334411", 140 },
                    { 29, "Quận 8, TP.HCM", "Tô Văn Hùng", "VIP", "0981223344", 1100 },
                    { 30, "Gò Vấp, TP.HCM", "Chu Thanh Huyền", "Bạc", "0904887766", 30 },
                    { 31, "Bắc Ninh", "Phùng Quý Đôn", "Chuẩn", "0932445566", 0 },
                    { 32, "Hải Phòng", "Lê Bảo Ngọc", "Vàng", "0915667788", 65 },
                    { 33, "Nam Định", "Trần Đình Trọng", "Thành viên", "0984556677", 190 },
                    { 34, "Thái Nguyên", "Nguyễn Kiều Oanh", "VIP", "0909114477", 780 },
                    { 35, "Phú Nhuận, TP.HCM", "Lê Quang Liêm", "Vàng", "0918225588", 80 }
                 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "FullName", "IsActive", "Password", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "Nguyễn Quản Trị", true, "123", "Admin", "admin01" },
                    { 2, "Lê Thu Ngân", true, "123", "Cashier", "cashier01" },
                    { 3, "Trần Thủ Kho", true, "123", "Warehouse", "ware01" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "Barcode", "CategoryId", "Price", "ProductName", "StockQuantity" },
                values: new object[,]
                {
                    { 1, "893456789001", 1, 18000m, "Kem Ốc Quế Cornetto Socola", 120 },
                    { 2, "893456789002", 1, 48000m, "Kem Hộp Vinamilk Vani 450ml", 45 },
                    { 3, "893456789003", 1, 12000m, "Kem Que Merino Đậu Đỏ", 150 },
                    { 4, "893456789004", 1, 22000m, "Kem Que Celano Bánh Cá", 8 },
                    { 5, "893456789005", 2, 10000m, "Nước Ngọt Coca Cola Lon 320ml", 320 },
                    { 6, "893456789006", 2, 10000m, "Nước Ngọt Pepsi Không Calo Lon 320ml", 200 },
                    { 7, "893456789007", 2, 9000m, "Nước Ngọt Sprite Chai 390ml", 110 },
                    { 8, "893456789008", 3, 11000m, "Trà Xanh Không Độ Chai 455ml", 180 },
                    { 9, "893456789009", 3, 11000m, "Trà Ô Long Tea+ Chai 450ml", 160 },
                    { 10, "893456789010", 3, 13000m, "Nước Ép Cam Teppy Chai 327ml", 6 },
                    { 11, "893456789011", 4, 15000m, "Nước Tăng Lực Red Bull Lon 250ml", 140 },
                    { 12, "893456789012", 4, 11000m, "Nước Tăng Lực Sting Dâu Chai 330ml", 210 },
                    { 13, "893456789013", 4, 6000m, "Nước Khoáng Thiên Nhiên Lavie 500ml", 300 },
                    { 14, "893456789014", 5, 12500m, "Snack Khoai Tây O'Star 60g", 90 },
                    { 15, "893456789015", 5, 54000m, "Bánh Chocopie Orion Hộp 12 Cái", 50 },
                    { 16, "893500110029", 1, 10000m, "Kem Que Merino Đậu Xanh Nước Cốt Dừa 60g", 180 },
                    { 17, "893456789017", 2, 10000m, "Nước Ngọt 7Up Lon 320ml", 140 },
                    { 18, "893456789018", 2, 11000m, "Nước Ngọt Mirinda Cam Lon 320ml", 160 },
                    { 19, "893456789019", 3, 12000m, "Trà Đào C2 Chai 455ml", 190 },
                    { 20, "893456789020", 4, 10000m, "Nước Tăng Lực Wake Up 247 Chai 330ml", 250 },
                    { 21, "893456789021", 5, 6000m, "Snack Mực Bento Thái Lan Gói 20g", 110 },
                    { 22, "893456789022", 5, 15000m, "Kẹo Dẻo Chupa Chups Hương Trái Cây", 85 },
                    { 23, "893456789023", 1, 25000m, "Kem Celano Sôcôla Hạnh Nhân 70g", 60 },
                    { 24, "893456789024", 2, 12000m, "Sữa Chua Uống Vinamilk Probi Chai 130ml", 130 },
                    { 25, "893456789025", 5, 35000m, "Bánh Quy Bơ Danisa Hộp Nhỏ", 40 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId",
                table: "OrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
