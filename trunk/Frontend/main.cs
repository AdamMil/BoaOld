/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targetting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection.Emit;
using Boa.AST;
using Boa.Runtime;
using Boa.Modules;

namespace Boa.TextFrontend
{

public class Text
{ static void DoInteractive()
  { Options.Interactive = true;

    Frame topFrame = PrepareFrame();
    try
    { while(true)
      { try
        { Statement stmt=null;
          Console.Write(sys.ps1);
          string source = Console.ReadLine();
          if(source==null) break;

          try { stmt = Parser.FromString(source).Parse(); }
          catch(SyntaxErrorException e)
          { if(e.Message.IndexOf("expected indent")==-1 && e.Message.IndexOf("expecting 'except'")==-1) throw;
            source += '\n';
            while(true)
            { Console.Write(sys.ps2);
              string line = Console.ReadLine();
              if(line==null) break;
              source += line + '\n';
            }
            stmt = Parser.FromString(source).Parse();
          }
          if(Compiled)
          { stmt.PostProcessForCompile();
            SnippetMaker.Generate(stmt).Run(topFrame);
          }
          else
          { stmt.PostProcessForInterpret();
            stmt.Execute(topFrame);
          }
        }
        catch(Exception e)
        { if(e is SystemExitException) throw;
          Console.Error.WriteLine();
          Console.Error.WriteLine(e);
        }
      }
      if(WriteSnippets) SnippetMaker.DumpAssembly();
    }
    finally { if(sys.exitfunc!=null) Ops.Call(sys.exitfunc); }
  }

  static int Main(string[] args)
  { try
    { string outfile=null;
      PEFileKinds exeType = PEFileKinds.ConsoleApplication;
      ArrayList errors = new ArrayList();
      StringCollection files = new StringCollection();

      for(int i=0; i<args.Length; i++)
      { string arg = args[i];
        if((arg[0]=='-' || arg[0]=='/') && arg.Length!=1 && arg!="--")
        { string value;
          { int index = arg.IndexOf(':');
            value = index==-1 || index==arg.Length-1 ? null : arg.Substring(index+1);
            arg = index==-1 ? arg.Substring(1) : arg.Substring(1, index-1);
          }
          try
          { switch(arg)
            { case "banner": Banner=IsTrue(value); break;
              case "compiled": Compiled=IsTrue(value); break;
              case "debug": Options.Debug=IsTrue(value); break;
              case "?": case "help": case "-help": Usage(); return 0;
              case "lib": sys.path.insert(0, value==null ? "" : value); break;
              case "nostdlib": Options.NoStdLib=IsTrue(value); break;
              case "o": case "optimize": Options.Optimize=IsTrue(value); break;
              case "out": outfile=value; break;
              case "snippets": WriteSnippets=IsTrue(value); break;
              case "t": case "target":
                switch(value)
                { case "exe": exeType = PEFileKinds.ConsoleApplication; break;
                  case "winexe": exeType = PEFileKinds.WindowApplication; break;
                  case "dll": case "lib": case "library":
                    exeType = PEFileKinds.Dll; break;
                  default: errors.Add("Unknown value for -type: "+value); break;
                }
                break;
              default: errors.Add("Unknown argument: "+arg); break;
            }
          }
          catch { errors.Add("Invalid value for -"+arg+": "+value); }
        }
        else if(arg=="/") errors.Add("Invalid switch: /");
        else
        { if(files.Count==0) sys.argv.append(arg=="--" ? "" : arg);
          else if(arg=="-") errors.Add("Standard input cannot be specified along with other input files.");
          files.Add(arg);
          if(arg=="--") for(i++; i<args.Length; i++) sys.argv.append(args[i]);
        }
      }

      if(errors.Count!=0)
      { foreach(string error in errors) Console.Error.WriteLine("ERROR: "+error);
        Usage();
        return 1;
      }
      errors = null;

      if(Banner && (files.Count==0 || outfile!=null)) ShowBanner();

      if(files.Count==0) DoInteractive();
      else
      { bool stdin = files[0]=="-";
        string basename = stdin ? "main" : Path.GetFileNameWithoutExtension(files[0]);

        try
        { Parser parser;
          if(files.Count==1)
          { if(outfile!=null) Console.Error.WriteLine("Reading {0}...", stdin ? "standard input" : files[0]);
            if(stdin) parser = new Parser("<stdin>", Console.In);
            else
            { sys.path[0] = Path.GetDirectoryName(Path.GetFullPath(files[0]));
              parser = Parser.FromFile(files[0]);
            }
            if(outfile!=null)
            { Console.Error.WriteLine("Compiling...");
              ModuleGenerator.Generate(basename, outfile, parser.Parse(), exeType);
              Console.Error.WriteLine("Successfully wrote "+outfile);
            }
            else
            { Snippet s = SnippetMaker.Generate(parser.Parse(), basename);
              if(WriteSnippets) SnippetMaker.DumpAssembly();
              s.Run(PrepareFrame());
            }
          }
          else if(outfile==null)
          { Console.Error.WriteLine("ERROR: If multiple input files are specified, an output file must also "+
                                    "be specified.");
            return 1;
          }
          else
          { throw new NotImplementedException("Multiple-file compilation not supported.");

            AssemblyGenerator ag = new AssemblyGenerator(basename, outfile);
            for(int i=0; i<files.Count; i++)
            { sys.path[0] = Path.GetDirectoryName(Path.GetFullPath(files[i]));
              parser = Parser.FromFile(files[i]);
              ModuleGenerator.Generate(ag, Path.GetFileNameWithoutExtension(files[i]), outfile, parser.Parse(),
                                       true, i==0, exeType);
            }
            ag.Save();
          }
        }
        catch(Exception e)
        { Console.Error.WriteLine("Errors occurred during compilation:");
          if(e is SyntaxErrorException) Console.Error.WriteLine(e.Message);
          else Console.Error.WriteLine(e);
          return 1;
        }
      }
    }
    catch(SystemExitException e)
    { if(e.ExitArg==null) return 0;
      if(e.ExitArg is int) return (int)e.ExitArg;
      Console.Error.WriteLine(Ops.Str(e.ExitArg));
      return 1;
    }
    return 0;
  }

  static string GenerateName(string name, PEFileKinds type)
  { string ext = type==PEFileKinds.Dll ? ".dll" : ".exe";
    string baseName = Path.GetDirectoryName(name);
    if(baseName!="") baseName += Path.DirectorySeparatorChar;
    baseName += Path.GetFileNameWithoutExtension(name);

    name = baseName + ext;
    for(int i=0; File.Exists(name); i++) name = baseName + i.ToString() + ext;
    return name;
  }

  static bool IsTrue(string value)
  { if(value==null) value="";
    switch(value.ToLower())
    { case "-": case "0": case "no": case "off": case "false": return false;
      case "+": case "1": case "yes": case "on": case "true": return true;
      default: throw new ArgumentException();
    }
  }

  static Frame PrepareFrame()
  { Module top = new Module();
    Frame topFrame = new Frame(top);
    Ops.Frames.Push(topFrame);

    if(!Options.NoStdLib) top.__setattr__("__builtins__", Importer.Import("__builtin__"));
    top.__setattr__("__name__", "__main__");
    Importer.Import("string");
    return topFrame;
  }
  
  static void ShowBanner()
  { Console.Error.WriteLine("Boa, a python-like language for the .NET platform");
    Console.Error.WriteLine("Copyright Adam Milazzo 2004-2005. http://www.adammil.net");
  }
  
  static void Usage()
  { ShowBanner();
    Console.WriteLine("usage: boa [option] ... [file ... | -] [arg] ...");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("-banner:[-|+]      Display copyright banner");
    Console.WriteLine("-compiled:[-|+]    Enable compiled code (default=on)");
    Console.WriteLine("-debug:[-|+]       Emit debugging information");
    Console.WriteLine("-help              Show this message");
    Console.WriteLine("-lib:<path>        Specify additional library paths");
    Console.WriteLine("-nostdlib:[-|+]    Don't import the builtin functions");
    Console.WriteLine("-o[ptimize]:[-|+]  Enable optimizations");
    Console.WriteLine("-out:<file>        Compile and save the output (overrides -compiled)");
    Console.WriteLine("-snippets[-|+]     Save the snippets .dll");
    Console.WriteLine("-t[arget]:exe      Build a console application (used with -out)");
    Console.WriteLine("-t[arget]:library  Build a library (used with -out)");
    Console.WriteLine("-t[arget]:winexe   Build a windowed application (used with -out)");
    Console.WriteLine();
    Console.WriteLine("Other arguments:");
    Console.WriteLine("file               Read script file");
    Console.WriteLine("-                  Read standard input");
    Console.WriteLine("--                 Store remaining arguments in sys.argv[1:] and enter");
    Console.WriteLine("                   interactive mode");
    Console.WriteLine("arg ...            Arguments to store in sys.argv[1:]");
    Console.WriteLine();
    Console.WriteLine("Environment variables:");
    Console.WriteLine("BOA_LIB_PATH       A path to prefix to the module search path");
  }

  static bool Banner=true, Compiled=true, WriteSnippets;
}

} // namespace Boa.TextFrontend