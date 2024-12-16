using BrainStromAPIs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;

public static class BrainStormDatasEndpoints
{
    public static void RegisterAppDatasEndpoints(this WebApplication app)
    {
        #region ��¼ע��
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
        .WithDescription("���ݴ�����˻����룬��֤�û��Ƿ���ڣ������Ƿ���ȷ������������֤JWT Token")
        .WithName("Login");
        #endregion
        #region ������
        //����������������������������������������������������������������������������CRUD����������������������������������������������������������������������������
        // �ύһ��Idea
        app.MapPost("/api/ideas", async (AppDbContext db, HttpContext httpContext, CreateIdeaModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = new Idea
            {
                Title = model.Title,
                Description = model.Description,
                ThemeTitle = model.ThemeTitle,
                TagsName = model.TagsName
            };

            //�����û������û���ֵ��idea.User
            idea.User = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            idea.Theme = await db.Themes.FirstOrDefaultAsync(t => t.Title == idea.ThemeTitle);
            idea.Tags = await db.Tags.Where(t => idea.TagsName.Contains(t.Name)).ToListAsync();

            db.Ideas.Add(idea);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea created successfully." });
        })
        .WithDescription("�ύһ��Idea�����б��⣬���������⣬��ǩ")
        .WithName("CreateIdea")
        .RequireAuthorization();

        //�����û�id���Ҳ������������
        app.MapGet("/api/ideas", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            //ͨ���û�id���Ҳ������������
            var ideas = await db.Ideas
                .Where(i => i.UserId == userId)
                .ToListAsync();

            return Results.Ok(ideas);
        })
            .WithDescription("�����û�id���Ҳ�����������У��û�id��JWT Token�б��棩")
        .WithName("GetIdeas")
        .RequireAuthorization();

        //�������id���Ҳ����أ����ҵ�ǰ�û��ģ�
        app.MapGet("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.Id == id) //��Ҫ���ڴ��û�id
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(idea);
        })
            .WithDescription("�������id���Ҳ����أ����ҵ�ǰ�û��ģ�")
        .WithName("GetIdeaById")
        .RequireAuthorization();

        //�������id�޸���У��޸ĵ�ǰ�û��ģ�
        app.MapPut("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id, UpdateIdeaModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;
            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.Id == id)
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            idea.Title = model.Title;
            idea.Description = model.Description;
            idea.ThemeTitle = model.ThemeTitle;
            idea.TagsName = model.TagsName;


            idea.Theme = await db.Themes.FirstOrDefaultAsync(t => t.Title == idea.ThemeTitle);
            idea.Tags = await db.Tags.Where(t => idea.TagsName.Contains(t.Name)).ToListAsync();


            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea updated successfully." });
        })
            .WithDescription("�������id�޸���У��޸ĵ�ǰ�û��ģ�")
        .WithName("UpdateIdea")
        .RequireAuthorization();

        //�������idɾ����У�ɾ����ǰ�û��ģ�
        app.MapDelete("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.Id == id)
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            db.Ideas.Remove(idea);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea deleted successfully." });
        })
            .WithDescription("�������idɾ����У�ɾ����ǰ�û��ģ�")
        .WithName("DeleteIdea")
        .RequireAuthorization();

        //���������������������������������������������������������������ݲ������ҡ�������������������������������������������������������������������
        //�������������Ҳ�����������У�Ĭ�ϰ���ʱ��˳������
        app.MapGet("/api/ideas/SearchByTheme", async (AppDbContext db, HttpContext httpContext, string themeName) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;
            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.ThemeTitle == themeName)
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefaultAsync();
            return Results.Ok(idea);
        })
            .WithDescription("���������������������")
            .WithName("GetIdeasByTheme")
            .RequireAuthorization();

        //���ݱ�ǩ�����Ҳ������������,Ĭ�ϰ���ʱ��˳������
        app.MapGet("/api/ideas/SearchByTag", async (AppDbContext db, HttpContext httpContext, string tagName) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;
            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.TagsName.Contains(tagName))
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefaultAsync();
            return Results.Ok(idea);
        })
            .WithDescription("���ݱ�ǩ�������������")
            .WithName("GetIdeasByTag")
            .RequireAuthorization();

        //�����������ͱ�ǩ�����Ҳ������������,Ĭ�ϰ���ʱ��˳������
        //TODO: �޸ģ����ڷ��ص��ǵ�һ����Ӧ�÷�������
        app.MapGet("/api/ideas/SearchByThemeAndTag/{rule}", async (AppDbContext db, HttpContext httpContext, string themeName, string tagName,string ?rule) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            if (string.IsNullOrEmpty(themeName)||db.Themes.FirstOrDefault()==null)
            {
                return Results.BadRequest("ThemeName is null or empty.");
            }

            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.ThemeTitle == themeName && i.TagsName.Contains(tagName))
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefaultAsync();
            return Results.Ok(idea);
        })
            .WithDescription("�����������ͱ�ǩ�������������")
            .WithName("GetIdeasByThemeAndTag")
            .RequireAuthorization();

        //�����������������һ�����
        app.MapGet("/api/ideas/RandomByTheme/{rule}", async (AppDbContext db, HttpContext httpContext, string themeName,string? rule) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;
            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.ThemeTitle == themeName)
                .OrderBy(i => Guid.NewGuid())
                .FirstOrDefaultAsync();
            return Results.Ok(idea);
        })
            .WithDescription("�����������������һ�����")
            .WithName("GetRandomIdeaByTheme")
            .RequireAuthorization();

        //���ݱ�ǩ���������һ�����
        app.MapGet("/api/ideas/RandomByTag", async (AppDbContext db, HttpContext httpContext, string tagName) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;
            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.TagsName.Contains(tagName))
                .OrderBy(i => Guid.NewGuid())
                .FirstOrDefaultAsync();
            return Results.Ok(idea);
        })
            .WithDescription("���ݱ�ǩ���������һ�����")
            .WithName("GetRandomIdeaByTag")
            .RequireAuthorization();
        #endregion
        #region �����CRUD

        app.MapPost("/api/themes",async (AppDbContext db, HttpContext httpContext, CreateThemeModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var theme = new Theme
            {
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
            };

            db.Themes.Add(theme);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea created successfully." });
        })
        .WithDescription("�½�һ������")
        .WithName("CreateThemes")
        .RequireAuthorization();

        app.MapGet("/api/themes", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            //ͨ���û�id���Ҳ������������
            var themes = await db.Themes
                .Where(i => i.UserId == userId)
                .ToListAsync();

            return Results.Ok(themes);
        })
        .WithDescription("��ѯ��������")
        .WithName("GetThemes")
        .RequireAuthorization();

        app.MapDelete("/api/themes/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var theme = await db.Themes
                .Where(i => i.UserId == userId && i.Id == id)
                .FirstOrDefaultAsync();

            if (theme == null)
            {
                return Results.NotFound();
            }

            db.Themes.Remove(theme);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea deleted successfully." });
        })
            .WithDescription("��������idɾ����У�ɾ����ǰ�û��ģ�")
        .WithName("DeleteTheme")
        .RequireAuthorization();

        #endregion
        #region ��ǩ��CRUD
        app.MapGet("/api/ideas/tags", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var tags = await db.Tags
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return Results.Ok(tags);
        })
    .WithDescription("��ѯ��ǰ�û����еı�ǩ")
        .WithName("GetTags")
        .RequireAuthorization();
        app.MapPost("/api/ideas/tags", async (AppDbContext db, HttpContext httpContext, string tagName) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var tag = new Tag
            {
                UserId = userId,
                Name = tagName
            };

            db.Tags.Add(tag);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Tag created successfully." });
        })
            .WithDescription("�½�һ����ǩ")
            .WithName("CreateTag")
            .RequireAuthorization();
        #endregion
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
    public string ThemeTitle { get; set; }
    public ICollection<string> TagsName { get; set; }

}
public class UpdateIdeaModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string ThemeTitle { get; set; }
    public ICollection<string> TagsName { get; set; }
}

public class CreateThemeModel
{
    public string Title { get; set; }
    public string Description { get; set; }
}