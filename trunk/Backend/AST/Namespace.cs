using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Namespace
public abstract class Namespace
{ public Namespace(Namespace parent) { Parent=parent; }

  public Slot GetLocalSlot(Name name) { return (Slot)slots[name.String]; }
  public Slot GetSlotForGet(Name name)
  { Slot s = (Slot)slots[name.String];
    return s==null ? GetGlobalSlot(name) : s;
  }
  public Slot GetSlotForSet(Name name) { return GetSlot(name); }
  
  public virtual void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { throw new NotImplementedException("SetArgs: "+GetType());
  }

  public Namespace Parent;
  
  protected abstract Slot MakeSlot(Name name);
  protected Hashtable slots = new Hashtable();

  Slot GetGlobalSlot(Name name) { return Parent==null ? GetSlot(name) : Parent.GetGlobalSlot(name); }
  Slot GetSlot(Name name)
  { Slot ret = (Slot)slots[name.String];
    if(ret!=null) return ret;
    slots[name.String] = ret = MakeSlot(name);
    return ret;
  }
  Slot MakeGlobalSlot(Name name) { return Parent==null ? MakeSlot(name) : Parent.MakeGlobalSlot(name); }
}
#endregion

#region FrameNamespace
public class FrameNamespace : Namespace
{ public FrameNamespace(TypeGenerator tg, CodeGenerator cg) : base(null)
  { codeGen = cg;
    Slot field = new StaticSlot(tg.TypeBuilder.DefineField("__frame", typeof(Frame),
                                                           FieldAttributes.Public|FieldAttributes.Static));
    FrameSlot = new FrameObjectSlot(cg, new ArgSlot(cg.MethodBuilder, 0, "frame"), field);
  }

  public override void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { foreach(Name name in names) slots[name.String] = MakeSlot(name);
  }

  public FrameObjectSlot FrameSlot;

  protected override Slot MakeSlot(Name name) { return new NamedFrameSlot(FrameSlot, name.String); }

  CodeGenerator codeGen;
}
#endregion

#region LocalNamespace
public class LocalNamespace : Namespace
{ public LocalNamespace(Namespace parent, CodeGenerator cg) : base(parent) { codeGen=cg; }

  public override void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { for(int i=0; i<names.Length; i++) slots[names[i].String] = new ArgSlot(mb, i+offset, names[i].String);
  }
  public void SetArgs(Name[] names, CodeGenerator cg, Slot objArray)
  { if(names.Length==0) return;
    objArray.EmitGet(cg);
    for(int i=0; i<names.Length; i++)
    { if(i!=names.Length-1) cg.ILG.Emit(OpCodes.Dup);
      cg.EmitInt(i);
      cg.ILG.Emit(OpCodes.Ldelem_Ref);
      Slot slot = new LocalSlot(cg.ILG.DeclareLocal(typeof(object)), names[i].String);
      slot.EmitSet(cg);
      slots[names[i].String] = slot;
    }
  }

  public void UnpackClosedVar(Name name, CodeGenerator cg)
  { Slot slot = new LocalSlot(codeGen.ILG.DeclareLocal(typeof(ClosedVar)), name.String);
    slot.EmitSet(cg);
    slots[name.String] = new ClosedSlot(slot);
  }

  protected override Slot MakeSlot(Name name)
  { return name.Scope==Scope.Closed ? (Slot)new ClosedSlot(codeGen, name.String)
                                    : (Slot)new LocalSlot(codeGen.ILG.DeclareLocal(typeof(object)), name.String);
  }

  CodeGenerator codeGen;
}
#endregion

} // namespace Boa.AST
