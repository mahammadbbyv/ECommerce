using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.DTOs;

public class UpdateCartItemDto
{
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
