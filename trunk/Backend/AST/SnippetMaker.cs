using System;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public abstract class Snippet
{ public abstract object Run(Frame frame);
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
    tg.TypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

    CodeGenerator cg = tg.DefineMethod(MethodAttributes.Public|MethodAttributes.Virtual, "Run",
                                       typeof(object), new Type[] { typeof(Frame) });
    FrameNamespace fns = new FrameNamespace(tg, cg);
    cg.Namespace = fns;

    cg.EmitArgGet(0);
    fns.FrameSlot.FieldSlot.EmitSet(cg);

    cg.EmitArgGet(0);
    cg.EmitFieldGet(typeof(Frame), "Module");
    tg.ModuleField.EmitSet(cg);

    body.Emit(cg);
    if(!(body is ReturnStatement)) cg.EmitReturn(null);
    return (Snippet)tg.FinishType().GetConstructor(Type.EmptyTypes).Invoke(null);
  }

  public static AssemblyGenerator Assembly = new AssemblyGenerator("snippets", "snippets.dll");

  static int assCount, typeCount;
}

} // namespace IronPython.AST
