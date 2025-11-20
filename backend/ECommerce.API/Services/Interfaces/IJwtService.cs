using ECommerce.API.Models;

namespace ECommerce.API.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    int? ValidateToken(string token);
}
