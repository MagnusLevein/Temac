using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Errors;
using Temac.Interpretation;
using Temac.Miscellaneous;

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

internal class TextTokenizer : Tokenizer
{
    readonly bool _htmlEscape;
    readonly StringBuilder databuf;

    public override string PublicName => "text file";

    public TextTokenizer(bool htmlEscape) : base()
    {
        _htmlEscape = htmlEscape;
        databuf = new StringBuilder();
    }

    /// <summary>
    /// Tokenize one single line.
    /// </summary>
    /// <param name="analyzer">StructureAnalyzer to be used for all tokens</param>
    /// <param name="location">Location of line start</param>
    /// <param name="ptr">StringPointer to line contents</param>
    /// <param name="destination">In which DataBlock to write the tokens</param>
    protected override void TokenizeLine(StructureAnalyzer analyzer, Location location, StringPointer ptr, DataBlock destination)
    {
        databuf.Clear();
        int relativeCol = 0;

        var whitespaceToken = SkipLeadingWhitespace(location, ptr);

        if (whitespaceToken != null)
            relativeCol = whitespaceToken.ColumnPositions;

        for (int colSkip; ptr[0] != null; ptr.MovePtr(colSkip), relativeCol += colSkip)
        {
            colSkip = 1;

            if (_htmlEscape)
            {
                if (ptr[0] == '<')
                {
                    databuf.Append("&lt;");
                    continue;
                }
                if (ptr[0] == '>')
                {
                    databuf.Append("&gt;");
                    continue;
                }
                if (ptr[0] == '&')
                {
                    databuf.Append("&amp;");
                    continue;
                }
            }

            databuf.Append(ptr[0]);
            if (ptr[0] == '\t')
            {
                relativeCol += TAB_SPACING - (relativeCol % TAB_SPACING) - 1;
            }
        }

        if (databuf.Length > 0)
        {
            if (whitespaceToken != null)
            {
                destination.WriteNext(analyzer.Analyze(whitespaceToken));
                whitespaceToken = null;
            }
            destination.WriteNext(analyzer.Analyze(new DataToken(location.GetRelativeLocation(relativeCol - databuf.Length), databuf.ToString())));
        }

        destination.WriteNext(analyzer.Analyze(new EndOfLineToken(location.GetRelativeLocation(relativeCol), EndOfLineKind.Explicit)));

    }
}
