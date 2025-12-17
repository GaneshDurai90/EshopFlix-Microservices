using System;
using System.Linq;
using AuthService.Application.Repositories;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AuthServiceDbContext _dbContext;

        public RefreshTokenRepository(AuthServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public RefreshToken Save(RefreshToken token)
        {
            _dbContext.RefreshTokens.Add(token);
            _dbContext.SaveChanges();
            return token;
        }

        public RefreshToken? GetByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return _dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefault(t => t.Token == token);
        }

        public void Revoke(RefreshToken token, string? replacedByToken = null)
        {
            if (token == null)
            {
                return;
            }

            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByToken = replacedByToken;
            _dbContext.SaveChanges();
        }
    }
}
