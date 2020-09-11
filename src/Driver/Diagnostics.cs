using System;
using System.IO;

namespace Driver
{
    public class DiagnosticsException : Exception
    {
        public DiagnosticsException(string message) : base(message) { }
    }

    public class Diagnostics
    {
        public enum Severity
        {
            Info,
            Warning,
            Error
        }

        private static ConsoleColor GetSeverityColor(Severity severity)
        {
            switch (severity) {
                case Severity.Info:
                    return ConsoleColor.Cyan;
                case Severity.Warning:
                    return ConsoleColor.Yellow;
                case Severity.Error:
                    return ConsoleColor.Red;
            }

            return ConsoleColor.White;
        }

        public delegate TextWriter ReportHandler(Severity severity, string message);

        public delegate void TextWriterDelegate(TextWriter writer);

        private static ReportHandler _reportHandler = (severity, message) => {
            ConsoleColor oldForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = GetSeverityColor(severity);
            Console.Error.Write($"[{severity}] ");
            Console.ForegroundColor = oldForegroundColor;
            Console.Error.WriteLine(message);
            return Console.Error;
        };

        public static void SetReportHandler(ReportHandler reportHandler) => _reportHandler = reportHandler;

        public static int ErrorCount { get; private set; }

        public static void Report(Severity severity, string message)
        {
            _reportHandler(severity, message);
            if (severity == Severity.Error)
                ErrorCount += 1;
        }

        public static void Report(Severity severity, string message, TextWriterDelegate writerDelegate)
        {
            writerDelegate(_reportHandler(severity, message));
            if (severity == Severity.Error)
                ErrorCount += 1;
        }

        public static void ThrowIfErrors()
        {
            if (ErrorCount > 0)
                throw new DiagnosticsException("Errors occured while running job");
        }

        public static DiagnosticsException ThrowError(string message)
        {
            // Error-only, immediate-throw version that has the exception as the return type.
            // This is useful for situations where the caller wants to use a throw statement for control flow needs.
            _reportHandler(Severity.Error, message);
            ErrorCount += 1;
            throw new DiagnosticsException(message);
        }

        public static DiagnosticsException ThrowError(string message, TextWriterDelegate writerDelegate)
        {
            writerDelegate(_reportHandler(Severity.Error, message));
            ErrorCount += 1;
            throw new DiagnosticsException(message);
        }
    }
}
