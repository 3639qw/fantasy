using Microsoft.EntityFrameworkCore;
using GameServer.Models;
using GameServer.Data;

var builder = WebApplication.CreateBuilder(args);

// DbContext 등록
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 42)) // 실제 MySQL 버전
    ));

// OpenAPI 설정 (개발 환경에서만)
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

// OpenAPI 설정
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS 설정
app.UseHttpsRedirection();

// 로그인 API
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

app.Run();