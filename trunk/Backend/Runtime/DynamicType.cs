/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

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

namespace Boa.Runtime
{

public abstract class DynamicType
{ public virtual void DelAttr(object self, string name) { throw new NotSupportedException(); }

  public object GetAttr(object self, string name)
  { object value;
    if(GetAttr(self, name, out value)) return value;
    throw Ops.AttributeError("'{0}' object has no attribute '{1}'", __name__, name);
  }

  public virtual bool GetAttr(object self, string name, out object value)
  { value = null;
    return false;
  }

  public virtual List GetAttrNames(object self) { return new List(0); }

  public virtual bool IsSubclassOf(object other) { throw new NotSupportedException(); }

  public virtual string Repr(object self)
  { object ret;
    if(Ops.TryInvoke(self, "__repr__", out ret)) return (string)ret;
    return Ops.Str(self);
  }
  
  public virtual void SetAttr(object self, string name, object value)
  { throw Ops.AttributeError("'{0}' object has no attribute '{1}'", __name__, name);
  }

  public object __name__;
}

public class NullType : DynamicType
{ NullType() { __name__ = "NullType"; }

  public override bool IsSubclassOf(object other) { return other==this; }

  public static readonly NullType Value = new NullType();
}

} // namespace Boa.Runtime