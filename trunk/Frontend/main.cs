#define COMPILED

using System;
using Language.AST;
using Language.Runtime;

namespace Language.Frontend
{

public class Text
{ 
  static void DoInteractive()
  { Module top = new Module();
    Frame topFrame = new Frame(top);

    System.IO.Stream stdin = Console.OpenStandardInput();
    while(true)
    { try
      { Console.Write(">>> ");
        Statement stmt = Parser.FromStream(stdin).ParseStatement();
        #if COMPILED
        FrameCode code = SnippetMaker.Generate(stmt);
break;
        code.Run(topFrame);
        #else
        object ret = stmt.Execute(topFrame);
        if(ret!=null) Console.WriteLine(ret);
        #endif
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
