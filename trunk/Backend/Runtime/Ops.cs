using System;

namespace Language.Runtime
{

public class Ops
{ public static AttributeErrorException AttributeError(string format, params object[] args)
  { return new AttributeErrorException(string.Format(format, args));
  }

  public static object Add(object a, object b)
  { if(a is int && b is int) return (int)a+(int)b;
    if(a is double && b is double) return (double)a+(double)b;
    throw Ops.TypeError("unsupported operand type(s) for +: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object BitwiseAnd(object a, object b)
  { if(a is int && b is int) return (int)a & (int)b;
    throw Ops.TypeError("unsupported operand type(s) for &: '{0}' and '{1}'",
                        a.GetType(), b.GetType());
  }

  public static object BitwiseOr(object a, object b)
  { if(a is int && b is int) return (int)a | (int)b;
    throw Ops.TypeError("unsupported operand type(s) for |: '{0}' and '{1}'",
                        a.GetType(), b.GetType());
  }

  public static object BitwiseXor(object a, object b)
  { if(a is int && b is int) return (int)a ^ (int)b;
    throw Ops.TypeError("unsupported operand type(s) for ^: '{0}' and '{1}'",
                        a.GetType(), b.GetType());
  }

  // TODO: check relative performance of "is" and GetTypeCode()
  public static object BitwiseNegate(object o)
  { if(o is int) return ~(int)o;
    throw Ops.TypeError("unsupported operand type for ~: '{0}'", o.GetType());
  }

  public static object BoolToObject(bool value) { return value ? TRUE : FALSE; }

  public static object Call(object func, params object[] args)
  { ICallable ic = func as ICallable;
    return ic==null ? Invoke(func, "__call__") : ic.Call(args);
  }

  public static object Decrement(object o)
  { if(o is int) return (int)o-1;
    throw Ops.TypeError("unsupported operand type for --: '{0}'", o.GetType());
  }
  
  public static object Divide(object a, object b)
  { if(a is int && b is int) return (int)a/(int)b;
    if(a is double && b is double) return (double)a/(double)b;
    throw Ops.TypeError("unsupported operand type(s) for /: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Equal(object a, object b) { return a==b ? TRUE : BoolToObject(a.Equals(b)); }

  public static object FloorDivide(object a, object b)
  { if(a is int && b is int) return (int)Math.Floor((double)a/(int)b);
    if(a is double && b is double) return Math.Floor((double)a/(double)b);
    throw Ops.TypeError("unsupported operand type(s) for //: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object GetAttr(object obj, string name)
  { throw Ops.AttributeError("type object '{0}' has no attribute '{1}'", obj.GetType(), name);
  }

  public static bool GetAttr(object obj, string name, out object value)
  { value = null;
    return false;
  }

  public static object Identical(object a, object b) { return a==b ? TRUE : FALSE; }

  public static object Increment(object o)
  { if(o is int) return (int)o+1;
    throw Ops.TypeError("unsupported operand type for ++: '{0}'", o.GetType());
  }
  
  public static object Invoke(object target, string name, params object[] args)
  { return Call(GetAttr(target, name), args);
  }

  public static bool TryToInvoke(object target, string name, out object retValue, params object[] args)
  { object method;
    if(GetAttr(target, name, out method)) { retValue = Call(method, args); return true; }
    else { retValue = null; return false; }
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

  public static object LeftShift(object a, object b)
  { if(a is int && b is int) return (int)a<<(int)b;
    throw Ops.TypeError("unsupported operand type(s) for <<: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Less(object a, object b) { return BoolToObject(((IComparable)a).CompareTo(b) < 0); }
  public static object LessEqual(object a, object b) { return BoolToObject(((IComparable)a).CompareTo(b) <= 0); }

  public static object LogicalAnd(object a, object b) { return IsTrue(a) ? b : a; }
  public static object LogicalOr (object a, object b) { return IsTrue(a) ? a : b; }

  public static object Modulus(object a, object b)
  { if(a is int && b is int) return (int)a%(int)b;
    if(a is double && b is double) { return Math.IEEERemainder((double)a, (double)b); }
    throw Ops.TypeError("unsupported operand type(s) for %: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Multiply(object a, object b)
  { if(a is int && b is int) return (int)a*(int)b;
    if(a is double && b is double) return (double)a*(double)b;
    throw Ops.TypeError("unsupported operand type(s) for *: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object More(object a, object b) { return BoolToObject(((IComparable)a).CompareTo(b) > 0); }
  public static object MoreEqual(object a, object b) { return BoolToObject(((IComparable)a).CompareTo(b) >= 0); }

  public static NameErrorException NameError(string format, params object[] args)
  { return new NameErrorException(string.Format(format, args));
  }

  public static object Negate(object o)
  { if(o is int) return -(int)o;
    if(o is double) return -(double)o;
    throw Ops.TypeError("unsupported operand type for -: '{0}'", o.GetType());
  }

  public static object Power(object a, object b)
  { if(a is int && b is int) return (int)Math.Pow((int)a, (int)b);
    if(a is double && b is double) return Math.Pow((double)a, (double)b);
    throw Ops.TypeError("unsupported operand type(s) for **: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static void Print(object o) { Console.Write(o); }
  public static void PrintNewline() { Console.WriteLine(); }

  public static object RightShift(object a, object b)
  { if(a is int && b is int) return (int)a>>(int)b;
    throw Ops.TypeError("unsupported operand type(s) for >>: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static object Subtract(object a, object b)
  { if(a is int && b is int) return (int)a-(int)b;
    if(a is double && b is double) return (double)a-(double)b;
    throw Ops.TypeError("unsupported operand type(s) for -: '{0}' and '{1}'", a.GetType(), b.GetType());
  }

  public static SyntaxErrorException SyntaxError(string format, params object[] args)
  { return new SyntaxErrorException(string.Format(format, args));
  }

  public static TypeErrorException TypeError(string format, params object[] args)
  { return new TypeErrorException(string.Format(format, args));
  }

  public static readonly object FALSE=false, TRUE=true;
}

} // namespace Language.Runtime