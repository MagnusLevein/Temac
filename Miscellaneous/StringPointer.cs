using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

namespace Temac.Miscellaneous;

/// <summary>
/// Simulates some aspects of a C string pointer
/// </summary>
internal class StringPointer
{
    string data;
    int baseIndex, length;

    public StringPointer(string text)
    {
        data = text;
        baseIndex = 0;
        length = text.Length;
    }

    /// <returns>null if end of string reached, otherwise character</returns>
    public char? this[int i]
    {
        get
        {
            if (baseIndex + i < length)
                return data[baseIndex + i];
            return null;
        }
    }

    /// <returns>true if possible, false if end of string</returns>
    public bool MovePtr(int delta = 1)
    {
        baseIndex += delta;
        if (baseIndex < 0)
        {
            baseIndex = 0;
        }
        return baseIndex + delta < length;
    }
}
