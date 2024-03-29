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

  // TODO: implement a way to free temporaries
  public virtual Slot AllocTemp(Type type) { throw new NotImplementedException(); }

  public void BeginScope(Name[] names) { BeginScope(names, false); }
  public void BeginScope(Name[] names, bool keepAround)
  { if(names.Length==0) return;

    string[] snames = new string[names.Length];
    Slot[] oslots = new Slot[names.Length];
    for(int i=0; i<names.Length; i++)
    { string name;
      snames[i] = name = names[i].String;
      oslots[i] = (Slot)slots[name];
      slots[name] = codeGen.AllocLocalTemp(typeof(object), keepAround);
    }

    if(scopes==null) scopes = new Stack();
    scopes.Push(snames);
    scopes.Push(oslots);
  }
  
  public void EndScope()
  { Slot[] oslots  = (Slot[])scopes.Pop();
    string[] names = (string[])scopes.Pop();
    for(int i=0; i<oslots.Length; i++)
    { Slot s = oslots[i];
      string n = names[i];
      codeGen.FreeLocalTemp((Slot)slots[n]);
      if(s==null) slots.Remove(n);
      else slots[n] = s;
    }
  }

  // TODO: make sure this works with closures, etc
  public virtual void DeleteSlot(Name name) { slots.Remove(name.String); }
  public Slot GetLocalSlot(Name name) { return (Slot)slots[name.String]; } // does NOT make the slot!
  public Slot GetGlobalSlot(string name) { return GetGlobalSlot(new Name(name, Scope.Global)); }
  public Slot GetGlobalSlot(Name name) { return Parent==null ? GetSlot(name) : Parent.GetGlobalSlot(name); }

  public Slot GetSlotForGet(Name name)
  { Slot s = (Slot)slots[name.String];
    return s!=null ? s : name.Scope==Scope.Temporary ? GetSlot(name) : GetGlobalSlot(name);
  }
  public Slot GetSlotForSet(Name name) { return GetSlot(name); }
  
  public virtual void SetArgs(Name[] names, int offset, MethodBase mb)
  { throw new NotSupportedException("SetArgs: "+GetType());
  }

  public Namespace Parent, Global;

  protected abstract Slot MakeSlot(Name name);

  protected HybridDictionary slots = new HybridDictionary();
  protected CodeGenerator codeGen;

  Slot GetSlot(Name name)
  { Slot ret = (Slot)slots[name.String];
    if(ret==null)
    { if(name.Scope==Scope.Temporary)
        throw new ArgumentException("Temporary variables must be allocated with BeginScope()");
      ret = MakeSlot(name);
      slots[name.String] = ret;
    }
    return ret;
  }

  Stack scopes;
}
#endregion

#region FieldNamespace
public sealed class FieldNamespace : Namespace
{ public FieldNamespace(Namespace parent, string prefix, CodeGenerator cg) : base(parent, cg) { Prefix=prefix; }
  public FieldNamespace(Namespace parent, string prefix, CodeGenerator cg, Slot instance)
    : base(parent, cg) { this.instance=instance; Prefix=prefix; }
  
  public override Slot AllocTemp(Type type)
  { return new FieldSlot(instance, codeGen.TypeGenerator.TypeBuilder.DefineField("temp$"+Misc.NextIndex, type,
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

  public override void SetArgs(Name[] names, int offset, MethodBase mb)
  { for(; offset<names.Length; offset++) GetSlotForSet(names[offset]);
  }

  public string Prefix;

  Slot instance;
}
#endregion

#region FrameNamespace
public sealed class FrameNamespace : Namespace
{ public FrameNamespace(TypeGenerator tg, CodeGenerator cg) : base(null, cg)
  { Slot field = new StaticSlot(tg.TypeBuilder.DefineField("__frame", typeof(Frame),
                                                           FieldAttributes.Public|FieldAttributes.Static));
    FrameSlot = new FrameObjectSlot(cg, new ArgSlot((MethodBuilder)cg.MethodBase, 0, "frame"), field);
  }

  public override void DeleteSlot(Name name)
  { FrameSlot.EmitGet(codeGen);
    codeGen.EmitString(name.String);
    codeGen.EmitCall(typeof(Frame), "Delete");
  }

  public override void SetArgs(Name[] names, int offset, MethodBase mb)
  { foreach(Name name in names) slots[name] = MakeSlot(name);
  }

  public FrameObjectSlot FrameSlot;

  protected override Slot MakeSlot(Name name) { return new NamedFrameSlot(FrameSlot, name.String); }
}
#endregion

#region LocalNamespace
public sealed class LocalNamespace : Namespace
{ public LocalNamespace(Namespace parent, CodeGenerator cg) : base(parent, cg) { }

  public void AddClosedVars(Name[] names, Slot[] slots)
  { for(int i=0; i<names.Length; i++) this.slots[names[i].String] = slots[i];
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

  public void EmitLocalsDict(CodeGenerator cg)
  { MethodInfo add = typeof(HybridDictionary).GetMethod("Add", new Type[] { typeof(object), typeof(object) });

    cg.EmitNew(typeof(HybridDictionary));
    foreach(string name in slots.Keys)
    { cg.ILG.Emit(OpCodes.Dup);
      cg.EmitString(name);
      ((Slot)slots[name]).EmitGet(codeGen);
      cg.EmitCall(add);
    }
  }

  public override void SetArgs(Name[] names, int offset, MethodBase mb)
  { for(int i=0; i<names.Length; i++)
      slots[names[i].String] = new ArgSlot((MethodBuilder)mb, i+offset, names[i].String);
  }

  public void SetArgs(Name[] names, CodeGenerator cg, Slot objArray)
  { if(names.Length==0) return;
    // TODO: this should be optimized out by not using object[] param arrays everywhere
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

  protected override Slot MakeSlot(Name name)
  { switch(name.Scope)
    { case Scope.Free: case Scope.Global:
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
