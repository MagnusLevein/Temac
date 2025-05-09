using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Tokenization;

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
/// A "black hole", always ready for read and write access. Always empty.
/// </summary>
sealed class NullDataBlock : DataBlock
{
    public NullDataBlock() : base("$null")
    {
    }

    public override DataBlock OpenForReading() => this;

    public override DataBlock SelectSpecificLine(int lineNumber) => this;

    public override DataBlock CountLines(out int lines)
    {
        lines = 0;
        return this;
    }

    public override DataBlock OpenForWriting(bool append) => this;

    public override Token? ReadNext(Token? untilToken = null) => null;

    public override DataBlock WriteNext(Token token) => this;

    public override DataBlock Close(bool makeReadOnly = false) => this;
}
