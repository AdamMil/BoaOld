using System;
using System.IO;

namespace Boa.Runtime
{

public sealed class Importer
{ Importer() { }

  public static object Import(string name) { return Import(name, true); }

  public static object Import(string name, bool throwOnError)
  { object ret = Boa.Modules.sys.modules[name];
    if(ret!=null) return ret;

    string[] names = name.Split('.');
    object module = Load(names[0]);
    if(module!=null) Boa.Modules.sys.modules[names[0]] = module;

    for(int i=1; i<names.Length && module!=null; i++) module = Ops.GetAttr(module, names[i]);
    if(throwOnError && module==null) throw Ops.ImportError("module {0} could not be loaded", name);
    return module;
  }
  
  static object Load(string name)
  { object ret = LoadBuiltin(name);
    if(ret!=null) return ret;

    ret = LoadFromPath(name);
    if(ret!=null) return ret;

    ret = LoadReflected(name);
    return ret;
  }
  
  static object LoadBuiltin(string name)
  { Type type = Type.GetType("Boa.Modules."+name);
    return type==null ? null : ReflectedType.FromType(type);
  }
  
  static object LoadFromPath(string name) { throw new NotImplementedException(); }
  static object LoadReflected(string name) { throw new NotImplementedException(); }
}

} // namespace Boa.Runtime