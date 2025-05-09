using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Errors;
using Temac.Miscellaneous;
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

namespace Temac.Interpretation;

/// <summary>
/// Base class for variable parameter functions (for giving access to read/write functionality).
/// </summary>
class VariableParameter
{
    string _variableName = "";

    protected static bool IsVariableStartChar(char? ch)
    {
        if (ch == null)
            return false;
        if (Char.IsDigit((char)ch))
            return false;
        if (ch == '$')
            return true;
        return IsVariableChar(ch);
    }

    protected static bool IsVariableChar(char? ch)
    {
        if (ch == null)
            return false;
        if (Char.IsLetterOrDigit((char)ch))
            return true;
        if (ch == '_')
            return true;
        return false;
    }

    protected static bool IsDigit(char? ch) => ch != null && Char.IsDigit((char)ch);

    private VariableParameter(string variableExpression)
    {
        _variableName = variableExpression;
    }

    internal static VariableParameter HardcodedVariableParameter(string variableName) => new VariableParameter(variableName);

    public virtual DataBlock ReadFromDataBlock(Scope scope) => scope.ReadDataBlock(_variableName);

    public virtual DataBlock WriteToDataBlock(Scope scope, bool append) => scope.WriteDataBlock(_variableName, append);

    public virtual string ReadAsString(Scope scope, Token? untilToken = null)
    {
        var db = ReadFromDataBlock(scope);
        StringBuilder sb = new StringBuilder();
        Token? token;

        while ((token = db.ReadNext(untilToken)) != null)
        {
            if (token is EndOfLineToken eolt)
            {
                if (eolt.Kind != EndOfLineKind.Explicit)
                    continue;
            }
            if (token is CodeToken)
                ErrorHandler.Instance.Error("Expected plain text, but found unprocessed Temac code in \'" + db.Name + "\'.");
            else
                sb.Append(token.ToString());
        }

        db.Close();
        return sb.ToString();
    }

    public virtual string Dump() => _variableName;

    /// <summary>
    /// Normal variable and string constant match
    /// </summary>
    public static VariableParameter? TryMatchVariable(StringPointer data, ref int di)
    {
        if (IsVariableStartChar(data[di]))
        {
            return MatchVariableName(data, ref di);
        }
        else if (data[di] == '\"')
        {
            return ConstantParameter.MatchStringConstant(data, ref di);
        }
        else if (IsDigit(data[di]))
        {
            return ConstantParameter.MatchNumericConstant(data, ref di);
        }
        return null;
    }

    /// <summary>
    /// Match of a variable name with first character being anything (UTF-16) but control characters and space.
    /// </summary>
    public static VariableParameter? TryMatchTranslatedVariable(StringPointer data, ref int di)
    {
        var sb = new StringBuilder();

        // Normal variable
        if (IsVariableStartChar(data[di]))
            return MatchVariableName(data, ref di);

        if (data[di] == null || data[di] <= 32)
            return null;

        // Translate first character
        sb.AppendFormat("_{0:X4}_", (int)data[di]!);
        di++;

        while (IsVariableChar(data[di]))
        {
            sb.Append(data[di]);
            di++;
        }

        return new VariableParameter(sb.ToString());
    }

    private static VariableParameter? MatchVariableName(StringPointer data, ref int di)
    {
        var sb = new StringBuilder();

        if (!IsVariableStartChar(data[di]))
            return null;
        sb.Append(data[di]);
        di++;

        while (IsVariableChar(data[di]))
        {
            sb.Append(data[di]);
            di++;
        }

        return new VariableParameter(sb.ToString());
    }

    private static ConstantParameter? MatchStringConstant(StringPointer data, ref int di)
    {
        int initial_di = di;
        var sb = new StringBuilder();

        if (data[di] != '\"')
            return null;
        di++;

        while (data[di] != null)
        {
            if (data[di] == '\"')
            {
                if (data[di + 1] == '\"')
                {
                    sb.Append('\"');
                    di += 2;
                    continue;
                }
                di++;
                break;
            }
            sb.Append(data[di]);
            di++;
        }

        if (data[di] == null)
            throw new TemacVariableException("String constant without ending quote (\").", initial_di);

        return new ConstantParameter(sb.ToString());
    }

    private static ConstantParameter? MatchNumericConstant(StringPointer data, ref int di)
    {
        var sb = new StringBuilder();

        while (data[di] >= '0' && data[di] <= '9')
        {
            sb.Append(data[di]);
            di++;
        }

        if (sb.Length > 0)
            return new ConstantParameter(sb.ToString());
        return null;
    }

    /// <summary>
    /// Handles static text (or numerics) in places where variables are supported, and gives access to read-only functionality.
    /// </summary>
    private sealed class ConstantParameter : VariableParameter
    {
        string _constantString;

        DataBlock? _generatedDatablock = null;

        public ConstantParameter(string constantString) : base("")
        {
            _constantString = constantString;
        }

        public override DataBlock ReadFromDataBlock(Scope scope)
        {
            if (_generatedDatablock == null)
            {
                _generatedDatablock = new DataBlock("string constant");
                _generatedDatablock.OpenForWriting(append: false);
                _generatedDatablock.WriteNext(new DataToken(new CompilerLocation(), _constantString));
                _generatedDatablock.Close(makeReadOnly: true);
            }
            _generatedDatablock.OpenForReading();
            return _generatedDatablock;
        }

        public override string ReadAsString(Scope scope, Token? untilToken = null) => _constantString;

        public override DataBlock WriteToDataBlock(Scope scope, bool append)
        {
            ErrorHandler.Instance.Error("Cannot write to a constant value.");
            return DataBlock.SystemBlockNULL;
        }

        public override string Dump() => "\"" + _constantString.Replace("\"", "\"\"") + "\"";
    }
}
