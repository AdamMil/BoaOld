using System;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public sealed class BoaFunction : Node
{ public BoaFunction(Parameter[] parms, Statement body) { Parameters=parms; Body=body; }
  public BoaFunction(string name, Parameter[] parms, Statement body) : this(parms, body) { Name=new Name(name); }

  public Name   Name;
  public Name[] Inherit, Globals;
  public Parameter[] Parameters;
  public Statement Body;

  public string FuncName { get { return Name==null ? "lambda" : Name.String; } }

  public void EmitClosedGet(CodeGenerator cg)
  { if(Inherit==null) cg.ILG.Emit(OpCodes.Ldnull);
    else
    { cg.EmitNewArray(typeof(ClosedVar), Inherit.Length);
      ConstructorInfo ci = typeof(ClosedVar).GetConstructor(new Type[] { typeof(string) });
      FieldInfo fi = typeof(ClosedVar).GetField("Value");

      for(int i=0; i<Inherit.Length; i++)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(i);
        Slot slot = cg.Namespace.GetLocalSlot(Inherit[i]);
        ClosedSlot cs = slot as ClosedSlot;
        if(cs!=null) cs.Storage.EmitGet(cg);
        else
        { cg.EmitString(Inherit[i].String);
          cg.EmitNew(ci);
          cg.ILG.Emit(OpCodes.Dup);
          slot.EmitGet(cg);
          cg.EmitFieldSet(fi);
        }
        cg.ILG.Emit(OpCodes.Stelem_Ref);
      }
    }
  }

  public Slot GetParmsSlot(CodeGenerator cg)
  { if(namesSlot==null)
    { namesSlot = cg.TypeGenerator.AddStaticSlot(FuncName+"$parms"+index++, typeof(Parameter[]));
      CodeGenerator icg = cg.TypeGenerator.GetInitializer();
      ConstructorInfo nci = typeof(Name).GetConstructor(new Type[] { typeof(string), typeof(Scope) });
      ConstructorInfo pci = typeof(Parameter).GetConstructor(new Type[] { typeof(Name) });

      icg.EmitNewArray(typeof(Parameter), Parameters.Length);
      for(int i=0; i<Parameters.Length; i++)
      { icg.ILG.Emit(OpCodes.Dup);
        icg.EmitInt(i);
        icg.ILG.Emit(OpCodes.Ldelema, typeof(Parameter));
        icg.EmitString(Parameters[i].Name.String);
        icg.EmitInt((int)Parameters[i].Name.Scope);
        icg.EmitNew(nci);
        icg.EmitNew(pci);
        icg.ILG.Emit(OpCodes.Stobj, typeof(Parameter));
      }
      namesSlot.EmitSet(icg);
    }
    return namesSlot;
  }

  public CodeGenerator MakeImplMethod(CodeGenerator cg)
  { Name[] names = new Name[Parameters.Length]; 
    for(int i=0; i<Parameters.Length; i++) names[i] = Parameters[i].Name;

    Type[] parmTypes = Inherit==null ? new Type[] { typeof(object[]) }
                                     : new Type[] { typeof(CompiledFunction), typeof(object[]) };
    CodeGenerator icg = cg.TypeGenerator.DefineMethod(FuncName + "$f" + index++, typeof(object), parmTypes);
    LocalNamespace ns = new LocalNamespace(cg.Namespace, icg);
    icg.Namespace = ns;
    //icg.SetArgs(names, 1);
    ns.SetArgs(names, icg, new ArgSlot(icg.MethodBuilder, Inherit==null ? 0 : 1, "$names", typeof(object[])));

    if(Inherit!=null && Inherit.Length>0)
    { icg.EmitArgGet(0);
      icg.EmitFieldGet(typeof(CompiledFunction), "Closed");
      for(int i=0; i<Inherit.Length; i++)
      { if(i!=Inherit.Length-1) icg.ILG.Emit(OpCodes.Dup);
        icg.EmitInt(i);
        icg.ILG.Emit(OpCodes.Ldelem_Ref);
        ns.UnpackClosedVar(Inherit[i], icg);
      }
    }
    Body.Emit(icg);
    icg.EmitReturn(null);
    icg.Finish();
    return icg;
  }

  public object MakeFunction(Frame frame)
  { return new InterpretedFunction(frame, Name==null ? null : Name.String, Parameters, Globals, Body);
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this)) Body.Walk(w);
    w.PostWalk(this);
  }

  Slot namesSlot;
  static int index;
}

} // namespace Boa.AST