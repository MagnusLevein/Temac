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

internal class CodeToken : Token
{
    public Instruction Instruction { get; private set; }

    public VariableParameter[] Parameters { get; private set; }

    /// <summary>
    /// Used for if/else/switch/case/while and other block commands to hold information on where the next instuction on same block level is.
    /// </summary>
    public Token? NextStructuralSibling { get; private set; } = null;

    public override StructuralClass StructuralClass => Instruction.GetStructuralClass();

    public override bool IsFinalized => base.IsFinalized && (NextStructuralSibling != null || !StructuralClass.SiblingRequired());

    public CodeToken(Instruction instruction, VariableParameter[] parameters) : base()
    {
        Instruction = instruction;
        Parameters = parameters;
    }

    public void SetNextSibling(Token nextToken)
    {
        if (NextStructuralSibling != null || !StructuralClass.SiblingRequired())
            throw new TemacInternalException("Cannot modify token.");

        if (nextToken == this)
            throw new TemacInternalException("Cannot link token to itself.");

        NextStructuralSibling = nextToken;
    }

    public override string ToDump(bool compact = false, bool _ = false)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append('«');
        if (!compact)
            sb.Append(Location.GetLineColString(Location) + " ");

        sb.AppendFormat(Instruction.ToString());

        foreach (var parameter in Parameters)
            sb.AppendFormat(" {0}", parameter.Dump());

        if (!compact && NextStructuralSibling != null)
            sb.AppendFormat(" \u25BA{0}", Location.GetLineColString(NextStructuralSibling.Location));

        sb.Append("»");

        return sb.ToString();
    }

    public override string ToString()
    {
        return "{" + Instruction.ToString() + "}";
    }
}
