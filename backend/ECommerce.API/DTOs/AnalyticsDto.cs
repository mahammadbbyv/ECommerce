namespace ECommerce.API.DTOs;

public class AnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
}
