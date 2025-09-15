using System.ComponentModel;
using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace API.Domain.SemanticKernel.Plugins;

public sealed class TerminalPlugin
{
    private readonly ILogger<TerminalPlugin> _logger;

    public TerminalPlugin(ILogger<TerminalPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction, Description("Print 'Hello world' from the system shell and return the output.")]
    public async Task<string> HelloWorldAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("HelloWorldAsync invoked");
        var result = await RunCommandAsync("echo Hello world", ct);
        _logger.LogDebug("HelloWorldAsync completed. OutputLength={OutputLength}", result?.Length ?? 0);
        return result;
    }

    // DANGEROUS: only enable this in controlled environments.
    [KernelFunction, Description("Run a terminal command and return STDOUT. Use with extreme caution.")]
    public async Task<string> RunAsync(
        [Description("The exact command to run. Example: 'echo hi'")]
        string command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            _logger.LogWarning("RunAsync invoked with empty or whitespace command");
            return "No command provided.";
        }

        _logger.LogInformation("RunAsync invoked. CommandLength={CommandLength}", command.Length);
        _logger.LogDebug("RunAsync command preview: {Preview}", Redact(command));

        var result = await RunCommandAsync(command, ct);

        _logger.LogDebug("RunAsync completed. OutputLength={OutputLength}", result?.Length ?? 0);
        return result;
    }

    private async Task<string> RunCommandAsync(string command, CancellationToken ct)
    {
        var (file, args) = GetShellInvocation(command);
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        _logger.LogDebug("Starting process: {File} {ArgsPreview}", file, Redact(args));

        var sw = Stopwatch.StartNew();
        try
        {
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            // Wait for both streams and process exit
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            await process.WaitForExitAsync(ct);
            sw.Stop();

            var exitCode = process.ExitCode;
            _logger.LogInformation(
                "Process finished. ExitCode={ExitCode} DurationMs={DurationMs} StdOutLen={StdOutLen} StdErrLen={StdErrLen}",
                exitCode, sw.ElapsedMilliseconds, stdout?.Length ?? 0, stderr?.Length ?? 0);
            
            _logger.LogInformation("Process StdOut: {StdOut}", stdout);

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _logger.LogWarning("Process produced STDERR. Preview={Preview}", Redact(stderr));
                return $"STDOUT:\n{stdout}\nSTDERR:\n{stderr}".Trim();
            }

            return stdout.Trim();
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Process canceled after {DurationMs} ms: {File} {ArgsPreview}", sw.ElapsedMilliseconds,
                file, Redact(args));
            throw;
        }
        catch (Win32Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to start process: {File} {ArgsPreview}", file, Redact(args));
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Unexpected error running process: {File} {ArgsPreview}", file, Redact(args));
            throw;
        }
    }

    private static (string file, string args) GetShellInvocation(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            return ("cmd.exe", $"/c {command}");
        }

        // macOS/Linux
        return ("/bin/bash", $"-lc \"{command}\"");
    }

    private static string Redact(string? value, int keep = 200)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : (value.Length <= keep ? value : value[..keep] + "...");
}