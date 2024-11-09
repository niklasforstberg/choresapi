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
using ChoresApi.Models.DTOs;

namespace ChoresApi.Endpoints;

public static class SecurityEndpoints
{
    public static void ConfigureEndpoints(this WebApplication app, IConfiguration configuration)
    {
#if DEBUG
        app.MapGet("/api/security/dev-token/{userId:int?}", [AllowAnonymous] async (int? userId, ChoresAppDbContext dbcontext) =>
        {
            var devUser = await dbcontext.Set<ChoreUser>()
                .Include(u => u.Family)
                .FirstOrDefaultAsync(u => u.Id == (userId ?? 1));

            if (devUser is null) return Results.NotFound($"No user found with ID {userId ?? 1}");

            var claims = new List<Claim>
            {
                new Claim("id", devUser.Id.ToString() ?? ""),
                new Claim("firstName", devUser.FirstName ?? ""),
                new Claim("lastName", devUser.LastName ?? ""),
                new Claim("role", "User"),
                new Claim("email", devUser.Email?.ToString() ?? ""),
                new Claim("familyId", devUser.FamilyId?.ToString() ?? ""),
                new Claim("familyName", devUser.Family?.Name ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    issuer: configuration["Jwt:Issuer"],
                    audience: configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new { token = jwt });
        });
#endif

        app.MapGet("/api/security/requirements", [AllowAnonymous] () =>
        {
            return Results.Ok(new
            {
                password = new
                {
                    minLength = 8,
                    requiresUppercase = false,
                    requiresLowercase = false,
                    requiresDigit = false,
                    requiresSpecialChar = false
                },
                email = new
                {
                    maxLength = 256
                }
            });
        });

        app.MapPost("/api/security/login",
        [AllowAnonymous] async (ChoresAppDbContext dbcontext, UserDto userdto) =>
        {
            // Add validation
            if (string.IsNullOrWhiteSpace(userdto.Email) ||
                !IsValidEmail(userdto.Email) ||
                string.IsNullOrWhiteSpace(userdto.Password) ||
                userdto.Password.Length < 8)
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(userdto.Email)) errors.Add("Email is required");
                if (!IsValidEmail(userdto.Email)) errors.Add("Invalid email format");
                if (string.IsNullOrWhiteSpace(userdto.Password)) errors.Add("Password is required");
                if (userdto.Password?.Length < 8) errors.Add("Password must be at least 8 characters");

                return Results.BadRequest(new { messages = errors });
            }

            var choresappUser = await dbcontext.Set<ChoreUser>()
                .Include(u => u.Family)
                .FirstOrDefaultAsync(user => user.Email == userdto.Email);

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

            return Results.Ok(new { token = token });

        });


        app.MapPost("/api/security/register", [AllowAnonymous] async (ChoresAppDbContext dbcontext, UserDto userdto) =>
        {
            // Add validation
            if (string.IsNullOrWhiteSpace(userdto.Email) ||
                !IsValidEmail(userdto.Email) ||
                string.IsNullOrWhiteSpace(userdto.Password) ||
                userdto.Password.Length < 8)
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(userdto.Email)) errors.Add("Email is required");
                if (!IsValidEmail(userdto.Email)) errors.Add("Invalid email format");
                if (string.IsNullOrWhiteSpace(userdto.Password)) errors.Add("Password is required");
                if (userdto.Password?.Length < 8) errors.Add("Password must be at least 8 characters");

                return Results.BadRequest(new { messages = errors });
            }

            // Check if user already exists
            var existingUser = await dbcontext.Set<ChoreUser>().FirstOrDefaultAsync(u => u.Email == userdto.Email);
            if (existingUser != null)
            {
                return Results.Conflict(new { message = $"User with email {userdto.Email} already exists" });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(userdto.Password);

            ChoreUser choresappUser = new ChoreUser
            {
                Email = userdto.Email,
                PasswordHash = passwordHash,
                FirstName = userdto.FirstName,
                LastName = userdto.LastName,
                Role = userdto.Role,
                FamilyId = userdto.FamilyId,
                PhoneNumber = userdto.PhoneNumber,
                Address = userdto.Address,
                City = userdto.City,
                State = userdto.State,
                ZipCode = userdto.ZipCode,
                Country = userdto.Country
            };

            dbcontext.Add<ChoreUser>(choresappUser);

            try
            {
                await dbcontext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // In case of a race condition where the email was added between our check and save
                return Results.Conflict(new { message = $"User with email {userdto.Email} already exists" });
            }

            string token = CreateToken(choresappUser);

            return Results.Ok(new { token = token });
        });

        string CreateToken(ChoreUser user)
        {
            var claims = new List<Claim>
            {
                new Claim("id", user.Id.ToString() ?? ""),
                new Claim("firstName", user.FirstName ?? ""),
                new Claim("lastName", user.LastName ?? ""),
                new Claim("role", "User"),
                new Claim("email", user.Email?.ToString() ?? ""),
                new Claim("familyId", user.FamilyId?.ToString() ?? ""),
                new Claim("familyName", user.Family?.Name ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    issuer: configuration["Jwt:Issuer"],
                    audience: configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

