using System;
using System.Globalization;

namespace Boa.Runtime
{

public sealed class IntOps
{ IntOps() { }

  public static object Add(int a, object b)
  { try
    { if(b is int) return checked(a+(int)b);
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a+1) : a;
        case TypeCode.Byte: return checked(a + (byte)b);
        case TypeCode.Char: return a + (char)b; // TODO: see whether this should return int or char
        case TypeCode.Decimal: return a + (Decimal)b;
        case TypeCode.Double: return a + (double)b;
        case TypeCode.Int16: return checked(a + (short)b);
        case TypeCode.Int32: return checked(a + (int)b); // TODO: add these elsewhere
        case TypeCode.Int64: return checked(a + (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b + a;
          IConvertible ic = b as IConvertible;
          return ic!=null ? checked(a + ic.ToInt32(NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__radd__", a);
        case TypeCode.SByte: return checked(a + (sbyte)b);
        case TypeCode.Single: return a + (float)b;
        case TypeCode.UInt16: return checked(a + (ushort)b);
        case TypeCode.UInt32: return checked(a + (uint)b);
        case TypeCode.UInt64: return checked(a + (ulong)b);
      }
      throw Ops.TypeError("invalid operand types for +: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return LongOps.Add(a, b); }
  }

  public static object BitwiseAnd(int a, object b)
  { if(b is int) return a&(int)b;
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
        long lv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        return lv>int.MaxValue || lv<int.MinValue ? lv&a : (int)lv&a;
      case TypeCode.SByte: return a & (sbyte)b;
      case TypeCode.UInt16: return a & (ushort)b;
      case TypeCode.UInt32: return a & (uint)b;
      case TypeCode.UInt64: return a & (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for &: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseOr(int a, object b)
  { if(b is int) return a|(int)b;
    switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a|1 : a;
      case TypeCode.Byte: return a | (byte)b;
      case TypeCode.Int16: return a | (short)b;
      case TypeCode.Int32: return a | (int)b;
      case TypeCode.Int64: return a | (long)b;
      case TypeCode.Object:
        if(b is Integer) return (Integer)b | a;
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__ror__", a);
        long lv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        return lv>int.MaxValue || lv<int.MinValue ? lv|a : (int)lv|a;
      case TypeCode.SByte: return a | (sbyte)b;
      case TypeCode.UInt16: return a | (ushort)b;
      case TypeCode.UInt32: return a | (uint)b;
      case TypeCode.UInt64: return a | (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for |: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object BitwiseXor(int a, object b)
  { if(b is int) return a^(int)b;
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
        long lv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        return lv>int.MaxValue || lv<int.MinValue ? lv^a : (int)lv^a;
      case TypeCode.SByte: return a ^ (sbyte)b;
      case TypeCode.UInt16: return a ^ (ushort)b;
      case TypeCode.UInt32: return a ^ (uint)b;
      case TypeCode.UInt64: return a ^ (ulong)b;
    }
    throw Ops.TypeError("invalid operand types for ^: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
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
      case TypeCode.Int32: return a - (int)b;
      case TypeCode.Int64: return (int)(a - (long)b);
      case TypeCode.Object:
        if(b is Integer)
        { Integer v = (Integer)b;
          return v>a ? -1 : v<a ? 1 : 0;
        }
        IConvertible ic = b as IConvertible;
        return ic==null ? (int)(a - ic.ToInt64(NumberFormatInfo.InvariantInfo))
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
    throw Ops.TypeError("cannot compare '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object FloorDivide(int a, object b)
  { int bv;
    if(b is int) bv = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: bv = (bool)b ? 1 : 0; break;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Double: return Math.Floor(a/(double)b);
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int64:
      { long lv = (long)b;
        if(lv<0)
        { if(a<0 && lv<a) return 0;
          else if(a>=0 && lv<=-a) return -1;
        }
        else if(a>=0 && lv>a/2) return 1;
        else if(a<0 && lv>=-(long)a) return -1;
        bv = (int)lv;
        break;
      }
      case TypeCode.Object:
        if(b is Integer)
        { Integer iv = (Integer)b;
          if(iv.IsNegative)
          { if(a<0 && iv<a) return 0;
            else if(a>=0 && iv<=-a) return -1;
          }
          else if(a>=0 && iv>a/2) return 1;
          else if(a<0 && iv>=-(long)a) return -1;
          bv = iv.ToInt32();
        }
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rfloordiv__", a);
        long lv = ic.ToInt64(NumberFormatInfo.InvariantInfo);
        if(lv>int.MaxValue || lv<int.MinValue) return LongOps.FloorDivide(a, b);
        bv = (int)lv;
        break;
      case TypeCode.SByte: bv=(sbyte)b; break;
      case TypeCode.Single: return Math.Floor(a/(float)b);
      case TypeCode.UInt16: bv = (ushort)b; break;
      case TypeCode.UInt32:
      { uint ui = (uint)b;
        if(a>=0 && ui>(uint)a/2) return 1;
        else if(a<0 && ui>=(uint)-(long)a) return -1;
        else bv = (int)ui;
        break;
      }
      case TypeCode.UInt64:
      { ulong ul = (ulong)b;
        if(a>=0 && ul>(uint)a/2) return 1;
        else if(a<0 && ul>=(ulong)-(long)a) return -1;
        else bv = (int)ul;
        break;
      }
      default:
        throw Ops.TypeError("invalid operand types for //: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    if(bv==0) throw Ops.DivideByZeroError("floor division by zero");
    return (a<0 ? (a-bv+Math.Sign(bv)) : a) / bv;
  }

  public static object LeftShift(int a, object b)
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
      case TypeCode.SByte: shift = (sbyte)b;
      case TypeCode.UInt16: shift = (ushort)b;
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
    if(shift>31) return LongOps.LeftShift(a, shift);
    int res = a << shift;
    return res<a || (a&0x80000000) != (ires&0x80000000) ? LongOps.LeftShift(a, shift) : res;
  }

  public static object Modulus(int a, object b)
  { int bv;
    if(b is int) bv = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: bv = (bool)b ? 1 : 0; break;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int32: bv = (int)b; break;
      case TypeCode.Int64: int64:
      { long lv = (long)b;
        if(lv<0)
        { if(a<0 && lv<a) return a;
          else if(a>=0 && lv<=-a) return lv+a;
        }
        else if(a>=0 && lv>a) return a;
        else if(a<0 && lv>=-(long)a) return lv+a;
        bv = (int)lv;
        break;
      }
      case TypeCode.Object:
        if(b is Integer)
        { Integer iv = (Integer)b;
          if(iv.IsNegative)
          { if(a<0 && iv<a) return a;
            else if(a>=0 && iv<=-a) return iv+a;
          }
          else if(a>=0 && iv>a) return a;
          else if(a<0 && iv>=-(long)a) return iv+a;
          bv = iv.ToInt32();
        }
        IConvertible ic = b as IConvertible;
        if(ic!=null) { b = ic.ToInt64(NumberFormatInfo.InvariantInfo); goto int64; }
        return Ops.Invoke(b, "__rmod__", a);
      case TypeCode.SByte: bv = (sbyte)b; break;
      case TypeCode.UInt16: bv = (ushort)b; break;
      case TypeCode.UInt32:
      { uint ui = (uint)b;
        if(a>=0 && ui>(uint)a) return a;
        else if(a<0 && ui>=-(long)a) return ui+a;
        else bv = (int)ui;
        break;
      }
      case TypeCode.UInt64:
      { ulong ul = (ulong)b;
        if(a>=0 && ul>(uint)a) return a;
        else if(a<0 && ul>=(ulong)-(long)a) return ul+a;
        else bv = (int)ul;
        break;
      }
      default:
        throw Ops.TypeError("invalid operand types for %: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    if(bv==0) throw Ops.DivideByZeroError("modulus by zero");
    return a%b;
  }
  
  public static object Multiply(int a, object b)
  { try
    { if(b is int) return checked(a*(int)b);
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a*1) : a;
        case TypeCode.Byte: return checked(a * (byte)b);
        case TypeCode.Decimal: return a * (Decimal)b;
        case TypeCode.Double: return a * (double)b;
        case TypeCode.Int16: return checked(a * (short)b);
        case TypeCode.Int64: return checked(a * (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b * a;
          IConvertible ic = b as IConvertible;
          return ic!=null ? checked(a * ic.ToInt32(NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__rmul__", a);
        case TypeCode.SByte: return checked(a * (sbyte)b);
        case TypeCode.Single: return a * (float)b;
        case TypeCode.UInt16: return checked(a * (ushort)b);
        case TypeCode.UInt32: return checked(a * (uint)b);
        case TypeCode.UInt64: return checked(a * (ulong)b);
      }
      throw Ops.TypeError("invalid operand types for *: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return LongOps.Multiply(a, b); }
  }

  public static object Power(int a, object b)
  { int bv;
    if(b is int) bv = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: return (bool)b ? a : a<0 ? -1 : 1;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Decimal: return Math.Pow(a, ((IConvertible)b).ToDouble(NumberFormatInfo.InvariantInfo));
      case TypeCode.Double: return Math.Pow(a, (double)b);
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int32: bv = (int)b; break;
      case TypeCode.Int64: int64:
      { long v = (long)b;
        if(v<0 || v>int.MaxValue) return new IntegerOps(new Integer(a), b);
        bv = (int)v;
        break;
      }
      case TypeCode.Object:
        if(b is Integer)
        { Integer iv = (Integer)b;
          if(iv.IsNegative || iv>int.MaxValue) return new Integer(a).Pow(iv);
          bv = iv.ToInt32();
        }
        IConvertible ic = b as IConvertible;
        if(ic!=null) { b = ic.ToInt64(NumberFormatInfo.InvariantInfo); goto int64; }
        return Ops.Invoke(b, "__rpow__", a);
      case TypeCode.SByte: bv = (sbyte)b; break;
      case TypeCode.Single: return Math.Pow(a, (float)b);
      case TypeCode.UInt16: bv = (ushort)b; break;
      case TypeCode.UInt32:
      { uint v = (uint)b;
        if(v>(uint)int.MaxValue) return new IntegerOps(new Integer(a), b);
        bv = (int)ui;
        break;
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)b;
        if(v>(uint)int.MaxValue) return new IntegerOps(new Integer(a), b);
        bv = (int)v;
        break;
      }
      default:
        throw Ops.TypeError("invalid operand types for **: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }

    if(bv<0) return FloatOps(a, bv);
    try
    { int ix=1;
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
		catch(OverflowException) { return new IntegerOps(new Integer(a), b); }
  }

  public static object PowerMod(int a, object b, object c)
  { int mod = Ops.ToInt(o);
    if(mod==0) throw Ops.DivideByZeroError("ternary pow(): modulus by zero");

    object pow = Power(a, b);
    if(pow is int) return (int)pow % mod;
    if(pow is Integer) return (Integer)pow % mod;
    throw Ops.TypeError("ternary pow() requires that the base and exponent be integers");
  }

  public static object Subtract(int a, object b)
  { try
    { if(b is int) return checked(a-(int)b); // TODO: make sure this is worthwhile
      switch(Convert.GetTypeCode(b))
      { case TypeCode.Boolean: return (bool)b ? checked(a-1) : a;
        case TypeCode.Byte: return checked(a - (byte)b);
        case TypeCode.Char: return a - (char)b; // TODO: see whether this should return int or char
        case TypeCode.Decimal: return a - (Decimal)b;
        case TypeCode.Double: return a - (double)b;
        case TypeCode.Int16: return checked(a - (short)b);
        case TypeCode.Int64: return checked(a - (long)b);
        case TypeCode.Object:
          if(b is Integer) return (Integer)b - a;
          IConvertible ic = b as IConvertible;
          return ic!=null ? checked(a - ic.ToInt32(NumberFormatInfo.InvariantInfo))
                          : Ops.Invoke(b, "__rsub__", a);
        case TypeCode.SByte: return checked(a - (sbyte)b);
        case TypeCode.Single: return a - (float)b;
        case TypeCode.UInt16: return checked(a - (ushort)b);
        case TypeCode.UInt32: return checked(a - (uint)b);
        case TypeCode.UInt64: return checked(a - (ulong)b);
      }
      throw Ops.TypeError("invalid operand types for -: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }
    catch(OverflowException) { return LongOps.Subtract(a, b); }
  }

  public static object RightShift(int a, object b)
  { int shift;
    if(b is int) shift = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: if((bool)b) shift=1; else return a; break;
      case TypeCode.Byte: shift = (byte)b; break;
      case TypeCode.Int16: shift = (short)b; break;
      case TypeCode.Int64:
      { long lv = (long)b;
        if(lv>int.MaxValue || lv<int.MinValue) throw Ops.OverflowError("long int too large to convert to int");
        shift = (int)lv;
        break;
      }
      case TypeCode.Object:
        if(b is Integer) shift = ((Integer)b).ToInt32();
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rrshift__", a);
        shift = ic.ToInt32(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: shift = (sbyte)b;
      case TypeCode.UInt16: shift = (ushort)b;
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
      default: throw Ops.TypeError("invalid operand types for >>: '{0}' and '{1}'", Ops.TypeName(a), Ops.TypeName(b));
    }

    if(shift<0) throw Ops.ValueError("negative shift count");
    return shift>31 ? 0 : a>>shift;
  }
}

} // namespace Boa.Runtime