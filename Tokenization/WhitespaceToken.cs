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
/// A token that represents leading whitespace (' ' and '\t')
/// </summary>
internal class WhitespaceToken : Token
{
    public int ColumnPositions { get; private set; }

    public string WhitespaceString { get; private set; }

    public override StructuralClass StructuralClass => StructuralClass.Blanks;

    public WhitespaceToken(Location location, int columnPositions, string blanks) : base(location)
    {
        ColumnPositions = columnPositions;
        WhitespaceString = blanks;
    }

    public override string ToDump(bool compact = false, bool _ = false)
    {
        if (compact)
            return " ";

        return String.Format("«{0} Whitespace {1} cols»", Location.GetLineColString(Location), ColumnPositions);
    }

    public override string ToString() => WhitespaceString;
}
