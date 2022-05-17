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

/// <summary>
/// Base class for the tokenization, phase 1 in the compilation process. This converts an input file to tokens,
/// i.e. translate language syntax into a intermediate token form.
/// Different implementations of the class can read and understand different language dialects.
/// </summary>
abstract class Tokenizer
{
    protected const int TAB_SPACING = 4;

    protected struct Translation
    {
        // Special characters used in the patterns:
        // '\a' denotes anchor point for pattern repetition; the last character in the pattern is considered end-of-repetition
        // '\b' denotes possible blanks, i.e. 0 or more of space (ASCII 32)
        // '\v' denotes variable name or a variable expression: variable_name | "string constant" | integer | variable_name[<variable expression>]
        // '\n' denotes a variable name starting with nearly anything (a non-control character, and not a blank). It will be translated to a _0000_ expression if it otherwise would be invalid.

        public string Pattern;
        public Instruction Instruction;

        public Translation(string pattern, Instruction instruction)
        {
            Pattern = pattern;
            Instruction = instruction;
        }
    }

    /// <summary>
    /// This field is printed prior to an tokenization dump
    /// </summary>
    public abstract string PublicName { get; }

    public virtual void Tokenize(string inputFileName, DataBlock destination)
    {
        int lineNo = 1;

        var analyzer = new StructureAnalyzer();
        foreach (string currentLine in File.ReadLines(inputFileName))
        {
            var ptr = new StringPointer(currentLine.TrimEnd());
            TokenizeLine(analyzer, new Location(inputFileName, lineNo, 1), ptr, destination);
            lineNo++;
        }
        var eofLocation = new Location(inputFileName, lineNo, 0);
        TokenizeEndOfFile(analyzer, eofLocation, destination);
        analyzer.FinalCheck(eofLocation);
    }

    protected virtual WhitespaceToken? SkipLeadingWhitespace(Location location, StringPointer ptr)
    {
        int colSkip = 0;
        StringBuilder sb = new StringBuilder();
        
        while(true)
        {
            if (ptr[0] == ' ')
            {
                colSkip++;
                ptr.MovePtr();
                sb.Append(' ');
                continue;
            }
            if (ptr[0] == '\t')
            {
                colSkip += TAB_SPACING - (colSkip % TAB_SPACING);
                ptr.MovePtr();
                sb.Append('\t');
                continue;
            }
            break;
        }

        if (colSkip == 0)
            return null;

        return new WhitespaceToken(location, colSkip, sb.ToString());
    }

    /// <summary>
    /// Responsible for the tokenization of one line of input file.
    /// (If line-by-line tokenization does not work, override Tokenize() instead.)
    /// </summary>
    protected abstract void TokenizeLine(StructureAnalyzer structureAnalyzer, Location location, StringPointer ptr, DataBlock destination);

    /// <summary>
    /// Offers a possibility to do specific actions at end of file.
    /// </summary>
    protected virtual void TokenizeEndOfFile(StructureAnalyzer structureAnalyzer, Location location, DataBlock destination) { }


    /// <summary>
    /// Help function to tokenize a language command
    /// </summary>
    /// <param name="data">input data to interpret</param>
    /// <param name="command">valid commands in current language</param>
    /// <param name="dataSkip">number of character positions that shall be skipped, 0 if no valid command found</param>
    /// <returns>null if no valid command found</returns>
    protected CodeToken? FindTranslation(StringPointer data, Translation[] command, out int dataSkip)
    {
        CodeToken? codeToken; 

        for (int i = 0; i < command.Length; i++)
        {
            if ((codeToken=TestPattern(data, command[i], out dataSkip))!=null)
                return codeToken;
        }
        dataSkip = 0;
        return null;
    }


    /// <summary>
    /// Test one single command syntax against input text, and tokenize if possible
    /// </summary>
    /// <param name="data">input text</param>
    /// <param name="syntax">command syntax to test</param>
    /// <param name="dataSkip">number of character positions that was used to generate the CodeToken, 0 if no CodeToken was generated</param>
    /// <returns>generated CodeToken, or null if the syntax was not mached</returns>
    private CodeToken? TestPattern(StringPointer data, Translation syntax, out int dataSkip)
    {
        List<VariableParameter> parameters = new();
        dataSkip = 0;

        int di, pi, anchor = 0;

        string pattern = syntax.Pattern;

        for (di = pi = 0; pi < pattern.Length; pi++)
        {
            // Match anchor for start of repeated arguments
            if (pattern[pi] == '\a')
            {
                anchor = pi;
                continue;
            }

            // Mach regular character
            if (pattern[pi] >= 0x20)
            {
                // If repeated parameters are activated (= anchor position set), test if it is time to repeat
                if (pi == pattern.Length - 1 && anchor > 0 && pattern[pi] != data[di])
                {
                    pi = anchor;
                    continue;
                }

                if (pattern[pi] != data[di])
                    return null;
                di++;
                continue;
            }

            // Match possible blanks
            if (pattern[pi] == '\b')
            {
                while (data[di] == 0x20)
                    di++;
                continue;
            }

            // Match variable
            if (pattern[pi] == '\v')
            {
                var vp = VariableParameter.TryMatchVariable(data, ref di);
                if (vp == null)
                    return null;
                parameters.Add(vp);
                continue;
            }

            // Match variable with nearly anything valid as first character
            if (pattern[pi] == '\n')
            {
                var vp = VariableParameter.TryMatchTranslatedVariable(data, ref di);
                if (vp == null)
                    return null;
                parameters.Add(vp);
                continue;
            }

        }

        dataSkip = di;
        return new CodeToken(syntax.Instruction, parameters.ToArray());
    }

}