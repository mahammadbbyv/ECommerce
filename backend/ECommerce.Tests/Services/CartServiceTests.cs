using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Classes;
using ECommerce.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class CartServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CartService _cartService;
    private readonly User _testUser;
    private readonly Category _testCategory;
    private readonly Product _testProduct;

    public CartServiceTests()
    {
        _context = TestDbContext.CreateInMemoryContext();
        _cartService = new CartService(_context, MockLogger.Create<CartService>());

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
    public async Task GetUserCartAsync_NewUser_ShouldCreateAndReturnEmptyCart()
    {
        // Act
        var result = await _cartService.GetUserCartAsync(_testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalAmount);

        var cartInDb = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == _testUser.Id);
        Assert.NotNull(cartInDb);
    }

    [Fact]
    public async Task GetUserCartAsync_ExistingCart_ShouldReturnCart()
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

        // Act
        var result = await _cartService.GetUserCartAsync(_testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(_testProduct.Price * 2, result.TotalAmount);
    }

    [Fact]
    public async Task AddToCartAsync_NewItem_ShouldAddItemToCart()
    {
        // Arrange
        var addToCartDto = new AddToCartDto
        {
            ProductId = _testProduct.Id,
            Quantity = 3
        };

        // Act
        var result = await _cartService.AddToCartAsync(_testUser.Id, addToCartDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.Items[0].Quantity);
        Assert.Equal(_testProduct.Price * 3, result.TotalAmount);
    }

    [Fact]
    public async Task AddToCartAsync_ExistingItem_ShouldIncrementQuantity()
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

        var addToCartDto = new AddToCartDto
        {
            ProductId = _testProduct.Id,
            Quantity = 3
        };

        // Act
        var result = await _cartService.AddToCartAsync(_testUser.Id, addToCartDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity); // 2 + 3
    }

    [Fact]
    public async Task AddToCartAsync_InsufficientStock_ShouldThrowException()
    {
        // Arrange
        var addToCartDto = new AddToCartDto
        {
            ProductId = _testProduct.Id,
            Quantity = 15 // More than available stock (10)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddToCartAsync(_testUser.Id, addToCartDto));
        Assert.Contains("items available in stock", exception.Message);
    }

    [Fact]
    public async Task AddToCartAsync_NonExistentProduct_ShouldThrowException()
    {
        // Arrange
        var addToCartDto = new AddToCartDto
        {
            ProductId = 999,
            Quantity = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddToCartAsync(_testUser.Id, addToCartDto));
        Assert.Equal("Product not found", exception.Message);
    }

    [Fact]
    public async Task UpdateCartItemAsync_ValidQuantity_ShouldUpdateQuantity()
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

        var updateDto = new UpdateCartItemDto { Quantity = 5 };

        // Act
        var result = await _cartService.UpdateCartItemAsync(_testUser.Id, cartItem.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity);
    }

    [Fact]
    public async Task UpdateCartItemAsync_InsufficientStock_ShouldThrowException()
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

        var updateDto = new UpdateCartItemDto { Quantity = 15 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.UpdateCartItemAsync(_testUser.Id, cartItem.Id, updateDto));
        Assert.Contains("items available in stock", exception.Message);
    }

    [Fact]
    public async Task RemoveFromCartAsync_ExistingItem_ShouldRemoveAndReturnTrue()
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

        // Act
        var result = await _cartService.RemoveFromCartAsync(_testUser.Id, cartItem.Id);

        // Assert
        Assert.True(result);
        var itemInDb = await _context.CartItems.FindAsync(cartItem.Id);
        Assert.Null(itemInDb);
    }

    [Fact]
    public async Task RemoveFromCartAsync_NonExistentItem_ShouldReturnFalse()
    {
        // Act
        var result = await _cartService.RemoveFromCartAsync(_testUser.Id, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = new Cart { UserId = _testUser.Id };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var product2 = new Product
        {
            Name = "Mouse",
            Price = 29.99m,
            StockQuantity = 50,
            CategoryId = _testCategory.Id
        };
        _context.Products.Add(product2);
        await _context.SaveChangesAsync();

        var cartItems = new[]
        {
            new CartItem { CartId = cart.Id, ProductId = _testProduct.Id, Quantity = 2, Price = _testProduct.Price },
            new CartItem { CartId = cart.Id, ProductId = product2.Id, Quantity = 1, Price = product2.Price }
        };
        _context.CartItems.AddRange(cartItems);
        await _context.SaveChangesAsync();

        // Act
        await _cartService.ClearCartAsync(_testUser.Id);

        // Assert
        var cartInDb = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == _testUser.Id);
        Assert.NotNull(cartInDb);
        Assert.Empty(cartInDb.CartItems);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
