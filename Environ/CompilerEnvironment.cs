using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;
using Temac.Interpretation;

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
    private Arguments _arguments;

    private FileManager _fileManager;

    public bool HasWrittenAnything => _fileManager.HasWrittenAnything;

    public IReadOnlyList<string> NontouchedFiles => _fileManager.NontouchedFiles;

    /// <summary>
    /// Filename of main input file set from command line (required)
    /// </summary>
    public string MainInputFile => _arguments.InputFileName;

    /// <summary>
    /// Pattern for construction of output file names set from command line, defaults to "@.html"
    /// </summary>
    public string OutputFilePattern => _arguments.OutputFilePattern != "" ? _arguments.OutputFilePattern : "@.html";

    public bool DumpTokens => _arguments.DumpTokens;
    public bool DebugNewlines => _arguments.DebugNewlines;
    public bool ListFileUsage => _arguments.ListFileUsage;
    public bool TraceError => _arguments.TraceError;
    public int WhileMax => _arguments.WhileMax;
    public string CommandLineParameter => _arguments.CommandLineParameter;


    public CompilerEnvironment(Arguments arguments)
    {
        _arguments = arguments;
        _fileManager = new FileManager();
    }

    /// <summary>
    /// Typical OutputFilePattern values are like "@.html", "filename.ext", "subdir", "subdir/", "subdir/@.html"
    /// </summary>
    /// <exception cref="TemacException"></exception>
    public string GenerateOutputFileName(string inputFileName, out string nakedName)
    {
        nakedName = Path.GetFileNameWithoutExtension(inputFileName);

        string generatedName;

        if (OutputFilePattern.IndexOf('@') >= 0)
            generatedName = OutputFilePattern.Replace("@", nakedName);
        else
        {
            if (Path.GetExtension(OutputFilePattern).Length > 0)
                generatedName = OutputFilePattern;
            else
                generatedName = Path.Join(OutputFilePattern, Path.GetFileName(inputFileName));
        }

        if (Path.GetFileName(generatedName) != "")
            return generatedName;

        throw new TemacException("Could not generate output file name from \'"+ inputFileName +"\' with pattern \'"+ OutputFilePattern +"\'.");
    }

    /// <summary>
    /// From a given proposed output filename, tries to construct a filename that fullfills the output file pattern.
    /// </summary>
    /// <param name="proposedOutputFilename">wanted filename, e.g., document.htm</param>
    /// <param name="secureOutputFilename">when true is returned, a fullpath filename that fullfills the output file pattern</param>
    /// <returns>false indicates that the pattern was not fullfilled, and that secureOutputFilename is a filename suggestion to consider</returns>
    public bool MakeSecureFileName(string proposedOutputFilename, out string secureOutputFilename)
    {
        string generated = GenerateOutputFileName(proposedOutputFilename, out string _);
        if (generated == proposedOutputFilename)
        {
            secureOutputFilename = proposedOutputFilename;
            return true;
        }
        if (Path.GetFileName(generated) == proposedOutputFilename)
        {
            secureOutputFilename = generated;
            return true;
        }
        secureOutputFilename = Path.GetFileName(generated);
        return false;
    }

    public void TrackReadFile(string inputFileName) => _fileManager.AddInputFile(inputFileName);

    public void TrackWrittenFile(string outputFileName)
    {
        _fileManager.AddOutputFile(outputFileName);
        _fileManager.HasWrittenAnything = true;
    }

    public void TrackNonTouchedFile(string filename) => _fileManager.AddNonTouchedFile(filename);

    public void DumpFileUsageSummary() => _fileManager.DumpSummary();
}
