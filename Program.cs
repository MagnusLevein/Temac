using System;
using System.Reflection;
using Temac.Environ;
using Temac.Errors;
using System.Text;
using Temac.Miscellaneous;
using System.Linq;

// © Copyright 2022-2025 Magnus Levein.
// This file is part of Temac, Text Manuscript Compiler.
//
// Temac is free software: you can redistribute it and/or modify it under the
// terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version.
//
// Temac is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU General Public License for more
// details.
//
// You should have received a copy of the GNU General Public License along
// with Temac. If not, see <https://www.gnu.org/licenses/>.

namespace Temac;

class Program
{
    public enum ExitValues : int
    {
        Ok = 0,
        WarningOrInformation = 1,
        Error = 2,
        InternalError = 3,
        ArgumentError  = 4
    }
    static ExitValues ExitValue = ExitValues.Ok;

    enum ProgramCompletionState
    {
        ExitImmediately,
        ExitAfterPause,
        OfferRestart,
        RestartAccepted,
        RestartDeclined
    }

    static ProgramCompletionState CompletionState = ProgramCompletionState.ExitImmediately;

    public static void SetMinimumExitValue(ExitValues value)
    {
        if (value > ExitValue)
            ExitValue = value;
    }

    private static void MustNotRestart()
    {
        if (CompletionState != ProgramCompletionState.ExitImmediately)
            CompletionState = ProgramCompletionState.ExitAfterPause;
    }

    static int Main(string[] args)
    {
        string[] helpOptions = { "-?", "-h", "-help" };

        Console.OutputEncoding = Encoding.UTF8;

        do
        {
            PrintCopyright();

            if (args.Any(arg => helpOptions.Contains(arg)))
            {
                PrintHelp();
                return (int)ExitValues.WarningOrInformation;
            }

            try
            {
                var arguments = new Arguments(null, args);
                switch (arguments.ModeOfOperation)
                {
                    case Arguments.Mode.CompilerMode:
                        using (var compiler = new Compiler(arguments))
                        {
                            compiler.Compile(null);
                        }
                        break;

                    case Arguments.Mode.ProjectManagerMode:
                        CompletionState = ProgramCompletionState.OfferRestart;

                        new ProjectManager(arguments).Manage(null);

                        if (ErrorHandler.Instance.HasError)
                        {
                            ConsoleMessageHandler.Instance.Reset(true);
                            Console.Error.WriteLine();
                            Console.Error.WriteLine("***                   ***");
                            Console.Error.WriteLine("*** Compilation error ***");
                            Console.Error.WriteLine("***                   ***");
                        }
                        break;
                }

                if (ErrorHandler.Instance.HasError)
                    SetMinimumExitValue(ExitValues.Error);
            }
            catch (TemacException e)
            {
                ConsoleMessageHandler.Instance.Reset(true);
                if (e is TemacInternalException te)
                {
                    Console.Error.WriteLine("\nTemac internal error: {0}\nAborting.", te.Message);
                    ExitValue = ExitValues.InternalError;
                    MustNotRestart();
                }
                else if (e is TemacArgumentException ae)
                {
                    ErrorHandler.Instance.Error(ae);
                    PrintUsageSummay();
                    Console.Error.WriteLine("Try temac -h for a list of valid options.");
                    ExitValue = ExitValues.ArgumentError;
                }
                else
                {
                    Console.Error.WriteLine("\nTemac fatal error: {0}\nAborting.", e.Message);
                    ExitValue = ExitValues.Error;
                    MustNotRestart();
                }
            }
            catch (Exception e)
            {
                ConsoleMessageHandler.Instance.Reset(true);
                Console.Error.WriteLine("\nUncaught exception:\n{0}", e.Message);
                ExitValue = ExitValues.InternalError;
                MustNotRestart();
            }

            if(CompletionState == ProgramCompletionState.ExitImmediately)
                return (int)ExitValue;
            else if (CompletionState == ProgramCompletionState.ExitAfterPause)
            {
                ConsoleMessageHandler.Instance.Reset(false);
                Console.WriteLine("\nPress any key to confirm and quit.");
                Console.ReadKey(true);
                return (int)ExitValue;
            } else {
                bool defaultQuit = ExitValue <= ExitValues.WarningOrInformation;

                if (ExitValue == ExitValues.Ok)
                {
                    ConsoleMessageHandler.Instance.Reset(false);
                    Console.WriteLine("~~~ Done ~~~");
                }

                Console.WriteLine();

                // For successful compilation or ordinary compilation errors, we offer to run again

                if (defaultQuit)
                    Console.Write("Press R to run again, or [Q] to quit.");
                else
                    Console.Write("Press [R] to run again, or Q to quit.");

                do
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    CompletionState = key switch
                    {
                        ConsoleKey.Enter => defaultQuit ? ProgramCompletionState.RestartDeclined : ProgramCompletionState.RestartAccepted,
                        ConsoleKey.Spacebar => defaultQuit ? ProgramCompletionState.RestartDeclined : ProgramCompletionState.RestartAccepted,
                        ConsoleKey.R => ProgramCompletionState.RestartAccepted,
                        ConsoleKey.Q => ProgramCompletionState.RestartDeclined,
                        ConsoleKey.Escape => ProgramCompletionState.RestartDeclined,
                        _ => ProgramCompletionState.OfferRestart
                    };
                } while (CompletionState == ProgramCompletionState.OfferRestart);
                Console.WriteLine();
            }

            if (CompletionState != ProgramCompletionState.RestartAccepted)
                return (int)ExitValue;

            // Run again. Reinitialize.
            ErrorHandler.Reinitialize();
            ProjectManager.Reinitialize();
            ExitValue = ExitValues.Ok;
            CompletionState = ProgramCompletionState.ExitImmediately;
            Console.Clear();
            ConsoleMessageHandler.Reinitialize();
        } while (true);
    }

    static void PrintCopyright()
    {
        string versionstring = "";
        Version? version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
            versionstring = String.Format(" version {0}", version.ToString(3));
        Console.WriteLine("Temac - Text Manuscript Compiler{0} Copyright © 2022–2025 Magnus Levein.", versionstring);
        Console.WriteLine("Temac comes with ABSOLUTELY NO WARRANTY. This is free software, and you are welcome to redistribute it under certain");
        Console.WriteLine("conditions. See GNU General Public License (version 3) for details.");
        Console.WriteLine();
    }

    static void PrintUsageSummay()
    {
        Console.Write("\nUse:\n");
        Console.Write("temac  [compilerOptions...]  inputFile              [outputPattern]\n");
        Console.Write("temac  [projectOptions...]   projectFile.temacproj\n");
        Console.Write("temac  -? | -h | -help\n\n");
    }

    static void PrintHelp()
    {
        PrintUsageSummay();

        Console.Write("inputFile     – A Temac script file to compile.\n");
        Console.Write("outputPattern – A pattern for construction of output filenames. Use @ to represent the base name. Defaults to @.html.\n");
        Console.Write("projectFile   – A Temac project file (.temacproj) to process.\n\n");
        Console.Write("Options:\n\n");

        Console.Write("-?, -h, -help\n");
        Console.Write("\tThis brief help about command syntax and available options.\n\n");

        Console.Write("-dn, -debug-newlines\n");
        Console.Write("\tOutput end-of-line token data in compiled files. Not for production use!\n\n");

        Console.Write("-f, -files\n");
        Console.Write("\tPrint a summary of file usage to stdout.\n\n");

        Console.Write("-p text\n");
        Console.Write("-parameter text\n");
        Console.Write("\tAssign the specified text to the $parameter variable.\n\n");

        Console.Write("-s, -stop\n");
        Console.Write("\tStop execution on the first error. Prints a stack trace to stderr and a variable dump to stdout.\n\n");

        Console.Write("-v, -verbose\n");
        Console.Write("\tEnable verbose output when processing a Temac project file.\n\n");

        Console.Write("-t, -tokens\n");
        Console.Write("\tDump tokenization of read files to stdout.\n\n");

        Console.Write("-w limit\n-whilemax limit\n");
        Console.Write("\tSet the maximum number of iterations for while loops. If not specified, the default limit is 1000.\n\n");
    }
}
