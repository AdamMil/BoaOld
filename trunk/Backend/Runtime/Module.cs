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
using System.Collections.Specialized;
using System.Reflection;

namespace Boa.Runtime
{

[BoaType("module")]
public class Module : Boa.AST.Snippet, IHasAttributes, IRepresentable
{ public const string FieldName = "__module";

  public Module() { __dict__ = new Dict(); }
  public Module(IDictionary dict) { __dict__ = dict; }

  public override void Run(Frame frame) { throw new NotImplementedException("Run() not implemented!"); }

  public string __repr__()
  { object name = __dict__["__name__"];
    return name==null ? "<module>" : string.Format("<module {0}>", Ops.Repr(name));
  }
  public override string ToString() { return __repr__(); }

  #region IHasAttributes Members
  public List __attrs__() { return new List(__dict__.Keys); }
  public void __delattr__(string key)
  { if(!Ops.DelDescriptor(__dict__[key], null)) __dict__.Remove(key);
  }
  public object __getattr__(string name)
  { object obj = __dict__[name];
    if(obj!=null || __dict__.Contains(name)) return Ops.GetDescriptor(obj, null);
    return builtins.__getattr__(name);
  }
  public void __setattr__(string key, object value)
  { if(!Ops.SetDescriptor(__dict__[key], null, value)) __dict__[key] = value;
  }
  #endregion

  public readonly IDictionary __dict__;
  
  static readonly ReflectedType builtins = ReflectedType.FromType(typeof(Boa.Modules.__builtin__));
}

} // namespace Boa.Runtime
