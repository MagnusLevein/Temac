using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Miscellaneous;

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

public class DataBlockDump : IComparable<DataBlockDump>
{
    private string _variableName;
    private string _contentsDump;
    private string _sourceFiles;

    public bool IsPipe { get; set; } = false;
    public bool IsMacro { get; set; } = false;
    public bool IsReadonly { get; set; } = false;
    public bool IsReading { get; set; } = false;
    public bool IsWriting { get; set; } = false;
    public int ScopeNumber { get; set; } = 0;

    public string ExternalName
    {
        set
        {
            if(value != _variableName)
                _variableName = value + "\u2192" + _variableName;
        }
    }

    public DataBlockDump(string variableName, string contentsDump, string sourceFiles)
    {
        _variableName = variableName;
        _contentsDump = contentsDump;
        _sourceFiles = sourceFiles;
    }

    public static void PrintDump(IReadOnlyList<DataBlockDump> dumpList, int screenColumns)
    {
        Console.WriteLine("\nVariable dump:    %) pipe       R) reading    :) macro\n                  ×) read only  W) writing    \u00B6) newline\n");

        for (int i = 0, oldNumber = -1; i < dumpList.Count; i++)
        {
            if(i==0)
                dumpList[i].Print(screenColumns, printHeader: true);

            int delta = dumpList[i].ScopeNumber - oldNumber;
            if (delta > 0)
                Console.WriteLine((delta > 1 ? "sandbox=" : "").PadLeft(screenColumns - 1, delta > 1 ? '=' : i>0 ? '-' : '\u2500'));
            oldNumber = dumpList[i].ScopeNumber;
            dumpList[i].Print(screenColumns);
        }
        if(dumpList.Count > 0)
            Console.WriteLine("".PadRight(screenColumns - 1, '\u2500'));
        Console.WriteLine();
    }

    private void Print(int screenColumns, bool printHeader = false)
    {
        const int Blanks = 9;
        int nameWidth = (int) ((screenColumns-Blanks) * 0.20);
        int sourceWidth = (int)((screenColumns - Blanks) * 0.20);
        int contentsWidth = screenColumns - Blanks - nameWidth - sourceWidth;

        if (printHeader)
        {
            Console.WriteLine("    " + StringWidth.Fixed("Name", nameWidth) + "  " +
                      StringWidth.Fixed("Contents", contentsWidth) + "  " + StringWidth.Fixed("Source(s)", sourceWidth));
            return;
        }

        char type = IsPipe ? '%' : IsReadonly ? '×' : ' ';
        char status = IsWriting ? 'W' : IsReading ? 'R' : ' ';
        char macro = IsMacro ? ':' : ' ';

        Console.WriteLine($" {type}{status}{macro}" + StringWidth.Fixed(_variableName, nameWidth) + "  " + 
                          StringWidth.Fixed(_contentsDump, contentsWidth) + "  " + StringWidth.Fixed(_sourceFiles, sourceWidth));
    }
        
    public int CompareTo(DataBlockDump? other)
    {
        if(other == null)
            throw new NotSupportedException();

        if(this.ScopeNumber != other.ScopeNumber)
            return this.ScopeNumber - other.ScopeNumber;

        if(this._variableName[0] == '$' && other._variableName[0] != '$')
            return 1;

        if (this._variableName[0] != '$' && other._variableName[0] == '$')
            return -1;

        if (this.IsPipe && !other.IsPipe)
            return 1;

        if (!this.IsPipe && other.IsPipe)
            return -1;

        return this._variableName.CompareTo(other._variableName);
    }
}
