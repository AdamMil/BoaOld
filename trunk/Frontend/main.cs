#define COMPILED

using System;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Frontend
{

public class Text
{ static void DoInteractive()
  { Module top = new Module();
    Frame topFrame = new Frame(top);

    System.IO.Stream stdin = Console.OpenStandardInput();
    while(true)
    { try
      { Console.Write(">>> ");
string source = 
@"
def makeAdder(base):
    def add(n): return base+n
    return add
a = makeAdder(4)
b = makeAdder(6)
print a(3), b(3)
";
//        Statement stmt = Parser.FromStream(stdin).ParseStatement();
        Statement stmt = Parser.FromString(source).Parse();
        #if COMPILED
        stmt.PostProcessForCompile();
        FrameCode code = SnippetMaker.Generate(stmt);
        SnippetMaker.DumpAssembly();
        code.Run(topFrame);
        #else
        stmt.PostProcessForInterpret();
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
