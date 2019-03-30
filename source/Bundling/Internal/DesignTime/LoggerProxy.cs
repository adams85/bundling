// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE-THIRD-PARTY in the project root for license information.

using System;
using System.Collections.Concurrent;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Internal.DesignTime
{
    internal class LoggerProxy : ILogger
    {
        private static int TranslateLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    return 2;
                case LogLevel.Warning:
                    return 1;
                case LogLevel.Information:
                    return 0;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return -1;
                default:
                    throw new ArgumentException(null, nameof(logLevel));
            }
        }

        private readonly Action<int, string> _logger;
        private readonly string _shortCategoryName;

        public LoggerProxy(string categoryName, Action<int, string> logger)
        {
            CategoryName = categoryName ?? string.Empty;

            var index = CategoryName.LastIndexOf('.');
            _shortCategoryName = index >= 0 ? CategoryName.Substring(index + 1) : CategoryName;

            _logger = logger;
        }

        public string CategoryName { get; }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger(
                TranslateLogLevel(logLevel),
                $"[{_shortCategoryName}] {formatter(state, exception)}");
        }
    }

    internal class LoggerProxyProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, LoggerProxy> _loggers;
        private readonly Action<int, string> _logger;

        public LoggerProxyProvider(Action<int, string> logger)
        {
            _logger = logger;
            _loggers = new ConcurrentDictionary<string, LoggerProxy>();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, key => new LoggerProxy(key, _logger));
        }

        public void Dispose() { }
    }
}
