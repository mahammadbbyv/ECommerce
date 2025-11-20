using ECommerce.API.DTOs;

namespace ECommerce.API.Services.Interfaces;

public interface IAdminService
{
    Task<AnalyticsDto> GetAnalyticsAsync();
}
