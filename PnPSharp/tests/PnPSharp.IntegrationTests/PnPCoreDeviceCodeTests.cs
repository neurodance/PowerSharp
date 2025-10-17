#if ENABLE_INTEGRATION_TESTS
using System;
using System.Threading.Tasks;
using PnPSharp.PnP;
using Xunit;

namespace PnPSharp.IntegrationTests
{
    public class PnPCoreDeviceCodeTests
    {
        [Fact(DisplayName = "PnP Core DeviceCode login can read Web title")]
        public async Task PnPCore_DeviceCode_Works()
        {
            var siteUrl = Environment.GetEnvironmentVariable("PNP_TEST_SITE_URL");
            var clientId = Environment.GetEnvironmentVariable("PNP_TEST_CLIENT_ID");
            Assert.False(string.IsNullOrWhiteSpace(siteUrl), 
                "Set PNP_TEST_SITE_URL to a SharePoint site URL");
            Assert.False(string.IsNullOrWhiteSpace(clientId), 
                "Set PNP_TEST_CLIENT_ID to an Entra app ID (Device Code)");

            var ctx = await PnPContextFactory.CreateInteractiveAsync(
                new Uri(siteUrl!), clientId!);
            var web = await SharePointOperations.GetWebAsync(ctx);
            Assert.False(string.IsNullOrWhiteSpace(web.Title));
        }
    }
}
#endif