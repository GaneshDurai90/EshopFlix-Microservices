using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Repositories
{
    public interface IUserRepository
    {
        bool RegisterUser(User user, string role);
        IEnumerable<User> GetAll();
        User GetUserByEmail(string email);
        User GetUserById(int id);
    }
}
