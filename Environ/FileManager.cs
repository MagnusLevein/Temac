using System;
using System.Collections.Generic;
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

/// <summary>
/// Keep track of read and written files. Stop writing to an input file.
/// </summary>
internal class FileManager
{
    private Dictionary<string, int> _readFiles, _writtenFiles;

    private List<string> _nontouchedFiles;

    private bool _hasWrittenAnything = false;

    public IReadOnlyList<string> NontouchedFiles => _nontouchedFiles;

    public bool HasWrittenAnything
    {
        get => _hasWrittenAnything;
        set
        {
            if (value != true)
                throw new NotSupportedException();
            _hasWrittenAnything = true;
        }
    }

    public FileManager()
    {
        _readFiles = new Dictionary<string, int>();
        _writtenFiles = new Dictionary<string, int>();
        _nontouchedFiles = new List<string>();
    }

    public void AddInputFile(string fileName)
    {
        if (_readFiles.ContainsKey(fileName))
        {
            _readFiles[fileName]++;
            return;
        }
        _readFiles.Add(fileName, 1);
    }

    public void AddOutputFile(string fileName)
    {
        if (_readFiles.ContainsKey(fileName))
        {
            throw new TemacException($"\'{fileName}\' is read as an input file. Writing to that file is not allowed.");
        }
        if (_writtenFiles.ContainsKey(fileName))
        {
            _writtenFiles[fileName]++;
            return;
        }
        _writtenFiles.Add(fileName, 1);
    }

    public void AddNonTouchedFile(string fileName)
    {
        _nontouchedFiles.Add(fileName);
    }

    public void DumpSummary()
    {
        int maxlen = 0;

        foreach (var file in _readFiles)
            maxlen = Math.Max(maxlen, file.Key.Length);
        foreach (var file in _writtenFiles)
            maxlen = Math.Max(maxlen, file.Key.Length);

        if (_readFiles.Count > 0)
        {
            Console.WriteLine("\n=== Read files ===");
            foreach (var file in _readFiles)
            {
                Console.WriteLine("{0}  (×{1})",file.Key.PadRight(maxlen), file.Value);
            }
        }

        if (_writtenFiles.Count > 0)
        {
            Console.WriteLine("\n=== Written files ===");
            foreach (var file in _writtenFiles)
            {
                Console.WriteLine("{0}  (×{1})", file.Key.PadRight(maxlen), file.Value);
            }
        }
    }
}
