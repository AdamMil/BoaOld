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
      icg.ILG.Emit(OpCodes.Dup);
      ns.FrameSlot.EmitSet(icg);
      icg.EmitString("__name__");
      icg.EmitString(name);
      icg.EmitCall(typeof(Boa.Runtime.Frame), "SetGlobal");
      body.Emit(icg);
      icg.ILG.Emit(OpCodes.Ret);

      return (Boa.Runtime.Module)tg.FinishType().GetConstructor(Type.EmptyTypes).Invoke(null);
    }
    finally { Options.Interactive = interactive; }
  }
}

} // namespace Boa.AST