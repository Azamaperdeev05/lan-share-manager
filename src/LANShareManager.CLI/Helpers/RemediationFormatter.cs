using LANShareManager.Core.Diagnostics;
using LANShareManager.Core.Interfaces;

namespace LANShareManager.CLI.Helpers;

public static class RemediationFormatter
{
    public static void Print(RemediationReport report)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ ❌ ҚАТЕ АНЫҚТАЛҒАН ОРЫН (ТОЧКА СБОЯ): {report.FailurePoint}");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════════╣");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"║ 📌 Санаты: {report.CategoryName}");
        Console.WriteLine($"║ ⚠️  Атауы:   {report.ErrorTitle}");
        Console.ResetColor();

        Console.WriteLine("║");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("║ 🔍 НЕГЕ БҰЛ БОЛДЫ (ПРИЧИНА):");
        Console.ForegroundColor = ConsoleColor.Gray;
        foreach (var line in WrapText(report.CauseDescription, 78))
        {
            Console.WriteLine($"║    {line}");
        }
        Console.ResetColor();

        Console.WriteLine("║");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║ 💡 АЛДЫМЕН МЫНАНЫ ДҰРЫСТАҢЫЗ (СНАЧАЛА ИСПРАВЬТЕ ЭТО):");
        Console.ResetColor();

        for (int i = 0; i < report.RemediationSteps.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"║    [{i + 1}] ");
            Console.ForegroundColor = ConsoleColor.White;
            var stepLines = WrapText(report.RemediationSteps[i], 72);
            Console.WriteLine(stepLines[0]);
            for (int j = 1; j < stepLines.Count; j++)
            {
                Console.WriteLine($"║        {stepLines[j]}");
            }
        }
        Console.ResetColor();

        if (!string.IsNullOrWhiteSpace(report.QuickCommand))
        {
            Console.WriteLine("║");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("║ ⌨️  ШҰҒЫЛ ТҮЗЕТУ ПӘРМЕНІ (БЫСТРАЯ КОМАНДА):");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"║    {report.QuickCommand}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static async Task<bool> OfferAutoFixAsync(
        RemediationReport report,
        IFirewallService firewallService,
        INetworkService networkService,
        Func<Task> onLanmanServerFix)
    {
        if (!report.CanAutoFix) return false;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"🛠️  {report.AutoFixDescription}? [Y/n]: ");
        Console.ResetColor();

        string? answer = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (answer == "n" || answer == "no" || answer == "н" || answer == "нет")
        {
            Console.WriteLine("Автоматты түзету бас тартылды.");
            return false;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n[Авто-түзету басталды...]");
        Console.ResetColor();

        try
        {
            switch (report.CategoryKey)
            {
                case "LANMAN_SERVICE":
                    await onLanmanServerFix();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ 'LanmanServer' қызметі сәтті қосылды!");
                    Console.ResetColor();
                    return true;

                case "NETWORK_PUBLIC":
                    bool netOk = await networkService.SwitchNetworkToPrivateAsync();
                    if (netOk)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ Желі профилі 'Private' (Частная) күйіне сәтті ауыстырылды!");
                        Console.ResetColor();
                        return true;
                    }
                    break;

                case "FIREWALL_445":
                    bool fwOk = await firewallService.EnableSmbFirewallRulesAsync();
                    if (fwOk)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ Брандмауэр ережелері сәтті қосылып, SMB порты ашылды!");
                        Console.ResetColor();
                        return true;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Авто-түзету кезінде қате болды: {ex.Message}");
            Console.ResetColor();
        }

        return false;
    }

    private static List<string> WrapText(string text, int maxWidth)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        string[] words = text.Split(' ');
        var current = "";

        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 <= maxWidth)
            {
                current += (current.Length == 0 ? "" : " ") + word;
            }
            else
            {
                if (!string.IsNullOrEmpty(current)) result.Add(current);
                current = word;
            }
        }

        if (!string.IsNullOrEmpty(current)) result.Add(current);
        return result;
    }
}
