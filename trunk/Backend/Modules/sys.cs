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
using System.Collections;
using System.Reflection;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class sys
{ sys() { }
  static sys()
  { modules["__builtin__"] = Importer.Import("__builtin__");

    if(Options.Interactive) path.append("");
    else path.append(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

    string lib = Environment.GetEnvironmentVariable("BOA_LIB_PATH");
    if(lib!=null && lib!="") path.append(lib);

    path.append(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                System.IO.Path.DirectorySeparatorChar + "lib");
  }

  public static string __repr__() { return "<module 'sys' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  public static void exit() { exit(0); }
  public static void exit(object obj) { throw new SystemExitException(obj); }
  
  public static void loadAssemblyByName(string name) { ReflectedPackage.LoadAssemblyByName(name); }
  public static void loadAssemblyFromFile(string filename) { ReflectedPackage.LoadAssemblyFromFile(filename); }

  public static readonly object __displayhook__ =
    Ops.GenerateFunction("displayhook", new Parameter[] { new Parameter("value") }, new CallTargetN(display));
  public static readonly object __excepthook__; // TODO: implement this
  public static readonly object __stdin__  = new BoaFile(Console.OpenStandardInput());
  public static readonly object __stdout__ = new BoaFile(Console.OpenStandardOutput());
  public static readonly object __stderr__ = new BoaFile(Console.OpenStandardError());

  public static readonly List argv = new List();

  public static readonly Tuple builtin_module_names =
    new Tuple("__builtin__", "binascii", "bisect", "codecs", "dotnet", "dotnetpath", "math", "md5",
              "operator", "os", "random", "re", "socket", "string", "struct", "sys", "time", "types");

  #if BIGENDIAN
  public static string byteorder = "big";
  #else
  public static string byteorder = "little";
  #endif
  
  public static string copyright = "Boa, Copyright 2004 Adam Milazzo";

  public static object displayhook = __displayhook__;
  public static object excepthook = __excepthook__;
  public static string executable; // TODO: implement this
  public static object exitfunc;
  
  public static int hexversion = 0x00010000; // 0.1.0.0
  public static int maxint = int.MaxValue;
  public static int maxunicode = (int)char.MaxValue;

  public static readonly Dict modules = new Dict();
  public static readonly List path    = new List();
  public static string platform = Environment.OSVersion.Platform.ToString();
  public static string ps1 = ">>> ";
  public static string ps2 = "... ";
  
  public static object stdin  = __stdin__;
  public static object stdout = __stdout__;
  public static object stderr = __stderr__;
  
  public static List warnoptions = new List(); // TODO: populate this list on startup
  
  public static int recursionlimit = 1000; // TODO: make this take effect
  public static int tracebacklimit = 1000; // TODO: make this take effect

  public static string version = "0.2.0";
  public static Tuple version_info = new Tuple(0, 2, 0, "devel");

  internal static Stack Exceptions = new Stack();

  static object display(params object[] values) // TODO: optimize this and use CallTarget1 or something
  { if(values[0]!=null)
    { Console.WriteLine(Ops.Repr(values[0]));
      __builtin__._ = values[0];
    }
    return null;
  }
}

} // namespace Boa.Modules