using Microsoft.EntityFrameworkCore;
using SanBong.Data;
using SanBong.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Giữ nguyên tên property, không convert sang camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register HttpClient
builder.Services.AddHttpClient();

// Register Payment Services
var momoConfig = new MoMoConfig();
builder.Configuration.GetSection("Payment:MoMo").Bind(momoConfig);
builder.Services.AddSingleton(momoConfig);

var zaloPayConfig = new ZaloPayConfig();
builder.Configuration.GetSection("Payment:ZaloPay").Bind(zaloPayConfig);
builder.Services.AddSingleton(zaloPayConfig);

builder.Services.AddScoped<MoMoPaymentService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<MoMoPaymentService>>();
    return new MoMoPaymentService(momoConfig, httpClient, logger);
});

builder.Services.AddScoped<ZaloPayPaymentService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<ZaloPayPaymentService>>();
    return new ZaloPayPaymentService(zaloPayConfig, httpClient, logger);
});

builder.Services.AddScoped<PaymentServiceFactory>();
builder.Services.AddScoped<PaymentManager>();

// Register Sandbox Payment Service (for demo)
builder.Services.AddScoped<SandboxPaymentService>();

// Register Holiday Discount Service (giảm giá ngày lễ 40%)
builder.Services.AddScoped<HolidayDiscountService>();
// Register Bank Transfer Service
builder.Services.AddScoped<BankTransferService>();

// Register Booking Lock Service
builder.Services.AddScoped<BookingLockService>();

var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
