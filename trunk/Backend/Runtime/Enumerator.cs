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

#region BoaCharEnumerator
public class BoaCharEnumerator : IEnumerator
{ public BoaCharEnumerator(string s) { str=s; index=-1; }

  public object Current
  { get
    { if(index<0 || index>=str.Length) throw new InvalidOperationException();
      return new string(str[index], 1);
    }
  }

  public bool MoveNext()
  { if(index>=str.Length-1) return false;
    index++;
    return true;
  }

  public void Reset() { index=-1; }
  
  string str;
  int index;
}
#endregion

#region ISeqEnumerator
public class ISeqEnumerator : IEnumerator
{ public ISeqEnumerator(ISequence seq) { this.seq=seq; index=-1; length=seq.__len__(); }

  public object Current
  { get
    { if(index<0 || index>=length) throw new InvalidOperationException();
      return seq.__getitem__(index);
    }
  }
  
  public bool MoveNext()
  { if(index>=length-1) return false;
    index++;
    return true;
  }
  
  public void Reset() { index=-1; }

  ISequence seq;
  int index, length;
}
#endregion

#region IterEnumerator
public class IterEnumerator : IEnumerator
{ public IterEnumerator(object o)
  { iter = o;
    next = Ops.GetAttr(o, "next");
    Ops.GetAttr(o, "reset", out reset);
  }

  public object Current
  { get
    { if(state!=State.IN) throw new InvalidOperationException();
      return current;
    }
  }

  public bool MoveNext()
  { if(state==State.EOF) return false;
    try { current=Ops.Call(next); state=State.IN; return true; }
    catch(StopIterationException) { state=State.EOF; return false; }
  }

  public void Reset()
  { if(reset==null) throw new NotImplementedException("this iterator does not implement reset()");
    Ops.Call(reset);
    state = State.BOF;
  }

  enum State : byte { BOF, IN, EOF }
  object iter, current, next, reset;
  State state;
}
#endregion

#region SeqEnumerator
public class SeqEnumerator : IEnumerator
{ public SeqEnumerator(object seq)
  { length  = Ops.ToInt(Ops.Invoke(seq, "__length__"));
    getitem = Ops.GetAttr(seq, "__getitem__");
    index   = -1;
  }

  public object Current
  { get
    { if(index<0 || index>=length) throw new InvalidOperationException();
      return current;
    }
  }
  
  public bool MoveNext()
  { if(index>=length-1) return false;
    current = Ops.Call(getitem, ++index);
    return true;
  }
  
  public void Reset() { index=-1; }

  object getitem, current;
  int index, length;
}
#endregion

} // namespace Boa.Runtime