using BrainStromAPIs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration; //获取配置信息

//builder.Services.AddDbContext<BrainStormDbContext>(options => options.UseInMemoryDatabase("AppData")); //暂时先使用内存数据库做测试

//注入服务
builder.Services.AddDbContext<BrainStormDbContext>(options =>
    options.UseSqlServer(configuration["DataBaseSet:ConnectionString"])
);
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 使用对称密钥（例如 HS256）来验证 JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // 设置签名密钥
            IssuerSigningKey = key,
            ValidateIssuer = false,  // 如果不验证发行者，可以设置为 false
            ValidateAudience = false,  // 如果不验证受众，可以设置为 false
            ValidateLifetime = false,  // 验证令牌过期时间
            ClockSkew = TimeSpan.Zero  // 过期时间允许的时钟偏差
        };

        // 添加错误处理回调
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // 记录验证失败的错误信息
                context.NoResult();
                context.Response.Headers.Add("Token-Error", "Invalid Token");
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // 记录挑战失败的错误信息
                Console.WriteLine($"Authentication challenge: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddLogging(options => options.AddConsole());

builder.Services.AddEndpointsApiExplorer(); //添加API探索器

builder.Services.AddSwaggerGen(c =>
{
    // 允许Swagger在请求头中传递自定义头部信息
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // 添加自定义头部
    c.OperationFilter<AddCustomHeaderOperationFilter>();
});


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//配置应用程序以提供静态文件并启用默认文件映射
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

BrainStormDatasEndpoints.RegisterAppDatasEndpoints(app); //注册用户数据的API

app.Run();


