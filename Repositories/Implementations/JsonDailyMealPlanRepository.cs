using System;
using System.Collections.Generic;
using System.Linq;
using CtrlEat.Models;
using CtrlEat.Repositories.Interfaces;

namespace CtrlEat.Repositories.Implementations
{
    public class JsonDailyMealPlanRepository : IDailyMealPlanRepository
    {
        private readonly JsonFileHelper _fileHelper;
        private int _nextId = 1;

        public JsonDailyMealPlanRepository(string filePath = "Data/dailyMealPlans.json")
        {
            _fileHelper = new JsonFileHelper(filePath);
            InitializeNextId();
        }

        private void InitializeNextId()
        {
            var list = _fileHelper.ReadData<DailyMealPlan>();
            if (list.Count > 0)
                _nextId = list.Max(p => p.Id) + 1;
        }

        public int Create(DailyMealPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var list = _fileHelper.ReadData<DailyMealPlan>();
            plan.Id = _nextId++;
            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;
            list.Add(plan);
            _fileHelper.WriteData(list);
            return plan.Id;
        }

        public void Delete(int id)
        {
            var list = _fileHelper.ReadData<DailyMealPlan>();
            var existing = list.FirstOrDefault(p => p.Id == id);
            if (existing == null) throw new InvalidOperationException($"DailyMealPlan with id {id} not found");
            list.Remove(existing);
            _fileHelper.WriteData(list);
        }

        public List<DailyMealPlan> GetAll()
        {
            return _fileHelper.ReadData<DailyMealPlan>();
        }

        public DailyMealPlan GetById(int id)
        {
            var list = _fileHelper.ReadData<DailyMealPlan>();
            return list.FirstOrDefault(p => p.Id == id);
        }

        public List<DailyMealPlan> GetByUserId(int userId)
        {
            var list = _fileHelper.ReadData<DailyMealPlan>();
            return list.Where(p => p.UserId == userId).ToList();
        }

        public void Update(DailyMealPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var list = _fileHelper.ReadData<DailyMealPlan>();
            var existing = list.FirstOrDefault(p => p.Id == plan.Id);
            if (existing == null) throw new InvalidOperationException($"DailyMealPlan with id {plan.Id} not found");

            existing.BreakfastRecipeId = plan.BreakfastRecipeId;
            existing.LunchRecipeId = plan.LunchRecipeId;
            existing.DinnerRecipeId = plan.DinnerRecipeId;
            existing.Snacks = plan.Snacks;
            existing.Date = plan.Date;
            existing.TotalCalories = plan.TotalCalories;
            existing.UpdatedAt = DateTime.UtcNow;

            _fileHelper.WriteData(list);
        }
    }
}
