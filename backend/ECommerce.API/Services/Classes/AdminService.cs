using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services.Classes;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(ApplicationDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync()
    {
        _logger.LogInformation("Retrieving analytics data");

        // Calculate total revenue from paid orders
        var totalRevenue = await _context.Orders
            .Where(o => o.PaymentStatus == "Paid")
            .SumAsync(o => o.TotalAmount);

        // Count total orders
        var totalOrders = await _context.Orders.CountAsync();

        // Count orders by status
        var ordersByStatus = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Get top 10 products by sales
        var topProducts = await _context.OrderItems
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalSold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Subtotal)
            })
            .OrderByDescending(p => p.TotalSold)
            .Take(10)
            .ToListAsync();

        // Count total customers (users with role Customer)
        var totalCustomers = await _context.Users
            .Where(u => u.Role == "Customer")
            .CountAsync();

        // Count total products
        var totalProducts = await _context.Products.CountAsync();

        _logger.LogInformation("Analytics data retrieved successfully");

        return new AnalyticsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            OrdersByStatus = ordersByStatus,
            TopProducts = topProducts,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts
        };
    }
}
