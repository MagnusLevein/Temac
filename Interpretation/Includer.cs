using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Tokenization;
using Temac.Errors;
using System.IO;

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

namespace Temac.Interpretation;

static class Includer
{
    /// <summary>
    /// Loads a text file with the given tokenizer to a new DataBlock. Returns the DataBlock opened for reading.
    /// </summary>
    static public DataBlock IncludeFile(string fileName, Tokenizer tokenizer, Location? location)
    {
        fileName = fileName.Trim(); // Bug correction v. 1.1.2
        if (String.IsNullOrEmpty(fileName))
        {
            ErrorHandler.Instance.Error("Cannot include a file with empty filename.");
            return DataBlock.SystemBlockNULL;
        }

        try
        {
            var inputDataBlock = new DataBlock("file " + fileName);
            Compiler.Environment.TrackReadFile(fileName);
            tokenizer.Tokenize(fileName, inputDataBlock.OpenForWriting(append: false));
            inputDataBlock.Close(makeReadOnly: true);

            if (Compiler.Environment.DumpTokens)
                inputDataBlock.Dump(tokenizer.PublicName);

            return inputDataBlock.OpenForReading();
        }
        catch (IOException e)
        {
            ErrorHandler.Instance.Error(e.Message, location);
            return DataBlock.SystemBlockNULL;
        }
    }

    static public Tokenizer GetTokenizer(Instruction includeInstruction)
    {
        switch (includeInstruction)
        {
            case Instruction.Include:
                return new TemacTokenizer();

            case Instruction.IncludeHtmlescape:
                return new TextTokenizer(htmlEscape: true);

            case Instruction.IncludeDefsHtmlescape:
                return new DefinitionTokenizer(htmlEscape: true);

            case Instruction.IncludeBase64:
                return new Base64Tokenizer();
        }

        throw new TemacInternalException("Unhandled instruction in Includer.GetTokenizer()");
    }
}
