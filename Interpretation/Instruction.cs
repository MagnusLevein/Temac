using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

// !! DO NOTE the relationship between the enum values of Instruction, StructuralClass and ComparitionOperator: !!
// !!           Instruction = StructuralClass | instruction_number | ComparitionOperator                        !!
// !!             bitmask:        0xf0000            0x0fff0               0x0000f                              !!

public enum StructuralClass : UInt32
{
    Blanks          = 0x00000,
    Continuous      = 0x10000,
    SingleLine      = 0x20000,
    EndOfLine       = 0x30000,
    GeneralBlock    = 0x40000,
    EndBlock        = 0x50000,
    If              = 0x60000,
    Else            = 0x70000,
    EndIf           = 0x80000,
    Switch          = 0x90000,
    Case            = 0xa0000,
    Default         = 0xb0000,
    EndSwitch       = 0xc0000,
    While           = 0xd0000,
    EndWhile        = 0xe0000
}

enum ComparitionOperator : Byte
{
    None            = 0x0,
    Equal           = 0x1,
    NotEqual        = 0x2,
    Greater         = 0x3,
    GreaterOrEqual  = 0x4,
    Less            = 0x5,
    LessOrEqual     = 0x6,
}

enum Instruction : UInt32
{
    ComparitionOperator_Mask= 0x0000f,

    Increment               = 0x10010,
    Decrement               = 0x10020,
    GetFiles                = 0x10030,
    ContextBegin            = 0x10040,
    ContextEnd              = 0x10050,
    Unwrap                  = 0x10060,

    Include                 = 0x10090,
    IncludeHtmlescape       = 0x100a0,
    IncludeDefsHtmlescape   = 0x100b0,
    IncludeBase64           = 0x100c0,

    CountLines              = 0x10100,
    ReadLine                = 0x10200,
    Invoke                  = 0x10300,
    VariableSet             = 0x20400,
    VariableAppend          = 0x20500,
    VariableSetBlock        = 0x40600,
    VariableAppendBlock     = 0x40700,
    MacroBlock              = 0x40800,

    Sandbox                 = 0x40900,
    SandboxWithFilename     = 0x40a00,
    Output                  = 0x40b00,
    EndBlock                = 0x50f00,

    If_Family               = 0x61000,
    IfEqual                 = 0x61001,            
    IfNotEqual              = 0x61002,         
    IfGreater               = 0x61003,          
    IfGreaterOrEqual        = 0x61004,   
    IfLess                  = 0x61005,             
    IfLessOrEqual           = 0x61006,
    Else                    = 0x72000,
    EndIf                   = 0x83000,
    
    Switch                  = 0x94000,
    Case                    = 0xa5000,
    Default                 = 0xb6000,
    EndSwitch               = 0xc7000,

    While_Family            = 0xd8000,
    WhileEqual              = 0xd8001,
    WhileNotEqual           = 0xd8002,
    WhileGreater            = 0xd8003,
    WhileGreaterOrEqual     = 0xd8004,
    WhileLess               = 0xd8005,
    WhileLessOrEqual        = 0xd8006,
    EndWhile                = 0xe9000
}

static class InstructionExtensionMethods
{
    static public StructuralClass GetStructuralClass(this Instruction instruction) => (StructuralClass)(0xf0000 & (UInt32)instruction);

    static public ComparitionOperator GetComparitionOperator(this Instruction instruction) => (ComparitionOperator)(0xf & (UInt32)instruction);

    static public bool SiblingRequired(this StructuralClass s) => (s == StructuralClass.Case ||
                                                                   s == StructuralClass.Default || 
                                                                   s == StructuralClass.Else || 
                                                                   s == StructuralClass.GeneralBlock || 
                                                                   s == StructuralClass.If || 
                                                                   s == StructuralClass.SingleLine || 
                                                                   s == StructuralClass.Switch ||
                                                                   s == StructuralClass.While );

    static public Instruction GetInstructionFamily(this Instruction instruction) => (Instruction)(0xffff0 & (UInt32)instruction);
}
