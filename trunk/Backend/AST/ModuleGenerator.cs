/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

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
using Boa.Runtime;

namespace Boa.AST
{

public sealed class ModuleGenerator
{ ModuleGenerator() { }

  public static Runtime.Module Generate(string name, string filename, Statement body)
  { return Generate(new AssemblyGenerator(name, filename), name, filename, body,
                    false, false, false, PEFileKinds.ConsoleApplication);
  }

  public static Runtime.Module Generate(string name, string filename, Statement body, PEFileKinds type)
  { return Generate(new AssemblyGenerator(name, filename), name, filename, body, false, true, true, type);
  }

  public static Runtime.Module Generate(AssemblyGenerator ag, string name, string filename, Statement body,
                                        bool staticCompile, bool saveModule, bool entryPoint, PEFileKinds type)
  { if(staticCompile) throw new NotImplementedException("Static compilation is not implemented.");

    body.PostProcessForCompile();
    bool interactive = Options.Interactive;
    Options.Interactive = false;
    try
    { TypeGenerator tg = ag.DefineType(name, typeof(Runtime.Module));

      CodeGenerator icg = tg.DefineMethod(MethodAttributes.Virtual|MethodAttributes.Public|MethodAttributes.HideBySig,
                                          "Run", typeof(void), new Type[] { typeof(Frame) });
      FrameNamespace ns = new FrameNamespace(tg, icg);
      icg.Namespace = ns;
      icg.ILG.Emit(OpCodes.Ldarg_0);
      tg.ModuleField.EmitSet(icg);
      icg.ILG.Emit(OpCodes.Ldarg_1);
      ns.FrameSlot.EmitSet(icg);

      icg.ILG.Emit(OpCodes.Ldarg_1);
      icg.EmitString("__name__");
      icg.EmitString(name);
      icg.EmitCall(typeof(Frame), "SetGlobal");

      string docstring = Misc.BodyToDocString(body);
      if(docstring!=null)
      { icg.ILG.Emit(OpCodes.Ldarg_1);
        icg.EmitString("__doc__");
        icg.EmitString(docstring);
        icg.EmitCall(typeof(Frame), "SetGlobal");
      }

      body.Emit(icg);
      icg.EmitReturn();
      icg.Finish();

      if(entryPoint && type!=PEFileKinds.Dll)
      { CodeGenerator ecg = tg.DefineMethod(MethodAttributes.Static|MethodAttributes.HideBySig,
                                            "Main", typeof(void), new Type[] { typeof(string[]) });
        Slot mod=ecg.AllocLocalTemp(typeof(Runtime.Module)), frame=ecg.AllocLocalTemp(typeof(Frame)),
            argi=ecg.AllocLocalTemp(typeof(int));
        Label nextarg=ecg.ILG.DefineLabel(), lastarg=ecg.ILG.DefineLabel();

        ecg.EmitFieldGet(typeof(Modules.sys), "argv");
        ecg.EmitCall(typeof(Assembly), "GetExecutingAssembly");
        ecg.EmitPropGet(typeof(Assembly), "Location");
        ecg.EmitCall(typeof(List), "append");

        ecg.EmitInt(0);
        argi.EmitSet(ecg);
        ecg.ILG.MarkLabel(nextarg);
        argi.EmitGet(ecg);
        ecg.EmitArgGet(0);
        ecg.EmitPropGet(typeof(string[]), "Length");
        ecg.ILG.Emit(OpCodes.Clt);
        ecg.ILG.Emit(OpCodes.Brfalse_S, lastarg);
        ecg.EmitFieldGet(typeof(Modules.sys), "argv");
        ecg.EmitArgGet(0);
        argi.EmitGet(ecg);
        ecg.ILG.Emit(OpCodes.Ldelem_Ref);
        ecg.EmitCall(typeof(List), "append");
        argi.EmitGet(ecg);
        ecg.EmitInt(1);
        ecg.ILG.Emit(OpCodes.Add);
        argi.EmitSet(ecg);
        ecg.ILG.Emit(OpCodes.Br_S, nextarg);
        ecg.ILG.MarkLabel(lastarg);

        ecg.EmitNew(tg.TypeBuilder.DefineDefaultConstructor(MethodAttributes.Public));
        ecg.ILG.Emit(OpCodes.Dup);
        mod.EmitSet(ecg);
        ecg.EmitNew(typeof(Frame), new Type[] { typeof(Runtime.Module) });
        frame.EmitSet(ecg);

        ecg.EmitFieldGet(typeof(Ops), "Frames");
        frame.EmitGet(ecg);
        ecg.EmitCall(typeof(System.Collections.Stack), "Push");

        if(!Options.NoStdLib)
        { mod.EmitGet(ecg);
          ecg.EmitString("__builtins__");
          ecg.EmitString("__builtin__");
          ecg.EmitCall(typeof(Importer), "Import", new Type[] { typeof(string) });
          ecg.EmitCall(typeof(Runtime.Module), "__setattr__");
        }

        mod.EmitGet(ecg);
        ecg.EmitString("__name__");
        ecg.EmitString("__main__");
        ecg.EmitCall(typeof(Runtime.Module), "__setattr__");

        ecg.EmitCall(typeof(Importer), "ImportStandardModules");

        mod.EmitGet(ecg);
        frame.EmitGet(ecg);
        ecg.EmitCall((MethodInfo)icg.MethodBase);

        ecg.FreeLocalTemp(mod);
        ecg.FreeLocalTemp(frame);
        ecg.FreeLocalTemp(argi);

        ecg.EmitReturn();
        ecg.Finish();

        ag.Assembly.SetEntryPoint((MethodInfo)ecg.MethodBase, type);
        if(Options.Debug) ag.Module.SetUserEntryPoint((MethodInfo)icg.MethodBase);
      }

      Type mtype = tg.FinishType();
      if(saveModule) { ag.Save(); return null; }
      return (Runtime.Module)mtype.GetConstructor(Type.EmptyTypes).Invoke(null);
    }
    finally { Options.Interactive = interactive; }
  }
}

} // namespace Boa.AST
