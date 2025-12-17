using AuthService.Application.DTO;
using AuthService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        IUserAppService _userAppService;
        public AuthController(IUserAppService userAppService)
        {
            _userAppService = userAppService;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginDTO loginDTO)

        {
            try
            {
                UserDTO user = _userAppService.LoginUser(loginDTO);
                if (user == null)
                {
                    return Unauthorized("Invalid email or password");
                }
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult Refresh([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                var tokenResponse = _userAppService.RefreshToken(request);
                return Ok(tokenResponse);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                await _userAppService.RevokeRefreshTokenAsync(request);
                return Ok();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult SignUp([FromBody] SignUpDTO signUpDTO, string role)
        {
            bool isSuccess = _userAppService.SignUpUser(signUpDTO, role);
            if (isSuccess)
            {
                return Ok("User registered successfully");
            }
            return BadRequest("User registration failed");
        }
    }
}
