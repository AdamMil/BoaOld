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
using System.Collections;
using System.Globalization;

// TODO: investigate python's changes to the division operator
// TODO: should we allow integer operations on chars?
// TODO: implement all of http://docs.python.org/ref/specialnames.html
// TODO: double check all mathematical operations
// TODO: support floats in all integer operations as long as they are whole numbers
// TODO: implement a warning system
namespace Boa.Runtime
{

[Flags] public enum Conversion
{ Unsafe=1, Safe=3, Reference=5, Identity=7, None=8, Overflow=10, 
  Failure=8, Success=1
}

// TODO: make heavy use of regions in here
public sealed class Ops
{ Ops() { }

  public static readonly object Missing = "<Missing>";
  public static readonly object NotImplemented = "<NotImplemented>";
  public static readonly DefaultBoaComparer DefaultComparer = new DefaultBoaComparer();

  public class DefaultBoaComparer : IComparer
  { public int Compare(object x, object y) { return Ops.Compare(x, y); }
  }

  public static AssertionErrorException AssertionError(Boa.AST.Node node, string format, params object[] args)
  { return new AssertionErrorException(Source(node)+string.Format(format, args));
  }

  public static AttributeErrorException AttributeError(string format, params object[] args)
  { return new AttributeErrorException(string.Format(format, args));
  }

  public static object Add(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.Add((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return IntOps.Add((int)(byte)a, b);
      case TypeCode.Char:
        if(b is string) return (char)a+(string)b;
        break;
      case TypeCode.Decimal:
        if(b is Decimal) return (Decimal)a + (Decimal)b;
        break;
      case TypeCode.Double:  return FloatOps.Add((double)a, b);
      case TypeCode.Int16: return IntOps.Add((int)(short)a, b);
      case TypeCode.Int32: return IntOps.Add((int)a, b);
      case TypeCode.Int64: return LongOps.Add((long)a, b);
      case TypeCode.Object:
      { if(a is Integer) return IntegerOps.Add((Integer)a, b);
        if(a is Complex) return ComplexOps.Add((Complex)a, b);
        if(a is ICollection || a is ISequence) return ArrayOps.Concat(a, b);
        object ret;
        return TryInvoke(a, "__add__", out ret, b) ? ret : Invoke(b, "__radd__", a);
      }
      case TypeCode.SByte: return IntOps.Add((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Add((float)a, b);
      case TypeCode.String:
        if(b is string) return (string)a + (string)b;
        if(b is char) return (string)a + (char)b;
        break;
      case TypeCode.UInt16: return IntOps.Add((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.Add((int)v, b) : LongOps.Add((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.Add((long)v, b) : IntegerOps.Add(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for +: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static object BitwiseAnd(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean:
        if(b is bool) return FromBool((bool)a && (bool)b);
        return IntOps.BitwiseAnd((bool)a ? 1 : 0, b);
      case TypeCode.Byte:  return IntOps.BitwiseAnd((int)(byte)a, b);
      case TypeCode.Int16: return IntOps.BitwiseAnd((int)(short)a, b);
      case TypeCode.Int32: return IntOps.BitwiseAnd((int)a, b);
      case TypeCode.Int64: return LongOps.BitwiseAnd((long)a, b);
      case TypeCode.Object:
      { if(a is Integer) return IntegerOps.BitwiseAnd((Integer)a, b);
        object ret;
        return TryInvoke(a, "__and__", out ret, b) ? ret : Invoke(b, "__rand__", a);
      }
      case TypeCode.SByte: return IntOps.BitwiseAnd((int)(sbyte)a, b);
      case TypeCode.UInt16: return IntOps.BitwiseAnd((int)(short)a, b);
      case TypeCode.UInt32:
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.BitwiseAnd((int)v, b) : LongOps.BitwiseAnd((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.BitwiseAnd((long)v, b) : IntegerOps.BitwiseAnd(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for &: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static object BitwiseOr(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean:
        if(b is bool) return FromBool((bool)a || (bool)b);
        return IntOps.BitwiseOr((bool)a ? 1 : 0, b);
      case TypeCode.Byte:  return IntOps.BitwiseOr((int)(byte)a, b);
      case TypeCode.Int16: return IntOps.BitwiseOr((int)(short)a, b);
      case TypeCode.Int32: return IntOps.BitwiseOr((int)a, b);
      case TypeCode.Int64: return LongOps.BitwiseOr((long)a, b);
      case TypeCode.Object:
      { if(a is Integer) return IntegerOps.BitwiseOr((Integer)a, b);
        object ret;
        return TryInvoke(a, "__or___", out ret, b) ? ret : Invoke(b, "__ror___", a);
      }
      case TypeCode.SByte: return IntOps.BitwiseOr((int)(sbyte)a, b);
      case TypeCode.UInt16: return IntOps.BitwiseOr((int)(short)a, b);
      case TypeCode.UInt32:
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.BitwiseOr((int)v, b) : LongOps.BitwiseOr((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.BitwiseOr((long)v, b) : IntegerOps.BitwiseOr(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for |: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static object BitwiseXor(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean:
        if(b is bool) return FromBool((bool)a != (bool)b);
        return IntOps.BitwiseXor((bool)a ? 1 : 0, b);
      case TypeCode.Byte:  return IntOps.BitwiseXor((int)(byte)a, b);
      case TypeCode.Int16: return IntOps.BitwiseXor((int)(short)a, b);
      case TypeCode.Int32: return IntOps.BitwiseXor((int)a, b);
      case TypeCode.Int64: return LongOps.BitwiseXor((long)a, b);
      case TypeCode.Object:
      { if(a is Integer) return IntegerOps.BitwiseXor((Integer)a, b);
        object ret;
        return TryInvoke(a, "__xor__", out ret, b) ? ret : Invoke(b, "__rxor__", a);
      }
      case TypeCode.SByte: return IntOps.BitwiseXor((int)(sbyte)a, b);
      case TypeCode.UInt16: return IntOps.BitwiseXor((int)(short)a, b);
      case TypeCode.UInt32:
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.BitwiseXor((int)v, b) : LongOps.BitwiseXor((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.BitwiseXor((long)v, b) : IntegerOps.BitwiseXor(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for ^: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static object BitwiseNegate(object a)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return (bool)a ? -2 : -1;
      case TypeCode.Byte:  return ~(int)(byte)a;
      case TypeCode.Int16: return ~(int)(short)a;
      case TypeCode.Int32: return ~(int)a;
      case TypeCode.Int64: return ~(long)a;
      case TypeCode.Object: return a is Integer ? ~(Integer)a : Invoke(a, "__invert__");
      case TypeCode.SByte: return ~(int)(sbyte)a;
      case TypeCode.UInt16: return ~(int)(short)a;
      case TypeCode.UInt32:
      { uint v = (uint)a;
        return v<=int.MaxValue ? (object)~(int)v : (object)~(long)v;
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? (object)(~(long)v) : (object)(~new Integer(v));
      }
    }
    throw TypeError("unsupported operand type for ~: '{0}'", TypeName(a));
  }

  public static object Call(object func, params object[] args)
  { ICallable ic = func as ICallable;
    return ic==null ? Invoke(func, "__call__", args) : ic.Call(args);
  }
  
  public unsafe static object Call(object func, CallArg[] args)
  { int ai=0, pi=0, num=0;
    bool hasdict=false;

    for(; ai<args.Length; ai++)
      if(args[ai].Type==null) num++;
      else if(args[ai].Type==CallArg.ListType)
      { ICollection col = args[ai].Value as ICollection;
        if(col!=null) num += col.Count;
        else
        { ISequence seq = args[ai].Value as ISequence;
          num += seq==null ? Ops.ToInt(Ops.Invoke(args[ai].Value, "__len__")) : seq.__len__();
        }
      }
      else if(args[ai].Type is int) num += (int)args[ai].Type;
      else break;
    
    object[] positional = num==0 ? null : new object[num];
    for(int i=0; i<ai; i++)
      if(args[i].Type==null) positional[pi++] = args[i].Value;
      else if(args[i].Type==CallArg.ListType)
      { ICollection col = args[i].Value as ICollection;
        if(col!=null) { col.CopyTo(positional, pi); pi += col.Count; }
        else
        { IEnumerator e = Ops.GetEnumerator(args[i].Value);
          while(e.MoveNext()) positional[pi++] = e.Current;
        }
      }
      else if(args[i].Type is int)
      { object[] items = (object[])args[i].Value;
        items.CopyTo(positional, pi); pi += items.Length;
      }

    if(ai==args.Length) return Call(func, positional);
    
    num = 0;
    for(int i=ai; i<args.Length; i++)
      if(args[i].Type==CallArg.DictType)
      { IDictionary dict = args[i].Value as IDictionary;
        if(dict!=null) num += dict.Count;
        else
        { IMapping map = args[i].Value as IMapping;
          num += map==null ? Ops.ToInt(Ops.Invoke(args[i].Value, "__len__")) : map.__len__();
        }
        hasdict = true;
      }
      else num += ((object[])args[i].Type).Length;

    if(!hasdict) return Call(func, positional, (string[])args[ai].Value, (object[])args[ai].Type);

    string[] names = new string[num];
    object[] values = new object[num];
    pi = 0;
    for(; ai<args.Length; ai++)
      if(args[ai].Type!=CallArg.DictType)
      { string[] na = (string[])args[ai].Value;
        na.CopyTo(names, pi);
        ((object[])args[ai].Type).CopyTo(values, pi);
        pi += na.Length;
      }
      else
      { IDictionary dict = args[ai].Value as IDictionary;
        if(dict!=null)
          foreach(DictionaryEntry e in dict)
          { names[pi] = Ops.Str(e.Key);
            values[pi] = e.Value;
            pi++;
          }
        else
        { IMapping map = args[ai].Value as IMapping;
          if(map!=null)
          { IEnumerator e = map.iteritems();
            while(e.MoveNext())
            { Tuple tup = (Tuple)e.Current;
              names[pi] = Ops.Str(tup.items[0]);
              values[pi] = tup.items[1];
              pi++;
            }
          }
          else
          { object eo = Ops.Invoke(args[ai].Value, "itemiter");
            if(eo is IEnumerator)
            { IEnumerator e = (IEnumerator)eo;
              while(e.MoveNext())
              { Tuple tup = e.Current as Tuple;
                if(tup==null || tup.items.Length!=2)
                  throw TypeError("dict expansion: itemiter()'s iterator should return (key,value)");
                names[pi] = Ops.Str(tup.items[0]);
                values[pi] = tup.items[1];
                pi++;
              }
            }
            else
            { object next = Ops.GetAttr(eo, "next");
              try
              { while(true)
                { Tuple tup = Ops.Call(next) as Tuple;
                  if(tup==null || tup.items.Length!=2)
                    throw TypeError("dict expansion: itemiter()'s iterator should return (key,value)");
                  names[pi] = Ops.Str(tup.items[0]);
                  values[pi] = tup.items[1];
                  pi++;
                }
              }
              catch(StopIterationException) { }
            }
          }
        }
      }
    
    return Call(func, positional, names, values);
  }

  public static object Call(object func, object[] positional, string[] names, object[] values)
  { IFancyCallable ic = func as IFancyCallable;
    if(ic==null) throw Ops.NotImplementedError("This object does not support named arguments.");
    return ic.Call(positional, names, values);
  }
  
  public static object Call0(object func) { return Call(func, Misc.EmptyArray); }
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
    switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean:
        if(b is bool) return (bool)a ? (bool)b ? 0 : 1 : (bool)b ? -1 : 0;
        return IntOps.Compare((bool)a ? 1 : 0, b);
      case TypeCode.Byte: return IntOps.Compare((int)(byte)a, b);
      case TypeCode.Char: return IntOps.Compare((int)(char)a, b);
      case TypeCode.Decimal:
        if(b is Decimal) return ((Decimal)a).CompareTo(b);
        break;
      case TypeCode.Double: return FloatOps.Compare((double)a, b);
      case TypeCode.Empty: return b==null ? 0 : -1;
      case TypeCode.Int16: return IntOps.Compare((int)(short)a, b);
      case TypeCode.Int32: return IntOps.Compare((int)a, b);
      case TypeCode.Int64: return LongOps.Compare((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.Compare((Integer)a, b);
        if(a is Complex) return ComplexOps.Compare((Complex)a, b);
        if(a is ICollection || a is ISequence) return ArrayOps.Compare(a, b);
        object ret;
        return Ops.ToInt(TryInvoke(a, "__cmp__", out ret, b) ? ret : Invoke(b, "__cmp__", a));
      case TypeCode.SByte: return IntOps.Compare((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Compare((float)a, b);
      case TypeCode.String:
      { string sb = b as string;
        return sb==null ? "string".CompareTo(TypeName(b)) : ((string)a).CompareTo(sb);
      }
      case TypeCode.UInt16: return IntOps.Compare((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.Compare((int)v, b) : LongOps.Compare((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.Compare((long)v, b) : IntegerOps.Compare(new Integer(v), b);
      }
    }
    throw TypeError("can't compare '{0}' to '{1}'", TypeName(a), TypeName(b));
  }

  public static object ConvertTo(object o, Type type)
  { switch(ConvertTo(o==null ? null : o.GetType(), type))
    { case Conversion.Identity: case Conversion.Reference: return o;
      case Conversion.None: throw TypeError("object cannot be converted to '{0}'", type);
      default:
        if(type==typeof(bool)) return FromBool(IsTrue(o));
        try { return Convert.ChangeType(o, type); }
        catch(OverflowException) { throw ValueError("large value caused overflow"); }
    }
  }
  
  public static Conversion ConvertTo(Type from, Type to)
  { if(from==null)
      return !to.IsValueType ? Conversion.Reference : to==typeof(bool) ? Conversion.Safe : Conversion.None;
    else if(to==from) return Conversion.Identity;
    else if(to.IsAssignableFrom(from)) return Conversion.Reference;

    // TODO: check whether it's faster to use IndexOf() or our own loop
    // TODO: check whether it's possible to speed up this big block of checks up somehow
    // TODO: add support for Integer, Complex, and Decimal
    if(from.IsPrimitive && to.IsPrimitive)
    { if(from==typeof(int))    return IsIn(typeConv[4], to)   ? Conversion.Safe : Conversion.Unsafe;
      if(to  ==typeof(bool))   return IsIn(typeConv[9], from) ? Conversion.None : Conversion.Safe;
      if(from==typeof(double)) return Conversion.None;
      if(from==typeof(long))   return IsIn(typeConv[6], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(char))   return IsIn(typeConv[8], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(byte))   return IsIn(typeConv[1], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(uint))   return IsIn(typeConv[5], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(float))  return to==typeof(double) ? Conversion.Safe : Conversion.None;
      if(from==typeof(short))  return IsIn(typeConv[2], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(ushort)) return IsIn(typeConv[3], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(sbyte))  return IsIn(typeConv[0], to) ? Conversion.Safe : Conversion.Unsafe;
      if(from==typeof(ulong))  return IsIn(typeConv[7], to) ? Conversion.Safe : Conversion.Unsafe;
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
  { if(Convert.GetTypeCode(desc)!=TypeCode.Object) return false;
    IDataDescriptor dd = desc as IDataDescriptor;
    if(dd != null) { dd.__delete__(instance); return true; }
    object dummy;
    return TryInvoke(desc, "__delete__", out dummy, instance);
  }

  public static void DelIndex(object obj, object index)
  { IMutableSequence seq = obj as IMutableSequence;
    if(seq!=null)
    { Slice slice = index as Slice;
      if(slice!=null) seq.__delitem__(slice);
      else seq.__delitem__(Ops.ToInt(index));
    }
    else Ops.Invoke(obj, "__delitem__", index);
  }

  public static object Divide(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return FloatOps.Divide((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return FloatOps.Divide((byte)a, b);
      case TypeCode.Decimal:
        if(b is Decimal) return (Decimal)a / (Decimal)b;
        break;
      case TypeCode.Double:  return FloatOps.Divide((double)a, b);
      case TypeCode.Int16: return FloatOps.Divide((short)a, b);
      case TypeCode.Int32: return FloatOps.Divide((int)a, b);
      case TypeCode.Int64: return FloatOps.Divide((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.Divide((Integer)a, b);
        if(a is Complex) return ComplexOps.Divide((Complex)a, b);
        object ret;
        return TryInvoke(a, "__truediv__",  out ret, b) ? ret :
               TryInvoke(b, "__rtruediv__", out ret, a) ? ret :
               TryInvoke(a, "__div__", out ret, b)      ? ret : Invoke(b, "__rdiv__", a);
      case TypeCode.SByte: return FloatOps.Divide((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Divide((float)a, b);
      case TypeCode.UInt16: return FloatOps.Divide((int)(short)a, b);
      case TypeCode.UInt32: return FloatOps.Divide((uint)a, b);
      case TypeCode.UInt64: return FloatOps.Divide((ulong)a, b);
    }
    throw TypeError("unsupported operand types for /: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static DivideByZeroException DivideByZeroError(string format, params object[] args)
  { return new DivideByZeroException(string.Format(format, args));
  }

  public static Tuple DivMod(object a, object b) // TODO: this could be optimized with type-specific code
  { object mod = Modulus(a, b);
    return new Tuple(Divide(Subtract(a, mod), b), mod);
  }

  public static System.IO.EndOfStreamException EOFError(string format, params object[] args)
  { return new System.IO.EndOfStreamException(string.Format(format, args));
  }

  public static object Equal(object a, object b)
  { if(a==b) return TRUE;
    return FromBool(a is Complex ? ((Complex)a).Equals(b) : Compare(a, b)==0);
  }

  public static object FindMetaclass(Tuple bases, IDictionary dict)
  { object mc = dict["__metaclass__"];
    if(mc!=null) return mc;
    // TODO: use a better rule for choosing the metaclass?
    return bases.Count>0 ? GetDynamicType(bases.items[0]) : ReflectedType.FromType(typeof(UserType));
  }

  public static int FixIndex(int index, int length)
  { if(index<0)
    { index += length;
      if(index<0) throw IndexError("index out of range: {0}", index-length);
    }
    else if(index>=length) throw IndexError("index out of range: {0}", index);
    return index;
  }

  public static int FixSliceIndex(int index, int length)
  { if(index<0)
    { index += length;
      if(index<0) index=0;
    }
    else if(index>length) index=length;
    return index;
  }

  public static object FloorDivide(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.FloorDivide((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return IntOps.FloorDivide((int)(byte)a, b);
      case TypeCode.Double:  return FloatOps.FloorDivide((double)a, b);
      case TypeCode.Int16: return IntOps.FloorDivide((int)(short)a, b);
      case TypeCode.Int32: return IntOps.FloorDivide((int)a, b);
      case TypeCode.Int64: return LongOps.FloorDivide((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.FloorDivide((Integer)a, b);
        object ret;
        return TryInvoke(a, "__floordiv__", out ret, b) ? ret : Invoke(b, "__rfloordiv__", a);
      case TypeCode.SByte: return IntOps.FloorDivide((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.FloorDivide((float)a, b);
      case TypeCode.UInt16: return IntOps.FloorDivide((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.FloorDivide((int)v, b) : LongOps.FloorDivide((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.FloorDivide((long)v, b) : IntegerOps.FloorDivide(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for /: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  // TODO: check whether we can eliminate this (ie, "true is true" still works)
  public static object FromBool(bool value) { return value ? TRUE : FALSE; }

  public static CompiledFunction GenerateFunction(string name, Boa.AST.Parameter[] parms, Delegate target)
  { string[] names = new string[parms.Length];
    for(int i=0; i<parms.Length; i++) names[i] = parms[i].Name.String;
    if(target is CallTargetN)
      return new CompiledFunctionN(name, names, null, false, false, names.Length, null, (CallTargetN)target);
    else if(target is CallTargetFN)
      return new CompiledFunctionFN(name, names, null, false, false, names.Length, null, (CallTargetFN)target);
    else throw new ArgumentException("Unhandled target type: " + target.GetType().FullName);
  }

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
  { if(Convert.GetTypeCode(desc)!=TypeCode.Object) return desc; // TODO: i'm not sure how much this optimization helps (if at all)

    IDescriptor d = desc as IDescriptor;
    if(d!=null) return d.__get__(instance);
    object ret;
    if(TryInvoke(desc, "__get__", out ret, instance)) return ret;
    return desc;
  }

  public static DynamicType GetDynamicType(object o)
  { if(o==null) return NullType.Value;
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
  { if(o is string) e=new BoaCharEnumerator((string)o);
    else if(o is IDictionary) e=((IDictionary)o).Keys.GetEnumerator();
    else if(o is IEnumerable) e=((IEnumerable)o).GetEnumerator();
    else if(o is ISequence) e=new ISeqEnumerator((ISequence)o);
    else if(o is IEnumerator) e=(IEnumerator)o;
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
    for(int i=1; i<trace.FrameCount; i++)
    { Type type = trace.GetFrame(i).GetMethod().DeclaringType;
      System.Reflection.FieldInfo fi = type.GetField(Module.FieldName);
      if(fi!=null && type.IsSubclassOf(typeof(Boa.AST.Snippet))) return (Module)fi.GetValue(null);
    }
    if(Frames.Count>0) return ((Frame)Frames.Peek()).Module;
    throw new InvalidOperationException("No executing module");
  }

  public static object GetIndex(object obj, object index)
  { ISequence seq = obj as ISequence;
    if(seq!=null)
    { Slice slice = index as Slice;
      return slice==null ? seq.__getitem__(ToInt(index)) : seq.__getitem__(slice);
    }
    else
    { string s = obj as string;
      if(s!=null)
      { Slice slice = index as Slice;
        return slice==null ? new string(s[FixIndex(ToInt(index), s.Length)], 1) : StringOps.Slice(s, slice);
      }
    }
    return Ops.Invoke(obj, "__getitem__", index);
  }

  public static ImportErrorException ImportError(string format, params object[] args)
  { return new ImportErrorException(string.Format(format, args));
  }

  public static void ImportFrom(Module module, string moduleName, string[] names, string[] asNames)
  { if(names.Length==0) return;
    if(names[0]=="*") ImportStar(module, moduleName);
    else
    { IHasAttributes mod = (IHasAttributes)Importer.Import(moduleName);

      if(mod is ReflectedType)
      { ReflectedType rmod = (ReflectedType)mod;
        for(int i=0; i<names.Length; i++)
          module.__setattr__(asNames[i]==null ? names[i] : asNames[i], rmod.GetRawAttr(names[i]));
      }
      else
        for(int i=0; i<names.Length; i++)
          module.__setattr__(asNames[i]==null ? names[i] : asNames[i], mod.__getattr__(names[i]));
    }
  }

  public static void ImportStar(Module module, string moduleName)
  { IHasAttributes mod = (IHasAttributes)Importer.Import(moduleName);
    ISequence exports = mod.__getattr__("__all__") as ISequence;

    if(mod is ReflectedType)
    { ReflectedType rmod = (ReflectedType)mod;
      if(exports==null)
      { foreach(string name in mod.__attrs__()) if(name[0]!='_') module.__setattr__(name, rmod.GetRawAttr(name));
      }
      else
        for(int i=0,len=exports.__len__(); i<len; i++)
        { string name = (string)exports.__getitem__(i);
          module.__setattr__(name, rmod.GetRawAttr(name));
        }
    }
    else
    { Module m = (Module)mod;
      if(exports==null)
      { foreach(string name in m.__dict__.Keys) if(name[0]!='_') module.__setattr__(name, mod.__getattr__(name));
      }
      else
        for(int i=0,len=exports.__len__(); i<len; i++)
        { string name = (string)exports.__getitem__(i);
          module.__setattr__(name, mod.__getattr__(name));
        }
    }
  }

  public static IndexErrorException IndexError(string format, params object[] args)
  { return new IndexErrorException(string.Format(format, args));
  }

  public static object Invoke(object target, string name, params object[] args)
  { object ret=Call(GetAttr(target, name), args);
    if(ret==NotImplemented)
      throw Ops.TypeError("method '{0}' is not implemented on type '{1}'", name, TypeName(target));
    return ret;
  }

  public static System.IO.IOException IOError(string format, params object[] args)
  { return new System.IO.IOException(string.Format(format, args));
  }

  public static object IsIn(object a, object b, bool not)
  { IContainer ct = b as IContainer;
    if(ct==null)
    { object isin = Invoke(b, "__contains__", a);
      return not ? FromBool(!Ops.IsTrue(isin)) : isin;
    }
    else return FromBool(not ? !ct.__contains__(a) : ct.__contains__(a));
  }

  public static bool IsTrue(object a)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return (bool)a;
      case TypeCode.Byte:    return (byte)a!=0;
      case TypeCode.Char:    return (char)a!=0;
      case TypeCode.Decimal: return (Decimal)a!=0;
      case TypeCode.Double:  return (double)a!=0;
      case TypeCode.Empty:   return false;
      case TypeCode.Int16:   return (short)a!=0;
      case TypeCode.Int32:   return (int)a!=0;
      case TypeCode.Int64:   return (long)a!=0;
      case TypeCode.Object:
        if(a is Integer) return (Integer)a!=0;
        if(a is Complex) return ComplexOps.NonZero((Complex)a);
        if(a is ICollection) return ((ICollection)a).Count>0;
        if(a is ISequence) return ((ISequence)a).__len__()>0;
        object ret;
        if(TryInvoke(a, "__nonzero__", out ret)) return IsTrue(ret);
        if(TryInvoke(a, "__len__", out ret)) return Ops.ToInt(ret)>0;
        return true;
      case TypeCode.SByte:  return (sbyte)a!=0;
      case TypeCode.Single: return (float)a!=0;
      case TypeCode.String: return ((string)a).Length>0;
      case TypeCode.UInt16: return (short)a!=0;
      case TypeCode.UInt32: return (uint)a!=0;
      case TypeCode.UInt64: return (ulong)a!=0;
    }
    return true;
  }

  public static KeyErrorException KeyError(string format, params object[] args)
  { return new KeyErrorException(string.Format(format, args));
  }

  public static object LeftShift(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.LeftShift((bool)a ? 1 : 0, b);
      case TypeCode.Byte:  return IntOps.LeftShift((int)(byte)a, b);
      case TypeCode.Int16: return IntOps.LeftShift((int)(short)a, b);
      case TypeCode.Int32: return IntOps.LeftShift((int)a, b);
      case TypeCode.Int64: return LongOps.LeftShift((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.LeftShift((Integer)a, b);
        object ret;
        return TryInvoke(a, "__lshift__", out ret, b) ? ret : Invoke(b, "__rlshift__", a);
      case TypeCode.SByte: return IntOps.LeftShift((int)(sbyte)a, b);
      case TypeCode.UInt16: return IntOps.LeftShift((int)(short)a, b);
      case TypeCode.UInt32: return LongOps.LeftShift((long)(uint)a, b);
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.LeftShift((long)v, b) : IntegerOps.LeftShift(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for <<: '{0}' and '{1}'",
                    TypeName(a), TypeName(b));
  }

  public static object Less(object a, object b) { return FromBool(Compare(a,b)<0); }
  public static object LessEqual(object a, object b) { return FromBool(Compare(a,b)<=0); }

  public static object LogicalAnd(object a, object b) { return IsTrue(a) ? b : a; }
  public static object LogicalOr (object a, object b) { return IsTrue(a) ? a : b; }

  public static LookupErrorException LookupError(string format, params object[] args)
  { return new LookupErrorException(string.Format(format, args));
  }

  public static object MakeClass(string module, string name, Tuple bases, IDictionary dict)
  { if(bases.Count==0) bases = new Tuple(new object[] { ReflectedType.FromType(typeof(object)) });
    object metaclass = FindMetaclass(bases, dict);
    if(metaclass==ReflectedType.FromType(typeof(ReflectedType)) ||
       metaclass==ReflectedType.FromType(typeof(UserType)))
      return new UserType(module, name, bases, dict);
    return Call(metaclass, name, bases, dict);
  }

  public static TypeErrorException MethodCalledWithoutInstance(string name)
  { return new TypeErrorException(name+" is a method and requires an instance object");
  }

  public static object Modulus(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.Modulus((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return IntOps.Modulus((int)(byte)a, b);
      case TypeCode.Decimal: return FloatOps.Modulus(((IConvertible)a).ToDouble(NumberFormatInfo.InvariantInfo), b);
      case TypeCode.Double:  return FloatOps.Modulus((double)a, b);
      case TypeCode.Int16:   return IntOps.Modulus((int)(short)a, b);
      case TypeCode.Int32:   return IntOps.Modulus((int)a, b);
      case TypeCode.Int64:   return LongOps.Modulus((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.Modulus((Integer)a, b);
        object ret;
        return TryInvoke(a, "__mod__", out ret, b) ? ret : Invoke(b, "__rmod__", a);
      case TypeCode.SByte: return IntOps.Modulus((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Modulus((float)a, b);
      case TypeCode.String: return StringOps.PrintF((string)a, b);
      case TypeCode.UInt16: return IntOps.Modulus((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.Modulus((int)v, b) : LongOps.Modulus((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.Modulus((long)v, b) : IntegerOps.Modulus(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for %: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static object Multiply(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.Multiply((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return IntOps.Multiply((int)(byte)a, b);
      case TypeCode.Char:    return IntOps.Multiply((int)(char)a, b);
      case TypeCode.Decimal:
        if(b is Decimal) return (Decimal)a * (Decimal)b;
        break;
      case TypeCode.Double:  return FloatOps.Multiply((double)a, b);
      case TypeCode.Int16: return IntOps.Multiply((int)(short)a, b);
      case TypeCode.Int32: return IntOps.Multiply((int)a, b);
      case TypeCode.Int64: return LongOps.Multiply((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.Multiply((Integer)a, b);
        if(a is Complex) return ComplexOps.Multiply((Complex)a, b);
        if(a is ICollection || a is ISequence) return ArrayOps.Multiply(a, b);
        object ret;
        return TryInvoke(a, "__mul__", out ret, b) ? ret : Invoke(b, "__rmul__", a);
      case TypeCode.SByte: return IntOps.Multiply((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Multiply((float)a, b);
      case TypeCode.String: return StringOps.Multiply((string)a, b);
      case TypeCode.UInt16: return IntOps.Multiply((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.Multiply((int)v, b) : LongOps.Multiply((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.Multiply((long)v, b) : IntegerOps.Multiply(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for *: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static object More(object a, object b) { return FromBool(Compare(a,b)>0); }
  public static object MoreEqual(object a, object b) { return FromBool(Compare(a,b)>=0); }

  public static NameErrorException NameError(string format, params object[] args)
  { return new NameErrorException(string.Format(format, args));
  }

  public static object Negate(object a)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return (bool)a ? -1 : 0;
      case TypeCode.Byte:  return -(int)(byte)a;
      case TypeCode.Decimal: return -(Decimal)a;
      case TypeCode.Double: return -(double)a;
      case TypeCode.Int16: return -(int)(short)a;
      case TypeCode.Int32: return -(int)a;
      case TypeCode.Int64: return -(long)a;
      case TypeCode.Object:
        return a is Integer ? -(Integer)a : a is Complex ? -(Complex)a : Invoke(a, "__neg__");
      case TypeCode.SByte: return -(int)(sbyte)a;
      case TypeCode.Single: return -(float)a;
      case TypeCode.UInt16: return -(int)(short)a;
      case TypeCode.UInt32:
      { uint v = (uint)a;
        return v<=int.MaxValue ? (object)-(int)v : (object)-(long)v;
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? (object)-(long)v : (object)-new Integer(v);
      }
    }
    throw TypeError("unsupported operand type for unary -: '{0}'", TypeName(a));
  }

  public static object NotEqual(object a, object b)
  { if(a==b) return FALSE;
    return FromBool(a is Complex ? !((Complex)a).Equals(b) : Compare(a, b)!=0);
  }

  public static NotImplementedException NotImplementedError(string format, params object[] args)
  { return new NotImplementedException(string.Format(format, args));
  }

  public static OSErrorException OSError(string format, params object[] args)
  { return new OSErrorException(string.Format(format, args));
  }

  public static OverflowException OverflowError(string format, params object[] args)
  { return new OverflowException(string.Format(format, args));
  }

  public static object Power(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.Power((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return IntOps.Power((int)(byte)a, b);
      case TypeCode.Double:  return FloatOps.Power((double)a, b);
      case TypeCode.Int16: return IntOps.Power((int)(short)a, b);
      case TypeCode.Int32: return IntOps.Power((int)a, b);
      case TypeCode.Int64: return LongOps.Power((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.Power((Integer)a, b);
        if(a is Complex) return ComplexOps.Power((Complex)a, b);
        object ret;
        return TryInvoke(a, "__pow__", out ret, b) ? ret : Invoke(b, "__rpow__", a);
      case TypeCode.SByte: return IntOps.Power((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Power((float)a, b);
      case TypeCode.UInt16: return IntOps.Power((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.Power((int)v, b) : LongOps.Power((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.Power((long)v, b) : IntegerOps.Power(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for **: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  // TODO: optimize this
  public static object PowerMod(object a, object b, object c)
  { if(Convert.GetTypeCode(a)==TypeCode.Object)
    { if(a is Integer) return IntegerOps.PowerMod((Integer)a, b, c);
      if(a is Complex) return ComplexOps.PowerMod((Complex)a, b, c);
      return Invoke(a, "__pow__", b, c);
    }
    return Modulus(Power(a, b), c);
  }

  public static IEnumerator PrepareTupleAssignment(object value, int items)
  { if(value is ICollection)
    { ICollection col = (ICollection)value;
      if(col.Count==items)
      { IDictionary d = col as IDictionary;
        return d==null ? col.GetEnumerator() : d.Keys.GetEnumerator();
      }
    }
    else if(value is string)
    { string s = (string)value;
      if(s.Length==items) return new BoaCharEnumerator(s);
    }
    else if(value is ISequence)
    { ISequence seq = (ISequence)value;
      if(seq.__len__()==items) return new ISeqEnumerator(seq);
    }
    else if(ToInt(Invoke(value, "__len__", Misc.EmptyArray)) == items) return new SeqEnumerator(value);

    throw Ops.ValueError("wrong number of values to unpack");
  }

  public static void Print(object file, object o)
  { string str = Str(o);
    if(file==null) { Console.Write(str); return; }

    IFile f = file as IFile;
    if(f!=null) { f.write(str); return; }

    System.IO.Stream stream = file as System.IO.Stream;
    if(stream!=null)
    { byte[] data = System.Text.Encoding.Default.GetBytes(str);
      stream.Write(data, 0, data.Length);
      return;
    }

    Invoke(file, "write", str);
  }

  public static void PrintNewline(object file)
  { if(file==null) { Console.WriteLine(); return; }

    IFile f = file as IFile;
    if(f!=null) { f.writebyte('\n'); return; }

    System.IO.Stream stream = file as System.IO.Stream;
    if(stream!=null) { stream.WriteByte((byte)'\n'); return; }

    Invoke(file, "write", "\n");
  }

  // TODO: handle char somehow
  public static string Repr(object o)
  { switch(Convert.GetTypeCode(o))
    { case TypeCode.Boolean: return (bool)o ? "true" : "false";
      case TypeCode.Byte: case TypeCode.Int16: case TypeCode.Int32: case TypeCode.SByte: case TypeCode.UInt16:
        return o.ToString();
      case TypeCode.Double: return ((double)o).ToString("R");
      case TypeCode.Empty: return "null";
      case TypeCode.Int64: case TypeCode.UInt64: return o.ToString()+'L';
      case TypeCode.Object:
        if(o is IRepresentable) return ((IRepresentable)o).__repr__();
        if(o is Array) return ArrayOps.Repr((Array)o);
        break;
      case TypeCode.Single: return ((float)o).ToString("R");
      case TypeCode.String: return StringOps.Escape((string)o);
      case TypeCode.UInt32:
      { string ret = o.ToString();
        if((uint)o>int.MaxValue) ret += 'L';
        return ret;
      }
    }
    return GetDynamicType(o).Repr(o);
  }

  public static object RightShift(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.RightShift((bool)a ? 1 : 0, b);
      case TypeCode.Byte:  return IntOps.RightShift((int)(byte)a, b);
      case TypeCode.Int16: return IntOps.RightShift((int)(short)a, b);
      case TypeCode.Int32: return IntOps.RightShift((int)a, b);
      case TypeCode.Int64: return LongOps.RightShift((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.RightShift((Integer)a, b);
        object ret;
        return TryInvoke(a, "__rshift__", out ret, b) ? ret : Invoke(b, "__rrshift__", a);
      case TypeCode.SByte: return IntOps.RightShift((int)(sbyte)a, b);
      case TypeCode.UInt16: return IntOps.RightShift((int)(short)a, b);
      case TypeCode.UInt32: return LongOps.RightShift((long)(uint)a, b);
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.RightShift((long)v, b) : IntegerOps.RightShift(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for >>: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static RuntimeException RuntimeError(string format, params object[] args)
  { return new RuntimeException(string.Format(format, args));
  }

  public static List SequenceSlice(ISequence seq, Slice slice)
  { Tuple tup = slice.indices(seq.__len__());
    return SequenceSlice(seq, (int)tup.items[0], (int)tup.items[1], (int)tup.items[2]);
  }
  public static List SequenceSlice(ISequence seq, int start, int stop, int step)
  { if(step<0 && start<=stop || step>0 && start>=stop) return new List();
    int sign = Math.Sign(step);
    List ret = new List((stop-start+step-sign)/step);
    if(step<0) for(; start>stop; start+=step) ret.append(seq.__getitem__(start));
    else for(; start<stop; start+=step) ret.append(seq.__getitem__(start));
    return ret;
  }

  public static void SetAttr(object value, object o, string name)
  { IHasAttributes iha = o as IHasAttributes;
    if(iha!=null)  iha.__setattr__(name, value);
    else GetDynamicType(o).SetAttr(o, name, value);
  }

  public static bool SetDescriptor(object desc, object instance, object value)
  { if(Convert.GetTypeCode(desc)!=TypeCode.Object) return false; // TODO: i'm not sure how much this optimization helps (if at all)
    IDataDescriptor dd = desc as IDataDescriptor;
    if(dd!=null) { dd.__set__(instance, value); return true; }
    object dummy;
    return TryInvoke(desc, "__set__", out dummy, instance, value);
  }

  public static void SetIndex(object value, object obj, object index)
  { IMutableSequence seq = obj as IMutableSequence;
    if(seq!=null)
    { Slice slice = index as Slice;
      if(slice!=null) seq.__setitem__(slice, value);
      else seq.__setitem__(Ops.ToInt(index), value);
    }
    else Ops.Invoke(obj, "__setitem__", index, value);
  }

  public static string Str(object o)
  { switch(Convert.GetTypeCode(o))
    { case TypeCode.Boolean: return (bool)o ? "true" : "false";
      case TypeCode.Empty: return "null";
      case TypeCode.Object:
      { object ret;
        if(TryInvoke(o, "__str__", out ret)) return Ops.ToString(ret);
        break;
      }
      case TypeCode.String: return (string)o;
    }
    return o.ToString();
  }

  public static object Subtract(object a, object b)
  { switch(Convert.GetTypeCode(a))
    { case TypeCode.Boolean: return IntOps.Subtract((bool)a ? 1 : 0, b);
      case TypeCode.Byte:    return IntOps.Subtract((int)(byte)a, b);
      case TypeCode.Char:
        if(b is char) return (char)a-(char)b;
        break;
      case TypeCode.Decimal:
        if(b is Decimal) return (Decimal)a-(Decimal)b;
        break;
      case TypeCode.Double:  return FloatOps.Subtract((double)a, b);
      case TypeCode.Int16: return IntOps.Subtract((int)(short)a, b);
      case TypeCode.Int32: return IntOps.Subtract((int)a, b);
      case TypeCode.Int64: return LongOps.Subtract((long)a, b);
      case TypeCode.Object:
        if(a is Integer) return IntegerOps.Subtract((Integer)a, b);
        if(a is Complex) return ComplexOps.Subtract((Complex)a, b);
        object ret;
        return TryInvoke(a, "__sub__", out ret, b) ? ret : Invoke(b, "__rsub__", a);
      case TypeCode.SByte: return IntOps.Subtract((int)(sbyte)a, b);
      case TypeCode.Single: return FloatOps.Subtract((float)a, b);
      case TypeCode.UInt16: return IntOps.Subtract((int)(short)a, b);
      case TypeCode.UInt32: 
      { uint v = (uint)a;
        return v<=int.MaxValue ? IntOps.Subtract((int)v, b) : LongOps.Subtract((long)v, b);
      }
      case TypeCode.UInt64:
      { ulong v = (ulong)a;
        return v<=long.MaxValue ? LongOps.Subtract((long)v, b) : IntegerOps.Subtract(new Integer(v), b);
      }
    }
    throw TypeError("unsupported operand types for -: '{0}' and '{1}'", TypeName(a), TypeName(b));
  }

  public static SyntaxErrorException SyntaxError(string format, params object[] args)
  { return new SyntaxErrorException(string.Format(format, args));
  }

  public static SyntaxErrorException SyntaxError(Boa.AST.Node node, string format, params object[] args)
  { return new SyntaxErrorException(Source(node)+string.Format(format, args));
  }

  public static double ToFloat(object o)
  { if(o is double) return (double)o;
    try { return Convert.ToDouble(o); }
    catch(OverflowException) { throw ValueError("too big for float"); }
    catch(InvalidCastException) { throw TypeError("expected float, but got {0}", TypeName(o)); }
  }

  public static int ToInt(object o)
  { if(o is int) return (int)o;
    try { return Convert.ToInt32(o); }
    catch(OverflowException) { throw ValueError("too big for int"); } // TODO: allow conversion to long integer?
    catch(InvalidCastException) { throw TypeError("expected int, but got {0}", TypeName(o)); }
  }

  public static uint ToUInt(object o)
  { if(o is int) return (uint)(int)o;
    if(o is uint) return (uint)o;
    if(o is long)
    { long lv = (long)o;
      if(lv<0 || lv>uint.MaxValue) throw ValueError("too big for uint");
      return (uint)lv;
    }
    try { return Convert.ToUInt32(o); }
    catch(OverflowException) { throw ValueError("too big for uint"); } // TODO: allow conversion to long integer?
    catch(InvalidCastException) { throw TypeError("expected uint, but got {0}", TypeName(o)); }
  }

  public static long ToLong(object o)
  { if(o is long) return (long)o;
    try { return Convert.ToInt64(o); }
    catch(OverflowException) { throw ValueError("too big for long"); } // TODO: allow conversion to long integer?
    catch(InvalidCastException) { throw TypeError("expected long, but got {0}", TypeName(o)); }
  }

  public static ulong ToULong(object o)
  { if(o is long) return (ulong)(long)o;
    if(o is ulong) return (ulong)o;
    try { return Convert.ToUInt64(o); }
    catch(OverflowException) { throw ValueError("too big for ulong"); } // TODO: allow conversion to long integer?
    catch(InvalidCastException) { throw TypeError("expected ulong, but got {0}", TypeName(o)); }
  }

  public static string ToString(object o)
  { if(o==null) throw Ops.TypeError("'null' could not be converted to string");
    try
    { if(o is string) return (string)o;
      return o.ToString();
    }
    catch { throw Ops.TypeError("'{0}' could not be converted to string", TypeName(o)); }
  }

  public static TypeErrorException TooFewArgs(string name, int expected, int got)
  { return TypeError("{0} requires at least {1} arguments ({2} given)", name, expected, got);
  }

  public static TypeErrorException TooManyArgs(string name, int expected, int got)
  { return TypeError("{0} requires at most {1} arguments ({2} given)", name, expected, got);
  }

  public static bool TryInvoke(object target, string name, out object retValue, params object[] args)
  { object method;
    if(GetAttr(target, name, out method)) { retValue = Call(method, args); return retValue!=NotImplemented; }
    else { retValue = null; return false; }
  }

  public static TypeErrorException TypeError(string format, params object[] args)
  { return new TypeErrorException(string.Format(format, args));
  }
  public static TypeErrorException TypeError(Boa.AST.Node node, string format, params object[] args)
  { return new TypeErrorException(Source(node)+string.Format(format, args));
  }

  public static string TypeName(object o) { return GetDynamicType(o).__name__.ToString(); }

  public static ValueErrorException ValueError(string format, params object[] args)
  { return new ValueErrorException(string.Format(format, args));
  }
  public static ValueErrorException ValueError(Boa.AST.Node node, string format, params object[] args)
  { return new ValueErrorException(Source(node)+string.Format(format, args));
  }

  public static TypeErrorException WrongNumArgs(string name, int expected, int got)
  { return TypeError("{0} requires {1} arguments ({2} given)", name, expected, got);
  }

  public static Stack Frames = new Stack();

  public static readonly object FALSE=false, TRUE=true;

  static bool IsIn(Type[] typeArr, Type type)
  { for(int i=0; i<typeArr.Length; i++) if(typeArr[i]==type) return true;
    return false;
  }

  static string Source(Boa.AST.Node node)
  { return string.Format("{0}({1},{2}): ", node.Source, node.Line, node.Column);
  }

  static readonly ReflectedType StringType = ReflectedType.FromType(typeof(string));
  static readonly Type[][] typeConv = 
  { // FROM
    new Type[] { typeof(int), typeof(double), typeof(short), typeof(long), typeof(float) }, // sbyte
    new Type[] // byte
    { typeof(int), typeof(double), typeof(uint), typeof(short), typeof(ushort), typeof(long), typeof(ulong),
      typeof(float)
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
