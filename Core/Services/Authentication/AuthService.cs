using Jira_Time_Manager.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Jira_Time_Manager.Core.Services.Authentication
{
    public class AuthService
    {
        private readonly IDbContextFactory<JiraTimeManagerDbContext> _dbFactory;
        private readonly IConfiguration _config;

        public AuthService(IDbContextFactory<JiraTimeManagerDbContext> dbFactory, IConfiguration config)
        {
            _dbFactory = dbFactory;
            _config = config;
        }

        public async Task<string?> LoginAsync(string staffNo)
        {
            using var context = await _dbFactory.CreateDbContextAsync();


            var employee = await context.Employees.FirstOrDefaultAsync(e => e.StaffNo == staffNo);
            if (employee == null) return null;


            bool isManager = await context.Managers.AnyAsync(m => m.EmployeeId == employee.EmployeeId);


            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
            new Claim(ClaimTypes.Name, employee.FirstName),
            new Claim("StaffNo", employee.StaffNo),
            new Claim(ClaimTypes.Role, isManager ? "Manager" : "Employee")
        };


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["JwtSettings:ExpiryInMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
