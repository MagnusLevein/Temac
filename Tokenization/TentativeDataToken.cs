using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;

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
/// ContextualDataTokens are of either kind "Begin" or "End". If a Begin and an End comes together (in this order), both of them will disappear.
/// That magic happens in DataBlock.WriteNext().
/// </summary>
internal class TentativeDataToken : DataToken
{
    private Kind _kind;
    private string _contextName;

    public bool IsBeginKind => _kind == Kind.Begin;

    public bool IsEndKind => _kind == Kind.End;

    public enum Kind { Begin, End };

    public TentativeDataToken(Location location, string data, Kind kind, string contextName) : base(location, data)
    {
        _kind = kind;
        _contextName = contextName;
    }

    public static bool Matching(TentativeDataToken beginning, TentativeDataToken ending)
    {
        if (!beginning.IsBeginKind || !ending.IsEndKind)
            throw new ArgumentException();

        return beginning._contextName == ending._contextName;
    }
}
