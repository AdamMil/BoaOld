using System;
using System.Collections;
using System.Collections.Specialized;
using Boa.AST;

namespace Boa.Runtime
{

// FIXME: this doesn't work properly for derived classes
public class ClassWrapper : IDescriptor, IFancyCallable
{ public ClassWrapper(Function func, BoaType type) { this.func=func; this.type=type; }

  public object __get__(object instance)
  { return instance==null ? this : new ClassWrapper(func, ((Instance)instance).__class__);
  }

  public object Call(object[] positional, string[] names, object[] values)
  { object[] npos = new object[positional.Length+1];
    npos[0] = type;
    if(positional.Length>0) Array.Copy(positional, 0, npos, 1, positional.Length);
    return func.Call(npos, names, values);
  }

  object Boa.Runtime.ICallable.Call(params object[] args)
  { object[] nargs = new object[args.Length+1];
    nargs[0] = type;
    if(args.Length>0) Array.Copy(args, 0, nargs, 1, args.Length);
    return func.Call(nargs);
  }

  public override string ToString() { return string.Format("<classmethod '{0}' on '{1}'>", func.Name, type.__name__); }

  Function func;
  BoaType type;
}

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

  public override string ToString() { return string.Format("<method '{0}'>", func.Name); }

  Function func;
  object instance;
}

public class Instance : IDynamicObject
{ public Instance(UserType type)
  { __class__ = type;
    __dict__  = new Dict();
  }

  public DynamicType GetDynamicType() { return __class__; }
  public override string ToString() { return string.Format("<'{0}' instance>", __class__.__name__); }

  public UserType __class__;
  public Dict __dict__;
}

public class UserType : BoaType
{ public UserType(string module, string name, Tuple bases, IDictionary dict) : base(typeof(object)) // TODO: use a better rule for determining the base class type
  { Initialize();
    __name__   = name;
    __module__ = module;
    this.bases = bases;

    foreach(DictionaryEntry e in dict)
    { Function func = e.Value as Function;
      this.dict[e.Key] = func!=null && func.Type==FunctionType.Class ? new ClassWrapper(func, this) : e.Value;
    }
    
    List[] mros = new List[bases.Count+1];
    mros[0] = new List(bases.items);
    for(int i=0; i<bases.Count; i++) mros[i+1] = new List(((BoaType)bases.items[i]).mro);

    List mrolist = new List();
    mrolist.Add(this);
    bool done=false;
    do mrolist.Add(MergeMRO(mros, ref done)); while(!done);
    
    mro = new Tuple(mrolist);
  }

  public Tuple __bases__ { get { return bases; } }

  public override object Call(params object[] args)
  { object obj, dummy;
    // TODO: implement __new__
    //if(!Ops.TryInvoke(this, "__new__", out obj, nargs)) obj = cons.Call(args);
    obj = new Instance(this);

    if(obj!=null) Ops.TryInvoke(obj, "__init__", out dummy, args);
    return obj;
  }

  public override void DelAttr(Tuple mro, int index, object self, string name)
  { Instance ui = (Instance)self;
    if(ui!=null && ui.__dict__.Contains(name)) ui.__dict__.Remove(name);
    else
    { object slot = LookupSlot(mro, index, name);
      if(slot!=null) Ops.DelDescriptor(slot, self);
      else throw Ops.AttributeError("no such slot '{0}'", name);
    }
  }
  public override void DelAttr(object self, string name) { DelAttr(mro, 0, self, name); }

  public override object GetAttr(Tuple mro, int index, object self, string name)
  { object value;
    if(!GetAttr(mro, index, self, name, out value)) throw Ops.AttributeError("no such slot '{0}'", name);
    return value;
  }
  
  public bool GetAttr(Tuple mro, int index, object self, string name, out object value)
  { object slot = LookupSlot(mro, index, name);

    if(slot!=null)
    { Function func = slot as Function;
      if(func!=null && (func.Type==FunctionType.Unmarked || func.Type==FunctionType.Method))
        value = new MethodWrapper(func, self);
      else value = Ops.GetDescriptor(slot, self);
      return true;
    }

    value = null;
    return false;
  }

  public override bool GetAttr(object self, string name, out object value)
  { Instance ui = (Instance)self;

    if(self!=null)
    { if(name=="__dict__")
      { value = ui.__dict__;
        return true;
      }
      if(name=="__class__")
      { value = ui.__class__;
        return true;
      }

      if(ui.__dict__.Contains(name))
      { value = Ops.GetDescriptor(ui.__dict__[name], self);
        return true;
      }
    }

    return GetAttr(mro, 0, self, name, out value);
  }

  public override List GetAttrNames(object self)
  { Dict keys = new Dict();
    if(self!=null)
    { Instance ui = (Instance)self;
      foreach(object key in ui.__dict__.Keys) keys[key] = null;
    }
    return __attrs__(keys);
  }

  public override DynamicType GetDynamicType() { return ReflectedType.FromType(typeof(UserType)); } // TODO: cache somewhere?

  public override bool IsSubclassOf(object other)
  { foreach(object type in mro.items)
    { if(this==type) return true;
      ReflectedType rt = type as ReflectedType;
      if(rt!=null && (this.type==rt.Type || this.type.IsSubclassOf(rt.Type))) return true;
    }
    return false;
  }

  public override void SetAttr(Tuple mro, int index, object self, string name, object value)
  { if(self!=null) ((Instance)self).__dict__[name] = value;
    else
    { object slot = LookupSlot(mro, index, name);
      if(slot!=null) Ops.SetDescriptor(slot, null, value);
      else throw Ops.AttributeError("no such slot '{0}'", name);
    }
  }
  public override void SetAttr(object self, string name, object value) { SetAttr(mro, 0, self, name, value); }

  public override string ToString() { return string.Format("<class '{0}.{1}'>", __module__, __name__); }

  public override List __attrs__() { return __attrs__(new Dict()); }

  public object __module__;
  
  List __attrs__(Dict keys)
  { foreach(BoaType type in mro)
    { UserType ut = type as UserType;
      if(ut!=null) foreach(object key in ut.dict.Keys) keys[key] = null;
      else foreach(object key in type.GetAttrNames(this)) keys[key] = null;
    }

    List ret = keys.keys();
    ret.sort();
    return ret;
  }

  static object MergeMRO(List[] lists, ref bool done)
  { for(int li=0; li<lists.Length; li++)
    { if(lists[li].Count==0) continue;

      object bo = lists[li][0];
      for(int oi=li+1; oi<lists.Length; oi++)
      { if(lists[oi].Count>1 && lists[oi].__getitem__(-1)==bo) goto badhead;
      }

      done = true;
      for(int oi=0; oi<lists.Length; oi++)
      { List l = lists[oi];
        for(int i=0; i<l.Count; i++) if(l[i]==bo) { l.RemoveAt(i); break; }
        if(l.Count>0) done = false;
      }
      return bo;

      badhead:;
    }
    throw Ops.TypeError("MRO conflict");
  }

  Tuple bases;
}

} // namespace Boa.Runtime