using System;
using System.IO;
using Boa.AST;
using Boa.Modules;

// TODO: clean up broken module out of sys.modules if import fails
namespace Boa.Runtime
{

public sealed class Importer
{ Importer() { }

  public static object Import(string name) { return Import(name, true, false); }
  public static object Import(string name, bool throwOnError) { return Import(name, throwOnError, false); }
  public static object Import(string name, bool throwOnError, bool returnTop)
  { object ret = sys.modules[name]; // TODO: look at this... for a dotted name, will this ever be true?
    if(ret!=null) return ret;

    string[] names = name.Split('.');
    object top = Load(names[0]), module = top;
    if(top!=null) sys.modules[names[0]] = top;

    for(int i=1; i<names.Length && module!=null; i++) module = Ops.GetAttr(module, names[i]);
    if(returnTop) module=top;
    if(throwOnError && module==null) throw Ops.ImportError("module {0} could not be loaded", name);
    return module;
  }

  public static object ImportTop(string name) { return Import(name, true, true); }
  public static object ImportTop(string name, bool throwOnError) { return Import(name, throwOnError, true); }
  
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
  
  static object LoadFromPath(string name)
  { foreach(string dirname in Boa.Modules.sys.path)
    { string path = Path.Combine(dirname=="" ? Environment.CurrentDirectory : dirname , name);
      if(Directory.Exists(path) && File.Exists(Path.Combine(path, "__init__.boa"))) return LoadPackage(name, path);
      path += ".boa";
      if(File.Exists(path)) return LoadFromSource(name, path, null);
    }
    return null;
  }

  static object LoadFromSource(string name, string filename, List __path__)
  { Module mod = ModuleGenerator.Generate(name, filename, Parser.FromFile(filename).Parse());
    if(__path__!=null) mod.__setattr__("__path__", __path__);
    sys.modules[name] = mod;
    mod.Run(new Frame(mod));
    return mod;
  }

  static object LoadPackage(string name, string path)
  { List __path__ = new List();
    __path__.append(path);
    return LoadFromSource(name, Path.Combine(path, "__init__.boa"), __path__);
  }

  static object LoadReflected(string name) { return ReflectedPackage.GetPackage(name); }
}

} // namespace Boa.Runtime