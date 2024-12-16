using BrainStromAPIs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;

public static class BrainStormDatasEndpoints
{
    public static void RegisterAppDatasEndpoints(this WebApplication app)
    {
        #region 登录注册
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
        #endregion
        #region 灵感相关
        //——————————————————————————————————————CRUD——————————————————————————————————————
        // 提交一条Idea
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

            //查找用户，将用户赋值给idea.User
            idea.User = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            idea.Theme = await db.Themes.FirstOrDefaultAsync(t => t.Title == idea.ThemeTitle);
            idea.Tags = await db.Tags.Where(t => idea.TagsName.Contains(t.Name)).ToListAsync();

            db.Ideas.Add(idea);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Idea created successfully." });
        })
        .WithDescription("提交一条Idea，带有标题，描述，主题，标签")
        .WithName("CreateIdea")
        .RequireAuthorization();

        //根据用户id查找并返回所有灵感
        app.MapGet("/api/ideas", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            //通过用户id查找并返回所有灵感
            var ideas = await db.Ideas
                .Where(i => i.UserId == userId)
                .ToListAsync();

            return Results.Ok(ideas);
        })
            .WithDescription("根据用户id查找并返回所有灵感（用户id在JWT Token中保存）")
        .WithName("GetIdeas")
        .RequireAuthorization();

        //根据灵感id查找并返回（查找当前用户的）
        app.MapGet("/api/ideas/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var idea = await db.Ideas
                .Where(i => i.UserId == userId && i.Id == id) //既要属于此用户id
                .FirstOrDefaultAsync();

            if (idea == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(idea);
        })
            .WithDescription("根据灵感id查找并返回（查找当前用户的）")
        .WithName("GetIdeaById")
        .RequireAuthorization();

        //根据灵感id修改灵感（修改当前用户的）
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
            .WithDescription("根据灵感id修改灵感（修改当前用户的）")
        .WithName("UpdateIdea")
        .RequireAuthorization();

        //根据灵感id删除灵感（删除当前用户的）
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
            .WithDescription("根据灵感id删除灵感（删除当前用户的）")
        .WithName("DeleteIdea")
        .RequireAuthorization();

        //——————————————————————————————根据参数查找——————————————————————————————————
        //根据主题名查找并返回所有灵感，默认按照时间顺序排序
        app.MapGet("/api/ideas/SearchByTheme/{rule}", async (AppDbContext db, HttpContext httpContext, string themeName, string? rule) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

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
            .WithDescription("根据主题名返回所有灵感")
            .WithName("GetIdeasByTheme")
            .RequireAuthorization();

        //根据标签名查找并返回所有灵感,默认按照时间顺序排序
        app.MapGet("/api/ideas/SearchByTag/{rule}", async (AppDbContext db, HttpContext httpContext, string tagName, string? rule) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

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
            .WithDescription("根据标签名返回所有灵感")
            .WithName("GetIdeasByTag")
            .RequireAuthorization();

        //根据主题名和标签名查找并返回所有灵感,默认按照时间顺序排序
        app.MapGet("/api/ideas/SearchByThemeAndTag/{rule}", async (AppDbContext db, HttpContext httpContext, string themeName, string tagName,string ?rule) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

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
            .WithDescription("根据主题名和标签名返回所有灵感")
            .WithName("GetIdeasByThemeAndTag")
            .RequireAuthorization();

        //根据主题名随机返回一条灵感
        app.MapGet("/api/ideas/RandomByTheme", async (AppDbContext db, HttpContext httpContext, string themeName) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

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
            .WithDescription("根据主题名随机返回一条灵感")
            .WithName("GetRandomIdeaByTheme")
            .RequireAuthorization();

        //根据标签名随机返回一条灵感
        app.MapGet("/api/ideas/RandomByTag", async (AppDbContext db, HttpContext httpContext, string tagName) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

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
            .WithDescription("根据标签名随机返回一条灵感")
            .WithName("GetRandomIdeaByTag")
            .RequireAuthorization();
        #endregion
        #region 主题的CRUD

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
        .WithDescription("新建一个主题")
        .WithName("CreateThemes")
        .RequireAuthorization();

        app.MapGet("/api/themes", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            //通过用户id查找并返回所有灵感
            var themes = await db.Themes
                .Where(i => i.UserId == userId)
                .ToListAsync();

            return Results.Ok(themes);
        })
        .WithDescription("查询所有主题")
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
            .WithDescription("根据主题id删除灵感（删除当前用户的）")
        .WithName("DeleteTheme")
        .RequireAuthorization();

        #endregion
        #region 标签的CRUD
        app.MapGet("/api/ideas/tags", async (AppDbContext db, HttpContext httpContext) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

            var tags = await db.Tags
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return Results.Ok(tags);
        })
    .WithDescription("查询当前用户所有的标签")
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
            .WithDescription("新建一个标签")
            .WithName("CreateTag")
            .RequireAuthorization();
        app.MapDelete("/api/ideas/tags/{id}", async (AppDbContext db, HttpContext httpContext, int id) =>
        {
            var userId = int.TryParse(httpContext.User?.Identity?.Name, out var parsedId) ? parsedId : 0;

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
            .WithDescription("根据标签id删除标签（删除当前用户的）");
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
