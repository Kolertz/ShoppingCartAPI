using System.Net;

namespace ShoppingList.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Login { get; set; }
        public required string Password { get; set; }
        public List<UserItem> UserItems { get; set; } = [];
        public decimal Balance { get; set; }
    }
}
