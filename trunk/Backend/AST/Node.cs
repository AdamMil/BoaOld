using System;

namespace Boa.AST
{

public interface IWalker
{ void PostWalk(Node node);
  bool Walk(Node node);
}

public struct Argument
{ public Argument(Expression expr) { Expression=expr; }
  public Expression Expression;
}

public class Name
{ public enum Scope : byte { Free, Local, Global, Closed }

  public Name(string name) { String=name; Type=Scope.Free; }
  public Name(string name, Scope type) { String=name; Type=type; }

  public override int GetHashCode() { return String.GetHashCode(); }

  public string String;
  public Scope  Type;
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

public struct Parameter
{ public Parameter(Name name) { Name=name; }
  public Parameter(string name) { Name=new Name(name, Name.Scope.Local); }
  public override int GetHashCode() { return Name.GetHashCode(); }
  public Name Name;
}

} // namespace Boa.AST
