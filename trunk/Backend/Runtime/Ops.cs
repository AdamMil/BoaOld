using System;
using System.Collections;

// TODO: investigate python's changes to the division operator

namespace Boa.Runtime
{

[Flags] public enum Conversion
{ Unsafe=1, Safe=3, Reference=5, Identity=7, None=8, Overflow=10, 
  Failure=8, Success=1
}

public sealed class Ops
{ Ops() { }

  public static readonly object Missing = "<missing>"; // relies on .NET interning strings
  public static readonly DefaultBoaComparer DefaultComparer = new DefaultBoaComparer();

  #region ISeqEnumerator
  public class ISeqEnumerator : IEnumerator
  { public ISeqEnumerator(ISequence seq) { this.seq=seq; index=-1; length=seq.__len__(); }

    public object Current
    { get
      { if(index<0 || index>=length) throw new InvalidOperationException();
        return seq.__getitem__(index);
      }
    }
    
    public bool MoveNext()
    { if(index>=length-1) return false;
      index++;
      return true;
    }
    
    public void Reset() { index=-1; }

    ISequence seq;
    int index, length;
  }
  #endregion

  #region IterEnumerator
  public class IterEnumerator : IEnumerator
  { public IterEnumerator(object o)
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
      state = State.BOF;
    }

    enum State : byte { BOF, IN, EOF }
    object iter, current, next, reset;
    State state;
  }
  #endregion
  
  #region SeqEnumerator
  public class SeqEnumerator : IEnumerator
  { public SeqEnumerator(object seq)
    { length  = Ops.ToInt(Ops.Invoke(seq, "__length__"));
      getitem = Ops.GetAttr(seq, "__getitem__");
      index   = -1;
    }

    public object Current
    { get
      { if(index<0 || index>=length) throw new InvalidOperationException();
        return current;
      }
    }
    
    public bool MoveNext()
    { if(index>=length-1) return false;
      current = Ops.Call(getitem, ++index);
      return true;
    }
    
    public void Reset() { index=-1; }

    object getitem, current;
    int index, length;
  }
  #endregion

  public class DefaultBoaComparer : IComparer
  { public int Compare(object x, object y) { return Ops.Compare(x, y); }
  }

  public static AssertionErrorException AssertionError(Boa.AST.Node node, string format, params object[] args)
  { AssertionErrorException e = new AssertionErrorException(string.Format(format, args));
    e.SetPosition(node);
    return e;
  }

  public static AttributeErrorException AttributeError(string format, params object[] args)
  { return new AttributeErrorException(string.Format(format, args));
  }

  public static object Add(object a, object b)
  { if(a is int && b is int) return (int)a+(int)b;
    if(a is double && b is double) return (double)a+(double)b;
    if(a is string && b is string) return (string)a+(string)b;
    throw TypeError("unsupported operand type(s) for +: '{0}' and '{1}'",
                    GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object BitwiseAnd(object a, object b)
  { if(a is int && b is int) return (int)a & (int)b;
    throw TypeError("unsupported operand type(s) for &: '{0}' and '{1}'",
                    GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object BitwiseOr(object a, object b)
  { if(a is int && b is int) return (int)a | (int)b;
    throw TypeError("unsupported operand type(s) for |: '{0}' and '{1}'",
                    GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object BitwiseXor(object a, object b)
  { if(a is int && b is int) return (int)a ^ (int)b;
    throw TypeError("unsupported operand type(s) for ^: '{0}' and '{1}'",
                    GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  // TODO: check relative performance of "is" and GetTypeCode()
  public static object BitwiseNegate(object o)
  { if(o is int) return ~(int)o;
    throw TypeError("unsupported operand type for ~: '{0}'", o.GetType());
  }

  public static object Call(object func, params object[] args)
  { ICallable ic = func as ICallable;
    return ic==null ? Invoke(func, "__call__", args) : ic.Call(args);
  }
  
  public static object Call0(object func) { return Call(func); }
  public static object Call1(object func, object a0) { return Call(func, a0); }

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
  { switch(ConvertTo(o==null ? null : o.GetType(), type))
    { case Conversion.Identity: case Conversion.Reference: return o;
      case Conversion.None: throw Ops.TypeError("object cannot be converted to '{0}'", type);
      default:
        if(type==typeof(bool)) return FromBool(IsTrue(o));
        try { return Convert.ChangeType(o, type); }
        catch(OverflowException) { throw Ops.ValueError("large value caused overflow"); }
    }
  }
  
  public static Conversion ConvertTo(Type from, Type to)
  { if(from==null)
      return !to.IsValueType ? Conversion.Reference : to==typeof(bool) ? Conversion.Safe : Conversion.None;
    else if(to==from) return Conversion.Identity;
    else if(to.IsAssignableFrom(from)) return Conversion.Reference;

    if(from.IsPrimitive && to.IsPrimitive) // TODO: check whether it's faster to use IndexOf() or our own loop
    { if(from==typeof(int))    return IsIn(typeConv[4], to)   ? Conversion.Unsafe : Conversion.Safe;
      if(to  ==typeof(bool))   return IsIn(typeConv[9], from) ? Conversion.None : Conversion.Safe;
      if(from==typeof(double)) return Conversion.None;
      if(from==typeof(long))   return IsIn(typeConv[6], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(char))   return IsIn(typeConv[8], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(byte))   return IsIn(typeConv[1], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(uint))   return IsIn(typeConv[5], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(float))  return to==typeof(double) ? Conversion.Safe : Conversion.None;
      if(from==typeof(short))  return IsIn(typeConv[2], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(ushort)) return IsIn(typeConv[3], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(sbyte))  return IsIn(typeConv[0], to) ? Conversion.Unsafe : Conversion.Safe;
      if(from==typeof(ulong))  return IsIn(typeConv[7], to) ? Conversion.Unsafe : Conversion.Safe;
    }
    if(from.IsArray && to.IsArray && to.GetElementType().IsAssignableFrom(from.GetElementType()))
      return Conversion.Reference;
    return Conversion.None;
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
    return TryInvoke(desc, "__delete__", out dummy, instance);
  }

  public static object Divide(object a, object b)
  { if(a is int && b is int) return (int)a/(int)b;
    if(a is double && b is double) return (double)a/(double)b;
    throw TypeError("unsupported operand type(s) for /: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static EOFErrorException EOFError(string format, params object[] args)
  { return new EOFErrorException(string.Format(format, args));
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
    throw TypeError("unsupported operand type(s) for //: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object FromBool(bool value) { return value ? TRUE : FALSE; }

  public static object GetAttr(object o, string name)
  { IHasAttributes iha = o as IHasAttributes;
    if(iha!=null)
    { object ret = iha.__getattr__(name);
      if(ret==Ops.Missing) throw AttributeError("object has no attribute '{0}'", name);
      return ret;
    }
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
    if(TryInvoke(desc, "__get__", out ret, instance)) return ret; // TODO: this is expensive.. optimize it.
    return desc;
  }

  public static DynamicType GetDynamicType(object o)
  { if(o==null) return NoneType.Value;
    IDynamicObject dt = o as IDynamicObject;
    if(dt!=null) return dt.GetDynamicType();
    if(o is string) return StringType;
    return ReflectedType.FromType(o.GetType());
  }

  public static IEnumerator GetEnumerator(object o)
  { IEnumerator e;
    if(GetEnumerator(o, out e)) return e;
    throw TypeError("object is not enumerable");
  }

  public static bool GetEnumerator(object o, out IEnumerator e)
  { if(o is string) e=StringOps.GetEnumerator((string)o);
    else if(o is IEnumerator) e=(IEnumerator)o;
    else if(o is IEnumerable) e=((IEnumerable)o).GetEnumerator();
    else if(o is ISequence) e=new ISeqEnumerator((ISequence)o);
    else
    { object iter;
      if(TryInvoke(o, "__iter__", out iter)) e = new IterEnumerator(iter);
      else
      { object len, getitem;
        if(GetAttr(o, "__len__", out len) && GetAttr(o, "__getitem__", out getitem)) e = new SeqEnumerator(o);
        else e=null;
      }
    }
    return e!=null;
  }

  public static Module GetExecutingModule()
  { System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
    for(int i=trace.FrameCount-1; i>=0; i--)
    { Type type = trace.GetFrame(i).GetMethod().DeclaringType;
      System.Reflection.FieldInfo fi = type.GetField(Module.FieldName);
      if(fi!=null && type.IsSubclassOf(typeof(Boa.AST.Snippet))) return (Module)fi.GetValue(null);
    }
    if(Frames.Count>0) return ((Frame)Frames.Peek()).Module;
    throw new InvalidOperationException("No executing module");
  }

  public static void Import(Module module, string[] names, string[] asNames)
  { for(int i=0; i<names.Length; i++)
    { string dname = asNames[i]!=null ? asNames[i] : names[i].IndexOf('.')==-1 ? names[i] : names[i].Split('.')[0];
      module.__setattr__(dname, asNames[i]==null ? Importer.ImportTop(names[i]) : Importer.Import(names[i]));
    }
  }

  public static ImportErrorException ImportError(string format, params object[] args)
  { return new ImportErrorException(string.Format(format, args));
  }

  public static void ImportFrom(Module module, string moduleName, string[] names, string[] asNames)
  { if(names.Length==0) return;
    if(names[0]=="*") ImportStar(module, moduleName);
    else
    { IHasAttributes mod = (IHasAttributes)Importer.Import(moduleName);
      ISequence exports = mod.__getattr__("__all__") as ISequence;
      if(exports==null && mod is Module) return;
      for(int i=0; i<names.Length; i++)
        if(exports==null || exports.__contains__(names[i]))
          module.__setattr__(asNames[i]==null ? names[i] : asNames[i], mod.__getattr__(names[i]));
    }
  }

  public static void ImportStar(Module module, string moduleName)
  { IHasAttributes mod = (IHasAttributes)Importer.Import(moduleName);
    ISequence exports = mod.__getattr__("__all__") as ISequence;
    if(exports==null && mod is Module) return;
    if(exports==null) foreach(string name in mod.__attrs__()) module.__setattr__(name, mod.__getattr__(name));
    else
      for(int i=0,len=exports.__len__(); i<len; i++)
      { string name = (string)exports.__getitem__(i);
        module.__setattr__(name, mod.__getattr__(name));
      }
  }

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
    if(TryInvoke(o, "__nonzero__", out o)) return IsTrue(o);
    return true;
  }

  public static KeyErrorException KeyError(string format, params object[] args)
  { return new KeyErrorException(string.Format(format, args));
  }

  public static object LeftShift(object a, object b)
  { if(a is int && b is int) return (int)a<<(int)b;
    throw TypeError("unsupported operand type(s) for <<: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object Less(object a, object b) { return FromBool(Compare(a,b)<0); }
  public static object LessEqual(object a, object b) { return FromBool(Compare(a,b)<=0); }

  public static object LogicalAnd(object a, object b) { return IsTrue(a) ? b : a; }
  public static object LogicalOr (object a, object b) { return IsTrue(a) ? a : b; }

  public static object Modulus(object a, object b)
  { if(a is int && b is int) return (int)a%(int)b;
    if(a is double && b is double) { return Math.IEEERemainder((double)a, (double)b); }
    throw TypeError("unsupported operand type(s) for %: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static object Multiply(object a, object b)
  { if(a is int && b is int) return (int)a*(int)b;
    if(a is double && b is double) return (double)a*(double)b;
    throw TypeError("unsupported operand type(s) for *: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
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

    string s = o as string;
    if(s!=null) return StringOps.Quote(s);
    
    IRepresentable ir = o as IRepresentable;
    if(ir!=null) return ir.__repr__();

    Array a = o as Array;
    if(a!=null) return ArrayOps.Repr(a);

    if(o is long) return ((long)o).ToString() + "L";

    return GetDynamicType(o).Repr(o);
  }

  public static object RightShift(object a, object b)
  { if(a is int && b is int) return (int)a>>(int)b;
    throw TypeError("unsupported operand type(s) for >>: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static RuntimeException RuntimeError(string format, params object[] args)
  { return new RuntimeException(string.Format(format, args));
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
    return TryInvoke(desc, "__set__", out dummy, instance, value);
  }

  public static string Str(object o)
  { if(o is string) return (string)o;
    object ret;
    if(TryInvoke(o, "__str__", out ret)) return Ops.ToString(ret);
    return o.ToString();
  }

  public static object Subtract(object a, object b)
  { if(a is int && b is int) return (int)a-(int)b;
    if(a is double && b is double) return (double)a-(double)b;
    throw TypeError("unsupported operand type(s) for -: '{0}' and '{1}'", GetDynamicType(a).__name__, GetDynamicType(b).__name__);
  }

  public static SyntaxErrorException SyntaxError(string format, params object[] args)
  { return new SyntaxErrorException(string.Format(format, args));
  }

  public static SyntaxErrorException SyntaxError(Boa.AST.Node node, string format, params object[] args)
  { SyntaxErrorException e = new SyntaxErrorException(string.Format(format, args));
    e.SetPosition(node);
    return e;
  }

  public static object ToBoa(object o) { return o is char ? new string((char)o, 1) : o; }

  public static double ToFloat(object o)
  { if(o is double) return (double)o;
    try { return Convert.ToDouble(o); }
    catch(OverflowException) { throw Ops.ValueError("too big for float"); }
    catch(InvalidCastException) { throw Ops.TypeError("expected float, found {0}", GetDynamicType(o).__name__); }
  }

  public static int ToInt(object o)
  { if(o is int) return (int)o;
    try { return Convert.ToInt32(o); }
    catch(OverflowException) { throw Ops.ValueError("too big for int"); } // TODO: allow conversion to long integer?
    catch(InvalidCastException) { throw Ops.TypeError("expected int, found {0}", GetDynamicType(o).__name__); }
  }

  public static string ToString(object o)
  { if(o is string) return (string)o;
    return o.ToString();
  }

  public static TypeErrorException TooFewArgs(string name, int expected, int got)
  { return TypeError("{0} requires at least {1} ({2} given)", name, expected, got);
  }

  public static bool TryInvoke(object target, string name, out object retValue, params object[] args)
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

  public static Stack Frames = new Stack();

  public static readonly object FALSE=false, TRUE=true;

  static bool IsIn(Type[] typeArr, Type type)
  { for(int i=0; i<typeArr.Length; i++) if(typeArr[i]==type) return true;
    return false;
  }

  static readonly ReflectedType StringType = ReflectedType.FromType(typeof(string));
  static readonly Type[][] typeConv = 
  { // FROM
    new Type[] { typeof(int), typeof(double), typeof(short), typeof(long), typeof(float) }, // sbyte
    new Type[] // byte
    { typeof(int), typeof(double), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(float)
    },
    new Type[] { typeof(int), typeof(double), typeof(long), typeof(float) }, // short
    new Type[] { typeof(int), typeof(double), typeof(uint), typeof(long), typeof(ulong), typeof(float) }, // ushort
    new Type[] { typeof(double), typeof(long), typeof(float) }, // int
    new Type[] { typeof(double), typeof(long), typeof(ulong), typeof(float) }, // uint
    new Type[] { typeof(double), typeof(float) }, // long
    new Type[] { typeof(double), typeof(float) }, // ulong
    new Type[] // char
    { typeof(int), typeof(double), typeof(ushort), typeof(uint), typeof(long), typeof(ulong), typeof(float)
    },

    // TO
    new Type[] // bool
    { typeof(int), typeof(byte), typeof(char), typeof(sbyte), typeof(short), typeof(ushort), typeof(uint),
      typeof(long), typeof(ulong)
    }
  };
}

} // namespace Boa.Runtime
