using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Language.Runtime;

namespace Language.AST
{

#region Slot
public abstract class Slot
{ public abstract Type Type { get; }

  public abstract void EmitGet(CodeGenerator cg);
  public abstract void EmitGetAddr(CodeGenerator cg);

  public abstract void EmitSet(CodeGenerator cg);
  public virtual void EmitSet(CodeGenerator cg, Slot val) { val.EmitGet(cg); EmitSet(cg); }
}
#endregion

#region ArgSlot
public class ArgSlot : Slot
{ public ArgSlot(MethodBuilder methodBuilder, int index, string name)
    : this(methodBuilder, index, name, typeof(object)) { }
  public ArgSlot(MethodBuilder methodBuilder, int index, string name, Type type)
    : this(methodBuilder, methodBuilder.DefineParameter(index+1, ParameterAttributes.None, name), type) { }
  public ArgSlot(MethodBuilder methodBuilder, ParameterBuilder parameterBuilder, Type type)
  { builder   = parameterBuilder;
    isStatic  = methodBuilder.IsStatic;
    this.type = type;
  }

  public override Type Type { get { return type; } }

  public override void EmitGet(CodeGenerator cg) { cg.EmitArgGet(builder.Position-1); }
  public override void EmitGetAddr(CodeGenerator cg) { cg.EmitArgGetAddr(builder.Position-1); }
  public override void EmitSet(CodeGenerator cg) { cg.EmitArgSet(builder.Position-1); }

  ParameterBuilder builder;
  Type type;
  bool isStatic;
}
#endregion

#region LocalSlot
public class LocalSlot : Slot
{ public LocalSlot(LocalBuilder lb) { builder = lb; }
  
  public override Type Type { get { return builder.LocalType; } }

  public override void EmitGet(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Ldloc, builder); }
  public override void EmitGetAddr(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Ldloca, builder); }
  public override void EmitSet(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Stloc, builder); }

  LocalBuilder builder;
}
#endregion

#region FrameObjectSlot
public class FrameObjectSlot : Slot
{ public FrameObjectSlot(CodeGenerator baseCg, ArgSlot argSlot, Slot fieldSlot)
  { BaseCodeGenerator=baseCg; ArgSlot=argSlot; FieldSlot=fieldSlot;
  }

  public override Type Type { get { return typeof(Frame); } }

  public override void EmitGet(CodeGenerator cg)
  { if(BaseCodeGenerator==cg) ArgSlot.EmitGet(cg);
    else FieldSlot.EmitGet(cg);
  }
  
  public override void EmitGetAddr(CodeGenerator cg)
  { throw new NotSupportedException("the address of the frame slot cannot be retrieved");
  }

  public override void EmitSet(CodeGenerator cg)
  { FieldSlot.EmitSet(cg); // only used when setting this initially, for functions besides func(Frame)
  }

  public ArgSlot ArgSlot;
  public Slot FieldSlot;
  public CodeGenerator BaseCodeGenerator;
}
#endregion

#region NamedFrameSlot
public class NamedFrameSlot : Slot
{ public NamedFrameSlot(Slot frame, string name) { Frame=frame; Name=name; }

  public override Type Type { get { return typeof(object); } }

  public override void EmitGet(CodeGenerator cg)
  { Frame.EmitGet(cg);
    cg.EmitString(Name);
    cg.EmitCall(typeof(Frame), "GetGlobal");
  }
  
  public override void EmitGetAddr(CodeGenerator cg)
  { throw new NotImplementedException("address of frame slot");
  }

  public override void EmitSet(CodeGenerator cg)
  { Slot temp = cg.AllocLocalTemp(typeof(object));
    temp.EmitSet(cg);
    EmitSet(cg, temp);
    cg.FreeLocalTemp(temp);
  }

  public override void EmitSet(CodeGenerator cg, Slot val)
  { Frame.EmitGet(cg);
    cg.EmitString(Name);
    val.EmitGet(cg);
    cg.EmitCall(typeof(Frame), "SetGlobal");
  }

  public Slot Frame;
  public string Name;
}
#endregion

#region StaticSlot
public class StaticSlot : Slot
{ public StaticSlot(FieldInfo field) { this.field=field; }

  public override Type Type { get { return field.FieldType; } }

  public override void EmitGet(CodeGenerator cg) { cg.EmitFieldGet(field); }
  public override void EmitGetAddr(CodeGenerator cg) { cg.EmitFieldGetAddr(field); }
  public override void EmitSet(CodeGenerator cg) { cg.EmitFieldSet(field); }

  FieldInfo field;
}
#endregion

} // namespace Language.AST