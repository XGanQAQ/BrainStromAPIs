using BrainStromAPIs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;

public static class AppDatasEndpoints
{
    public static void RegisterAppDatasEndpoints(this WebApplication app)
    {
        //注册用户数据的API，简短的根据传入的账户密码，在数据库中创建一个用户
        app.MapPost("/api/auth/register", async (AppDbContext db, RegisterModel model) =>
        {
            // 验证用户是否已经存在
            if (await db.Users.AnyAsync(u => u.Username == model.Username))
            {
                return Results.BadRequest("Username is already taken.");
            }

            var user = new User
            {
                Email = model.Email,
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "User registered successfully" });
        })
        .WithDescription("传入的账户密码，在数据库中创建一个用户")
        .WithName("Register");


        //登录API，根据传入的账户密码，验证用户是否存在，如果存在则生成JWT Token
        app.MapPost("/api/auth/login", async (AppDbContext db, AuthService authService, LoginModel model) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            // 验证用户是否存在 以及 密码是否正确 
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Results.Unauthorized(); //new { message = "Invalid username or password." }
            }

            // 生成 JWT Token
            var token = authService.GenerateJwtToken(user);

            return Results.Ok(new { Token = token });
        })
        .WithDescription("根据传入的账户密码，验证用户是否存在，密码是否正确，返回身份验证JWT Token")
        .WithName("Login");


        //——————————————————————————————————————————————————————————————————————————————
        // 提交一条简短的Idea
        app.MapPost("/api/ideas", async (AppDbContext db, HttpContext httpContext, CreateIdeaModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = new Idea
            {
                Title = model.Title,
                Description = model.Description,
                CreatedBy = userId
            };

            db.Ideas.Add(idea);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea created successfully." });
        })
        .WithDescription("提交一条简短的Idea")
        .WithName("CreateIdea")
        .RequireAuthorization();

        app.MapGet("/api/ideas", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var ideas = await db.Ideas
                .Where(i => i.CreatedBy == userId)
                .ToListAsync();

            return Results.Ok(ideas);
        })
            .WithDescription("查找并返回所有灵感")
        .WithName("GetIdeas")
        .RequireAuthorization();

        app.MapGet("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = await db.Ideas
                .Where(i => i.CreatedBy == userId && i.Id == id)
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(idea);
        })
            .WithDescription("查找指定id的灵感返回")
        .WithName("GetIdeaById")
        .RequireAuthorization();

        app.MapPut("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id, UpdateIdeaModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;
            var idea = await db.Ideas
                .Where(i => i.CreatedBy == userId && i.Id == id)
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            idea.Title = model.Title;
            idea.Description = model.Description;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea updated successfully." });
        })
            .WithDescription("修改指定灵感")
        .WithName("UpdateIdea")
        .RequireAuthorization();


        app.MapDelete("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = await db.Ideas
                .Where(i => i.CreatedBy == userId && i.Id == id)
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            db.Ideas.Remove(idea);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea deleted successfully." });
        })
            .WithDescription("删除指定灵感")
        .WithName("DeleteIdea")
        .RequireAuthorization();

    }
}


public class RegisterModel
{
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class CreateIdeaModel
{
    public string Title { get; set; }
    public string Description { get; set; }

}

public class UpdateIdeaModel
{
    public string Title { get; set; }
    public string Description { get; set; }
}