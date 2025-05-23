﻿using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public class UserStateService
    {
        public int UserId { get; private set; } = 0;
        public string Username { get; set; } = string.Empty;
        public LoggedInUser LoggedInUser { get; private set; } = new LoggedInUser();


        public void SetUser(int userId, string username, string email)
        {
            UserId = userId;
            Username = username;
            LoggedInUser.UserName = username;
            LoggedInUser.Email = email;
            LoggedInUser.UserID = userId;
        }

        public void ClearUser()
        {
            UserId = 0;
            Username = string.Empty;
            LoggedInUser.UserName = string.Empty;
            LoggedInUser.Email = string.Empty;
        }

    }
}