/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

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
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Namespace
public abstract class Namespace
{ public Namespace(Namespace parent, CodeGenerator cg)
  { Parent  = parent;
    Global  = parent==null || parent.Parent==null ? parent : parent.Parent;
    codeGen = cg;
  }

  public virtual Slot AllocTemp(Type type) { throw new NotImplementedException(); }
  // TODO: make sure this works with closures, etc
  public virtual void DeleteSlot(Name name) { slots.Remove(name.String); }
  public Slot GetLocalSlot(Name name) { return (Slot)slots[name.String]; } // does NOT make the slot!
  public Slot GetGlobalSlot(string name) { return GetGlobalSlot(new Name(name, Scope.Global)); }
  public Slot GetGlobalSlot(Name name) { return Parent==null ? GetSlot(name) : Parent.GetGlobalSlot(name); }

  public Slot GetSlotForGet(Name name)
  { Slot s = (Slot)slots[name.String];
    return s!=null ? s : name.Scope==Scope.Private ? GetSlot(name) : GetGlobalSlot(name);
  }
  public Slot GetSlotForSet(Name name) { return GetSlot(name); }
  
  public virtual void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { throw new NotSupportedException("SetArgs: "+GetType());
  }

  public Namespace Parent, Global;
  
  protected string GetKey(Name name) { return name.Scope==Scope.Private ? "private$"+name.String : name.String; }

  protected abstract Slot MakeSlot(Name name);

  protected HybridDictionary slots = new HybridDictionary();
  protected CodeGenerator codeGen;

  Slot GetSlot(Name name)
  { string key = GetKey(name);
    Slot ret = (Slot)slots[key];
    if(ret==null)
    { ret = name.Scope==Scope.Private ? new LocalSlot(codeGen.ILG.DeclareLocal(typeof(object))) : MakeSlot(name);
      slots[key] = ret;
    }
    return ret;
  }
}
#endregion

#region FieldNamespace
public class FieldNamespace : Namespace
{ public FieldNamespace(Namespace parent, string prefix, CodeGenerator cg) : base(parent, cg) { Prefix=prefix; }
  public FieldNamespace(Namespace parent, string prefix, CodeGenerator cg, Slot instance)
    : base(parent, cg) { this.instance=instance; Prefix=prefix; }
  
  public override Slot AllocTemp(Type type)
  { return new FieldSlot(instance, codeGen.TypeGenerator.TypeBuilder.DefineField("temp$"+count++, type,
                                                                                 FieldAttributes.Public));
  }

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

  protected override Slot MakeSlot(Name name)
  { if(name.Scope==Scope.Global)
    { Namespace par = Parent;
      while(par!=null && !(par is FrameNamespace)) par = par.Parent;
      if(par==null) throw new InvalidOperationException("There is no FrameNamespace in the hierachy");
      return par.GetGlobalSlot(name);
    }
    else
    { return new FieldSlot(instance, codeGen.TypeGenerator.TypeBuilder.DefineField(Prefix+name.String, typeof(object),
                                                                                   FieldAttributes.Public));
    }
  }

  public override void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { for(; offset<names.Length; offset++) GetSlotForSet(names[offset]);
  }

  public string Prefix;

  Slot instance;
  
  static int count;
}
#endregion

#region FrameNamespace
public class FrameNamespace : Namespace
{ public FrameNamespace(TypeGenerator tg, CodeGenerator cg) : base(null, cg)
  { Slot field = new StaticSlot(tg.TypeBuilder.DefineField("__frame", typeof(Frame),
                                                           FieldAttributes.Public|FieldAttributes.Static));
    FrameSlot = new FrameObjectSlot(cg, new ArgSlot(cg.MethodBuilder, 0, "frame"), field);
  }

  public override void DeleteSlot(Name name)
  { FrameSlot.EmitGet(codeGen);
    codeGen.EmitString(name.String);
    codeGen.EmitCall(typeof(Frame), "Delete");
  }

  public override void SetArgs(Name[] names, int offset, MethodBuilder mb)
  { foreach(Name name in names) slots[GetKey(name)] = MakeSlot(name);
  }

  public FrameObjectSlot FrameSlot;

  protected override Slot MakeSlot(Name name)
  { if(name.Scope==Scope.Private) throw new NotImplementedException();
    return new NamedFrameSlot(FrameSlot, name.String);
  }
}
#endregion

#region LocalNamespace
public class LocalNamespace : Namespace
{ public LocalNamespace(Namespace parent, CodeGenerator cg) : base(parent, cg) { }

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
}
#endregion

} // namespace Boa.AST
