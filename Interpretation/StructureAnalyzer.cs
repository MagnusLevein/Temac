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

namespace Temac.Interpretation;

/// <summary>
/// The StructureAnalyzer performs phase 2 of 3 in the compilation process, and shall be called from the tokenizer.
/// </summary>
internal class StructureAnalyzer
{
    Stack<Token> _stack;

    public StructureAnalyzer()
    {
        _stack = new Stack<Token>();
    }

    const string Err = "Syntax error in command order. ";

    private void Unexpected(CodeToken ct) => ErrorHandler.Instance.StructuralError(Err + "Unexpected " + ct.Instruction.ToString() + ".", ct.Location);

    private void PopMatching(Token linkToToken, params StructuralClass[] correct)
    {
        if (_stack.Count <= 0)
        {
            Unexpected((CodeToken)linkToToken);
            return;
        }

        var topToken = _stack.Peek();

        // Bug correction v. 1.0.1:  When EOL is expected, it is also ok to get any other closing token, and implicitly close all awaiting SingleLine tokens.
        while (topToken.StructuralClass == StructuralClass.SingleLine && linkToToken.StructuralClass != StructuralClass.EndOfLine)
        {
            ((CodeToken)topToken).SetNextSibling(linkToToken);
            _stack.Pop();
            topToken = _stack.Peek();
        }

        if (!correct.Contains(topToken.StructuralClass))
        {
            Unexpected((CodeToken)linkToToken);
            return;
        }

        ((CodeToken)topToken).SetNextSibling(linkToToken);
        _stack.Pop();
    }

    /// <summary>
    /// All CodeTokens must be runt through Analyze, in order to collect and store the 'NextStructuralSibling' information,
    /// that is required in the interpretation phase.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Token Analyze(Token token)
    {
        Token? topToken = null;

        if (token.StructuralClass == StructuralClass.Blanks)
            return token;

        if (_stack.Count > 0)
        {
            topToken = _stack.Peek();

            if (topToken.StructuralClass == StructuralClass.Switch && token.StructuralClass != StructuralClass.Case && token.StructuralClass != StructuralClass.EndOfLine)
                ErrorHandler.Instance.StructuralError(Err + "Expected \'case\'.", token.Location);
        }

        switch (token.StructuralClass)
        {
            case StructuralClass.SingleLine:
            case StructuralClass.GeneralBlock:
            case StructuralClass.If:
            case StructuralClass.Switch:
            case StructuralClass.While:
                _stack.Push(token);
                break;

            case StructuralClass.Else:
                PopMatching(token, StructuralClass.If);
                _stack.Push(token);
                break;

            case StructuralClass.EndIf:
                PopMatching(token, StructuralClass.If, StructuralClass.Else);
                break;

            case StructuralClass.Case:
                PopMatching(token, StructuralClass.Switch, StructuralClass.Case);
                _stack.Push(token);
                break;

            case StructuralClass.Default:
                PopMatching(token, StructuralClass.Case);
                _stack.Push(token);
                break;

            case StructuralClass.EndSwitch:
                PopMatching(token, StructuralClass.Case, StructuralClass.Default);
                break;

            case StructuralClass.EndWhile:
                PopMatching(token, StructuralClass.While);
                break;

            case StructuralClass.EndOfLine:
                while (_stack.TryPeek(out topToken))
                {
                    if (topToken.StructuralClass != StructuralClass.SingleLine)
                        break;
                    ((CodeToken)topToken).SetNextSibling(token);
                    _stack.Pop();
                }
                break;

            case StructuralClass.EndBlock:
                PopMatching(token, StructuralClass.GeneralBlock);
                break;
        }

        return token;
    }

    public void FinalCheck(Location location)
    {
        if (_stack.Count != 0)
        {
            ErrorHandler.Instance.StructuralError(Err + _stack.Count.ToString() + " unclosed instruction(s) at end of file.", location);
            _stack.Clear();
        }
    }
}
