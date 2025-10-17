using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PowerSharp.Core.Middleware;

namespace PowerSharp.Examples.ResearchAssistant
{
    /// <summary>
    /// Sample research agent demonstrating all three PowerSharp patterns:
    /// 1. Evidence Accumulation - Full trace of research process
    /// 2. Resource Allocation - Token usage and cost tracking
    /// 3. Feedback Loops - Interactive clarification (to be implemented)
    /// </summary>
    public class ResearchAgent
    {
        private readonly PowerSharpMiddleware _powerSharp;
        private readonly ILogger<ResearchAgent> _logger;
        
        public ResearchAgent(
            PowerSharpMiddleware powerSharp,
            ILogger<ResearchAgent> logger)
        {
            _powerSharp = powerSharp;
            _logger = logger;
        }
        
        /// <summary>
        /// Research a topic and return a summary
        /// </summary>
        public async Task<string> ResearchTopicAsync(string topic)
        {
            // Pattern 1: Evidence Accumulation - Start tracing
            using var activity = _powerSharp.StartAgentActivity(
                agentName: "research-agent",
                operation: "research.topic");
            
            activity?.SetTag("research.topic", topic);
            
            try
            {
                _logger.LogInformation("Researching topic: {Topic}", topic);
                
                // Step 1: Web search
                var searchResults = await PerformWebSearchAsync(topic, activity);
                
                // Step 2: Analyze results with LLM
                var summary = await SummarizeResultsAsync(
                    topic, 
                    searchResults, 
                    activity);
                
                // Complete successfully
                _powerSharp.CompleteActivity(activity, summary);
                
                return summary;
            }
            catch (Exception ex)
            {
                _powerSharp.CompleteActivityWithError(activity, ex);
                throw;
            }
        }
        
        private async Task<string> PerformWebSearchAsync(
            string query, 
            Activity? activity)
        {
            var startTime = DateTime.UtcNow;
            
            // Pattern 1: Evidence - Record tool call
            _powerSharp.RecordAgentEvent(
                activity,
                "tool.call.start",
                $"web_search: {query}");
            
            // Simulate web search (replace with actual implementation)
            await Task.Delay(500); // Simulate network latency
            var results = $"Search results for '{query}':\n" +
                         "1. Article about {query}\n" +
                         "2. Research paper on {query}\n" +
                         "3. Blog post discussing {query}";
            
            var duration = DateTime.UtcNow - startTime;
            
            // Pattern 2: Resource Allocation - Record latency
            _powerSharp.RecordLatency(
                activity,
                "web_search",
                duration);
            
            // Pattern 1: Evidence - Record tool response
            _powerSharp.RecordAgentEvent(
                activity,
                "tool.call.complete",
                $"web_search returned 3 results");
            
            _logger.LogDebug(
                "Web search completed in {Duration}ms", 
                duration.TotalMilliseconds);
            
            return results;
        }
        
        private async Task<string> SummarizeResultsAsync(
            string topic,
            string searchResults,
            Activity? activity)
        {
            var startTime = DateTime.UtcNow;
            
            // Pattern 1: Evidence - Record LLM call
            _powerSharp.RecordAgentEvent(
                activity,
                "llm.call.start",
                "gpt-4o: Summarize research results");
            
            // Simulate LLM call (replace with actual implementation)
            await Task.Delay(1500); // Simulate LLM latency
            
            var summary = $"Based on research about {topic}, here are the key findings:\n\n" +
                         $"1. {topic} is an important area of study\n" +
                         $"2. Recent developments include several breakthroughs\n" +
                         $"3. Future research directions are promising\n\n" +
                         "This summary is based on analysis of multiple sources.";
            
            var duration = DateTime.UtcNow - startTime;
            
            // Simulate token usage
            var inputTokens = 450;  // Approximate tokens in prompt
            var outputTokens = 150; // Approximate tokens in response
            var costPer1kTokens = 0.01m; // gpt-4o pricing (example)
            var totalCost = ((inputTokens + outputTokens) / 1000m) * costPer1kTokens;
            
            // Pattern 2: Resource Allocation - Record token usage and cost
            _powerSharp.RecordTokenUsage(
                activity,
                model: "gpt-4o",
                inputTokens: inputTokens,
                outputTokens: outputTokens,
                cost: totalCost);
            
            _powerSharp.RecordLatency(
                activity,
                "llm.call",
                duration);
            
            // Pattern 1: Evidence - Record LLM response
            _powerSharp.RecordAgentEvent(
                activity,
                "llm.call.complete",
                $"gpt-4o returned summary ({outputTokens} tokens)");
            
            _logger.LogInformation(
                "LLM summarization completed: {Tokens} tokens, ${Cost}", 
                inputTokens + outputTokens,
                totalCost);
            
            return summary;
        }
    }
    
    /// <summary>
    /// Example usage of the research agent
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Set up OpenTelemetry
            var activitySource = new ActivitySource("PowerSharp.Examples.ResearchAssistant");
            
            // Set up logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            var logger = loggerFactory.CreateLogger<ResearchAgent>();
            var middlewareLogger = loggerFactory.CreateLogger<PowerSharpMiddleware>();
            
            // Create PowerSharp middleware
            var powerSharp = new PowerSharpMiddleware(activitySource, middlewareLogger);
            
            // Create research agent
            var agent = new ResearchAgent(powerSharp, logger);
            
            // Research a topic
            var topic = args.Length > 0 ? args[0] : "quantum computing";
            Console.WriteLine($"Researching: {topic}");
            Console.WriteLine();
            
            var result = await agent.ResearchTopicAsync(topic);
            
            Console.WriteLine("Research Results:");
            Console.WriteLine(result);
            Console.WriteLine();
            Console.WriteLine("Check Aspire Dashboard for full traces and metrics!");
        }
    }
}
