/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

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

namespace Boa.Runtime
{

public sealed class ComplexOps
{ ComplexOps() { }

  public static object Add(Complex a, object b)
  { if(b is Complex) return a + (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a+1 : a;
      case TypeCode.Byte: return a + (byte)b;
      case TypeCode.Decimal:
        return a + ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a + (double)b;
      case TypeCode.Int16: return a + (short)b;
      case TypeCode.Int32: return a + (int)b;
      case TypeCode.Int64: return a + (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic!=null ? (object)(a + ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.Invoke(b, "__radd__", a);
      case TypeCode.SByte: return a + (sbyte)b;
      case TypeCode.Single: return a + (float)b;
      case TypeCode.UInt16: return a + (ushort)b;
      case TypeCode.UInt32: return a + (uint)b;
      case TypeCode.UInt64: return a + (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for +: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static int Compare(Complex a, object b)
  { throw Ops.TypeError("cannot compare complex numbers except for equality/inequality");
  }

  public static object Divide(Complex a, object b)
  { if(b is Complex) return a / (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a / ((bool)b ? 1 : 0);
      case TypeCode.Byte: return a / (byte)b;
      case TypeCode.Decimal:
        return a / ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a / (double)b;
      case TypeCode.Int16: return a / (short)b;
      case TypeCode.Int32: return a / (int)b;
      case TypeCode.Int64: return a / (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        object ret;
        return ic!=null ? (object)(a / ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.TryInvoke(b, "__rtruediv__", out ret, a) ? ret : Ops.Invoke(b, "__rdiv__", a);
      case TypeCode.SByte: return a / (sbyte)b;
      case TypeCode.Single: return a / (float)b;
      case TypeCode.UInt16: return a / (ushort)b;
      case TypeCode.UInt32: return a / (uint)b;
      case TypeCode.UInt64: return a / (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for /: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object Multiply(Complex a, object b)
  { if(b is Complex) return a * (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a : new Complex(0);
      case TypeCode.Byte: return a * (byte)b;
      case TypeCode.Decimal:
        return a * ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a * (double)b;
      case TypeCode.Int16: return a * (short)b;
      case TypeCode.Int32: return a * (int)b;
      case TypeCode.Int64: return a * (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic!=null ? (object)(a * ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.Invoke(b, "__rmul__", a);
      case TypeCode.SByte: return a * (sbyte)b;
      case TypeCode.Single: return a * (float)b;
      case TypeCode.UInt16: return a * (ushort)b;
      case TypeCode.UInt32: return a * (uint)b;
      case TypeCode.UInt64: return a * (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for *: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static bool NonZero(Complex a) { return a.real!=0 && a.imag!=0; }

  public static object Power(Complex a, object b)
  { if(b is Complex) return a.Pow((Complex)b);

    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return a.Pow((bool)b ? 1 : 0);
      case TypeCode.Byte: return a.Pow((byte)b);
      case TypeCode.Decimal:
        return a.Pow(((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo));
      case TypeCode.Double: return a.Pow((double)b);
      case TypeCode.Int16: return a.Pow((short)b);
      case TypeCode.Int32: return a.Pow((int)b);
      case TypeCode.Int64: return a.Pow((long)b);
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic!=null ? a.Pow(ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.Invoke(b, "__rpow__", a);
      case TypeCode.SByte: return a.Pow((sbyte)b);
      case TypeCode.Single: return a.Pow((float)b);
      case TypeCode.UInt16: return a.Pow((ushort)b);
      case TypeCode.UInt32: return a.Pow((uint)b);
      case TypeCode.UInt64: return a.Pow((ulong)b);
    }
    throw Ops.TypeError("invalid operand types for **: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }
  
  public static object PowerMod(Complex a, object b, object c)
  { throw Ops.TypeError("complex modulus not supported");
  }

  public static object Subtract(Complex a, object b)
  { if(b is Complex) return a - (Complex)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a-1 : a;
      case TypeCode.Byte: return a - (byte)b;
      case TypeCode.Decimal:
        return a - ((IConvertible)b).ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo);
      case TypeCode.Double: return a - (double)b;
      case TypeCode.Int16: return a - (short)b;
      case TypeCode.Int32: return a - (int)b;
      case TypeCode.Int64: return a - (long)b;
      case TypeCode.Object:
        IConvertible ic = b as IConvertible;
        return ic!=null ? (object)(a - ic.ToDouble(System.Globalization.NumberFormatInfo.InvariantInfo))
                        : Ops.Invoke(b, "__rsub__", a);
      case TypeCode.SByte: return a - (sbyte)b;
      case TypeCode.Single: return a - (float)b;
      case TypeCode.UInt16: return a - (ushort)b;
      case TypeCode.UInt32: return a - (uint)b;
      case TypeCode.UInt64: return a - (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for -: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }
}

} // namespace Boa.Runtime
