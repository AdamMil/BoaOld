using System;

namespace Boa.AST
{

public enum Scope : byte { Free, Local, Global, Closed }

public interface IWalker
{ void PostWalk(Node node);
  bool Walk(Node node);
}

public enum ArgType { Normal, List, Dict };
public struct Argument
{ public Argument(Expression expr) { Name=null; Expression=expr; Type=ArgType.Normal; }
  public Argument(string name, Expression expr) { Name=name; Expression=expr; Type=ArgType.Normal; }
  public Argument(Expression expr, ArgType type) { Name=null; Expression=expr; Type=type; }

  public void ToCode(System.Text.StringBuilder sb)
  { if(Name!=null) sb.Append(Name);
    else if(Type==ArgType.List) sb.Append('*');
    else if(Type==ArgType.Dict) sb.Append("**");
    Expression.ToCode(sb, 0);
  }

  public string Name;
  public Expression Expression;
  public ArgType Type;
}

public class ExceptClause : Node
{ public ExceptClause(Expression type, NameExpression target, Statement body) { Type=type; Target=target; Body=body; }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append(Type==null ? "except" : "except ");
    if(Type!=null) Type.ToCode(sb, 0);
    if(Target!=null)
    { sb.Append(", ");
      Target.ToCode(sb, 0);
    }
    sb.Append(": ");
    Body.ToCode(sb, indent+Options.IndentSize);
    if(!(Body is Suite)) sb.Append('\n');
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { if(Type!=null) Type.Walk(w);
      if(Target!=null) Target.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Type;
  public NameExpression Target;
  public Statement  Body;
}

public struct ImportName
{ public ImportName(string name) { Name=name; AsName=null; }
  public ImportName(string name, string asName) { Name=name; AsName=asName; }

  public void ToCode(System.Text.StringBuilder sb)
  { sb.Append(Name);
    if(AsName!=null)
    { sb.Append(" as ");
      sb.Append(AsName);
    }
  }

  public string Name, AsName;
}

public class Name
{ public Name(string name) { String=name; Scope=Scope.Free; }
  public Name(string name, Scope scope) { String=name; Scope=scope; }

  public override int GetHashCode() { return String.GetHashCode(); }

  public string String;
  public Scope  Scope;
}

public abstract class Node
{ public void SetLocation(string source, int line, int column) { Source=source; Line=line; Column=column; }
  public string Source="<unknown>";
  public int Line, Column;
  
  public bool IsConstant
  { get { return (Flags&NodeFlag.Constant)!=0; }
    set { if(value) Flags|=NodeFlag.Constant; else Flags&=~NodeFlag.Constant; }
  }

  public virtual object GetValue() { throw new NotSupportedException(); }
  public virtual void Optimize() { }

  public string ToCode()
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    ToCode(sb, 0);
    return sb.ToString();
  }
  public abstract void ToCode(System.Text.StringBuilder sb, int indent);

  public virtual void Walk(IWalker w)
  { w.Walk(this);
    w.PostWalk(this);
  }

  protected void StatementToCode(System.Text.StringBuilder sb, Statement stmt, int indent)
  { bool isSuite = stmt is Suite;
    if(!isSuite) sb.Append(' ');
    stmt.ToCode(sb, indent);
    if(!isSuite) sb.Append('\n');
  }
  
  [Flags] enum NodeFlag : byte { Constant=1 }
  NodeFlag Flags;
}

public enum ParamType { Required, Optional, List, Dict }

public struct Parameter
{ public Parameter(string name) : this(name, ParamType.Required) { }
  public Parameter(string name, Expression defaultValue) : this(name, ParamType.Optional) { Default=defaultValue; }
  public Parameter(string name, ParamType type) { Name=new Name(name, Scope.Local); Type=type; Default=null; }
  public Parameter(Name name, Expression defaultValue, ParamType type) { Name=name; Default=defaultValue; Type=type; }

  public override int GetHashCode() { return Name.GetHashCode(); }

  public void ToCode(System.Text.StringBuilder sb)
  { if(Type==ParamType.List) sb.Append('*');
    else if(Type==ParamType.Dict) sb.Append("**");
    sb.Append(Name.String);
    if(Default!=null) { sb.Append('='); Default.ToCode(sb, 0); }
  }

  public Name Name;
  public Expression Default;
  public ParamType Type;
}

} // namespace Boa.AST
