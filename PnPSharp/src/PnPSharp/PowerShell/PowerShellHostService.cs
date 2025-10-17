using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace PnPSharp.PowerShell
{
    public sealed class PowerShellHostOptions
    {
        public int MinRunspaces { get; init; } = 1;
        public int MaxRunspaces { get; init; } = 5;
        public bool AutoImportPnPModule { get; init; } = true;
        public string? PnPModulePath { get; init; }
        public IReadOnlyList<string>? AdditionalModules { get; init; }
        public string? InitializationScript { get; init; }
    }

    public sealed class PowerShellHostService : IPowerShellHostService, IAsyncDisposable
    {
        private readonly RunspacePool _pool;
        private readonly PowerShellHostOptions _options;

        public PowerShellHostService(PowerShellHostOptions? options = null)
        {
            _options = options ?? new PowerShellHostOptions();
            var iss = InitialSessionState.CreateDefault2();

            if (_options.AutoImportPnPModule)
            {
                if (!string.IsNullOrWhiteSpace(_options.PnPModulePath))
                    iss.ImportPSModule(new[] { _options.PnPModulePath! });
                else
                    iss.ImportPSModule(new[] { "PnP.PowerShell" });
            }
            if (_options.AdditionalModules is not null && _options.AdditionalModules.Count > 0)
                iss.ImportPSModule(_options.AdditionalModules.ToArray());

            _pool = RunspaceFactory.CreateRunspacePool(
                _options.MinRunspaces <= 0 ? 1 : _options.MinRunspaces,
                _options.MaxRunspaces <= 0 ? 5 : _options.MaxRunspaces,
                iss, host: null);
            _pool.Open();

            if (!string.IsNullOrWhiteSpace(_options.InitializationScript))
            {
                using var ps = System.Management.Automation.PowerShell.Create();
                ps.RunspacePool = _pool;
                ps.AddScript(_options.InitializationScript!, useLocalScope: true);
                try { ps.Invoke(); } catch { /* Ignore init errors */ }
            }
        }

        public async Task<IReadOnlyList<PSObject>> InvokeCmdletAsync(
            string cmdletName,
            IDictionary<string, object?>? parameters = null,
            CancellationToken ct = default)
        {
            using var ps = System.Management.Automation.PowerShell.Create();
            ps.RunspacePool = _pool;
            ps.AddCommand(cmdletName);
            if (parameters != null)
                foreach (var kvp in parameters)
                    ps.AddParameter(kvp.Key, kvp.Value);
            return await InvokeAsync(ps, ct).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<PSObject>> InvokeScriptAsync(
            string script,
            IDictionary<string, object?>? parameters = null,
            CancellationToken ct = default)
        {
            using var ps = System.Management.Automation.PowerShell.Create();
            ps.RunspacePool = _pool;
            ps.AddScript(script, useLocalScope: true);
            if (parameters != null)
                foreach (var kvp in parameters)
                    ps.AddParameter(kvp.Key, kvp.Value);
            return await InvokeAsync(ps, ct).ConfigureAwait(false);
        }

        private static async Task<IReadOnlyList<PSObject>> InvokeAsync(
            System.Management.Automation.PowerShell ps, 
            CancellationToken ct)
        {
            try
            {
                var results = await Task.Factory.FromAsync(
                    ps.BeginInvoke(), 
                    ps.EndInvoke).ConfigureAwait(false);
                    
                if (ps.Streams.Error != null && ps.Streams.Error.Count > 0)
                {
                    var msg = string.Join(Environment.NewLine, 
                        ps.Streams.Error.Select(e => e.ToString()));
                    throw new InvalidOperationException($"PowerShell errors: {msg}");
                }
                return new ReadOnlyCollection<PSObject>(results.ToList());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("PowerShell invocation failed", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            _pool.Close();
            _pool.Dispose();
        }
    }
}