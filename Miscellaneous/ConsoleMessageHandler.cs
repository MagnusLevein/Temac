using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;

namespace Temac.Miscellaneous;

/// <summary>
/// Prints and erases the status message set with $status = ... during compilation.
/// </summary>
/// <remarks>
/// Call ConsoleMessageHandler.Instance.Reset() before printing to the console.
/// </remarks>
internal class ConsoleMessageHandler
{
    static private ConsoleMessageHandler _instance = new ConsoleMessageHandler();

    static public void Reinitialize()
    {
        _instance = new ConsoleMessageHandler();
    }

    static public ConsoleMessageHandler Instance => _instance;

    public int ScreenColumns { get; private set; }

    private int CurrentStatusLength = 0;

    private ConsoleMessageHandler()
    {
        int columns = 80;
        try
        {
            columns = Console.IsOutputRedirected ? 200 : Math.Max(Console.WindowWidth, 80);
        }
        catch { }
        ScreenColumns = columns;
    }

    public void SetStatusText(string message)
    {
        string status = StringWidth.Max(message, ScreenColumns - 1);
        Console.Write("\r" + StringWidth.Replaces(status, CurrentStatusLength));
        Console.Out.Flush();
        CurrentStatusLength = status.Length;
    }

    public void Reset(bool keepStatusText = false)
    {
        if (keepStatusText)
        {
            if (CurrentStatusLength > 0)
            {
                Console.WriteLine();
                Console.WriteLine();
            }
            CurrentStatusLength = 0;
            return;
        }
        SetStatusText("");
    }
}
