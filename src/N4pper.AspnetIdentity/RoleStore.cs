using Microsoft.AspNetCore.Identity;
using N4pper.AspnetIdentity.Model;
using N4pper.QueryUtils;
using Neo4j.Driver.V1;
using OMnG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace N4pper.AspnetIdentity
{
    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role</typeparam>
    public class RoleStore<TRole> : RoleStore<TRole, IdentityDriverProvider, string>
        where TRole : IdentityRole<string>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="RoleStore{TRole}"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public RoleStore(IdentityDriverProvider context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class RoleStore<TRole, TContext> : RoleStore<TRole, TContext, string>
        where TRole : IdentityRole<string>
        where TContext : IdentityDriverProvider
    {
        /// <summary>
        /// Constructs a new instance of <see cref="RoleStore{TRole, TContext}"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public RoleStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserRole">The type of the class representing a user role.</typeparam>
    /// <typeparam name="TRoleClaim">The type of the class representing a role claim.</typeparam>
    public class RoleStore<TRole, TContext, TKey> :
        RoleStoreBase<TRole, TKey>,
        IQueryableRoleStore<TRole>,
        IRoleClaimStore<TRole>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TContext : IdentityDriverProvider
    {
        /// <summary>
        /// Constructs a new instance of <see cref="RoleStore{TRole, TContext, TKey, TUserRole, TRoleClaim}"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public RoleStore(TContext context, IdentityErrorDescriber describer = null)
            : base(describer)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            ErrorDescriber = describer ?? new IdentityErrorDescriber();
        }
        
        /// <summary>
        /// Gets the database context for this store.
        /// </summary>
        public TContext Context { get; private set; }
                
        /// <summary>
        /// Creates a new role in a store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role to create in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        public async override Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node("p",type: typeof(TRole));
                TRole tmp = await session.AsAsync(s=>s.ExecuteQuery<TRole>(
                    $"CREATE {n} " +
                    $"SET p+=$role, p.{nameof(role.EntityId)}=id(p) " +
                    $"RETURN p",
                    new { role = role.ExludeProperties(p=>p.EntityId) }).FirstOrDefault(), 
                    cancellationToken);
                if (tmp == null)
                    return IdentityResult.Failed();
                else
                    role.EntityId = tmp.EntityId;
            }
            return IdentityResult.Success;
        }

        /// <summary>
        /// Updates a role in a store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role to update in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        public async override Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            role.ConcurrencyStamp = Guid.NewGuid().ToString();

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TRole));
                await session.RunAsync(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityRole.Id)}:$role.{nameof(IdentityRole.Id)},{nameof(IdentityRole.EntityId)}:$role.{nameof(IdentityRole.EntityId)}}}) " +
                    $"SET p+=$role",
                    new { role = role.ToPropDictionary() });
            }
            return IdentityResult.Success;
        }

        /// <summary>
        /// Deletes a role from the store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role to delete from the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        public async override Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TRole));
                await session.RunAsync(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityRole.Id)}:$role.{nameof(IdentityRole.Id)},{nameof(IdentityRole.EntityId)}:$role.{nameof(IdentityRole.EntityId)}}}) " +
                    $"DETACH DELETE p",
                    new { role = role.ToPropDictionary() });
            }
            return IdentityResult.Success;
        }
        
        /// <summary>
        /// Finds the role who has the specified ID as an asynchronous operation.
        /// </summary>
        /// <param name="id">The role ID to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public override async Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var roleId = ConvertIdFromString(id);

            Node n = new Node(type: typeof(TRole));

            using (ISession session = Context.GetDriver().Session())
            {
                return await session.AsAsync(s=>
                s.ExecuteQuery<TRole>(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityRole.Id)}:${nameof(roleId)}}}) " +
                    $"RETURN p",
                    new { roleId }).FirstOrDefault(), 
                    cancellationToken);
            }
        }

        /// <summary>
        /// Finds the role who has the specified normalized name as an asynchronous operation.
        /// </summary>
        /// <param name="normalizedName">The normalized role name to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public override async Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            Node n = new Node(type: typeof(TRole));

            using (ISession session = Context.GetDriver().Session())
            {
                return await session.AsAsync(s=>
                s.ExecuteQuery<TRole>(
                    $"MATCH (p{n.Labels} {{{nameof(IdentityRole.NormalizedName)}:${nameof(normalizedName)}}}) " +
                    $"RETURN p",
                    new { normalizedName }).FirstOrDefault(), 
                    cancellationToken);
            }
        }
                
        /// <summary>
        /// Get the claims associated with the specified <paramref name="role"/> as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose claims should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the claims granted to a role.</returns>
        public async override Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TRole));
                Node r = new Node(type: typeof(IdentityClaim));

                TKey roleId = role.Id;

                return await session.AsAsync(s=>
                s.ExecuteQuery<IdentityClaim>(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityRole.Id)}:${nameof(roleId)}}})" +
                    $"-{new Rel("rel", type: typeof(Relationships.Has))}->" +
                    $"(r{r.Labels}) " +
                    $"RETURN r",
                    new { roleId }).ToList().Select(p => p.ToClaim()).ToList(), 
                    cancellationToken);
            }
        }

        /// <summary>
        /// Adds the <paramref name="claim"/> given to the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to add the claim to.</param>
        /// <param name="claim">The claim to add to the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TRole));
                Node c = new Node(type: typeof(IdentityClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey roleId = role.Id;

                IdentityClaim iclaim = new IdentityClaim();
                iclaim.InitializeFromClaim(claim);

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityRole.Id)}:${nameof(roleId)}}}) " +
                    $"CREATE (n)-{rel}->(c{c.Labels})" +
                    $"SET c+=${nameof(claim)}, c.{nameof(IGraphEntity.EntityId)}=id(c)",
                    new { roleId, claim = iclaim.SelectProperties(p=>new { p.ClaimValue, p.ClaimType }) });
            }
        }

        /// <summary>
        /// Removes the <paramref name="claim"/> given from the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to remove the claim from.</param>
        /// <param name="claim">The claim to remove from the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async override Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            using (ISession session = Context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(TRole));
                Node c = new Node(type: typeof(IdentityClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                TKey roleId = role.Id;

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(IdentityRole.Id)}:${nameof(roleId)}}})" +
                    $"-{rel}->" +
                    $"(c{c.Labels} {{{nameof(IdentityClaim.ClaimValue)}:${nameof(Claim.Value)},{nameof(IdentityClaim.ClaimType)}:${nameof(Claim.Type)}}})" +
                    $"DETACH DELETE c",
                    new { roleId, claim.Value, claim.Type });
            }
        }

        /// <summary>
        /// A navigation property for the roles the store contains.
        /// </summary>
        public override IQueryable<TRole> Roles
        {
            get
            {
                using (ISession session = Context.GetDriver().Session())
                {
                    Node n = new Node(type: typeof(TRole));
                    return session.ExecuteQuery<TRole>(
                        $"MATCH (p{n.Labels}) " +
                        $"RETURN p").ToList().AsQueryable();
                }
            }
        }
    }
}
