using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

// TODO: implement module documentation strings
namespace Boa.Runtime
{

[BoaType("module")]
public class Module : Boa.AST.Snippet, IHasAttributes, IRepresentable
{ public const string FieldName = "__module";

  public Module() { __dict__ = new Dict(); }
  public Module(IDictionary dict) { __dict__ = dict; }

  public override object Run(Frame frame) { throw new NotImplementedException("Run() not implemented!"); }

  public string __repr__()
  { object name = __dict__["__name__"];
    return name==null ? "<module>" : string.Format("<module {0}>", Ops.Repr(name));
  }
  public override string ToString() { return __repr__(); }

  #region IHasAttributes Members
  public List __attrs__() { return new List(__dict__.Keys); }
  public void __delattr__(string key) { __dict__.Remove(key); }
  public object __getattr__(string name)
  { if(__dict__.Contains(name)) return __dict__[name]; // TODO: eliminate double lookup
    return builtins.__getattr__(name);
  }
  public void __setattr__(string key, object value) { __dict__[key] = value; }
  #endregion

  public readonly IDictionary __dict__;
  
  static ReflectedType builtins = ReflectedType.FromType(typeof(Boa.Modules.__builtin__));
}

} // namespace Boa.Runtime
