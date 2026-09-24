using System.Collections.Generic;
using CtrlEat.Models;

namespace CtrlEat.Repositories.Interfaces
{
    public interface IDailyMealPlanRepository
    {
        DailyMealPlan GetById(int id);
        List<DailyMealPlan> GetAll();
        List<DailyMealPlan> GetByUserId(int userId);
        int Create(DailyMealPlan plan);
        void Update(DailyMealPlan plan);
        void Delete(int id);
    }
}
