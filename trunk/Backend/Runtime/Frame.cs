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

namespace Boa.Runtime
{

public class Frame
{ public Frame(IDictionary locals, IDictionary globals) { Locals=locals; Module=new Module(globals); }
  public Frame(Module module) { Locals=module.__dict__; Module=module; }
  public Frame(Frame parent) : this(parent, new HybridDictionary()) { }
  public Frame(Frame parent, IDictionary locals)
  { Locals=locals;
    if(parent!=null) { Parent=parent; Module=parent.Module; }
    else { Module=new Module(Locals); }
  }

  public void Delete(string name) { Locals.Remove(name); }

  public object Get(string name) // TODO: eliminate double lookup
  { if(globalNames!=null && globalNames.Contains(name)) return GetGlobal(name);
    if(Locals.Contains(name)) return Locals[name];
    return Parent==null ? GetGlobal(name) : Parent.Get(name);
  }

  public void Set(string name, object value)
  { if(globalNames!=null && globalNames.Contains(name)) Module.__setattr__(name, value);
    else Locals[name] = value;
  }

  public object GetGlobal(string name)
  { object obj = Module.__getattr__(name);
    if(obj==Ops.Missing) throw Ops.NameError("name '{0}' does not exist", name);
    return obj;
  }

  public void MarkGlobal(string name)
  { if(globalNames==null) globalNames = new HybridDictionary();
    globalNames[name] = name;
  }

  public void SetGlobal(string name, object value) { Ops.SetAttr(value, Module, name); }

  public Frame Parent;
  public IDictionary Locals;
  public Module Module;
  
  HybridDictionary globalNames;
}

} // namespace Boa.Runtime
