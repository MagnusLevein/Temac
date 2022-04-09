using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Temac.Errors;

/// <summary>
/// All exceptions indicating compiler internal errors shall be derived from TemacInternalException
/// </summary>
public class TemacInternalException : TemacException
{
    public TemacInternalException(string message) : base(message)
    {
    }
}

internal class WrongNumberOfArgumentsInternalException : TemacInternalException
{
    public WrongNumberOfArgumentsInternalException(CodeToken cToken)
        : base(Location.GetLocationString(cToken.Location) + "  " + cToken.Instruction + ": Wrong number of arguments.")
    { 
    }
}

/// <summary>
/// Do note that this exception is for internal errors,
/// i.e. there must be some internal logic failure if this exception is thrown.
/// </summary>
internal class ExpectedTokenNotFoundInternalException : TemacInternalException
{
    public ExpectedTokenNotFoundInternalException(string expected, Token? nextToken)
        : base("Expected " + expected + " but got " + (nextToken == null ? "end-of-stream" : nextToken.ToDump()))
    {
    }
}