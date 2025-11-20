using ECommerce.API.DTOs;

namespace ECommerce.API.Services.Interfaces;

public interface ICartService
{
    Task<CartDto> GetUserCartAsync(int userId);
    Task<CartDto> AddToCartAsync(int userId, AddToCartDto addToCartDto);
    Task<CartDto> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemDto updateCartItemDto);
    Task<bool> RemoveFromCartAsync(int userId, int cartItemId);
    Task<bool> ClearCartAsync(int userId);
}
