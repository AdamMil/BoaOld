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

namespace Boa.Runtime
{

// FIXME: handle nested classes

[BoaType("package")]
public class ReflectedPackage : IHasAttributes
{ public ReflectedPackage(string name) { __name__=name; __dict__=new Dict(); }

  #region IHasAttributes Members
  public List __attrs__() { return __dict__.keys(); }
  public object __getattr__(string key)
  { object obj = __dict__[key];
    return obj!=null || __dict__.Contains(obj) ? obj : Ops.Missing;
  }
  public void __setattr__(string key, object value) { __dict__[key]=value; }
  public void __delattr__(string key) { __dict__.Remove(key); }
  #endregion

  public override string ToString() { return string.Format("<namespace '{0}'>", __name__); }

  public string __name__;
  public Dict __dict__;
  
  public static ReflectedPackage FromNamespace(string ns)
  { string[] bits = ns.Split('.');
    ReflectedPackage rns;
    lock(dict) rns = (ReflectedPackage)dict[bits[0]];
    if(rns==null)
    { rns = new ReflectedPackage(bits[0]);
      lock(dict) dict[bits[0]] = rns;
    }

    ns = bits[0];
    for(int i=1; i<bits.Length; i++)
    { ns = ns+'.'+bits[i];
      ReflectedPackage tp = (ReflectedPackage)rns.__dict__[bits[i]];
      if(tp==null) rns.__dict__[bits[i]] = tp = new ReflectedPackage(ns);
      rns = tp;
    }
    return rns;
  }

  public static ReflectedPackage GetPackage(string name)
  { Initialize(); // do we want to do this?
    lock(dict) return (ReflectedPackage)dict[name];
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
    { if(type.Namespace==null) continue;
      if(p==null || p.__name__!=type.Namespace) p = ReflectedPackage.FromNamespace(type.Namespace);
      p.__dict__[type.Name] = ReflectedType.FromType(type); // do this lazily?
    }
  }

  static void Initialize()
  { if(!initialized)
    { initialized=true;
      LoadAssemblyByName("mscorlib", false);
      LoadAssemblyByName("System", false);
    }
  }

  static Dict dict = new Dict();
  static bool initialized;
}

} // namespace Boa.Runtime