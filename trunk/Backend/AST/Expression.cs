using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Expression
public abstract class Expression : Node
{ public virtual void Assign(object value, Frame frame)
  { throw new NotImplementedException("Assign: "+GetType());
  }

  public abstract void Emit(CodeGenerator cg);
  public virtual void EmitSet(CodeGenerator cg) { throw new NotImplementedException("EmitSet: "+GetType()); }
  public virtual void EmitDel(CodeGenerator cg) { throw new NotImplementedException("EmitDel: "+GetType()); }
  public abstract object Evaluate(Frame frame);
}
#endregion

#region AndExpression
public class AndExpression : BinaryExpression
{ public AndExpression(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { LHS.Emit(cg);
    cg.ILG.Emit(OpCodes.Dup);
    cg.EmitIsTrue();
    Label lab = cg.ILG.DefineLabel();
    cg.ILG.Emit(OpCodes.Brfalse, lab);
    cg.ILG.Emit(OpCodes.Pop);
    RHS.Emit(cg);
    cg.ILG.MarkLabel(lab);
  }

  public override object Evaluate(Frame frame)
  { object value = LHS.Evaluate(frame);
    return Ops.IsTrue(value) ? RHS.Evaluate(frame) : value;
  }
}
#endregion

#region AttrExpression
public class AttrExpression : Expression
{ public AttrExpression(Expression o, string attr) { Object=o; Attribute=attr; }

  public override void Assign(object value, Frame frame) { Ops.SetAttr(value, Object.Evaluate(frame), Attribute); }
  public override void Emit(CodeGenerator cg)
  { Object.Emit(cg);
    cg.EmitString(Attribute);
    cg.EmitCall(typeof(Ops), "GetAttr", new Type[] { typeof(object), typeof(string) });
  }
  public override void EmitSet(CodeGenerator cg)
  { Object.Emit(cg);
    cg.EmitString(Attribute);
    cg.EmitCall(typeof(Ops), "SetAttr", new Type[] { typeof(object), typeof(object), typeof(string) });
  }
  public override object Evaluate(Frame frame) { return Ops.GetAttr(Object.Evaluate(frame), Attribute); }

  public Expression Object;
  public string Attribute;
}
#endregion

#region BinaryExpression
public abstract class BinaryExpression : Expression
{ public override void Walk(IWalker w)
  { if(w.Walk(this))
    { LHS.Walk(w);
      RHS.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression LHS, RHS;
}
#endregion

#region BinaryOpExpression
public class BinaryOpExpression : BinaryExpression
{ public BinaryOpExpression(BinaryOperator op, Expression lhs, Expression rhs) { Op=op; LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { LHS.Emit(cg);
    RHS.Emit(cg);
    Op.Emit(cg);
  }

  public override object Evaluate(Frame frame) { return Op.Evaluate(LHS.Evaluate(frame), RHS.Evaluate(frame)); }

  BinaryOperator Op;
}
#endregion

#region CallExpression
public class CallExpression : Expression
{ public CallExpression(Expression target, Argument[] args) { Arguments=args; Target=target; }

  public override void Emit(CodeGenerator cg)
  { Target.Emit(cg);
    Expression[] exprs = new Expression[Arguments.Length];
    for(int i=0; i<Arguments.Length; i++) exprs[i] = Arguments[i].Expression;
    cg.EmitObjectArray(exprs);
    cg.EmitCall(typeof(Ops), "Call", new Type[] { typeof(object), typeof(object[]) });
  }

  public override object Evaluate(Frame frame)
  { object callee = Target.Evaluate(frame);
    if(Arguments.Length==0) return Ops.Call(callee);

    object[] parms = new object[Arguments.Length];
    for(int i=0; i<Arguments.Length; i++) parms[i] = Arguments[i].Expression.Evaluate(frame);
    return Ops.Call(callee, parms);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Target.Walk(w);
    w.PostWalk(this);
  }

  public Argument[] Arguments;
  public Expression Target;
}
#endregion

#region ConstantExpression
public class ConstantExpression : Expression
{ public ConstantExpression(object value) { Value=value; }
  
  public override void Emit(CodeGenerator cg) { cg.EmitConstant(Value); }
  public override object Evaluate(Frame frame) { return Value; }

  public object Value;
}
#endregion

#region HashExpression
// TODO: check for duplicate entries
public class HashExpression : Expression
{ public HashExpression() : this(new DictionaryEntry[0]) { }
  public HashExpression(DictionaryEntry[] entries) { Entries=entries; }

  public override void Emit(CodeGenerator cg)
  { MethodInfo mi = typeof(Dict).GetMethod("set_Item");
    cg.EmitNew(typeof(Dict));
    foreach(DictionaryEntry e in Entries)
    { cg.ILG.Emit(OpCodes.Dup);
      ((Expression)e.Key).Emit(cg);
      ((Expression)e.Value).Emit(cg);
      cg.EmitCall(mi);
    }
  }

  public override object Evaluate(Frame frame)
  { Dict dict = new Dict();
    foreach(DictionaryEntry e in Entries)
      dict[((Expression)e.Key).Evaluate(frame)] = ((Expression)e.Value).Evaluate(frame);
    return dict;
  }

  public DictionaryEntry[] Entries;
}
#endregion

#region LambdaExpression
public class LambdaExpression : Expression
{ public LambdaExpression(Parameter[] parms, Statement body)
  { if(body is ExpressionStatement) body = new ReturnStatement(((ExpressionStatement)body).Expression);
    Function = new BoaFunction(parms, body);
  }

  public override void Emit(CodeGenerator cg)
  { throw new NotImplementedException();
  }

  public override object Evaluate(Frame frame) { return Function.MakeFunction(frame); }

  public override void Walk(IWalker w) { Function.Walk(w); }

  public BoaFunction Function;
}
#endregion

#region ListExpression
public class ListExpression : Expression
{ public ListExpression() : this(new Expression[0]) { }
  public ListExpression(Expression[] expressions) { Expressions=expressions; }

  public override void Emit(CodeGenerator cg)
  { MethodInfo mi = typeof(List).GetMethod("append");
    cg.EmitInt(Expressions.Length);
    cg.EmitNew(typeof(List), new Type[] { typeof(int) });
    foreach(Expression e in Expressions)
    { cg.ILG.Emit(OpCodes.Dup);
      e.Emit(cg);
      cg.EmitCall(mi);
    }
  }
  
  public override object Evaluate(Frame frame)
  { List list = new List(Expressions.Length);
    foreach(Expression e in Expressions) list.append(e);
    return list;
  }

  public Expression[] Expressions;
}
#endregion

#region ListCompExpression
public class ListCompExpression : Expression
{ public ListCompExpression(Expression item, Name[] names, Expression list, Expression test)
  { Item=item; List=list; Test=test;

    if(names.Length==1) Names = new NameExpression(names[0]);
    else
    { Expression[] ne = new Expression[names.Length];
      for(int i=0; i<names.Length; i++) ne[i] = new NameExpression(names[i]);
      Names=new TupleExpression(ne);
    }
  }

  public override void Emit(CodeGenerator cg)
  { throw new NotImplementedException();
  }

  public override object Evaluate(Frame frame)
  { ConstantExpression ce = new ConstantExpression(null);
    AssignStatement ass = new AssignStatement(Names, ce);
    List list = new List();
    IEnumerator e = Ops.GetEnumerator(List.Evaluate(frame));
    while(e.MoveNext())
    { ce.Value = e.Current;
      ass.Execute(frame);
      if(Test==null || Ops.IsTrue(Test.Evaluate(frame))) list.append(Item.Evaluate(frame));
    }
    return list;
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Item.Walk(w);
      List.Walk(w);
      if(Test!=null) Test.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Item, Names, List, Test;
}
#endregion

#region NameExpression
public class NameExpression : Expression
{ public NameExpression(string name) { Name=new Name(name); }
  public NameExpression(Name name) { Name=name; }

  public override void Assign(object value, Frame frame) { frame.Set(Name.String, value); }
  public override void Emit(CodeGenerator cg) { cg.EmitGet(Name); }
  public override void EmitSet(CodeGenerator cg) { cg.EmitSet(Name); }
  public override object Evaluate(Frame frame) { return frame.Get(Name.String); }

  public Name Name;
}
#endregion

#region OrExpression
public class OrExpression : BinaryExpression
{ public OrExpression(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { LHS.Emit(cg);
    cg.ILG.Emit(OpCodes.Dup);
    cg.EmitIsTrue();
    Label lab = cg.ILG.DefineLabel();
    cg.ILG.Emit(OpCodes.Brtrue, lab);
    cg.ILG.Emit(OpCodes.Pop);
    RHS.Emit(cg);
    cg.ILG.MarkLabel(lab);
  }

  public override object Evaluate(Frame frame)
  { object value = LHS.Evaluate(frame);
    return Ops.IsTrue(value) ? value : RHS.Evaluate(frame);
  }
}
#endregion

#region TernaryExpression
public class TernaryExpression : Expression
{ public TernaryExpression(Expression test, Expression ifTrue, Expression ifFalse)
  { Test=test; IfTrue=ifTrue; IfFalse=ifFalse;
  }

  public override void Emit(CodeGenerator cg)
  { Label end = cg.ILG.DefineLabel(), iff = cg.ILG.DefineLabel();
    Test.Emit(cg);
    cg.EmitIsTrue();
    cg.ILG.Emit(OpCodes.Brfalse, iff);
    IfTrue.Emit(cg);
    cg.ILG.Emit(OpCodes.Br, end);
    cg.ILG.MarkLabel(iff);
    IfFalse.Emit(cg);
    cg.ILG.MarkLabel(end);
  }
  
  public override object Evaluate(Frame frame)
  { return Ops.IsTrue(Test.Evaluate(frame)) ? IfTrue.Evaluate(frame) : IfFalse.Evaluate(frame);
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Test.Walk(w);
      IfTrue.Walk(w);
      IfFalse.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Test, IfTrue, IfFalse;
}
#endregion

#region TupleExpression
public class TupleExpression : Expression
{ public TupleExpression() : this(new Expression[0]) { }
  public TupleExpression(Expression[] expressions) { Expressions=expressions; }

  public override void Assign(object value, Frame frame)
  { if(value is ISequence)
    { ISequence seq = (ISequence)value;
      if(seq.__len__() != Expressions.Length) throw Ops.ValueError("too many values to unpack");
      for(int i=0; i<Expressions.Length; i++) Expressions[i].Assign(seq.__getitem__(i), frame);
    }
    else if(value is string)
    { string s = (string)value;
      if(s.Length != Expressions.Length) throw Ops.ValueError("too many values to unpack");
      for(int i=0; i<Expressions.Length; i++) Expressions[i].Assign(new string(s[i], 1), frame);
    }
    else // assume it's a sequence
    { object getitem = Ops.GetAttr(value, "__getitem__");
      if(Ops.ToInt(Ops.Call(Ops.GetAttr(value, "__length__")))!=Expressions.Length)
        throw Ops.ValueError("too many values to unpack");
      for(int i=0; i<Expressions.Length; i++) Expressions[i].Assign(Ops.Call(getitem, i), frame);
    }
  }

  public override void Emit(CodeGenerator cg)
  { cg.EmitObjectArray(Expressions);
    cg.EmitNew(typeof(Tuple), new Type[] { typeof(object[]) });
  }
  public override object Evaluate(Frame frame)
  { object[] arr = new object[Expressions.Length];
    for(int i=0; i<arr.Length; i++) arr[i] = Expressions[i].Evaluate(frame);
    return new Tuple(arr);
  }

  public Expression[] Expressions;
}
#endregion

#region UnaryExpression
public class UnaryExpression : Expression
{ public UnaryExpression(Expression expr, UnaryOperator op) { Expression=expr; Op=op; }

  public override object Evaluate(Frame frame) { return Op.Evaluate(Expression.Evaluate(frame)); }
  public override void Emit(CodeGenerator cg)
  { Expression.Emit(cg);
    Op.Emit(cg);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
  public UnaryOperator Op;
}
#endregion

} // namespace Boa.AST
