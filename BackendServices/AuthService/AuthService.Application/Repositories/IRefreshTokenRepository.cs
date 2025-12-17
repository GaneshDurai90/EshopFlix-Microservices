using AuthService.Domain.Entities;

namespace AuthService.Application.Repositories
{
    public interface IRefreshTokenRepository
    {
        RefreshToken Save(RefreshToken token);
        RefreshToken? GetByToken(string token);
        void Revoke(RefreshToken token, string? replacedByToken = null);
    }
}
