#define COMPILED

using System;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Frontend
{

public class Text
{ static void DoInteractive()
  { Options.Interactive = true;

    Module top = new Module();
    Frame topFrame = new Frame(top);
    topFrame.SetGlobal("_", null);

    while(true)
    { try
      { Statement stmt=null;
        Console.Write(">>> ");
        string source = Console.ReadLine();
        if(source=="quit" || source=="exit") break;

        try
        { stmt = Parser.FromString(source).Parse();
        }
        catch(SyntaxErrorException e)
        { if(e.Message.IndexOf("expected indent")==-1) throw;
          source += '\n';
          while(true)
          { Console.Write("... ");
            string line = Console.ReadLine();
            if(line==null) break;
            source += line + '\n';
          }
          stmt = Parser.FromString(source).Parse();
        }

        topFrame.SetGlobal("_", null);
        #if COMPILED
        stmt.PostProcessForCompile();
        object ret = SnippetMaker.Generate(stmt).Run(topFrame);
        if(ret==null) ret = topFrame.GetGlobal("_");
        #else
        stmt.PostProcessForInterpret();
        stmt.Execute(topFrame);
        object ret = topFrame.GetGlobal("_");
        #endif
        if(ret!=null) Console.WriteLine(ret);
      }
      catch(Exception e)
      { Console.Error.WriteLine();
        Console.Error.WriteLine(e);
      }
    }
    SnippetMaker.DumpAssembly();
  }

  static void Main()
  { DoInteractive();
  }
}

}
