using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;
using Temac.Interpretation;

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

namespace Temac.Tokenization;

/// <summary>
/// A token that represents Enf of line, 'line-feed'
/// </summary>
class EndOfLineToken : Token
{
    public EndOfLineKind Kind { get; private set; }

    public EndOfLineToken(Location location, EndOfLineKind kind) : base(location)
    {
        Kind = kind;
    }

    public override StructuralClass StructuralClass => StructuralClass.EndOfLine;

    public override string ToDump(bool compact = false)
    {
        if (compact)
            return "\u00b6";

        return String.Format("«{0} EOL{1}»", Location.GetLineColString(Location), Kind != EndOfLineKind.Default ? "-" + Kind.ToString() : "");
    }

    public string ToDebug()
    {
        return String.Format("«EOL{1}: {0}»", Location.GetLocationString(Location), Kind != EndOfLineKind.Default ? "-" + Kind.ToString() : "");
    }

    public override string ToString()
    {
        return System.Environment.NewLine;
    }

}

enum EndOfLineKind
{
    Default = 0,
    Hidden,
    Explicit
}
