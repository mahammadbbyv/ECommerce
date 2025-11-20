using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services.Classes;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CartService> _logger;

    public CartService(ApplicationDbContext context, ILogger<CartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartDto> GetUserCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            // Create a new cart if one doesn't exist
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return MapToCartDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(int userId, AddToCartDto addToCartDto)
    {
        // Get or create cart
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
        }

        // Get product
        var product = await _context.Products.FindAsync(addToCartDto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product not found");
        }

        // Check stock availability
        if (product.StockQuantity < addToCartDto.Quantity)
        {
            throw new InvalidOperationException($"Only {product.StockQuantity} items available in stock");
        }

        // Check if product already in cart
        var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addToCartDto.ProductId);

        if (existingCartItem != null)
        {
            // Update quantity
            var newQuantity = existingCartItem.Quantity + addToCartDto.Quantity;
            
            if (product.StockQuantity < newQuantity)
            {
                throw new InvalidOperationException($"Only {product.StockQuantity} items available in stock");
            }

            existingCartItem.Quantity = newQuantity;
            existingCartItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Add new cart item
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = addToCartDto.ProductId,
                Quantity = addToCartDto.Quantity,
                Price = product.Price,
                CreatedAt = DateTime.UtcNow
            };
            cart.CartItems.Add(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} added to cart for user {UserId}", addToCartDto.ProductId, userId);

        // Reload cart with all relationships
        await _context.Entry(cart).Collection(c => c.CartItems).LoadAsync();
        foreach (var item in cart.CartItems)
        {
            await _context.Entry(item).Reference(ci => ci.Product).LoadAsync();
        }

        return MapToCartDto(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemDto updateCartItemDto)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
        if (cartItem == null)
        {
            throw new InvalidOperationException("Cart item not found");
        }

        // Check stock availability
        if (cartItem.Product.StockQuantity < updateCartItemDto.Quantity)
        {
            throw new InvalidOperationException($"Only {cartItem.Product.StockQuantity} items available in stock");
        }

        cartItem.Quantity = updateCartItemDto.Quantity;
        cartItem.UpdatedAt = DateTime.UtcNow;
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cart item {CartItemId} updated for user {UserId}", cartItemId, userId);

        return MapToCartDto(cart);
    }

    public async Task<bool> RemoveFromCartAsync(int userId, int cartItemId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return false;
        }

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
        if (cartItem == null)
        {
            return false;
        }

        _context.CartItems.Remove(cartItem);
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cart item {CartItemId} removed for user {UserId}", cartItemId, userId);

        return true;
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return false;
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cart cleared for user {UserId}", userId);

        return true;
    }

    private static CartDto MapToCartDto(Cart cart)
    {
        var items = cart.CartItems.Select(ci => new CartItemDto
        {
            Id = ci.Id,
            ProductId = ci.ProductId,
            ProductName = ci.Product.Name,
            ProductImageUrl = ci.Product.ImageUrl,
            Quantity = ci.Quantity,
            Price = ci.Price,
            Subtotal = ci.Price * ci.Quantity,
            StockQuantity = ci.Product.StockQuantity
        }).ToList();

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items,
            TotalAmount = items.Sum(i => i.Subtotal),
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt
        };
    }
}
