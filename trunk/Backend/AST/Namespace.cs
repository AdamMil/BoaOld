using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Language.Runtime;

namespace Language.AST
{

#region Namespace
public abstract class Namespace
{ public Namespace(Namespace parent) { Parent=parent; }

  public Slot GetSlotForGet(string name)
  { Slot s = (Slot)slots[name];
    return s==null ? GetGlobalSlot(name) : s;
  }
  public Slot GetSlotForSet(string name) { return GetSlot(name); }
  
  public virtual void SetArgs(string[] names, MethodBuilder mb)
  { throw new NotImplementedException("SetArgs: "+GetType());
  }

  public Namespace Parent;
  
  protected abstract Slot MakeSlot(string name);
  protected Hashtable slots = new Hashtable();

  Slot GetGlobalSlot(string name) { return Parent==null ? GetSlot(name) : Parent.GetGlobalSlot(name); }
  Slot GetSlot(string name)
  { Slot ret = (Slot)slots[name];
    if(ret!=null) return ret;
    slots[name] = ret = MakeSlot(name);
    return ret;
  }
  Slot MakeGlobalSlot(string name) { return Parent==null ? MakeSlot(name) : Parent.MakeGlobalSlot(name); }
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

  public override void SetArgs(string[] names, MethodBuilder mb)
  { foreach(string name in names) slots[name] = MakeSlot(name);
  }

  public FrameObjectSlot FrameSlot;

  protected override Slot MakeSlot(string name) { return new NamedFrameSlot(FrameSlot, name); }

  CodeGenerator codeGen;
}
#endregion

#region LocalNamespace
public class LocalNamespace : Namespace
{ public LocalNamespace(Namespace parent, CodeGenerator cg) : base(parent) { codeGen=cg; }
  
  public override void SetArgs(string[] names, MethodBuilder mb)
  { for(int i=0; i<names.Length; i++) slots[names[i]] = new ArgSlot(mb, i, names[i]);
  }

  protected override Slot MakeSlot(string name)
  { LocalBuilder b = codeGen.ILG.DeclareLocal(typeof(object));
    //b.SetLocalSymInfo(name);
    return new LocalSlot(b);
  }

  CodeGenerator codeGen;
}
#endregion

} // namespace Language.AST