using System;
using System.Collections;
using System.Collections.Specialized;
using Boa.AST;

namespace Boa.Runtime
{

public class MethodWrapper : IDescriptor, IFancyCallable
{ public MethodWrapper(Function func) { this.func = func; }
  public MethodWrapper(Function func, object instance) { this.func=func; this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new MethodWrapper(func, instance); }

  public object Call(object[] positional, string[] names, object[] values)
  { object[] npos = new object[positional.Length+1];
    npos[0] = instance;
    if(positional.Length>0) Array.Copy(positional, 0, npos, 1, positional.Length);
    return func.Call(npos, names, values);
  }

  object Boa.Runtime.ICallable.Call(params object[] args)
  { object[] nargs = new object[args.Length+1];
    nargs[0] = instance;
    if(args.Length>0) Array.Copy(args, 0, nargs, 1, args.Length);
    return func.Call(nargs);
  }
  
  Function func;
  object instance;
}

public class UserInstance : IDynamicObject
{ public UserInstance(UserType type, IDictionary dict)
  { __class__ = type;
    __dict__  = new Dict(dict);

    foreach(DictionaryEntry e in dict)
    { Function func = e.Value as Function;
      if(func!=null) __dict__[e.Key] = new MethodWrapper(func);
    }
  }

  public UserType __class__;
  public Dict __dict__;

  public DynamicType GetDynamicType() { return __class__; }
}

public class UserType : BoaType
{ public UserType(string module, string name, Tuple bases, IDictionary dict) : base(typeof(UserType))
  { Initialize();
    __name__   = name;
    __module__ = module;
    this.bases = bases;
    this.dict.update(dict);
  }

  public Tuple __bases__ { get { return bases; } }

  public override object Call(params object[] args)
  { object obj, dummy;
    //if(!Ops.TryInvoke(this, "__new__", out obj, nargs)) obj = cons.Call(args);
    obj = new UserInstance(this, dict);

    if(obj!=null) Ops.TryInvoke(obj, "__init__", out dummy, args);
    return obj;
  }

  public override void DelAttr(object self, string name)
  { UserInstance ui = (UserInstance)self;
    if(ui.__dict__.Contains(name)) ui.__dict__.Remove(name); // TODO: use descriptors
    else
    { object slot = RawGetSlot(name);
      if(slot!=null) Ops.DelDescriptor(slot, self);
      else throw Ops.AttributeError("no such slot '{0}'", name);
    }
  }

  public override bool GetAttr(object self, string name, out object value)
  { UserInstance ui = (UserInstance)self;
    if(ui.__dict__.Contains(name))
    { value = Ops.GetDescriptor(ui.__dict__[name], self);
      return true;
    }

    object slot = RawGetSlot(name);
    if(slot!=null)
    { value = Ops.GetDescriptor(slot, self);
      return true;
    }

    value = null;
    return false;
  }

  public override DynamicType GetDynamicType() { return ReflectedType.FromType(typeof(UserType)); } // TODO: cache somewhere?

  public override bool IsSubclassOf(object other) // TODO: this is not right
  { if(this==other) return true;
    ReflectedType rt = other as ReflectedType;
    if(rt!=null) return type==rt.Type || type.IsSubclassOf(rt.Type);
    foreach(BoaType bt in bases.items) if(bt.IsSubclassOf(other)) return true;
    return false;
  }

  public override void SetAttr(object self, string name, object value)
  { ((UserInstance)self).__dict__[name] = value; // TODO: use descriptors
  }

  public override string ToString() { return string.Format("<class '{0}.{1}'>", __module__, __name__); }

  public object __module__;
  
  Tuple bases;
}

} // namespace Boa.Runtime