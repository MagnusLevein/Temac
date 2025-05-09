using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Errors;
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

class OutputDataBlock : DataBlock
{
    private string _filename;

    private static List<string> _nontouchedFiles = new List<string>();

    public static bool HasWrittenAnything { get; private set; } = false;

    public static IReadOnlyList<string> NontouchedFiles => _nontouchedFiles;


    public OutputDataBlock(string filename)
        : base("output file " + filename)
    {
        _filename = filename;
    }

    public override DataBlock OpenForReading()
    {
        ErrorHandler.Instance.Error("Data block \'" + Name + "\' is writeonly.");
        return SystemBlockNULL;
    }

    private bool TestForContents(IList<Token> tokens)
    {
        foreach (var token in _tokens)
        {
            if(token is DataToken || token is CodeToken)
                return true;
            if(token is EndOfLineToken eolt && eolt.Kind == EndOfLineKind.Explicit)
                return true;
        }

        return false;
    }

    public override DataBlock Close(bool makeReadOnly = false)
    {
        StringBuilder linebuf = new();
        int dataTokens = 0;
        Token? latestDataOrWhitespaceToken = null;
        bool isFirstColumn;

        if (makeReadOnly)
            throw new TemacInternalException("OutputDataBlock cannot be made readonly.");

        // Stop writing if we have compilation errors
        if (ErrorHandler.Instance.HasError)
            return base.Close();

        // Check if the buffer actually contains any data?
        if (!TestForContents(_tokens))
        {
            // No data. Do not touch output file, but if it do exists, issue a warning.
            if (File.Exists(_filename))
            {
                _nontouchedFiles.Add(_filename);
            }
            return base.Close();
        }

        CompilerEnvironment.Instance.TrackWrittenFile(_filename);

        using (var sw = new StreamWriter(_filename))
        {
            HasWrittenAnything = true;
            isFirstColumn = true;
            for(int i=0; i < _tokens.Count; i++)
            {
                Token token = _tokens[i];
                Token? nextToken = i+1 < _tokens.Count ? _tokens[i + 1] : null;

                if (token is CodeToken)
                {
                    ErrorHandler.Instance.Error("Unprocessed Temac code in output stream. Ignored.");
                    continue;
                }
                dataTokens += token is DataToken ? 1 : 0;

                // Leading spaces separated from its original context is ignored
                if (token is WhitespaceToken && Token.IsSameLine(token, nextToken) == false)
                    continue;

                // Leading spaces followed by non-explicit end of line token from the same line are also ignored (both of them)
                if (token is WhitespaceToken && nextToken is EndOfLineToken eolt && eolt.Kind != EndOfLineKind.Explicit && Token.IsSameLine(token, nextToken) == true)
                {
                    i++;
                    continue;
                }

                // ’Leading spaces’ which actually aren't at the beginning of a line are also ignored
                if (token is WhitespaceToken && !isFirstColumn)
                    continue;

                if (token is WhitespaceToken || token is DataToken)
                {
                    linebuf.Append(token.ToString());
                    isFirstColumn = false;
                    latestDataOrWhitespaceToken = token;
                    continue;
                }

                if (token is EndOfLineToken eolToken)
                {
                    if (eolToken.Kind == EndOfLineKind.Hidden)
                    {
                        sw.Write(linebuf.ToString());
                    }
                    else if (eolToken.Kind == EndOfLineKind.Explicit || (dataTokens > 0))
                    {
                        if (CompilerEnvironment.Instance.DebugNewlines)
                        {
                            linebuf.Append(eolToken.ToDebug());
                        }
                        sw.WriteLine(linebuf.ToString());
                        isFirstColumn = true;
                    }
                    else
                    {
                        if (dataTokens > 0)
                            sw.Write(linebuf.ToString());

                        // If not: no data tokens, and no explicit newline. Skip this line.
                    }
                    linebuf.Clear();
                    dataTokens = 0;
                    continue;
                }
                throw new TemacInternalException("Unhandled token variant.");
            }

            if (linebuf.Length > 0)
                sw.WriteLine(linebuf.ToString());
        }

        return base.Close();
    }
}