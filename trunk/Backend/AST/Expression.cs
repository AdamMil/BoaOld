using System;
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

#region BinaryExpression
public abstract class BinaryExpression : Expression
{ public override void Walk(IWalker w)
  { if(w.Walk(this))
    { w.Walk(LHS);
      w.Walk(RHS);
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
  { if(w.Walk(this)) w.Walk(Target);
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

#region NameExpression
public class NameExpression : Expression
{ public NameExpression(Name name) { Name=name; }

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
    { w.Walk(Test);
      w.Walk(IfTrue);
      w.Walk(IfFalse);
    }
    w.PostWalk(this);
  }

  public Expression Test, IfTrue, IfFalse;
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
  { if(w.Walk(this)) w.Walk(Expression);
    w.PostWalk(this);
  }

  public Expression Expression;
  public UnaryOperator Op;
}
#endregion

} // namespace Boa.AST
