using System.Security.Claims;
using System.Text;
using BlogManagement.Api.Middleware;
using BlogManagement.Api.Security;
using BlogManagement.Api.Uploads;
using BlogManagement.Api.Localization;
using Microsoft.AspNetCore.Mvc;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using BlogManagement.Infrastructure.Auth;
using BlogManagement.Infrastructure.Persistence;
using BlogManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables().AddCommandLine(args);
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddDbContext<BlogManagementDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ImageStorage>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "username",
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length == 0) policy.AllowAnyOrigin();
    else policy.WithOrigins(allowedOrigins);
    policy.AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en").AddSupportedCultures("en", "ar").AddSupportedUICultures("en", "ar");
    options.ApplyCurrentCultureToResponseHeaders = true;
});
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ValidationProblemDetails(
        context.ModelState.Where(entry => entry.Value?.Errors.Count > 0).ToDictionary(
            entry => entry.Key,
            entry => entry.Value!.Errors.Select(error => ApiMessages.Validation(entry.Key,
                string.IsNullOrEmpty(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage)).ToArray()))
    {
        Status = 400, Title = ApiMessages.Translate("Validation failed"), Instance = context.HttpContext.Request.Path
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Blog Management API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(builder.Environment.WebRootPath, "uploads"));
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(builder.Environment.WebRootPath);
var app = builder.Build();

app.UseRequestLocalization();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode is 401 or 403)
        await response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = response.StatusCode,
            Title = ApiMessages.Translate(response.StatusCode == 401 ? "Unauthorized" : "Forbidden"),
            Detail = ApiMessages.Translate(response.StatusCode == 401 ? "Please sign in to continue." : "You do not have permission to perform this action.")
        });
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

if (builder.Configuration.GetValue<bool>("Database:EnsureCreatedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BlogManagementDbContext>();
    await db.Database.EnsureCreatedAsync();
    await ImageSchemaUpgrade.ApplyAsync(db);
    await DataSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:SeedDemoData"))
{
    using var scope = app.Services.CreateScope();
    await DemoDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<BlogManagementDbContext>(),
        builder.Configuration["Database:DemoPassword"] ?? DemoDataSeeder.DefaultPassword);
}

app.Run();

public partial class Program { }
