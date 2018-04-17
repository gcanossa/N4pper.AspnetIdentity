using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;
using N4pper.AspnetIdentity.Model;
using System;
using System.Linq.Expressions;
using Xunit;

namespace UnitTest
{
    public class StoresTests : IdentitySpecificationTestBase<IdentityUser, IdentityRole>
    {
        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            throw new NotImplementedException();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            throw new NotImplementedException();
        }

        protected override object CreateTestContext()
        {
            throw new NotImplementedException();
        }

        protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            throw new NotImplementedException();
        }

        protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "", bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false)
        {
            throw new NotImplementedException();
        }

        protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName)
        {
            throw new NotImplementedException();
        }

        protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName)
        {
            throw new NotImplementedException();
        }

        protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
        {
            throw new NotImplementedException();
        }

        protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName)
        {
            throw new NotImplementedException();
        }

        protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName)
        {
            throw new NotImplementedException();
        }
    }
}
