using System;
using System.Collections;

namespace Boa.Runtime
{

public abstract class DynamicType
{ public virtual void DelAttr(object self, string name)
  { throw new NotImplementedException();
  }

  public object GetAttr(object self, string name)
  { object value;
    if(GetAttr(self, name, out value)) return value;
    throw Ops.AttributeError("'{0}' object has no attribute '{1}'", __name__, name);
  }

  public virtual bool GetAttr(object self, string name, out object value)
  { value = null;
    return false;
  }

  public virtual List GetAttrNames(object self) { return new List(0); }

  public virtual bool IsSubclassOf(object other) { throw new NotImplementedException(); }

  public virtual string Repr(object self)
  { object ret;
    if(Ops.TryInvoke(self, "__repr__", out ret)) return (string)ret;
    return self.ToString();
  }
  
  public virtual void SetAttr(object self, string name, object value)
  { throw Ops.AttributeError("'{0}' object has no attribute '{1}'", __name__, name);
  }

  public object __name__;
}

public class NoneType : DynamicType
{ NoneType() { __name__ = "NoneType"; }

  public override bool IsSubclassOf(object other) { return other==this; }

  public static readonly NoneType Value = new NoneType();
}

} // namespace Boa.Runtime