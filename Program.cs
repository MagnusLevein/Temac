using System;
using System.Reflection;
using Temac.Environ;
using Temac.Tokenization;
using Temac.Interpretation;
using Temac.Errors;
using System.IO;
using System.Text;
using System.Collections.Generic;

// © Copyright 2022 Magnus Levein.
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
    enum ExitValues : int
    {
        Ok = 0,
        WarningOrInformation = 1,
        Error = 2,
        InternalError = 3,
        ArgumentError  = 4
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ExitValues exitValue = ExitValues.Ok;

        PrintCopyright();
        try
        {
            if (AnalyzeArguments(args, CompilerEnvironment.Instance))
                return (int)ExitValues.WarningOrInformation;

            if (!CompilerEnvironment.Instance.IsSetup)
                throw new TemacArgumentException("Input file name not given.");

            string mainOutputFilename = CompilerEnvironment.Instance.GenerateOutputFileName(CompilerEnvironment.Instance.MainInputFile, out string nakedName);

            var mainScope = new Scope(CompilerEnvironment.Instance.CommandLineParameter).GetNewInOutFileScope(CompilerEnvironment.Instance.MainInputFile, mainOutputFilename, nakedName);

            var inputDataBlock = Includer.IncludeFile(CompilerEnvironment.Instance.MainInputFile, new TemacTokenizer());

            var outputDataBlock = new OutputDataBlock(mainOutputFilename).OpenForWriting(append: false);

            Interpreter.Interpret(mainScope, inputDataBlock, outputDataBlock, null);
            if(Interpreter.StopAndTrace)
                Console.Error.WriteLine("Trace finished.");

            if (CompilerEnvironment.Instance.TraceError && Interpreter.StopAndTrace)
            {
                if (Interpreter.VariableDump != null)
                {
                    int columns = 80;
                    try
                    {
                        columns = Console.IsOutputRedirected ? 200 : Math.Max(Console.WindowWidth, 80);
                    }
                    catch { }
                    DataBlockDump.PrintDump(Interpreter.VariableDump, columns);
                }
            }

            inputDataBlock.Close();
            outputDataBlock.Close();

            if (ErrorHandler.Instance.HasError)
                exitValue = ExitValues.Error;
        }
        catch (TemacException e)
        {
            if (e is TemacInternalException te)
            {
                Console.Error.WriteLine("\nTemac internal error: {0}\nAborting.", te.Message);
                exitValue = ExitValues.InternalError;
            }
            else if (e is TemacArgumentException ae)
            {
                Console.Error.WriteLine("\nUse:\ntemac   [options ...]   inputFile  [outputPattern]\n");
                Console.Error.WriteLine("Argument error: " + ae.Message + "\n");
                Console.Error.WriteLine("Try temac -h for a list of valid options.");
                exitValue = ExitValues.ArgumentError;
            }
            else
            {
                Console.Error.WriteLine("\nTemac fatal error: {0}\nAborting.", e.Message);
                exitValue = ExitValues.Error;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("\nUncaught exception:\n{0}", e.Message);
            exitValue = ExitValues.InternalError;
        }

        if (CompilerEnvironment.Instance.ListFileUsage)
        {
            CompilerEnvironment.Instance.DumpFileUsageSummary();
        }

        // Maybe issue some warnings
        StringBuilder warnings = new StringBuilder();
        if (CompilerEnvironment.Instance.DebugNewlines && OutputDataBlock.HasWrittenAnything)
        {
            warnings.AppendLine("WARNING: Parameter -debug-newlines is given, which makes the output files soiled with end-of-line token information.");
        }
        if (ErrorHandler.Instance.HasError)
        {
            warnings.AppendLine("DO NOTE: One or more output files were not written to, because of compilation errors.");
        }
        else
        {
            foreach (var filename in OutputDataBlock.NontouchedFiles)
                warnings.AppendLine($"WARNING: File \'{filename}\' was never written to (no data), but there is an old file with that name.");
        }

        if (warnings.Length > 0)
        {
            Console.Error.Write("\n--------\n{0}--------\n", warnings.ToString());
            if (exitValue == ExitValues.Ok)
            {
                exitValue = ExitValues.WarningOrInformation;
            }
        }

        return (int)exitValue;
    }

    private enum NextArgument
    {
        Default = 0,
        Whilemax,
        Parameter
    }

    /// <summary>
    /// Reads command line parameters, and updates the compiler environment accordingly
    /// </summary>
    /// <returns>true if we shall quit with no errors</returns>
    static bool AnalyzeArguments(string[] args, CompilerEnvironment compilerEnvironment)
    {
        NextArgument next = NextArgument.Default;

        foreach (string arg in args)
        {
            bool argIsInt = Int32.TryParse(arg, out int argAsInt);

            switch (next)
            {
                case NextArgument.Whilemax:
                    if(!argIsInt)
                        throw new TemacArgumentException("Expected integer after -w, but got \'" + arg + "\'.");
                    compilerEnvironment.WhileMax = argAsInt;
                    next = NextArgument.Default;
                    continue;

                case NextArgument.Parameter:
                    compilerEnvironment.CommandLineParameter = arg;
                    next = NextArgument.Default;
                    continue;
            }

            switch (arg)
            {
                case "-?":
                case "-h":
                case "-help":
                    PrintHelp();
                    return true;

                case "-dn":
                case "-debug-newlines":
                    compilerEnvironment.DebugNewlines = true;
                    break;

                case "-t":
                case "-tokens":
                    compilerEnvironment.DumpTokens = true;
                    break;

                case "-f":
                case "-files":
                    compilerEnvironment.ListFileUsage = true;
                    break;

                case "-p":
                case "-parameter":
                    next = NextArgument.Parameter;
                    break;

                case "-s":
                case "-stop":
                    compilerEnvironment.TraceError = true;
                    break;

                case "-w":
                case "-whilemax":
                    next = NextArgument.Whilemax;
                    break;

                default:
                    if (arg[0] == '-')
                        throw new TemacArgumentException("Unknown option \'"+arg+"\'.");
                    if (compilerEnvironment.SetMainInputFile(arg))
                        break;
                    if (compilerEnvironment.SetOutputFilePattern(arg))
                        break;
                    throw new TemacArgumentException("What shall I do with \'"+arg+"\'?");
            }
        }
        return false;
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

    static void PrintHelp()
    {
        Console.Write("\nUse:\n");
        Console.Write("temac   [options ...]   inputFile  [outputPattern]\n\n");
        Console.Write("inputFile: a temac text file to read\n\n");
        Console.Write("outputPattern: pattern for construction of output filenames, with @ indicating it\'s base name. Defaults to @.html.\n\n");
        Console.Write("options:\n\n");

        Console.Write("-?, -h, -help\n");
        Console.Write("\tThis brief help.\n\n");

        Console.Write("-dn, -debug-newlines\n");
        Console.Write("\tPrint end-of-line token data in output files.\n");
        Console.Write("\tDo not use this in production!\n\n");

        Console.Write("-f, -files\n");
        Console.Write("\tList file usage summary to stdout.\n\n");

        Console.Write("-p text\n");
        Console.Write("-parameter text\n");
        Console.Write("\tSet variable $parameter to the specified text.\n\n");

        Console.Write("-s, -stop\n");
        Console.Write("\tStop on first error, and print stack trace and variable dump to stderr.\n\n");

        Console.Write("-t, -tokens\n");
        Console.Write("\tDump tokenization of read files to stdout.\n\n");

        Console.Write("-w limit\n-whilemax limit\n");
        Console.Write("\tSet max limit for while loops, defaults to 1000.\n\n");
    }
}
