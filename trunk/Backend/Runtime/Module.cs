using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace Boa.Runtime
{

public class Module : IHasAttributes
{ public const string FieldName = "__module";

  public Module() { __dict__ = new HybridDictionary(); }
  public Module(IDictionary dict) { __dict__ = dict; }

  #region IHasAttributes Members
  public List __attrs__() { return new List(__dict__.Keys); }
  public void __delattr__(string key) { __dict__.Remove(key); }
  public object __getattr__(string name)
  { if(__dict__.Contains(name)) return __dict__[name]; // TODO: eliminate double lookup
    object ret = builtins.__getattr__(name);
    if(ret==Ops.Missing) throw Ops.AttributeError("no such name '{0}'", name);
    return ret;
  }
  public void __setattr__(string key, object value) { __dict__[key] = value; }
  #endregion

  public readonly IDictionary __dict__;

  static ReflectedType builtins = ReflectedType.FromType(typeof(Boa.Modules.__builtin__));
}

} // namespace Boa.Runtime
