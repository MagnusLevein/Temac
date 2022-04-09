using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
using Temac.Interpretation;

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

namespace Temac.Errors;

class ErrorHandler
{
    static private ErrorHandler _instance = new ErrorHandler();

    static public ErrorHandler Instance => _instance;

    private int tokenizationErr = 0;
    private int structuralErr = 0;

    bool hasError = false;

    public bool HasError => hasError;

    public bool CanInterpret => tokenizationErr == 0 && structuralErr == 0;

    private List<string> _shownErrors = new List<string>();

    private ErrorHandler()
    {
    }

    private void HighlightFirstError()
    {
        if (!hasError)
            Console.Error.WriteLine("** Error list **");
        hasError = true;
    }

    public void Error(string message, Location? location = null, bool doNotFilter = false)
    {
        if (Interpreter.StopAndTrace)
            return;

        if (!doNotFilter)
        {
            // Don't show exactly the same message more than once
            if (_shownErrors.Contains(message))
                return;
            _shownErrors.Add(message);
        }

        HighlightFirstError();
        Console.Error.WriteLine(SuffixWithBlanks(Location.GetLocationString(location != null ? location : Interpreter.SetLocation)) + message);

        if (CompilerEnvironment.Instance.TraceError)
        {
            Console.Error.WriteLine("\nProcessing stopped on first error, since parameter -stop was given.\n\nRecursion trace:");
            Interpreter.StopAndTrace = true;
        }
    }

    private string SuffixWithBlanks(string text)
    {
        if (String.IsNullOrEmpty(text))
            return String.Empty;

        return text + "  ";
    }

    public void TokenizationError(string message, Location? location = null)
    {
        tokenizationErr++;
        Error(message, location, doNotFilter: true);
    }

    public void StructuralError(string message, Location? location = null)
    {
        structuralErr++;
        if (tokenizationErr < 1)
        { 
            // Don't report structural errors if there is tokenization errors.
            Error(message, location, doNotFilter: true);
        }
    }
}
