using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShoppingList;
using ShoppingList.Entities;
using ShoppingList.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)));

// Чтение конфигурации JWT из appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var adminIds = builder.Configuration.GetValue<List<int>>("AdminIds")!;

builder.Services.AddAutoMapper(typeof(Program));

// Добавление аутентификации
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
});
builder.Services.AddAuthorization();

// Добавляем Swagger
builder.Services.AddSwaggerGen(c =>
{
    // Описание схемы безопасности для JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = @"Введите токен в формате 'Bearer {токен}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Добавляем требование авторизации для всех эндпоинтов
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Метод генерации JWT-
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/cart", async (HttpContext context, ApplicationContext db, IMapper mapper) =>
{
    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var items = await GetCart(db, userId);
    var errors = GenerateStockWarningMessages(items);

    if (errors != "")
    {
        return Results.Conflict(errors);
    }
    else
    {
        var response = mapper.Map<IEnumerable<ItemDTO>>(items);
        //var response = items.Select(i => new ItemDTO { Name = i.Name, Quantity = i.OriginalQuantity, Price = i.Price });

        return Results.Ok(response);
    }
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapDelete("/cart/remove", async (HttpContext context, ApplicationContext db, int itemId) =>
{
    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var item = await db.UserItems.FirstOrDefaultAsync(x => x.ItemId == itemId && x.Status == ItemStatus.InCart);
    if (item is null)
    {
        return Results.NotFound("Item is not found.");
    }

    db.Remove(item);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPatch("/cart/change-quantity", async (HttpContext context, ApplicationContext db, int itemId, int quantity) =>
{
    if (quantity <= 0)
    {
        return Results.BadRequest("Quantity must be greater than 0.");
    }

    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var item = await db.UserItems.FirstOrDefaultAsync(x => x.ItemId == itemId && x.Status == ItemStatus.InCart);
    if (item is null)
    {
        return Results.NotFound("Item is not found.");
    }

    if (quantity >= item.Quantity)
    {
        return Results.Conflict("Quantity exceeds available items in the cart. Use /cart/remove to remove item.");
    }

    item.Quantity -= quantity;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPost("/cart/add", async (HttpContext context, ApplicationContext db, int itemId, int quantity) =>
{
    if (itemId <= 0 || quantity <= 0)
    {
        return Results.BadRequest();
    }

    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var item = await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId);
    if (item is null)
    {
        return Results.NotFound("Item is not found.");
    }

    if (quantity > item.Quantity)
    {
        return Results.BadRequest("There are not enough items available.");
    }

    var userItem = new UserItem 
    { 
        ItemId = item.Id,
        Quantity = quantity,
        UserId = userId,
        Status = ItemStatus.InCart
    };

    await db.UserItems.AddAsync(userItem);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPost("/cart/checkout", async (HttpContext context, ApplicationContext db) =>
{
    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
    if (user is null)
    {
        return Results.NotFound("User is not found.");
    }

    var items = await GetCart(db, userId);
    var errors = GenerateStockWarningMessages(items);
    if (errors != "")
    {
        return Results.Conflict(errors);
    } else
    {
        var fullItems = await db.UserItems
        .Where(ui => ui.UserId == userId && ui.Status == ItemStatus.InCart)
        .Include(ui => ui.Item).ToListAsync();

        var price = items.Sum(i => i.Price * i.OriginalQuantity);
        if (user.Balance < price)
        {
            return Results.Conflict($"Not enough balance({user.Balance}). Increase your balance by atleast {price - user.Balance:N2}");
        }

        user.Balance -= price;
        foreach (var item in fullItems)
        {
            item.Status = ItemStatus.Purchased;
            item.Item!.Quantity -= item.Quantity;
        }
        await db.SaveChangesAsync();

        return Results.Ok($"You spent {price:N2}$. Well done!");
    }

})
    .WithOpenApi()
    .RequireAuthorization();

app.MapGet("/my-storage", async (HttpContext context, ApplicationContext db) =>
{
    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var items = await db.UserItems
        .Include(ui => ui.Item)
        .Where(ui => ui.UserId == userId && ui.Status == ItemStatus.Purchased)
        .AsNoTracking()
        .Select(ui => new
        {
            ui.Item!.Name,
            ui.Quantity,
            ui.Item!.Price
        })
        .ToListAsync();

    return Results.Ok(items);
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapGet("/catalog", async (ApplicationContext db) =>
{
    var items = await db.Items.Where(i => i.Quantity > 0).Select(i => new
    {
        i.Id,
        i.Name,
        i.Quantity,
        i.Price
    }).ToListAsync();

    return Results.Ok(items);
})
    .WithOpenApi();

app.MapPost("/register", async (string login, string password, ApplicationContext db) =>
{
    if (password.Length < 8)
        return Results.BadRequest("The password must consist of at least 8 characters.");

    if (await db.Users.AnyAsync(u => u.Login == login))
        return Results.BadRequest("User with such login is already exists.");

    var user = new User { Login = login, Password = password };
    await db.AddAsync(user);
    await db.SaveChangesAsync();
    var token = GenerateJwtToken(jwtSettings, user.Login, user.Id, adminIds);
    return Results.Ok(new { token });
})
    .WithOpenApi();

app.MapPost("/login", async (HttpContext context, string login, string password, ApplicationContext db) =>
{
    if (context.User.Identity!.IsAuthenticated)
    {
        return Results.Ok("User is already authenticated.");
    }

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login && u.Password == password);
    if (user is not null)
    {
        var token = GenerateJwtToken(jwtSettings, user.Login, user.Id, adminIds);
        return Results.Ok(new { token });
    }

    return Results.NotFound("User is not found.");
})
    .WithOpenApi();

app.MapGet("/balance", async (HttpContext context, ApplicationContext db) =>
{
    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
    if (user is null)
    {
        return Results.NotFound("User is not found.");
    }

    return Results.Ok(user.Balance);
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPatch("/balance/add", async (HttpContext context, ApplicationContext db, decimal amount) =>
{
    if (amount <= 0)
    {
        return Results.BadRequest("Incorrect amount.");
    }
    if (!TryGetUserId(context, out int userId))
    {
        return Results.NotFound("User is not found.");
    }

    var user = await db.Users.FindAsync(userId);
    if (user is null)
    {
        return Results.NotFound("User is not found.");
    }

    user.Balance += amount;

    await db.SaveChangesAsync();
    return Results.Ok($"Current balance is {user.Balance}");
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPost("/storage/item/add", async (ApplicationContext db, string name, int quantity, decimal price) =>
{
    if (quantity < 0 || price <= 0 || string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest("Wrong input");
    }

    await db.Items.AddAsync(new Item
    {
        Name = name,
        Quantity = quantity,
        Price = price
    });

    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithOpenApi()
    .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.MapPatch("/storage/item/change", async (ApplicationContext db, int id, string? name, decimal? price, int? newQuantity, int? addToQuantity) =>
{
    // Проверяем некорректное использование newQuantity и addToQuantity
    if (newQuantity.HasValue && addToQuantity.HasValue)
    {
        return Results.BadRequest("You can't use \"newQuantity\" and \"addToQuantity\" at the same time.");
    }

    // Ищем item в базе
    var item = await db.Items.FindAsync(id);
    if (item is null)
    {
        return Results.NotFound("Item not found.");
    }

    // Обновляем поля объекта, если они не null
    item.Quantity = newQuantity ?? item.Quantity + (addToQuantity ?? 0);
    if (!string.IsNullOrEmpty(name))
    {
        item.Name = name;
    }
    if (price.HasValue)
    {
        item.Price = price.Value;
    }

    // Сохраняем изменения
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithOpenApi()
    .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.Run();

static string GenerateJwtToken(IConfigurationSection jwtSettings, string login, int id, List<int> adminIds)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, id.ToString()),
        new(ClaimTypes.Name, login)
    };

    if (adminIds.Contains(id))
    {
        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
    }
    else
    {
        claims.Add(new Claim(ClaimTypes.Role, "User"));
    }

    var token = new JwtSecurityToken(
        issuer: jwtSettings["Issuer"],
        audience: jwtSettings["Audience"],
        claims: claims,
        expires: DateTime.Now.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"]!)),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
static async Task<List<ItemToChange>> GetCart(ApplicationContext db, int userId)
{
    var items = await db.UserItems
        .Where(ui => ui.UserId == userId && ui.Status == ItemStatus.InCart)
        .Include(ui => ui.Item)
        .Select(ui => new ItemToChange
        {
            Name = ui.Item!.Name!,
            OriginalQuantity = ui.Quantity,
            StorageQuantity = ui.Item.Quantity,
            Price = ui.Item.Price
        })
        .ToListAsync();

    return items;
}
static string GenerateStockWarningMessages(List <ItemToChange> items)
{
    var stringBuilder = new StringBuilder();
    foreach (var item in items)
    {
        if (item.OriginalQuantity > item.StorageQuantity)
        {
            stringBuilder.AppendLine($"{item.Name} quantity in stock is {item.StorageQuantity}, but your cart has {item.OriginalQuantity}. Please reduce amount of {item.Name} to {item.OriginalQuantity} or less.");
        }
    }

    return stringBuilder.ToString();
}
static bool TryGetUserId(HttpContext context, out int userId)
{
    return int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}