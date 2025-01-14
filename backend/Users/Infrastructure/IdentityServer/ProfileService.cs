﻿using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace MediaLakeUsers.Infrastructure.IdentityServer
{
    public class ProfileService : IProfileService
    {
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.IssuedClaims.AddRange(context.Subject.Claims.Where(x => x.Type == CustomClaimTypes.Roles).ToList());
            context.IssuedClaims.Add(context.Subject.Claims.Single(x => x.Type == CustomClaimTypes.Email));
            context.IssuedClaims.Add(context.Subject.Claims.Single(x => x.Type == CustomClaimTypes.Name));
            context.IssuedClaims.Add(context.Subject.Claims.Single(x => x.Type == CustomClaimTypes.UserId));

            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            return Task.FromResult(context.IsActive);
        }
    }
}
