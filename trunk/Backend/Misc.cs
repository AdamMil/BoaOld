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

// TODO: make use of 'readonly' attribute
// TODO: make use of 'sealed' attribute
// TODO: unify variable names

using System;

namespace Boa
{

public sealed class Misc
{ private Misc() { }
  
  public static Type[] MakeTypeArray(Type type, int length)
  { Type[] arr = new Type[length];
    for(int i=0; i<length; i++) arr[i] = type;
    return arr;
  }
  
  public static readonly object[] EmptyArray = new object[0];
  public static readonly Type TypeOfObjectRef = Type.GetType("System.Object&");
}

public sealed class Options
{ private Options() { }

  public static int  IndentSize=2;
  public static bool Debug=true, Interactive;
}

}

