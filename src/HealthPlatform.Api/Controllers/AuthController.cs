using HealthPlatform.Api.Contracts.Auth;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager, IJwtTokenService jwt) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await userManager.FindByEmailAsync(request.Email.Trim());
        if (usuario is null || !usuario.Ativo)
            return Unauthorized(new { message = "Email ou senha invalidos." });

        var result = await signInManager.CheckPasswordSignInAsync(usuario, request.Senha, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Email ou senha invalidos." });

        var roles = await userManager.GetRolesAsync(usuario);
        var token = jwt.Create(usuario, roles.ToArray());

        return Ok(new LoginResponse(token.Token, token.ExpiresAtUtc, usuario.Nome, usuario.TipoUsuario.ToString()));
    }
}
