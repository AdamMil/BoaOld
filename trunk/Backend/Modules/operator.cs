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
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class @operator
{ @operator() { }

  public static string __repr__() { return "<module 'operator' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  // boolean
  /*public static object contains(object a, object b) { return Ops.In(a, b); }*/ // TODO: implement this
  [DocString("eq(a, b) -> bool\nequivalent to 'a==b'")]
  public static object eq(object a, object b) { return Ops.Equal(a, b); }
  [DocString("ge(a, b) -> bool\nequivalent to 'a>=b'")]
  public static object ge(object a, object b) { return Ops.MoreEqual(a, b); }
  [DocString("gt(a, b) -> bool\nequivalent to 'a>b'")]
  public static object gt(object a, object b) { return Ops.More(a, b); }
  [DocString("is_(a, b) -> bool\nequivalent to 'a is b'")]
  public static object is_(object a, object b) { return Ops.FromBool(a==b); }
  [DocString("is_not(a, b) -> bool\nequivalent to 'a is not b'")]
  public static object is_not(object a, object b) { return Ops.FromBool(a!=b); }
  [DocString("le(a, b) -> bool\nequivalent to 'a<=b'")]
  public static object le(object a, object b) { return Ops.LessEqual(a, b); }
  [DocString("lt(a, b) -> bool\nequivalent to 'a<b'")]
  public static object lt(object a, object b) { return Ops.Less(a, b); }
  [DocString("ne(a, b) -> bool\nequivalent to 'a!=b'")]
  public static object ne(object a, object b) { return Ops.NotEqual(a, b); }
  [DocString("not_(o) -> bool\nequivalent to '!o'")]
  public static object not_(object o) { return Ops.FromBool(!Ops.IsTrue(o)); }
  [DocString("truth(o) -> bool\nequivalent to 'bool(o)'")]
  public static object truth(object o) { return Ops.FromBool(Ops.IsTrue(o)); }
  
  // logical
  [DocString("land(a, b) -> object\nequivalent to 'a&&b', but without short-circuiting")]
  public static object land(object a, object b) { return Ops.IsTrue(a) ? b : a; }
  [DocString("lor(a, b) -> object\nequivalent to 'a||b', but without short-circuiting")]
  public static object lor(object a, object b) { return Ops.IsTrue(a) ? a : b; }
  
  // mathematical and bitwise
  [DocString("add(a, b) -> object\nequivalent to 'a+b'")]
  public static object add(object a, object b) { return Ops.Add(a, b); }
  [DocString("and(a, b) -> object\nequivalent to 'a&b'")]
  public static object and(object a, object b) { return Ops.BitwiseAnd(a, b); }
  [DocString("div(a, b) -> object\nequivalent to 'a/b'")]
  public static object div(object a, object b) { return Ops.Divide(a, b); }
  [DocString("floordiv(a, b) -> object\nequivalent to 'a//b'")]
  public static object floordiv(object a, object b) { return Ops.FloorDivide(a, b); }
  [DocString("invert(o) -> object\nequivalent to '~o'")]
  public static object invert(object o) { return Ops.BitwiseNegate(o); }
  [DocString("lshift(a, b) -> object\nequivalent to 'a<<b'")]
  public static object lshift(object a, object b) { return Ops.LeftShift(a, b); }
  [DocString("mod(a, b) -> object\nequivalent to 'a%b'")]
  public static object mod(object a, object b) { return Ops.Modulus(a, b); }
  [DocString("mul(a, b) -> object\nequivalent to 'a*b'")]
  public static object mul(object a, object b) { return Ops.Multiply(a, b); }
  [DocString("neg(o) -> object\nequivalent to '-o'")]
  public static object neg(object o) { return Ops.Negate(o); }
  [DocString("or(a, b) -> object\nequivalent to 'a|b'")]
  public static object or(object a, object b) { return Ops.BitwiseOr(a, b); }
  [DocString("pow(a, b) -> object\nequivalent to 'a**b'")]
  public static object pow(object a, object b) { return Ops.Power(a, b); }
  [DocString("rshift(a, b) -> object\nequivalent to 'a>>b'")]
  public static object rshift(object a, object b) { return Ops.RightShift(a, b); }
  [DocString("sub(a, b) -> object\nequivalent to 'a-b'")]
  public static object sub(object a, object b) { return Ops.Subtract(a, b); }
  [DocString("xor(a, b) -> object\nequivalent to 'a^b'")]
  public static object xor(object a, object b) { return Ops.BitwiseXor(a, b); }
  
  // indexing
  [DocString("delitem(obj, index)\nEquivalent to 'del obj[index]'")]
  public static void delitem(object obj, object index) { Ops.DelIndex(obj, index); }
  [DocString("delslice(obj, start, end)\nEquivalent to 'del obj[start:end]'")]
  public static void delslice(object obj, object start, object end) { Ops.DelIndex(obj, new Slice(start, end)); }

  [DocString("getitem(obj, index) -> object\nEquivalent to 'obj[index]'")]
  public static object getitem(object obj, object index) { return Ops.GetIndex(obj, index); }
  [DocString("getslice(obj, start, end) -> object\nEquivalent to 'obj[start:end]'")]
  public static object getslice(object obj, object start, object end)
  { return Ops.GetIndex(obj, new Slice(start, end));
  }

  [DocString("setitem(obj, index, value)\nEquivalent to 'obj[index]=value'")]
  public static void setitem(object obj, object index, object value) { Ops.SetIndex(obj, index, value); }
  [DocString("setslice(obj, start, end, value)\nEquivalent to 'obj[start:end]=value'")]
  public static void setslice(object obj, object start, object end, object value)
  { Ops.SetIndex(obj, new Slice(start, end), value);
  }
}

} // namespace Boa.Modules