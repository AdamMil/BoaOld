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

#define COMPILED

using System;
using Boa.AST;
using Boa.Runtime;
using Boa.Modules;

namespace Boa.Frontend
{

public class Text
{ static void DoInteractive()
  { Options.Interactive = true;
Options.Debug = false;
sys.path[1] = "c:/code/Boa/Backend/lib";

    Module top = new Module();
    Frame topFrame = new Frame(top);
    Ops.Frames.Push(topFrame);

    top.__setattr__("__builtins__", Importer.Import("__builtin__"));
    top.__setattr__("__name__", "__main__");
    
    while(true)
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
