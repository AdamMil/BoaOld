using System;
using System.Collections;
using System.Reflection;

namespace Boa.Runtime
{

#region NextMethod
// simulates next() method on IEnumerator objects
public class NextMethod : IDescriptor, ICallable
{ NextMethod() { }
  NextMethod(object instance) { this.instance = instance; }
  static NextMethod()
  { current = (ReflectedProperty)ReflectedType.FromType(typeof(IEnumerator)).LookupSlot("Current");
    Value = new NextMethod();
  }

  public object __get__(object instance)
  { return instance==null ? this : new NextMethod(instance);
  }
  
  public object Call(params object[] args)
  { ReflectedMethod moveNext = (ReflectedMethod)Ops.GetAttr(instance, "MoveNext");
    if(Ops.IsTrue(moveNext.Call())) return current.__get__(instance);
    throw new StopIterationException();
  }

  object instance;

  public static readonly NextMethod Value;
  static ReflectedProperty current;
}
#endregion

#region ReflectedConstructor
public class ReflectedConstructor : ReflectedMethodBase
{ public ReflectedConstructor(ConstructorInfo ci) : base(ci) { }
  public override string ToString() { return string.Format("<constructor for {0}>", sigs[0].DeclaringType.FullName); }
}
#endregion

#region ReflectedEvent
public class ReflectedEvent : IDescriptor
{ public ReflectedEvent(EventInfo ei) { info=ei; }

	// TODO: automatically create new delegate types for different event signatures
	public class BoaEventHandler
	{ public BoaEventHandler(object func) { this.func = func; }

		public void Handle() { Ops.Call(func); }
		public void Handle(object sender, EventArgs e) { Ops.Call(func, sender, e); }
		
		object func;
	}

  public object __iadd__(object instance, object func)
  { Delegate handler = func as Delegate;
    if(handler==null) handler = Delegate.CreateDelegate(info.EventHandlerType, new BoaEventHandler(func), "Handle");
    info.AddEventHandler(instance, handler);
    return null;
  }

  public object __isub__(object instance, object func)
  { Delegate handler = func as Delegate;
    if(handler==null) throw new NotImplementedException("removing non-delegate event handlers");
    info.RemoveEventHandler(instance, handler);
    return null;
  }

  public object __get__(object instance) { return this; }

  public override string ToString()
  { return string.Format("<event {0} on {1}>", info.Name, info.DeclaringType.FullName);
  }

  EventInfo info;
}
#endregion

#region ReflectedField
public class ReflectedField : IDataDescriptor
{ public ReflectedField(FieldInfo fi) { info=fi; }

  public object __get__(object instance)
  { return instance!=null || info.IsStatic ? Ops.ToBoa(info.GetValue(instance)) : this;
  }

  public void __set__(object instance, object value)
  { if(info.IsInitOnly || info.IsLiteral) throw Ops.TypeError("{0} is a read-only attribute", info.Name);
    if(instance==null && !info.IsStatic) throw Ops.TypeError("{0} is an instance field", info.Name);
    info.SetValue(instance, Ops.ConvertTo(value, info.FieldType));
  }

  public void __delete__(object instance) { throw Ops.TypeError("can't delete field on built-in object"); }

  public override string ToString()
  { return string.Format("<field {0} on {1}>", info.Name, info.DeclaringType.FullName);
  }

  FieldInfo info;
}
#endregion

#region ReflectedMethod
public class ReflectedMethod : ReflectedMethodBase, IDescriptor
{ public ReflectedMethod(MethodInfo mi) : base(mi) { }
  public ReflectedMethod(MethodBase[] sigs, object instance) : base(sigs, instance) { }

  public object __get__(object instance) { return instance==null ? this : new ReflectedMethod(sigs, instance); }

  public override string ToString()
  { return string.Format("<method {0} on {1}>", __name__, sigs[0].DeclaringType.FullName);
  }
}
#endregion

#region ReflectedMethodBase
public abstract class ReflectedMethodBase : ICallable
{ protected ReflectedMethodBase(MethodBase mb) : this(new MethodBase[] { mb }) { }
  protected ReflectedMethodBase(MethodBase[] sigs) { this.sigs=sigs; }
  protected ReflectedMethodBase(MethodBase[] sigs, object instance) { this.sigs=sigs; this.instance=instance; }

  public string __name__ { get { return sigs[0].Name; } }

  public object Call(params object[] args) // TODO: do better binding than this
  { foreach(MethodBase sig in sigs)
    { ParameterInfo[] parms = sig.GetParameters();
      if(parms.Length!=args.Length) continue;
      for(int i=0; i<parms.Length; i++) args[i] = Ops.ConvertTo(args[i], parms[i].ParameterType);
      return sig.IsConstructor ? ((ConstructorInfo)sig).Invoke(args) : sig.Invoke(instance, args);
    }
    throw Ops.TypeError("unable to bind arguments to method {0} on {1}", __name__, sigs[0].DeclaringType.FullName);
  }

  internal void Add(MethodBase sig)
  { MethodBase[] narr = new MethodBase[sigs.Length+1];
    sigs.CopyTo(narr, 0);
    narr[sigs.Length] = sig;
    sigs = narr;
  }

  protected MethodBase[] sigs;
  protected object instance;
}
#endregion

// TODO: either extend this to handle property parameters, or handle them some other way
#region ReflectedProperty
public class ReflectedProperty : IDataDescriptor
{ public ReflectedProperty(PropertyInfo info) { this.info=info; }

  public object __get__(object instance)
  { if(!info.CanRead) throw Ops.TypeError("{0} is a non-readable attribute", info.Name);
    MethodInfo mi = info.GetGetMethod();
    return instance!=null || mi.IsStatic ? mi.Invoke(instance, Misc.EmptyArray) : this;
  }

  public void __set__(object instance, object value)
  { if(!info.CanWrite) throw Ops.TypeError("non-writeable attribute");
    MethodInfo mi = info.GetSetMethod();
    if(instance==null && !mi.IsStatic) throw Ops.TypeError("{0} is an instance property", info.Name);
    mi.Invoke(instance, new object[] { Ops.ConvertTo(value, info.PropertyType) });
  }

  public void __delete__(object instance) { throw Ops.TypeError("can't delete property on built-in object"); }

  public override string ToString()
  { return string.Format("<property {0} on {1}>", info.Name, info.DeclaringType.FullName);
  }

  PropertyInfo info;
}
#endregion

#region ReflectedType
public class ReflectedType : BoaType
{ ReflectedType(Type type) : base(type) { }

  public override object Call(params object[] args)
  { Initialize();
    return cons.Call(args);
  }

  public override void DelAttr(object self, string name)
  { object slot = RawGetSlot(name);
    if(slot==null) throw Ops.AttributeError("no such slot '{0}'", name);
    if(!Ops.DelDescriptor(slot, this)) dict.Remove(name);
  }

  public override bool GetAttr(object self, string name, out object value)
  { object slot = RawGetSlot(name);
    if(slot!=null)
    { value = Ops.GetDescriptor(slot, self);
      return true;
    }
    value = null;
    return false;
  }

  public override DynamicType GetDynamicType() { return MyDynamicType; }

  public override void SetAttr(object self, string name, object value)
  { object slot = RawGetSlot(name);
    if(slot==null) throw Ops.AttributeError("no such slot '{0}'", name);
    Ops.SetDescriptor(slot, self, value);
  }

  public override string ToString() { return string.Format("<type {0}>", Ops.Repr(__name__)); }

  public static ReflectedType FromType(Type type)
  { ReflectedType rt = (ReflectedType)types[type];
    if(rt==null) types[type] = rt = new ReflectedType(type);
    return rt;
  }

  protected override void Initialize()
  { if(initialized) return;
    base.Initialize();
    foreach(ConstructorInfo ci in type.GetConstructors()) AddConstructor(ci);
    foreach(EventInfo ei in type.GetEvents()) AddEvent(ei);
    foreach(FieldInfo fi in type.GetFields()) AddField(fi);
    foreach(MethodInfo mi in type.GetMethods()) AddMethod(mi);
    foreach(PropertyInfo pi in type.GetProperties()) AddProperty(pi);

    if(!dict.Contains("next") && typeof(IEnumerator).IsAssignableFrom(type)) dict["next"] = NextMethod.Value;
  }

  void AddConstructor(ConstructorInfo ci)
  { if(!ci.IsPublic) return;
    if(cons==null) cons = new ReflectedConstructor(ci);
    else cons.Add(ci);
  }

  void AddEvent(EventInfo ei) { dict[ei.Name] = new ReflectedEvent(ei); }
  void AddField(FieldInfo fi) { dict[fi.Name] = new ReflectedField(fi); }

  void AddMethod(MethodInfo mi)
  { ReflectedMethod rm = (ReflectedMethod)dict[mi.Name];
    if(rm==null) dict[mi.Name] = new ReflectedMethod(mi);
    else rm.Add(mi);
  }

  void AddProperty(PropertyInfo pi) { dict[pi.Name] = new ReflectedProperty(pi); }

  ReflectedConstructor cons;

  static Hashtable types = new Hashtable(); // assumes these fields are initialized in order
  static readonly DynamicType MyDynamicType = ReflectedType.FromType(typeof(ReflectedType));
}
#endregion

} // namespace Boa.Runtime