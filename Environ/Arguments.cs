using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;

namespace Temac.Environ;

/// <summary>
/// Reads and keeps comman line parameters
/// </summary>
class Arguments
{
    public enum Mode
    {
        Undefined = 0,
        CompilerMode,
        ProjectManagerMode
    }

    public Mode ModeOfOperation { get; private set; } = Mode.Undefined;

    /// <summary>
    /// Dump tokens after every file inclusion
    /// </summary>
    public bool DumpTokens { get; private set; } = false;

    /// <summary>
    /// Print EOL tokens in output files (DO NOT USE IN PRODUCTION)
    /// </summary>
    public bool DebugNewlines { get; private set; } = false;

    /// <summary>
    /// List file usage summary
    /// </summary>
    public bool ListFileUsage { get; private set; } = false;

    /// <summary>
    /// Stop on first error, and print trace
    /// </summary>
    public bool TraceError { get; private set; } = false;

    /// <summary>
    /// Enables detailed output for debugging and analysis.
    /// Only used in ProjectManagerMode.
    /// </summary>
    public bool Verbose { get; private set; } = false;

    /// <summary>
    /// Maximum number of while turns in loop (defaults to 1000)
    /// </summary>
    public int WhileMax { get; private set; } = 1000;

    /// <summary>
    /// Value for '$parameter'
    /// </summary>
    public string CommandLineParameter { get; private set; } = "";

    /// <summary>
    /// Filename of input file set from command line (required)
    /// </summary>
    public string InputFileName { get; private set; } = "";

    /// <summary>
    /// Pattern for construction of output file names as set from command line.
    /// In CompilerEnvironment a default value is applied if not set from command line.
    /// </summary>
    public string OutputFilePattern { get; private set; } = "";


    private enum NextArgument
    {
        Default = 0,
        Whilemax,
        Parameter
    }

    private Arguments()
    {
    }

    public Arguments(Location? location, string[] args) : this(new Arguments(), location, args)
    {
    }

    public Arguments(Arguments inheritedArguments, Location? location, string[] args)
    {
        DumpTokens = inheritedArguments.DumpTokens;
        DebugNewlines = inheritedArguments.DebugNewlines;
        ListFileUsage = inheritedArguments.ListFileUsage;
        TraceError = inheritedArguments.TraceError;
        Verbose = inheritedArguments.Verbose;
        WhileMax = inheritedArguments.WhileMax;
        CommandLineParameter = inheritedArguments.CommandLineParameter;

        NextArgument next = NextArgument.Default;
        bool verboseExlpicitSet = false;

        foreach (string arg in args)
        {
            bool argIsInt = Int32.TryParse(arg, out int argAsInt);

            switch (next)
            {
                case NextArgument.Whilemax:
                    if (!argIsInt)
                        throw new TemacArgumentException(location, "Expected integer after -w, but got \'" + arg + "\'.");
                    WhileMax = argAsInt;
                    next = NextArgument.Default;
                    continue;

                case NextArgument.Parameter:
                    CommandLineParameter = arg;
                    next = NextArgument.Default;
                    continue;
            }

            switch (arg)
            {
                case "-?":
                case "-h":
                case "-help":
                    throw new TemacArgumentException(location, "Parameter -help is only valid from the command line, not from a project file.");

                case "-dn":
                case "-debug-newlines":
                    DebugNewlines = true;
                    break;

                case "-t":
                case "-tokens":
                    DumpTokens = true;
                    break;

                case "-f":
                case "-files":
                    ListFileUsage = true;
                    break;

                case "-p":
                case "-parameter":
                    next = NextArgument.Parameter;
                    break;

                case "-s":
                case "-stop":
                    TraceError = true;
                    break;

                case "-v":
                case "-verbose":
                    Verbose = true;
                    verboseExlpicitSet = true;
                    break;

                case "-w":
                case "-whilemax":
                    next = NextArgument.Whilemax;
                    break;

                default:
                    if (arg[0] == '-')
                        throw new TemacArgumentException(location, "Unknown option \'" + arg + "\'.");
                    if (SetInputFile(location, arg))
                        break;
                    if (ModeOfOperation == Mode.CompilerMode && SetOutputFilePattern(arg))
                        break;
                    throw new TemacArgumentException(location, "What shall I do with \'" + arg + "\'?");
            }
        }

        if(ModeOfOperation == Mode.CompilerMode && verboseExlpicitSet)
            Console.Error.WriteLine("Flag -verbose ignored, as it is only used with .temacproj files.\n");

        if (String.IsNullOrEmpty(InputFileName))
            throw new TemacArgumentException(location, "Input file name not given.");
    }

    /// <summary>
    /// Set InputFile, which can only be done once.
    /// </summary>
    /// <returns>true if set, false if already set</returns>
    private bool SetInputFile(Location? location, string filename)
    {
        if (InputFileName != "")
            return false;

        if (Path.GetFileName(filename) == "")
            throw new TemacArgumentException(location, "Bad input file name.");

        ModeOfOperation = filename.ToLowerInvariant().EndsWith(".temacproj") ? Mode.ProjectManagerMode : Mode.CompilerMode;
        InputFileName = filename;
        return true;
    }

    /// <summary>
    /// Set OutputFilePattern, which can only be done once (and is only relevant in CompilerMode).
    /// </summary>
    /// <returns>true if set, false if already done</returns>
    private bool SetOutputFilePattern(string pattern)
    {
        if (OutputFilePattern != "")
            return false;

        OutputFilePattern = pattern;
        return true;
    }
}
