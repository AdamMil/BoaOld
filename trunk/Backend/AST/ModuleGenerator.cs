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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Boa.AST
{

public sealed class ModuleGenerator
{ ModuleGenerator() { }

  public static Boa.Runtime.Module Generate(string name, string filename, Statement body)
  { body.PostProcessForCompile();
    bool interactive = Options.Interactive;
    Options.Interactive = false;
    try
    { AssemblyGenerator ag = new AssemblyGenerator(name, Path.GetFileNameWithoutExtension(filename)+".dll");
      TypeGenerator tg = ag.DefineType(name, typeof(Boa.Runtime.Module));

      ConstructorBuilder cons = tg.TypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
      CodeGenerator icg = tg.DefineMethod(MethodAttributes.Virtual|MethodAttributes.Public|MethodAttributes.HideBySig,
                                          "Run", typeof(void), new Type[] { typeof(Boa.Runtime.Frame) });
      FrameNamespace ns = new FrameNamespace(tg, icg);
      icg.Namespace = ns;
      icg.ILG.Emit(OpCodes.Ldarg_0);
      tg.ModuleField.EmitSet(icg);
      icg.ILG.Emit(OpCodes.Ldarg_1);
      ns.FrameSlot.EmitSet(icg);

      icg.ILG.Emit(OpCodes.Ldarg_1);
      icg.EmitString("__name__");
      icg.EmitString(name);
      icg.EmitCall(typeof(Boa.Runtime.Frame), "SetGlobal");

      string docstring = Misc.BodyToDocString(body);
      if(docstring!=null)
      { icg.ILG.Emit(OpCodes.Ldarg_1);
        icg.EmitString("__doc__");
        icg.EmitString(docstring);
        icg.EmitCall(typeof(Boa.Runtime.Frame), "SetGlobal");
      }

      body.Emit(icg);
      icg.ILG.Emit(OpCodes.Ret);

      return (Boa.Runtime.Module)tg.FinishType().GetConstructor(Type.EmptyTypes).Invoke(null);
    }
    finally { Options.Interactive = interactive; }
  }
}

} // namespace Boa.AST