using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N4pper.AspnetIdentity.Model;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace N4pper.AspnetIdentity
{
    public static class IServiceCollectionExtensions
    {
        #region nested types
        
        private class InternalDriverProvider : IdentityDriverProvider
        {
            public InternalDriverProvider(string uri, IAuthToken authToken, Config config, N4pperManager manager)
                : base(manager)
            {
                if (string.IsNullOrEmpty(uri))
                    throw new ArgumentNullException(nameof(uri));

                _uri = uri;
                _authToken = authToken ?? AuthTokens.None;
                _config = config ?? new Config();
            }

            private string _uri;
            private IAuthToken _authToken;
            private Config _config;

            public override string Uri => _uri;

            public override IAuthToken AuthToken => _authToken;

            public override Config Config => _config;
        }

        #endregion

        public static IdentityBuilder UseIdentityNeo4jStores(this IdentityBuilder ext, Options options)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            options = options ?? throw new ArgumentNullException(nameof(options));

            ext.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(ext.UserType),
                typeof(UserStore<,,>).MakeGenericType(ext.UserType, ext.RoleType, typeof(IdentityDriverProvider)));
            ext.Services.AddScoped(
                typeof(IRoleStore<>).MakeGenericType(ext.RoleType),
                typeof(RoleStore<,>).MakeGenericType(ext.RoleType, typeof(IdentityDriverProvider)));

            ext.Services.AddSingleton<IdentityDriverProvider>(provider=>new InternalDriverProvider(options.Uri, options.Token,options.Configuration, provider.GetRequiredService<N4pperManager>()));
            
            return ext;
        }
    }
}
