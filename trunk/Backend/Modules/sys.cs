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
  }

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 'sys' (built-in)>"; }

  public static void exit() { exit(0); }
  public static void exit(object obj) { throw new SystemExitException(obj); }
  
  public static void loadAssemblyByName(string name) { ReflectedPackage.LoadAssemblyByName(name); }
  public static void loadAssemblyFromFile(string filename) { ReflectedPackage.LoadAssemblyFromFile(filename); }

  public static readonly object __displayhook__ =
    new CompiledFunctionN("displayhook", new Parameter[] { new Parameter("value") }, null, new CallTargetN(display));
  public static readonly object __excepthook__; // TODO: implement this
  public static readonly object __stdin__; // TODO: implement this
  public static readonly object __stdout__; // TODO: implement this
  public static readonly object __stderr__; // TODO: implement this

  public static readonly List argv    = new List();
  public static readonly Tuple builtin_module_names = new Tuple("__builtin__", "operator", "string", "sys");

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
  
  public static int recursionlimit = 1000; // TODO: make this take effect
  public static int tracebacklimit = 1000; // TODO: make this take effect

  public static string version = "0.1.0";
  public static Tuple version_info = new Tuple(0, 1, 0, "devel");

  static object display(params object[] values) // TODO: optimize this and use CallTarget1 or something
  { if(values[0]!=null)
    { Console.WriteLine(Ops.Repr(values[0]));
      __builtin__._ = values[0];
    }
    return null;
  }
}

} // namespace Boa.Modules