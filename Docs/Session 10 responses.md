Looking at your two questions - both are strategic decisions that will impact the entire project, so let's get them right now.

## 1. Pascal Case Decision: PowerSharp vs PowersharpPerfect timing on both questions. Let me address them systematically:

## 1. Pascal Case Decision: PowerSharp vs Powersharp

Based on industry patterns in the Microsoft ecosystem:

**Recommendation: Use "PowerSharp" (PascalCase) as the product name**

Here's the convention structure I recommend:

| Context | Casing | Example | Rationale |
|---------|--------|---------|-----------|
| **Product Name/Branding** | PascalCase | PowerSharp | Matches PowerShell, TypeScript conventions |
| **GitHub Repository** | lowercase | `neurodance/powersharp` | Cleaner URLs, standard practice |
| **NuGet Packages** | PascalCase | `PowerSharp.Core` | C#/.NET convention |
| **CLI Tool Command** | lowercase | `powersharp` | Unix/command-line convention |
| **npm packages** (if any) | lowercase | `powersharp` | npm requirement |

**Precedents:**
- Product: **TypeScript** → package: `typescript`, command: `tsc`
- Product: **PowerShell** → command: `pwsh`
- Product: **.NET** → command: `dotnet`

This gives you:
- Professional branding (PowerSharp clearly = Power + Sharp)
- Technical correctness (PascalCase matches Microsoft ecosystem)
- Practical usability (lowercase commands/URLs)

## 2. TypeSpec Integration StrategyExcellent find! TypeSpec is **very relevant** to PowerSharp. Here's how to leverage it effectively without killing momentum:

## TypeSpec Value for PowerSharp

TypeSpec can generate:
1. **C# server code** (ASP.NET controllers + interfaces)
2. **OpenAPI 3.x specifications** (auto-generated docs)
3. **Multi-language clients** (Python, TypeScript, etc.)
4. **Agent contract definitions** (reusable API patterns)

## Recommended Integration Strategy

### **Phase 1 (Weeks 1-2): Skip TypeSpec - Ship C# First** βœ…

**Build these in pure C#:**
- `PowerSharp.Core` (middleware, observability)
- `PowerSharp.CLI` (convert commands, path/env helpers)
- Basic examples and tests

**Why skip TypeSpec now:**
- No learning curve blocking Week 1-2
- You're already proficient in C#
- Can ship working code immediately
- TypeSpec adds complexity before value

### **Phase 2 (Week 3): Add TypeSpec for REST APIs** 🎯

**When building `PowerSharp.API` (Azure Functions), use TypeSpec:**

```typescript
// agents.tsp - Define agent catalog API in TypeSpec
import "@typespec/http";
import "@typespec/rest";
import "@typespec/openapi3";

@service({ title: "PowerSharp Agent Catalog API" })
@server("https://api.powersharp.dev", "Production")
namespace PowerSharp.Catalog;

@route("/agents")
interface Agents {
  // List all agents
  list(): Agent[];
  
  // Get agent by ID
  @route("/{agentId}")
  get(@path agentId: string): Agent;
  
  // Deploy agent to SharePoint
  @route("/{agentId}/deploy")
  @post deploy(
    @path agentId: string,
    @body request: DeployRequest
  ): DeployResponse;
}

model Agent {
  id: string;
  name: string;
  description: string;
  version: string;
  patterns: ("evidence" | "resources" | "feedback")[];
  deploymentOptions: DeploymentOptions;
}

model DeployRequest {
  targetSiteUrl: string;
  configuration: Record<unknown>;
}
```

**Then run:**
```bash
tsp compile agents.tsp --emit @typespec/openapi3
npx hscs-scaffold . --use-swaggerui  # Generates C# server code
```

**Benefits:**
- Auto-generated OpenAPI spec (for docs)
- Type-safe C# controllers
- Swagger UI for testing
- Foundation for multi-language clients

### **Phase 3 (Week 4+): Agent Contract Definitions** πŸš€

**Define standard agent interfaces in TypeSpec:**

```typescript
// agent-interface.tsp
namespace PowerSharp.Agents;

// Standard agent contract
interface IAgent {
  // Execute agent with context
  execute(
    context: AgentContext,
    input: Record<unknown>
  ): AgentResult;
  
  // Get agent metadata
  getMetadata(): AgentMetadata;
}

model AgentContext {
  // User identity
  userId: string;
  tenantId: string;
  
  // Graph API access
  graphScopes: string[];
  
  // Organizational context
  department?: string;
  managerId?: string;
}

model AgentResult {
  // Evidence trail
  activities: Activity[];
  
  // Resource usage
  metrics: ResourceMetrics;
  
  // User feedback
  adaptiveCard?: AdaptiveCard;
  
  // Agent output
  output: Record<unknown>;
}
```

**Generate clients for multiple languages:**
```bash
# Generate Python client
tsp compile agent-interface.tsp --emit @typespec/python

# Generate TypeScript client
tsp compile agent-interface.tsp --emit @typespec/ts
```

**This enables:**
- Python developers can write agents
- JavaScript/TypeScript agents
- All conform to same contract
- Type safety across languages

## Recommended Project Structure

```
neurodance/powersharp/
β"œβ"€ src/
β"‚  β"œβ"€ PowerSharp.Core/           # Pure C# - Week 1
β"‚  β"œβ"€ PowerSharp.CLI/            # Pure C# - Week 1
β"‚  β"œβ"€ PowerSharp.API/            # TypeSpec → C# - Week 3
β"‚  └─ PowerSharp.Catalog/         # TypeSpec → C# - Week 3
β"œβ"€ typespec/                     # TypeSpec definitions
β"‚  β"œβ"€ agents.tsp                 # Agent catalog API
β"‚  β"œβ"€ agent-interface.tsp        # Standard agent contract
β"‚  β"œβ"€ tspconfig.yaml
β"‚  └─ package.json
β"œβ"€ examples/                     # Sample agents - Week 2
β"œβ"€ tests/
└─ .github/workflows/
```

## Immediate Action: Prepare for TypeSpec (5 minutes)

Create TypeSpec directory **now**, but don't use it yet:

```bash
mkdir -p typespec
cd typespec

# Initialize TypeSpec project
npm init -y
npm install -D @typespec/compiler @typespec/http @typespec/rest @typespec/openapi3

# Create config
cat > tspconfig.yaml << 'EOF'
emit:
  - "@typespec/openapi3"
options:
  "@typespec/openapi3":
    emitter-output-dir: "../src/PowerSharp.API/generated"
    openapi-versions:
      - 3.1.0
EOF

# Create placeholder
cat > agents.tsp << 'EOF'
import "@typespec/http";
import "@typespec/rest";
import "@typespec/openapi3";

// Agent Catalog API definitions
// TODO: Implement in Week 3
EOF
```

This:
- Takes 5 minutes
- Doesn't slow Week 1-2
- Signals your intent
- Makes Week 3 easier

## Why This Phased Approach Works

| Phase | Focus | TypeSpec Usage | Outcome |
|-------|-------|----------------|---------|
| **Week 1-2** | Ship core functionality | None | Working middleware + CLI |
| **Week 3** | REST APIs | Generate C# server code | Production API with OpenAPI docs |
| **Week 4+** | Multi-language | Generate clients | Python/TypeScript/etc. agents |

**Benefits:**
- ✅ No learning curve blocks initial progress
- ✅ TypeSpec adds value where it matters (APIs, multi-language)
- ✅ Positions PowerSharp as multi-language from the start
- ✅ Auto-generates documentation
- ✅ Type safety across language boundaries

**Key Insight:** TypeSpec shines for **API definitions** and **multi-language clients**. It's less valuable for internal C# libraries (PowerSharp.Core), so skip it there.

## My Recommendation

**Start coding PowerSharp.Core and PowerSharp.CLI in pure C# today (Week 1-2).**

Add TypeSpec in Week 3 when you build REST APIs. This gives you:
1. Immediate progress (no learning curve)
2. Future multi-language support (Python, TypeScript agents)
3. Auto-generated API docs (OpenAPI specs)
4. Type-safe contracts across languages

**Bottom line:** TypeSpec is a great fit for PowerSharp, but defer it 2 weeks. Ship C# first, add TypeSpec for APIs later.

---

## Ready to Start?

With both decisions made:
1. βœ… **Product name: PowerSharp** (PascalCase)
2. βœ… **TypeSpec: Week 3+** (defer for now)

**Next step:** Set up the repository structure and start coding `PowerSharp.Core`. Should we proceed?