using System;
using System.Collections;
using System.Globalization;

namespace Boa.Runtime
{

public sealed class ArrayOps
{ ArrayOps() { }

  public static int Compare(object[] arr1, int len1, object[] arr2, int len2)
  { int len = Math.Min(len1, len2);
    for(int i=0; i<len; i++)
    { int c = Ops.Compare(arr1[i], arr2[i]);
      if(c!=0) return c;
    }
    return len1==len2 ? 0 : len1<len2 ? -1 : 1;
  }
  
  public static object Concat(object a, object b)
  { Array aa, ab, ret=null;
    if(a is Tuple)
    { Tuple ta = (Tuple)a, tb = b as Tuple;
      if(tb==null) goto badTypes;
      aa = ta.items;
      ab = tb.items;
    }
    else if(a is Array)
    { if(a.GetType()!=b.GetType()) goto badTypes;
      aa  = (Array)a;
      ab  = (Array)b;
      ret = Array.CreateInstance(aa.GetType().GetElementType(), aa.Length+ab.Length);
    }
    else
    { aa = ToArray(a);
      ab = ToArray(b);
    }
    if(ret==null) ret = new object[aa.Length+ab.Length];
    aa.CopyTo(ret, 0);
    ab.CopyTo(ret, aa.Length);
    return a is Tuple ? new Tuple((object[])ret) : a is Array ? ret : (object)new List((object[])ret, ret.Length);
    
    badTypes:
    throw Ops.TypeError("invalid operand types for sequence concatenation: '{0}' and '{1}'",
                        Ops.TypeName(a), Ops.TypeName(b));
  }

  public static object Multiply(object a, object b)
  { int bv;
    if(b is int) bv = (int)b;
    else switch(Convert.GetTypeCode(b))
    { case TypeCode.Boolean: if((bool)b) return a; else bv=0; break;
      case TypeCode.Byte: bv = (byte)b; break;
      case TypeCode.Int16: bv = (short)b; break;
      case TypeCode.Int32: bv = (int)b; break;
      case TypeCode.Int64:
      { long lv = (long)b;
        if(lv>int.MaxValue || lv<int.MinValue) throw Ops.OverflowError("long int too large to convert to int");
        bv = (int)lv;
        break;
      }
      case TypeCode.Object:
        if(b is Integer) bv = ((Integer)b).ToInt32();
        IConvertible ic = b as IConvertible;
        if(ic==null) return Ops.Invoke(b, "__rmul__", a);
        bv = ic.ToInt32(NumberFormatInfo.InvariantInfo);
        break;
      case TypeCode.SByte: bv = (sbyte)b;
      case TypeCode.UInt16: bv = (ushort)b;
      case TypeCode.UInt32:
      { uint ui = (uint)b;
        if(ui>int.MaxValue) throw Ops.OverflowError("long int too large to convert to int");
        bv = (int)ui;
        break;
      }
      case TypeCode.UInt64:
      { ulong ul = (uint)b;
        if(ul>int.MaxValue) throw Ops.OverflowError("long int too large to convert to int");
        bv = (int)ul;
        break;
      }
      default: throw Ops.TypeError("invalid operand types for sequence multiplication: '{0}' and '{1}'",
                                   Ops.TypeName(a), Ops.TypeName(b));
    }
    
    if(bv<0) throw Ops.ValueError("multiplier for sequence multiplication cannot be negative");
    if(bv==1) return a;

    Array source, dest;
    if(a is Array)
    { source = (Array)a;
      dest = Array.CreateInstance(source.GetType().GetElementType(), source.Length*bv);
    }
    else
    { source = ToArray(a);
      dest = new object[source.Length*bv];
    }

    int ai = source.Length, bvb=bv, mask=0;
    Array.Copy(source, dest, 0);
    while((bv>>=1) != 0) { Array.Copy(dest, 0, dest, ai, ai); ai += ai; mask=mask<<1 | 1; }
    bv = bvb&mask;
    if(bv>0) Array.Copy(dest, 0, dest, ai, (bvb&mask)*source.Length);

    return a is Tuple ? new Tuple((object[])dest) : a is Array ? dest : (object)new List((object[])dest, dest.Length);
  }
  
  public static string Repr(Array arr)
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append(arr.GetType().FullName);
    sb.Append('(');
    for(int i=0; i<arr.Length; i++)
    { if(i>0) sb.Append(", ");
      sb.Append(Ops.Repr(arr.GetValue(i)));
    }
    sb.Append(')');
    return sb.ToString();
  }
  
  public static object[] ToArray(object o)
  { object[] ret;
    if(o is Tuple) ret = ((Tuple)o).items;
    else if(o is ICollection)
    { ICollection ic = (ICollection)o;
      ret = new object[ic.Count];
      ic.CopyTo(ret, 0);
    }
    else if(o is ISequence)
    { ISequence s = (ISequence)o;
      ret = new object[s.__len__()];
      for(int i=0; i<ret.Length; i++) ret[i] = s.__getitem__(i);
    }
    else
    { ret = new object[Ops.ToInt(Ops.Invoke(o, "__len__"))];
      object getitem = Ops.GetAttr(o, "__getitem__");
      for(int i=0; i<ret.Length; i++) ret[i] = Ops.Call(getitem, i);
    }
    return ret;
  }
}

} // namespace Boa.Runtime