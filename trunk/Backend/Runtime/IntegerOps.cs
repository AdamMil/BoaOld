using System;

namespace Boa.Runtime
{

public sealed class IntegerOps
{ IntegerOps() { }

  public static object Add(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a+1 : a;
      case TypeCode.Byte: return a + (byte)b;
      case TypeCode.Double: return a.ToDouble(null) + (double)b;
      case TypeCode.Decimal: return a.ToDecimal(null) + (Decimal)b;
      case TypeCode.Int16: return a + (short)b;
      case TypeCode.Int32: return a + (int)b;
      case TypeCode.Int64: return a + (long)b;
      case TypeCode.Object:
        if(b is Integer) return a + (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a + ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__radd__", a);
      case TypeCode.SByte: return a + (sbyte)b;
      case TypeCode.Single: return a.ToSingle(null) + (float)b;
      case TypeCode.UInt16: return a + (ushort)b;
      case TypeCode.UInt32: return a + (uint)b;
      case TypeCode.UInt64: return a + (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for +: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseAnd(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? (a&1).ToInt32(null) : 0;
      case TypeCode.Byte: return a & (byte)b;
      case TypeCode.Int16: return a & (short)b;
      case TypeCode.Int32: return a & (int)b;
      case TypeCode.Int64: return a & (long)b;
      case TypeCode.Object:
        if(b is Integer) return a & (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a & ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rand__", a);
      case TypeCode.SByte: return a & (sbyte)b;
      case TypeCode.UInt16: return a & (ushort)b;
      case TypeCode.UInt32: return a & (uint)b;
      case TypeCode.UInt64: return a & (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for &: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static Integer BitwiseOr(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a|1 : a;
      case TypeCode.Byte: return a | (byte)b;
      case TypeCode.Int16: return a | (short)b;
      case TypeCode.Int32: return a | (int)b;
      case TypeCode.Int64: return a | (long)b;
      case TypeCode.Object:
        if(b is Integer) return a | (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a | ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__ror__", a);
      case TypeCode.SByte: return a | (sbyte)b;
      case TypeCode.UInt16: return a | (ushort)b;
      case TypeCode.UInt32: return a | (uint)b;
      case TypeCode.UInt64: return a | (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for |: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static Integer BitwiseXor(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a^1 : a;
      case TypeCode.Byte: return a ^ (byte)b;
      case TypeCode.Int16: return a ^ (short)b;
      case TypeCode.Int32: return a ^ (int)b;
      case TypeCode.Int64: return a ^ (long)b;
      case TypeCode.Object:
        if(b is Integer) return a ^ (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a ^ ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rxor__", a);
      case TypeCode.SByte: return a ^ (sbyte)b;
      case TypeCode.UInt16: return a ^ (ushort)b;
      case TypeCode.UInt32: return a ^ (uint)b;
      case TypeCode.UInt64: return a ^ (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for ^: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static int Compare(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a.CompareTo((bool)b ? 1 : 0);
      case TypeCode.Byte: return a.CompareTo((byte)b);
      case TypeCode.Int16: return a.CompareTo((short)b);
      case TypeCode.Int32: return a.CompareTo((int)b);
      case TypeCode.Int64: return a.CompareTo((long)b);
      case TypeCode.Object:
        if(b is Integer) return a.CompareTo((Integer)b);
        IConvertible ic = b as IConvertible;
        return ic!=null ? a.CompareTo(ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : -Ops.ToInt(Ops.Invoke(b, "__cmp__", a));
      case TypeCode.SByte: return a.CompareTo((sbyte)b);
      case TypeCode.UInt16: return a.CompareTo((ushort)b);
      case TypeCode.UInt32: return a.CompareTo((uint)b);
      case TypeCode.UInt64: return a.CompareTo((ulong)b);
    }
    throw Ops.TypeError("can't compare '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object Divide(Integer a, object b)
  { Integer bv;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: bv = new Integer((bool)b ? 1 : 0); break;
      case TypeCode.Byte: bv = new Integer((byte)b); break;
      case TypeCode.Double: case TypeCode.Single: case TypeCode.Decimal: return FloatOps.Divide(a.ToDouble(null), b);
      case TypeCode.Int16: bv = new Integer((short)b); break;
      case TypeCode.Int32: bv = new Integer((int)b); break;
      case TypeCode.Int64: bv = new Integer((long)b); break;
      case TypeCode.Object:
        if(b is Integer) bv = (Integer)b;
        else
        { IConvertible ic = b as IConvertible;
          if(ic!=null) bv = new Integer(ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo));
          else 
          { object ret;
            return Ops.TryInvoke(b, "__rtruediv__", out ret, a) ? ret : Ops.Invoke(b, "__rdiv__", out ret, a);
          }
        }
        break;
      case TypeCode.SByte: bv = new Integer((sbyte)b); break;
      case TypeCode.UInt16: bv = new Integer((ushort)b); break;
      case TypeCode.UInt32: bv = new Integer((uint)b); break;
      case TypeCode.UInt64: bv = new Integer((ulong)b); break;
      default: throw Ops.TypeError("invalid operand types for /: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    if(bv.IsZero) throw Ops.DivideByZeroError("long division by zero");
    if(a.DivisibleBy(bv)) return a/b;
    return a.ToDouble(null) / b.ToDouble(null);
  }

  public static object FloorDivide(Integer a, object b)
  { Integer bv;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: bv = new Integer((bool)b ? 1 : 0); break;
      case TypeCode.Byte: bv = new Integer((byte)b); break;
      case TypeCode.Double: case TypeCode.Single: case TypeCode.Decimal: return FloatOps.Divide(a.ToDouble(null), b);
      case TypeCode.Int16: bv = new Integer((short)b); break;
      case TypeCode.Int32: bv = new Integer((int)b); break;
      case TypeCode.Int64: bv = new Integer((long)b); break;
      case TypeCode.Object:
        if(b is Integer) bv = (Integer)b;
        else
        { IConvertible ic = b as IConvertible;
          if(ic!=null) bv = new Integer(ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo));
          else return Ops.Invoke(b, "__rfloordiv__", out ret, a);
        }
        break;
      case TypeCode.SByte: bv = new Integer((sbyte)b); break;
      case TypeCode.UInt16: bv = new Integer((ushort)b); break;
      case TypeCode.UInt32: bv = new Integer((uint)b); break;
      case TypeCode.UInt64: bv = new Integer((ulong)b); break;
      default: throw Ops.TypeError("invalid operand types for /: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    if(bv.IsZero) throw Ops.DivideByZeroError("long floor division by zero");
    return (a<0 ? (a-bv+bv.Sign) : a) / bv;
  }

  public static Integer LeftShift(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a<<1 : a;
      case TypeCode.Byte: return a << (byte)b;
      case TypeCode.Int16: return a << (short)b;
      case TypeCode.Int32: return a << (int)b;
      case TypeCode.Int64: return a << (long)b;
      case TypeCode.Object:
        if(b is Integer) return a << (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a << ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rlshift__", a);
      case TypeCode.SByte: return a << (sbyte)b;
      case TypeCode.UInt16: return a << (ushort)b;
      case TypeCode.UInt32: return a << (uint)b;
      case TypeCode.UInt64: return a << (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for <<: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object Modulus(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a % ((bool)b ? 1 : 0);
      case TypeCode.Byte: return a % (byte)b;
      case TypeCode.Int16: return a % (short)b;
      case TypeCode.Int32: return a % (int)b;
      case TypeCode.Int64: return a % (long)b;
      case TypeCode.Object:
        if(b is Integer) return a % (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a % ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rmod__", a);
      case TypeCode.SByte: return a % (sbyte)b;
      case TypeCode.UInt16: return a % (ushort)b;
      case TypeCode.UInt32: return a % (uint)b;
      case TypeCode.UInt64: return a % (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for %: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object Multiply(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a : 0;
      case TypeCode.Byte: return a * (byte)b;
      case TypeCode.Int16: return a * (short)b;
      case TypeCode.Int32: return a * (int)b;
      case TypeCode.Int64: return a * (long)b;
      case TypeCode.Object:
        if(b is Integer) return a * (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a * ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rmul__", a);
      case TypeCode.SByte: return a * (sbyte)b;
      case TypeCode.UInt16: return a * (ushort)b;
      case TypeCode.UInt32: return a * (uint)b;
      case TypeCode.UInt64: return a * (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for *: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object Power(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a : a.IsNegative ? -1 : 1;
      case TypeCode.Byte: return a.Pow((byte)b);
      case TypeCode.Int16: return a.Pow((short)b);
      case TypeCode.Int32: return a.Pow((int)b);
      case TypeCode.Int64: return a.Pow((long)b);
      case TypeCode.Object:
        if(b is Integer) return a.Pow((Integer)b);
        IConvertible ic = b as IConvertible;
        return ic!=null ? a.Pow(ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.Invoke(b, "__rpow__", a);
      case TypeCode.SByte: return a.Pow((sbyte)b);
      case TypeCode.UInt16: return a.Pow((ushort)b);
      case TypeCode.UInt32: return a.Pow((uint)b);
      case TypeCode.UInt64: return a.Pow((ulong)b);
    }
    throw Ops.TypeError("invalid operand types for *: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object PowerMod(Integer a, object b, object c)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a.Pow((bool)b ? 1 : 0, c);
      case TypeCode.Byte: return a.Pow((byte)b, c);
      case TypeCode.Int16: return a.Pow((short)b, c);
      case TypeCode.Int32: return a.Pow((int)b, c);
      case TypeCode.Int64: return a.Pow((long)b, c);
      case TypeCode.Object:
        if(b is Integer) return a.Pow((Integer)b, c);
        IConvertible ic = b as IConvertible;
        if(ic!=null) return a.Pow(ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo), c);
        break;
      case TypeCode.SByte: return a.Pow((sbyte)b, c);
      case TypeCode.UInt16: return a.Pow((ushort)b, c);
      case TypeCode.UInt32: return a.Pow((uint)b, c);
      case TypeCode.UInt64: return a.Pow((ulong)b, c);
    }
    throw Ops.TypeError("invalid operand types for ternary pow: '{0}', '{1}', and '{2}'",
                        Ops.TypeName(a), Ops.TypeName(b), Ops.TypeName(c));
  }

  public static object Subtract(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a-1 : a;
      case TypeCode.Byte: return a - (byte)b;
      case TypeCode.Double: return a.ToDouble(null) - (double)b;
      case TypeCode.Decimal: return a.ToDecimal(null) - (Decimal)b;
      case TypeCode.Int16: return a - (short)b;
      case TypeCode.Int32: return a - (int)b;
      case TypeCode.Int64: return a - (long)b;
      case TypeCode.Object:
        if(b is Integer) return a - (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a - ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rsub__", a);
      case TypeCode.SByte: return a - (sbyte)b;
      case TypeCode.Single: return a.ToSingle(null) - (float)b;
      case TypeCode.UInt16: return a - (ushort)b;
      case TypeCode.UInt32: return a - (uint)b;
      case TypeCode.UInt64: return a - (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for -: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object RightShift(Integer a, object b)
  { switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a>>1 : a;
      case TypeCode.Byte: return a >> (byte)b;
      case TypeCode.Int16: return a >> (short)b;
      case TypeCode.Int32: return a >> (int)b;
      case TypeCode.Int64: return a >> (long)b;
      case TypeCode.Object:
        if(b is Integer) return a >> (Integer)b;
        IConvertible ic = b as IConvertible;
        return ic!=null ? a >> ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rrshift__", a);
      case TypeCode.SByte: return a >> (sbyte)b;
      case TypeCode.UInt16: return a >> (ushort)b;
      case TypeCode.UInt32: return a >> (uint)b;
      case TypeCode.UInt64: return a >> (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for >>: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }
}

} // namespace Boa.Runtime