using System;
using System.CodeDom.Compiler;

namespace AdamMil.Boa
{

#region Enums
internal enum Token
{ Period, Comma, BackQuote, LParen, RParen, LBrace, RBrace, LBracket, RBracket, Question, Colon,
  Semicolon, Percent,
  Plus, Minus, Power, Times, FloorDiv, Divide, Increment, Decrement, LeftShift, RightShift,
  BitAnd, BitOr, BitNot, BitXor, LogAnd, LogOr, LogNot, Match, NotMatch, Replace,
  If, Elif, Else, Try, Except, Finally, For, While, Do, Def, Is, In, Break, Raise, Pass, Print, Return, Continue,
  Yield, Import, As, Exec, Class, Struct, Interface, Self, Static, Sealed, Virtual, Override, With, Prop, Get, Set,
  Lambda, And, Or, Not, Typeof, New, Package, Goto, Label, Ref, Enum, Switch, Case, Lock, Event, Delegate, Const,
  Abstract,
  Access, Assign, Compare, TypeName, Identifier, Literal, UnaryMinus, PostIncrement, PostDecrement,
  Call, Member, Index, Slice, Hash, List, Tuple, Pair, Declare, Block, CFor, Attribute, Parameter, Cast, Regex,
  EventAdd, EventRemove,
  Unit, Assembly, EOF
}

[Flags]
internal enum Access
{ None=0, Private=0, Internal=1, Protected=2, Public=3, Family=4,
  Static=8, Sealed=16, Virtual=32, Override=64, Abstract=128, Const=256, New=512,
  AccessMask=7
}

internal enum Assign
{ Assign, LetStar, Plus, Minus, Times, Divide, Modulus, BitAnd, BitOr, BitNot, BitXor, LeftShift, RightShift,
}

internal enum Compare { Less, More, Equal, NotEqual, LessEqual, MoreEqual, Identical, NotIdentical }

internal enum BoaType
{ IntegralStart,
  Byte=IntegralStart, Sbyte, Short, Ushort, Int, Uint, Long, Ulong, IntegralEnd=Ulong,
  Char, Float, Double, Function, Hash, List, String, Var, Void
}
#endregion

#region Node classes
internal class Node
{ public Node(Token token) { Token=token; }
  public Node(Token token, Node n) { Token=token; Add(n); }
  public Node(Token token, Node n1, Node n2) { Token=token; Add(n1); Add(n2); }
  public Node(Token token, params Node[] nodes) { Token=token; foreach(Node n in nodes) Add(n); }
  
  public Node this[int index] { get { return Children[index]; } }

  public void Add(Node node)
  { if(Children==null) Children=new Node[2];
    else if(Count==Children.Length)
    { Node[] na = new Node[Count*2];
      Array.Copy(Children, na, Count);
      Children = na;
    }
    Children[Count++] = node;
  }
  
  public override string ToString() { return ToString(0); }
  public string ToString(int level)
  { string indent = Indentation(level);
    string s = ToBaseString();
    string c = string.Empty;

    for(int i=0; i<Count; i++)
    { if(i!=0) c += "\n"+indent+"  ";
      if(Children[i]!=null) c += Children[i].ToString(level+1);
      else c += "[NULL]";
    }
    if(Count==1 && c.IndexOf('\n')==-1) s += " {"+c+"}";
    else if(Count!=0) s += "\n"+indent+"  "+c;
    return s;
  }
  
  protected virtual string ToBaseString()
  { return string.Format("{0}[{1},{2}]:", Token, Type,
                         Value is string[] ? "["+string.Join(",", (string[])Value)+"]" : Value);
  }

  string Indentation(int level)
  { string s=string.Empty;
    while(level-->0) s += "  ";
    return s;
  }

  public Token  Token;
  public object Value, Type;
  public int    Count;

  Node[] Children;
}

internal class DeclNode : Node
{ public DeclNode(Token token, Access access, object type, string name, Node n) : base(token, n)
  { Access=access; Type=type; Value=name;
  }
  public DeclNode(Token token, Access access, object type, string name, Node n1, Node n2) : base(token, n1, n2)
  { Access=access; Type=type; Value=name;
  }
  public DeclNode(Token token, Access access, object type, string name, params Node[] nodes) : base(token, nodes)
  { Access=access; Type=type; Value=name;
  }

  public string Name { get { return (string)Value; } }
  public Access Access;

  protected override string ToBaseString()
  { return string.Format("{0}[{1},{2},{3}]:", Token, Access.ToString().Replace(", ", "|"), Type, Value);
  }
}

internal class ImportNode : Node
{ public ImportNode(string ns) : base(Token.Import) { Value=ns; }
  public string Name { get { return (string)Value; } }
  public string Alias;
}

internal class ParamNode : Node
{ public ParamNode(bool isref, object type, string name) : base(Token.Parameter) { Ref=isref; Type=type; Value=name; }
  public ParamNode(Token token, object type, string name) : base(token) { Type=type; Value=name; }
  public string Name { get { return (string)Value; } }
  public bool Ref;

  protected override string ToBaseString()
  { return string.Format("{0}[{1}{2},{3}]:", Token, Ref ? "ref," : "", Type, Value);
  }
}

internal class RegexNode : Node
{ public RegexNode(string pattern, Node replace, string flags) : base(Token.Regex, replace)
  { Value=pattern; Flags=flags;
  }
  public string Pattern { get { return (string)Value; } }
  public Node   Replace { get { return this[0]; } }
  public string Flags;

  protected override string ToBaseString() { return string.Format("{0}[/{1}/{2}]:", Token, Value, Flags); }
}
#endregion

internal class ArrayOf
{ public ArrayOf(object type) { Type=type; Dimensions=1; }
  public ArrayOf(object type, int dimensions) { Type=type; Dimensions=dimensions; }
  public object Type;
  public int Dimensions;
  
  public override string ToString()
  { string s = Type.ToString()+'[';
    for(int i=1; i<Dimensions; i++) s += ',';
    return s += ']';
  }
}

internal class ParseTree
{ //public static void CheckSemantics(Node tree, CompilerErrorCollection errors);
  //public static void Decorate(Node tree);
  //public static void Optimize(Node tree);
}

} // namespace AdamMil.Boa

