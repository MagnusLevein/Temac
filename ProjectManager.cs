using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Errors;
using Temac.Miscellaneous;

namespace Temac;

internal class ProjectManager
{
    private Arguments _arguments;

    private string _initialDirectory;

    private static bool _stoppingBecauseOfErrors = false;

    private static int _deep = 0;

    public bool PauseProjectBecauseOfWarnings { get; private set; } = false;

    static public void Reinitialize()
    {
        _stoppingBecauseOfErrors = false;
        _deep = 0;
    }

    public ProjectManager(Arguments arguments)
    {
        if (arguments.ModeOfOperation != Arguments.Mode.ProjectManagerMode)
            throw new InvalidOperationException();

        _deep++;
        _arguments = arguments;
        _initialDirectory = Directory.GetCurrentDirectory();
    }

    private void RestoreDirectory()
    {
        if (!_stoppingBecauseOfErrors)
            PrintIfVerbose($"cd {QuoteIfNeccesary(_initialDirectory)}");
        Directory.SetCurrentDirectory(_initialDirectory);
    }

    private string QuoteIfNeccesary(string txt)
    {
        if (txt.Contains(' '))
            return '\"' + txt + '\"';
        return txt;
    }

    public void Manage(Location? location)
    {
        try
        {
            string fullname = Path.GetFullPath(_arguments.InputFileName);
            string workdir = Path.GetDirectoryName(fullname)!;

            PrintIfVerbose($"cd {QuoteIfNeccesary(workdir)}");
            Directory.SetCurrentDirectory(workdir);

            int lineNo = 0;
            foreach (string currentLine in File.ReadLines(fullname))
            {
                lineNo++;
                location = new Location(fullname, lineNo, 1);
                string trimmed = currentLine.Replace('\t', ' ').Trim();

                // Empty line or comment
                if (trimmed == "" || trimmed.StartsWith("#"))
                    continue;

                if (PauseProjectBecauseOfWarnings)
                {
                    ConsoleMessageHandler.Instance.Reset(false);
                    Console.Write(">> Press any key to confirm");
                    Console.ReadKey(true);
                    Console.Write("\r                           \r");
                    PauseProjectBecauseOfWarnings = false;
                }

                string lowercase = trimmed.ToLowerInvariant();

                // 'echo' - prints a text
                if (lowercase == "echo" || lowercase == "echo.")
                {
                    ConsoleMessageHandler.Instance.Reset(false);
                    Console.WriteLine();
                    continue;
                } else if (lowercase.StartsWith("echo "))
                {
                    ConsoleMessageHandler.Instance.Reset(false);
                    Console.WriteLine(trimmed.Substring(5).Trim());
                    continue;
                }

                // 'temac' - compiles a Temac file, or processes a .temacproj project file
                if (lowercase.StartsWith("temac "))
                {
                    PrintIfVerbose(trimmed);

                    var commandArguments = new Arguments(_arguments, location, TurnToArgumentArray(trimmed.Substring(6)));

                    switch (commandArguments.ModeOfOperation)
                    {
                        case Arguments.Mode.CompilerMode:
                            using (var compiler = new Compiler(commandArguments))
                            {
                                compiler.Compile(location);
                                PauseProjectBecauseOfWarnings = compiler.PauseProjectBecauseOfWarnings;
                            }
                            break;

                        case Arguments.Mode.ProjectManagerMode:
                            if (_deep < 10)
                            {
                                var manager = new ProjectManager(commandArguments);
                                manager.Manage(location);
                                PauseProjectBecauseOfWarnings = manager.PauseProjectBecauseOfWarnings;
                            }
                            else
                                ErrorHandler.Instance.Error("Temac project files nested too deep. Ignoring this one.", location);
                            break;
                    }

                    if (ErrorHandler.Instance.HasError)
                    {
                        PrintIfVerbose("Stopping because of errors.");
                        _stoppingBecauseOfErrors = true;
                        RestoreDirectory();
                        return;
                    }

                    continue;
                }

                ErrorHandler.Instance.Error("Invalid command in Temac project file. Aborting.", new Location(fullname, lineNo, 1), doNotFilter: true);
                RestoreDirectory();
                return;
            }
        }
        catch (IOException e)
        {
            ErrorHandler.Instance.Error(e.Message + " Aborting.", location, doNotFilter: true);
        }
        finally
        {
            RestoreDirectory();
        }
    }

    /// <summary>
    /// Scans a command line and turns it to an argument array. Quotation with double quotes (") is supported.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// -f -s input.temac => { "-f", "-s", "input.temac" }
    /// -f "special file with ""quotations"".txt" => { "-f" , "special file with \"quotations\".txt" }
    /// </remarks>
    private string[] TurnToArgumentArray(string commandLine)
    {
        List<string> arguments = new();
        StringBuilder currentArgument = new();

        bool inQuote = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char current = commandLine[i];
            char next = i + 1 < commandLine.Length ? commandLine[i + 1] : '\0';

            if (current == '\"')
            {
                if (inQuote)
                {
                    if (next == '\"')
                    {
                        currentArgument.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
                else
                {
                    inQuote = true;
                }
                continue;
            }

            if (Char.IsWhiteSpace(current) && !inQuote)
            {
                if (currentArgument.Length == 0)
                    continue;

                arguments.Add(currentArgument.ToString());
                currentArgument = new();
            }
            else
            {
                currentArgument.Append(current);
            }        
        }

        if(currentArgument.Length > 0)
            arguments.Add(currentArgument.ToString());

        return arguments.ToArray();
    }

    private void PrintIfVerbose(string message)
    {
        if (!_arguments.Verbose)
            return;

        ConsoleMessageHandler.Instance.Reset(false);
        Console.WriteLine(message);
    }
}
