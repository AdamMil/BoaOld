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
public sealed class _bisect
{ _bisect() { }

  public static string __repr__() { return "<module 'bisect' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  public static int bisect(List a, object x) { return bisect_right(a, x, 0, -1); }
  public static int bisect(List a, object x, int lo) { return bisect_right(a, x, lo, -1); }
  public static int bisect(List a, object x, int lo, int hi) { return bisect_right(a, x, lo, hi); }

  public static int bisect_left(List a, object x) { return bisect_left(a, x, 0, -1); }
  public static int bisect_left(List a, object x, int lo) { return bisect_left(a, x, lo, -1); }
  public static int bisect_left(List a, object x, int lo, int hi)
  { if(hi<0) hi=a.Count;
    while(lo<hi)
    { int mid = (lo+hi)/2;
      if(Ops.Compare(x, a[mid])<0) lo=mid+1;
      else hi=mid;
    }
    return lo;
  }

  public static int bisect_right(List a, object x) { return bisect_right(a, x, 0, -1); }
  public static int bisect_right(List a, object x, int lo) { return bisect_right(a, x, lo, -1); }
  public static int bisect_right(List a, object x, int lo, int hi)
  { if(hi<0) hi=a.Count;
    while(lo<hi)
    { int mid = (lo+hi)/2;
      if(Ops.Compare(x, a[mid])<0) hi=mid;
      else lo=mid+1;
    }
    return lo;
  }

  public static void insort(List a, object x) { insort_right(a, x, 0, -1); }
  public static void insort(List a, object x, int lo) { insort_right(a, x, lo, -1); }
  public static void insort(List a, object x, int lo, int hi) { insort_right(a, x, lo, hi); }
  
  public static void insort_left(List a, object x) { insort_left(a, x, 0, -1); }
  public static void insort_left(List a, object x, int lo) { insort_left(a, x, lo, -1); }
  public static void insort_left(List a, object x, int lo, int hi) { a.insert(bisect_left(a, x, lo, hi), x); }

  public static void insort_right(List a, object x) { insort_right(a, x, 0, -1); }
  public static void insort_right(List a, object x, int lo) { insort_right(a, x, lo, -1); }
  public static void insort_right(List a, object x, int lo, int hi) { a.insert(bisect_right(a, x, lo, hi), x); }  
}

} // namespace Boa.Modules