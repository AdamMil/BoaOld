using System;
using System.Collections;
using System.Reflection;

namespace Boa.Runtime
{

#region Specialized attributes
public class SpecialAttr
{
#region DelegateCaller
public class DelegateCaller : IDescriptor
{ DelegateCaller() { }

  public object __get__(object instance)
  { Delegate d = (Delegate)instance;
    return d==null ? (object)this : new ReflectedMethod(new MethodBase[] { d.Method }, d.Target);
  }
  
  public static DelegateCaller Value = new DelegateCaller();
}
#endregion

#region NextMethod
// simulates next() method on IEnumerator objects
public class NextMethod : IDescriptor, ICallable
{ NextMethod() { }
  NextMethod(IEnumerator instance) { this.instance = instance; }

  public object __get__(object instance) { return instance==null ? this : new NextMethod((IEnumerator)instance); }
  
  public object Call(params object[] args)
  { if(instance.MoveNext()) return instance.Current;
    throw new StopIterationException();
  }

  public static NextMethod Value = new NextMethod();

  IEnumerator instance;
}
#endregion

#region StringReprMethod
public class StringReprMethod : IDescriptor, ICallable
{ public StringReprMethod() { }
  StringReprMethod(string instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new StringReprMethod((string)instance); }
  public object Call(params object[] args) { return StringOps.Quote(instance); }

  string instance;
}
#endregion
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

  public object Call(params object[] args)
  { Type[] types  = new Type[args.Length];
    Match[] res   = new Match[sigs.Length];
    int bestMatch = -1;

    for(int i=0; i<args.Length; i++) types[i] = args[i]==null ? null : args[i].GetType();

    for(int mi=0; mi<sigs.Length; mi++) // TODO: cache the binding results somehow?
    { ParameterInfo[] parms = sigs[mi].GetParameters();
      bool paramArray = parms.Length>0 && IsParamArray(parms[parms.Length-1]), alreadyPA=false;
      int lastRP = paramArray ? parms.Length-1 : parms.Length;
      if(args.Length<lastRP || !paramArray && args.Length!=parms.Length) continue;

      res[mi].Conv = Conversion.Identity;
      // check types of all parameters except the parameter array if there is one
      for(int i=0; i<lastRP; i++)
      { Conversion conv = Ops.ConvertTo(types[i], parms[i].ParameterType);
        if(conv==Conversion.None || conv<res[mi].Conv)
        { res[mi].Conv=conv;
          if(conv==Conversion.None) goto nextSig;
        }
      }

      if(paramArray) // TODO: allow tuples and possibly lists as well to be used here
      { if(args.Length==parms.Length) // check if the last argument is an array already
        { Conversion conv = Ops.ConvertTo(types[lastRP], parms[lastRP].ParameterType);
          if(conv==Conversion.Identity || conv==Conversion.Reference)
          { if(conv<res[mi].Conv) res[mi].Conv=conv;
            alreadyPA = true;
            goto done;
          }
        }

        // check that all remaining arguments can be converted to the member type of the parameter array
        Type type = parms[lastRP].ParameterType.GetElementType();
        for(int i=lastRP; i<args.Length; i++)
        { Conversion conv = Ops.ConvertTo(types[i], type);
          if(conv==Conversion.None || conv<res[mi].Conv)
          { res[mi].Conv=conv;
            if(conv==Conversion.None) goto nextSig;
          }
        }
      }

      done:
      res[mi] = new Match(res[mi].Conv, parms, lastRP, alreadyPA, paramArray);
      if(bestMatch==-1 || res[mi]>res[bestMatch]) bestMatch=mi;

      nextSig:;
    }

    if(bestMatch==-1)
      throw Ops.TypeError("unable to bind arguments to method '{0}' on {1}", __name__, sigs[0].DeclaringType.FullName);

    Match best = res[bestMatch];

    // check for ambiguous bindings
    if(sigs.Length>1 && best.Conv!=Conversion.Identity)
      for(int i=0; i<res.Length; i++)
        if(i!=bestMatch && res[i]==best)
          throw Ops.TypeError("ambiguous argument types (multiple functions matched method '{0}' on {1})",
                              __name__, sigs[0].DeclaringType.FullName);

    // do the actual conversion
    for(int i=0, end=best.APA ? args.Length : best.Last; i<end; i++)
      args[i] = Ops.ConvertTo(args[i], best.Parms[i].ParameterType);

    if(best.Last!=best.Parms.Length && !best.APA)
    { object[] narr = new object[best.Parms.Length];
      Array.Copy(args, 0, narr, 0, best.Last);

      Type type = best.Parms[best.Last].ParameterType.GetElementType();
      Array pa = Array.CreateInstance(type, args.Length-best.Last);
      for(int i=0; i<pa.Length; i++) pa.SetValue(Ops.ConvertTo(args[i+best.Last], type), i);
      args=narr; args[best.Last]=pa;
    }

    return sigs[bestMatch].IsConstructor ? ((ConstructorInfo)sigs[bestMatch]).Invoke(args)
                                         : sigs[bestMatch].Invoke(instance, args);
  }

  internal void Add(MethodBase sig)
  { MethodBase[] narr = new MethodBase[sigs.Length+1];
    sigs.CopyTo(narr, 0);
    narr[sigs.Length] = sig;
    sigs = narr;
  }

  struct Match
  { public Match(Conversion conv, ParameterInfo[] parms, int last, bool apa, bool pa)
    { Conv=conv; Parms=parms; Last=last; APA=apa; PA=pa; Exact=!pa || apa;
    }

    public static bool operator<(Match a, Match b) { return a.Conv<b.Conv || !a.Exact && b.Exact; }
    public static bool operator>(Match a, Match b) { return a.Conv>b.Conv || a.Exact && !b.Exact; }
    public static bool operator==(Match a, Match b) { return a.Conv==b.Conv && a.Exact==b.Exact; }
    public static bool operator!=(Match a, Match b) { return a.Conv!=b.Conv || a.Exact!=b.Exact; }
    
    public override bool Equals(object obj) { return  obj is Match ? this==(Match)obj : false; }
    public override int GetHashCode() { throw new NotImplementedException(); }

    public Conversion Conv;
    public ParameterInfo[] Parms;
    public int Last;
    public bool APA, Exact, PA;
  }

  static bool IsParamArray(ParameterInfo pi) { return pi.IsDefined(typeof(ParamArrayAttribute), false); }

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
  
  public override bool IsSubclassOf(object other)
  { ReflectedType rt = other as ReflectedType;
    return rt==null ? false : rt.type.IsAssignableFrom(type);
  }

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

    if(typeof(IEnumerator).IsAssignableFrom(type))
    { if(!dict.Contains("next")) dict["next"] = SpecialAttr.NextMethod.Value;
      if(!dict.Contains("reset")) dict["reset"] = dict["Reset"];
      if(!dict.Contains("value")) dict["value"] = dict["Current"];
    }
    else if(type.IsSubclassOf(typeof(Delegate)))
    { if(!dict.Contains("__call__")) dict["__call__"] = SpecialAttr.DelegateCaller.Value;
    }
    else if(type==typeof(string)) dict["__repr__"] = new SpecialAttr.StringReprMethod();
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