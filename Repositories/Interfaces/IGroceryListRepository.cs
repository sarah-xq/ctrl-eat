using System;
using System.Collections.Generic;
using CtrlEat.Models;

namespace CtrlEat.Repositories.Interfaces
{
    public interface IGroceryListRepository
    {
        GroceryList GetById(int id);
        List<GroceryList> GetAll();
        List<GroceryList> GetByDate(DateTime date);
        int Create(GroceryList groceryList);
        void Update(GroceryList groceryList);
        void Delete(int id);
    }
}
