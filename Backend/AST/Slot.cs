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
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
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
public sealed class ArgSlot : Slot
{ public ArgSlot(MethodBuilder mb, int index, string name) : this(mb, index, name, typeof(object)) { }
  public ArgSlot(MethodBuilder mb, int index, string name, Type type)
    : this(mb, mb.DefineParameter(index+1, ParameterAttributes.None, name), type) { }
  public ArgSlot(MethodBase mb, ParameterBuilder parameterBuilder, Type type)
  { builder   = parameterBuilder;
    isStatic  = mb.IsStatic;
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

#region FieldSlot
public sealed class FieldSlot : Slot
{ public FieldSlot(FieldInfo fi) { Info=fi; }
  public FieldSlot(Slot instance, FieldInfo fi) { Instance=instance; Info=fi; }

  public override Type Type { get { return Info.FieldType; } }

  public override void EmitGet(CodeGenerator cg)
  { if(Instance!=null) Instance.EmitGet(cg);
    cg.EmitFieldGet(Info);
  }

  public override void EmitGetAddr(CodeGenerator cg)
  { if(Instance!=null) Instance.EmitGet(cg);
    cg.EmitFieldGetAddr(Info);
  }

  public override void EmitSet(CodeGenerator cg)
  { if(Instance==null) cg.EmitFieldSet(Info);
    Slot temp = cg.AllocLocalTemp(Info.FieldType);
    temp.EmitSet(cg);
    EmitSet(cg, temp);
    cg.FreeLocalTemp(temp);
  }
  public override void EmitSet(CodeGenerator cg, Slot val)
  { if(Instance!=null) Instance.EmitGet(cg);
    val.EmitGet(cg);
    cg.EmitFieldSet(Info);
  }

  public FieldInfo Info;
  public Slot Instance;
}
#endregion

#region FrameObjectSlot
public sealed class FrameObjectSlot : Slot
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

#region LocalSlot
public sealed class LocalSlot : Slot
{ public LocalSlot(LocalBuilder lb) { builder = lb; }
  public LocalSlot(LocalBuilder lb, string name)
  { builder = lb; 
    if(Options.Debug) lb.SetLocalSymInfo(name);
  }
  
  public override Type Type { get { return builder.LocalType; } }

  public override void EmitGet(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Ldloc, builder); }
  public override void EmitGetAddr(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Ldloca, builder); }
  public override void EmitSet(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Stloc, builder); }

  LocalBuilder builder;
}
#endregion

#region NamedFrameSlot
public sealed class NamedFrameSlot : Slot
{ public NamedFrameSlot(Slot frame, string name) { Frame=frame; Name=name; }

  public override Type Type { get { return typeof(object); } }

  public override void EmitGet(CodeGenerator cg)
  { Frame.EmitGet(cg);
    cg.EmitString(Name);
    cg.EmitCall(typeof(Frame), "GetGlobal");
  }
  
  public override void EmitGetAddr(CodeGenerator cg)
  { throw new NotSupportedException("address of frame slot");
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
public sealed class StaticSlot : Slot
{ public StaticSlot(FieldInfo field) { this.field=field; }

  public override Type Type { get { return field.FieldType; } }

  public override void EmitGet(CodeGenerator cg) { cg.EmitFieldGet(field); }
  public override void EmitGetAddr(CodeGenerator cg) { cg.EmitFieldGetAddr(field); }
  public override void EmitSet(CodeGenerator cg) { cg.EmitFieldSet(field); }

  FieldInfo field;
}
#endregion

#region ThisSlot
public sealed class ThisSlot : Slot
{ public ThisSlot(Type type) { this.type=type; }

  public override Type Type { get { return type; } }

  public override void EmitGet(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Ldarg_0); }
  public override void EmitGetAddr(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Ldarga, 0); }
  public override void EmitSet(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Starg, 0); }
  
  Type type;
}
#endregion

} // namespace Boa.AST
