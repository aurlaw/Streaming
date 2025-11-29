using CustomerDemo.Attributes;

namespace CustomerDemo.Models;

[GenerateSerializer("L")]
public class OrderLine
{
    [ReportItem(1, 100, PaddingDirection = PaddingDirection.Right)]
    public string ProductName { get; set; } = string.Empty;
    
    [ReportItem(2, 5, PaddingChar = '0', PaddingDirection = PaddingDirection.Left)]
    public int Quantity { get; set; }
    
    [ReportItem(3, 5, PaddingChar = '0', PaddingDirection = PaddingDirection.Left)]
    public decimal Price { get; set; }
    
}