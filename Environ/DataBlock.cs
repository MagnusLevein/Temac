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

namespace Temac.Environ;

public class DataBlock
{
    protected enum Status
    {
        Closed = 0,
        Writing = 1,
        Reading = 2,
        ReadingSpecificLine = 3
    }

    static public DataBlock SystemBlockNULL = new NullDataBlock();

    static public DataBlock SystemBlockERR = new ErrDataBlock();

    public string Name { get; protected set; }

    public bool IsReadonly { get; private set; } = false;

    protected Status _status = Status.Closed;

    protected List<Token> _tokens;

    protected List<int> _lineEndings;

    protected int _currentToken;

    public DataBlock(string name)
    {
        Name = name;
        _tokens = new List<Token>();
        _lineEndings = new List<int>();
    }

    public virtual DataBlock OpenForReading()
    {
        if (_status != Status.Closed)
        {
            ErrorHandler.Instance.Error("\'" + Name + "\' is already locked for " + (_status == Status.Writing ? "writing." :"reading."));
            return DataBlock.SystemBlockNULL;
        }

        _status = Status.Reading;
        _currentToken = 0;
        return this;
    }

    /// <summary>
    /// Used to read a specific line in for example a directory listing
    /// </summary>
    /// <param name="lineNumber">1-based line number</param>
    public virtual DataBlock SelectSpecificLine(int lineNumber)
    {
        if (_status != Status.Reading && _status != Status.ReadingSpecificLine)
            throw new TemacInternalException("Data block \'" + Name + "\' is not opened for reading.");

        if (lineNumber < 1)
        {
            ErrorHandler.Instance.Error("Bad line number (must be 1 or greater, but is " + lineNumber + ") when reading a specific line from \'" + Name + "\'.");
            return DataBlock.SystemBlockNULL;
        }

        _status = Status.ReadingSpecificLine;
        if (lineNumber > 1)
        {
            int lineEndingIndex = lineNumber - 2;
            if (lineEndingIndex >= _lineEndings.Count)
            {
                _currentToken = _tokens.Count; // End of stream
                return this;
            }

            _currentToken = _lineEndings[lineEndingIndex] + 1;
        }

        // Skip initial whitespace and non-significant EOL tokens
        for (; _currentToken < _tokens.Count && IsNonSpecificBlank(_tokens[_currentToken]); _currentToken++) ;

        return this;
    }

    /// <summary>
    /// Returns true for tokens that shall be skipped in leading position when selecting a specific line
    /// </summary>
    private bool IsNonSpecificBlank(Token token)
    {
        if (token is WhitespaceToken)
            return true;

        if (token is EndOfLineToken eolt)
            return eolt.Kind != EndOfLineKind.Explicit;

        return false;
    }

    /// <summary>
    /// Number of selectable lines, used in conjunction with SelectSpecificLine()
    /// </summary>
    public virtual DataBlock CountLines(out int lines)
    {
        if (_status != Status.Reading && _status != Status.ReadingSpecificLine)
            throw new TemacInternalException("Data block \'" + Name + "\' is not opened for reading.");

        lines = _lineEndings.Count;

        return this;
    }

    public virtual DataBlock OpenForWriting(bool append)
    {
        if (_status != Status.Closed)
        {
            ErrorHandler.Instance.Error("\'" + Name + "\' is already locked for " + (_status == Status.Writing ? "writing." : "reading."));
            return DataBlock.SystemBlockNULL;
        }

        if (IsReadonly)
        {
            ErrorHandler.Instance.Error("\'" + Name + " \' is readonly.");
            return DataBlock.SystemBlockNULL;
        }

        _status = Status.Writing;
        if (!append)
        {
            _tokens.Clear();
        }
        return this;
    }

    public virtual DataBlock Close(bool makeReadOnly = false)
    {
        if (_status == Status.Closed)
            throw new TemacInternalException("Data block \'" + Name + "\' is already closed.");

        if (makeReadOnly)
            IsReadonly = true;

        _status = Status.Closed;

        return this;
    }

    /// <summary>
    /// Read next token in an open-for-reading data block.
    /// </summary>
    /// <param name="untilToken">Stop right before this token is read; null to read until end of data block.</param>
    /// <returns>read token, or null if end of block</returns>
    /// <exception cref="TemacInternalException"></exception>
    public virtual Token? ReadNext(Token? untilToken = null)
    {
        if (_status != Status.Reading && _status != Status.ReadingSpecificLine)
            throw new TemacInternalException("Data block \'" + Name + "\' is not opened for reading.");

        if (_currentToken < _tokens.Count)
        {
            var token = _tokens[_currentToken];
            if (untilToken != null && untilToken == token)
            {
                return null;
            }
            if (_status == Status.ReadingSpecificLine && token is EndOfLineToken)
            {
                return null;
            }
            if (_status == Status.ReadingSpecificLine && token is CodeToken)
            {
                ErrorHandler.Instance.Error("\'" + Name + "\' contains Temac code, which is not supported with this command.");
                return null;
            }
            _currentToken++;

            if (!token.IsFinalized)
                throw new TemacInternalException("Found a token that was not properly finalized.");

            return token;
        }

        return null;
    }

    public void SkipUntil(Token? untilToken)
    {
        while (ReadNext(untilToken) != null);
    }

    /// <summary>
    /// Read next token in an open-for-reading data block, but skip over WhitespaceToken and EndOfLineToken
    /// </summary>
    public Token? ReadNextExceptBlanks(Token? untilToken = null)
    {
        Token? token;
        do
        {
            token = ReadNext(untilToken);
            if (token == null)
                return null;
        } while (token is WhitespaceToken || token is EndOfLineToken);
        return token;
    }

    public void StoreReadPointer(out int storedCurrentToken)
    {
        if (_status != Status.Reading)
            throw new TemacInternalException("Cannot store read pointer: data block \'" + Name + "\' is not opened for reading.");

        storedCurrentToken = _currentToken;
    }

    public void RestoreReadPointer(int storedCurrentToken)
    {
        if (_status != Status.Reading)
            throw new TemacInternalException("Cannot restore read pointer: data block \'" + Name + "\' is not opened for reading.");

        if(storedCurrentToken < 0 || storedCurrentToken >= _tokens.Count )
            throw new TemacInternalException("Cannot restore read pointer in data block \'" + Name + "\': pointer is out of range.");

        _currentToken = storedCurrentToken;
    }

    /// <summary>
    /// Write a token to an open-for-writing data block.
    /// </summary>
    /// <param name="token">NB: the token should be analyzed with StructeAnalyzer !</param>
    /// <returns>reference to current DataBlock</returns>
    public virtual DataBlock WriteNext(Token token)
    {
        if (_status != Status.Writing)
            throw new TemacInternalException("Data block \'" + Name + "\' is not opened for writing.");

        if (token is EndOfLineToken eolt && (_tokens.Count > 0 || eolt.Kind == EndOfLineKind.Explicit))
            _lineEndings.Add(_tokens.Count);

        // Handle the magic thar makes begin-end context pairs disappear
        if (token is TentativeDataToken tdt && tdt.IsEndKind)
        {
            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                if (_tokens[i] is WhitespaceToken || _tokens[i] is EndOfLineToken)
                    continue;
                if (_tokens[i] is TentativeDataToken otdt && otdt.IsBeginKind)
                {
                    if (TentativeDataToken.Matching(otdt, tdt))
                    {
                        _tokens.RemoveRange(i, _tokens.Count - i);
                        return (this);
                    }

                    string beginningLocation = Location.GetLocationString(otdt.Location);
                    ErrorHandler.Instance.Error($"Cannot pair this context-end with the context-begin from {beginningLocation}.", tdt.Location);
                }
                break;
            }
        }

        _tokens.Add(token);

        return this;
    }

    /// <summary>
    /// Debug feature, dump to stdout
    /// </summary>
    /// <exception cref="TemacInternalException"></exception>
    public void Dump(string tokenizerName)
    {
        string filename="";
        int lineNum=0;

        Console.Write("### Dump of {0} [{1}] ###", Name, tokenizerName);
        foreach (var token in _tokens)
        {
            if (token == null)
                throw new TemacInternalException("Trying to dump a null token.");
            if (token.Location?.Filename != filename || token.Location?.LineNo != lineNum)
            {
                filename=token.Location != null ? token.Location.Filename : "unknown file";
                lineNum=token.Location != null ? token.Location.LineNo : 0;
                if (lineNum > 0)
                    Console.Write("\n{0} {1,4}: ", filename, lineNum);
                else
                    Console.WriteLine(); // special case for base64 (binary) included file
            }
            Console.Write(token.ToDump());
        }
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Debug feature, get dump object for further processing
    /// </summary>
    public DataBlockDump GetDump()
    {
        StringBuilder contents = new StringBuilder();
        
        List<string> sourceFiles = new List<string>();

        bool hasCode = false;

        foreach (var token in _tokens)
        {
            if (token == null)
                throw new TemacInternalException("Trying to dump a null token.");

            if(!String.IsNullOrEmpty(token.Location?.Filename) && !sourceFiles.Contains(token.Location.Filename))
                sourceFiles.Add(token.Location.Filename);

            if(token is CodeToken)
                hasCode = true;

            contents.Append(token.ToDump(compact: true, fullBinary: true));
        }

        return new DataBlockDump(this.Name, contents.ToString(), String.Join(';', sourceFiles)) 
                                {
                                    IsMacro = hasCode,
                                    IsReadonly = IsReadonly,
                                    IsReading = _status == Status.Reading || _status == Status.ReadingSpecificLine,
                                    IsWriting = _status == Status.Writing
                                };
    }
}
