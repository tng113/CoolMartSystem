using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CoolMart.API.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Entity Framework Core với SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CoolMartDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cấu hình JWT Bearer Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Cấu hình Swagger kèm nút nhập Token (Authorize)
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CoolMart API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// TC 11: Bật CORS cho ứng dụng WinForms / client khác gọi vào API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWinForms", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowWinForms");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Tạo (hoặc cập nhật) Stored Procedure báo cáo doanh thu theo ngày
try
{
    using var spScope = app.Services.CreateScope();
    var spDb = spScope.ServiceProvider.GetRequiredService<CoolMartDbContext>();
    spDb.Database.ExecuteSqlRaw(@"
CREATE OR ALTER PROCEDURE sp_RevenueByDay
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CreatedDate AS DATE) AS OrderDate,
           COUNT(*) AS TotalOrders,
           SUM(FinalAmount) AS Revenue
    FROM Orders
    WHERE CAST(CreatedDate AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CAST(CreatedDate AS DATE)
    ORDER BY OrderDate;
END");

    // TC 34: Stored Procedure tính doanh thu theo ca (thu ngân) trong từng ngày
    spDb.Database.ExecuteSqlRaw(@"
CREATE OR ALTER PROCEDURE sp_RevenueByShift
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CashierUsername,
           CAST(CreatedDate AS DATE) AS OrderDate,
           COUNT(*) AS TotalOrders,
           SUM(FinalAmount) AS Revenue
    FROM Orders
    WHERE CAST(CreatedDate AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CashierUsername, CAST(CreatedDate AS DATE)
    ORDER BY OrderDate, CashierUsername;
END");
}
catch (Exception ex)
{
    Console.WriteLine("Không tạo được Stored Procedure báo cáo (chạy Update-Database trước): " + ex.Message);
}

app.Run();