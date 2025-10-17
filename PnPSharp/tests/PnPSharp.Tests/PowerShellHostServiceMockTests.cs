using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PnPSharp.PowerShell;
using Xunit;
using System.Management.Automation;

namespace PnPSharp.Tests
{
    public class PowerShellHostServiceMockTests
    {
        [Fact]
        public async Task Consumer_Calls_Interface_Methods_Successfully()
        {
            var mock = new Mock<IPowerShellHostService>(MockBehavior.Strict);
            mock
                .Setup(m => m.InvokeCmdletAsync(
                    "Get-ChildItem", 
                    It.IsAny<IDictionary<string, object?>>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PSObject> { new PSObject("ok") });

            var results = await mock.Object.InvokeCmdletAsync(
                "Get-ChildItem", 
                new Dictionary<string, object?> { {"Path","."} });
            Assert.Single(results);
            Assert.Equal("ok", results[0].BaseObject);
            mock.VerifyAll();
        }

        [Fact]
        public void HostService_Implements_Interface()
        {
            var host = new PowerShellHostService();
            Assert.IsAssignableFrom<IPowerShellHostService>(host);
        }
    }
}