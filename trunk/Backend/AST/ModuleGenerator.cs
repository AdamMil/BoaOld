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

    AssemblyGenerator ag = new AssemblyGenerator(name, Path.GetFileNameWithoutExtension(filename)+".dll");
    TypeGenerator tg = ag.DefineType(name, typeof(Boa.Runtime.Module));

    ConstructorBuilder cons = tg.TypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
    CodeGenerator icg = tg.DefineMethod(MethodAttributes.Virtual|MethodAttributes.Public, "Run",
                                        typeof(void), new Type[] { typeof(Boa.Runtime.Frame) });
    icg.Namespace = new FrameNamespace(tg, icg);
    icg.ILG.Emit(OpCodes.Ldarg_0);
    tg.ModuleField.EmitSet(icg);
    body.Emit(icg);
    icg.ILG.Emit(OpCodes.Ret);

    return (Boa.Runtime.Module)tg.FinishType().GetConstructor(Type.EmptyTypes).Invoke(null);
  }
}

} // namespace Boa.AST