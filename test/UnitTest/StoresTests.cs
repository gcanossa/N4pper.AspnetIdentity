using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;
using N4pper;
using N4pper.AspnetIdentity;
using N4pper.AspnetIdentity.Model;
using System;
using System.Linq.Expressions;
using Xunit;

namespace UnitTest
{
    [TestCaseOrderer(AsIKnow.XUnitExtensions.Constants.PriorityOrdererTypeName, AsIKnow.XUnitExtensions.Constants.PriorityOrdererTypeAssemblyName)]
    [Collection(nameof(Neo4jCollection))]
    public class StoresTests : IdentitySpecificationTestBase<IdentityUser, IdentityRole>
    {
        protected Neo4jFixture Fixture { get; set; }

        public StoresTests(Neo4jFixture fixture)
        {
            Fixture = fixture;
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddTransient<IUserStore<IdentityUser>>(provider => Fixture.GetService<IUserStore<IdentityUser>>());//(provider=>new UserStore<IdentityUser>(context as DriverProvider));
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddTransient<IRoleStore<IdentityRole>>(provider => Fixture.GetService<IRoleStore<IdentityRole>>());//(provider => new RoleStore<IdentityRole>(context as DriverProvider));
        }

        protected override object CreateTestContext()
        {
            return Fixture.GetService<DriverProvider>();
        }

        protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            IdentityRole role = new IdentityRole()
            {
                Name = useRoleNamePrefixAsRoleName ? roleNamePrefix : roleNamePrefix + Guid.NewGuid().ToString("N")
            };
            role.NormalizedName = role.Name.ToUpper();

            return role;
        }

        protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "", bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false)
        {
            IdentityUser user = new IdentityUser()
            {
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd,
                UserName = useNamePrefixAsUserName ? namePrefix : namePrefix + Guid.NewGuid().ToString("N")
            };

            user.NormalizedEmail = user.Email.ToUpper();
            user.NormalizedUserName = user.UserName.ToUpper();

            return user;
        }

        protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName)
        {
            return role => role.Name == roleName;
        }

        protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName)
        {
            return role => role.Name.StartsWith(roleName);
        }

        protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName)
        {
            return user => user.UserName == userName;
        }

        protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName)
        {
            return user => user.UserName.StartsWith(userName);
        }
    }
}
