using Microsoft.EntityFrameworkCore;
using GameServer.Models;
using GameServer.Data;

var builder = WebApplication.CreateBuilder(args);

// DbContext ���
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 42)) // ���� MySQL ����
    ));

// OpenAPI ���� (���� ȯ�濡����)
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// OpenAPI ����
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS ����
app.UseHttpsRedirection();

// �α��� API
app.MapPost("/api/login", async (GameDbContext db, LoginRequest req) =>
{
    var user = await db.UserAccount
        .FirstOrDefaultAsync(u => u.ID == req.ID);

    if (user == null)
    {
        return Results.Unauthorized(); // 401 Unauthorized
    }

    var response = new LoginResponse
    {
        UserUniqueID = user.UserUniqueID,
        Nickname = user.Nickname,
    };

    return Results.Ok(response); // 200 OK + JSON
});

app.MapPost("/api/register", async (GameDbContext db, RegisterRequest req) =>
{
    var existingUser = await db.UserAccount.FirstOrDefaultAsync(u => u.ID == req.ID);
    if (existingUser != null)
    {
        return Results.Conflict(new { message = "This ID is already in use" });
    }

    var newUser = new UserAccount
    {
        ID = req.ID,
        Password = req.Password,
        Email = req.Email,
        Name = req.Name,
        Nickname = req.Nickname
    };

    db.UserAccount.Add(newUser);
    await db.SaveChangesAsync();

    var response = new RegisterResponse
    {
        UserUniqueID = newUser.UserUniqueID,
        Nickname = newUser.Nickname
    };

    return Results.Ok(response);
});

app.Run();