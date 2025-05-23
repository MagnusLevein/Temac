using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Errors;
using Temac.Interpretation;
using Temac.Miscellaneous;
using Temac.Tokenization;

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

internal class Compiler : IDisposable
{
    private static CompilerEnvironment? _environment;

    public static bool HasCompilerEnvironment => _environment != null;

    public bool PauseProjectBecauseOfWarnings { get; private set; } = false;

    public static CompilerEnvironment Environment
    {
        get
        {
            if (_environment == null)
                throw new TemacInternalException("Trying to use " + nameof(CompilerEnvironment) + " before it is initialized, or after it was disposed.");

            return _environment;
        }
    }

    private static int _instances = 0;

    public Compiler(Arguments arguments)
    {
        if (arguments.ModeOfOperation != Arguments.Mode.CompilerMode)
            throw new InvalidOperationException();

        if (Interlocked.Increment(ref _instances) > 1)
        {
            Interlocked.Decrement(ref _instances);
            throw new TemacInternalException("Only one " + nameof(Compiler) + " instance allowed at once.");
        }

        _environment = new CompilerEnvironment(arguments);
    }

    public void Dispose()
    {
        _environment = null;
        Interlocked.Decrement(ref _instances);

        DataBlock.Reinitialize();
        OutputDataBlock.Reinitialize();
        Interpreter.Reinitialize();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns>HasError </returns>
    /// <exception cref="TemacArgumentException"></exception>
    public void Compile(Location? location)
    {
        try
        {
            string mainOutputFilename = Environment.GenerateOutputFileName(Environment.MainInputFile, out string nakedName);

            var mainScope = new Scope(Environment.CommandLineParameter).GetNewInOutFileScope(Environment.MainInputFile, mainOutputFilename, nakedName);

            var inputDataBlock = Includer.IncludeFile(Environment.MainInputFile, new TemacTokenizer(), location);

            var outputDataBlock = new OutputDataBlock(mainOutputFilename).OpenForWriting(append: false);

            Interpreter.Interpret(mainScope, inputDataBlock, outputDataBlock, null);
            if (Interpreter.StopAndTrace)
                Console.Error.WriteLine("Trace finished.");

            if (Environment.TraceError && Interpreter.StopAndTrace)
            {
                if (Interpreter.VariableDump != null)
                {
                    DataBlockDump.PrintDump(Interpreter.VariableDump, ConsoleMessageHandler.Instance.ScreenColumns);
                }
            }

            inputDataBlock.Close();
            outputDataBlock.Close();
        }
        finally
        {
            ConsoleMessageHandler.Instance.Reset();

            if (Environment.ListFileUsage)
            {
                Environment.DumpFileUsageSummary();
            }

            // Maybe issue some warnings
            StringBuilder warnings = new StringBuilder();
            if (Environment.DebugNewlines && Environment.HasWrittenAnything)
            {
                warnings.AppendLine("WARNING: Parameter -debug-newlines is given, which makes the output files soiled with end-of-line token information.");
                PauseProjectBecauseOfWarnings = true;
            }
            if (ErrorHandler.Instance.HasError)
            {
                warnings.AppendLine("DO NOTE: One or more output files were not written to, because of compilation errors.");
            }
            else
            {
                foreach (var filename in Environment.NontouchedFiles)
                {
                    warnings.AppendLine($"WARNING: File \'{filename}\' was never written to (no data), but there is an old file with that name.");
                    PauseProjectBecauseOfWarnings = true;
                }
            }

            if (warnings.Length > 0)
            {
                Console.Error.Write("\n--------\n{0}--------\n", warnings.ToString());
                Program.SetMinimumExitValue(Program.ExitValues.WarningOrInformation);
            }

            Console.WriteLine();
        }
    }

    private enum NextArgument
    {
        Default = 0,
        Whilemax,
        Parameter
    }
}
