using System;

namespace Boa.AST
{

public interface IWalker
{ void PostWalk(Node node);
  bool Walk(Node node);
}

public abstract class Node
{ public void SetLocation(string source, int line, int column) { Source=source; Line=line; Column=column; }
  public string Source="<unknown>";
  public int Line, Column;
  
  public virtual void Walk(IWalker w)
  { w.Walk(this);
    w.PostWalk(this);
  }
}

} // namespace Boa.AST
