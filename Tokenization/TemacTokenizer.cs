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

class TemacTokenizer : Tokenizer
{
    readonly static Translation[] Syntax = {
        new Translation("<.\b>",                                  Instruction.EndBlock),

        new Translation("<:include \b\v\b>",                      Instruction.Include),
        new Translation("<:include-text2html \b\v\b>",            Instruction.IncludeHtmlescape),
        new Translation("<:include-defs2html \b\v\b>",            Instruction.IncludeDefsHtmlescape),
        new Translation("<:include-bin2base64 \b\v\b>",           Instruction.IncludeBase64),

        new Translation("<:count \b\v\b>",                        Instruction.CountLines),
        new Translation("<:=\b\v[\v]\b>",                         Instruction.ReadLine),
        new Translation("<:=\b\v\b>",                             Instruction.Invoke), // without parameters
        new Translation("<:=\b\v \b\a\v\b>",                      Instruction.Invoke), // with 1 or more parameter(s)
        new Translation("<:\b\v\b=>",                             Instruction.VariableSet),
        new Translation("<:\b\v\b.=>",                            Instruction.VariableAppend),
        new Translation("<:\b\v\b=:>",                            Instruction.VariableSetBlock),
        new Translation("<:\b\v\b.=:>",                           Instruction.VariableAppendBlock),
        new Translation("<:\b\v\b:>",                             Instruction.MacroBlock),

        new Translation("<:\b\v++\b>",                            Instruction.Increment),
        new Translation("<:\b\v--\b>",                            Instruction.Decrement),
        new Translation("<:getfiles \b\v\b>",                     Instruction.GetFiles), // with one argument (pattern)
        new Translation("<:getfiles \b\v \b\v\b>",                Instruction.GetFiles), // with two arguments (pattern, path)
        new Translation("<:context-begin \b\v\b>",                Instruction.ContextBegin), // with one argument (data)
        new Translation("<:context-begin \b\v \b\v\b>",           Instruction.ContextBegin), // with two arguments (data, context name)
        new Translation("<:context-end \b\v\b>",                  Instruction.ContextEnd), // with one argument (data)
        new Translation("<:context-end \b\v \b\v\b>",             Instruction.ContextEnd), // with two arguments (data, context name)
        new Translation("<:unwrap \b\v \b\v \b\v\b>",             Instruction.Unwrap),

        new Translation("<:sandbox\b>",                           Instruction.Sandbox),               // without filename; without pipes
        new Translation("<:sandbox \b\a%\v\b>",                   Instruction.Sandbox),               // without filename; with 1 or more pipe(s)
        new Translation("<:sandbox \b\v\b>",                      Instruction.SandboxWithFilename),   // with filename; without pipes
        new Translation("<:sandbox \b\v \b\a%\v\b>",              Instruction.SandboxWithFilename),   // with filename; with 1 or more pipe(s)
        new Translation("<:output \b\v\b>",                       Instruction.Output),

        new Translation("<:if \b\v\b==\b\v\b>",                   Instruction.IfEqual),
        new Translation("<:if \b\v\b!=\b\v\b>",                   Instruction.IfNotEqual),
        new Translation("<:if \b\v\b>=\b\v\b>",                   Instruction.IfGreaterOrEqual),
        new Translation("<:if \b\v\b<=\b\v\b>",                   Instruction.IfLessOrEqual),
        new Translation("<:if \b\v\b>\b\v\b>",                    Instruction.IfGreater),
        new Translation("<:if \b\v\b<\b\v\b>",                    Instruction.IfLess),
        new Translation("<:else\b>",                              Instruction.Else),
        new Translation("<.if\b>",                                Instruction.EndIf),

        new Translation("<:switch \b\v\b>",                       Instruction.Switch),
        new Translation("<:case \b\a\v\b>",                       Instruction.Case), // with one ore more arguments (!)
        new Translation("<:default\b>",                           Instruction.Default),
        new Translation("<.switch\b>",                            Instruction.EndSwitch),

        new Translation("<:while \b\v\b==\b\v\b>",                Instruction.WhileEqual),
        new Translation("<:while \b\v\b!=\b\v\b>",                Instruction.WhileNotEqual),
        new Translation("<:while \b\v\b>=\b\v\b>",                Instruction.WhileGreaterOrEqual),
        new Translation("<:while \b\v\b<=\b\v\b>",                Instruction.WhileLessOrEqual),
        new Translation("<:while \b\v\b>\b\v\b>",                 Instruction.WhileGreater),
        new Translation("<:while \b\v\b<\b\v\b>",                 Instruction.WhileLess),
        new Translation("<.while\b>",                             Instruction.EndWhile)
    };
    
    readonly StringBuilder databuf;

    public override string PublicName => "Temac file";

    public TemacTokenizer() : base()
    {
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

        var whitespaceToken = SkipLeadingWhitespace(location, ptr);

        if (whitespaceToken != null)
            relativeCol = whitespaceToken.ColumnPositions;

        for (int colSkip; ptr[0] != null; ptr.MovePtr(colSkip), relativeCol += colSkip)
        {
            colSkip = 1;

            if (ptr[0] == '<')
            {
                if (ptr[1] == ':' && ptr[2] == '>')
                {
                    // Rest of line is comment. A comment line with only blanks is not stored at all.
                    if (databuf.Length > 0)
                    {
                        if (whitespaceToken != null)
                        {
                            destination.WriteNext(analyzer.Analyze(whitespaceToken));
                            whitespaceToken = null;
                        }
                        destination.WriteNext(analyzer.Analyze(new DataToken(location.GetRelativeLocation(relativeCol - databuf.Length), databuf.ToString())));
                        destination.WriteNext(analyzer.Analyze(new EndOfLineToken(location.GetRelativeLocation(relativeCol), EndOfLineKind.Hidden)));
                    }
                    else if (hasCode)
                    {   // Bug correction v. 1.1.2: But if it has code, end of line might be required
                        destination.WriteNext(analyzer.Analyze(new EndOfLineToken(location.GetRelativeLocation(relativeCol), EndOfLineKind.Hidden)));
                    }
                    return;
                }

                if (ptr[1] == ':' || ptr[1] == '.')
                {
                    // Start of Temac command
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
                        var codeToken = FindTranslation(ptr, Syntax, out int dataSkip);
                        if (codeToken != null)
                        {
                            hasCode = true;
                            codeToken.SetLocation(location.GetRelativeLocation(relativeCol));
                            destination.WriteNext(analyzer.Analyze(codeToken));
                            colSkip = dataSkip;
                            continue;
                        }
                        ErrorHandler.Instance.TokenizationError("Unknown Temac command, or syntax error.", location.GetRelativeLocation(relativeCol));
                    }
                    catch (TemacVariableException e)
                    {
                        ErrorHandler.Instance.TokenizationError(e.Message, location.GetRelativeLocation(relativeCol + e.RelativePosition));
                    }
                    // Skip rest of the line.
                    destination.WriteNext(analyzer.Analyze(new EndOfLineToken(location.GetRelativeLocation(relativeCol),EndOfLineKind.Explicit)));
                    return;
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

        destination.WriteNext(analyzer.Analyze(new EndOfLineToken(location.GetRelativeLocation(relativeCol), hasCode ? EndOfLineKind.Default : EndOfLineKind.Explicit)));
    }
}
