using System;
using System.Collections;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class sys
{ sys() { }
  static sys()
  { modules = new Dict();
    modules["__builtin__"] = Importer.Import("__builtin__");
  }

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 'sys' (built-in)>"; }

  public static Dict modules;
}

} // namespace Boa.Modules