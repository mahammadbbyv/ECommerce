using ECommerce.API.DTOs;

namespace ECommerce.API.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentDto> CreatePaymentIntentAsync(int userId, CreatePaymentIntentDto createPaymentIntentDto);
    Task<bool> HandleWebhookAsync(string payload, string signature);
}
