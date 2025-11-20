using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.DTOs;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
