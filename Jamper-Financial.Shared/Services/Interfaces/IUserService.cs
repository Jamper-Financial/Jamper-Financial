﻿using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<UserProfile> GetUserProfileByIdAsync(int userId);
        Task<bool> UpdateUserProfileAsync(UserProfile userProfile);
        Task<bool> DeleteUserProfileAsync(int userId);
    }
}