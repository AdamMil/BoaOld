using System;
using System.Collections;
using System.Collections.Specialized;
using Boa.AST;

namespace Boa.Runtime
{

// FIXME: make these wrappers expose the attributes of the underlying functions
// TODO: create a base class for these wrappers

// FIXME: this doesn't work properly for derived classes (or does it?)
#region FunctionWrapper
public class FunctionWrapper : IHasAttributes
{ public FunctionWrapper(IFancyCallable func) { this.func = func; }

  protected internal IFancyCallable func;

  public List __attrs__() { return Ops.GetAttrNames(func); }
  public object __getattr__(string key) { return key=="__call__" ? this : Ops.GetAttr(func, key); }
  public void __setattr__(string key, object value) { Ops.SetAttr(value, func, key); }
  public void __delattr__(string key) { Ops.DelAttr(func, key); }
}
#endregion

#region ClassWrapper
public class ClassWrapper : FunctionWrapper, IDescriptor, IFancyCallable
{ public ClassWrapper(IFancyCallable func, BoaType type) : base(func) { this.type=type; }

  public object __get__(object instance)
  { return instance==null ? this : new ClassWrapper(func, ((IInstance)instance).__class__);
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

  // FIXME: fix this
  public override string ToString()
  { return string.Format("<classmethod '{0}' on '{1}'>", /*func.Name*/"", type.__name__);
  }

  BoaType type;
}
#endregion

#region MethodWrapper
public class MethodWrapper : FunctionWrapper, IDescriptor, IFancyCallable
{ public MethodWrapper(IFancyCallable func) : base(func) { }
  public MethodWrapper(IFancyCallable func, object instance) : base(func) { this.instance=instance; }

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

  // FIXME: fix this
  public override string ToString()
  { return "<method>";
    //return string.Format("<method '{0}'>", func.Name);
  }

  object instance;
}
#endregion

#region UserType
public class UserType : BoaType
{ public UserType(string module, string name, Tuple bases, IDictionary dict)
    : base(TypeMaker.MakeType(module, name, bases.items, dict))
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
  { object dummy;

    // TODO: implement __new__
    //if(!Ops.TryInvoke(this, "__new__", out obj, nargs)) obj = cons.Call(args);

    // TODO: choose a more appropriate constructor if we've derived from .NET classes that have constructors w/ params
    IInstance obj = (IInstance)type.GetConstructor(Type.EmptyTypes).Invoke(null);
    obj.__class__ = this;

    if(obj!=null) Ops.TryInvoke(obj, "__init__", out dummy, args);
    return obj;
  }

  public override void DelAttr(Tuple mro, int index, object self, string name)
  { IInstance ui = (IInstance)self;
    if(ui!=null)
    { int count = ui.__dict__.Count; // i assume it's more efficient to check .Count twice than to call Contains()
      ui.__dict__.Remove(name);      // (to see if the item was actually removed)
      if(ui.__dict__.Count!=count) return;
    }

    object slot = LookupSlot(mro, index, name);
    if(slot!=Ops.Missing) Ops.DelDescriptor(slot, self);
    else throw Ops.AttributeError("no such slot '{0}'", name);
  }
  public override void DelAttr(object self, string name) { DelAttr(mro, 0, self, name); }

  public override object GetAttr(Tuple mro, int index, object self, string name)
  { object value;
    if(!GetAttr(mro, index, self, name, out value)) throw Ops.AttributeError("no such slot '{0}'", name);
    return value;
  }
  
  public bool GetAttr(Tuple mro, int index, object self, string name, out object value)
  { object slot = LookupSlot(mro, index, name);

    if(slot!=Ops.Missing)
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
  { IInstance ui = (IInstance)self;

    if(self!=null)
    { if(name=="__dict__")
      { value = ui.__dict__;
        return true;
      }
      if(name=="__class__")
      { value = ui.__class__;
        return true;
      }

      object obj = ui.__dict__[name];
      if(obj!=null || ui.__dict__.Contains(name))
      { value = Ops.GetDescriptor(obj, self);
        return true;
      }
    }

    return GetAttr(mro, 0, self, name, out value);
  }

  public override List GetAttrNames(object self)
  { Dict keys = new Dict();
    if(self!=null)
    { IInstance ui = (IInstance)self;
      foreach(object key in ui.__dict__.Keys) keys[key] = null;
    }
    return __attrs__(keys);
  }

  public override DynamicType GetDynamicType() { return ReflectedType.FromType(typeof(UserType)); } // TODO: cache somewhere?

  public override bool IsSubclassOf(object other)
  { ReflectedType ort = other as ReflectedType;
    for(int i=0; i<mro.items.Length; i++) if(mro.items[i]==other) return true;
    if(ort!=null)
      for(int i=0; i<mro.items.Length; i++)
      { ReflectedType rt = mro.items[i] as ReflectedType;
        if(rt!=null && (rt==ort || rt.IsSubclassOf(ort))) return true;
      }
    return false;
  }

  public override void SetAttr(Tuple mro, int index, object self, string name, object value)
  { object slot = LookupSlot(mro, index, name);
    if(slot!=Ops.Missing && (self==null || slot is ReflectedMember) && Ops.SetDescriptor(slot, null, value)) return;
    else if(self!=null) ((IInstance)self).__dict__[name] = value;
    else throw Ops.AttributeError("no such slot '{0}'", name);
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
#endregion

} // namespace Boa.Runtime