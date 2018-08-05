using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public class Logger
    {
        public static Logger Current { get; } = new Logger();

        public LogLevel MinLogLevel { get; set; } = LogLevel.Trace;

        public void Fatal(string text) => Log(LogLevel.Fatal, text);
        public void Error(string text) => Log(LogLevel.Error, text);
        public void Warning(string text) => Log(LogLevel.Warning, text);
        public void Info(string text) => Log(LogLevel.Info, text);
        public void Trace(string text) => Log(LogLevel.Trace, text);
        public void LowTrace(Func<string> getText)
        {
            if (MinLogLevel == LogLevel.LowTrace)
                Log(LogLevel.LowTrace, getText());
        }
    


        void Log(LogLevel logLevel, string text)
        {
            // TODO: in queue
            if ((int)logLevel >= (int)MinLogLevel)
                lock (locker)
                {
                    var foreground = Console.ForegroundColor;
                    var background = Console.BackgroundColor;
                    try
                    {
                        TextWriter writer = null;
                        switch (logLevel)
                        {
                            case LogLevel.Fatal:
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.BackgroundColor = ConsoleColor.DarkRed;
                                writer = Console.Error;
                                break;
                            case LogLevel.Error:
                                Console.ForegroundColor = ConsoleColor.Red;
                                writer = Console.Error;
                                break;
                            case LogLevel.Warning:
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                writer = Console.Out;
                                break;
                            case LogLevel.Info:
                                Console.ForegroundColor = ConsoleColor.Green;
                                writer = Console.Out;
                                break;
                            case LogLevel.Trace:
                            case LogLevel.LowTrace:
                                writer = Console.Out;
                                break;
                        }
                        writer.WriteLine(text);
                    }
                    finally
                    {
                        Console.ForegroundColor = foreground;
                        Console.BackgroundColor = background;
                    }
                }
        }
        
        public enum LogLevel
        {
            LowTrace,
            Trace,
            Info,
            Warning,
            Error,
            Fatal
        }

        Logger()
        {
        }

        object locker = new object();
    }
}
