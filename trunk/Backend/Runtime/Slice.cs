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

namespace Boa.Runtime
{

[BoaType("slice")]
public class Slice : IRepresentable
{ public Slice() { }
  public Slice(object stop) { this.stop=stop; }
  public Slice(object start, object stop) { this.start=start; this.stop=stop; }
  public Slice(object start, object stop, object step)
  { if(step!=null && Ops.ToInt(step)==0) throw Ops.ValueError("slice(): step cannot be zero");
    this.start=start; this.stop=stop; this.step=step;
  }

  public override bool Equals(object obj)
  { Slice o = obj as Slice;
    return o!=null && (start==null ? o.start==null : start.Equals(o.start)) &&
                      (stop ==null ? o.stop ==null : stop .Equals(o.stop))  &&
                      (step ==null ? o.step ==null : step.Equals(o.step));
  }
  
  public override int GetHashCode()
  { return (start==null ? 0 : start.GetHashCode()) ^ (stop==null ? 0 : stop.GetHashCode());
  }

  public Tuple indices(int length)
  { int step  = (this.step==null ? 1 : Ops.ToInt(this.step));
    int start = (this.start==null ? step>0 ? 0 : length-1 : Ops.FixSliceIndex(Ops.ToInt(this.start), length));
    int stop  = (this.stop==null ? step>0 ? length : -1 : Ops.FixSliceIndex(Ops.ToInt(this.stop), length));
    return new Tuple(start, stop, step);
  }

  public override string ToString() { return __repr__(); }

  public string __repr__()
  { return string.Format("slice({0}, {1}, {2})", Ops.Repr(start), Ops.Repr(stop), Ops.Repr(step));
  }

  public readonly object start, stop, step;
}

} // namespace Boa.Runtime