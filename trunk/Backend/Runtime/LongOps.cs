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
using System.Globalization;

namespace Boa.Runtime
{

public sealed class LongOps
{ LongOps() { }

  public static object Add(long a, object b)
  { try
    { if(b is long) return checked(a+(long)b); // TODO: make sure this is worthwhile
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a+1) : a;
        case TypeCode.Byte: return checked(a + (byte)b);
        case TypeCode.Char: return a + (char)b; // TODO: see whether this should return int or char
        case TypeCode.Decimal: return a + (Decimal)b;
        case TypeCode.Double: return a + (double)b;
        case TypeCode.Empty: return 1;
        case TypeCode.Int16: return checked(a + (short)b);
        case TypeCode.Int32: return checked(a + (int)b);
        case TypeCode.Int64: return checked(a + (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b + a;
          IConvertible ic = b as IConvertible;
          return ic!=null ? checked(a + ic.ToInt64(NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__radd__", a);
        case TypeCode.SByte: return checked(a + (sbyte)b);
        case TypeCode.Single: return a + (float)b;
        case TypeCode.UInt16: return checked(a + (ushort)b);
        case TypeCode.UInt32: return checked(a + (uint)b);
        case TypeCode.UInt64:
        { ulong bv = (ulong)b;
          if(bv>long.MaxValue) return new Integer(a)+bv;
          return checked(a + (long)b);
        }
      }
      throw Ops.TypeError("invalid operand types for +: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return IntegerOps.Add(new Integer(a), b); }
  }

  public static object BitwiseAnd(long a, object b)
  { if(b is long) return a&(long)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a&1 : 0;
      case TypeCode.Byte: return a & (byte)b;
      case TypeCode.Int16: return a & (short)b;
      case TypeCode.Int32: return a & (int)b;
      case TypeCode.Int64: return a & (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b & a;
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rand__", a);
        return a & ic.ToInt64(NumberFormatInfo.InvariantInfo);
      case TypeCode.SByte: return a & (sbyte)b;
      case TypeCode.UInt16: return a & (ushort)b;
      case TypeCode.UInt32: return a & (uint)b;
      case TypeCode.UInt64: return (ulong)a & (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for &: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseOr(long a, object b)
  { if(b is long) return a|(long)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a|1 : a;
      case TypeCode.Byte: return a | (byte)b;
      case TypeCode.Int16: return a | (ushort)(short)b;
      case TypeCode.Int32: return a | (uint)(int)b;
      case TypeCode.Int64: return a | (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b | a;
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__ror__", a);
        return a | ic.ToInt64(NumberFormatInfo.InvariantInfo);
      case TypeCode.SByte: return a | (byte)(sbyte)b;
      case TypeCode.UInt16: return a | (ushort)b;
      case TypeCode.UInt32: return a | (uint)b;
      case TypeCode.UInt64: return (ulong)a | (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for |: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseXor(long a, object b)
  { if(b is long) return a^(long)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a^1 : a;
      case TypeCode.Byte: return a ^ (byte)b;
      case TypeCode.Int16: return a ^ (short)b;
      case TypeCode.Int32: return a ^ (int)b;
      case TypeCode.Int64: return a ^ (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b ^ a;
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rxor__", a);
        return a ^ ic.ToInt64(NumberFormatInfo.InvariantInfo);
      case TypeCode.SByte: return a ^ (sbyte)b;
      case TypeCode.UInt16: return a ^ (ushort)b;
      case TypeCode.UInt32: return a ^ (uint)b;
      case TypeCode.UInt64: return (ulong)a ^ (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for ^: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static int Compare(long a, object b)
  { if(b is long) return (int)(a-(long)b);
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (int)((bool)b ? a-1 : a);
      case TypeCode.Byte: return (int)(a - (byte)b);
      case TypeCode.Char: return (int)(a - (char)b);
      case TypeCode.Decimal:
      { Decimal v = (Decimal)b;
        return a<v ? -1 : a>v ? 1 : 0;
      }
      case TypeCode.Double:
      { double av=a, bv = (double)b;
        return av<bv ? -1 : av>bv ? 1 : 0;
      }
      case TypeCode.Empty: return 1;
      case TypeCode.Int16: return (int)(a - (short)b);
      case TypeCode.Int32: return (int)(a - (int)b);
      case TypeCode.Int64: return (int)(a - (long)b);
      case TypeCode.Object:
        if(b is Integer)
        { Integer v = (Integer)b;
          return v>a ? -1 : v<a ? 1 : 0;
        }
        IConvertible ic = b as IConvertible;
        return ic==null ? (int)(a - ic.ToInt64(NumberFormatInfo.InvariantInfo))
                        : -Ops.ToInt(Ops.Invoke(b, "__cmp__", a));
      case TypeCode.SByte: return (int)(a - (sbyte)b);
      case TypeCode.Single:
      { float av=a, bv=(float)b;
        return av<bv ? -1 : av>bv ? 1 : 0;
      }
      case TypeCode.String: return -1;
      case TypeCode.UInt16: return (int)(a - (ushort)b);
      case TypeCode.UInt32: return a<0 ? -1 : (int)((ulong)a - (uint)b);
      case TypeCode.UInt64: return a<0 ? -1 : (int)((ulong)a - (ulong)b);
    }
    throw Ops.TypeError("cannot compare '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object FloorDivide(long a, object b)
  { long bv;
    if(b is long) bv = (long)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: bv = (bool)b ? 1 : 0; break;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Double: return Math.Floor(a/(double)b);
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int32: bv = (int)b; break;
      case TypeCode.Int64: bv = (long)b; break;
      case TypeCode.Object:
        if(b is Integer)
        { Integer iv = (Integer)b;
          if(iv.Sign<0)
          { if(a<0 && iv<a) return 0;
            else if(a>=0 && iv<=-a) return -1;
          }
          else if(a>=0 && iv>a/2) return 1;
          else if(a<0 && iv>=-(long)a) return -1;
          bv = iv.ToInt64();
        }
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rfloordiv__", a);
        bv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: bv=(sbyte)b; break;
      case TypeCode.Single: return Math.Floor(a/(float)b);
      case TypeCode.UInt16: bv = (ushort)b; break;
      case TypeCode.UInt32: bv = (uint)b; break;
      case TypeCode.UInt64:
      { ulong ul = (ulong)b;
        if(a>=0 && ul>(uint)a/2) return 1;
        else if(a<0 && ul>=(ulong)-a) return -1;
        else bv = (long)ul;
        break;
      }
      default:
        throw Ops.TypeError("invalid operand types for //: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    if(bv==0) throw Ops.DivideByZeroError("floor division by zero");
    return (a<0 ? (a-bv+Math.Sign(bv)) : a) / bv;
  }

  public static object LeftShift(long a, object b)
  { int shift;
    if(b is int) shift = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: if((bool)b) shift=1; else return a; break;
      case TypeCode.Byte: shift = (byte)b; break;
      case TypeCode.Int16: shift = (short)b; break;
      case TypeCode.Int32: shift = (int)b; break;
      case TypeCode.Int64:
      { long lv = (long)b;
        if(lv>int.MaxValue || lv<int.MinValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)lv;
        break;
      }
      case TypeCode.Object:
        if(b is Integer) shift = ((Integer)b).ToInt32();
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rlshift__", a);
        shift = ic.ToInt32(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: shift = (sbyte)b; break;
      case TypeCode.UInt16: shift = (ushort)b; break;
      case TypeCode.UInt32:
      { uint ui = (uint)b;
        if(ui>int.MaxValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)ui;
        break;
      }
      case TypeCode.UInt64:
      { ulong ul = (uint)b;
        if(ul>int.MaxValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)ul;
        break;
      }
      default: throw Ops.TypeError("invalid operand types for <<: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }

    if(shift<0) throw Ops.ValueError("negative shift count");
    if(shift>63) return new Integer(a)<<shift;
    long res = a << shift;
    if(res<a || ((ulong)a&0x8000000000000000) != ((ulong)res&0x8000000000000000)) return new Integer(a)<<shift;
    else return res;
  }

  public static object Modulus(long a, object b)
  { long bv;
    if(b is int) bv = (int)b; // TODO: is int most likely?
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: bv = (bool)b ? 1 : 0; break;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Decimal: return Math.IEEERemainder(a, ((IConvertible)b).ToDouble(NumberFormatInfo.InvariantInfo));
      case TypeCode.Double: return Math.IEEERemainder(a, (double)b);
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int32: bv = (int)b; break;
      case TypeCode.Int64: bv = (long)b; break;
      case TypeCode.Object:
        if(b is Integer)
        { Integer iv = (Integer)b;
          if(iv.Sign<0)
          { if(a<0 && iv<a) return a;
            else if(a>=0 && iv<=-a) return iv+a;
          }
          else if(a>=0 && iv>a) return a;
          else if(a<0 && -iv<=a) return iv+a;
          bv = iv.ToInt64();
        }
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rmod__", a);
        bv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: bv = (sbyte)b; break;
      case TypeCode.Single: return Math.IEEERemainder(a, (float)b);
      case TypeCode.UInt16: bv = (ushort)b; break;
      case TypeCode.UInt32: bv = (uint)b; break;
      case TypeCode.UInt64:
      { ulong ul = (ulong)b;
        if(a>=0 && ul>(ulong)a) return a;
        else if(a<0 && ul>=(a==long.MinValue ? unchecked((ulong)-long.MinValue) : (ulong)-a)) return ul+(ulong)a;
        else bv = (long)ul;
        break;
      }
      default:
        throw Ops.TypeError("invalid operand types for %: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    if(bv==0) throw Ops.DivideByZeroError("modulus by zero");
    return a%bv;
  }

  public static object Multiply(long a, object b)
  { try
    { if(b is int) return checked(a*(int)b); // TODO: is 'int' most common?
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a*1) : a;
        case TypeCode.Byte: return checked(a * (byte)b);
        case TypeCode.Decimal: return a * (Decimal)b;
        case TypeCode.Double: return a * (double)b;
        case TypeCode.Int16: return checked(a * (short)b);
        case TypeCode.Int32: return checked(a * (int)b);
        case TypeCode.Int64: return checked(a * (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b * a;
          IConvertible ic = b as IConvertible;
          return ic!=null ? checked(a * ic.ToInt64(NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__rmul__", a);
        case TypeCode.SByte: return checked(a * (sbyte)b);
        case TypeCode.Single: return a * (float)b;
        case TypeCode.UInt16: return checked(a * (ushort)b);
        case TypeCode.UInt32: return checked(a * (uint)b);
        case TypeCode.UInt64:
        { ulong ul = (ulong)b;
          if(ul>(ulong)int.MaxValue) return new Integer(a)*ul;
          return checked(a * (long)ul);
        }
      }
      throw Ops.TypeError("invalid operand types for *: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return IntegerOps.Multiply(new Integer(a), b); }
  }

  public static object Power(long a, object b)
  { long bv;
    if(b is int) bv = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a : a<0 ? -1 : 1;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Decimal: return Math.Pow(a, ((IConvertible)b).ToDouble(NumberFormatInfo.InvariantInfo));
      case TypeCode.Double: return Math.Pow(a, (double)b);
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int32: bv = (int)b; break;
      case TypeCode.Int64: bv = (long)b; break;
      case TypeCode.Object:
        if(b is Integer)
        { Integer iv = (Integer)b;
          if(iv<0 || iv>long.MaxValue) return new Integer(a).Pow(iv);
          bv = iv.ToInt64(null);
        }
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rpow__", a);
        bv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: bv = (sbyte)b; break;
      case TypeCode.Single: return Math.Pow(a, (float)b);
      case TypeCode.UInt16: bv = (ushort)b; break;
      case TypeCode.UInt32: bv = (uint)b; break;
      case TypeCode.UInt64:
      { ulong v = (ulong)b;
        if(v>(ulong)long.MaxValue) return IntegerOps.Power(new Integer(a), b);
        bv = (long)v;
        break;
      }
      default:
        throw Ops.TypeError("invalid operand types for **: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }

    if(bv<0) return Math.Pow(a, bv);
    try
    { long ix=1;
	    while(bv > 0)
	    { if((bv&1)!=0)
	      { if(a==0) break;
	        checked { ix = ix*a; }
		    }
	 	    bv >>= 1;
	      if(bv==0) break;
	 	    checked { a *= a;	}
		  }
		  return ix;
		}
		catch(OverflowException) { return IntegerOps.Power(new Integer(a), b); }
  }

  public static object PowerMod(long a, object b, object o)
  { long mod = Ops.ToLong(o);
    if(mod==0) throw Ops.DivideByZeroError("ternary pow(): modulus by zero");

    object pow = Power(a, b);
    if(pow is long) return (long)pow % mod;
    if(pow is Integer) return (Integer)pow % mod;
    throw Ops.TypeError("ternary pow() requires that the base and exponent be integers");
  }

  public static object Subtract(long a, object b)
  { try
    { if(b is long) return checked(a-(long)b); // TODO: make sure this is worthwhile
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a-1) : a;
        case TypeCode.Byte: return checked(a - (byte)b);
        case TypeCode.Char: return a - (char)b; // TODO: see whether this should return int or char
        case TypeCode.Decimal: return a - (Decimal)b;
        case TypeCode.Double: return a - (double)b;
        case TypeCode.Int16: return checked(a - (short)b);
        case TypeCode.Int32: return checked(a - (int)b);
        case TypeCode.Int64: return checked(a - (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b - a;
          IConvertible ic = b as IConvertible;
          return ic!=null ? checked(a - ic.ToInt64(NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__rsub__", a);
        case TypeCode.SByte: return checked(a - (sbyte)b);
        case TypeCode.Single: return a - (float)b;
        case TypeCode.UInt16: return checked(a - (ushort)b);
        case TypeCode.UInt32: return checked(a - (uint)b);
        case TypeCode.UInt64:
        { ulong ul = (ulong)b;
          if(ul>(ulong)long.MaxValue) return new Integer(a)-ul;
          return checked(a - (long)ul);
        }
      }
      throw Ops.TypeError("invalid operand types for -: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return IntegerOps.Subtract(new Integer(a), b); }
  }

  public static object RightShift(long a, object b)
  { int shift;
    if(b is int) shift = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: if((bool)b) shift=1; else return a; break;
      case TypeCode.Byte: shift = (byte)b; break;
      case TypeCode.Int16: shift = (short)b; break;
      case TypeCode.Int32: shift = (int)b; break;
      case TypeCode.Int64:
      { long lv = (long)b;
        if(lv>int.MaxValue || lv<int.MinValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)lv;
        break;
      }
      case TypeCode.Object:
        if(b is Integer) shift = ((Integer)b).ToInt32();
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rlshift__", a);
        shift = ic.ToInt32(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: shift = (sbyte)b; break;
      case TypeCode.UInt16: shift = (ushort)b; break;
      case TypeCode.UInt32:
      { uint ui = (uint)b;
        if(ui>int.MaxValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)ui;
        break;
      }
      case TypeCode.UInt64:
      { ulong ul = (uint)b;
        if(ul>int.MaxValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)ul;
        break;
      }
      default: throw Ops.TypeError("invalid operand types for >>return shift>31 ? 0 : a>>shift;: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }

    if(shift<0) throw Ops.ValueError("negative shift count");
    return shift>63 ? 0 : a>>shift;
  }
}

} // namespace Boa.Runtime