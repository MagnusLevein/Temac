using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;

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

namespace Temac.Environ;

class CompilerEnvironment
{
    private static CompilerEnvironment _instance = new CompilerEnvironment();

    public static CompilerEnvironment Instance => _instance;

    private string _mainInputFile = "";
    
    private string _outputFilePattern = "";

    private FileManager _fileManager;

    /// <summary>
    /// Is the compiler environment enough setup for run?
    /// </summary>
    public bool IsSetup => _mainInputFile != "";

    /// <summary>
    /// Filename of main input file set from command line (required)
    /// </summary>
    public string MainInputFile => _mainInputFile;

    /// <summary>
    /// Pattern for construction of output file names set from command line, defaults to "@.html"
    /// </summary>
    public string OutputFilePattern => _outputFilePattern != "" ? _outputFilePattern : "@.html";

    /// <summary>
    /// Option set from command line: dump tokens after every file inclusion
    /// </summary>
    public bool DumpTokens { get; set; } = false;

    /// <summary>
    /// Option set from command line: print EOL tokens in output files (DO NOT USE IN PRODUCTION)
    /// </summary>
    public bool DebugNewlines { get; set; } = false;

    /// <summary>
    /// Option set from command line: list file usage summary
    /// </summary>
    public bool ListFileUsage { get; set; } = false;

    /// <summary>
    /// Stop on first error, and print trace
    /// </summary>
    public bool TraceError { get; set; } = false;

    /// <summary>
    /// Option set from command line: maximum number of while turns in loop (defaults to 1000)
    /// </summary>
    public int WhileMax { get; set; } = 1000;

    
    public string CommandLineParameter { get; set; } = "";

    /// <summary>
    /// Set MainInputFile, which can only be done once.
    /// </summary>
    /// <returns>true if set, false if already set</returns>
    public bool SetMainInputFile(string filename)
    {
        if (_mainInputFile != "")
            return false;
        if (Path.GetFileName(filename) == "")
            throw new TemacArgumentException("Bad input file name.");
        _mainInputFile = filename;
        return true;
    }

    /// <summary>
    /// Set OutputFilePattern, which can only be done once.
    /// </summary>
    /// <returns>true if set, false if already done</returns>
    public bool SetOutputFilePattern(string pattern)
    {
        if (_outputFilePattern != "")
            return false;
        _outputFilePattern = pattern;
        return true;
    }

    private CompilerEnvironment()
    {
        _fileManager = new FileManager();
    }

    /// <summary>
    /// Typical OutputFilePattern values are like "@.html", "subdir", "subdir/", "subdir/@.html"
    /// </summary>
    /// <exception cref="TemacException"></exception>
    public string GenerateOutputFileName(string inputFileName, out string nakedName)
    {
        nakedName = Path.GetFileNameWithoutExtension(inputFileName);

        string generatedName;

        if (OutputFilePattern.IndexOf('@') >= 0)
            generatedName = OutputFilePattern.Replace("@", nakedName);
        else
            generatedName = Path.Join(OutputFilePattern, Path.GetFileName(inputFileName));

        if (Path.GetFileName(generatedName) != "")
            return generatedName;

        throw new TemacException("Could not generate output file name from \'"+ inputFileName +"\' with pattern \'"+ OutputFilePattern +"\'.");
    }

    /// <summary>
    /// Tests if a file name fullfills the output file pattern.
    /// </summary>
    /// <param name="outputFileName"></param>
    /// <returns>true indicates that the pattern was not fullfilled</returns>
    public bool IsUnsecureFileName(string outputFileName)
    {
        string generated = GenerateOutputFileName(outputFileName, out string _);
        return outputFileName != generated;
    }

    public void TrackReadFile(string inputFileName) => _fileManager.AddInputFile(inputFileName);

    public void TrackWrittenFile(string outputFileName) => _fileManager.AddOutputFile(outputFileName);

    public void DumpFileUsageSummary() => _fileManager.DumpSummary();
}
