using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using LANShareManager.Core.Interfaces;

namespace LANShareManager.Infrastructure.Process;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success => ExitCode == 0;
}

public class PowerShellProcessRunner
{
    private readonly ILoggerService? _logger;

    static PowerShellProcessRunner()
    {
        try
        {
            // Register CodePages to support OEM (CP866) and Windows ANSI (CP1251) encodings
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        catch { }
    }

    public PowerShellProcessRunner(ILoggerService? logger = null)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> RunPowerShellCommandAsync(string command, int timeoutSeconds = 30)
    {
        _logger?.LogInfo($"Executing PowerShell: {command}");

        // Bootstrap UTF-8 console output/input and pipeline encodings in PowerShell.
        // This prevents PowerShell from converting Cyrillic/non-ASCII output to CP866/ASCII or '????'.
        string utf8Bootstrap = @"
$OutputEncoding = [System.Text.Encoding]::UTF8
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}
try { [Console]::InputEncoding = [System.Text.Encoding]::UTF8 } catch {}
";
        string fullCommand = utf8Bootstrap + "\r\n" + command;

        byte[] bytes = Encoding.Unicode.GetBytes(fullCommand);
        string encodedCommand = Convert.ToBase64String(bytes);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -OutputFormat Text -EncodedCommand {encodedCommand}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await process.WaitForExitAsync(cts.Token);

            string output = outputBuilder.ToString().Trim();
            string rawError = errorBuilder.ToString().Trim();
            string cleanError = CleanErrorMessage(rawError);

            if (process.ExitCode != 0)
            {
                _logger?.LogWarning($"PowerShell exited with code {process.ExitCode}. Error: {cleanError}");
            }

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = output,
                StandardError = cleanError
            };
        }
        catch (OperationCanceledException)
        {
            _logger?.LogError($"PowerShell command timed out after {timeoutSeconds}s: {command}");
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = $"Команда PowerShell прервана по таймауту ({timeoutSeconds} сек)."
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to execute PowerShell command: {command}", ex);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message
            };
        }
    }

    public async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, int timeoutSeconds = 30)
    {
        _logger?.LogInfo($"Executing process: {fileName} {arguments}");

        // For console tools like net.exe, netsh.exe on Windows, encoding is usually OEM (CP866 on RU, CP437 on US)
        Encoding encoding = Encoding.UTF8;
        try
        {
            int oemCp = System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
            encoding = Encoding.GetEncoding(oemCp);
        }
        catch
        {
            encoding = Encoding.UTF8;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding
        };

        try
        {
            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await process.WaitForExitAsync(cts.Token);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString().Trim(),
                StandardError = CleanErrorMessage(errorBuilder.ToString().Trim())
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to execute process {fileName} {arguments}", ex);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message
            };
        }
    }

    public static string CleanErrorMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Strip CLIXML wrapper if present
        if (raw.Contains("#< CLIXML"))
        {
            try
            {
                var matches = Regex.Matches(raw, @"<S S=""Error"">(.*?)</S>", RegexOptions.Singleline);
                if (matches.Count > 0)
                {
                    var sb = new StringBuilder();
                    foreach (Match m in matches)
                    {
                        string line = m.Groups[1].Value
                            .Replace("_x000D__x000A_", "\n")
                            .Replace("_x000A_", "\n")
                            .Replace("_x000D_", "\r");
                        sb.Append(line);
                    }
                    string extracted = sb.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(extracted))
                        return extracted;
                }
            }
            catch { }
        }

        return raw.Trim();
    }
}
