using Microsoft.SemanticKernel;

namespace API.Domain.SemanticKernel.ActionFilters;

public sealed class TerminalCommandGuardFilter : IFunctionInvocationFilter
{
    private readonly ILogger<TerminalCommandGuardFilter> _logger;

    private static readonly HashSet<string> BlockedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm", "del", "shutdown", "reboot", "format", "mkfs", "rd", "rmdir", "mkdir"
    };

    public TerminalCommandGuardFilter(ILogger<TerminalCommandGuardFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        // Only guard the "terminal" plugin's "Run" function
        if (string.Equals(context.Function.PluginName, "terminal", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(context.Function.Name, "Run", StringComparison.OrdinalIgnoreCase))
        {
            string? command = null;

            if (context.Arguments.TryGetValue("command", out var value) && value is not null)
            {
                command = value.ToString();
            }

            if (!string.IsNullOrWhiteSpace(command))
            {
                var firstToken = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                if (BlockedCommands.Contains(firstToken))
                {
                    _logger.LogWarning("Blocked dangerous command: {Command}", command);

                    // Short-circuit: set a result and do not call the actual function
                    context.Result = new FunctionResult(context.Function, "This command is not allowed.");
                    return;
                }
            }
        }

        // Continue execution
        await next(context);

        // Optional: post-processing on context.Result for logging, redaction, etc.
    }
}