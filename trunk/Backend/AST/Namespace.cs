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

  public Slot GetSlotForGet(Name name)
  { Slot s = (Slot)slots[name];
    return s==null ? GetGlobalSlot(name) : s;
  }
  public Slot GetSlotForSet(Name name) { return GetSlot(name); }
  
  public virtual void SetArgs(Name[] names, MethodBuilder mb)
  { throw new NotImplementedException("SetArgs: "+GetType());
  }

  public Namespace Parent;
  
  protected abstract Slot MakeSlot(Name name);
  protected Hashtable slots = new Hashtable();

  Slot GetGlobalSlot(Name name) { return Parent==null ? GetSlot(name) : Parent.GetGlobalSlot(name); }
  Slot GetSlot(Name name)
  { Slot ret = (Slot)slots[name];
    if(ret!=null) return ret;
    slots[name] = ret = MakeSlot(name);
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

  public override void SetArgs(Name[] names, MethodBuilder mb)
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

  public override void SetArgs(Name[] names, MethodBuilder mb)
  { for(int i=0; i<names.Length; i++)
    { Slot arg = new ArgSlot(mb, i, names[i].String);
      slots[names[i].String] = names[i].Type==Name.Scope.Closed ? new ClosedSlot(names[i], codeGen, arg) : arg;
    }
  }

  protected override Slot MakeSlot(Name name)
  { if(name.Type==Name.Scope.Closed) return new ClosedSlot(name, codeGen);
    LocalBuilder b = codeGen.ILG.DeclareLocal(typeof(object));
    // TODO: reenable this
    //b.SetLocalSymInfo(name);
    return new LocalSlot(b);
  }

  CodeGenerator codeGen;
}
#endregion

} // namespace Boa.AST
