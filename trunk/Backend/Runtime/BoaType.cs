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
  { this.type=type;

    if(type==typeof(object)) __name__ = "object";
    else if(type==typeof(string)) __name__ = "string";
    else if(type==typeof(int)) __name__ = "int";
    else if(type==typeof(double)) __name__ = "float";
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
  { Initialize();
    return dict.keys();
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

  // TODO: should this include virtual slots like __dict__ ?
  public override List GetAttrNames(object self) { return __attrs__(); }
  public override bool GetAttr(object self, string name, out object value)
  { value = __getattr__(name);
    return value!=Ops.Missing;
  }

  internal object LookupSlot(string name) { return RawGetSlot(name); }

  protected virtual void Initialize() { dict=new Dict(); initialized=true; }

  protected object RawGetSlot(string name)
  { Initialize();
    if(name=="__dict__") return dict;
    return dict[name];
  }

  protected void RawRemoveSlot(string name) { dict.Remove(name); }

  protected void RawSetSlot(string name, object value) { dict[name] = value; }

  protected Dict dict;
  protected Type type;
  protected bool initialized;
}

} // namespace Boa.Runtime