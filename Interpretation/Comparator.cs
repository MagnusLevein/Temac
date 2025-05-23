using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;

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
/// Comparison functions used used by the interpreter.
/// </summary>
static class Comparator
{
    /// <summary>
    /// Performs the comparison of ’left’ and ’right’ variables (given as strings), according to the given ’comparison’ operator.
    /// If both of them are interpretable as integers, they are compared as numbers. Otherways as strings.
    /// This makes  1 == "01"  being a true statement.
    /// </summary>
    static internal bool Comparison(ComparisonOperator comparison, string left, string right)
    {
        if (Int32.TryParse(left, out int leftValue) && Int32.TryParse(right, out int rightValue))
        {
            return CompareNumbers(comparison, leftValue, rightValue);
        }

        return CompareStrings(comparison, left, right);
    }

    static private bool CompareNumbers(ComparisonOperator comparison, int leftValue, int rightValue)
    {
        return CompareByOperator(comparison, leftValue - rightValue);
    }

    static private bool CompareStrings(ComparisonOperator comparison, string leftString, string rightString)
    {
        if (comparison == ComparisonOperator.Equal)
            return String.Equals(leftString, rightString, StringComparison.Ordinal);

        if (comparison == ComparisonOperator.NotEqual)
            return !String.Equals(leftString, rightString, StringComparison.Ordinal);

        return CompareByOperator(comparison, String.Compare(leftString, rightString, StringComparison.CurrentCulture));
    }

    /// <summary>
    /// Converts a ’compareValue’ (less than 0, 0, greater than 0) to a boolean value (true or false), in accordance with the selected ’comparison’ operator.
    /// In effect, the function is a 3 x 6 lookup table that finds the correct truthness value.
    /// </summary>
    static private bool CompareByOperator(ComparisonOperator comparison, int compareValue)
    {
        if (compareValue == 0)
        {
            switch (comparison)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.GreaterOrEqual:
                case ComparisonOperator.LessOrEqual:
                    return true;
                default:
                    return false;
            }
        }
        if (compareValue < 0)
        {
            switch (comparison)
            {
                case ComparisonOperator.NotEqual:
                case ComparisonOperator.Less:
                case ComparisonOperator.LessOrEqual:
                    return true;
                default:
                    return false;
            }
        }
        switch (comparison)
        {
            case ComparisonOperator.NotEqual:
            case ComparisonOperator.Greater:
            case ComparisonOperator.GreaterOrEqual:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Performs the case test in a switch/case operation. (Note that the case instruction can have multiple parameters.)
    /// </summary>
    static public bool CaseTest(Scope scope, string switchString, VariableParameter[] cases)
    {
        foreach (var variable in cases)
        {
            if (String.Equals(switchString, variable.ReadAsString(scope)))
                return true;
        }
        return false;
    }
}
