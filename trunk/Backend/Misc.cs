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

// TODO: make use of 'readonly' attribute
// TODO: make use of 'sealed' attribute
// TODO: make parameter names more consistent

using System;
using Boa.AST;

[assembly: System.Reflection.AssemblyKeyFile("Z:/code/RosArt.snk")]

namespace Boa
{

public sealed class Misc
{ private Misc() { }
  
  public static long NextIndex { get { lock(indexLock) return index++; } }

  public unsafe static string ArrayToHex(byte[] bytes)
  { const string hex = "0123456789abcdef";
    System.Text.StringBuilder sb = new System.Text.StringBuilder(bytes.Length*2);

    fixed(char* hp=hex)
    fixed(byte* bp=bytes)
      for(int i=0,len=bytes.Length; i<len; i++)
      { byte b = bp[i];
        sb.Append(hp[b>>4]);
        sb.Append(hp[b&0xF]);
      }
    return sb.ToString();
  }

  public static string BodyToDocString(Statement body)
  { Suite suite = body as Suite;
    if(suite!=null && suite.Statements[0] is ExpressionStatement) // TODO: strip uniform whitespace after second line
    { ExpressionStatement es = (ExpressionStatement)suite.Statements[0];
      if(es.Expression is ConstantExpression) return ((ConstantExpression)es.Expression).Value as string;
    }
    return null;
  }
  public static Type[] MakeTypeArray(Type type, int length)
  { Type[] arr = new Type[length];
    for(int i=0; i<length; i++) arr[i] = type;
    return arr;
  }
  
  public static readonly object[] EmptyArray = new object[0];
  public static readonly Type TypeOfObjectRef = Type.GetType("System.Object&");
  static long index;
  static object indexLock = "<Misc_INDEX_LOCK>";
}

public sealed class Options
{ private Options() { }

  public static int  IndentSize=2;
  public static bool Debug, Optimize, Interactive, NoStdLib;
}

}

