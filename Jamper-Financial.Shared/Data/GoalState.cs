// File: Jamper-Financial.Shared/Data/GoalState.cs
using System.Collections.Generic;

namespace Jamper_Financial.Shared.Data
{
    public class GoalState
    {
        public List<Goal> Goals { get; set; } = new List<Goal>();

        public void AddGoal(Goal goal)
        {
            Goals.Add(goal);
        }
        public void RemoveGoal(Goal goal)
        {
            Goals.Remove(goal);
        }
    }
    public class Goal
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string AccountType { get; set; }
        public bool IsQuickGoal { get; set; }
        public bool IsRetirementGoal { get; set; }
        public bool IsEmergencyFundGoal { get; set; }
        public bool IsTravelGoal { get; set; }
        public bool IsHomeGoal { get; set; }
        public string Category { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; }
        public bool ShowDescription { get; set; }
        public string Frequency { get; set; }
        public bool IsFadingOut { get; set; }
    }
}
