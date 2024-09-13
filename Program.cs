using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ChoresApp.Helpers;
using ChoresApp.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ChoresAppDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("SqliteConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
    app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "The family chores API");
            c.InjectStylesheet("/swagger/custom.css");
            c.RoutePrefix = String.Empty;
        });
}

app.UseHttpsRedirection();

// Add user endpoints
app.MapUserEndpoints();
app.MapFamilyEndpoints();

app.Run();
