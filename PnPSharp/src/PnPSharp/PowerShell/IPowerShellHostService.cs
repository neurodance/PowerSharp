using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PnPSharp.PowerShell
{
    public interface IPowerShellHostService
    {
        Task<IReadOnlyList<PSObject>> InvokeCmdletAsync(
            string cmdletName,
            IDictionary<string, object?>? parameters = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<PSObject>> InvokeScriptAsync(
            string script,
            IDictionary<string, object?>? parameters = null,
            CancellationToken ct = default);
    }
}