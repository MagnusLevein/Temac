using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

/// <summary>
/// All data blocks live in a scope. Scopes are chained together back to the root scope.
/// </summary>
public class Scope
{
    protected Scope? _parentScope = null;

    protected Dictionary<string, DataBlock> _dataBlocks = new Dictionary<string, DataBlock>();

    protected static readonly string[] ReadOnlyBlocks = { "$YYYY", "$MM", "$DD", "$FileIn", "$FileOut", "$FileName", "$parameter" };

    /// <summary>
    /// Set up the root scope
    /// </summary>
    public Scope(string commandLineParameter)
    {
        _dataBlocks.Add("$null", DataBlock.SystemBlockNULL);
        _dataBlocks.Add("$err", DataBlock.SystemBlockERR);

        var today = DateTime.Today;
        _dataBlocks.Add("$YYYY", new DataBlock("$YYYY").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), today.Year.ToString())).Close(makeReadOnly: true));
        _dataBlocks.Add("$MM", new DataBlock("$MM").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), today.Month.ToString("00"))).Close(makeReadOnly: true));
        _dataBlocks.Add("$DD", new DataBlock("$DD").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), today.Day.ToString("00"))).Close(makeReadOnly: true));
        _dataBlocks.Add("$parameter", new DataBlock("$parameter").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), commandLineParameter)).Close(makeReadOnly: true));
        _dataBlocks.Add("$blankline", new DataBlock("$blankline").OpenForWriting(false).Close());
    }

    protected Scope(Scope parent)
    {
        _parentScope = parent;
    }

    public virtual void DumpTrace(IList<DataBlockDump> dump, int scopeNumber)
    {
        foreach (var block in _dataBlocks)
        {
            var blockdump = block.Value.GetDump();
            blockdump.ExternalName = block.Key;
            blockdump.ScopeNumber = scopeNumber;
            dump.Add(blockdump);
        }
        if(_parentScope != null)
            _parentScope.DumpTrace(dump, scopeNumber);
    }

    /// <summary>
    /// Find and open a datablock for reading. (Returns the NULL system data block on errors.)
    /// </summary>
    public virtual DataBlock ReadDataBlock(string name)
    {
        if (_dataBlocks.ContainsKey(name))
            return _dataBlocks[name].OpenForReading();

        if(_parentScope != null)
            return _parentScope.ReadDataBlock(name);

        ErrorHandler.Instance.Error("Variable \'" + name + "\' does not exist.");
        return DataBlock.SystemBlockNULL;
    }

    /// <summary>
    /// Find and open - or create and open - datablock for writing
    /// </summary>
    public virtual DataBlock WriteDataBlock(string name, bool append)
    {
        if (_dataBlocks.ContainsKey(name))
            return _dataBlocks[name].OpenForWriting(append);

        if (IsParameterName(name, out int _))
        {
            ErrorHandler.Instance.Error("\'" + name + "\' is readonly.");
            return DataBlock.SystemBlockNULL;
        }

        if (_parentScope != null)
            return _parentScope.WriteDataBlock(name, append);

        var db = new DataBlock(name);
        _dataBlocks.Add(name, db);
        return db.OpenForWriting(append);
    }

    public Scope GetNewSandboxScope(IList<DataBlock> pipes)
    {
        return new SandboxScope(this, pipes);
    }

    public Scope GetNewMacroScope(IList<DataBlock> parameters, Location invocationLocation)
    {
        return new MacroScope(this, parameters, invocationLocation);
    }

    public Scope GetNewInOutFileScope(string fileIn, string fileOut, string fileName)
    {
        return new FileScope(this, fileIn, fileOut, fileName);
    }


    protected bool IsParameterName(string name, out int number)
    {
        number = 0;
        if (name[0] != '$')
            return false;
        return int.TryParse(name.Substring(1), out number);
    }

    /// <summary>
    /// Allow global read access, but only writing in the sandbox isolation or pre-defined pipes.
    /// </summary>
    private class SandboxScope : Scope
    {
        List<string> _pipeNames = new List<string>(); // ONLY used to be able to indicate pipes in variable dump

        public SandboxScope(Scope parent, IList<DataBlock> pipes) : base(parent)
        {
            foreach (var pipe in pipes)
            {
                _dataBlocks.Add(pipe.Name, pipe);
                _pipeNames.Add(pipe.Name);
            }
        }

        public override DataBlock WriteDataBlock(string name, bool append)
        {
            if (name == "$null")
                return DataBlock.SystemBlockNULL;

            if (name == "$err")
                return DataBlock.SystemBlockERR.OpenForWriting(true);

            if (ReadOnlyBlocks.Contains(name) || IsParameterName(name, out int _))
            {
                ErrorHandler.Instance.Error("\'" + name + "\' is readonly.");
                return DataBlock.SystemBlockNULL;
            }

            if (_dataBlocks.ContainsKey(name))
                return _dataBlocks[name].OpenForWriting(append);

            var db = new DataBlock(name);
            _dataBlocks.Add(name, db);
            return db.OpenForWriting(append);
        }
        public override void DumpTrace(IList<DataBlockDump> dump, int scopeNumber)
        {
            foreach (var block in _dataBlocks)
            {
                var blockdump = block.Value.GetDump();
                blockdump.ExternalName = block.Key;
                blockdump.ScopeNumber = scopeNumber;
                if(_pipeNames.Contains(block.Key))
                    blockdump.IsPipe = true;
                dump.Add(blockdump);
            }
            if (_parentScope != null)
                _parentScope.DumpTrace(dump, scopeNumber + 2);
        }

    }

    /// <summary>
    /// Allow access to macro parameters ’$1’, ’$2’, ’$3’ ... and parameter count as ’$’
    /// </summary>
    private class MacroScope : Scope
    {
        IList<DataBlock> _parameters;
        DataBlock _parameterCount;
        Location _invocationLocation;

        public MacroScope(Scope parent, IList<DataBlock> parameters, Location invocation) : base(parent)
        {
            _parameters = parameters;
            _parameterCount = new DataBlock("parameter count").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), _parameters.Count.ToString())).Close(makeReadOnly: true);
            _invocationLocation = invocation;
        }

        public override DataBlock ReadDataBlock(string name)
        {
            if (name == "$")
                return _parameterCount.OpenForReading();

            if (IsParameterName(name, out int number))
            {
                if (number < 1)
                {
                    ErrorHandler.Instance.Error("Bad parameter count, at least $1 expected.");
                    return DataBlock.SystemBlockNULL;
                }
                if (number > _parameters.Count)
                {
                    ErrorHandler.Instance.Error("Macro expects (at least) " + number + " parameter(s).", _invocationLocation);
                    return DataBlock.SystemBlockNULL;
                }
                return _parameters[number - 1].OpenForReading();
            }

            return base.ReadDataBlock(name);
        }

        public override DataBlock WriteDataBlock(string name, bool append)
        {
            return base.WriteDataBlock(name, append);
        }

        public override void DumpTrace(IList<DataBlockDump> dump, int scopeNumber)
        {
            var cntDump = _parameterCount.GetDump();
            cntDump.ExternalName = "$";
            cntDump.ScopeNumber = scopeNumber;
            dump.Add(cntDump);

            for (int i = 0; i < _parameters.Count; i++)
            {
                var paramDump = _parameters[i].GetDump();
                paramDump.ExternalName = "$" + (i + 1);
                paramDump.ScopeNumber = scopeNumber;
                dump.Add(paramDump);
            }
            base.DumpTrace(dump, scopeNumber + 1);
        }

    }

    /// <summary>
    /// Allows for a local level of $FileIn, $FileOut and $FileName system variables
    /// </summary>
    private class FileScope : Scope
    {
        public FileScope(Scope parent, string fileIn, string fileOut, string fileName) : base(parent)
        {
            _dataBlocks.Add("$FileIn", new DataBlock("$FileIn").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), fileIn)).Close(makeReadOnly: true));
            _dataBlocks.Add("$FileOut", new DataBlock("$FileOut").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), fileOut)).Close(makeReadOnly: true));
            _dataBlocks.Add("$FileName", new DataBlock("$FileName").OpenForWriting(false).WriteNext(new DataToken(new CompilerLocation(), fileName)).Close(makeReadOnly: true));
        }
    }
}