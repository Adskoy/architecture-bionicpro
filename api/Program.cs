using System.Security.Claims;
using Bionicpro;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var authOpts = new KeycloakOptions(string.Empty, string.Empty, string.Empty);
builder.Configuration.Bind("Identity", authOpts);

builder.Services
  .AddCors()
  .AddTransient<IClaimsTransformation, ClaimsTransformer>()
  .AddHttpContextAccessor()
  .AddAuthorization(b => {
    b.AddPolicy("user_only", policy => policy.RequireRole("prothetic_user"));
  })
  .AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  })
  .AddJwtBearer(options => {
    options.Authority = authOpts.Authority;
    options.MetadataAddress = $"{authOpts.Authority}/.well-known/openid-configuration";
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new() {
      // это в дев среде работать не будет
      ValidateIssuer = false,
      ValidateAudience = false,
      //это уже нужное
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = authOpts.Authority
    };
  });

var app = builder.Build();

app.UseCors(x => x
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
  )
  .UseAuthentication()
  .UseAuthorization();

app
  .MapGet("/reports", (IHttpContextAccessor httpContextAccessor) => {
    return Results.Ok(new {
      UserId = httpContextAccessor.HttpContext!.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value,
      UserName = httpContextAccessor.HttpContext.User!.FindFirst("name")!.Value,
      CollectedData = new int [100].Select(_ => new MioHeartbeat(Guid.NewGuid(), DateTimeOffset.Now.AddHours(-1),
        "mio_sensor_42", new Random().NextDouble()))
    });
  })
    .RequireAuthorization("user_only");

app.Run();

record KeycloakOptions(string Authority, string ClientId, string ClientSecret);
record MioHeartbeat(Guid Id, DateTimeOffset Timestamp, string SensorKind, double SensorValue);