using System;
using System.Threading.Tasks;
using PnPSharp.PowerShell;
using Xunit;

namespace PnPSharp.Tests
{
    public class PowerShellHostServiceSmokeTests
    {
        [Fact]
        public async Task Invoke_GetDate_Works()
        {
            var options = new PowerShellHostOptions { AutoImportPnPModule = false };
            await using var host = new PowerShellHostService(options);

            var results = await host.InvokeCmdletAsync("Get-Date");
            Assert.NotEmpty(results);
            Assert.IsType<DateTime>(results[0].BaseObject);
        }
    }
}