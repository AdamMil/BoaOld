/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

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
public sealed class types
{ types() { }

  public static string __repr__() { return "<module 'types' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static readonly DynamicType   NoneType = NullType.Value;
  public static readonly ReflectedType TypeType = ReflectedType.FromType(typeof(DynamicType));
  public static readonly ReflectedType BooleanType = ReflectedType.FromType(typeof(bool));
  public static readonly ReflectedType IntType  = ReflectedType.FromType(typeof(int));
  public static readonly ReflectedType LongType = ReflectedType.FromType(typeof(long));
  public static readonly ReflectedType IntegerType = ReflectedType.FromType(typeof(Integer));
  public static readonly ReflectedType FloatType = ReflectedType.FromType(typeof(double));
  public static readonly ReflectedType ComplexType = ReflectedType.FromType(typeof(Complex));
  public static readonly ReflectedType StringType = ReflectedType.FromType(typeof(string));
  public static readonly ReflectedType UnicodeType = StringType;
  public static readonly ReflectedType TupleType = ReflectedType.FromType(typeof(Tuple));
  public static readonly ReflectedType ListType = ReflectedType.FromType(typeof(List));
  public static readonly ReflectedType DictType = ReflectedType.FromType(typeof(Dict));
  public static readonly ReflectedType DictionaryType = DictType;
  public static readonly ReflectedType FunctionType = ReflectedType.FromType(typeof(Function));
  public static readonly ReflectedType LambdaType = FunctionType;
  public static readonly ReflectedType GeneratorType = ReflectedType.FromType(typeof(Generator));
  public static readonly ReflectedType UserType = ReflectedType.FromType(typeof(UserType));
  public static readonly ReflectedType MethodType = ReflectedType.FromType(typeof(MethodWrapper));
  public static readonly ReflectedType UnboundMethodType = MethodType;
  public static readonly ReflectedType BuiltinMethodType = ReflectedType.FromType(typeof(ReflectedMethodBase));
  public static readonly ReflectedType FileType = ReflectedType.FromType(typeof(BoaFile));
  public static readonly ReflectedType XRangeType = ReflectedType.FromType(typeof(Boa.Modules.__builtin__.XRange));
  public static readonly ReflectedType SliceType = ReflectedType.FromType(typeof(Slice));
  public static readonly Tuple StringTypes = new Tuple(new object[] { StringType });
}

} // namespace Boa.Modules