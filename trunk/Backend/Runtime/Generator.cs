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

[BoaType("generator")]
public abstract class Generator : IEnumerator
{ public object Current
  { get
    { if(state!=State.In) throw new InvalidOperationException();
      return current;
    }
  }

  public bool MoveNext()
  { try
    { if(state==State.Done || !InnerNext(out current)) { state=State.Done; return false; }
      state=State.In; return true;
    }
    catch(Exception) { state=State.Done; throw; }
  }

  public void Reset() { throw new NotSupportedException(); }
  
  public override string ToString() { return "<generator function>"; }

  protected abstract bool InnerNext(out object current);

  protected int jump = Int32.MaxValue;

  enum State { Before, In, Done }
  object current;
  State  state = State.Before;
}

} // namespace Boa.Runtime