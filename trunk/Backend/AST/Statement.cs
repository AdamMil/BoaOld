using System;
using System.Reflection;
using System.Reflection.Emit;
using Language.Runtime;

namespace Language.AST
{

#region Statement
public abstract class Statement : Node
{ public abstract void Emit(CodeGenerator cg);
  public abstract object Execute(Frame frame);
}
#endregion

#region Suite
public class Suite : Statement
{ public Suite(Statement[] stmts) { Statements=stmts; }
  
  public override void Emit(CodeGenerator cg)
  { foreach(Statement stmt in Statements)
    { //cg.EmitPosition(stmt);
      stmt.Emit(cg);
    }
  }

  public override object Execute(Frame frame)
  { object ret = null;
    foreach(Statement stmt in Statements) ret = stmt.Execute(frame);
    return ret;
  }

  public Statement[] Statements;
}
#endregion

#region AssignStatement
public class AssignStatement : Statement
{ public AssignStatement(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { RHS.Emit(cg);
    LHS.EmitSet(cg);
  }

  public override object Execute(Frame frame)
  { object value = RHS.Evaluate(frame);
    LHS.Assign(value, frame);
    return value;
  }

  public Expression LHS, RHS;
}
#endregion

#region DefStatement
public class DefStatement : Statement
{ public DefStatement(string name, Parameter[] parms, Statement body) { Name=name; Parameters=parms; Body=body; }

  public override void Emit(CodeGenerator cg)
  { string[] parms = new string[Parameters.Length];
    for(int i=0; i<Parameters.Length; i++) parms[i] = Parameters[i].Name;

    CodeGenerator impl = MakeImplMethod(cg, parms);

    cg.EmitString(Name);
    cg.EmitStringArray(parms);
    cg.ILG.Emit(OpCodes.Ldnull);
    cg.ILG.Emit(OpCodes.Ldftn, impl.MethodBuilder);
    cg.EmitNew((ConstructorInfo)typeof(CallTarget).GetMember(".ctor")[0]);
    cg.EmitNew(typeof(CompiledFunction), funcTypes);

    cg.EmitSet(Name);
  }
  
  public override object Execute(Frame frame)
  { object func = MakeFunction(frame);
    frame.Set(Name, func);
    return func;
  }
  
  public string Name;
  public Parameter[] Parameters;
  public Statement Body;

  CodeGenerator MakeImplMethod(CodeGenerator cg, string[] parms)
  { CodeGenerator icg = cg.TypeGenerator.DefineMethod(Name + "$f" + index++, typeof(object),
                                                      new Type[] { typeof(object[]) });
    icg.Namespace = new LocalNamespace(cg.Namespace, icg);
    icg.SetArgs(parms);
    Body.Emit(icg);
    icg.EmitReturn(null);
    icg.Finish();
    return icg;
  }

  public object MakeFunction(Frame frame)
  { string[] parms = new string[Parameters.Length]; // TODO: get rid of this (optimize it away)
    for(int i=0; i<Parameters.Length; i++) parms[i] = Parameters[i].Name;
    return new InterpretedFunction(frame, Name, parms, Body);
  }

  int index;

  static Type[] funcTypes = { typeof(string), typeof(string[]), typeof(CallTarget) };
}
#endregion

#region ExpressionStatement
public class ExpressionStatement : Statement
{ public ExpressionStatement(Expression expr) { Expression=expr; }

  public override void Emit(CodeGenerator cg)
  { Expression.Emit(cg);
    cg.ILG.Emit(OpCodes.Pop);
  }
  public override object Execute(Frame frame) { return Expression.Evaluate(frame); }

  public Expression Expression;
}
#endregion

#region IfStatement
public class IfStatement : Statement
{ public IfStatement(Expression test, Statement body, Statement else_) { Test=test; Body=body; Else=else_; }

  public override void Emit(CodeGenerator cg)
  { Label endLab = cg.ILG.DefineLabel(), elseLab = Else==null ? new Label() : cg.ILG.DefineLabel();
    cg.EmitIsTrue(Test);
    cg.ILG.Emit(OpCodes.Brfalse, Else==null ? endLab : elseLab);
    Body.Emit(cg);
    if(Else!=null)
    { cg.ILG.Emit(OpCodes.Br, endLab);
      cg.ILG.MarkLabel(elseLab);
      Else.Emit(cg);
    }
    cg.ILG.MarkLabel(endLab);
  }

  public override object Execute(Frame frame)
  { if(Ops.IsTrue(Test.Evaluate(frame))) return Body.Execute(frame);
    if(Else!=null) return Else.Execute(frame);
    return null;
  }

  public Expression Test;
  public Statement Body, Else;
}
#endregion

#region PrintStatement
public class PrintStatement : Statement
{ public PrintStatement() { Expressions=null; TrailingNewline=true; }
  public PrintStatement(Expression[] exprs, bool trailingNewline)
  { Expressions=exprs; TrailingNewline=trailingNewline;
  }

  public override void Emit(CodeGenerator cg)
  { if(Expressions!=null)
      foreach(Expression e in Expressions)
      { e.Emit(cg);
        cg.EmitCall(typeof(Ops), "Print");
      }
    if(TrailingNewline || Expressions.Length==0) cg.EmitCall(typeof(Ops), "PrintNewline");
  }

  public override object Execute(Frame frame)
  { if(Expressions!=null) foreach(Expression e in Expressions) Ops.Print(e.Evaluate(frame));
    if(TrailingNewline || Expressions.Length==0) Ops.PrintNewline();
    return null;
  }

  public Expression[] Expressions;
  public bool TrailingNewline;
}
#endregion

#region ReturnStatement
public class ReturnStatement : Statement
{ public ReturnStatement() { }
  public ReturnStatement(Expression expression) { Expression=expression; }

  public override void Emit(CodeGenerator cg) { cg.EmitReturn(Expression); }
  public override object Execute(Frame frame) { return Expression==null ? null : Expression.Evaluate(frame); }

  public Expression Expression;
}
#endregion

} // namespace Language.AST
