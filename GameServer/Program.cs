using Microsoft.EntityFrameworkCore;
using GameServer.Models;
using GameServer.Data;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// DbContext 등록
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 42))
    ));

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// 로그인
app.MapPost("/api/login", async (GameDbContext db, LoginRequest req) =>
{
    var user = await db.UserAccount
        .FirstOrDefaultAsync(u => u.ID == req.ID);

    if (user == null)
    {
        return Results.Unauthorized();
    }

    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.Password);
    if (!isPasswordValid)
    {
        return Results.Unauthorized();
    }

    var response = new LoginResponse
    {
        UserUniqueID = user.UserUniqueID,
        Nickname = user.Nickname,
    };

    return Results.Ok(response);
});


// 회원가입
app.MapPost("/api/register", async (GameDbContext db, RegisterRequest req) =>
{
    var existingUser = await db.UserAccount.FirstOrDefaultAsync(u => u.ID == req.ID);
    if (existingUser != null)
    {
        return Results.Conflict(new { message = "This ID is already in use" });
    }

    // brypt hash
    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);

    var newUser = new UserAccount
    {
        ID = req.ID,
        Password = hashedPassword, // <= 비크립트 해시 변경점
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
