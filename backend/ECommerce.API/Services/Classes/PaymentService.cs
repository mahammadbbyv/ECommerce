using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ECommerce.API.Services.Classes;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ApplicationDbContext context, IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        // Set Stripe API key
        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
    }

    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(int userId, CreatePaymentIntentDto createPaymentIntentDto)
    {
        _logger.LogInformation("Creating payment intent for user {UserId}, order {OrderId}", userId, createPaymentIntentDto.OrderId);

        // Get order
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == createPaymentIntentDto.OrderId && o.UserId == userId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        if (order.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException("Order has already been paid");
        }

        try
        {
            // Convert amount to cents (Stripe expects the amount in the smallest currency unit)
            var amountInCents = (long)(order.TotalAmount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", order.Id.ToString() },
                    { "orderNumber", order.OrderNumber }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            // Update order with payment intent ID
            order.PaymentIntentId = paymentIntent.Id;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment intent {PaymentIntentId} created for order {OrderNumber}", paymentIntent.Id, order.OrderNumber);

            return new PaymentIntentDto
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = order.TotalAmount
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent for order {OrderId}", createPaymentIntentDto.OrderId);
            throw new InvalidOperationException($"Payment processing error: {ex.Message}");
        }
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature)
    {
        _logger.LogInformation("Handling Stripe webhook");

        var webhookSecret = _configuration["StripeSettings:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                webhookSecret
            );

            _logger.LogInformation("Webhook event type: {EventType}", stripeEvent.Type);

            // Handle the event
            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandlePaymentIntentSucceededAsync(paymentIntent);
                }
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandlePaymentIntentFailedAsync(paymentIntent);
                }
            }
            else
            {
                _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
            }

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return false;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent)
    {
        _logger.LogInformation("Payment succeeded for payment intent {PaymentIntentId}", paymentIntent.Id);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);

        if (order != null)
        {
            order.PaymentStatus = "Paid";
            order.Status = "Processing";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNumber} marked as paid", order.OrderNumber);
        }
        else
        {
            _logger.LogWarning("Order not found for payment intent {PaymentIntentId}", paymentIntent.Id);
        }
    }

    private async Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent)
    {
        _logger.LogInformation("Payment failed for payment intent {PaymentIntentId}", paymentIntent.Id);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);

        if (order != null)
        {
            order.PaymentStatus = "Failed";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNumber} marked as payment failed", order.OrderNumber);
        }
        else
        {
            _logger.LogWarning("Order not found for payment intent {PaymentIntentId}", paymentIntent.Id);
        }
    }
}
