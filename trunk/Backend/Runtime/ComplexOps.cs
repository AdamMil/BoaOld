using System;

namespace Boa.Runtime
{

public sealed class ComplexOps
{ ComplexOps() { }

  public Complex Add(Complex a, object b)
  { if(b is Complex) return a + (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a + (bool)b ? 1 : 0;
      case TypeCode.Byte: return a + (byte)b;
      case TypeCode.Decimal:
        return a + ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a + (double)b;
      case TypeCode.Int16: return a + (short)b;
      case TypeCode.Int32: return a + (int)b;
      case TypeCode.Int64: return a + (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic==null ? a + ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__radd__", a);
      case TypeCode.SByte: return a + (sbyte)b;
      case TypeCode.Single: return a + (float)b;
      case TypeCode.UInt16: return a + (ushort)b;
      case TypeCode.UInt32: return a + (uint)b;
      case TypeCode.UInt64: return a + (ulong)b;
    }
    return Ops.TypeError("invalid operand types for +: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public int Compare(Complex a, object b)
  { throw Ops.TypeError("cannot compare complex numbers except for equality/inequality");
  }

  public Complex Divide(Complex a, object b)
  { if(b is Complex) return a / (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a / (bool)b ? 1 : 0;
      case TypeCode.Byte: return a / (byte)b;
      case TypeCode.Decimal:
        return a / ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a / (double)b;
      case TypeCode.Int16: return a / (short)b;
      case TypeCode.Int32: return a / (int)b;
      case TypeCode.Int64: return a / (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic==null ? a / ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rdiv__", a);
      case TypeCode.SByte: return a / (sbyte)b;
      case TypeCode.Single: return a / (float)b;
      case TypeCode.UInt16: return a / (ushort)b;
      case TypeCode.UInt32: return a / (uint)b;
      case TypeCode.UInt64: return a / (ulong)b;
    }
    return Ops.TypeError("invalid operand types for /: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public Complex Multiply(Complex a, object b)
  { if(b is Complex) return a * (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a * (bool)b ? 1 : 0;
      case TypeCode.Byte: return a * (byte)b;
      case TypeCode.Decimal:
        return a * ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a * (double)b;
      case TypeCode.Int16: return a * (short)b;
      case TypeCode.Int32: return a * (int)b;
      case TypeCode.Int64: return a * (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic==null ? a * ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rmul__", a);
      case TypeCode.SByte: return a * (sbyte)b;
      case TypeCode.Single: return a * (float)b;
      case TypeCode.UInt16: return a * (ushort)b;
      case TypeCode.UInt32: return a * (uint)b;
      case TypeCode.UInt64: return a * (ulong)b;
    }
    return Ops.TypeError("invalid operand types for *: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public bool NonZero(Complex a) { return a.real!=0 && a.imag!=0; }

  public Complex Power(Complex a, object b)
  { if(b is Complex) return a.pow((Complex)b);

    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a.pow((bool)b ? 1 : 0);
      case TypeCode.Byte: return a.pow((byte)b);
      case TypeCode.Decimal:
        return a.pow(((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a.pow((double)b);
      case TypeCode.Int16: return a.pow((short)b);
      case TypeCode.Int32: return a.pow((int)b);
      case TypeCode.Int64: return a.pow((long)b);
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic==null ? a.pow(ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.Invoke(b, "__rpow__", a);
      case TypeCode.SByte: return a.pow((sbyte)b);
      case TypeCode.Single: return a.pow((float)b);
      case TypeCode.UInt16: return a.pow((ushort)b);
      case TypeCode.UInt32: return a.pow((uint)b);
      case TypeCode.UInt64: return a.pow((ulong)b);
    }
    return Ops.TypeError("invalid operand types for **: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public Complex Subtract(Complex a, object b)
  { if(b is Complex) return a - (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a - (bool)b ? 1 : 0;
      case TypeCode.Byte: return a - (byte)b;
      case TypeCode.Decimal:
        return a - ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a - (double)b;
      case TypeCode.Int16: return a - (short)b;
      case TypeCode.Int32: return a - (int)b;
      case TypeCode.Int64: return a - (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic==null ? a - ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo)
                        : Ops.Invoke(b, "__rsub__", a);
      case TypeCode.SByte: return a - (sbyte)b;
      case TypeCode.Single: return a - (float)b;
      case TypeCode.UInt16: return a - (ushort)b;
      case TypeCode.UInt32: return a - (uint)b;
      case TypeCode.UInt64: return a - (ulong)b;
    }
    return Ops.TypeError("invalid operand types for -: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }
}

} // namespace Boa.Runtime
