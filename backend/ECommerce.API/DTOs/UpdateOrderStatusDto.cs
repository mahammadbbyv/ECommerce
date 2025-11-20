using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.DTOs;

public class UpdateOrderStatusDto
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(Pending|Processing|Shipped|Delivered|Cancelled)$", 
        ErrorMessage = "Status must be one of: Pending, Processing, Shipped, Delivered, Cancelled")]
    public string Status { get; set; } = string.Empty;
}
