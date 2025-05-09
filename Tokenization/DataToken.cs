using System;
using System.Collections.Generic;
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

namespace Temac.Tokenization;

/// <summary>
/// A token that represents ordinary text
/// </summary>
internal class DataToken : Token
{
    private const int MaxBinaryDump = 80;

    string _data;
    bool _binary;

    public override StructuralClass StructuralClass => StructuralClass.Continuous;

    public DataToken(Location location, string data, bool isBinary = false) : base(location)
    {
        _data = data;
        _binary = isBinary;
    }

    public override string ToDump(bool _, bool fullBinary = false)
    {
        if(!_binary || _data.Length <= MaxBinaryDump || fullBinary)
            return _data;

        return _data.Substring(0, MaxBinaryDump/2) + "\u2026" + _data.Substring(_data.Length - MaxBinaryDump/2 -1, MaxBinaryDump/2);
    }

    public override string ToString()
    {
        return _data;
    }
}
