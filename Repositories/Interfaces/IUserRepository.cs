using System.Collections.Generic;
using CtrlEat.Models;

namespace CtrlEat.Repositories.Interfaces
{
    public interface IUserRepository
    {
        User GetById(int id);
        User GetByEmail(string email);
        List<User> GetAll();
        int Create(User user);
        void Update(User user);
        void Delete(int id);
    }
}
