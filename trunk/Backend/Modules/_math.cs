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
using System.Runtime.InteropServices;

namespace Boa.Modules
{

[BoaType("module")]
[System.Security.SuppressUnmanagedCodeSecurity()]
public sealed class _math
{ _math() { }

  #if LINUX
  const string ImportDll = "glibc.so"; // TODO: figure out what this should really be
  #else
  const string ImportDll = "msvcrt.dll";
  #endif
  
  public static string __repr__() { return "<module 'math' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static double degrees(double rads) { return rads*RadiansToDegrees; }
  public static double fabs(double v) { return Math.Abs(v); }

  [DllImport(ImportDll, CallingConvention=CallingConvention.Cdecl)]
  public static extern double fmod(double x, double y);

  public static Tuple frexp(double v)
  { int exp;
    return new Tuple(frexp(v, out exp), exp);
  }

  public static double hypot(double x, double y) { return Math.Sqrt(x*x+y*y); }
  public static double ldexp(Tuple t) { return ldexp(Ops.ToFloat(t.items[0]), Ops.ToInt(t.items[1])); }

  public static Tuple modf(double v)
  { double whole, frac=modf(v, out whole);
    return new Tuple(whole, frac);
  }

  public static double radians(double degs) { return degs*DegreesToRadians; }

  [DllImport(ImportDll, CallingConvention=CallingConvention.Cdecl)]
  static extern double frexp(double v, out int e);
  [DllImport(ImportDll, CallingConvention=CallingConvention.Cdecl)]
  static extern double ldexp(double m, int e);
  [DllImport(ImportDll, CallingConvention=CallingConvention.Cdecl)]
  static extern double modf(double v, out double whole);

  const double DegreesToRadians = Math.PI/180;
  const double RadiansToDegrees = 180/Math.PI;
}

} // namespace Boa.Modules