using System;

namespace Boa.Runtime
{

public sealed class IntOps
{ IntOps() { }

  public static object Add(int a, object b)
  { try
    { if(b is int) return checked(a+(int)b); // TODO: make sure this is worthwhile
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a+1) : a;
        case TypeCode.Byte: return checked(a + (byte)b);
        case TypeCode.Char: return a + (char)b; // TODO: see whether this should return int or char
        case TypeCode.Decimal: return a + (Decimal)b;
        case TypeCode.Double: return a + (double)b;
        case TypeCode.Int16: return checked(a + (short)b);
        case TypeCode.Int64: return checked(a + (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b + a;
          IConvertible ic = b as IConvertible;
          return ic==null ? checked(a + ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__radd__", a);
        case TypeCode.SByte: return checked(a + (sbyte)b);
        case TypeCode.Single: return a + (float)b;
        case TypeCode.UInt16: return checked(a + (ushort)b);
        case TypeCode.UInt32: return checked(a + (uint)b);
        case TypeCode.UInt64: return checked(a + (ulong)b);
      }
      return Ops.TypeError("invalid operand types for +: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return LongOps.Add(a, b); }
  }

  public static object BitwiseAnd(int a, object b)
  { if(b is int) return a&(int)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a&1 : 0;
      case TypeCode.Byte: return a & (byte)b;
      case TypeCode.Char: return a & (char)b;
      case TypeCode.Decimal: return a & (Decimal)b;
      case TypeCode.Double: return a & (double)b;
      case TypeCode.Int16: return a & (short)b;
      case TypeCode.Int64: return a & (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b & a;
        IConvertible ic = b as IConvertible;
        return ic==null ? a & ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rand__", a);
      case TypeCode.SByte: return a & (sbyte)b;
      case TypeCode.Single: return a & (float)b;
      case TypeCode.UInt16: return a & (ushort)b;
      case TypeCode.UInt32: return a & (uint)b;
      case TypeCode.UInt64: return a & (ulong)b;
    }
    return Ops.TypeError("invalid operand types for &: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseOr(int a, object b)
  { if(b is int) return a|(int)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a|1 : a;
      case TypeCode.Byte: return a | (byte)b;
      case TypeCode.Char: return a | (char)b;
      case TypeCode.Decimal: return a | (Decimal)b;
      case TypeCode.Double: return a | (double)b;
      case TypeCode.Int16: return a | (short)b;
      case TypeCode.Int64: return a | (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b | a;
        IConvertible ic = b as IConvertible;
        return ic==null ? a | ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__ror__", a);
      case TypeCode.SByte: return a | (sbyte)b;
      case TypeCode.Single: return a | (float)b;
      case TypeCode.UInt16: return a | (ushort)b;
      case TypeCode.UInt32: return a | (uint)b;
      case TypeCode.UInt64: return a | (ulong)b;
    }
    return Ops.TypeError("invalid operand types for |: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseXor(int a, object b)
  { if(b is int) return a^(int)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a^1 : a;
      case TypeCode.Byte: return a ^ (byte)b;
      case TypeCode.Char: return a ^ (char)b;
      case TypeCode.Decimal: return a ^ (Decimal)b;
      case TypeCode.Double: return a ^ (double)b;
      case TypeCode.Int16: return a ^ (short)b;
      case TypeCode.Int64: return a ^ (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b ^ a;
        IConvertible ic = b as IConvertible;
        return ic==null ? a ^ ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rxor__", a);
      case TypeCode.SByte: return a ^ (sbyte)b;
      case TypeCode.Single: return a ^ (float)b;
      case TypeCode.UInt16: return a ^ (ushort)b;
      case TypeCode.UInt32: return a ^ (uint)b;
      case TypeCode.UInt64: return a ^ (ulong)b;
    }
    return Ops.TypeError("invalid operand types for ^: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static int Compare(int a, object b)
  { if(b is int) return a-(int)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a-1 : a;
      case TypeCode.Byte: return a - (byte)b;
      case TypeCode.Char: return a - (char)b;
      case TypeCode.Decimal:
      { Decimal v = (Decimal)b;
        return a<v ? -1 : a>v ? 1 : 0;
      }
      case TypeCode.Double:
      { double av=a, bv = (double)b;
        return av<bv ? -1 : av>bv ? 1 : 0;
      }
      case TypeCode.Int16: return a - (short)b;
      case TypeCode.Int64: return (int)(a - (long)b);
      case TypeCode.Object:
        if(b is Integer)
        { Integer v = (Integer)b;
          return v>a ? -1 : v<a ? 1 : 0;
        }
        IConvertible ic = b as IConvertible;
        return ic==null ? (int)(a - ic.ToInt64(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : -Ops.ToInt(Ops.Invoke(b, "__cmp__", a));
      case TypeCode.SByte: return a - (sbyte)b;
      case TypeCode.Single:
      { float av=a, bv=(float)b;
        return av<bv ? -1 : av>bv ? 1 : 0;
      }
      case TypeCode.UInt16: return a - (ushort)b;
      case TypeCode.UInt32: return a<0 ? -1 : (int)((uint)a - (uint)b);
      case TypeCode.UInt64: return a<0 ? -1 : (int)((ulong)a - (ulong)b);
    }
    return Ops.TypeError("cannot compare '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object FloorDivide(int a, object b);
  public static object LeftShift(int a, object b);
  public static object Modulus(int a, object b);
  public static object Multiply(int a, object b);
  public static object Power(int a, object b);
  public static object PowerMod(int a, object b, object c);
  public static object Subtract(int a, object b);
  public static object RightShift(int a, object b);
}

} // namespace Boa.Runtime