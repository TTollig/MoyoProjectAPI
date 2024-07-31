using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MoyoProjectAPI.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MoyoProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    return BadRequest("Invalid role specified.");
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                return Ok(new { Result = "User registered successfully" });
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var token = await GenerateJwtToken(user);
                return Ok(new { Token = token });
            }
            return Unauthorized();
        }

        [HttpGet("github-login")]
        public IActionResult GitHubLogin()
        {
            var redirectUrl = Url.Action("GitHubCallback", "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("GitHub", redirectUrl);
            return Challenge(properties, "GitHub");
        }

        [HttpGet("github-callback")]
        public async Task<IActionResult> GitHubCallback()
        {
            var returnUrl = "http://localhost:4200/login";
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return BadRequest("Error loading external login information.");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                return BadRequest("Email claim not received from external provider.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email };
                var createUserResult = await _userManager.CreateAsync(user);
                if (!createUserResult.Succeeded)
                {
                    return BadRequest($"User creation failed: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
                }

          
                if (!await _roleManager.RoleExistsAsync("Capturer"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Capturer"));
                    if (!roleResult.Succeeded)
                    {
                        return BadRequest($"Role creation failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }

             
                var addToRoleResult = await _userManager.AddToRoleAsync(user, "Capturer");
                
                if (!addToRoleResult.Succeeded)
                {
                    return BadRequest($"Adding role to user failed: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
                }

                await _userManager.AddLoginAsync(user, info);
            }
            else
            {
               
                var existingLogins = await _userManager.GetLoginsAsync(user);
                if (existingLogins.All(x => x.LoginProvider != info.LoginProvider || x.ProviderKey != info.ProviderKey))
                {
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                    {
                        return BadRequest($"Adding external login failed: {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                    }
                }

                
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Capturer"))
                {
                    var addToRoleResult = await _userManager.AddToRoleAsync(user, "Capturer");
                    if (!addToRoleResult.Succeeded)
                    {
                        return BadRequest($"Adding role to user failed: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var token = await GenerateJwtToken(user);
            return Redirect($"{returnUrl}?token={token}");
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

public class RegisterModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}
