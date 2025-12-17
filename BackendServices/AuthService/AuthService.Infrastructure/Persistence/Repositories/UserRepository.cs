using AuthService.Application.Repositories;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        AuthServiceDbContext _db;
        public UserRepository(AuthServiceDbContext db)
        {
            _db = db;
        }
        public IEnumerable<User> GetAll()
        {
            return _db.Users.ToList();
        }

        public User GetUserByEmail(string email)
        {
            return _db.Users.Include(u => u.Roles).FirstOrDefault(u => u.Email == email);
        }

        public User GetUserById(int id)
        {
            return _db.Users.Include(u => u.Roles).FirstOrDefault(u => u.Id == id);
        }

        public bool RegisterUser(User user, string role)
        {
            Role existingRole = _db.Roles.FirstOrDefault(r => r.Name == role);
            if (existingRole == null)
            {
                return false; // Role does not exist
            }

            user.Roles.Add(existingRole);
            _db.Users.Add(user);
            _db.SaveChanges();
            return true; // User registered successfully
        }
    }
}
