using BlazorBootstrap;
using System.ComponentModel.DataAnnotations;

namespace Jamper_Financial.Shared.Models
{
    public class UserSettings
    {
        public int UserId { get; set; }
        public string? Currency { get; set; }
        public string? TimeZone { get; set; }
        public bool EnableSubscriptionNotification { get; set; }
        public bool EnableBudgetNotification { get; set; }
        public bool EnableGoalsNotification { get; set; }
    }
}
