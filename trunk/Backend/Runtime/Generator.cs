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