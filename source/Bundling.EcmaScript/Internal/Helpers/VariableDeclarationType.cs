using System;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    // https://exploringjs.com/es6/ch_variables.html#_ways-of-declaring-variables
    [Flags]
    internal enum VariableDeclarationType
    {
        None = 0,
        Var = 0x1,
        Let = 0x2,
        Const = 0x4,
        Import = 0x8,
        Class = 0x10,
        Function = 0x20,
        FunctionParam = 0x40,
        CatchClauseParam = 0x80,
    }
}
