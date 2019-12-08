// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE-THIRD-PARTY in the project root for license information.

using System;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal class CommandException : Exception
    {
        public CommandException(string message)
            : base(message)
        {
        }

        public CommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
