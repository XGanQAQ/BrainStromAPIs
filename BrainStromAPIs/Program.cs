using BrainStromAPIs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration; //��ȡ������Ϣ

//builder.Services.AddDbContext<BrainStormDbContext>(options => options.UseInMemoryDatabase("AppData")); //��ʱ��ʹ���ڴ����ݿ�������

//ע�����
builder.Services.AddDbContext<BrainStormDbContext>(options =>
    options.UseSqlServer(configuration["DataBaseSet:ConnectionString"])
);
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ʹ�öԳ���Կ������ HS256������֤ JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ����ǩ����Կ
            IssuerSigningKey = key,
            ValidateIssuer = false,  // �������֤�����ߣ���������Ϊ false
            ValidateAudience = false,  // �������֤���ڣ���������Ϊ false
            ValidateLifetime = false,  // ��֤���ƹ���ʱ��
            ClockSkew = TimeSpan.Zero  // ����ʱ�������ʱ��ƫ��
        };

        // ��Ӵ�����ص�
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // ��¼��֤ʧ�ܵĴ�����Ϣ
                context.NoResult();
                context.Response.Headers.Add("Token-Error", "Invalid Token");
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // ��¼��սʧ�ܵĴ�����Ϣ
                Console.WriteLine($"Authentication challenge: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddLogging(options => options.AddConsole());

builder.Services.AddEndpointsApiExplorer(); //���API̽����

builder.Services.AddSwaggerGen(c =>
{
    // ����Swagger������ͷ�д����Զ���ͷ����Ϣ
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

    // ����Զ���ͷ��
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

//����Ӧ�ó������ṩ��̬�ļ�������Ĭ���ļ�ӳ��
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

BrainStormDatasEndpoints.RegisterAppDatasEndpoints(app); //ע���û����ݵ�API

app.Run();


