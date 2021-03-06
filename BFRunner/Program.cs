﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using CommandLine;
using CommandLine.Text;

namespace JITBrainfuck {
    class Options {
        [Option('s', "memorysize",
            HelpText = "Memory size, default is 65536.",
            DefaultValue = Runner.DefaultMemoryLength)]
        public int MemorySize { get; set; }

        [Option('d', "stackdepth",
            HelpText = "Maximum stack depth, default is 256.",
            DefaultValue = Runner.DefaultStackDepth)]
        public int MaxStackDepth { get; set; }

        [Option('i', "interpret",
            HelpText = "Should interpret the source file instead of JIT compiling?")]
        public bool Interpret { get; set; }

        [Option('o', "output",
            HelpText = "Optional output executable assembly filename. " +
            "If provided, instead of running the code, " +
            "it will compile and output the executable file.")]
        public string OutputFileName { get; set; }

        [Option("assemblyname",
            HelpText = "Optional assembly name for signature of the output executable assembly file.")]
        public string OutputAssemblyName { get; set; }

        [ValueList(typeof(List<string>), MaximumElements = 1)]
        public List<string> FileName { get; set; }

        [HelpOption]
        public string GetUsage() {
            return HelpText.AutoBuild(this, (current) =>
                HelpText.DefaultParsingErrorsHandler(this, current)
            );
        }
    }

    class Program {
        static void Main(string[] args) {
            try {
                Options options = new Options();
                if(Parser.Default.ParseArguments(args, options)) {
                    if(options.FileName.Count < 1) {
                        Console.WriteLine("No file is selected");
                        return;
                    }

                    string arg = options.FileName[0], path = "", content;
                    try {
                        path = Path.GetFullPath(arg);
                        FileInfo fileInfo = new FileInfo(path);
                        if(!fileInfo.Exists) return;
                        using(StreamReader sr = fileInfo.OpenText())
                            content = sr.ReadToEnd();
                    } catch(ArgumentException) {
                        content = arg;
                    }

                    Runner runner = new Runner(content, options.MemorySize, options.MaxStackDepth);

                    if(!string.IsNullOrEmpty(options.OutputFileName)) {
                        if(string.IsNullOrEmpty(options.OutputAssemblyName))
                            options.OutputAssemblyName = Path.GetFileNameWithoutExtension(options.OutputFileName);
                        Console.WriteLine("Creating assembly {0} to {1}...", options.OutputAssemblyName, options.OutputFileName);
                        BF2Assembly.CompileToFile(runner, options.OutputAssemblyName, options.OutputFileName);
                        return;
                    }

                    if(!options.Interpret)
                        runner.Compile();

                    Console.InputEncoding = Encoding.ASCII;
                    Console.OutputEncoding = Encoding.ASCII;
                    runner.Run(Console.In, Console.Out);

                    Console.ReadKey(true);
                }
            } catch(Exception ex) {
                Console.OutputEncoding = Encoding.Default;
                Console.Write("Error: ");
                while(ex != null) {
                    Console.WriteLine("{0}\n{1}", ex.Message, ex.StackTrace);
                    if(ex == ex.InnerException)
                        break;
                    ex = ex.InnerException;
                    if(ex != null)
                        Console.Write("Inner Exception: ");
                }
            }
        }
    }
}
