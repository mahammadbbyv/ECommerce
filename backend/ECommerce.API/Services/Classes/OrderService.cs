using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services.Classes;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(int userId, CreateOrderDto createOrderDto)
    {
        _logger.LogInformation("Creating order for user {UserId}", userId);

        // Get user's cart with items
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
        {
            throw new InvalidOperationException("Cart is empty");
        }

        // Validate stock availability for all items
        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product.StockQuantity < cartItem.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product '{cartItem.Product.Name}'. Available: {cartItem.Product.StockQuantity}, Requested: {cartItem.Quantity}");
            }
        }

        // Generate unique order number
        var orderNumber = await GenerateOrderNumberAsync();

        // Calculate total amount
        var totalAmount = cart.CartItems.Sum(ci => ci.Price * ci.Quantity);

        // Create order
        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            TotalAmount = totalAmount,
            Status = "Pending",
            PaymentStatus = "Pending",
            ShippingAddress = createOrderDto.ShippingAddress,
            CreatedAt = DateTime.UtcNow
        };

        // Create order items from cart items
        foreach (var cartItem in cart.CartItems)
        {
            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Price = cartItem.Price,
                Subtotal = cartItem.Price * cartItem.Quantity
            };

            order.OrderItems.Add(orderItem);

            // Update product stock
            cartItem.Product.StockQuantity -= cartItem.Quantity;
        }

        _context.Orders.Add(order);

        // Clear cart items
        _context.CartItems.RemoveRange(cart.CartItems);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} created successfully for user {UserId}", orderNumber, userId);

        return await GetOrderDtoByIdAsync(order.Id);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
    {
        _logger.LogInformation("Retrieving orders for user {UserId}", userId);

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToOrderDto).ToList();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int userId, int orderId)
    {
        _logger.LogInformation("Retrieving order {OrderId} for user {UserId}", orderId, userId);

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        return order != null ? MapToOrderDto(order) : null;
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateOrderStatusDto)
    {
        _logger.LogInformation("Updating status for order {OrderId} to {Status}", orderId, updateOrderStatusDto.Status);

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found");
        }

        order.Status = updateOrderStatusDto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, updateOrderStatusDto.Status);

        return MapToOrderDto(order);
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        _logger.LogInformation("Retrieving all orders");

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToOrderDto).ToList();
    }

    private async Task<OrderDto> GetOrderDtoByIdAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found");
        }

        return MapToOrderDto(order);
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        var orderNumber = $"ORD-{timestamp}-{random}";

        // Ensure uniqueness
        while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            random = new Random().Next(1000, 9999);
            orderNumber = $"ORD-{timestamp}-{random}";
        }

        return orderNumber;
    }

    private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentIntentId = order.PaymentIntentId,
            ShippingAddress = order.ShippingAddress,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                ProductImageUrl = oi.Product.ImageUrl,
                Quantity = oi.Quantity,
                Price = oi.Price,
                Subtotal = oi.Subtotal
            }).ToList()
        };
    }
}
