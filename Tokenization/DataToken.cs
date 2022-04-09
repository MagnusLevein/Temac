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
/// A token that represents ordinary text
/// </summary>
internal class DataToken : Token
{
    string _data;
    public override StructuralClass StructuralClass => StructuralClass.Continuous;

    public DataToken(Location location, string data) : base(location)
    {
        _data = data;
    }

    public override string ToDump(bool _) => _data;

    public override string ToString()
    {
        return _data;
    }
}
