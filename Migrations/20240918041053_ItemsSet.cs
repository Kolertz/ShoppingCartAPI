using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Migrations
{
    /// <inheritdoc />
    public partial class ItemsSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserItem_Items_ItemId",
                table: "UserItem");

            migrationBuilder.DropForeignKey(
                name: "FK_UserItem_Users_UserId",
                table: "UserItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserItem",
                table: "UserItem");

            migrationBuilder.RenameTable(
                name: "UserItem",
                newName: "UserItems");

            migrationBuilder.RenameIndex(
                name: "IX_UserItem_ItemId",
                table: "UserItems",
                newName: "IX_UserItems_ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserItems",
                table: "UserItems",
                columns: ["UserId", "ItemId"]);

            migrationBuilder.AddForeignKey(
                name: "FK_UserItems_Items_ItemId",
                table: "UserItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserItems_Users_UserId",
                table: "UserItems",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.InsertData(
                table: "Items",
                columns: ["Id", "Name", "Price"],
                values: new object[,]
                {
                    { 1, "Laptop", 999.99m },       // Ноутбук
                    { 2, "Smartphone", 699.99m },   // Смартфон
                    { 3, "Headphones", 149.99m },   // Наушники
                    { 4, "Smartwatch", 249.99m },   // Умные часы
                    { 5, "Tablet", 399.99m },       // Планшет
                    { 6, "Wireless Mouse", 49.99m }, // Беспроводная мышь
                    { 7, "Mechanical Keyboard", 89.99m }, // Механическая клавиатура
                    { 8, "Gaming Monitor", 299.99m },    // Игровой монитор
                    { 9, "External Hard Drive", 79.99m }, // Внешний жесткий диск
                    { 10, "Bluetooth Speaker", 59.99m }  // Блютуз-колонка
                }
    );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValues: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            );
            migrationBuilder.DropForeignKey(
                name: "FK_UserItems_Items_ItemId",
                table: "UserItems");

            migrationBuilder.DropForeignKey(
                name: "FK_UserItems_Users_UserId",
                table: "UserItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserItems",
                table: "UserItems");

            migrationBuilder.RenameTable(
                name: "UserItems",
                newName: "UserItem");

            migrationBuilder.RenameIndex(
                name: "IX_UserItems_ItemId",
                table: "UserItem",
                newName: "IX_UserItem_ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserItem",
                table: "UserItem",
                columns: ["UserId", "ItemId"]);

            migrationBuilder.AddForeignKey(
                name: "FK_UserItem_Items_ItemId",
                table: "UserItem",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserItem_Users_UserId",
                table: "UserItem",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
