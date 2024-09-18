namespace ShoppingList.Entities
{
    public enum ItemStatus
    {
        InCart,
        Purchased,
        Reserved
    }
    public class UserItem
    {
        public int UserId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public ItemStatus Status { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
        public Item? Item { get; set; }
    }
}
