using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Namespace
public abstract class Namespace
{ public Namespace(Namespace parent)
  { Parent = parent;
    Global = parent==null || parent.Parent==null ? parent : parent.Parent;
  }

  public virtual Slot AllocTemp(Type type) { throw new NotImplementedException(); }
  // TODO: make sure this works with closures, etc
  public virtual void DeleteSlot(Name name) { slots.Remove(name.String); }
  public Slot GetLocalSlot(Name name) { return (Slot)slots[name.String]; } // does NOT make the slot!
  public Slot GetGlobalSlot(string name) { return GetGlobalSlot(new Name(name, Scope.Global)); }
  public Slot GetGlobalSlot(Name name) { return Parent==null ? GetSlot(name) : Parent.GetGlobalSlot(name); }

  public Slot GetSlotForGet(Name name)
  { Slot s = (Slot)slots[name.String];
    return s==null ? GetGlobalSlot(name) : s;
  }
  public Slot GetSlotForSet(Name name) { return GetSlot(name); }
  
  public virtual void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { throw new NotSupportedException("SetArgs: "+GetType());
  }

  public Namespace Parent, Global;
  
  protected abstract Slot MakeSlot(Name name);
  protected HybridDictionary slots = new HybridDictionary();

  Slot GetSlot(Name name)
  { Slot ret = (Slot)slots[name.String];
    if(ret!=null) return ret;
    slots[name.String] = ret = MakeSlot(name);
    return ret;
  }
}
#endregion

#region FieldNamespace
public class FieldNamespace : Namespace
{ public FieldNamespace(Namespace parent, string prefix, TypeGenerator tg) : base(parent)
  { this.tg=tg; Prefix=prefix;
  }
  public FieldNamespace(Namespace parent, string prefix, TypeGenerator tg, Slot instance)
    : base(parent) { this.tg=tg; this.instance=instance; Prefix=prefix; }
  
  public override Slot AllocTemp(Type type)
  { return new FieldSlot(instance, tg.TypeBuilder.DefineField("temp$"+count++, type, FieldAttributes.Public));
  }

  protected override Slot MakeSlot(Name name)
  { FieldInfo info = tg.TypeBuilder.DefineField(Prefix+name.String, typeof(object), FieldAttributes.Public);
    return new FieldSlot(instance, info);
  }

  public override void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { for(; offset<names.Length; offset++) GetSlotForSet(names[offset]);
  }

  public string Prefix;

  TypeGenerator tg;
  Slot instance;
  
  static int count;
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

  public override void DeleteSlot(Name name)
  { FrameSlot.EmitGet(codeGen);
    codeGen.EmitString(name.String);
    codeGen.EmitCall(typeof(Frame), "Delete");
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

  public override void DeleteSlot(Name name)
  { if(name.Scope==Scope.Global) // TODO: handle Free variables here?
    { Namespace par = Parent;
      while(par!=null && !(par is FrameNamespace)) par = par.Parent;
      if(par==null) throw new InvalidOperationException("There is no FrameNamespace in the hierachy");
      par.DeleteSlot(name);
    }
    else
    { codeGen.ILG.Emit(OpCodes.Ldnull);
      GetSlotForSet(name).EmitSet(codeGen);
    }
  }

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
  { switch(name.Scope)
    { case Scope.Closed: return new ClosedSlot(codeGen, name.String);
      case Scope.Free: case Scope.Global:
      { Namespace par = Parent;
        while(par!=null && !(par is FrameNamespace)) par = par.Parent;
        if(par==null) throw new InvalidOperationException("There is no FrameNamespace in the hierachy");
        return par.GetGlobalSlot(name);
      }
      case Scope.Local: return new LocalSlot(codeGen.ILG.DeclareLocal(typeof(object)), name.String);
      default: throw new Exception("unhandled scope type");
    }
  }

  CodeGenerator codeGen;
}
#endregion

} // namespace Boa.AST
