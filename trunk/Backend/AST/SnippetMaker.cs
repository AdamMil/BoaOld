/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

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
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public abstract class Snippet
{ public abstract void Run(Frame frame);
}

public class SnippetMaker
{ private SnippetMaker() { }

  public static void DumpAssembly()
  { Assembly.Save();
    Assembly = new AssemblyGenerator("snippets"+assCount, "snippets"+ assCount++ +".dll");
  }

  public static Snippet Generate(Statement body) { return Generate(body, "code_"+typeCount++); }
  public static Snippet Generate(Statement body, string typeName)
  { TypeGenerator tg = Assembly.DefineType(typeName, typeof(Snippet));
    CodeGenerator cg = tg.DefineMethod(MethodAttributes.Public|MethodAttributes.Virtual, "Run",
                                       typeof(void), new Type[] { typeof(Frame) });
    FrameNamespace fns = new FrameNamespace(tg, cg);
    cg.Namespace = fns;

    cg.EmitArgGet(0);
    fns.FrameSlot.FieldSlot.EmitSet(cg);

    cg.EmitArgGet(0);
    cg.EmitFieldGet(typeof(Frame), "Module");
    tg.ModuleField.EmitSet(cg);

    body.Emit(cg);
    cg.ILG.Emit(OpCodes.Ret);
    return (Snippet)tg.FinishType().GetConstructor(Type.EmptyTypes).Invoke(null);
  }

  public static AssemblyGenerator Assembly = new AssemblyGenerator("snippets", "snippets.dll");

  static int assCount, typeCount;
}

} // namespace IronPython.AST
