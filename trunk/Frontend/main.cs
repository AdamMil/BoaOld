//#define COMPILED

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
string source = 
@"
def makeAdder(base):
    def add(n): return base+n
    return add
";
Statement glob = Parser.FromString(source).Parse();
#if COMPILED
glob.PostProcessForCompile();
SnippetMaker.Generate(glob).Run(topFrame);
#else
glob.PostProcessForInterpret();
glob.Execute(topFrame);
#endif

    while(true)
    { try
      { Console.Write(">>> ");
//        Statement stmt = Parser.FromStream(stdin).ParseStatement();
        string line = Console.ReadLine();
        if(line=="quit" || line=="exit") break;
        Statement stmt = Parser.FromString(line).Parse();
        #if COMPILED
        stmt.PostProcessForCompile();
        FrameCode code = SnippetMaker.Generate(stmt);
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
