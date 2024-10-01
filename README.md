
# ShoppingCartAPI

**ShoppingCartAPI** is a lightweight API designed to simulate a basic shopping cart system. This project serves as a practice for building a Minimal API using ASP.NET Core. It includes essential features for managing users, items, and shopping carts, while maintaining simplicity to focus on core API development skills.

## Features

### 🛒 Cart and Item Management
- Add, update, and delete items in the shopping cart.
- Manage user-specific cart items and statuses.

### 👨‍🦲User Management
- Create and manage users with balances and track item purchases.

### 💾 Database Integration
- Entity Framework Core is used for database management, with migrations for easy schema updates.

## Tech Stack

- **ASP.NET Core Minimal API**: Built using the Minimal API approach for lightweight, simple web service development.
- **Entity Framework Core**: Used for handling data persistence and migrations.
- **SQL Server**: Stores user, item, and cart data.

## Structure Overview

- **Entities/**: Contains models for `User`, `Item`, and `UserItem`, representing the shopping cart system's core elements.
- **Migrations/**: Tracks database changes, allowing the schema to evolve with features.
- **Models/**: Includes `ItemDTO` and `ItemToChange` to structure API requests and data updates.

## Minimal API Approach

This API demonstrates a simplified structure for creating RESTful endpoints in ASP.NET Core using the Minimal API approach. Instead of traditional MVC controllers.

## Screenshots

![1](https://github.com/user-attachments/assets/61a98590-5b73-4348-a0ae-f1ed4d4b2a48)
![2](https://github.com/user-attachments/assets/9cc8b818-8b51-46fe-a400-261612baf472)
![3](https://github.com/user-attachments/assets/2cd116a7-b2d7-455b-8b09-e34ac53e0561)
