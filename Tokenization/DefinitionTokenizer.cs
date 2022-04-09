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

/// <remarks>
/// !! Do create a new DefinitionTokenizer for each file to analyze. !!
/// </remarks>
internal class DefinitionTokenizer : Tokenizer
{
    readonly bool _htmlEscape;
    readonly StringBuilder databuf;

    private List<Token> _waitingEOL = new List<Token>();
    private bool _waitingEndBlock = false;

    public override string PublicName => "definition file";

    public DefinitionTokenizer(bool htmlEscape) : base()
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
        bool hasCode = false;
        bool hasNonblank = false;
        bool skipBlanks = false;

        var whitespaceToken = SkipLeadingWhitespace(location, ptr);

        if (whitespaceToken != null)
            relativeCol = whitespaceToken.ColumnPositions;

        if (skipBlanks)
            whitespaceToken = null;

        for (int colSkip; ptr[0] != null; ptr.MovePtr(colSkip), relativeCol += colSkip)
        {
            colSkip = 1;

            if (ptr[0] != ' ' && ptr[0] != '\t')
                skipBlanks = false;

            if (!hasNonblank && ptr[0] == '#')
            {
                // This is a comment line. Do not store.
                return;
            }
            if (!hasNonblank && ptr[0] == '[')
            {
                try
                {
                    var codeToken = FindTranslation(ptr, new Translation[] {
                            new Translation("[\b\v\b]", Instruction.VariableSetBlock)
                        }, out int dataSkip);
                    if (codeToken != null)
                    {
                        hasCode = true;
                        _waitingEOL.Clear();
                        CodeToken endBlock = new CodeToken(Instruction.EndBlock, new VariableParameter[]{ });
                        endBlock.SetLocation(location.GetRelativeLocation(relativeCol-1));
                        codeToken.SetLocation(location.GetRelativeLocation(relativeCol));
                        if(_waitingEndBlock)
                            destination.WriteNext(analyzer.Analyze(endBlock)); 
                        destination.WriteNext(analyzer.Analyze(codeToken));
                        _waitingEndBlock = true;
                        colSkip = dataSkip;
                        // Skip blanks
                        skipBlanks = true;
                        continue;
                    }
                    ErrorHandler.Instance.TokenizationError("Syntax error in [ ... ] expression.", location.GetRelativeLocation(relativeCol));
                }
                catch (TemacVariableException e)
                {
                    ErrorHandler.Instance.TokenizationError(e.Message, location.GetRelativeLocation(relativeCol + e.RelativePosition));
                }
                // Skip line.
                return;
            }
            hasNonblank = true;

            if (_waitingEOL.Count > 0)
            {
                foreach (var token in _waitingEOL)
                    destination.WriteNext(analyzer.Analyze(token));
                _waitingEOL.Clear();
            }

            if (ptr[0] == '{')
            {
                // Start of Invoke command, {...}, med eller utan parametrar
                if (whitespaceToken != null)
                {
                    destination.WriteNext(analyzer.Analyze(whitespaceToken));
                    whitespaceToken = null;
                }
                if (databuf.Length > 0)
                {
                    destination.WriteNext(analyzer.Analyze(new DataToken(location.GetRelativeLocation(relativeCol - databuf.Length), databuf.ToString())));
                    databuf.Clear();
                }

                try
                {
                    var codeToken = FindTranslation(ptr, new Translation[] {
                            new Translation("{\b\n\b}", Instruction.Invoke),
                            new Translation("{\b\n \b\a\v\b}", Instruction.Invoke)
                        }, out int dataSkip);
                    if (codeToken != null)
                    {
                        hasCode = true;
                        codeToken.SetLocation(location.GetRelativeLocation(relativeCol));
                        destination.WriteNext(analyzer.Analyze(codeToken));
                        colSkip = dataSkip;
                        continue;
                    }
                    ErrorHandler.Instance.TokenizationError("Syntax error in { ... } expression.", location.GetRelativeLocation(relativeCol));
                }
                catch (TemacVariableException e)
                {
                    ErrorHandler.Instance.TokenizationError(e.Message, location.GetRelativeLocation(relativeCol + e.RelativePosition));
                }
                // Syntax Error - skip rest of the line.
                destination.WriteNext(analyzer.Analyze(new EndOfLineToken(location.GetRelativeLocation(relativeCol), EndOfLineKind.Explicit)));
                return;
            }

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

            if(!(skipBlanks && ptr[0] == ' ' || ptr[0] == '\t'))
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
            _waitingEOL.Add(new EndOfLineToken(location.GetRelativeLocation(relativeCol), hasCode ? EndOfLineKind.Default : EndOfLineKind.Explicit));
            return;
        }

        if (hasCode)
        {
            if(!skipBlanks)
                _waitingEOL.Add(new EndOfLineToken(location.GetRelativeLocation(relativeCol), EndOfLineKind.Default));
        }
        else
        {
            // Empty line
            skipBlanks = false;
            int cnt = _waitingEOL.Count;
            if (cnt == 1)
            {
                CodeToken newParagraph = new CodeToken(Instruction.Invoke, new VariableParameter[] { VariableParameter.HardcodedVariableParameter("$blankline") });
                newParagraph.SetLocation(location.GetRelativeLocation(relativeCol));
                _waitingEOL.Add(analyzer.Analyze(newParagraph));
            }
            if (cnt <= 1)
                _waitingEOL.Add(new EndOfLineToken(location.GetRelativeLocation(relativeCol), EndOfLineKind.Explicit));
        }
    }

    protected override void TokenizeEndOfFile(StructureAnalyzer analyzer, Location location, DataBlock destination)
    {
        if (_waitingEndBlock)
        {
            CodeToken endBlock = new CodeToken(Instruction.EndBlock, new VariableParameter[] { });
            endBlock.SetLocation(location);
            destination.WriteNext(analyzer.Analyze(endBlock));
            _waitingEndBlock = false;
        }
    }
}
