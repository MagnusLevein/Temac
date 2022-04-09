using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;
using Temac.Tokenization;

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

namespace Temac.Environ;

/// <summary>
/// Write to the ERR data block to rise an user defined error.
/// </summary>
sealed class ErrDataBlock : DataBlock
{
    public ErrDataBlock() : base("$err")
    {
    }

    public override DataBlock OpenForReading()
    {
        ErrorHandler.Instance.Error("Data block \'" + Name + "\' is writeonly.");
        return SystemBlockNULL;
    }

    public override DataBlock Close(bool makeReadOnly = false)
    {
        if (makeReadOnly)
            throw new TemacInternalException("Unsupported operation.");

        StringBuilder sb = new StringBuilder();
        foreach (var token in _tokens)
        {
            if (token is CodeToken)
                ErrorHandler.Instance.Error("Expected plain text, but found unprocessed Temac code in \'" + Name + "\'.");
            else
                sb.Append(token.ToString());
        }

        ErrorHandler.Instance.Error("User defined error condition:\n" + sb.ToString());
        base.Close();
        return this;
    }
}
