using System;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class @operator
{ @operator() { }

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 'operator' (built-in)>"; }

  // boolean
  /*public static object contains(object a, object b) { return Ops.In(a, b); }*/ // TODO: implement this
  public static object eq(object a, object b) { return Ops.Equal(a, b); }
  public static object ge(object a, object b) { return Ops.MoreEqual(a, b); }
  public static object gt(object a, object b) { return Ops.More(a, b); }
  public static object is_(object a, object b) { return Ops.FromBool(a==b); }
  public static object is_not(object a, object b) { return Ops.FromBool(a!=b); }
  public static object le(object a, object b) { return Ops.LessEqual(a, b); }
  public static object lt(object a, object b) { return Ops.Less(a, b); }
  public static object ne(object a, object b) { return Ops.NotEqual(a, b); }
  public static object not_(object o) { return Ops.FromBool(!Ops.IsTrue(o)); }
  public static object truth(object o) { return Ops.FromBool(Ops.IsTrue(o)); }
  
  // mathematical and bitwise
  public static object add(object a, object b) { return Ops.Add(a, b); }
  public static object and(object a, object b) { return Ops.BitwiseAnd(a, b); }
  public static object div(object a, object b) { return Ops.Divide(a, b); }
  public static object floordiv(object a, object b) { return Ops.FloorDivide(a, b); }
  public static object invert(object o) { return Ops.BitwiseNegate(o); }
  public static object lshift(object a, object b) { return Ops.LeftShift(a, b); }
  public static object mod(object a, object b) { return Ops.Modulus(a, b); }
  public static object mul(object a, object b) { return Ops.Multiply(a, b); }
  public static object neg(object o) { return Ops.Negate(o); }
  public static object or(object a, object b) { return Ops.BitwiseOr(a, b); }
  public static object pow(object a, object b) { return Ops.Power(a, b); }
  public static object rshift(object a, object b) { return Ops.RightShift(a, b); }
  public static object sub(object a, object b) { return Ops.Subtract(a, b); }
  public static object xor(object a, object b) { return Ops.BitwiseXor(a, b); }
  
  // indexing
  public static void delitem(object obj, object index) { Ops.DelIndex(obj, index); }
  public static void delslice(object obj, Slice slice) { Ops.DelIndex(obj, slice); }
  public static void delslice(object obj, object start, object end) { Ops.DelIndex(obj, new Slice(start, end)); }

  public static object getitem(object obj, object index) { return Ops.GetIndex(obj, index); }
  public static object getslice(object obj, Slice slice) { return Ops.GetIndex(obj, slice); }
  public static object getslice(object obj, object start, object end)
  { return Ops.GetIndex(obj, new Slice(start, end));
  }

  public static void setitem(object obj, object index, object value) { Ops.SetIndex(obj, index, value); }
  public static void setslice(object obj, Slice slice, object value) { Ops.SetIndex(obj, slice, value); }
  public static void setslice(object obj, object start, object end, object value)
  { Ops.SetIndex(obj, new Slice(start, end), value);
  }
}

} // namespace Boa.Modules