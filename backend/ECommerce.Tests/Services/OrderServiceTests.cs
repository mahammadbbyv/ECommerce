using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Classes;
using ECommerce.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderService _orderService;
    private readonly User _testUser;
    private readonly Category _testCategory;
    private readonly Product _testProduct;

    public OrderServiceTests()
    {
        _context = TestDbContext.CreateInMemoryContext();
        _orderService = new OrderService(_context, MockLogger.Create<OrderService>());

        // Setup test data
        _testUser = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PasswordHash = "hash",
            Role = "Customer"
        };
        _context.Users.Add(_testUser);

        _testCategory = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(_testCategory);

        _testProduct = new Product
        {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 999.99m,
            StockQuantity = 10,
            CategoryId = _testCategory.Id
        };
        _context.Products.Add(_testProduct);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ValidCart_ShouldCreateOrder()
    {
        // Arrange
        var cart = new Cart { UserId = _testUser.Id };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = _testProduct.Id,
            Quantity = 2,
            Price = _testProduct.Price
        };
        _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync();

        var createOrderDto = new CreateOrderDto
        {
            ShippingAddress = "123 Main St, City, Country"
        };

        // Act
        var result = await _orderService.CreateOrderFromCartAsync(_testUser.Id, createOrderDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.OrderNumber);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Pending", result.PaymentStatus);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(_testProduct.Price * 2, result.TotalAmount);

        // Verify cart was cleared
        var cartInDb = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == _testUser.Id);
        Assert.Empty(cartInDb!.CartItems);

        // Verify stock was reduced
        var productInDb = await _context.Products.FindAsync(_testProduct.Id);
        Assert.Equal(8, productInDb!.StockQuantity); // 10 - 2
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_EmptyCart_ShouldThrowException()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            ShippingAddress = "123 Main St, City, Country"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.CreateOrderFromCartAsync(_testUser.Id, createOrderDto));
        Assert.Equal("Cart is empty", exception.Message);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_InsufficientStock_ShouldThrowException()
    {
        // Arrange
        var cart = new Cart { UserId = _testUser.Id };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = _testProduct.Id,
            Quantity = 15, // More than available stock
            Price = _testProduct.Price
        };
        _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync();

        var createOrderDto = new CreateOrderDto
        {
            ShippingAddress = "123 Main St, City, Country"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.CreateOrderFromCartAsync(_testUser.Id, createOrderDto));
        Assert.Contains("Insufficient stock", exception.Message);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ShouldReturnUserOrders()
    {
        // Arrange
        var order1 = new Order
        {
            UserId = _testUser.Id,
            OrderNumber = "ORD-001",
            TotalAmount = 100m,
            Status = "Pending",
            PaymentStatus = "Pending",
            ShippingAddress = "Address 1"
        };
        var order2 = new Order
        {
            UserId = _testUser.Id,
            OrderNumber = "ORD-002",
            TotalAmount = 200m,
            Status = "Processing",
            PaymentStatus = "Paid",
            ShippingAddress = "Address 2"
        };
        _context.Orders.AddRange(order1, order2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.GetUserOrdersAsync(_testUser.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.OrderNumber == "ORD-001");
        Assert.Contains(result, o => o.OrderNumber == "ORD-002");
    }

    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ShouldReturnOrder()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            OrderNumber = "ORD-001",
            TotalAmount = 999.99m,
            Status = "Pending",
            PaymentStatus = "Pending",
            ShippingAddress = "123 Main St"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = _testProduct.Id,
            Quantity = 1,
            Price = _testProduct.Price,
            Subtotal = _testProduct.Price
        };
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.GetOrderByIdAsync(_testUser.Id, order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.OrderNumber, result.OrderNumber);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetOrderByIdAsync_NonExistentOrder_ShouldReturnNull()
    {
        // Act
        var result = await _orderService.GetOrderByIdAsync(_testUser.Id, 999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidOrder_ShouldUpdateStatus()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            OrderNumber = "ORD-001",
            TotalAmount = 100m,
            Status = "Pending",
            PaymentStatus = "Paid",
            ShippingAddress = "123 Main St"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateOrderStatusDto
        {
            Status = "Shipped"
        };

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(order.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Shipped", result.Status);

        var orderInDb = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("Shipped", orderInDb!.Status);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_NonExistentOrder_ShouldThrowException()
    {
        // Arrange
        var updateDto = new UpdateOrderStatusDto
        {
            Status = "Shipped"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.UpdateOrderStatusAsync(999, updateDto));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnAllOrders()
    {
        // Arrange
        var user2 = new User
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            PasswordHash = "hash",
            Role = "Customer"
        };
        _context.Users.Add(user2);
        await _context.SaveChangesAsync();

        var orders = new[]
        {
            new Order { UserId = _testUser.Id, OrderNumber = "ORD-001", TotalAmount = 100m, Status = "Pending", PaymentStatus = "Pending", ShippingAddress = "Addr 1" },
            new Order { UserId = user2.Id, OrderNumber = "ORD-002", TotalAmount = 200m, Status = "Processing", PaymentStatus = "Paid", ShippingAddress = "Addr 2" }
        };
        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.GetAllOrdersAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
