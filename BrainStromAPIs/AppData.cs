using Microsoft.EntityFrameworkCore;

namespace BrainStromAPIs
{
    public enum Role
    {
        administrators, //管理员
        users //用户
    }
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<User> Users { get; set; } //队伍里的成员
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Role Role { get; set; } 
        public ICollection<Team> Teams { get; set; } //用户属于的队伍
        public ICollection<Idea> Ideas { get; set; } // 用户创建的Idea
    }
    public class Theme
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
    public class Idea
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string ThemeTitle { get; set; }
        public Theme ?Theme { get; set; }
        public int UserId { get; set; }
        public User ?User { get; set; }
        public ICollection<string> TagsName { get; set; }
        public ICollection<Tag> ?Tags { get; set; } // 导航属性 一个Idea可以有多个Tag
    }

    public class Tag
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Idea> Ideas { get; set; }  // 导航属性 一个Tag可以对应多个Idea
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Idea> Ideas { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Theme> Themes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置 User 与 Idea 之间的关系
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.User) // 一个Idea只能有一个User
                .WithMany(u => u.Ideas) // 一个User可以有多个Idea
                .HasForeignKey(i => i.UserId) // 外键,被谁创建的Idea
                .OnDelete(DeleteBehavior.Cascade); // 级联删除

            //让User的Role字段使用string类型
            modelBuilder.Entity<User>()
                .Property(i => i.Role)
                .HasConversion<string>(); // 将枚举存储为字符串
        }
    }

    
}

