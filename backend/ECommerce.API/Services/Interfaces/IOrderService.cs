using ECommerce.API.DTOs;

namespace ECommerce.API.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(int userId, CreateOrderDto createOrderDto);
    Task<List<OrderDto>> GetUserOrdersAsync(int userId);
    Task<OrderDto?> GetOrderByIdAsync(int userId, int orderId);
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateOrderStatusDto);
    Task<List<OrderDto>> GetAllOrdersAsync();
}
