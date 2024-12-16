using Microsoft.EntityFrameworkCore;

namespace BrainStromAPIs
{
    public class UserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        // 检查用户名是否存在
        public async Task<bool> UserExistsAsync(string username)
        {
            return await _db.Users.AnyAsync(u => u.Username == username);
        }

        // 获取用户通过用户名
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        // 创建新用户
        public async Task CreateUserAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // 获取用户通过ID
        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }

}
