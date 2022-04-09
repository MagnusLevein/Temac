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
/// A chunk of analyzed text, splitted up in different kind of tokens: CodeToken, DataToken, WhitespaceToken, EnfOfLineToken
/// </summary>
public abstract class Token
{
    /// <summary>
    /// Token's start location in source file
    /// </summary>
    public Location? Location { get; private set; }

    public abstract StructuralClass StructuralClass { get; }

    public virtual bool IsFinalized => Location != null;

    /// <summary>
    /// Analyzes if two tokens seems to come from the same input file line
    /// </summary>
    /// <returns>
    /// true if they seems to come from the same input line,
    /// false if the seems to come from different input lines,
    /// null if location information is not well defined
    /// </returns>
    public static bool? IsSameLine(Token? first, Token? second)
    {
        if (first == null || second == null)
            return null;

        if (first.Location == null || second.Location == null)
            return null;

        if (first.Location is CompilerLocation || second.Location is CompilerLocation)
            return null;

        if(first.Location.Filename != second.Location.Filename)
            return false;

        if(first.Location.LineNo != second.Location.LineNo)
            return false;

        return true;
    }

    protected Token()
    {
        Location = null;
    }

    public Token(Location location)
    {
        Location = location;
    }

    public void SetLocation(Location location)
    {
        if (this.Location == null)
            this.Location = location;
        else
            throw new TemacInternalException("Cannot modify token.");
    }

    public abstract string ToDump(bool compact = false);
}
