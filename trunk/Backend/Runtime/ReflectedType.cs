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
using System.Reflection;
using System.Reflection.Emit;

namespace Boa.Runtime
{

#region Specialized attributes
public class SpecialAttr
{
#region ArrayGetItem
public class ArrayGetItem : IDescriptor, ICallable
{ ArrayGetItem(Array instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new ArrayGetItem((Array)instance); }

  public object Call(params object[] args)
  { if(args.Length==0) return Ops.TooFewArgs("System.Array.__getitem__()", 1, 0);
    if(instance==null) throw Ops.MethodCalledWithoutInstance("System.Array.__getitem__()");
    int length = instance.Length;
    switch(args.Length)
    { case 1: return instance.GetValue(Ops.FixIndex(Ops.ToInt(args[0]), length));
      case 2: return instance.GetValue(Ops.FixIndex(Ops.ToInt(args[0]), length),
                                       Ops.FixIndex(Ops.ToInt(args[1]), length));
      case 3: return instance.GetValue(Ops.FixIndex(Ops.ToInt(args[0]), length),
                                       Ops.FixIndex(Ops.ToInt(args[1]), length),
                                       Ops.FixIndex(Ops.ToInt(args[2]), length));
      default:
        int[] indices = new int[args.Length];
        for(int i=0; i<args.Length; i++) indices[i] = Ops.FixIndex(Ops.ToInt(args[i]), length);
        return instance.GetValue(indices);
    }
  }

  public override string ToString() { return "<method '__getitem__' on 'System.Array'>"; }
  
  public static readonly ArrayGetItem Value = new ArrayGetItem(null);

  Array instance;
}
#endregion

#region ArraySetItem
public class ArraySetItem : IDescriptor, ICallable
{ ArraySetItem(Array instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new ArraySetItem((Array)instance); }

  public object Call(params object[] args)
  { if(args.Length<2) return Ops.TooFewArgs("System.Array.__setitem__()", 2, args.Length);
    if(instance==null) throw Ops.MethodCalledWithoutInstance("System.Array.__setitem__()");
    instance.SetValue(Ops.ConvertTo(args[1], instance.GetType().GetElementType()),
                      Ops.FixIndex(Ops.ToInt(args[0]), instance.Length));
    return null;
  }

  public override string ToString() { return "<method '__setitem__' on 'System.Array'>"; }

  public static readonly ArraySetItem Value = new ArraySetItem(null);

  Array instance;
}
#endregion

#region DelegateCaller
public class DelegateCaller : IDescriptor
{ DelegateCaller() { }

  public object __get__(object instance)
  { Delegate d = (Delegate)instance;
    return d==null ? (object)this : new ReflectedMethod(new MethodBase[] { d.Method }, d.Target, null);
  }

  public override string ToString() { return "<method '__call__' on 'System.Delegate'>"; }

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
  { if(instance==null) throw Ops.MethodCalledWithoutInstance("IEnumerator.next()");
    if(instance.MoveNext()) return instance.Current;
    throw new StopIterationException();
  }

  public override string ToString() { return "<method 'next' on 'System.Collections.IEnumerator'>"; }

  public static NextMethod Value = new NextMethod();

  IEnumerator instance;
}
#endregion

#region StringContains
public class StringContains : IDescriptor, ICallable
{ public StringContains() { }
  StringContains(string instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new StringContains((string)instance); }

  public object Call(params object[] args)
  { if(args.Length!=1) throw Ops.WrongNumArgs("__getitem__", args.Length, 1);
    if(instance==null) throw Ops.MethodCalledWithoutInstance("string.__contains__()");
    string s = args[0] as string;
    if(s!=null) return Ops.FromBool((s.Length==1 ? instance.IndexOf(s[0]) : instance.IndexOf(s)) != -1);
    if(args[0] is char) return Ops.FromBool(instance.IndexOf((char)args[0])!=-1);
    throw Ops.TypeError("string.__contains__() expected a string or character argument, but got "+
                        Ops.TypeName(args[0]));
  }

  public override string ToString() { return "<method '__contains__' on 'System.String'>"; }

  string instance;
}
#endregion

#region StringGetItem
public class StringGetItem : IDescriptor, ICallable
{ public StringGetItem() { }
  StringGetItem(string instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new StringGetItem((string)instance); }

  public object Call(params object[] args)
  { if(args.Length!=1) throw Ops.WrongNumArgs("__getitem__", args.Length, 1);
    if(instance==null) throw Ops.MethodCalledWithoutInstance("string.__getitem__()");
    Slice slice = args[0] as Slice;
    return slice==null ? new string(instance[Ops.FixIndex(Ops.ToInt(args[0]), instance.Length)], 1)
                       : StringOps.Slice(instance, slice);
  }

  public override string ToString() { return "<method '__getitem__' on 'System.String'>"; }

  string instance;
}
#endregion

#region StringJoin
public class StringJoin : IDescriptor, ICallable
{ public StringJoin() { }
  StringJoin(string instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new StringJoin((string)instance); }

  public object Call(params object[] args)
  { if(args.Length!=1) throw Ops.WrongNumArgs("__getitem__", args.Length, 1);
    if(instance==null) throw Ops.MethodCalledWithoutInstance("string.join()");
    return Boa.Modules._string.join(args[0], instance);
  }

  public override string ToString() { return "<method 'join' on 'System.String'>"; }

  string instance;
}
#endregion

#region StringRepr
public class StringRepr : IDescriptor, ICallable
{ public StringRepr() { }
  StringRepr(string instance) { this.instance=instance; }

  public object __get__(object instance) { return instance==null ? this : new StringRepr((string)instance); }
  public object Call(params object[] args)
  { if(args.Length!=0) throw Ops.WrongNumArgs("__repr__", args.Length, 0);
    if(instance==null) throw Ops.MethodCalledWithoutInstance("string.__repr__()");
    return StringOps.Escape(instance);
  }

  public override string ToString() { return "<method '__repr__' on 'System.String'>"; }

  string instance;
}
#endregion

}
#endregion

#region DelegateProxy
public abstract class DelegateProxy
{ protected DelegateProxy(object callable) { this.callable = callable; }
	protected object callable;

	public static object Make(object callable, Type delegateType)
	{ ConstructorInfo ci;
	  lock(handlers) ci = (ConstructorInfo)handlers[delegateType];
	  if(ci==null)
	  { Type[] ctypes = { typeof(object) };

	    MethodInfo mi = delegateType.GetMethod("Invoke", BindingFlags.Public|BindingFlags.Instance);
		  if(mi==null) throw new ArgumentException("This doesn't seem to be a delegate.", "delegateType");

		  ParameterInfo[] pis = mi.GetParameters();
		  Type[] ptypes = new Type[pis.Length];
		  for(int i=0; i<pis.Length; i++) ptypes[i] = pis[i].ParameterType;

		  Key key = new Key(mi.ReturnType, ptypes);
      lock(sigs) ci = (ConstructorInfo)sigs[key];

		  if(ci==null)
		  { AST.TypeGenerator tg = AST.SnippetMaker.Assembly.DefineType(TypeAttributes.Public|TypeAttributes.Sealed,
		                                                                "EventHandler"+Misc.NextIndex,
		                                                                typeof(DelegateProxy));
		    ConstructorInfo pci =
		      typeof(DelegateProxy).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic, null, ctypes, null);
		    AST.CodeGenerator cg = tg.DefineChainedConstructor(pci);
		    cg.EmitReturn();
		    cg.Finish();

		    cg = tg.DefineMethod(MethodAttributes.Public, "Handle", mi.ReturnType, ptypes);
        cg.EmitThis();
        cg.EmitFieldGet(typeof(DelegateProxy).GetField("callable", BindingFlags.Instance|BindingFlags.NonPublic));
        if(pis.Length==0) cg.EmitCall(typeof(Ops), "Call0");
		    else
		    { cg.EmitNewArray(typeof(object), pis.Length);
          for(int i=0; i<pis.Length; i++)
          { cg.ILG.Emit(OpCodes.Dup);
            cg.EmitInt(i);
            cg.EmitArgGet(i);
            cg.ILG.Emit(OpCodes.Stelem_Ref);
          }
          cg.EmitCall(typeof(Ops), "Call", new Type[] { typeof(object), typeof(Type[]) });
		    }
		    if(mi.ReturnType==typeof(void)) cg.ILG.Emit(OpCodes.Pop);
		    else if(mi.ReturnType!=typeof(object))
		    { cg.EmitTypeOf(mi.ReturnType);
		      cg.EmitCall(typeof(Ops), "ConvertTo", new Type[] { typeof(object), typeof(Type) });
		    }
		    cg.EmitReturn();
		    cg.Finish();
		    
		    ci = tg.FinishType().GetConstructor(ctypes);
		    lock(sigs) sigs[key] = ci;
		  }

		  lock(handlers) handlers[delegateType] = ci;
	  }

	  return ci.Invoke(new object[] { callable });
	}

  struct Key
  { public Key(Type returnType, Type[] paramTypes) { ReturnType=returnType; ParamTypes=paramTypes; }

    public override bool Equals(object obj)
    { if(!(obj is Key)) return false;
      Key other = (Key)obj;
      if(ReturnType!=other.ReturnType || ParamTypes.Length!=other.ParamTypes.Length) return false;
      for(int i=0; i<ParamTypes.Length; i++) if(ParamTypes[i]!=other.ParamTypes[i]) return false;
      return true;
    }

    public override int GetHashCode()
    { int hash = ReturnType.GetHashCode();
      for(int i=0; i<ParamTypes.Length; i++) hash ^= ParamTypes[i].GetHashCode();
      return hash;
    }

    Type ReturnType;
    Type[] ParamTypes;
  }

	static Hashtable handlers = new Hashtable();
	static Hashtable sigs = new Hashtable();
}
#endregion

#region DocStringAttribute
public class DocStringAttribute : Attribute
{ public DocStringAttribute(string docs) { Docs=docs.Replace("\r\n", "\n"); }
  public string Docs;
}
#endregion

#region ReflectedMember
public class ReflectedMember
{ public ReflectedMember() { }
  public ReflectedMember(string docs) { __doc__=docs; }
  public ReflectedMember(MemberInfo mi)
  { object[] docs = mi.GetCustomAttributes(typeof(DocStringAttribute), false);
    if(docs.Length!=0) __doc__ = ((DocStringAttribute)docs[0]).Docs;
  }

  public string __doc__;
}
#endregion

// TODO: allow extra keyword parameters to set properties
#region ReflectedConstructor
public sealed class ReflectedConstructor : ReflectedMethodBase
{ public ReflectedConstructor(MethodBase ci) : base(ci) { }
  public override string ToString()
  { return string.Format("<constructor for '{0}'>", sigs[0].DeclaringType.FullName);
  }
}
#endregion

#region ReflectedEvent
public sealed class ReflectedEvent : ReflectedMember, IDescriptor
{ public ReflectedEvent(EventInfo ei) : base(ei) { info=ei; }
  public ReflectedEvent(EventInfo ei, object instance) : base(ei) { info=ei; this.instance=instance; }

  // TODO: use __iadd__ and __isub__ when we support that
  public void add(object func)
  { Delegate handler = func as Delegate;
    if(handler==null) handler = Ops.MakeDelegate(func, info.EventHandlerType);
    info.AddEventHandler(instance, handler);
  }

  public void sub(object func)
  { Delegate handler = func as Delegate;
    if(handler==null) throw new NotImplementedException("removing non-delegate event handlers");
    info.RemoveEventHandler(instance, handler);
  }

  public object __get__(object instance) { return instance==null ? this : new ReflectedEvent(info, instance); }

  public override string ToString()
  { return string.Format("<event '{0}' on '{1}'>", info.Name, info.DeclaringType.FullName);
  }

  internal EventInfo info;
  object instance;
}
#endregion

#region ReflectedField
public sealed class ReflectedField : ReflectedMember, IDataDescriptor
{ public ReflectedField(FieldInfo fi) : base(fi) { info=fi; }

  public object __get__(object instance)
  { return instance!=null || info.IsStatic ? info.GetValue(instance) : this;
  }

  public void __set__(object instance, object value)
  { if(info.IsInitOnly || info.IsLiteral) throw Ops.TypeError("{0} is a read-only attribute", info.Name);
    if(instance==null && !info.IsStatic) throw Ops.TypeError("{0} is an instance field", info.Name);
    info.SetValue(instance, Ops.ConvertTo(value, info.FieldType));
  }

  public void __delete__(object instance) { throw Ops.TypeError("can't delete field on built-in object"); }

  public override string ToString()
  { return string.Format("<field '{0}' on '{1}'>", info.Name, info.DeclaringType.FullName);
  }

  internal FieldInfo info;
}
#endregion

#region ReflectedMethod
public sealed class ReflectedMethod : ReflectedMethodBase, IDescriptor
{ public ReflectedMethod(MethodInfo mi) : base(mi) { }
  public ReflectedMethod(MethodBase[] sigs, object instance, string docs) : base(sigs, instance, docs) { }

  public object __get__(object instance)
  { return instance==null ? this : new ReflectedMethod(sigs, instance, __doc__);
  }

  public override string ToString()
  { return string.Format("<method '{0}' on '{1}'>", __name__, sigs[0].DeclaringType.FullName);
  }
}
#endregion

// TODO: respect default parameters
#region ReflectedMethodBase
public abstract class ReflectedMethodBase : ReflectedMember, IFancyCallable
{ protected ReflectedMethodBase(MethodBase mb) : base(mb)
  { sigs = new MethodBase[] { mb };
    hasStatic = allStatic = mb.IsStatic;
  }
  protected ReflectedMethodBase(MethodBase[] sigs, object instance, string docs) : base(docs)
  { this.sigs=sigs; this.instance=instance;
  }

  public string __name__ { get { return sigs[0].Name; } }

  public object __call__(params object[] args) { return Call(args); }

  public object Call(params object[] args)
  { Type[] types  = new Type[args.Length];
    Match[] res   = new Match[sigs.Length];
    int bestMatch = -1;

    for(int i=0; i<args.Length; i++) types[i] = args[i]==null ? null : args[i].GetType();

    for(int mi=0; mi<sigs.Length; mi++) // TODO: speed the binding up somehow?
    { if(instance==null && !sigs[mi].IsStatic && !sigs[mi].IsConstructor) continue;
      ParameterInfo[] parms = sigs[mi].GetParameters();
      bool paramArray = parms.Length>0 && IsParamArray(parms[parms.Length-1]);
      int lastRP      = paramArray ? parms.Length-1 : parms.Length;
      if(args.Length<lastRP || !paramArray && args.Length!=parms.Length) continue;

      TryMatch(parms, args, types, lastRP, paramArray, res, mi, ref bestMatch);
    }

    return DoCall(args, types, res, bestMatch);
  }

  public object Call(object[] positional, string[] names, object[] values)
  { Match[] res   = new Match[sigs.Length];
    int bestMatch = -1, numpos = positional==null ? 0 : positional.Length, numargs = numpos+values.Length;

    Type[]  types = new Type[numargs], rtypes = new Type[numargs], btypes=null;
    object[] args = new object[numargs], bargs=null;
    int[]    done = new int[numargs];

    for(int i=0; i<numpos; i++) types[i] = positional[i]==null ? null : positional[i].GetType();
    for(int i=0; i<values.Length; i++) types[i+numpos] = values[i]==null ? null : values[i].GetType();

    for(int mi=0; mi<sigs.Length; mi++) // TODO: cache the binding results somehow?
    { if(instance==null && !sigs[mi].IsStatic) continue;
      ParameterInfo[] parms = sigs[mi].GetParameters();
      if(names.Length>parms.Length) continue;
      for(int i=0; i<done.Length; i++) done[i] = -1;
      for(int i=0; i<names.Length; i++) // check named args
      { for(int j=0; j<parms.Length; j++)
          if(names[i]==parms[j].Name)
          { if(done[j]!=-1) throw Ops.TypeError("duplicate value for parameter '{0}'", names[i]);
            done[j] = i+numpos;
            args[j] = values[i];
            goto nextName;
          }
        goto nextSig;
        nextName:;
      }

      bool paramArray = parms.Length>0 && IsParamArray(parms[parms.Length-1]);
      int lastRP      = paramArray ? parms.Length-1 : parms.Length;
      for(int i=0, end=Math.Min(numpos, lastRP); i<end; i++)
      { if(done[i]!=-1) throw Ops.TypeError("duplicate value for parameter '{0}'", parms[i].Name);
        done[i] = i;
        args[i] = positional[i];
      }
      for(int i=0; i<done.Length; i++)
        if(done[i]==-1) goto nextSig;
        else rtypes[i] = types[done[i]];

      if(TryMatch(parms, args, rtypes, lastRP, paramArray, res, mi, ref bestMatch))
      { bargs  = (object[])args.Clone();
        btypes = (Type[])rtypes.Clone();
      }

      nextSig:;
    }
    
    return DoCall(bargs, btypes, res, bestMatch);
  }

  internal void Add(MethodBase sig)
  { if(__doc__==null)
    { object[] docs = sig.GetCustomAttributes(typeof(DocStringAttribute), false);
      if(docs.Length!=0) __doc__ = ((DocStringAttribute)docs[0]).Docs;
    }

    if(sig.IsStatic) hasStatic=true;
    else allStatic=false;

    MethodBase[] narr = new MethodBase[sigs.Length+1];
    sigs.CopyTo(narr, 0);
    narr[sigs.Length] = sig;
    sigs = narr;
  }

  struct Match
  { public Match(Conversion conv, ParameterInfo[] parms, int last, byte apa, bool pa)
    { Conv=conv; Parms=parms; Last=last; APA=apa; PA=pa;
    }

    public static bool operator<(Match a, Match b) { return a.Conv<b.Conv || a.PA && (!b.PA || a.APA<b.APA); }
    public static bool operator>(Match a, Match b) { return a.Conv>b.Conv || b.PA && (!a.PA || a.APA>b.APA); }
    public static bool operator==(Match a, Match b) { return a.Conv==b.Conv && a.PA==b.PA && a.APA==b.APA; }
    public static bool operator!=(Match a, Match b) { return a.Conv!=b.Conv || a.PA!=b.PA || a.APA!=b.APA; }
    
    public override bool Equals(object obj) { return  obj is Match ? this==(Match)obj : false; }
    public override int GetHashCode() { throw new NotSupportedException(); }

    public Conversion Conv;
    public ParameterInfo[] Parms;
    public int Last;
    public byte APA;
    public bool PA;
  }

  object DoCall(object[] args, Type[] types, Match[] res, int bestMatch)
  { if(bestMatch==-1) throw Ops.TypeError("unable to bind arguments to method '{0}' on {1}",
                                          __name__, sigs[0].DeclaringType.FullName);
    Match best = res[bestMatch];

    // check for ambiguous bindings
    if(sigs.Length>1 && best.Conv!=Conversion.Identity)
      for(int i=bestMatch+1; i<res.Length; i++)
        if(res[i]==best)
          throw Ops.TypeError("ambiguous argument types (multiple functions matched method '{0}' on {1})",
                              __name__, sigs[0].DeclaringType.FullName);

    // do the actual conversion
    for(int i=0, end=best.APA==2 ? args.Length : best.Last; i<end; i++)
      args[i] = Ops.ConvertTo(args[i], best.Parms[i].ParameterType);

    if(best.Last!=best.Parms.Length && best.APA==0)
    { object[] narr = new object[best.Parms.Length];
      Array.Copy(args, 0, narr, 0, best.Last);

      Type type = best.Parms[best.Last].ParameterType.GetElementType();
      Array pa = Array.CreateInstance(type, args.Length-best.Last);
      for(int i=0; i<pa.Length; i++) pa.SetValue(Ops.ConvertTo(args[i+best.Last], type), i);
      args=narr; args[best.Last]=pa;
    }
    else if(best.APA==1)
    { Type type = best.Parms[best.Last].ParameterType.GetElementType();
      object[] items = ((Tuple)args[best.Last]).items;
      if(type==typeof(object)) args[best.Last] = items;
      else
      { Array pa = Array.CreateInstance(type, items.Length);
        for(int i=0; i<items.Length; i++) pa.SetValue(Ops.ConvertTo(items[i], type), i);
        args[best.Last] = pa;
      }
    }

    try
    { return sigs[bestMatch].IsConstructor ? ((ConstructorInfo)sigs[bestMatch]).Invoke(args)
                                           : sigs[bestMatch].Invoke(instance, args);
    }
    catch(TargetInvocationException e) { throw e.InnerException; }
  }

  bool TryMatch(ParameterInfo[] parms, object[] args, Type[] types, int lastRP, bool paramArray,
                Match[] res, int mi, ref int bestMatch)
  { byte alreadyPA = 0;
    res[mi].Conv = Conversion.Identity;

    // check types of all parameters except the parameter array if there is one
    for(int i=0; i<lastRP; i++)
    { Conversion conv = Ops.ConvertTo(types[i], parms[i].ParameterType);
      if(conv==Conversion.None || conv<res[mi].Conv)
      { res[mi].Conv=conv;
        if(conv==Conversion.None) return false;
      }
    }

    if(paramArray)
    { if(args.Length==parms.Length) // check if the last argument is an array already
      { Conversion conv = Ops.ConvertTo(types[lastRP], parms[lastRP].ParameterType);
        if(conv==Conversion.Identity || conv==Conversion.Reference)
        { if(conv<res[mi].Conv) res[mi].Conv=conv;
          alreadyPA = 2;
          goto done;
        }
      }

      // check that all remaining arguments can be converted to the member type of the parameter array
      Type type = parms[lastRP].ParameterType.GetElementType();
      if(args.Length==parms.Length && types[lastRP]==typeof(Tuple))
      { if(type!=typeof(object))
        { object[] items = ((Tuple)args[lastRP]).items;
          for(int i=0; i<items.Length; i++)
          { Conversion conv = Ops.ConvertTo(items[i].GetType(), type);
            if(conv==Conversion.None) goto notCPA;
          }
        }
        alreadyPA = 1;
        goto done;
      }

      notCPA:
      for(int i=lastRP; i<args.Length; i++)
      { Conversion conv = Ops.ConvertTo(types[i], type);
        if(conv==Conversion.None || conv<res[mi].Conv)
        { res[mi].Conv=conv;
          if(conv==Conversion.None) return false;
        }
      }
    }

    done:
    res[mi] = new Match(res[mi].Conv, parms, lastRP, alreadyPA, paramArray);
    if(bestMatch==-1 || res[mi]>res[bestMatch]) { bestMatch=mi; return true; }
    return false;
  }

  static bool IsParamArray(ParameterInfo pi) { return pi.IsDefined(typeof(ParamArrayAttribute), false); }

  internal MethodBase[] sigs;
  internal bool hasStatic, allStatic;

  protected object instance;
}
#endregion

#region ReflectedProperty
public sealed class ReflectedProperty : ReflectedMember, IDataDescriptor
{ public ReflectedProperty(PropertyInfo info) : base(info) { state=new State(info); Add(info); }
  ReflectedProperty(State state, object instance, string docs) : base(docs)
  { this.state=state; this.instance=instance;
  }

  public void __delete__(object instance) { throw Ops.TypeError("can't delete properties on built-in objects"); }

  public object __get__(object instance)
  { if(state.index) return instance==null ? this : new ReflectedProperty(state, instance, __doc__);
    if(!state.canRead) throw Ops.TypeError("{0} is a non-readable attribute", state.info.Name);
    MethodInfo mi = state.info.GetGetMethod();
    return instance!=null || mi.IsStatic ? mi.Invoke(instance, Misc.EmptyArray) : this;
  }

  public object __getitem__(params object[] args)
  { if(!state.canRead) throw Ops.TypeError("{0} is a non-readable attribute", state.info.Name);
    return ((ReflectedMethod)state.get.__get__(instance)).Call(args);
  }

  public void __set__(object instance, object value) { setitem(instance, value); }
  public void __setitem__(params object[] args) { setitem(instance, args); }

  public void Add(PropertyInfo info)
  { if(info.GetIndexParameters().Length>0) state.index = true;

    if(info.CanRead)
    { if(state.get==null) state.get = new ReflectedMethod(info.GetGetMethod());
      else state.get.Add(info.GetGetMethod());
      state.canRead = true;
    }
    if(info.CanWrite)
    { if(state.set==null) state.set = new ReflectedMethod(info.GetSetMethod());
      else state.set.Add(info.GetSetMethod());
      state.canWrite = true;
    }
  }

  public override string ToString()
  { return string.Format("<property '{0}' on '{1}'>", state.info.Name, state.info.DeclaringType.FullName);
  }

  void setitem(object instance, params object[] args)
  { if(!state.canWrite) throw Ops.TypeError("{0} is a non-writeable attribute", state.info.Name);
    ((ReflectedMethod)state.set.__get__(instance)).Call(args);
  }

  internal class State
  { public State(PropertyInfo info) { this.info=info; }
    public PropertyInfo info;
    public ReflectedMethod get, set;
    public bool canRead, canWrite, index;
  }

  internal State state;

  object instance;
}
#endregion

// TODO: add sequence methods (including slicing) to lists and mapping methods to dictionaries
// TODO: add string methods
// TODO: special case Array slicing?
#region ReflectedType
public sealed class ReflectedType : BoaType
{ ReflectedType(Type type) : base(type)
  { mro = new Tuple(type==typeof(object) ? new object[] { this } :
                                           new object[] { this, ReflectedType.FromType(typeof(object)) });
  }

  public ReflectedConstructor Constructor { get { Initialize(); return cons; } }

  public override object Call(params object[] args)
  { Initialize();
    return cons.Call(args);
  }

  public override void DelAttr(object self, string name)
  { object slot = RawGetSlot(name);
    if(slot==Ops.Missing) throw Ops.AttributeError("no such slot '{0}'", name);
    if(!Ops.DelDescriptor(slot, self)) dict.Remove(name);
  }

  public override object GetRawAttr(string name) { return RawGetSlot(name); }

  public override bool GetAttr(object self, string name, out object value)
  { object slot = RawGetSlot(name);
    if(slot!=Ops.Missing)
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

  public override string Repr(object self)
  { ReflectedType rt = self as ReflectedType;
    if(rt!=null)
    { rt.Initialize();
      ReflectedMethod rm = rt.dict["__repr__"] as ReflectedMethod;
      if(rm!=null && rm.hasStatic) return Ops.ToString(rm.Call());
    }
    return self.ToString();
  }

  public override void SetAttr(object self, string name, object value)
  { object slot = RawGetSlot(name);
    if(slot==Ops.Missing || !Ops.SetDescriptor(slot, self, value))
    { if(self==null) dict[name] = value;
      else throw Ops.AttributeError("no such slot '{0}'", name);
    }
  }

  public override string ToString() { return __repr__(); }

  public string __repr__() { return string.Format("<type {0}>", Ops.Repr(__name__)); }

  public static ReflectedType FromType(Type type)
  { ReflectedType rt;
    lock(types) rt = (ReflectedType)types[type];
    if(rt==null)
    { rt = new ReflectedType(type);
      lock(types) types[type] = rt;
    }
    return rt;
  }

  internal Type Type { get { return type; } }

  protected override void Initialize()
  { if(initialized) return;
    base.Initialize();
    foreach(ConstructorInfo ci in type.GetConstructors()) AddConstructor(ci);
    foreach(EventInfo ei in type.GetEvents()) AddEvent(ei);
    foreach(FieldInfo fi in type.GetFields()) AddField(fi);
    foreach(MethodInfo mi in type.GetMethods()) AddMethod(mi);
    foreach(PropertyInfo pi in type.GetProperties()) AddProperty(pi);

    // dictionary enumerator
    if(typeof(IDictionaryEnumerator).IsAssignableFrom(type))
    { if(!dict.Contains("key")) dict["key"] = dict["Key"];
      if(!dict.Contains("value")) dict["value"] = dict["Value"];
    }

    // iterator protocol
    if(typeof(IEnumerator).IsAssignableFrom(type))
    { if(!dict.Contains("next")) dict["next"] = SpecialAttr.NextMethod.Value;
      if(!dict.Contains("reset")) dict["reset"] = dict["Reset"];
      if(!dict.Contains("value")) dict["value"] = dict["Current"];
    }
    if(typeof(IEnumerable).IsAssignableFrom(type))
    { if(!dict.Contains("__iter__")) dict["__iter__"] = dict["GetEnumerator"];
    }

    if(type.IsSubclassOf(typeof(Delegate))) // delegates
    { dict["__call__"] = SpecialAttr.DelegateCaller.Value;
    }
    else if(type.IsSubclassOf(typeof(Array))) // arrays
    { if(!dict.Contains("__getitem__")) dict["__getitem__"] = SpecialAttr.ArrayGetItem.Value;
      if(!dict.Contains("__setitem__")) dict["__setitem__"] = SpecialAttr.ArraySetItem.Value;
    }
    else if(type==typeof(string)) // strings
    { dict["__repr__"] = new SpecialAttr.StringRepr();
      dict["__getitem__"] = new SpecialAttr.StringGetItem();
      dict["__contains__"] = new SpecialAttr.StringContains();
      dict["join"]  = new SpecialAttr.StringJoin();
      dict["lower"] = dict["ToLower"];
      dict["upper"] = dict["ToUpper"];
    }
    else if(type==typeof(bool)) // bool
    { AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_bool", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, Type.EmptyTypes, null));
      AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_bool", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { typeof(object) }, null));
    }
    else if(type==typeof(double)) // double
    { AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_float", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, Type.EmptyTypes, null));
      AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_float", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { typeof(object) }, null));
    }
    else if(type==typeof(int)) // int
    { AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_int", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, Type.EmptyTypes, null));
      AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_int", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { typeof(object) }, null));
      AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_int", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { typeof(string), typeof(int) }, null));
    }
    else if(type==typeof(IEnumerator)) // IEnumerator
    { AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_iter", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { typeof(object) }, null));
      AddFakeConstructor(typeof(Modules.__builtin__).GetMethod("_iter", BindingFlags.Static|BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { typeof(object), typeof(object) }, null));
    }
    else if(type==typeof(System.Text.RegularExpressions.Match))
    { foreach(MethodInfo mi in typeof(Boa.Modules.re_internal.match).GetMethods()) AddMethod(mi);
      foreach(PropertyInfo pi in typeof(Boa.Modules.re_internal.match).GetProperties()) AddProperty(pi);
    }

    if(!dict.Contains("__doc__")) // add doc strings
    { object[] docs = type.GetCustomAttributes(typeof(DocStringAttribute), false);
      if(docs.Length!=0) dict["__doc__"] = ((DocStringAttribute)docs[0]).Docs;
    }

    foreach(MemberInfo m in type.GetDefaultMembers()) // add handler for default property
      if(m.MemberType==MemberTypes.Property)
      { ReflectedProperty prop = dict[m.Name] as ReflectedProperty;
        if(prop==null) continue;
        if(prop.state.get!=null && !dict.Contains("__getitem__")) dict["__getitem__"] = prop.state.get;
        if(prop.state.set!=null && !dict.Contains("__setitem__")) dict["__setitem__"] = prop.state.set;
      }

    foreach(Type t in type.GetNestedTypes()) AddNestedType(t);
  }

  void AddConstructor(ConstructorInfo ci)
  { if(!ci.IsPublic) return;
    if(cons==null) cons = new ReflectedConstructor(ci);
    else cons.Add(ci);
  }

  void AddFakeConstructor(MethodInfo mi)
  { if(cons==null) cons = new ReflectedConstructor(mi);
    else cons.Add(mi);
  }

  void AddEvent(EventInfo ei) { dict[ei.Name] = new ReflectedEvent(ei); }
  void AddField(FieldInfo fi) { dict[fi.Name] = new ReflectedField(fi); }

  void AddMethod(MethodInfo mi)
  { if(mi.IsSpecialName) return;
    ReflectedMethod rm = (ReflectedMethod)dict[mi.Name];
    if(rm==null) dict[mi.Name] = new ReflectedMethod(mi);
    else rm.Add(mi);
  }

  void AddNestedType(Type type) { dict[type.Name] = ReflectedType.FromType(type); }

  void AddProperty(PropertyInfo pi)
  { ReflectedProperty rp = (ReflectedProperty)dict[pi.Name];
    if(rp==null) dict[pi.Name] = new ReflectedProperty(pi);
    else rp.Add(pi);
  }

  ReflectedConstructor cons;

  static Hashtable types = new Hashtable(); // assumes these fields are initialized in order
  static readonly DynamicType MyDynamicType = ReflectedType.FromType(typeof(ReflectedType));
}
#endregion

} // namespace Boa.Runtime