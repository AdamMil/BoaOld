//#define COMPILED

using System;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Frontend
{

public class Test
{ public string Foo { get { return foo; } set { foo=value; if(FooChanged!=null) FooChanged(this, new EventArgs()); } }
  public string Foo2 { get { return foo.Replace(" ", ""); } }
  public object a=1, b=2, c=3;
  
  public event EventHandler FooChanged;

  public override string ToString()
  { return string.Format("<{0}, foo={1}>", GetType().FullName, Ops.Repr(foo));
  }

  string foo;
}

public class Text
{ static void DoInteractive()
  { Options.Interactive = true;

    Module top = new Module();
    Frame topFrame = new Frame(top);
    topFrame.SetGlobal("_", null);

topFrame.Set("obj", new Test());

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
