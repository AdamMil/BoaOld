using System;
using System.Collections;
using System.Reflection;

namespace Boa.Runtime
{

[BoaType("package")]
public class ReflectedPackage : IHasAttributes
{ public ReflectedPackage(string name) { __name__=name; __dict__=new Dict(); }

  static ReflectedPackage()
  { LoadAssemblyByName("mscorlib", false);
    LoadAssemblyByName("System", false);
  }

  #region IHasAttributes Members
  public List __attrs__() { return __dict__.keys(); }
  public object __getattr__(string key)
  { return __dict__.Contains(key) ? __dict__[key] : Ops.Missing; // TODO: eliminate double lookup
  }
  public void __setattr__(string key, object value) { __dict__[key]=value; }
  public void __delattr__(string key) { __dict__.Remove(key); }
  #endregion

  public override string ToString() { return string.Format("<namespace '{0}'>", __name__); }

  public string __name__;
  public Dict __dict__;
  
  public static ReflectedPackage FromNamespace(string ns)
  { string[] bits = ns.Split('.');
    ReflectedPackage rns = (ReflectedPackage)dict[bits[0]];
    if(rns==null) dict[bits[0]] = rns = new ReflectedPackage(bits[0]);

    for(int i=1; i<bits.Length; i++)
    { ReflectedPackage tp = (ReflectedPackage)rns.__dict__[bits[i]];
      if(tp==null) rns.__dict__[bits[i]] = tp = new ReflectedPackage(bits[i]);
      rns = tp;
    }
    return rns;
  }

  public static Assembly LoadAssemblyByName(string name) { return LoadAssemblyByName(name, true); }
  public static Assembly LoadAssemblyByName(string name, bool throwOnError)
  { Assembly a=null;
    try { a=Assembly.LoadWithPartialName(name); }
    catch
    { if(!throwOnError) return null;
      throw Ops.RuntimeError("Could not load assembly {0}", name);
    }
    InitAssembly(a);
    return a;
  }

  public static Assembly LoadAssemblyFromFile(string filename) { return LoadAssemblyFromFile(filename, true); }
  public static Assembly LoadAssemblyFromFile(string filename, bool throwOnError)
  { Assembly a=null;
    try { a = Assembly.LoadFrom(filename); }
    catch
    { if(!throwOnError) return null;
      throw Ops.RuntimeError("Could not load assembly from {0}", filename);
    }
    InitAssembly(a);
    return a;
  }

  static void InitAssembly(Assembly a)
  { ReflectedPackage p=null;
    foreach(Type type in a.GetTypes())
    { string ns = type.Namespace==null ? "Unnamed" : type.Namespace;
      if(p==null || p.__name__!=ns) p = ReflectedPackage.FromNamespace(ns);
      p.__dict__[type.Name] = ReflectedType.FromType(type); // do this lazily?
    }
  }

  static Dict dict = new Dict(128);
}

} // namespace Boa.Runtime