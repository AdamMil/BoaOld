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
    return o!=null && start.Equals(o.start) && stop.Equals(o.stop) && step.Equals(o.step);
  }
  
  public override int GetHashCode() { return start.GetHashCode() ^ stop.GetHashCode(); }

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