namespace Tutorial9.Model;

public class Product
{
    public int IdProduct { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
}