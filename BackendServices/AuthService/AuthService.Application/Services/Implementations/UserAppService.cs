using AuthService.Application.DTO;
using AuthService.Application.Repositories;
using AuthService.Application.Services.Abstractions;
using AuthService.Domain.Entities;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BC = BCrypt.Net.BCrypt;

namespace AuthService.Application.Services.Implementations
{
    public class UserAppService : IUserAppService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UserAppService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _mapper = mapper;
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(UserDTO user)
        {
            var (credentials, keyId) = ResolveSigningCredentials();
            int expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"]);
            var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

            var claims = BuildClaims(user);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            if (!string.IsNullOrWhiteSpace(keyId))
            {
                token.Header[JwtHeaderParameterNames.Kid] = keyId;
            }

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        private (SigningCredentials Credentials, string KeyId) ResolveSigningCredentials()
        {
            var keyId = _configuration["Jwt:ActiveKeyId"];
            string? rawKey = null;

            if (!string.IsNullOrWhiteSpace(keyId))
            {
                rawKey = _configuration[$"Jwt:Keys:{keyId}"];
            }

            if (string.IsNullOrWhiteSpace(rawKey))
            {
                rawKey = _configuration["Jwt:Key"];
                keyId ??= "legacy";
            }

            if (string.IsNullOrWhiteSpace(rawKey))
            {
                throw new InvalidOperationException("Jwt signing key is not configured.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            return (credentials, keyId);
        }

        private IEnumerable<Claim> BuildClaims(UserDTO user)
        {
            yield return new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString());
            yield return new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty);
            yield return new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString());
            yield return new Claim(ClaimTypes.Name, user.Name ?? string.Empty);
            yield return new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());

            var roles = user.Roles ?? Array.Empty<string>();
            if (roles.Length > 0)
            {
                yield return new Claim("roles", string.Join(",", roles));
            }

            foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                yield return new Claim(ClaimTypes.Role, role);
            }

            var scopes = ResolveScopes(roles);
            foreach (var scope in scopes)
            {
                yield return new Claim("scope", scope);
            }
        }

        private static IEnumerable<string> ResolveScopes(IEnumerable<string> roles)
        {
            var scopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "catalog.read"
            };

            foreach (var role in roles)
            {
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    scopes.UnionWith(new[]
                    {
                        "cart.read",
                        "cart.write",
                        "order.read",
                        "order.write",
                        "product.admin",
                        "payment.execute",
                        "shipping.read",
                        "shipping.write",
                        "admin.portal"
                    });
                }
                else if (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
                {
                    scopes.UnionWith(new[] { "cart.read", "cart.write", "order.read", "payment.execute", "shipping.read" });
                }
            }

            return scopes;
        }

        private RefreshToken BuildRefreshToken(int userId)
        {
            var refreshExpireValue = _configuration["Jwt:RefreshExpireDays"];
            var refreshExpireDays = string.IsNullOrWhiteSpace(refreshExpireValue)
                ? 14
                : Convert.ToInt32(refreshExpireValue);
            var tokenBytes = RandomNumberGenerator.GetBytes(64);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(tokenBytes),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshExpireDays)
            };
        }

        public IEnumerable<UserDTO> GetAllUsers()
        {
            var users = _userRepository.GetAll();
            if (users != null)
            {
                return _mapper.Map<IEnumerable<UserDTO>>(users);
            }

            return Enumerable.Empty<UserDTO>();
        }

        public UserDTO LoginUser(LoginDTO loginDTO)
        {
            var user = _userRepository.GetUserByEmail(loginDTO.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var isValidPassword = BC.Verify(loginDTO.Password, user.Password);
            if (!isValidPassword)
            {
                throw new UnauthorizedAccessException("Invalid password");
            }

            var userDto = _mapper.Map<UserDTO>(user);
            var (accessToken, accessExpiresAt) = GenerateJwtToken(userDto);
            var refreshToken = BuildRefreshToken(user.Id);
            _refreshTokenRepository.Save(refreshToken);

            userDto.Token = accessToken;
            userDto.AccessTokenExpiresAt = accessExpiresAt;
            userDto.RefreshToken = refreshToken.Token;
            userDto.RefreshTokenExpiresAt = refreshToken.ExpiresAt;
            return userDto;
        }

        public TokenResponseDTO RefreshToken(RefreshTokenRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new UnauthorizedAccessException("Refresh token is required.");
            }

            var storedToken = _refreshTokenRepository.GetByToken(request.RefreshToken);
            if (storedToken == null || !storedToken.IsActive)
            {
                throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
            }

            var user = storedToken.User ?? _userRepository.GetUserById(storedToken.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found for token.");
            }

            var userDto = _mapper.Map<UserDTO>(user);
            var (accessToken, accessExpiresAt) = GenerateJwtToken(userDto);
            var newRefreshToken = BuildRefreshToken(user.Id);

            _refreshTokenRepository.Revoke(storedToken, newRefreshToken.Token);
            _refreshTokenRepository.Save(newRefreshToken);

            return new TokenResponseDTO
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExpiresAt,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
            };
        }

        public Task RevokeRefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Task.CompletedTask;
            }

            var token = _refreshTokenRepository.GetByToken(request.RefreshToken);
            if (token == null)
            {
                return Task.CompletedTask;
            }

            _refreshTokenRepository.Revoke(token);
            return Task.CompletedTask;
        }

        public bool SignUpUser(SignUpDTO signUpDTO, string role)
        {
            var user = _userRepository.GetUserByEmail(signUpDTO.Email);
            if (user != null)
            {
                throw new InvalidOperationException("User already exists.");
            }

            user = _mapper.Map<User>(signUpDTO);
            user.Password = BC.HashPassword(signUpDTO.Password);
            return _userRepository.RegisterUser(user, role);
        }
    }
}
