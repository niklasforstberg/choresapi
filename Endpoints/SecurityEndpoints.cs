using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Azure.Core;
using ChoresApp.Models;
using ChoresApp.Helpers;
using ChoresApi.Models.Dtos;

namespace ChoresApi.Endpoints;

public static class SecurityEndpoints
{
    public static void ConfigureEndpoints(this WebApplication app, IConfiguration configuration)
    {

        app.MapPost("/api/security/login",
        [AllowAnonymous] async (ChoresAppDbContext dbcontext, UserDto userdto) =>
        {
            var choresappUser = await dbcontext.Set<ChoreUser>().FirstOrDefaultAsync(user => user.Email == userdto.Email);

            if (choresappUser is null)
            {
                Console.WriteLine("Could not find user " + userdto.Email);
                return Results.Unauthorized();
            }

            if (!BCrypt.Net.BCrypt.Verify(userdto.Password, choresappUser.PasswordHash))
            {
                return Results.Unauthorized();
            }

            string token = CreateToken(choresappUser);

            return Results.Ok(token);

        });


        app.MapPost("/api/security/register", [AllowAnonymous] async (ChoresAppDbContext dbcontext, UserDto userdto) =>
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(userdto.Password);

            ChoreUser choresappUser = new ChoreUser
            {
                Username = userdto.Username,
                Email = userdto.Email,
                PasswordHash = passwordHash
            };

            dbcontext.Add<ChoreUser>(choresappUser);
            await dbcontext.SaveChangesAsync();

            return Results.Ok("User " + userdto.Username + " has been registered");
        });

        string CreateToken(ChoreUser user)
        {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.GivenName, user.FirstName!),
                new Claim(ClaimTypes.Surname, user.LastName!), 
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Sid, user.Id.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}

