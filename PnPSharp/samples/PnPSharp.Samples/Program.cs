using PnPSharp.PnP;
using PnPSharp.PowerShell;
using System.Collections.Generic;
using System.Management.Automation;

Console.WriteLine("PnPSharp Samples (.NET 9)");

// Example A: PnP Core SDK direct
// var ctx = await PnPContextFactory.CreateInteractiveAsync(
//     new Uri("https://contoso.sharepoint.com/sites/demo"), 
//     "<clientId>",
//     "<tenantId>");
// var web = await SharePointOperations.GetWebAsync(ctx);
// Console.WriteLine($"Web: {web.Title} ({web.Url})");

// Example B: Host PowerShell in-process and call a cmdlet
await using var host = new PowerShellHostService(
    new PowerShellHostOptions { AutoImportPnPModule = false });
var result = await host.InvokeCmdletAsync(
    "Get-ChildItem", 
    new Dictionary<string, object?> { { "Path", "." } });
foreach (var item in result) 
    Console.WriteLine(item);

// Example C: Using PnP.PowerShell module via hosted PowerShell (interactive DeviceLogin)
// Pre-req: Install-Module PnP.PowerShell -Scope CurrentUser
// var psOptions = new PowerShellHostOptions { AutoImportPnPModule = true };
// await using var psHost = new PowerShellHostService(psOptions);
// await psHost.InvokeCmdletAsync("Connect-PnPOnline", new Dictionary<string, object?>{
//     { "Url", "https://contoso.sharepoint.com/sites/demo" },
//     { "DeviceLogin", true }
// });
// var webs = await psHost.InvokeCmdletAsync("Get-PnPWeb");
// foreach (var w in webs) Console.WriteLine(w);