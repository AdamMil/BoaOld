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

namespace Boa.Runtime
{

[AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface|AttributeTargets.Struct)]
public class BoaTypeAttribute : Attribute
{ public BoaTypeAttribute(string name) { Name=name; }
  public string Name;
}

public abstract class BoaType : DynamicType, IDynamicObject, ICallable, IHasAttributes
{ public BoaType(Type type)
  { this.type=type;
    inheritType = type;
    while(typeof(IInstance).IsAssignableFrom(inheritType)) inheritType = inheritType.BaseType;

    if(type==typeof(object)) __name__ = "object";
    else if(type==typeof(string)) __name__ = "string";
    else if(type==typeof(int)) __name__ = "int";
    else if(type==typeof(double)) __name__ = "float";
    else
    { BoaTypeAttribute attr = (BoaTypeAttribute)Attribute.GetCustomAttribute(type, typeof(BoaTypeAttribute));
      __name__ = attr==null ? type.FullName : attr.Name;
    }
  }

  #region ICallable
  public abstract object Call(params object[] args);
  #endregion

  #region IDynamicObject
  public abstract DynamicType GetDynamicType();
  #endregion

  #region IHasAttributes
  // TODO: this should include virtual slots like __name__, __dict__, etc., I think
  public virtual List __attrs__()
  { Initialize();
    return dict.keys();
  }

  public void __delattr__(string name)
  { object slot = RawGetSlot(name);
    if(slot!=Ops.Missing && Ops.DelDescriptor(slot, null)) return;
    RawRemoveSlot(name);
  }

  public object __getattr__(string name)
  { object slot = LookupSlot(name);
    if(slot!=Ops.Missing) return Ops.GetDescriptor(slot, null);
    if(name=="__name__") return __name__;
    return Ops.Missing;
  }

  public void __setattr__(string name, object value)
  { object slot = RawGetSlot(name);
    if(slot!=Ops.Missing && Ops.SetDescriptor(slot, null, value)) return;
    RawSetSlot(name, value);
  }
  #endregion

  public Type RealType { get { return type; } }
  public Type TypeToInheritFrom { get { return inheritType; } }

  public virtual void DelAttr(Tuple mro, int index, object self, string name) { DelAttr(self, name); }

  public override List GetAttrNames(object self) { return __attrs__(); }

  public virtual object GetAttr(Tuple mro, int index, object self, string name) { return GetAttr(self, name); }
  public override bool GetAttr(object self, string name, out object value)
  { value = __getattr__(name);
    return value!=Ops.Missing;
  }

  internal object LookupSlot(string name)
  { foreach(BoaType type in mro)
    { object slot = type.RawGetSlot(name);
      if(slot!=Ops.Missing) return slot;
    }
    return Ops.Missing;
  }
  
  protected object LookupSlot(Tuple mro, int index, string name)
  { for(; index<mro.Count; index++)
    { object slot = ((BoaType)mro.items[index]).RawGetSlot(name);
      if(slot!=Ops.Missing) return slot;
    }
    return Ops.Missing;
  }

  internal Tuple mro;

  protected virtual void Initialize()
  { if(!initialized)
    { dict=new Dict();
      initialized=true;
    }
  }

  protected object RawGetSlot(string name)
  { Initialize();
    if(name=="__dict__") return dict;
    if(name=="mro") return mro;
    object ret = dict[name];
    if(ret==null && !dict.Contains(name)) ret = Ops.Missing;
    return ret;
  }

  protected void RawRemoveSlot(string name) { dict.Remove(name); }

  protected void RawSetSlot(string name, object value) { dict[name] = value; }

  public virtual void SetAttr(Tuple mro, int index, object self, string name, object value)
  { SetAttr(self, name, value);
  }

  protected Dict dict;
  protected Type type, inheritType;
  protected bool initialized;
}

} // namespace Boa.Runtime