﻿using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace PostApiService.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AuthRepository(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public Task<IdentityResult> AddClaimsAsync(IdentityUser user, Claim claim)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckPasswordAsync(IdentityUser user, string password)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityUser> CreateAsync(IdentityUser user, string password)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityUser> FindByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<IdentityUser> FindByNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public Task<IList<Claim>> GetClaimsAsync(IdentityUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IList<string>> GetRolesAsync(IdentityUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityUser> GetUserAsync(ClaimsPrincipal principal)
        {
            throw new NotImplementedException();
        }
    }
}
