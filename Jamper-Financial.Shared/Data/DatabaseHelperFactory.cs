using Jamper_Financial.Shared.Models;
using System.Collections.Generic;

namespace Jamper_Financial.Shared.Data
{
    public class DatabaseHelperFactory
    {
        public List<Goal> GetGoals()
        {
            return DatabaseHelper.GetGoals();
        }

        public void InsertGoal(Goal goal)
        {
            DatabaseHelper.InsertGoal(goal);
        }

        public void DeleteGoal(int goalId)
        {
            DatabaseHelper.DeleteGoal(goalId);
        }
    }
}
