using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PnP.Core.Auth;
using PnP.Core.Services;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PnPSharp.PnP
{
    public static class PnPContextFactory
    {
        public static async Task<PnPContext> CreateAppOnlyAsync(
            Uri siteUrl, 
            string tenantId, 
            string clientId, 
            string certThumbprint)
        {
            var cert = GetCertificateByThumbprint(certThumbprint);
            var auth = new X509CertificateAuthenticationProvider(clientId, tenantId, cert);
            
            var services = new ServiceCollection();
            services.AddPnPCore(options =>
            {
                options.DefaultAuthenticationProvider = auth;
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var contextFactory = serviceProvider.GetRequiredService<IPnPContextFactory>();
            return await contextFactory.CreateAsync(siteUrl).ConfigureAwait(false);
        }

        public static async Task<PnPContext> CreateInteractiveAsync(Uri siteUrl, string clientId, string tenantId)
        {
            // DeviceCodeAuthenticationProvider constructor: (clientId, tenantId, redirectUri, deviceCodeNotification)
            var redirectUri = new Uri("http://localhost");
            var auth = new DeviceCodeAuthenticationProvider(clientId, tenantId, redirectUri, null);
            
            var services = new ServiceCollection();
            services.AddPnPCore(options =>
            {
                options.DefaultAuthenticationProvider = auth;
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var contextFactory = serviceProvider.GetRequiredService<IPnPContextFactory>();
            return await contextFactory.CreateAsync(siteUrl).ConfigureAwait(false);
        }

        private static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            if (certs.Count == 0)
                throw new InvalidOperationException($"Certificate with thumbprint '{thumbprint}' not found in CurrentUser\\My store");
            return certs[0];
        }
    }
}