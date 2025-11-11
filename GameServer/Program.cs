using Microsoft.EntityFrameworkCore;
using GameServer.Models;
using GameServer.Data;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// --------------------
// 1. DbContext 등록
// --------------------
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 42))
    ));

// --------------------
// 2. CORS / Swagger
// --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------
// 3. Authentication / Authorization
// --------------------
var key = Encoding.ASCII.GetBytes("this_is_a_very_long_secret_key_for_jwt_123456"); // JWT Signing key

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// --------------------
// 4. Middleware 순서
// --------------------
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --------------------
// 5. 로그인
// --------------------
app.MapPost("/api/login", async (GameDbContext db, LoginRequest req) =>
{
    var user = await db.UserAccount.FirstOrDefaultAsync(u => u.ID == req.ID);
    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
        return Results.Unauthorized();

    // JWT 발급
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserUniqueID.ToString()),
            new Claim(ClaimTypes.Name, user.Nickname)
        }),
        Expires = DateTime.UtcNow.AddHours(12),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    string jwt = tokenHandler.WriteToken(token);

    return Results.Ok(new
    {
        UserUniqueID = user.UserUniqueID,
        Nickname = user.Nickname,
        Token = jwt
    });
});

// --------------------
// 6. 회원가입
// --------------------
app.MapPost("/api/register", async (GameDbContext db, RegisterRequest req) =>
{
    var existingUser = await db.UserAccount.FirstOrDefaultAsync(u => u.ID == req.ID);
    if (existingUser != null)
        return Results.Conflict(new { message = "This ID is already in use" });

    var newUser = new UserAccount
    {
        ID = req.ID,
        Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
        Email = req.Email,
        Name = req.Name,
        Nickname = req.Nickname
    };

    db.UserAccount.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        UserUniqueID = newUser.UserUniqueID,
        Nickname = newUser.Nickname
    });
});

// --------------------
// 7. Scene 저장
// --------------------
app.MapPost("/api/scene/save", [Authorize] async (ClaimsPrincipal user, GameDbContext db, SceneDataRequest req) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

    Console.WriteLine($"[SAVE] User={userId}, Scene={req.SceneName}, X={req.PositionX}, Y={req.PositionY}");

    var existing = await db.UserSceneData
        .FirstOrDefaultAsync(x => x.UserUniqueID == userId && x.SceneName == req.SceneName);

    if (existing == null)
    {
        db.UserSceneData.Add(new UserSceneData
        {
            UserUniqueID = userId,
            SceneName = req.SceneName,
            PositionX = req.PositionX,
            PositionY = req.PositionY,
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {
        existing.PositionX = req.PositionX;
        existing.PositionY = req.PositionY;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Saved scene data" });
});

// --------------------
// 8. Scene 불러오기
// --------------------
app.MapGet("/api/scene/load/{sceneName}", [Authorize] async (ClaimsPrincipal user, GameDbContext db, string sceneName) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

    var sceneData = await db.UserSceneData
        .FirstOrDefaultAsync(x => x.UserUniqueID == userId && x.SceneName == sceneName);

    if (sceneData == null)
        return Results.NotFound(new { message = "No saved data" });

    return Results.Ok(sceneData);
});

app.Run();

// --------------------
// 9. DTO 정의
// --------------------
public class LoginRequest
{
    public string ID { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public int UserUniqueID { get; set; }
    public string Nickname { get; set; } = null!;
    public string Token { get; set; } = null!;
}

public class RegisterRequest
{
    public string ID { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Nickname { get; set; } = null!;
}

public class SceneDataRequest
{
    public string SceneName { get; set; } = null!;
    public float PositionX { get; set; }
    public float PositionY { get; set; }
}
