#if ENABLE_INTEGRATION_TESTS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PnPSharp.PowerShell;
using Xunit;

namespace PnPSharp.IntegrationTests
{
    public class PnPOnlineDeviceCodeTests
    {
        [Fact(DisplayName = "Connect-PnPOnline DeviceLogin can retrieve Get-PnPWeb")]
        public async Task ConnectPnPOnline_DeviceLogin_Works()
        {
            var siteUrl = Environment.GetEnvironmentVariable("PNP_TEST_SITE_URL");
            Assert.False(string.IsNullOrWhiteSpace(siteUrl), 
                "Set PNP_TEST_SITE_URL to a SharePoint site URL");

            var options = new PowerShellHostOptions { AutoImportPnPModule = true };
            await using var host = new PowerShellHostService(options);

            await host.InvokeCmdletAsync("Connect-PnPOnline", new Dictionary<string, object?> {
                ["Url"] = siteUrl!,
                ["DeviceLogin"] = true
            });

            var webs = await host.InvokeCmdletAsync("Get-PnPWeb");
            Assert.NotEmpty(webs);
        }
    }
}
#endif