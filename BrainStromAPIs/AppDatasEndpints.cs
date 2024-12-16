using BrainStromAPIs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;

public static class AppDatasEndpoints
{
    public static void RegisterAppDatasEndpoints(this WebApplication app)
    {
        //ע���û����ݵ�API����̵ĸ��ݴ�����˻����룬�����ݿ��д���һ���û�
        app.MapPost("/api/auth/register", async (AppDbContext db, RegisterModel model) =>
        {
            // ��֤�û��Ƿ��Ѿ�����
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
        .WithDescription("������˻����룬�����ݿ��д���һ���û�")
        .WithName("Register");


        //��¼API�����ݴ�����˻����룬��֤�û��Ƿ���ڣ��������������JWT Token
        app.MapPost("/api/auth/login", async (AppDbContext db, AuthService authService, LoginModel model) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            // ��֤�û��Ƿ���� �Լ� �����Ƿ���ȷ 
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Results.Unauthorized(); //new { message = "Invalid username or password." }
            }

            // ���� JWT Token
            var token = authService.GenerateJwtToken(user);

            return Results.Ok(new { Token = token });
        })
        .WithDescription("���ݴ�����˻����룬��֤�û��Ƿ���ڣ������Ƿ���ȷ�����������֤JWT Token")
        .WithName("Login");


        //������������������������������������������������������������������������������������������������������������������������������������������������������������
        // �ύһ����̵�Idea
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
        .WithDescription("�ύһ����̵�Idea")
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
            .WithDescription("���Ҳ������������")
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
            .WithDescription("����ָ��id����з���")
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
            .WithDescription("�޸�ָ�����")
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
            .WithDescription("ɾ��ָ�����")
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