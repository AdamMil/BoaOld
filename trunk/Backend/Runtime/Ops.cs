using System;
using System.Collections;

namespace Boa.Runtime
{

public sealed class Ops
{ Ops() { }

  public static readonly object Missing = "<missing>"; // relies on .NET interning strings
  public static readonly DefaultBoaComparer DefaultComparer = new DefaultBoaComparer();

  #region BoaEnumerator
  public class BoaEnumerator : IEnumerator
  { public BoaEnumerator(object o)
    { iter = o;
      next = Ops.GetAttr(o, "next");
      Ops.GetAttr(o, "reset", out reset);
    }

    public object Current
    { get
      { if(state!=State.IN) throw new InvalidOperationException();
        return current;
      }
    }

    public bool MoveNext()
    { if(state==State.EOF) return false;
      try { current=Ops.Call(next); state=State.IN; return true; }
      catch(StopIterationException) { state=State.EOF; return false; }
    }

    public void Reset()
    { if(reset==null) throw new NotImplementedException("this iterator does not implement reset()");
      Ops.Call(reset);
    }

    enum State : byte { BOF, IN, EOF }
    object iter, current, next, reset;
    State state;
  }
  #endregion

  public class DefaultBoaComparer : IComparer
  { public int Compare(object x, object y) { return Ops.Compare(x, y); }
  }

  public static AttributeErrorException AttributeError(string format, params object[] args)
  { return new AttributeErrorException(string.Format(format, args));
  }

  public static object Add(object a, object b)
  { if(a is int && b is int) return (int)a+(int)b;
    if(a is double && b is double) return (double)a+(double)b;
    throw TypeError("unsupported operand type(s) for +: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object BitwiseAnd(object a, object b)
  { if(a is int && b is int) return (int)a & (int)b;
    throw TypeError("unsupported operand type(s) for &: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object BitwiseOr(object a, object b)
  { if(a is int && b is int) return (int)a | (int)b;
    throw TypeError("unsupported operand type(s) for |: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object BitwiseXor(object a, object b)
  { if(a is int && b is int) return (int)a ^ (int)b;
    throw TypeError("unsupported operand type(s) for ^: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  // TODO: check relative performance of "is" and GetTypeCode()
  public static object BitwiseNegate(object o)
  { if(o is int) return ~(int)o;
    throw TypeError("unsupported operand type for ~: '{0}'", o.GetType());
  }

  public static object Call(object func, params object[] args)
  { ICallable ic = func as ICallable;
    return ic==null ? Invoke(func, "__call__") : ic.Call(args);
  }
  
  public static object CallWithArgsSequence(object func, object seq)
  { object[] arr = seq as object[];
    if(arr!=null) return Call(func, arr);

    Tuple tup = seq as Tuple;
    if(tup!=null) return Call(func, tup.items);
    
    ICollection col = seq as ICollection;
    if(col!=null)
    { arr = new object[col.Count];
      col.CopyTo(arr, 0);
      return Call(func, arr);
    }

    List list = new List(Ops.GetEnumerator(seq));
    arr = new object[list.Count];
    list.CopyTo(arr, 0);
    return Call(func, arr);
  }

  public static int Compare(object a, object b)
  { if(a==b) return 0;
    IComparable c = a as IComparable;
    if(c!=null)
      try { return c.CompareTo(b); }
      catch(ArgumentException) { }
    throw Ops.TypeError("can't compare {0} to {1}", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object ConvertTo(object o, Type type)
  { if(o==null) return null;
    if(type==typeof(bool)) return FromBool(IsTrue(o));
    try { return Convert.ChangeType(o, type); }
    catch(OverflowException) { throw Ops.ValueError("large value caused overflow"); }
  }

  public static void DelAttr(object o, string name)
  { IHasAttributes iha = o as IHasAttributes;
    if(iha!=null) iha.__delattr__(name);
    else GetDynamicType(o).DelAttr(o, name);
  }

  public static bool DelDescriptor(object desc, object instance)
  { IDataDescriptor dd = desc as IDataDescriptor;
    if(dd != null) { dd.__delete__(instance); return true; }
    object dummy;
    return TryToInvoke(desc, "__delete__", out dummy, instance);
  }

  public static object Divide(object a, object b)
  { if(a is int && b is int) return (int)a/(int)b;
    if(a is double && b is double) return (double)a/(double)b;
    throw TypeError("unsupported operand type(s) for /: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Equal(object a, object b) { return a==b ? TRUE : FromBool(Compare(a,b)==0); }

  public static int FixIndex(int index, int length)
  { if(index<0)
    { index += length;
      if(index<0) throw IndexError("index out of range: {0}", index-length);
    }
    else if(index>=length) throw IndexError("index out of range: {0}", index);
    return index;
  }

  public static object FloorDivide(object a, object b)
  { if(a is int && b is int) return (int)Math.Floor((double)a/(int)b);
    if(a is double && b is double) return Math.Floor((double)a/(double)b);
    throw TypeError("unsupported operand type(s) for //: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object FromBool(bool value) { return value ? TRUE : FALSE; }

  public static object GetAttr(object o, string name)
  { IHasAttributes iha = o as IHasAttributes;
    if(iha!=null) return iha.__getattr__(name);
    return GetDynamicType(o).GetAttr(o, name);
  }

  public static bool GetAttr(object o, string name, out object value)
  { try
    { IHasAttributes iha = o as IHasAttributes;
      if(iha!=null)
      { value = iha.__getattr__(name);
        return value!=Missing;
      }
      return GetDynamicType(o).GetAttr(o, name, out value);
    }
    catch(AttributeErrorException) { value=null; return false; }
  }

  public static List GetAttrNames(object o)
  { IHasAttributes iha = o as IHasAttributes;
    return iha==null ? GetDynamicType(o).GetAttrNames(o) : iha.__attrs__();
  }

  public static object GetDescriptor(object desc, object instance)
  { IDescriptor d = desc as IDescriptor;
    if(d!=null) return d.__get__(instance);
    object ret;
    if(TryToInvoke(desc, "__get__", out ret, instance)) return ret; // TODO: this is expensive.. optimize it.
    return desc;
  }

  public static DynamicType GetDynamicType(object o)
  { IDynamicObject dt = o as IDynamicObject;
    if(dt!=null) return dt.GetDynamicType();
    if(o is string) return StringType;
    if(o==null) return NoneType.Value;
    return ReflectedType.FromType(o.GetType());
  }

  public static IEnumerator GetEnumerator(object o)
  { if(o is string) return StringOps.GetEnumerator((string)o);
    if(o is IEnumerator) return (IEnumerator)o;
    if(o is IEnumerable) return ((IEnumerable)o).GetEnumerator();
    return new BoaEnumerator(Invoke(o, "__iter__"));
  }

  public static bool GetEnumerator(object o, out IEnumerator e)
  { if(o is string) e=StringOps.GetEnumerator((string)o);
    else if(o is IEnumerator) e=(IEnumerator)o;
    else if(o is IEnumerable) e=((IEnumerable)o).GetEnumerator();
    else
    { object iter;
      if(TryToInvoke(o, "__iter__", out iter)) e = new BoaEnumerator(iter);
      else { e=null; return false; }
    }
    return true;
  }

  public static object Identical(object a, object b) { return a==b ? TRUE : FALSE; }

  public static IndexErrorException IndexError(string format, params object[] args)
  { return new IndexErrorException(string.Format(format, args));
  }

  public static object Invoke(object target, string name, params object[] args)
  { return Call(GetAttr(target, name), args);
  }

  public static bool IsTrue(object o)
  { if(o==null) return false;
    switch(Convert.GetTypeCode(o))
    { case TypeCode.Boolean: return (bool)o;
      case TypeCode.Int32: return ((int)o) != 0;
      case TypeCode.String: return ((string)o).Length != 0;
    }
    if(o is System.Collections.ICollection) return ((System.Collections.ICollection)o).Count != 0;
    if(TryToInvoke(o, "__nonzero__", out o)) return IsTrue(o);
    return true;
  }

  public static KeyErrorException KeyError(string format, params object[] args)
  { return new KeyErrorException(string.Format(format, args));
  }

  public static object LeftShift(object a, object b)
  { if(a is int && b is int) return (int)a<<(int)b;
    throw TypeError("unsupported operand type(s) for <<: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Less(object a, object b) { return FromBool(Compare(a,b)<0); }
  public static object LessEqual(object a, object b) { return FromBool(Compare(a,b)<=0); }

  public static object LogicalAnd(object a, object b) { return IsTrue(a) ? b : a; }
  public static object LogicalOr (object a, object b) { return IsTrue(a) ? a : b; }

  public static object Modulus(object a, object b)
  { if(a is int && b is int) return (int)a%(int)b;
    if(a is double && b is double) { return Math.IEEERemainder((double)a, (double)b); }
    throw TypeError("unsupported operand type(s) for %: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Multiply(object a, object b)
  { if(a is int && b is int) return (int)a*(int)b;
    if(a is double && b is double) return (double)a*(double)b;
    throw TypeError("unsupported operand type(s) for *: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object More(object a, object b) { return FromBool(Compare(a,b)>0); }
  public static object MoreEqual(object a, object b) { return FromBool(Compare(a,b)>=0); }

  public static NameErrorException NameError(string format, params object[] args)
  { return new NameErrorException(string.Format(format, args));
  }

  public static object Negate(object o)
  { if(o is int) return -(int)o;
    if(o is double) return -(double)o;
    throw TypeError("unsupported operand type for -: '{0}'", o.GetType());
  }

  public static object NotEqual(object a, object b) { return a==b ? FALSE : FromBool(Compare(a,b)!=0); }

  public static NotImplementedException NotImplemented(string format, params object[] args)
  { return new NotImplementedException(string.Format(format, args));
  }

  public static object Power(object value, object power)
  { if(value is int && power is int) return (int)Math.Pow((int)value, (int)power);
    if(value is double && power is double) return Math.Pow((double)value, (double)power);
    throw TypeError("unsupported operand type(s) for **: '{0}' and '{1}'", value.GetType(), power.GetType());
  }
  public static object PowerMod(object value, object power, object mod) { return Modulus(Power(value, power), mod); }

  public static void Print(object o) { Console.Write(o); }
  public static void PrintNewline() { Console.WriteLine(); }

  public static string Repr(object o)
  { if(o==null) return "null";

    string s = o as string; // optimization
    if(s!=null) return StringOps.Quote(s);
    
    IRepresentable ir = o as IRepresentable;
    if(ir!=null) return ir.ToReprString();

    Array a = o as Array;
    if(a!=null) return ArrayOps.Repr(a);

    if(o is long) return ((long)o).ToString() + "L";

    return GetDynamicType(o).Repr(o);
  }

  public static object RightShift(object a, object b)
  { if(a is int && b is int) return (int)a>>(int)b;
    throw TypeError("unsupported operand type(s) for >>: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static void SetAttr(object value, object o, string name)
  { IHasAttributes iha = o as IHasAttributes;
    if(iha!=null)  iha.__setattr__(name, value);
    else GetDynamicType(o).SetAttr(o, name, value);
  }

  public static bool SetDescriptor(object desc, object instance, object value)
  { IDataDescriptor dd = desc as IDataDescriptor;
    if(dd!=null) { dd.__set__(instance, value); return true; }
    object dummy;
    return TryToInvoke(desc, "__set__", out dummy, instance, value);
  }

  public static object Subtract(object a, object b)
  { if(a is int && b is int) return (int)a-(int)b;
    if(a is double && b is double) return (double)a-(double)b;
    throw TypeError("unsupported operand type(s) for -: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static SyntaxErrorException SyntaxError(string format, params object[] args)
  { return new SyntaxErrorException(string.Format(format, args));
  }

  public static object ToBoa(object o) { return o is char ? new string((char)o, 1) : o; }

  public static int ToInt(object o)
  { if(o is int) return (int)o;
    try { return Convert.ToInt32(o); }
    catch(OverflowException) { throw Ops.ValueError("too big for int"); } // TODO: allow conversion to long integer?
    catch(InvalidCastException) { throw Ops.TypeError("expected int, found {0}", GetDynamicType(o).__name__); }
  }
  
  public static TypeErrorException TooFewArgs(string name, int expected, int got)
  { return TypeError("{0} requires at least {1} ({2} given)", name, expected, got);
  }

  public static bool TryToInvoke(object target, string name, out object retValue, params object[] args)
  { object method;
    if(GetAttr(target, name, out method)) { retValue = Call(method, args); return true; }
    else { retValue = null; return false; }
  }

  public static TypeErrorException TypeError(string format, params object[] args)
  { return new TypeErrorException(string.Format(format, args));
  }

  public static ValueErrorException ValueError(string format, params object[] args)
  { return new ValueErrorException(string.Format(format, args));
  }

  public static readonly object FALSE=false, TRUE=true;
  
  static readonly ReflectedType StringType = ReflectedType.FromType(typeof(string));
}

} // namespace Boa.Runtime
