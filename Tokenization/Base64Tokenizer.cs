using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Errors;
using Temac.Interpretation;
using Temac.Miscellaneous;

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

internal class Base64Tokenizer : Tokenizer
{
    public override string PublicName => "base64 encoded binary data";

    public override void Tokenize(string inputFileName, DataBlock destination)
    {
        string base64string = Convert.ToBase64String(File.ReadAllBytes(inputFileName));
        destination.WriteNext(new DataToken(new Location(inputFileName, 0, 0), base64string, isBinary: true));
    }

    protected override void TokenizeLine(StructureAnalyzer structureAnalyzer, Location location, StringPointer ptr, DataBlock destination) => throw new NotSupportedException();
}
