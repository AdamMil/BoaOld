using System;

namespace Boa.Runtime
{

[BoaType("slice")]
public class Slice
{ public Slice(int stop) { start=0; this.stop=stop; step=1; }
  public Slice(int start, int stop) { this.start=start; this.stop=stop; step=1; }
  public Slice(int start, int stop, int step)
  { if(step==0) throw Ops.ValueError("slice(): step cannot be zero");
    this.start=start; this.stop=stop; this.step=step;
  }
  
  public override bool Equals(object obj)
  { Slice o = obj as Slice;
    return o!=null && o.start==start && o.stop==stop && o.step==step;
  }
  
  public override int GetHashCode() { return start.GetHashCode() ^ stop.GetHashCode(); }

  public Tuple indices(int length)
  { return new Tuple(Ops.FixSliceIndex(this.start, length), Ops.FixSliceIndex(this.stop, length), step);
  }

  public readonly int start, stop, step;
}

} // namespace Boa.Runtime