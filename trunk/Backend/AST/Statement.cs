using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Statement
public abstract class Statement : Node
{ public abstract void Emit(CodeGenerator cg);
  public abstract object Execute(Frame frame);
  
  public void PostProcessForCompile()
  { PostProcess();
    Walk(new NameDecorator());
  }
  public void PostProcessForInterpret() { PostProcess(); }

  void PostProcess() { }

  #region NameDecorator
  // TODO: handle lambda, pragma global, and other types of assignments
  class NameDecorator : IWalker
  { public void PostWalk(Node node)
    { if(node==current)
      { inDef=false;

        ArrayList inherit = innerFuncs.Count==0 ? null : new ArrayList();
        foreach(DefStatement def in innerFuncs)
        { NameDecorator dec = new NameDecorator();
          def.Walk(dec);
          foreach(Name dname in dec.localNames.Values)
            if(dname.Scope==Scope.Free)
            { Name name = (Name)localNames[dname.String];
              if(name!=null)
              { if(name.Scope==Scope.Local) name.Scope=Scope.Closed;
                inherit.Add(name);
              }
              else
              { localNames[dname.String] = dname;
                inherit.Add(dname);
              }
            }
          def.Inherit = (Name[])inherit.ToArray(typeof(Name));
          inherit.Clear();
        }
      }
    }

    public bool Walk(Node node)
    { if(inDef)
      { if(node is DefStatement)
        { DefStatement de = (DefStatement)node;
          innerFuncs.Add(node);
          de.Name = AddLocal(de.Name);
          de.Name.Scope = Scope.Local;
          return false;
        }
        else if(node is NameExpression)
        { NameExpression ne = (NameExpression)node;
          ne.Name = AddLocal(ne.Name);
        }
        else if(node is AssignStatement)
        { AssignStatement ass = (AssignStatement)node;
          if(ass.LHS is NameExpression)
          { NameExpression ne = (NameExpression)ass.LHS;
            ne.Name = AddLocal(ne.Name);
            ne.Name.Scope = Scope.Local;
          }
          else throw Ops.NotImplemented("Unhandled type '{0}' in NameDecorator", node.GetType());
          Walk(ass.RHS);
          return false;
        }
        return true;
      }
      else if(node is Suite) return true;
      else if(node is DefStatement)
      { if(innerFuncs==null)
        { innerFuncs = new ArrayList();
          localNames = new SortedList();
        }
        inDef=true; current=node;

        DefStatement def = (DefStatement)node;
        foreach(Parameter p in def.Parameters) localNames[p.Name.String] = p.Name;
        return true;
      }
      return false;
    }
    
    Name AddLocal(Name name)
    { Name lname = (Name)localNames[name.String];
      if(lname==null) localNames[name.String] = lname = name;
      return lname;
    }

    Node       current;
    ArrayList  innerFuncs;
    SortedList localNames;
    bool inDef;
  }
  #endregion
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
  { if(w.Walk(this)) foreach(Statement stmt in Statements) stmt.Walk(w);
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
    { LHS.Walk(w);
      RHS.Walk(w);
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

    Type targetType = Inherit==null ? typeof(CallTargetN) : typeof(CallTargetFN);
    Type funcType   = Inherit==null ? typeof(CompiledFunctionN) : typeof(CompiledFunctionFN);

    cg.EmitString(Name.String);
    GetParmsSlot(cg).EmitGet(cg);
    EmitClosedGet(cg);
    cg.ILG.Emit(OpCodes.Ldnull); // create delegate
    cg.ILG.Emit(OpCodes.Ldftn, impl.MethodBuilder);
    cg.EmitNew((ConstructorInfo)targetType.GetMember(".ctor")[0]);
    cg.EmitNew(funcType, new Type[] { typeof(string), typeof(Parameter[]), typeof(ClosedVar[]), targetType });
    cg.EmitSet(Name);
    index++;
  }

  public override object Execute(Frame frame)
  { object func = MakeFunction(frame);
    frame.Set(Name.String, func);
    return func;
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this)) Body.Walk(w);
    w.PostWalk(this);
  }

  public Name Name;
  public Parameter[] Parameters;
  public Statement Body;

  void EmitClosedGet(CodeGenerator cg)
  { if(Inherit==null) cg.ILG.Emit(OpCodes.Ldnull);
    else
    { cg.EmitNewArray(typeof(ClosedVar), Inherit.Length);
      ConstructorInfo ci = typeof(ClosedVar).GetConstructor(new Type[] { typeof(string) });
      FieldInfo fi = typeof(ClosedVar).GetField("Value");

      for(int i=0; i<Inherit.Length; i++)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(i);
        Slot slot = cg.Namespace.GetLocalSlot(Inherit[i]);
        ClosedSlot cs = slot as ClosedSlot;
        if(cs!=null) cs.Storage.EmitGet(cg);
        else
        { cg.EmitString(Inherit[i].String);
          cg.EmitNew(ci);
          cg.ILG.Emit(OpCodes.Dup);
          slot.EmitGet(cg);
          cg.EmitFieldSet(fi);
        }
        cg.ILG.Emit(OpCodes.Stelem_Ref);
      }
    }
  }

  Slot GetParmsSlot(CodeGenerator cg)
  { if(namesSlot==null)
    { namesSlot = cg.TypeGenerator.AddStaticSlot(Name.String+"$parms"+index, typeof(Parameter[]));
      CodeGenerator icg = cg.TypeGenerator.GetInitializer();
      ConstructorInfo nci = typeof(Name).GetConstructor(new Type[] { typeof(string), typeof(Scope) });
      ConstructorInfo pci = typeof(Parameter).GetConstructor(new Type[] { typeof(Name) });

      icg.EmitNewArray(typeof(Parameter), Parameters.Length);
      for(int i=0; i<Parameters.Length; i++)
      { icg.ILG.Emit(OpCodes.Dup);
        icg.EmitInt(i);
        icg.ILG.Emit(OpCodes.Ldelema, typeof(Parameter));
        icg.EmitString(Parameters[i].Name.String);
        icg.EmitInt((int)Parameters[i].Name.Scope);
        icg.EmitNew(nci);
        icg.EmitNew(pci);
        icg.ILG.Emit(OpCodes.Stobj, typeof(Parameter));
      }
      namesSlot.EmitSet(icg);
    }
    return namesSlot;
  }

  CodeGenerator MakeImplMethod(CodeGenerator cg)
  { Name[] names = new Name[Parameters.Length]; 
    for(int i=0; i<Parameters.Length; i++) names[i] = Parameters[i].Name;

    Type[] parmTypes = Inherit==null ? new Type[] { typeof(object[]) }
                                     : new Type[] { typeof(CompiledFunction), typeof(object[]) };
    CodeGenerator icg = cg.TypeGenerator.DefineMethod(Name.String + "$f" + index++, typeof(object), parmTypes);
    LocalNamespace ns = new LocalNamespace(cg.Namespace, icg);
    icg.Namespace = ns;
    //icg.SetArgs(names, 1);
    ns.SetArgs(names, icg, new ArgSlot(icg.MethodBuilder, Inherit==null ? 0 : 1, "$names", typeof(object[])));

    if(Inherit!=null && Inherit.Length>0)
    { icg.EmitArgGet(0);
      icg.EmitFieldGet(typeof(CompiledFunction), "Closed");
      for(int i=0; i<Inherit.Length; i++)
      { if(i!=Inherit.Length-1) icg.ILG.Emit(OpCodes.Dup);
        icg.EmitInt(i);
        icg.ILG.Emit(OpCodes.Ldelem_Ref);
        ns.UnpackClosedVar(Inherit[i], icg);
      }
    }
    Body.Emit(icg);
    icg.EmitReturn(null);
    icg.Finish();
    return icg;
  }

  public object MakeFunction(Frame frame) { return new InterpretedFunction(frame, Name.String, Parameters, Body); }
  
  public Name[] Inherit;

  Slot namesSlot;
  static int index;
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
  { if(w.Walk(this)) Expression.Walk(w);
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
    { Test.Walk(w);
      Body.Walk(w);
      if(Else!=null) Else.Walk(w);
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
  { if(w.Walk(this)) foreach(Expression e in Expressions) e.Walk(w);
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
  { if(w.Walk(this) && Expression!=null) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

} // namespace Boa.AST
