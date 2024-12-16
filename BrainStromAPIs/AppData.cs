using Microsoft.EntityFrameworkCore;

namespace BrainStromAPIs
{
    public enum Role
    {
        administrators, //管理员
        users //用户
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Role Role { get; set; } 
        public ICollection<Idea> Ideas { get; set; }
    }

    public class Idea
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ?Description { get; set; }
        public string ?Category { get; set; }
        public int CreatedBy { get; set; } // 外键
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Tag> ?Tags { get; set; } // 导航属性 一个Idea可以有多个Tag
        public User User { get; set; }   // 导航属性
    }

    public class Tag
    {
        public int Id { get; set; }
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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置 User 与 Idea 之间的关系
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.User) // 一个Idea只能有一个User
                .WithMany(u => u.Ideas) // 一个User可以有多个Idea
                .HasForeignKey(i => i.CreatedBy) // 外键
                .OnDelete(DeleteBehavior.Cascade); // 级联删除

            //让User的Role字段使用string类型
            modelBuilder.Entity<User>()
                .Property(i => i.Role)
                .HasConversion<string>(); // 将枚举存储为字符串
        }
    }

    
}

