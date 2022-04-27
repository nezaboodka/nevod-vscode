using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Nezaboodka.Nevod.LanguageServer;

#if DEBUG
while (!Debugger.IsAttached)
    Thread.Yield();
#endif

Server server = new();
using Stream input = Console.OpenStandardInput();
using Stream output = Console.OpenStandardOutput();
return server.Process(output, input);
