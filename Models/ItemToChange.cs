namespace ShoppingList.Models
{
    public class ItemToChange
    {
        public required string Name { get; set; }
        public int OriginalQuantity { get; set; }
        public int StorageQuantity { get; set; }
        public decimal Price { get; set; }
    }
}
