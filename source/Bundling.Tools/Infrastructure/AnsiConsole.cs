// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE-THIRD-PARTY in the project root for license information.

using System;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    internal static class AnsiConsole
    {
        public static readonly AnsiTextWriter _out = new AnsiTextWriter(Console.Out);

        public static void WriteLine(string text)
            => _out.WriteLine(text);
    }
}
