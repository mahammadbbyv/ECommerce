using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.DTOs;

public class CreateOrderDto
{
    [Required(ErrorMessage = "Shipping address is required")]
    [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
    public string ShippingAddress { get; set; } = string.Empty;
}
