using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace PowerSharp.Core.Middleware
{
    /// <summary>
    /// Core middleware that intercepts agent calls for observability.
    /// Implements three patterns: Evidence Accumulation, Resource Allocation, Feedback Loops.
    /// </summary>
    public class PowerSharpMiddleware
    {
        private readonly ActivitySource _activitySource;
        private readonly ILogger<PowerSharpMiddleware> _logger;
        
        public PowerSharpMiddleware(
            ActivitySource activitySource,
            ILogger<PowerSharpMiddleware> logger)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        #region Evidence Accumulation Pattern
        
        /// <summary>
        /// Start an agent activity for tracing (Evidence Accumulation pattern).
        /// Creates an OpenTelemetry Activity to capture the evidence trail.
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="operation">Operation being performed</param>
        /// <returns>Activity for the agent call, or null if tracing is disabled</returns>
        public Activity? StartAgentActivity(
            string agentName, 
            string operation = "agent.call")
        {
            var activity = _activitySource.StartActivity(
                operation, 
                ActivityKind.Internal);
                
            if (activity != null)
            {
                activity.SetTag("agent.name", agentName);
                activity.SetTag("agent.timestamp", DateTimeOffset.UtcNow.ToString("o"));
                activity.SetTag("agent.operation", operation);
                
                _logger.LogInformation(
                    "Agent activity started: {AgentName} - {Operation} - TraceId: {TraceId}", 
                    agentName, 
                    operation,
                    activity.TraceId);
            }
                
            return activity;
        }
        
        /// <summary>
        /// Record an agent event (tool call, LLM response, etc.)
        /// </summary>
        public void RecordAgentEvent(
            Activity? activity,
            string eventType,
            string eventData)
        {
            if (activity == null) return;
            
            var timestamp = DateTimeOffset.UtcNow.ToString("o");
            activity.AddEvent(new ActivityEvent(
                eventType,
                timestamp: DateTimeOffset.UtcNow,
                tags: new ActivityTagsCollection
                {
                    { "event.data", eventData },
                    { "event.timestamp", timestamp }
                }));
            
            _logger.LogDebug(
                "Agent event recorded: {EventType} - {EventData}", 
                eventType, 
                eventData);
        }
        
        #endregion
        
        #region Resource Allocation Pattern
        
        /// <summary>
        /// Record token usage for an agent call (Resource Allocation pattern)
        /// </summary>
        public void RecordTokenUsage(
            Activity? activity,
            string model,
            int inputTokens,
            int outputTokens,
            decimal cost)
        {
            if (activity == null) return;
            
            activity.SetTag("agent.model", model);
            activity.SetTag("agent.tokens.input", inputTokens);
            activity.SetTag("agent.tokens.output", outputTokens);
            activity.SetTag("agent.tokens.total", inputTokens + outputTokens);
            activity.SetTag("agent.cost", cost);
            
            _logger.LogInformation(
                "Token usage recorded: Model={Model}, Input={Input}, Output={Output}, Cost=${Cost}", 
                model, 
                inputTokens, 
                outputTokens, 
                cost);
        }
        
        /// <summary>
        /// Record operation latency
        /// </summary>
        public void RecordLatency(
            Activity? activity,
            string operation,
            TimeSpan duration)
        {
            if (activity == null) return;
            
            activity.SetTag($"agent.latency.{operation}", duration.TotalMilliseconds);
            
            _logger.LogDebug(
                "Latency recorded: {Operation} - {Duration}ms", 
                operation, 
                duration.TotalMilliseconds);
        }
        
        #endregion
        
        #region Feedback Loops Pattern
        
        /// <summary>
        /// Request feedback from user via Adaptive Card (Feedback Loops pattern)
        /// </summary>
        public async Task<TResponse?> RequestFeedbackAsync<TResponse>(
            Activity? activity,
            string cardJson,
            TimeSpan? timeout = null)
            where TResponse : class
        {
            if (activity == null) return null;
            
            activity.SetTag("agent.feedback.requested", true);
            activity.SetTag("agent.feedback.card", cardJson);
            
            _logger.LogInformation(
                "Feedback requested from user - TraceId: {TraceId}", 
                activity.TraceId);
            
            // TODO: Implement Adaptive Card rendering and response capture
            // This will be implemented in Phase 1 with actual Adaptive Cards integration
            
            await Task.Delay(0); // Placeholder
            return null;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Complete an activity with success status
        /// </summary>
        public void CompleteActivity(
            Activity? activity,
            string result)
        {
            if (activity == null) return;
            
            activity.SetTag("agent.status", "success");
            activity.SetTag("agent.result", result);
            activity.SetTag("agent.completed_at", DateTimeOffset.UtcNow.ToString("o"));
            
            _logger.LogInformation(
                "Agent activity completed successfully - TraceId: {TraceId}", 
                activity.TraceId);
        }
        
        /// <summary>
        /// Complete an activity with error status
        /// </summary>
        public void CompleteActivityWithError(
            Activity? activity,
            Exception exception)
        {
            if (activity == null) return;
            
            activity.SetTag("agent.status", "error");
            activity.SetTag("agent.error.type", exception.GetType().Name);
            activity.SetTag("agent.error.message", exception.Message);
            activity.SetTag("agent.error.stacktrace", exception.StackTrace);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            
            _logger.LogError(
                exception,
                "Agent activity failed - TraceId: {TraceId}", 
                activity.TraceId);
        }
        
        #endregion
    }
}
