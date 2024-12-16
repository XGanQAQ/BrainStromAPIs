using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace PocketPlaneAPIs
{
    public class PlayerData
    {
        [Key]
        public int AccountId { get; set; }
        public string? Name { get; set; }
        public int Money { get; set; } = 0;
        public int Level { get; set; } = 1;
    }

    public class PlayerDataDb : DbContext
    {
        public PlayerDataDb(DbContextOptions options) : base(options) { }
        public DbSet<PlayerData> playerDatas { get; set; } = null!;
    }

    public static class PlayerDataEndpoints
    {
        public static void RegisterPlayerDatasEndpoints(this WebApplication app)
        {
            app.MapGet("/PlayerDatas/GetAll", GetAllPlayerDatas)
            .WithName("GetAllPlayerDatas")
            .WithOpenApi();

            app.MapPost("/PlayerDatas/Post", PostPlayerData)
            .WithName("PostPlayerData")
            .WithOpenApi();
        }

        private static async Task<IResult> GetAllPlayerDatas(PlayerDataDb db)
        {
            //获取所有玩家数据
            var playerDatas = await db.playerDatas.ToListAsync();
            return Results.Ok(playerDatas);
        }
        private static async Task<IResult> PostPlayerData(PlayerDataDb db, PlayerData playerData)
        {
            //根据玩家参数，在数据库中创建一个新的玩家数据
            if (playerData == null) return Results.BadRequest("Player data is null");

            await db.playerDatas.AddAsync(playerData);
            await db.SaveChangesAsync();

            return Results.Ok(playerData);
        }
    }
}
