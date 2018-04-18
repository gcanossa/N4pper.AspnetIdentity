using System;
using System.Collections.Generic;
using System.Text;
using Neo4j.Driver.V1;

namespace N4pper.AspnetIdentity
{
    public abstract class IdentityDriverProvider : DriverProvider
    {
        public IdentityDriverProvider(N4pperManager manager) : base(manager)
        {
        }
    }
}
