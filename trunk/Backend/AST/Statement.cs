using System;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Statement
public abstract class Statement : Node
{ public abstract void Emit(CodeGenerator cg);
  public abstract object Execute(Frame frame);
  
  public static void PostProcessForCompile()
  { PostProcess();
    DecorateNames();
  }
  public static void PostProcessForInterpret() { PostProcess(); }

  static void DecorateNames()
  { 
  }
  static void PostProcess() { }
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

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Statement stmt in Statements) w.Walk(stmt);
    w.PostWalk(this);
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

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { w.Walk(LHS);
      w.Walk(RHS);
    }
    w.PostWalk(this);
  }

  public Expression LHS, RHS;
}
#endregion

#region DefStatement
public class DefStatement : Statement
{ public DefStatement(string name, Parameter[] parms, Statement body)
  { Name=new Name(name); Parameters=parms; Body=body;
  }

  public override void Emit(CodeGenerator cg)
  { CodeGenerator impl = MakeImplMethod(cg);

    cg.EmitString(Name.String);
    GetParmsSlot(cg).EmitGet(cg);
    cg.ILG.Emit(OpCodes.Ldnull);
    cg.ILG.Emit(OpCodes.Ldftn, impl.MethodBuilder);
    cg.EmitNew((ConstructorInfo)typeof(CallTarget).GetMember(".ctor")[0]);
    cg.EmitNew(typeof(CompiledFunction), new Type[] { typeof(string), typeof(Parameter[]), typeof(CallTarget) });

    cg.EmitSet(Name);
  }

  public override object Execute(Frame frame)
  { object func = MakeFunction(frame);
    frame.Set(Name.String, func);
    return func;
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this)) w.Walk(Body);
    w.PostWalk(this);
  }

  public Name Name;
  public Parameter[] Parameters;
  public Statement Body;

  Slot GetParmsSlot(CodeGenerator cg)
  { if(namesSlot==null)
    { namesSlot = cg.TypeGenerator.AddStaticSlot(Name.String+"$parms", typeof(Parameter[]));
      CodeGenerator icg = cg.TypeGenerator.GetInitializer();
      ConstructorInfo nci = typeof(Name).GetConstructor(new Type[] { typeof(string), typeof(Name.Flag) });
      ConstructorInfo pci = typeof(Name).GetConstructor(new Type[] { typeof(Name) });

      icg.EmitNewArray(typeof(Parameter), Parameters.Length);
      for(int i=0; i<Parameters.Length; i++)
      { icg.ILG.Emit(OpCodes.Dup);
        icg.EmitInt(i);
        icg.ILG.Emit(OpCodes.Ldelema);
        icg.EmitString(Parameters[i].Name.String);
        icg.EmitInt((int)Parameters[i].Name.Flags);
        icg.EmitNew(nci);
        icg.EmitNew(pci);
        icg.ILG.Emit(OpCodes.Stobj, typeof(Parameter));
      }
      namesSlot.EmitSet(cg);
    }
    return namesSlot;
  }

  CodeGenerator MakeImplMethod(CodeGenerator cg)
  { Name[] names = new Name[Parameters.Length]; 
    for(int i=0; i<Parameters.Length; i++) names[i] = Parameters[i].Name;

    CodeGenerator icg = cg.TypeGenerator.DefineMethod(Name + "$f" + index++, typeof(object),
                                                      new Type[] { typeof(object[]) });
    icg.Namespace = new LocalNamespace(cg.Namespace, icg);
    icg.SetArgs(names);
    Body.Emit(icg);
    icg.EmitReturn(null);
    icg.Finish();
    return icg;
  }

  public object MakeFunction(Frame frame) { return new InterpretedFunction(frame, Name.String, Parameters, Body); }

  Slot namesSlot;
  int index;
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

  public override void Walk(IWalker w)
  { if(w.Walk(this)) w.Walk(Expression);
    w.PostWalk(this);
  }

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

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { w.Walk(Test);
      w.Walk(Body);
      if(Else!=null) w.Walk(Else);
    }
    w.PostWalk(this);
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

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Expression e in Expressions) w.Walk(e);
    w.PostWalk(this);
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

  public override void Walk(IWalker w)
  { if(w.Walk(this) && Expression!=null) w.Walk(Expression);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

} // namespace Boa.AST
