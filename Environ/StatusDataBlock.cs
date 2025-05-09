using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;
using Temac.Miscellaneous;
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
/// Write to the STATUS data block to set a progress status message during
/// compilation.
/// </summary>
sealed class StatusDataBlock : DataBlock
{
    public StatusDataBlock() : base("$status")
    {
    }

    public override DataBlock Close(bool makeReadOnly = false)
    {
        if (makeReadOnly)
            throw new TemacInternalException("Unsupported operation.");

        StringBuilder sb = new StringBuilder();
        foreach (var token in _tokens)
        {
            if (token is WhitespaceToken)
                continue;
            if (token is EndOfLineToken eolt)
            {
                sb.Append(" ");
            }
            else
            {
                if (token is CodeToken)
                    ErrorHandler.Instance.Error("Expected plain text, but found unprocessed Temac code in \'" + Name + "\'.");
                else
                    sb.Append(token.ToString());
            }
        }

        ConsoleMessageHandler.Instance.SetStatusText(sb.ToString().Trim());
        base.Close();
        return this;
    }
}
