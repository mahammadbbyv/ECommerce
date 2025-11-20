using ECommerce.API.Data;
using ECommerce.API.Models;
using ECommerce.API.Services.Classes;
using ECommerce.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class AdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminService _adminService;

    public AdminServiceTests()
    {
        _context = TestDbContext.CreateInMemoryContext();
        _adminService = new AdminService(_context, MockLogger.Create<AdminService>());
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnCorrectTotalRevenue()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(350m, result.TotalRevenue); // Only paid orders: 200 + 150
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnCorrectTotalOrders()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(3, result.TotalOrders);
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldGroupOrdersByStatus()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(3, result.OrdersByStatus.Count);
        Assert.Equal(1, result.OrdersByStatus["Pending"]);
        Assert.Equal(1, result.OrdersByStatus["Processing"]);
        Assert.Equal(1, result.OrdersByStatus["Shipped"]);
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnTopProducts()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.NotEmpty(result.TopProducts);
        Assert.True(result.TopProducts.Count <= 10); // Should return max 10 products
        
        var topProduct = result.TopProducts.First();
        Assert.True(topProduct.TotalSold > 0);
        Assert.True(topProduct.TotalRevenue > 0);
    }

    [Fact]
    public async Task GetAnalyticsAsync_TopProducts_ShouldBeOrderedByTotalSold()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PasswordHash = "hash",
            Role = "Customer"
        };
        _context.Users.Add(user);

        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var products = new[]
        {
            new Product { Name = "Product A", Price = 10m, StockQuantity = 100, CategoryId = category.Id },
            new Product { Name = "Product B", Price = 20m, StockQuantity = 100, CategoryId = category.Id },
            new Product { Name = "Product C", Price = 30m, StockQuantity = 100, CategoryId = category.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            OrderNumber = "ORD-001",
            TotalAmount = 100m,
            Status = "Delivered",
            PaymentStatus = "Paid",
            ShippingAddress = "123 Main St"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderItems = new[]
        {
            new OrderItem { OrderId = order.Id, ProductId = products[0].Id, Quantity = 5, Price = 10m, Subtotal = 50m },
            new OrderItem { OrderId = order.Id, ProductId = products[1].Id, Quantity = 10, Price = 20m, Subtotal = 200m },
            new OrderItem { OrderId = order.Id, ProductId = products[2].Id, Quantity = 3, Price = 30m, Subtotal = 90m }
        };
        _context.OrderItems.AddRange(orderItems);
        await _context.SaveChangesAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(3, result.TopProducts.Count);
        Assert.Equal("Product B", result.TopProducts[0].ProductName); // Highest quantity (10)
        Assert.Equal("Product A", result.TopProducts[1].ProductName); // Second (5)
        Assert.Equal("Product C", result.TopProducts[2].ProductName); // Third (3)
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnCorrectTotalCustomers()
    {
        // Arrange
        var users = new[]
        {
            new User { FirstName = "John", LastName = "Doe", Email = "john@example.com", PasswordHash = "hash", Role = "Customer" },
            new User { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", PasswordHash = "hash", Role = "Customer" },
            new User { FirstName = "Admin", LastName = "User", Email = "admin@example.com", PasswordHash = "hash", Role = "Admin" }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(2, result.TotalCustomers); // Only customers, not admin
    }

    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnCorrectTotalProducts()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var products = new[]
        {
            new Product { Name = "Product 1", Price = 10m, StockQuantity = 10, CategoryId = category.Id },
            new Product { Name = "Product 2", Price = 20m, StockQuantity = 20, CategoryId = category.Id },
            new Product { Name = "Product 3", Price = 30m, StockQuantity = 30, CategoryId = category.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(3, result.TotalProducts);
    }

    [Fact]
    public async Task GetAnalyticsAsync_EmptyDatabase_ShouldReturnZeroValues()
    {
        // Act
        var result = await _adminService.GetAnalyticsAsync();

        // Assert
        Assert.Equal(0, result.TotalRevenue);
        Assert.Equal(0, result.TotalOrders);
        Assert.Empty(result.OrdersByStatus);
        Assert.Empty(result.TopProducts);
        Assert.Equal(0, result.TotalCustomers);
        Assert.Equal(0, result.TotalProducts);
    }

    private async Task SeedTestDataAsync()
    {
        // Users
        var users = new[]
        {
            new User { FirstName = "John", LastName = "Doe", Email = "john@example.com", PasswordHash = "hash", Role = "Customer" },
            new User { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", PasswordHash = "hash", Role = "Customer" },
            new User { FirstName = "Admin", LastName = "User", Email = "admin@example.com", PasswordHash = "hash", Role = "Admin" }
        };
        _context.Users.AddRange(users);

        // Category and Products
        var category = new Category { Name = "Electronics", Description = "Electronic devices" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var products = new[]
        {
            new Product { Name = "Laptop", Price = 999.99m, StockQuantity = 10, CategoryId = category.Id },
            new Product { Name = "Mouse", Price = 29.99m, StockQuantity = 50, CategoryId = category.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Orders
        var orders = new[]
        {
            new Order
            {
                UserId = users[0].Id,
                OrderNumber = "ORD-001",
                TotalAmount = 100m,
                Status = "Pending",
                PaymentStatus = "Pending",
                ShippingAddress = "123 Main St"
            },
            new Order
            {
                UserId = users[0].Id,
                OrderNumber = "ORD-002",
                TotalAmount = 200m,
                Status = "Processing",
                PaymentStatus = "Paid",
                ShippingAddress = "456 Oak Ave"
            },
            new Order
            {
                UserId = users[1].Id,
                OrderNumber = "ORD-003",
                TotalAmount = 150m,
                Status = "Shipped",
                PaymentStatus = "Paid",
                ShippingAddress = "789 Pine Rd"
            }
        };
        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Order Items
        var orderItems = new[]
        {
            new OrderItem { OrderId = orders[0].Id, ProductId = products[0].Id, Quantity = 1, Price = 999.99m, Subtotal = 999.99m },
            new OrderItem { OrderId = orders[1].Id, ProductId = products[1].Id, Quantity = 2, Price = 29.99m, Subtotal = 59.98m },
            new OrderItem { OrderId = orders[2].Id, ProductId = products[0].Id, Quantity = 1, Price = 999.99m, Subtotal = 999.99m }
        };
        _context.OrderItems.AddRange(orderItems);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
