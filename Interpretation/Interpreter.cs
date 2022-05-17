using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temac.Environ;
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

static class Interpreter
{
    private const int MAX_DEEP = 300;

    private static Stack<Location> _locationStack = new Stack<Location>();

    public static bool StopAndTrace { get; set; } = false;

    private static bool Stop => StopAndTrace || !ErrorHandler.Instance.CanInterpret;

    public static IReadOnlyList<DataBlockDump>? VariableDump { get; private set; } = null;

    public static Location? SetLocation => _locationStack.Count > 0 ? _locationStack.Peek() : null;

    /// <summary>
    /// Temac main interpreter, recursive.
    /// </summary>
    /// <param name="scope">current Scope</param>
    /// <param name="src">open data block for reading code</param>
    /// <param name="dest">open data block for writing result</param>
    /// <param name="untilToken">stop and return before handling this token; null to continue until end of data block</param>
    static public void Interpret(Scope scope, DataBlock src, DataBlock dest, Token? untilToken)
    {
        Token? token;

        if (!ErrorHandler.Instance.CanInterpret)
            Console.Error.WriteLine("Interpretation cancelled due to syntax error(s).");

        while (!Stop && (token = src.ReadNext(untilToken)) != null)
        {
            if (token is CodeToken ctoken)
            {
                if (_locationStack.Count > MAX_DEEP)
                {
                    ErrorHandler.Instance.Error("Recursion too deep.");
                    if (!StopAndTrace)
                    {
                        Console.Error.WriteLine("\nRecursion trace:");
                        StopAndTrace = true;
                    }
                    break;
                }

                _locationStack.Push(ctoken.Location!);

                #region ------------------------------ Instruction interpretation ------------------------------

                Token? nextToken;
                Instruction instruction = ctoken.Instruction;
                switch (instruction.GetInstructionFamily())
                {


                    case Instruction.Increment:
                    case Instruction.Decrement:
                        if (ctoken.Parameters.Length != 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        int newValue;
                        Int32.TryParse(ctoken.Parameters[0].ReadAsString(scope), out int value);
                        if (instruction == Instruction.Increment)
                            newValue = value + 1;
                        else
                            newValue = value - 1;
                        ctoken.Parameters[0].WriteToDataBlock(scope, false).WriteNext(new DataToken(new CompilerLocation(), newValue.ToString())).Close();
                        break;



                    case Instruction.GetFiles:
                        if (ctoken.Parameters.Length < 1 || ctoken.Parameters.Length > 2)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        string dirPath = ".";
                        if (ctoken.Parameters.Length == 2)
                            dirPath = ctoken.Parameters[1].ReadAsString(scope);

                        try
                        {
                            if (!Directory.Exists(dirPath))
                            {
                                ErrorHandler.Instance.Error("\'" + dirPath + "\' is not a valid (relative or absolute) directory path.");
                                break;
                            }

                            List<string> dirList = new List<string>(Directory.GetFiles(dirPath, ctoken.Parameters[0].ReadAsString(scope)));
                            dirList.Sort();
                            foreach (var item in dirList)
                            {
                                string filename = item;
                                if (filename.StartsWith("." + Path.DirectorySeparatorChar))
                                    filename = filename.Substring(2);

                                dest.WriteNext(new DataToken(new CompilerLocation(), filename));
                                dest.WriteNext(new EndOfLineToken(new CompilerLocation(), EndOfLineKind.Explicit));
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.Instance.Error("Directory access error: " + ex.Message);
                        }
                        break;



                    case Instruction.ContextBegin:
                    case Instruction.ContextEnd:
                        if (ctoken.Parameters.Length < 1 || ctoken.Parameters.Length > 2)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        dest.WriteNext(new TentativeDataToken(ctoken.Location!, ctoken.Parameters[0].ReadAsString(scope),
                            instruction == Instruction.ContextBegin ? TentativeDataToken.Kind.Begin : TentativeDataToken.Kind.End,
                            ctoken.Parameters.Length > 1 ? ctoken.Parameters[1].ReadAsString(scope) : ""));
                        break;



                    case Instruction.Include:
                    case Instruction.IncludeHtmlescape:
                    case Instruction.IncludeDefsHtmlescape:
                    case Instruction.IncludeBase64:
                        if (ctoken.Parameters.Length != 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        DataBlock includedDb = Includer.IncludeFile(ctoken.Parameters[0].ReadAsString(scope), Includer.GetTokenizer(instruction));
                        Interpret(scope, includedDb, dest, null);
                        includedDb.Close();
                        break;



                    case Instruction.CountLines:
                        if (ctoken.Parameters.Length != 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        ctoken.Parameters[0].ReadFromDataBlock(scope).CountLines(out int numberOfLines).Close();
                        dest.WriteNext(new DataToken(new CompilerLocation(), numberOfLines.ToString()));
                        break;



                    case Instruction.ReadLine:
                        if (ctoken.Parameters.Length != 2)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        Int32.TryParse(ctoken.Parameters[1].ReadAsString(scope), out int whichLine);
                        DataBlock singleLineDb = ctoken.Parameters[0].ReadFromDataBlock(scope).SelectSpecificLine(whichLine);
                        Interpret(scope, singleLineDb, dest, null);
                        singleLineDb.Close();
                        break;



                    case Instruction.Invoke:
                        // Invoke a macro or show a variable
                        if (ctoken.Parameters.Length < 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        List<DataBlock> parameters = new();
                        for (int i = 1; i < ctoken.Parameters.Length; i++)
                        {
                            parameters.Add(ctoken.Parameters[i].ReadFromDataBlock(scope).Close());
                        }
                        DataBlock readDb = ctoken.Parameters[0].ReadFromDataBlock(scope);
                        Interpret(scope.GetNewMacroScope(parameters, ctoken.Location!), readDb, dest, null);
                        readDb.Close();
                        break;



                    case Instruction.VariableSet:
                    case Instruction.VariableAppend:
                    case Instruction.VariableSetBlock:
                    case Instruction.VariableAppendBlock:
                    case Instruction.MacroBlock:
                        // Load a data block with data or code
                        if (ctoken.Parameters.Length != 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        DataBlock writeDb = ctoken.Parameters[0].WriteToDataBlock(scope, instruction == Instruction.VariableAppend || instruction == Instruction.VariableAppendBlock);
                        if (instruction == Instruction.MacroBlock)
                            CopyStream(src, writeDb, ctoken.NextStructuralSibling!);
                        else
                            Interpret(scope, src, writeDb, ctoken.NextStructuralSibling);
                        writeDb.Close();

                        if (Stop)
                            break;

                        nextToken = src.ReadNext(untilToken);
                        if (instruction.GetStructuralClass() == StructuralClass.GeneralBlock)
                            ExpectInstruction(nextToken, "EndBlock", Instruction.EndBlock);
                        else
                        {
                            // Bug correction v. 1.0.1:  When EOL is expected, it is also ok to get end-of-stream, if it is nested expressions.
                            if (nextToken != null && !(nextToken is EndOfLineToken))
                                throw new ExpectedTokenNotFoundInternalException("end of line", nextToken);
                        }
                        break;



                    case Instruction.Sandbox:
                    case Instruction.SandboxWithFilename:
                        // Start sandboxed scope
                        int pi = instruction == Instruction.SandboxWithFilename ? 1 : 0;

                        List<DataBlock> pipes = new List<DataBlock>();
                        for (; pi < ctoken.Parameters.Length; pi++)
                        {
                            pipes.Add(ctoken.Parameters[pi].WriteToDataBlock(scope, append: true).Close());
                        }
                        Scope sandboxScope = scope.GetNewSandboxScope(pipes);

                        if (instruction == Instruction.SandboxWithFilename)
                        {
                            // Start sandboxed scope with set filename $-variables
                            if (ctoken.Parameters.Length < 1)
                                throw new WrongNumberOfArgumentsInternalException(ctoken);

                            string useFileName = ctoken.Parameters[0].ReadAsString(scope);
                            string scopeOutputFilename = CompilerEnvironment.Instance.GenerateOutputFileName(useFileName, out string useNakedName);
                            sandboxScope = sandboxScope.GetNewInOutFileScope(useFileName, scopeOutputFilename, useNakedName);
                        }

                        Interpret(sandboxScope, src, dest, ctoken.NextStructuralSibling);

                        if (Stop)
                            break;

                        nextToken = src.ReadNext(untilToken);
                        ExpectInstruction(nextToken, "EndBlock", Instruction.EndBlock);
                        break;



                    case Instruction.Output:
                        // Redirect output to given file
                        if (ctoken.Parameters.Length != 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        string outFile = ctoken.Parameters[0].ReadAsString(scope);
                        DataBlock customOutput = DataBlock.SystemBlockNULL;
                        if (CompilerEnvironment.Instance.IsUnsecureFileName(outFile))
                            ErrorHandler.Instance.Error($"Output file name \'{outFile}\' does not match the output pattern (\'{CompilerEnvironment.Instance.OutputFilePattern}\').", ctoken.Location);
                        else
                            customOutput = new OutputDataBlock(outFile).OpenForWriting(append: false);

                        Interpret(scope, src, customOutput, ctoken.NextStructuralSibling);
                        customOutput.Close();

                        if (Stop)
                            break;

                        nextToken = src.ReadNext(untilToken);
                        ExpectInstruction(nextToken, "EndBlock", Instruction.EndBlock);
                        break;



                    case Instruction.If_Family:
                        if (ctoken.Parameters.Length != 2)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        bool isTrue = Comparator.Comparition((ComparitionOperator)(instruction & Instruction.ComparitionOperator_Mask), ctoken.Parameters[0].ReadAsString(scope), ctoken.Parameters[1].ReadAsString(scope));
                        if (isTrue)
                            Interpret(scope, src, dest, ctoken.NextStructuralSibling);                  // call if-true code
                        else
                            src.SkipUntil(ctoken.NextStructuralSibling);                                // skip if-true code

                        if (Stop)
                            break;

                        nextToken = src.ReadNext(untilToken);
                        ExpectInstruction(nextToken, "Else or EndIf", Instruction.Else, Instruction.EndIf);

                        if ((nextToken as CodeToken)!.Instruction == Instruction.Else)
                        {
                            _locationStack.Update(nextToken.Location!);

                            if (isTrue)
                                src.SkipUntil((nextToken as CodeToken)!.NextStructuralSibling);                 // skip else code
                            else
                                Interpret(scope, src, dest, (nextToken as CodeToken)!.NextStructuralSibling);   // call else code

                            if (Stop)
                                break;

                            nextToken = src.ReadNext(untilToken);
                            ExpectInstruction(nextToken, "EndIf", Instruction.EndIf);
                        }
                        break;



                    case Instruction.Switch:
                        if (ctoken.Parameters.Length != 1)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        string switchString = ctoken.Parameters[0].ReadAsString(scope);

                        bool hasTrueCase = false;
                        do
                        {
                            nextToken = src.ReadNextExceptBlanks(untilToken);
                            if (nextToken == null || !(nextToken is CodeToken) || !(((CodeToken)nextToken).Instruction == Instruction.Case))
                                break;
                            if (((CodeToken)nextToken).Parameters.Length < 1)
                                throw new WrongNumberOfArgumentsInternalException((CodeToken)nextToken);

                            _locationStack.Update(nextToken.Location!);
                            bool isCase = Comparator.CaseTest(scope, switchString, ((CodeToken)nextToken).Parameters);
                            if (isCase)
                            {
                                hasTrueCase = true;
                                Interpret(scope, src, dest, (nextToken as CodeToken)!.NextStructuralSibling);               // call case code
                            }
                            else
                                src.SkipUntil((nextToken as CodeToken)!.NextStructuralSibling);                             // skip case code
                        } while (!Stop);

                        if (Stop)
                            break;
                        ExpectInstruction(nextToken, "Default or EndSwitch", Instruction.Default, Instruction.EndSwitch);

                        if ((nextToken as CodeToken)!.Instruction == Instruction.Default)
                        {
                            _locationStack.Update(nextToken.Location!);
                            if (!hasTrueCase)
                                Interpret(scope, src, dest, (nextToken as CodeToken)!.NextStructuralSibling);               // call default code
                            else
                                src.SkipUntil((nextToken as CodeToken)!.NextStructuralSibling);                             // skip default code

                            if (Stop)
                                break;

                            nextToken = src.ReadNextExceptBlanks(untilToken);
                        }
                        ExpectInstruction(nextToken, "EndSwitch", Instruction.EndSwitch);
                        break;



                    case Instruction.While_Family:
                        if (ctoken.Parameters.Length != 2)
                            throw new WrongNumberOfArgumentsInternalException(ctoken);

                        int whileCounter = 0;
                        src.StoreReadPointer(out int storedCurrentToken);
                        while (!Stop && Comparator.Comparition((ComparitionOperator)(instruction & Instruction.ComparitionOperator_Mask), ctoken.Parameters[0].ReadAsString(scope), ctoken.Parameters[1].ReadAsString(scope)))
                        {
                            whileCounter++;
                            if (whileCounter > CompilerEnvironment.Instance.WhileMax)
                            {
                                ErrorHandler.Instance.Error("While loop exceeds the max limit of " + CompilerEnvironment.Instance.WhileMax + " turns; change with command line parameter -w.");
                                break;
                            }
                            src.RestoreReadPointer(storedCurrentToken);
                            Interpret(scope, src, dest, ctoken.NextStructuralSibling);                  // call while code
                        }
                        src.SkipUntil(ctoken.NextStructuralSibling); // required if while was never true, does no harm if it was

                        if (Stop)
                            break;

                        nextToken = src.ReadNext(untilToken);
                        ExpectInstruction(nextToken, "EndWhile", Instruction.EndWhile);
                        break;


                    default:
                        throw new TemacInternalException("Uninterpreted instruction " + ctoken.ToDump());
                }

                #endregion ----------------------------------------------------------------------------------------

                var leftLocation = _locationStack.Pop();
                if (StopAndTrace)
                {
                    // Dump recursion trace
                    Console.Error.WriteLine(" \u25BA {0}", Location.GetLocationString(leftLocation));
                    
                    // Save variable dump
                    if (VariableDump == null)
                    {
                        List<DataBlockDump> dbDump = new List<DataBlockDump>();
                        scope.DumpTrace(dbDump, 0);
                        dbDump.Sort();
                        VariableDump = dbDump;
                    }
                }
            }
            else
            {
                dest.WriteNext(token);
            }
        }
    }
        
    static private void CopyStream(DataBlock src, DataBlock dest, Token untilToken)
    {
        Token? token;
        while ((token = src.ReadNext(untilToken)) != null)
            dest.WriteNext(token);
    }

    static private void ExpectInstruction(Token? nextToken,string expectedTokenDescription, params Instruction[] instructions)
    {
        if (nextToken == null || !(nextToken is CodeToken cToken) || !instructions.Contains(cToken.Instruction))
            throw new ExpectedTokenNotFoundInternalException(expectedTokenDescription, nextToken);
    }

    private static void Update(this Stack<Location> locationStack, Location location)
    {
        locationStack.Pop();
        locationStack.Push(location);
    }
}
