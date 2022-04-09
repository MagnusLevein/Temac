using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

namespace Temac.Errors;

/// <summary>
/// Helps backtracking an error, by keeping location information on every token.
/// </summary>
public class Location
{
    public string Filename { get; private set; }

    public int LineNo { get; private set; }

    public int ColumnNo { get; private set; }

    public Location(string filename, int lineNo, int colNo)
    {
        Filename = filename;
        LineNo = lineNo;
        ColumnNo = colNo;
    }

    public virtual Location GetRelativeLocation(int columnSkip)
    {
        return new Location(Filename, LineNo, ColumnNo + columnSkip);
    }

    public static string GetLineColString(Location? location)
    {
        if (location == null || location.LineNo == 0)
            return "?:?";

        return string.Format("{0}:{1}", location.LineNo, location.ColumnNo);
    }

    public static string GetLocationString(Location? location)
    {
        if (location == null)
            return "";

        if (location is CompilerLocation cloc)
            return cloc.GetLocationString();

        return string.Format("{0} ({1}:{2})", location.Filename, location.LineNo, location.ColumnNo);

    }

}

internal class CompilerLocation : Location
{
    public CompilerLocation() : base("",0,0)
    {
    }

    public override Location GetRelativeLocation(int columnSkip) => throw new NotSupportedException();

    public string GetLocationString()
    {
        return "compiler generated";
    }
}

