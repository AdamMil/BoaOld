using System;
using System.Collections;
using System.Collections.Specialized;

namespace Boa.Runtime
{

[AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface|AttributeTargets.Struct, Inherited=false)]
public class BoaTypeAttribute : Attribute
{ public BoaTypeAttribute(string name) { Name=name; }
  public string Name;
}

public abstract class BoaType : DynamicType, IDynamicObject, ICallable, IHasAttributes
{ public BoaType(Type type)
  { this.type=type; dict=new Dict();

    if(type==typeof(object)) __name__ = "object";
    else
    { BoaTypeAttribute attr = (BoaTypeAttribute)Attribute.GetCustomAttribute(type, typeof(BoaTypeAttribute), false);
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
  public List __attrs__()
  { List ret = dict.keys();
    ret.sort();
    return ret;
  }

  public void __delattr__(string name)
  { object slot = RawGetSlot(name);
    if(slot!=null && Ops.DelDescriptor(slot, null)) return;
    RawRemoveSlot(name);
  }

  public object __getattr__(string name)
  { object slot = LookupSlot(name);
    if(slot!=null) return Ops.GetDescriptor(slot, null);
    if(name=="__name__") return __name__;
    return Ops.Missing;
  }

  public void __setattr__(string name, object value)
  { object slot = RawGetSlot(name);
    if(slot!=null && Ops.SetDescriptor(slot, null, value)) return;
    RawSetSlot(name, value);
  }
  #endregion

  public override List GetAttrNames(object self) { return __attrs__(); }

  protected object LookupSlot(string name) { return RawGetSlot(name); }

  protected object RawGetSlot(string name)
  { if(name=="__dict__") return dict;
    /*if(name=="__getattr__") return getAttributeF;
    if(name=="__setattr__") return setAttributeF;
    if(name=="__cmp__") return cmpF;
    if(name=="__repr__") return reprF;*/
    return dict[name];
  }

  protected void RawRemoveSlot(string name) { dict.Remove(name); }

  protected void RawSetSlot(string name, object value) { dict[name] = value; }

  protected Dict dict;
  protected Type type;
}

} // namespace Boa.Runtime