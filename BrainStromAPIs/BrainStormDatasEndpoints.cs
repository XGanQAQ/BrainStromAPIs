using BrainStromAPIs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Security.Claims;

public static class BrainStormDatasEndpoints
{
    public static void RegisterAppDatasEndpoints(this WebApplication app)
    {
        #region ��¼ע��
        //ע���û����ݵ�API����̵ĸ��ݴ�����˻����룬�����ݿ��д���һ���û�
        app.MapPost("/api/auth/register", async (BrainStormDbContext db, RegisterModel model) =>
        {
            // ��֤�û��Ƿ��Ѿ�����
            if (await db.Users.AnyAsync(u => u.Username == model.Username))
            {
                return Results.BadRequest(new { message="Username is already taken." });
            }

            var user = new User
            {
                Email = model.Email,
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };


            db.Users.Add(user);
            await db.SaveChangesAsync();
            InitUserTheme(db,model.Username);
            
            return Results.Ok(new { message = "User registered successfully" });
        })
        .WithDescription("������˻����룬�����ݿ��д���һ���û�")
        .WithName("Register");


        //��¼API�����ݴ�����˻����룬��֤�û��Ƿ���ڣ��������������JWT Token
        app.MapPost("/api/auth/login", async (BrainStormDbContext db, AuthService authService, LoginModel model) =>
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

        app.MapGet("/api/auth/info", async (BrainStormDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (userId == 0)
            {
                Console.WriteLine("httpContext.User= " + httpContext.User);
                Console.WriteLine($"ClaimTypes.NameIdentifier= " + httpContext.User?.FindFirst(ClaimTypes.NameIdentifier));
                Console.WriteLine($"Value= " + httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }

            if (user == null)
            {
                return Results.NotFound(new { message = $"Can't find the user information,it's user Id is {userId}" });
            }

            return Results.Ok(new { username = user.Username, email = user.Email });
        })
        .WithDescription("����Token�������û���Ϣ")
        .WithName("GetUserInformation")
        .RequireAuthorization();
        #endregion
        #region ������
        //����������������������������������������������������������������������������CRUD����������������������������������������������������������������������������
        // �ύһ��Idea
        app.MapPost("/api/ideas", async (BrainStormDbContext db, HttpContext httpContext, CreateIdeaModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            foreach (var tagName in model.TagsName)
            {
                if (db.Tags.FirstOrDefault(t => t.Name == tagName) == null)
                {
                    return Results.BadRequest("�����ڴ˱�ǩ���������Ƿ��Ѿ�������ǰ��ǩ.");
                }
            }

            var idea = new Idea
            {
                Title = model.Title,
                Description = model.Description,
                ThemeTitle = model.ThemeTitle,
                TagsName = model.TagsName
            };

            idea.UserId = userId;
            //�����û������û���ֵ��idea.User
            idea.User = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            idea.Theme = await db.Themes.FirstOrDefaultAsync(t => t.Title == idea.ThemeTitle);
            //idea.ThemeId = idea.Theme.Id;
            idea.Tags = await db.Tags.Where(t => idea.TagsName.Contains(t.Name)).ToListAsync();

            if(idea.Theme == null)
            {
                return Results.BadRequest("�����ڴ����⣬�������Ƿ��Ѿ�������ǰ����.");
            }
            else
            {
                idea.ThemeId = idea.Theme.Id;
            }

            db.Ideas.Add(idea);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea created successfully." });
        })
        .WithDescription("�ύһ��Idea�����б��⣬���������⣬��ǩ")
        .WithName("CreateIdea")
        .RequireAuthorization();

        //�����û�id���Ҳ������������
        app.MapGet("/api/ideas", async (BrainStormDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

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
        app.MapGet("/api/ideas/{id}", async (BrainStormDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

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
        app.MapPut("/api/ideas/{id}", async (BrainStormDbContext db, HttpContext httpContext, int id, UpdateIdeaModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;
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
        app.MapDelete("/api/ideas/{id}", async (BrainStormDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;


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
        app.MapGet("/api/ideas/SearchByTheme/{rule}", async (BrainStormDbContext db, HttpContext httpContext, string themeName, string? rule) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (string.IsNullOrEmpty(themeName) || db.Themes.FirstOrDefault() == null)
            {
                return Results.BadRequest("ThemeName is null or empty.");
            }

            List<Idea> ideas;
            if(rule== "descend")
            {
                ideas = await db.Ideas
                .Where(i => i.UserId == userId && i.ThemeTitle == themeName)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            }
            else
            {
                ideas = await db.Ideas
                .Where(i => i.UserId == userId && i.ThemeTitle == themeName)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
            }
            return Results.Ok(ideas);
        })
            .WithDescription("���������������������")
            .WithName("GetIdeasByTheme")
            .RequireAuthorization();

        //���ݱ�ǩ�����Ҳ������������,Ĭ�ϰ���ʱ��˳������
        app.MapGet("/api/ideas/SearchByTag/{rule}", async (BrainStormDbContext db, HttpContext httpContext, string tagName, string? rule) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (string.IsNullOrEmpty(tagName) || db.Tags.FirstOrDefault() == null)
            {
                return Results.BadRequest("TagName is null or empty.");
            }

            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.TagsName.Contains(tagName))
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
            return Results.Ok(idea);
        })
            .WithDescription("���ݱ�ǩ�������������")
            .WithName("GetIdeasByTag")
            .RequireAuthorization();

        //�����������ͱ�ǩ�����Ҳ������������,Ĭ�ϰ���ʱ��˳������
        app.MapGet("/api/ideas/SearchByThemeAndTag/{rule}", async (BrainStormDbContext db, HttpContext httpContext, string themeName, string tagName,string ?rule) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (string.IsNullOrEmpty(themeName)||db.Themes.FirstOrDefault()==null)
            {
                return Results.BadRequest("ThemeName is null or empty.");
            }

            if (string.IsNullOrEmpty(tagName) || db.Tags.FirstOrDefault() == null)
            {
                return Results.BadRequest("TagName is null or empty.");
            }
            List<Idea> ideas;
                ideas = await db.Ideas
                .Where(i => i.UserId == userId && i.ThemeTitle == themeName && i.TagsName.Contains(tagName))
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
            return Results.Ok(ideas);


        })
            .WithDescription("�����������ͱ�ǩ�������������")
            .WithName("GetIdeasByThemeAndTag")
            .RequireAuthorization();

        //�����������������һ�����
        app.MapGet("/api/ideas/RandomByTheme", async (BrainStormDbContext db, HttpContext httpContext, string themeName) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (string.IsNullOrEmpty(themeName) || db.Themes.FirstOrDefault() == null)
            {
                return Results.BadRequest("ThemeName is null or empty.");
            }

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
        app.MapGet("/api/ideas/RandomByTag", async (BrainStormDbContext db, HttpContext httpContext, string tagName) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            if (string.IsNullOrEmpty(tagName) || db.Tags.FirstOrDefault() == null)
            {
                return Results.BadRequest("TagName is null or empty.");
            }

            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.TagsName.Contains(tagName))
                .OrderBy(i => Guid.NewGuid())
                .FirstOrDefaultAsync();
            return Results.Ok(idea);
        })
            .WithDescription("���ݱ�ǩ���������һ�����")
            .WithName("GetRandomIdeaByTag")
        .RequireAuthorization();

        app.MapGet("/api/ideas/ideas/All", async (BrainStormDbContext db, HttpContext httpContext) =>
        {
            //var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            //����ʱ��˳�����򣬷��������û������
            var ideas = await db.Ideas
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            return Results.Ok(ideas);
        })
            .WithDescription("����ʱ��˳�����򣬷��������û������")
            .WithName("GetAllUserIdeas")
            .RequireAuthorization();
        #endregion
        #region �����CRUD

        app.MapPost("/api/themes",async (BrainStormDbContext db, HttpContext httpContext, CreateThemeModel model) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            var theme = new Theme
            {
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
            };

            db.Themes.Add(theme);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Theme created successfully." });
        })
        .WithDescription("�½�һ������")
        .WithName("CreateThemes")
        .RequireAuthorization();

        app.MapGet("/api/themes", async (BrainStormDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            //ͨ���û�id���Ҳ������������
            var themes = await db.Themes
                .Where(i => i.UserId == userId)
                .ToListAsync();

            return Results.Ok(themes);
        })
        .WithDescription("��ѯ��������")
        .WithName("GetThemes")
        .RequireAuthorization();

        app.MapDelete("/api/themes/{id}", async (BrainStormDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

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
        app.MapGet("/api/tags", async (BrainStormDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            var tags = await db.Tags
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return Results.Ok(tags);
        })
    .WithDescription("��ѯ��ǰ�û����еı�ǩ")
        .WithName("GetTags")
        .RequireAuthorization();
        app.MapPost("/api/tags", async (BrainStormDbContext db, HttpContext httpContext, string tagName) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

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
        app.MapDelete("/api/tags/{id}", async (BrainStormDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;

            var tag = await db.Tags
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (tag == null)
            {
                return Results.NotFound();
            }

            db.Tags.Remove(tag);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Tag deleted successfully." });
        })
            .WithDescription("���ݱ�ǩidɾ����ǩ��ɾ����ǰ�û��ģ�");

        #endregion
    }

    //��ʼ���û�������
    private static async void InitUserTheme(BrainStormDbContext db,String UserName)
    {
        int ThisUserId = db.Users.FirstOrDefault(u => u.Username == UserName).Id;
        var theme1 = new Theme
        {
            Title = "����",
            UserId = ThisUserId,
            Description = "�������"
        };
        var theme2 = new Theme
        {
            Title = "����",
            UserId = ThisUserId,
            Description = "�������"
        };
        var theme3 = new Theme
        {
            Title = "�����",
            UserId = ThisUserId,
            Description = "��������"
        };
        var theme4 = new Theme
        {
            Title = "����",
            UserId = ThisUserId,
            Description = "�������"
        };
        var theme5 = new Theme
        {
            Title = "��������",
            UserId = ThisUserId,
            Description = "�����������"
        };
        db.Themes.Add(theme1);
        db.Themes.Add(theme2);
        db.Themes.Add(theme3);
        db.Themes.Add(theme4);
        db.Themes.Add(theme5);
        await db.SaveChangesAsync();
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
