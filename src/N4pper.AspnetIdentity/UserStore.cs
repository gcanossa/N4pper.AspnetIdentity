using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using N4pper.AspnetIdentity.Model;
using N4pper.QueryUtils;
using Neo4j.Driver.V1;
using OMnG;

namespace N4pper.AspnetIdentity
{/// <summary>
 /// Represents a new instance of a persistence store for users, using the default implementation
 /// of <see cref="IdentityUser{TKey}"/> with a string as a primary key.
 /// </summary>
    public class UserStore : UserStore<IdentityUser<string>>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(IdentityDriverProvider context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Creates a new instance of a persistence store for the specified user type.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    public class UserStore<TUser> : UserStore<TUser, IdentityRole, IdentityDriverProvider, string>
        where TUser : IdentityUser<string>, new()
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser}"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(IdentityDriverProvider context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class UserStore<TUser, TRole, TContext> : UserStore<TUser, TRole, TContext, string>
        where TUser : IdentityUser<string>
        where TRole : IdentityRole<string>
        where TContext : IdentityDriverProvider
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext}"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }
    
    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserToken">The type representing a user token.</typeparam>
    public class UserStore<TUser, TRole, TContext, TKey> :
        UserStoreBase<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TContext : IdentityDriverProvider
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Creates a new instance of the store.
        /// </summary>
        /// <param name="context">The context used to access the store.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(describer ?? new IdentityErrorDescriber())
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            Context = context;
        }

        /// <summary>
        /// Gets the database context for this store.
        /// </summary>
        public TContext Context { get; private set; }
                
        /// <summary>
        /// Creates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        public async override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node("p", type: typeof(TUser));
                TUser tmp = await session.AsAsync(s=>s.ExecuteQuery<TUser>(
                    $"CREATE {n} " +
                    $"SET p+=$user, p.{nameof(user.EntityId)}=id(p), p :{typeof(TUser).Name} " +
                    $"RETURN p",
                    new { user = user.ExludeProperties(p => p.EntityId) }).FirstOrDefault(),
                    cancellationToken);
                if (tmp == null)
                    return IdentityResult.Failed();
                else
                    user.EntityId = tmp.EntityId;
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Updates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the update operation.</returns>
        public async override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                await session.RunAsync(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityUser.Id)}:$user.{nameof(IdentityUser.Id)},{nameof(IdentityUser.EntityId)}:$user.{nameof(IdentityUser.EntityId)}}}) " +
                    $"SET p+=$user",
                    new { user = user.ToPropDictionary() });
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Deletes the specified <paramref name="user"/> from the user store.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the update operation.</returns>
        public async override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                await session.RunAsync(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(IdentityUser.Id)},{nameof(IdentityUser.EntityId)}:${nameof(IdentityUser.EntityId)}}}) " +
                    $"DETACH DELETE p",
                    user.SelectProperties(p=> new { p.Id, p.EntityId }));
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="userId"/> if it exists.
        /// </returns>
        public override async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                return await session.AsAsync<TUser>(p=>
                p.ExecuteQuery<TUser>(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}}) " +
                    $"RETURN p",
                    new { userId }).FirstOrDefault(), cancellationToken);
            }
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified normalized user name.
        /// </summary>
        /// <param name="normalizedUserName">The normalized user name to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="normalizedUserName"/> if it exists.
        /// </returns>
        public override async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                return await session.AsAsync(p=>
                p.ExecuteQuery<TUser>(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityUser.NormalizedUserName)}:${nameof(normalizedUserName)}}}) " +
                    $"RETURN p",
                    new { normalizedUserName }).FirstOrDefault(), cancellationToken);
            }
        }

        /// <summary>
        /// A navigation property for the users the store contains.
        /// </summary>
        public override IQueryable<TUser> Users
        {
            get
            {
                using (ISession session = Context.GetDriver().Session())
                {
                    Node n = new Node(type: typeof(TUser));
                    return session.ExecuteQuery<TUser>(
                        $"MATCH (p{n.Labels}) " +
                        $"RETURN p").ToList().AsQueryable();
                }
            }
        }
        
        /// <summary>
        /// Adds the given <paramref name="normalizedRoleName"/> to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the role to.</param>
        /// <param name="normalizedRoleName">The role to add.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async override Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException("Vaue cannot be null or empty", nameof(normalizedRoleName));
            }
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node r = new Node(type: typeof(TRole));
                Rel rel = new Rel(type: typeof(Relationships.IsIn));

                TKey userId = user.Id;
                TKey roleId = session.ExecuteQuery<TRole>($"MATCH (r{r.Labels} {{{nameof(IdentityRole.NormalizedName)}:${nameof(normalizedRoleName)}}}) RETURN r",new { normalizedRoleName }).Select(p=>p.Id).FirstOrDefault();
                if (roleId == null)
                {
                    throw new InvalidOperationException(string.Format("Role '{0}' not found", normalizedRoleName));
                }

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}}) " +
                    $"MATCH (r{r.Labels} {{{nameof(IdentityRole.Id)}:${nameof(roleId)}}}) " +
                    $"CREATE (n)-{rel}->(r)",
                    new { userId, roleId });
            }
        }

        /// <summary>
        /// Removes the given <paramref name="normalizedRoleName"/> from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the role from.</param>
        /// <param name="normalizedRoleName">The role to remove.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async override Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException("Vaue cannot be null or empty", nameof(normalizedRoleName));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node r = new Node(type: typeof(TRole));

                TKey userId = user.Id;
                TKey roleId = session.ExecuteQuery<TRole>($"MATCH (r{r.Labels} {{{nameof(IdentityRole.NormalizedName)}:${nameof(normalizedRoleName)}}}) RETURN r", new { normalizedRoleName }).Select(p => p.Id).FirstOrDefault();
                if (roleId == null)
                {
                    throw new InvalidOperationException(string.Format("Role '{0}' not found", normalizedRoleName));
                }

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{new Rel("rel",type: typeof(Relationships.IsIn))}->" +
                    $"(r{r.Labels} {{{nameof(IdentityRole.Id)}:${nameof(roleId)}}}) " +
                    $"DELETE rel",
                    new { userId, roleId });
            }
        }

        /// <summary>
        /// Retrieves the roles the specified <paramref name="user"/> is a member of.
        /// </summary>
        /// <param name="user">The user whose roles should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the roles the user is a member of.</returns>
        public override async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node r = new Node(type: typeof(TRole));

                TKey userId = user.Id;

                return await session.AsAsync(s=>
                s.ExecuteQuery<TRole>(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{new Rel("rel", type: typeof(Relationships.IsIn))}->" +
                    $"(r{r.Labels}) " +
                    $"RETURN r",
                    new { userId }).Select(p => p.Name).ToList(), cancellationToken);
            }
        }

        /// <summary>
        /// Returns a flag indicating if the specified user is a member of the give <paramref name="normalizedRoleName"/>.
        /// </summary>
        /// <param name="user">The user whose role membership should be checked.</param>
        /// <param name="normalizedRoleName">The role to check membership of</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> containing a flag indicating if the specified user is a member of the given group. If the 
        /// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
        public override async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException("Vaue cannot be null or empty", nameof(normalizedRoleName));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node r = new Node(type: typeof(TRole));

                TKey userId = user.Id;

                return await session.AsAsync(s=>
                s.ExecuteQuery<TRole>(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{new Rel("rel", type: typeof(Relationships.IsIn))}->" +
                    $"(r{r.Labels} {{{nameof(IdentityRole.NormalizedName)}:${nameof(normalizedRoleName)}}}) " +
                    $"RETURN r",
                    new { userId, normalizedRoleName }).Count()>0, cancellationToken);
            }
        }

        /// <summary>
        /// Get the claims associated with the specified <paramref name="user"/> as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose claims should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the claims granted to a user.</returns>
        public async override Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node r = new Node(type: typeof(IdentityClaim));

                TKey userId = user.Id;

                return await session.AsAsync(s=>
                s.ExecuteQuery<IdentityClaim>(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{new Rel("rel", type: typeof(Relationships.Has))}->" +
                    $"(r{r.Labels}) " +
                    $"RETURN r",
                    new { userId }).ToList().Select(p => p.ToClaim()).ToList(), cancellationToken);
            }
        }

        /// <summary>
        /// Adds the <paramref name="claims"/> given to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the claim to.</param>
        /// <param name="claims">The claim to add to the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            List<IDictionary<string, object>> claimsList = claims
                .Select(p=> {
                    IdentityClaim iclaim = new IdentityClaim();
                    iclaim.InitializeFromClaim(p);
                    return iclaim;
                    }).Select(p => p.SelectProperties(c => new { c.ClaimValue, c.ClaimType })).ToList();
            if (claimsList.Count > 0)
            {
                using (ISession session = Context.GetDriver().Session())
                {
                    Node n = new Node(type: typeof(TUser));
                    Node c = new Node(type: typeof(IdentityClaim));
                    Rel rel = new Rel(type: typeof(Relationships.Has));

                    TKey userId = user.Id;

                    await session.RunAsync(
                        $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}}) " +
                        $"UNWIND ${nameof(claimsList)} as row " +
                        $"CREATE (n)-{rel}->(c{c.Labels})" +
                        $"SET c+=row, c.{nameof(IGraphEntity.EntityId)}=id(c), c :{typeof(IdentityClaim).Name}",
                        new { userId, claimsList });
                }
            }
        }

        /// <summary>
        /// Replaces the <paramref name="claim"/> on the specified <paramref name="user"/>, with the <paramref name="newClaim"/>.
        /// </summary>
        /// <param name="user">The user to replace the claim on.</param>
        /// <param name="claim">The claim replace.</param>
        /// <param name="newClaim">The new claim replacing the <paramref name="claim"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async override Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }


            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey userId = user.Id;
                string oldClaimValue = claim.Value;
                string oldClaimType = claim.Type;
                string newClaimValue = newClaim.Value;
                string newClaimType = newClaim.Type;

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{rel}->" +
                    $"(c{c.Labels} {{{nameof(IdentityClaim.ClaimValue)}:${nameof(oldClaimValue)},{nameof(IdentityClaim.ClaimType)}:${nameof(oldClaimType)}}}) " +
                    $"SET c.{nameof(IdentityClaim.ClaimValue)}=${nameof(newClaimValue)},c.{nameof(IdentityClaim.ClaimType)}=${nameof(newClaimType)}",
                    new { userId, oldClaimType, oldClaimValue, newClaimType, newClaimValue });
            }
        }

        /// <summary>
        /// Removes the <paramref name="claims"/> given from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the claims from.</param>
        /// <param name="claims">The claim to remove.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async override Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            List<IDictionary<string, object>> claimsList = claims.Select(p=>p.SelectProperties(c => new { c.Value, c.Type })).ToList();
            if (claimsList.Count > 0)
            {
                using (ISession session = Context.GetDriver().Session())
                {
                    Node n = new Node(type: typeof(TUser));
                    Node c = new Node(type: typeof(IdentityClaim));
                    Rel rel = new Rel(type: typeof(Relationships.Has));

                    TKey userId = user.Id;

                    await session.RunAsync(
                        $"UNWIND ${nameof(claimsList)} as row " +
                        $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                        $"-{rel}->" +
                        $"(c{c.Labels} {{{nameof(IdentityClaim.ClaimValue)}:row.{nameof(Claim.Value)},{nameof(IdentityClaim.ClaimType)}:row.{nameof(Claim.Type)}}})" +
                        $"DETACH DELETE c",
                        new { userId, claimsList });
                }
            }
        }

        /// <summary>
        /// Adds the <paramref name="login"/> given to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the login to.</param>
        /// <param name="login">The login to add to the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task AddLoginAsync(TUser user, UserLoginInfo login,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserLogin));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey userId = user.Id;

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}}) " +
                    $"CREATE (n)-{rel}->(c{c.Labels})" +
                    $"SET c+=${nameof(login)},c.{nameof(IGraphEntity.EntityId)}=id(c), c :{typeof(IdentityUserLogin).Name}",
                    new { userId, login = login.SelectProperties(typeof(UserLoginInfo)) });
            }
        }

        /// <summary>
        /// Removes the <paramref name="loginProvider"/> given from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the login from.</param>
        /// <param name="loginProvider">The login to remove from the user.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserLogin));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey userId = user.Id;

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{rel}->" +
                    $"(c{c.Labels} {{{nameof(IdentityUserLogin.LoginProvider)}:${nameof(loginProvider)},{nameof(IdentityUserLogin.ProviderKey)}:${nameof(providerKey)}}})" +
                    $"DETACH DELETE c",
                    new { userId, loginProvider, providerKey });
            }
        }

        /// <summary>
        /// Retrieves the associated logins for the specified <param ref="user"/>.
        /// </summary>
        /// <param name="user">The user whose associated logins to retrieve.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing a list of <see cref="UserLoginInfo"/> for the specified <paramref name="user"/>, if any.
        /// </returns>
        public async override Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserLogin));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey userId = user.Id;

                return await session.AsAsync(s=>
                s.ExecuteQuery<IdentityUserLogin>(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{rel}->" +
                    $"(c{c.Labels})" +
                    $"RETURN c",
                    new { userId }).ToList().Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList(),
                    cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves the user associated with the specified login provider and login provider key.
        /// </summary>
        /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey"/>.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
        /// </returns>
        public async override Task<TUser> FindByLoginAsync(string loginProvider, string providerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserLogin));
                Rel rel = new Rel(type: typeof(Relationships.Has));
                    
                return await session.AsAsync(s=>
                s.ExecuteQuery<TUser>(
                    $"MATCH (n{n.Labels})" +
                    $"-{rel}->" +
                    $"(c{c.Labels} {{{nameof(IdentityUserLogin.LoginProvider)}:${nameof(loginProvider)},{nameof(IdentityUserLogin.ProviderKey)}:${nameof(providerKey)}}})" +
                    $"RETURN n",
                    new { loginProvider, providerKey }).FirstOrDefault(),
                    cancellationToken);
            }
        }

        /// <summary>
        /// Gets the user, if any, associated with the specified, normalized email address.
        /// </summary>
        /// <param name="normalizedEmail">The normalized email address to return the user for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous lookup operation, the user if any associated with the specified normalized email address.
        /// </returns>
        public override async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserLogin));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                return await session.AsAsync(s=>
                s.ExecuteQuery<TUser>(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.NormalizedEmail)}:${nameof(normalizedEmail)}}})" +
                    $"RETURN n",
                    new { normalizedEmail }).FirstOrDefault(),
                    cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves all users with the specified claim.
        /// </summary>
        /// <param name="claim">The claim whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that contain the specified claim. 
        /// </returns>
        public async override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                return await session.AsAsync(s=>
                s.ExecuteQuery<TUser>(
                    $"MATCH (n{n.Labels})-{rel}-(c{c.Labels} {{{nameof(IdentityClaim.ClaimType)}:${nameof(claim.Type)},{nameof(IdentityClaim.ClaimValue)}:${nameof(claim.Value)}}})" +
                    $"RETURN n",
                    new { claim.Type, claim.Value }).ToList(),
                    cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves all users in the specified role.
        /// </summary>
        /// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that are in the specified role. 
        /// </returns>
        public async override Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(normalizedRoleName))
            {
                throw new ArgumentNullException(nameof(normalizedRoleName));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(TRole));
                Rel rel = new Rel(type: typeof(Relationships.IsIn));

                return await session.AsAsync(s=>
                s.ExecuteQuery<TUser>(
                    $"MATCH (n{n.Labels})-{rel}->(c{c.Labels} {{{nameof(IdentityRole.NormalizedName)}:${nameof(normalizedRoleName)}}})" +
                    $"RETURN n",
                    new { normalizedRoleName }).ToList(), 
                    cancellationToken);
            }
        }


        /// <summary>
        /// Sets the token value for a particular user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="loginProvider">The authentication provider for the token.</param>
        /// <param name="name">The name of the token.</param>
        /// <param name="value">The value of the token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserToken));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey userId = user.Id;

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}}) " +
                    $"MERGE (n)-{rel}->(c{c.Labels} {{{nameof(IdentityUserToken.LoginProvider)}:${nameof(loginProvider)},{nameof(IdentityUserToken.Name)}:${nameof(name)},{nameof(IdentityUserToken.Value)}:${nameof(value)}}})" +
                    $"ON CREATE SET c.{nameof(IGraphEntity.EntityId)}=id(c), c :{typeof(IdentityUserToken).Name}",
                    new { userId, loginProvider, name, value });
            }
        }

        /// <summary>
        /// Deletes a token for a user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="loginProvider">The authentication provider for the token.</param>
        /// <param name="name">The name of the token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TUser));
                Node c = new Node(type: typeof(IdentityUserToken));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey userId = user.Id;

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{rel}->(c{c.Labels} {{{nameof(IdentityUserToken.LoginProvider)}:${nameof(loginProvider)},{nameof(IdentityUserToken.Name)}:${nameof(name)}}})" +
                    $"DETACH DELETE c",
                    new { userId, loginProvider, name });
            }
        }

        /// <summary>
        /// Returns the token value.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="loginProvider">The authentication provider for the token.</param>
        /// <param name="name">The name of the token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task<string> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            Node n = new Node(type: typeof(TUser));
            Node c = new Node(type: typeof(IdentityUserToken));
            Rel rel = new Rel(type: typeof(Relationships.Has));

            TKey userId = user.Id;

            using (ISession session = Context.GetDriver().Session())
            {
                return await session.AsAsync(s=>
                s.ExecuteQuery<IdentityUserToken>(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityUser.Id)}:${nameof(userId)}}})" +
                    $"-{rel}->" +
                    $"(c{c.Labels} {{{nameof(IdentityUserToken.LoginProvider)}:${nameof(loginProvider)},{nameof(IdentityUserToken.Name)}:${nameof(name)}}})" +
                    $"RETURN c",
                    new { userId, loginProvider, name }).Select(p => p.Value).FirstOrDefault(), 
                    cancellationToken);
            }
        }
    }
}
