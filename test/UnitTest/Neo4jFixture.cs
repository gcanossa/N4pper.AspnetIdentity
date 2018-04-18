﻿using AsIKnow.XUnitExtensions;
using Microsoft.Extensions.DependencyInjection;
using AsIKnow.DependencyHelpers.Neo4j;
using System;
using System.Collections.Generic;
using System.Text;
using Neo4j.Driver.V1;
using N4pper;
using N4pper.Diagnostic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using N4pper.AspnetIdentity.Model;
using N4pper.AspnetIdentity;

namespace UnitTest
{
    public interface Neo4jServer
    { }

    public class Neo4jFixture : DockerEnvironmentsBaseFixture<Neo4jServer>
    {
        protected override void ConfigureServices(ServiceCollection sc)
        {
            sc.AddSingleton<IConfigurationRoot>(Configuration);

            sc.AddSingleton<DriverProvider, Neo4jServer_DriverProvider>();

            sc.AddN4pper();

            sc.AddTransient<IdentityErrorDescriber, IdentityErrorDescriber>();
            sc.AddTransient<IUserStore<IdentityUser>, UserStore<IdentityUser>>();
            sc.AddTransient<IRoleStore<IdentityRole>, RoleStore<IdentityRole>>();

            sc.AddTransient<Neo4jServer_DriverBuilder>(provider => new Neo4jServer_DriverBuilder(Configuration));

            sc.AddLogging(builder => builder.AddDebug().AddConsole());
        }


        public class Neo4jServer_DriverProvider : DriverProvider
        {
            private IConfigurationRoot _conf;
            public Neo4jServer_DriverProvider(IConfigurationRoot conf, N4pperManager manager)
                : base(manager)
            {
                _conf = conf;
            }

            public override string Uri => _conf.GetConnectionString("DefaultConnection");

            public override IAuthToken AuthToken => AuthTokens.None;

            public override Config Config => new Config();
        }
        public class Neo4jServer_DriverBuilder : DriverBuilder
        {
            private IConfigurationRoot _conf;
            public Neo4jServer_DriverBuilder(IConfigurationRoot conf)
            {
                _conf = conf;
            }
            public override string Uri => _conf.GetConnectionString("DefaultConnection");

            public override IAuthToken AuthToken => AuthTokens.None;

            public override Config Config => new Config();
        }

        public void Configure()
        {
            WaitForDependencies(builder => builder.AddNeo4jServer<Neo4jServer_DriverBuilder>("test"));
        }
    }
}
