//    Copyright 2018-2020 Andrew White
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Finbuckle.MultiTenant;

namespace PerTenantAuthenticationSample
{
    public class SampleConfigurationStore : IMultiTenantStore<SampleTenantInfo>
    {
        internal static readonly string defaultSectionName = "Finbuckle:MultiTenant:Stores:ConfigurationStore";
        private readonly IConfigurationSection section;
        private ConcurrentDictionary<string, SampleTenantInfo> tenantMap;

        public SampleConfigurationStore(IConfiguration configuration) : this(configuration, defaultSectionName)
        {
        }

        public SampleConfigurationStore(IConfiguration configuration, string sectionName)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrEmpty(sectionName))
            {
                throw new ArgumentException("Section name provided to the Configuration Store is null or empty.", nameof(sectionName));
            }

            section = configuration.GetSection(sectionName);
            if(!section.Exists())
            {
                throw new MultiTenantException("Section name provided to the Configuration Store is invalid.");
            }

            UpdateTenantMap();
            ChangeToken.OnChange(() => section.GetReloadToken(), UpdateTenantMap);
        }

        private void UpdateTenantMap()
        {
            var newMap = new ConcurrentDictionary<string, SampleTenantInfo>(StringComparer.OrdinalIgnoreCase);
            var tenants = section.GetSection("Tenants").GetChildren();

            foreach(var tenantSection in tenants)
            {
                var newTenant = section.GetSection("Defaults").Get<SampleTenantInfo>((options => options.BindNonPublicProperties = true));
                tenantSection.Bind(newTenant, options => options.BindNonPublicProperties = true);
                newMap.TryAdd(newTenant.Identifier, newTenant);
                foreach(var ident in newTenant.Identifiers)
                {
                    newMap.TryAdd(ident, newTenant);
                }
            }

            var oldMap = tenantMap;
            tenantMap = newMap;
        }

        public Task<bool> TryAddAsync(SampleTenantInfo tenantInfo)
        {
            throw new NotImplementedException();
        }

        public async Task<SampleTenantInfo> TryGetAsync(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return await Task.FromResult(tenantMap.Where(kv => kv.Value.Id == id).SingleOrDefault().Value);
        }

        public async Task<SampleTenantInfo> TryGetByIdentifierAsync(string identifier)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            return await Task.FromResult(tenantMap.TryGetValue(identifier, out var result) ? result : null);
        }

        public Task<bool> TryRemoveAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryUpdateAsync(SampleTenantInfo tenantInfo)
        {
            throw new NotImplementedException();
        }
    }
}