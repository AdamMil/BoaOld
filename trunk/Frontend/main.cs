//#define COMPILED

using System;
using Boa.AST;
using Boa.Runtime;
using Boa.Modules;

namespace Boa.Frontend
{

public class Text
{ static void DoInteractive()
  { Options.Interactive = true;
    sys.path.append("");

    Module top = new Module();
    Frame topFrame = new Frame(top);
    Ops.Frames.Push(topFrame);
    
    top.__setattr__("__builtins__", Importer.Import("__builtin__"));
    top.__setattr__("__name__", "main");

    while(true)
    { try
      { Statement stmt=null;
        Console.Write(sys.ps1);
        string source = Console.ReadLine();
        if(source==null) break;

        try { stmt = Parser.FromString(source).Parse(); }
        catch(SyntaxErrorException e)
        { if(e.Message.IndexOf("expected indent")==-1) throw;
          source += '\n';
          while(true)
          { Console.Write(sys.ps2);
            string line = Console.ReadLine();
            if(line==null) break;
            source += line + '\n';
          }
          stmt = Parser.FromString(source).Parse();
        }

        #if COMPILED
        stmt.PostProcessForCompile();
        SnippetMaker.Generate(stmt).Run(topFrame);
        #else
        stmt.PostProcessForInterpret();
        stmt.Execute(topFrame);
        #endif
      }
      catch(Exception e)
      { if(e is SystemExitException) throw;
        if(e.InnerException is SystemExitException) throw e.InnerException;
        Console.Error.WriteLine();
        Console.Error.WriteLine(e);
      }
    }
    SnippetMaker.DumpAssembly();
  }

  static int Main()
  { try
    { DoInteractive();
    }
    catch(SystemExitException e)
    { if(e.ExitArg==null) return 0;
      if(e.ExitArg is int) return (int)e.ExitArg;
      Console.Error.WriteLine(Ops.Str(e.ExitArg));
      return 1;
    }
    finally
    { if(sys.exitfunc!=null) Ops.Call(sys.exitfunc);
    }
    return 0;
  }
}

}
