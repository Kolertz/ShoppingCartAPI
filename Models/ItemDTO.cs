namespace ShoppingList.Models
{
    public class ItemDTO
    {
        public required string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
