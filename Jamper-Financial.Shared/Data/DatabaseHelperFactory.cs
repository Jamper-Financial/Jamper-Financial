using Jamper_Financial.Shared.Models;
using System.Collections.Generic;

namespace Jamper_Financial.Shared.Data
{
    public class DatabaseHelperFactory
    {
        public List<Goal> GetGoals(int userid)
        {
            return DatabaseHelper.GetGoals(userid);
        }

        public void InsertGoal(Goal goal)
        {
            DatabaseHelper.InsertGoal(goal);
        }

        public void DeleteGoal(int goalId)
        {
            DatabaseHelper.DeleteGoal(goalId);
        }
        public void UpdateGoal(Goal goal) // ADDED
        {
            DatabaseHelper.UpdateGoal(goal);
        }
    }
}
