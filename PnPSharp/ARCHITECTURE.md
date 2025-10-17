# Architecture: PnPSharp

```mermaid
flowchart LR
  subgraph Consumers
    A[.NET 9 apps (Web/Worker/Console)]
  end
  subgraph PnPSharp Library
    S1[PowerShellHostService (Runspace Pool)]
    S2[PnP Wrappers (PnP.Core / Framework)]
  end
  A -->|Direct SDK| S2
  A -->|Cmdlets| S1
  subgraph External
    G[Microsoft Graph]
    SP[SharePoint Online]
    PS[PowerShell 7.x]
  end
  S2 --> G
  S2 --> SP
  S1 --> PS
```

**Design**: Prefer direct SDKs; host PowerShell only when cmdlets are required; reuse runspaces; pluggable auth.