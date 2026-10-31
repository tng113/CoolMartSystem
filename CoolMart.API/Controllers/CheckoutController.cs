using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CoolMart.API.Controllers
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CheckoutRequestDto
    {
        public string CashierUsername { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int PointsToRedeem { get; set; } = 0; // Tiêu chí 48: Đổi điểm thưởng
        public decimal VoucherDiscount { get; set; } = 0; // Voucher giảm giá (0, 10.000 hoặc 20.000)
        public List<CartItemDto> Items { get; set; } = new();
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Cashier")]
    public class CheckoutController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CheckoutController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult ProcessCheckout([FromBody] CheckoutRequestDto request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Giỏ hàng rỗng, không thể thanh toán!" });

            if (string.IsNullOrWhiteSpace(request.CashierUsername))
                return BadRequest(new { message = "Thiếu thông tin thu ngân!" });

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    decimal subTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
                    int totalItems = request.Items.Sum(i => i.Quantity);

                    // Xử lý đổi điểm thưởng giảm tiền (1 điểm = 1.000đ)
                    decimal discount = 0;
                    if (request.CustomerId > 0 && request.PointsToRedeem > 0)
                    {
                        string checkPointsSql = "SELECT RewardPoints FROM Customers WHERE CustomerId = @CustomerId";
                        int currentPoints = 0;
                        using (SqlCommand cmdCheckPts = new SqlCommand(checkPointsSql, connection, transaction))
                        {
                            cmdCheckPts.Parameters.AddWithValue("@CustomerId", request.CustomerId);
                            object? ptsObj = cmdCheckPts.ExecuteScalar();
                            currentPoints = ptsObj != null ? Convert.ToInt32(ptsObj) : 0;
                        }

                        if (currentPoints < request.PointsToRedeem)
                            throw new InvalidOperationException($"Khách hàng chỉ có {currentPoints} điểm, không đủ để đổi {request.PointsToRedeem} điểm!");

                        discount = request.PointsToRedeem * 1000m;
                    }

                    // Voucher: chỉ chấp nhận các mức hợp lệ, tránh client gửi số tùy ý
                    decimal voucher = (request.VoucherDiscount == 10000m || request.VoucherDiscount == 20000m)
                        ? request.VoucherDiscount : 0m;

                    // VAT 8% tính trên tiền hàng (khớp với màn hình POS)
                    decimal vat = Math.Round(subTotal * 0.08m, 0);

                    decimal finalAmount = Math.Max(0, subTotal - discount - voucher + vat);
                    int rewardPointsEarned = (int)(finalAmount / 10000); // 10.000đ = 1 điểm thưởng mới
                    string orderCode = $"HD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

                    // 1. Tạo hóa đơn trong Orders
                    string insertOrderSql = @"
                        INSERT INTO Orders (OrderCode, CreatedDate, CashierUsername, CustomerId, TotalItems, FinalAmount, RewardPoints)
                        OUTPUT INSERTED.OrderId
                        VALUES (@OrderCode, GETDATE(), @CashierUsername, @CustomerId, @TotalItems, @FinalAmount, @RewardPoints)";

                    int orderId = 0;
                    using (SqlCommand cmdOrder = new SqlCommand(insertOrderSql, connection, transaction))
                    {
                        cmdOrder.Parameters.AddWithValue("@OrderCode", orderCode);
                        cmdOrder.Parameters.AddWithValue("@CashierUsername", request.CashierUsername.Trim());

                        // Không chọn khách -> dùng khách vãng lai (ID 3 trong seed) vì cột CustomerId không cho NULL
                        int effectiveCustomerId = request.CustomerId > 0 ? request.CustomerId : 3;
                        cmdOrder.Parameters.AddWithValue("@CustomerId", effectiveCustomerId);

                        cmdOrder.Parameters.AddWithValue("@TotalItems", totalItems);
                        cmdOrder.Parameters.AddWithValue("@FinalAmount", finalAmount);
                        cmdOrder.Parameters.AddWithValue("@RewardPoints", rewardPointsEarned);
                        orderId = (int)cmdOrder.ExecuteScalar();
                    }

                    // 2 & 3. Kiểm tra tồn kho, trừ tồn kho và ghi chi tiết hóa đơn
                    foreach (var item in request.Items)
                    {
                        string checkStockSql = "SELECT StockQuantity, ProductName FROM Products WHERE ProductId = @ProductId";
                        int currentStock = 0;
                        string productName = string.Empty;

                        using (SqlCommand cmdCheck = new SqlCommand(checkStockSql, connection, transaction))
                        {
                            cmdCheck.Parameters.AddWithValue("@ProductId", item.ProductId);
                            using (SqlDataReader reader = cmdCheck.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new InvalidOperationException($"Không tìm thấy sản phẩm có ID: {item.ProductId}");

                                currentStock = Convert.ToInt32(reader["StockQuantity"]);
                                productName = reader["ProductName"].ToString() ?? string.Empty;
                            }
                        }

                        if (currentStock < item.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"Sản phẩm '{productName}' (ID: {item.ProductId}) không đủ hàng! " +
                                $"(Tồn kho còn: {currentStock}, yêu cầu mua: {item.Quantity})");
                        }

                        string insertDetailSql = @"
                            INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice, LineTotal)
                            VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @LineTotal)";
                        using (SqlCommand cmdDetail = new SqlCommand(insertDetailSql, connection, transaction))
                        {
                            cmdDetail.Parameters.AddWithValue("@OrderId", orderId);
                            cmdDetail.Parameters.AddWithValue("@ProductId", item.ProductId);
                            cmdDetail.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmdDetail.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                            cmdDetail.Parameters.AddWithValue("@LineTotal", item.Quantity * item.UnitPrice);
                            cmdDetail.ExecuteNonQuery();
                        }

                        string updateStockSql = @"
                            UPDATE Products 
                            SET StockQuantity = StockQuantity - @Qty 
                            WHERE ProductId = @ProductId";
                        using (SqlCommand cmdStock = new SqlCommand(updateStockSql, connection, transaction))
                        {
                            cmdStock.Parameters.AddWithValue("@Qty", item.Quantity);
                            cmdStock.Parameters.AddWithValue("@ProductId", item.ProductId);
                            cmdStock.ExecuteNonQuery();
                        }
                    }

                    // 4. Cập nhật điểm thưởng khách hàng
                    if (request.CustomerId > 0)
                    {
                        int netPointsChange = rewardPointsEarned - request.PointsToRedeem;
                        if (netPointsChange != 0)
                        {
                            string updatePtsSql = @"
                                UPDATE Customers 
                                SET RewardPoints = RewardPoints + @PointsChange 
                                WHERE CustomerId = @CustomerId";
                            using (SqlCommand cmdPts = new SqlCommand(updatePtsSql, connection, transaction))
                            {
                                cmdPts.Parameters.AddWithValue("@PointsChange", netPointsChange);
                                cmdPts.Parameters.AddWithValue("@CustomerId", request.CustomerId);
                                cmdPts.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        message = "Thanh toán hóa đơn POS thành công!",
                        orderId = orderId,
                        orderCode = orderCode,
                        totalItems = totalItems,
                        subTotal = subTotal,
                        discount = discount,
                        finalAmount = finalAmount,
                        rewardPointsAdded = rewardPointsEarned
                    });
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }

                    return BadRequest(new
                    {
                        success = false,
                        message = "Giao dịch bị hủy và đã được Rollback an toàn!",
                        errorDetail = ex.Message
                    });
                }
            }
        }
    }
}