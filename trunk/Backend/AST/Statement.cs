using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

// TODO: possibly rework compiled closures
// TODO: using exceptions is very slow
// TODO: add versions optimized for System.Array ?
#region Exceptions (used to aid implementation)
public class BreakException : Exception
{ public static BreakException Value = new BreakException();
}
public class ContinueException : Exception
{ public static ContinueException Value = new ContinueException();
}
public class ReturnException : Exception
{ public ReturnException(object value) { Value=value; }
  public object Value;
}
#endregion

#region Walkers
// TODO: implement 'break' and 'continue' inside 'finally' blocks
#region JumpFinder
class JumpFinder : IWalker
{ public JumpFinder(Label start, Label end) { this.start=start; this.end=end; }

  public void PostWalk(Node node)
  { if(node is TryStatement) inTry--;
  }

  public bool Walk(Node node)
  { if(node is BreakStatement)
    { BreakStatement bs = (BreakStatement)node;
      bs.Label = end;
      bs.NeedsLeave = InTry;
    }
    else if(node is ContinueStatement)
    { ContinueStatement cs = (ContinueStatement)node;
      cs.Label = start;
      cs.NeedsLeave = InTry;
    }
    else if(node is WhileStatement || node is ForStatement) return false;
    else if(node is TryStatement) inTry++;
    return true;
  }

  bool InTry { get { return inTry>0; } }

  Label start, end;
  int inTry;
}
#endregion

#region Optimizer
public class Optimizer : IWalker
{ public void PostWalk(Node n) { n.Optimize(); }
  public bool Walk(Node n) { return true; }
}
#endregion
#endregion

#region Statement
public abstract class Statement : Node
{ public abstract void Emit(CodeGenerator cg);
  public abstract void Execute(Frame frame);
  
  public void PostProcessForCompile()
  { PostProcess();
    Walk(new NameDecorator());
  }
  public void PostProcessForInterpret() { PostProcess(); }

  void PostProcess()
  { Walk(new TryChecker());
    if(!Options.Debug) Walk(new Optimizer());
  }

  #region NameDecorator
  class NameDecorator : IWalker
  { public void PostWalk(Node node)
    { if(node==current)
      { inDef=false;

        ArrayList inherit = innerFuncs.Count==0 ? null : new ArrayList();
        foreach(BoaFunction func in innerFuncs)
        { NameDecorator dec = new NameDecorator();
          func.Walk(dec);
          foreach(Name dname in dec.names.Values)
            if(dname.Scope==Scope.Free)
            { Name name = (Name)names[dname.String];
              if(name==null)
              { names[dname.String] = dname;
                inherit.Add(dname);
              }
              else if(name.Scope==Scope.Local)
              { name.Scope=Scope.Closed;
                inherit.Add(name);
              }
            }
          if(inherit.Count>0)
          { func.Inherit = (Name[])inherit.ToArray(typeof(Name));
            inherit.Clear();
          }
        }
      }
    }

    public bool Walk(Node node)
    { while(node is ParenExpression) node = ((ParenExpression)node).Expression;

      if(inDef)
      { if(node is BoaFunction)
        { BoaFunction fun = (BoaFunction)node;
          innerFuncs.Add(fun);
          if(fun.Name!=null)
          { fun.Name = AddName(fun.Name);
            fun.Name.Scope = Scope.Local;
          }
          return false;
        }
        else if(node is AssignStatement)
          foreach(Expression e in ((AssignStatement)node).LHS) HandleAssignment(e);
        else if(node is ForStatement) HandleAssignment(((ForStatement)node).Names);
        else if(node is ListCompExpression) HandleAssignment(((ListCompExpression)node).Names);
        else if(node is TryStatement)
        { foreach(ExceptClause ec in ((TryStatement)node).Except) if(ec.Target!=null) HandleAssignment(ec.Target);
        }
        else if(node is NameExpression)
        { NameExpression ne = (NameExpression)node;
          ne.Name = AddName(ne.Name);
        }
      }
      else if(node is BoaFunction)
      { if(innerFuncs==null)
        { innerFuncs = new ArrayList();
          names = new SortedList();
        }

        BoaFunction func = (BoaFunction)node;
        foreach(Parameter p in func.Parameters)
        { if(func.Globals!=null)
            for(int i=0; i<func.Globals.Length; i++)
              if(func.Globals[i].String==p.Name.String)
                throw Ops.SyntaxError(node, "'{0}' is both local and global", p.Name.String);
          names[p.Name.String] = p.Name;
        }
        current=func; inDef=true;
      }
      return true;
    }

    Name AddName(Name name)
    { Name lname = (Name)names[name.String];
      if(lname==null)
      { names[name.String] = lname = name;
        if(current.Globals!=null)
          for(int i=0; i<current.Globals.Length; i++)
            if(current.Globals[i].String==name.String)
            { name.Scope = Scope.Global;
              break;
            }
      }
      return lname;
    }
    
    void HandleAssignment(Expression assignedTo)
    { while(assignedTo is ParenExpression) assignedTo = ((ParenExpression)assignedTo).Expression;
      if(assignedTo is NameExpression)
      { NameExpression ne = (NameExpression)assignedTo;
        ne.Name = AddName(ne.Name);
        if(ne.Name.Scope!=Scope.Global) ne.Name.Scope = Scope.Local;
      }
      else if(assignedTo is TupleExpression)
        foreach(Expression e in ((TupleExpression)assignedTo).Expressions) HandleAssignment(e);
    }

    BoaFunction current;
    ArrayList  innerFuncs;
    SortedList names;
    bool inDef;
  }
  #endregion
  
  #region TryChecker
  public class TryChecker : IWalker
  { public void PostWalk(Node node) { }

    public bool Walk(Node node)
    { if(inTry)
      { if(node is TryStatement)
        { if(nested==null) nested = new ArrayList();
          nested.Add(node);
        }
        return true;
      }
      else if(node is TryStatement)
      { TryStatement ts = (TryStatement)node;
        ts.Body.Walk(this);
        if(ts.Else!=null) ts.Else.Walk(this);
        if(ts.Finally!=null) ts.Finally.Walk(this);

        inTry = true;
        foreach(ExceptClause ec in ts.Except) ec.Body.Walk(this);
        inTry = false;

        if(nested!=null && nested.Count>0)
        { TryStatement[] nest = (TryStatement[])nested.ToArray(typeof(TryStatement));
          nested.Clear();
          foreach(TryStatement nts in nest) nts.Walk(this);
        }
        return false;
      }
      else if(node is RaiseStatement)
      { if(((RaiseStatement)node).Expression==null)
          throw Ops.SyntaxError(node, "expression-less 'raise' can only occur inside 'except' block");
      }
      else return true;
      return false;
    }

    ArrayList nested;
    bool inTry;
  }
  #endregion
}
#endregion

#region AssertStatement
public class AssertStatement : Statement
{ public AssertStatement(Expression e) { Expression=e; }
  
  public override void Emit(CodeGenerator cg)
  { if(Options.Debug)
    { Label good = cg.ILG.DefineLabel();
      Expression.Emit(cg);
      cg.EmitCall(typeof(Ops), "IsTrue");
      cg.ILG.Emit(OpCodes.Brtrue, good);
      cg.EmitString("assertion failed: ");
      cg.EmitString(Expression.ToCode());
      cg.EmitCall(typeof(string), "Concat", new Type[] { typeof(string), typeof(string) });
      cg.EmitNew(typeof(AssertionErrorException));
      cg.ILG.Emit(OpCodes.Dup);
      cg.EmitString(Source);
      cg.EmitInt(Line);
      cg.EmitInt(Column);
      cg.EmitCall(typeof(AssertionErrorException), "SetPosition");
      cg.ILG.Emit(OpCodes.Throw);
      cg.ILG.MarkLabel(good);
    }
  }
  
  public override void Execute(Frame frame)
  { if(Options.Debug && !Ops.IsTrue(Expression.Evaluate(frame)))
      throw Ops.AssertionError(this, "assertion failed: "+Expression.ToCode());
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("assert ");
    Expression.ToCode(sb, 0);
    sb.Append('\n');
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

#region AssignStatement
public class AssignStatement : Statement
{ public AssignStatement() { }
  public AssignStatement(Expression lhs) { LHS=new Expression[] { lhs }; }
  public AssignStatement(Expression lhs, Expression rhs) : this(lhs) { RHS=rhs; }
  public AssignStatement(Expression[] lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { bool leftAllTups = RHS is TupleExpression;
    if(leftAllTups) foreach(Expression e in LHS) if(!(e is TupleExpression)) { leftAllTups=false; break; }
    if(leftAllTups && RHS is TupleExpression)
    { TupleExpression rhs = (TupleExpression)RHS;
      TupleExpression[] lhs = new TupleExpression[LHS.Length];
      for(int i=0; i<LHS.Length; i++)
      { lhs[i] = (TupleExpression)LHS[i];
        if(lhs[i].Expressions.Length != rhs.Expressions.Length)
          throw Ops.ValueError(this, "wrong number of values to unpack");
      }

      for(int i=0; i<rhs.Expressions.Length; i++) rhs.Expressions[i].Emit(cg);
      for(int i=rhs.Expressions.Length-1; i>=0; i--)
        for(int j=lhs.Length-1; j>=0; j--)
        { if(j!=0) cg.ILG.Emit(OpCodes.Dup);
          lhs[j].Expressions[i].EmitSet(cg);
        }
    }
    else
    { object value=null;
      bool emitted=true;
      if(RHS!=null)
      { if(RHS.IsConstant) { value=RHS.GetValue(); emitted=false; }
        else RHS.Emit(cg);
      }

      for(int ti=LHS.Length-1; ti>=0; ti--)
      { if(LHS[ti] is TupleExpression)
        { TupleExpression lhs = (TupleExpression)LHS[ti];
          if(IsConstant)
          { if(value is ISequence)
            { if(((ISequence)value).__len__() != lhs.Expressions.Length)
                throw Ops.ValueError(this, "wrong number of values to unpack");
            }
            else if(value is string)
            { if(((string)value).Length != lhs.Expressions.Length)
                throw Ops.ValueError(this, "wrong number of values to unpack");
            }
            else
            { object len;
              if(!Ops.TryInvoke(value, "__len__", out len))
                throw Ops.TypeError(this, "expecting a sequence on the right side of a tuple assignment");
              if(Ops.ToInt(len) != lhs.Expressions.Length)
                throw Ops.ValueError(this, "wrong number of values to unpack");
            }
            IEnumerator e = Ops.GetEnumerator(value);
            int i=0;
            while(e.MoveNext() && i<lhs.Expressions.Length)
            { cg.EmitConstant(e.Current);
              lhs.Expressions[i++].EmitSet(cg);
            }
            if(emitted && ti==0) cg.ILG.Emit(OpCodes.Pop);
          }
          else
          { // TODO: maybe reduce this to a single case that uses an enumerator (after checking lengths)?
            // TODO: consider removing the string case by converting it to an ISequence with SequenceWrapper
            #region lots of MSIL code here
            Label notseq = cg.ILG.DefineLabel(), notstr = cg.ILG.DefineLabel(), badlen = cg.ILG.DefineLabel(),
                  end = cg.ILG.DefineLabel();
            Slot lenslot = cg.AllocLocalTemp(typeof(int)), seqslot = cg.AllocLocalTemp(typeof(ISequence)),
                strslot = cg.AllocLocalTemp(typeof(string)), objslot = cg.AllocLocalTemp(typeof(object));

            if(!emitted) { cg.EmitConstant(value); emitted=true; }
            if(ti!=0) cg.ILG.Emit(OpCodes.Dup);
            cg.ILG.Emit(OpCodes.Dup);
            cg.ILG.Emit(OpCodes.Isinst, typeof(ISequence)); // this does return a valid object. use it somehow?
            cg.ILG.Emit(OpCodes.Brfalse, notseq);
            // it's a sequence
            cg.ILG.Emit(OpCodes.Castclass, typeof(ISequence));  
            cg.ILG.Emit(OpCodes.Dup);
            seqslot.EmitSet(cg);
            cg.EmitCall(typeof(IContainer), "__len__");
            cg.ILG.Emit(OpCodes.Dup);
            lenslot.EmitSet(cg);
            cg.EmitInt(lhs.Expressions.Length);
            cg.ILG.Emit(OpCodes.Ceq);
            cg.ILG.Emit(OpCodes.Brfalse, badlen);
            lenslot.EmitGet(cg);
            cg.EmitInt(0);
            cg.ILG.Emit(OpCodes.Ceq);
            cg.ILG.Emit(OpCodes.Brtrue, end);
            // sequence is the right length and >0
            seqslot.EmitGet(cg);
            for(int i=0; i<lhs.Expressions.Length; i++)
            { if(i!=lhs.Expressions.Length-1) cg.ILG.Emit(OpCodes.Dup);
              cg.EmitInt(i);
              cg.EmitCall(typeof(ISequence), "__getitem__", new Type[] { typeof(int) });
              lhs.Expressions[i].EmitSet(cg);
            }
            cg.ILG.Emit(OpCodes.Br, end);
            cg.ILG.MarkLabel(notseq);
            // it's not a sequence. maybe it's a string
            cg.ILG.Emit(OpCodes.Dup);
            cg.ILG.Emit(OpCodes.Isinst, typeof(string));
            cg.ILG.Emit(OpCodes.Brfalse, notstr);
            // it's a string, cast and check the length
            cg.ILG.Emit(OpCodes.Castclass, typeof(string));
            cg.ILG.Emit(OpCodes.Dup);
            strslot.EmitSet(cg);
            cg.EmitPropGet(typeof(string), "Length");
            cg.ILG.Emit(OpCodes.Dup);
            lenslot.EmitSet(cg);
            cg.EmitInt(lhs.Expressions.Length);
            cg.ILG.Emit(OpCodes.Ceq);
            cg.ILG.Emit(OpCodes.Brfalse, badlen);
            lenslot.EmitGet(cg);
            cg.EmitInt(0);
            cg.ILG.Emit(OpCodes.Ceq);
            cg.ILG.Emit(OpCodes.Brtrue, end);
            // it's a string of the right length (>0)
            strslot.EmitGet(cg);
            for(int i=0; i<lhs.Expressions.Length; i++)
            { if(i!=lhs.Expressions.Length-1) cg.ILG.Emit(OpCodes.Dup);
              cg.EmitInt(i);
              cg.EmitCall(typeof(string), "get_Chars");
              cg.EmitInt(1);
              cg.EmitNew(typeof(string), new Type[] { typeof(char), typeof(int) });
              lhs.Expressions[i].EmitSet(cg);
            }
            cg.ILG.Emit(OpCodes.Br, end);
            cg.ILG.MarkLabel(notstr);
            // it's not a string. assume it's a dynamic sequence
            cg.ILG.Emit(OpCodes.Dup);
            objslot.EmitSet(cg); // just sticking the object here temporarily
            cg.EmitString("__len__");
            cg.EmitCall(typeof(Ops), "GetAttr", new Type[] { typeof(object), typeof(string) });
            cg.EmitCall(typeof(Ops), "Call0");
            cg.EmitCall(typeof(Ops), "ToInt");
            cg.ILG.Emit(OpCodes.Dup);
            lenslot.EmitSet(cg);
            cg.EmitInt(lhs.Expressions.Length);
            cg.ILG.Emit(OpCodes.Ceq);
            cg.ILG.Emit(OpCodes.Brfalse, badlen);
            lenslot.EmitGet(cg);
            cg.EmitInt(0);
            cg.ILG.Emit(OpCodes.Ceq);
            cg.ILG.Emit(OpCodes.Brtrue, end);
            // well, it has a __len__ attribute and it returns the correct non-zero length
            objslot.EmitGet(cg);
            cg.EmitString("__getitem__");
            cg.EmitCall(typeof(Ops), "GetAttr", new Type[] { typeof(object), typeof(string) });
            for(int i=0; i<lhs.Expressions.Length; i++)
            { if(i!=lhs.Expressions.Length-1) cg.ILG.Emit(OpCodes.Dup);
              cg.EmitInt(i);
              cg.EmitCall(typeof(Ops), "Call1");
              lhs.Expressions[i].EmitSet(cg);
            }
            cg.ILG.Emit(OpCodes.Br, end);
            cg.ILG.MarkLabel(badlen);
            cg.EmitString("wrong number of values to unpack (expected {0}, got {1})");
            cg.EmitNewArray(typeof(object), 2);
            cg.ILG.Emit(OpCodes.Dup);
            cg.EmitInt(0);
            cg.EmitInt(lhs.Expressions.Length);
            cg.ILG.Emit(OpCodes.Box, typeof(int));
            cg.ILG.Emit(OpCodes.Stelem_Ref);
            cg.ILG.Emit(OpCodes.Dup);
            cg.EmitInt(1);
            lenslot.EmitGet(cg);
            cg.ILG.Emit(OpCodes.Box, typeof(int));
            cg.ILG.Emit(OpCodes.Stelem_Ref);
            cg.EmitCall(typeof(Ops), "ValueError", new Type[] { typeof(string), typeof(object[]) });
            cg.ILG.Emit(OpCodes.Dup);
            cg.EmitString(Source);
            cg.EmitInt(Line);
            cg.EmitInt(Column);
            cg.EmitCall(typeof(ValueErrorException), "SetPosition",
                        new Type[] { typeof(string), typeof(int), typeof(int) });
            cg.ILG.Emit(OpCodes.Throw);
            cg.ILG.MarkLabel(end); // phew!

            cg.FreeLocalTemp(lenslot);
            cg.FreeLocalTemp(seqslot);
            cg.FreeLocalTemp(strslot);
            cg.FreeLocalTemp(objslot);
            #endregion
          }
        }
        else
        { if(!emitted) { cg.EmitConstant(value); emitted=true; }
          if(ti!=0) cg.ILG.Emit(OpCodes.Dup);
          LHS[ti].EmitSet(cg);
        }
      }
    }
  }

  public override void Execute(Frame frame)
  { object value = RHS.Evaluate(frame);
    for(int i=LHS.Length-1; i>=0; i--) LHS[i].Assign(value, frame);
  }

  public override object GetValue() { return RHS.GetValue(); }

  public override void Optimize() { IsConstant = RHS!=null && RHS.IsConstant; }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { foreach(Expression e in LHS)
    { e.ToCode(sb, 0);
      sb.Append(" = ");
    }
    RHS.ToCode(sb, 0);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { foreach(Expression e in LHS) e.Walk(w);
      RHS.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression[] LHS;
  public Expression   RHS;
}
#endregion

#region BreakStatement
public class BreakStatement : JumpStatement
{ public override void Execute(Frame frame) { throw BreakException.Value; }
  public override void ToCode(System.Text.StringBuilder sb, int indent) { sb.Append("break"); }
}
#endregion

#region ClassStatement
public class ClassStatement : Statement
{ public ClassStatement(string name, Expression[] bases, Statement body)
  { Name  = new Name(name);
    Bases = bases;
    Body  = body;
  }
  
  public override void Emit(CodeGenerator cg)
  { throw new NotImplementedException();
  }

  public override void Execute(Frame frame)
  { 
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("class ");
    sb.Append(Name.String);
    sb.Append(" (");
    for(int i=0; i<Bases.Length; i++)
    { if(i!=0) sb.Append(", ");
      Bases[i].ToCode(sb, 0);
    }
    sb.Append("):");
    StatementToCode(sb, Body, indent+Options.IndentSize);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { foreach(Expression e in Bases) e.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  public Name Name;
  public Expression[] Bases;
  public Statement Body;
}
#endregion

#region ContinueStatement
public class ContinueStatement : JumpStatement
{ public override void Execute(Frame frame) { throw ContinueException.Value; }
  public override void ToCode(System.Text.StringBuilder sb, int indent) { sb.Append("continue"); }
}
#endregion

#region DefStatement
public class DefStatement : Statement
{ public DefStatement(string name, Parameter[] parms, Statement body)
  { Function = new BoaFunction(this, name, parms, body);
    GlobalFinder gf = new GlobalFinder();
    Function.Body.Walk(gf);
    Function.Globals = gf.Globals==null || gf.Globals.Count==0 ? null : (Name[])gf.Globals.ToArray(typeof(Name));
  }

  public override void Emit(CodeGenerator cg)
  { Slot funcSlot = cg.Namespace.GetSlotForSet(Function.Name);
    Function.Emit(cg);
    funcSlot.EmitSet(cg);
  }

  public override void Execute(Frame frame) { frame.Set(Function.FuncName, Function.MakeFunction(frame)); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("def ");
    sb.Append(Function.Name.String);
    sb.Append("(");
    for(int i=0; i<Function.Parameters.Length; i++)
    { if(i!=0) sb.Append(", ");
      Function.Parameters[i].ToCode(sb);
    }
    sb.Append("):");
    StatementToCode(sb, Function.Body, indent+Options.IndentSize);
  }

  public override void Walk(IWalker w) { Function.Walk(w); }

  public BoaFunction Function;

  #region GlobalFinder
  class GlobalFinder : IWalker
  { public void PostWalk(Node node) { }

    public bool Walk(Node node)
    { while(node is ParenExpression) node = ((ParenExpression)node).Expression;
      if(node is DefStatement || node is LambdaExpression) return false; // TODO: what if 'def' and 'global' collide?
      else if(node is GlobalStatement)
      { if(Globals==null) Globals = new ArrayList();
        foreach(Name n in ((GlobalStatement)node).Names)
          if(assigned!=null && assigned.Contains(n.String))
            throw Ops.SyntaxError(node, "'{0}' assigned to before associated 'global' statement", n.String);
          else Globals.Add(n);
        return false;
      }
      else if(node is AssignStatement)
      { foreach(Expression e in ((AssignStatement)node).LHS) HandleAssignment(e);
        return false;
      }
      else if(node is ForStatement) HandleAssignment(((ForStatement)node).Names);
      else if(node is ListCompExpression) HandleAssignment(((ListCompExpression)node).Names);
      else if(node is TryStatement)
        foreach(ExceptClause ec in ((TryStatement)node).Except) if(ec.Target!=null) HandleAssignment(ec.Target);
      return true;
    }

    void HandleAssignment(Expression e)
    { while(e is ParenExpression) e = ((ParenExpression)e).Expression;
      if(e is NameExpression)
      { if(assigned==null) assigned=new HybridDictionary();
        assigned[((NameExpression)e).Name.String] = null;
      }
      else if(e is TupleExpression) foreach(Expression te in ((TupleExpression)e).Expressions) HandleAssignment(te);
    }

    public ArrayList Globals;
    HybridDictionary assigned;
  }
  #endregion
}
#endregion

#region DelStatement
public class DelStatement : Statement
{ public DelStatement(Expression expr) { Expression=expr; }

  public override void Emit(CodeGenerator cg) { Expression.EmitDel(cg); }
  public override void Execute(Frame frame) { Expression.Delete(frame); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("del ");
    Expression.ToCode(sb, 0);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

#region ExpressionStatement
public class ExpressionStatement : Statement
{ public ExpressionStatement(Expression expr) { Expression=expr; }

  public override void Emit(CodeGenerator cg)
  { Expression.Emit(cg);
    if(Options.Interactive)
    { Slot temp = cg.AllocLocalTemp(typeof(object));
      temp.EmitSet(cg);
      cg.EmitFieldGet(typeof(Boa.Modules.sys), "displayhook");
      temp.EmitGet(cg);
      cg.EmitCall(typeof(Ops), "Call1");
      cg.FreeLocalTemp(temp);
    }
    cg.ILG.Emit(OpCodes.Pop);
  }

  public override void Execute(Frame frame)
  { object ret = Expression.Evaluate(frame);
    if(Options.Interactive) Ops.Call(Boa.Modules.sys.displayhook, ret);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent) { Expression.ToCode(sb, indent); }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

#region IfStatement
public class IfStatement : Statement
{ public IfStatement(Expression test, Statement body, Statement elze) { Test=test; Body=body; Else=elze; }

  public override void Emit(CodeGenerator cg)
  { if(!Options.Debug && Test.IsConstant)
    { if(Ops.IsTrue(Test.GetValue())) Body.Emit(cg);
      else if(Else!=null) Else.Emit(cg);
    }
    else
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
  }

  public override void Execute(Frame frame)
  { if(Ops.IsTrue(Test.Evaluate(frame))) Body.Execute(frame);
    else if(Else!=null) Else.Execute(frame);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent) { ToCode(sb, indent, false); }

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

  void ToCode(System.Text.StringBuilder sb, int indent, bool elif)
  { sb.Append(elif ? "elif " : "if ");
    Test.ToCode(sb, 0);
    sb.Append(':');
    StatementToCode(sb, Body, indent+Options.IndentSize);
    if(Else!=null)
    { sb.Append(' ', indent);
      if(Else is IfStatement) ((IfStatement)Else).ToCode(sb, indent, true);
      else
      { sb.Append("else:");
        StatementToCode(sb, Else, indent+Options.IndentSize);
      }
    }
  }
}
#endregion

#region ImportFromStatement
public class ImportFromStatement : Statement
{ public ImportFromStatement(string module, params ImportName[] names) { Module=module; Names=names; }

  public override void Emit(CodeGenerator cg)
  { cg.TypeGenerator.ModuleField.EmitGet(cg);
    cg.EmitString(Module);
    if(Names[0].Name=="*") cg.EmitCall(typeof(Ops), "ImportStar");
    else
    { string[] names=new string[Names.Length], asNames=new string[Names.Length];
      for(int i=0; i<Names.Length; i++) { names[i]=Names[i].Name; asNames[i]=Names[i].AsName; }
      cg.EmitStringArray(names);
      cg.EmitStringArray(asNames);
      cg.EmitCall(typeof(Ops), "ImportFrom");
    }
  }

  public override void Execute(Frame frame)
  { if(Names[0].Name=="*") Ops.ImportStar(frame.Module, Module);
    else
    { string[] names=new string[Names.Length], asNames=new string[Names.Length];
      for(int i=0; i<Names.Length; i++) { names[i]=Names[i].Name; asNames[i]=Names[i].AsName; }
      Ops.ImportFrom(frame.Module, Module, names, asNames);
    }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("from ");
    sb.Append(Module);
    sb.Append(" import ");
    for(int i=0; i<Names.Length; i++)
    { if(i!=0) sb.Append(", ");
      Names[i].ToCode(sb);
    }
  }

  public ImportName[] Names;
  public string Module;
}
#endregion

#region ImportStatement
public class ImportStatement : Statement
{ public ImportStatement(params ImportName[] names) { Names=names; }

  public override void Emit(CodeGenerator cg)
  { string[] names=new string[Names.Length], asNames=new string[Names.Length];
    for(int i=0; i<Names.Length; i++) { names[i]=Names[i].Name; asNames[i]=Names[i].AsName; }
    cg.TypeGenerator.ModuleField.EmitGet(cg);
    cg.EmitStringArray(names);
    cg.EmitStringArray(asNames);
    cg.EmitCall(typeof(Ops), "Import");
  }

  public override void Execute(Frame frame)
  { string[] names=new string[Names.Length], asNames=new string[Names.Length];
    for(int i=0; i<Names.Length; i++) { names[i]=Names[i].Name; asNames[i]=Names[i].AsName; }
    Ops.Import(frame.Module, names, asNames);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("import ");
    for(int i=0; i<Names.Length; i++)
    { if(i!=0) sb.Append(", ");
      Names[i].ToCode(sb);
    }
  }

  public ImportName[] Names;
}
#endregion

#region ForStatement
public class ForStatement : Statement
{ public ForStatement(Name[] names, Expression expr, Statement body)
  { if(names.Length==1) Names = new NameExpression(names[0]);
    else
    { Expression[] ne = new Expression[names.Length];
      for(int i=0; i<names.Length; i++) ne[i] = new NameExpression(names[i]);
      Names=new TupleExpression(ne);
    }
    Expression=expr; Body=body;
  }

  public override void Emit(CodeGenerator cg)
  { AssignStatement ass = new AssignStatement(Names);
    Label start=cg.ILG.DefineLabel(), end=cg.ILG.DefineLabel();

    Body.Walk(new JumpFinder(start, end));
    Expression.Emit(cg);
    cg.EmitCall(typeof(Ops), "GetEnumerator", new Type[] { typeof(object) });
    cg.ILG.MarkLabel(start);
    cg.ILG.Emit(OpCodes.Dup);
    cg.EmitCall(typeof(IEnumerator), "MoveNext");
    cg.ILG.Emit(OpCodes.Brfalse, end);
    cg.ILG.Emit(OpCodes.Dup);
    cg.EmitPropGet(typeof(IEnumerator), "Current");
    ass.Emit(cg);
    Body.Emit(cg);
    cg.ILG.Emit(OpCodes.Br, start);
    cg.ILG.MarkLabel(end);
    cg.ILG.Emit(OpCodes.Pop);
  }

  public override void Execute(Frame frame)
  { ConstantExpression ce = new ConstantExpression(null);
    AssignStatement ass = new AssignStatement(Names, ce);
    ass.Optimize();

    IEnumerator e = Ops.GetEnumerator(Expression.Evaluate(frame));
    while(e.MoveNext())
    { ce.Value=e.Current;
      ass.Execute(frame);
      try { Body.Execute(frame); }
      catch(BreakException) { break; }
      catch(ContinueException) { }
    }
  }
  
  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("for ");
    if(Names is TupleExpression)
    { TupleExpression te = (TupleExpression)Names;
      for(int i=0; i<te.Expressions.Length; i++)
      { if(i!=0) sb.Append(',');
        te.Expressions[i].ToCode(sb, 0);
      }
    }
    else Names.ToCode(sb, 0);
    sb.Append(" in ");
    Expression.ToCode(sb, 0);
    sb.Append(':');
    StatementToCode(sb, Body, indent+Options.IndentSize);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Names.Walk(w);
      Expression.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Names;
  public Expression Expression;
  public Statement  Body;
}
#endregion

#region GlobalStatement
public class GlobalStatement : Statement
{ public GlobalStatement(string[] names)
  { Names = new Name[names.Length];
    for(int i=0; i<names.Length; i++) Names[i] = new Name(names[i], Scope.Global);
  }

  public override void Emit(CodeGenerator cg) { }
  public override void Execute(Frame frame) { foreach(Name name in Names) frame.MarkGlobal(name.String); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("global ");
    for(int i=0; i<Names.Length; i++)
    { if(i!=0) sb.Append(", ");
      sb.Append(Names[i].String);
    }
  }

  public Name[] Names;
}
#endregion

#region JumpStatement
public abstract class JumpStatement : Statement
{ public override void Emit(CodeGenerator cg) { cg.ILG.Emit(NeedsLeave ? OpCodes.Leave : OpCodes.Br, Label); }
  public Label Label;
  public bool  NeedsLeave;
}
#endregion

#region PassStatement
public class PassStatement : Statement
{ public override void Emit(CodeGenerator cg) { }
  public override void Execute(Frame frame) { }
  public override void ToCode(System.Text.StringBuilder sb, int indent) { sb.Append("pass"); }
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

  public override void Execute(Frame frame)
  { if(Expressions!=null) foreach(Expression e in Expressions) Ops.Print(e.Evaluate(frame));
    if(TrailingNewline || Expressions.Length==0) Ops.PrintNewline();
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("print ");
    for(int i=0; i<Expressions.Length; i++)
    { if(i!=0) sb.Append(", ");
      Expressions[i].ToCode(sb, 0);
    }
    if(!TrailingNewline) sb.Append(',');
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this) && Expressions!=null) foreach(Expression e in Expressions) e.Walk(w);
    w.PostWalk(this);
  }

  public Expression[] Expressions;
  public bool TrailingNewline;
}
#endregion

// TODO: add source, line, and column to exceptions
#region RaiseStatement
public class RaiseStatement : Statement
{ public RaiseStatement() { }
  public RaiseStatement(Expression e) { Expression=e; }

  public override void Emit(CodeGenerator cg)
  { if(Expression==null) cg.ILG.Emit(OpCodes.Rethrow);
    else
    { Label bad=cg.ILG.DefineLabel();
      Expression.Emit(cg);
      cg.ILG.Emit(OpCodes.Isinst, typeof(Exception));
      cg.ILG.Emit(OpCodes.Dup);
      cg.ILG.Emit(OpCodes.Brfalse_S, bad);
      cg.ILG.Emit(OpCodes.Throw);
      cg.ILG.MarkLabel(bad);
      cg.ILG.Emit(OpCodes.Pop);
      cg.EmitString("exceptions must be derived from System.Exception");
      cg.EmitNew(typeof(TypeErrorException));
      cg.ILG.Emit(OpCodes.Throw);
    }
  }

  public override void Execute(Frame frame)
  { if(Expression==null) throw (Exception)Boa.Modules.sys.Exceptions.Peek(); // this isn't quite the same...
    else
    { Exception e = Expression.Evaluate(frame) as Exception;
      if(e==null) throw Ops.TypeError("exceptions must be derived from System.Exception");
      throw e;
    }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { if(Expression==null) sb.Append("raise");
    else
    { sb.Append("raise ");
      Expression.ToCode(sb, 0);
    }
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this) && Expression!=null) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

#region ReturnStatement
public class ReturnStatement : Statement
{ public ReturnStatement() { }
  public ReturnStatement(Expression expression) { Expression=expression; }

  public override void Emit(CodeGenerator cg) { cg.EmitReturn(Expression); }
  public override void Execute(Frame frame)
  { throw new ReturnException(Expression==null ? null : Expression.Evaluate(frame));
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { if(Expression==null) sb.Append("return");
    else
    { sb.Append("return ");
      Expression.ToCode(sb, 0);
    }
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this) && Expression!=null) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

#region Suite
public class Suite : Statement
{ public Suite(Statement[] stmts) { Statements=stmts; SetLocation(stmts[0].Source, stmts[0].Line, stmts[0].Column); }

  public override void Emit(CodeGenerator cg)
  { foreach(Statement stmt in Statements)
    { cg.EmitPosition(stmt);
      stmt.Emit(cg);
    }
  }

  public override void Execute(Frame frame) { foreach(Statement stmt in Statements) stmt.Execute(frame); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('\n');
    foreach(Statement s in Statements)
    { if(indent>0) sb.Append(' ', indent);
      s.ToCode(sb, indent);
      if(!(s is IfStatement)) sb.Append('\n');
    }
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Statement stmt in Statements) stmt.Walk(w);
    w.PostWalk(this);
  }

  public Statement[] Statements;
}
#endregion

#region TryStatement
public class TryStatement : Statement
{ public TryStatement(Statement body, ExceptClause[] except, Statement elze, Statement final)
  { Body=body; Except=except; Else=elze; Finally=final;
  }
  
  public override void Emit(CodeGenerator cg)
  { Slot occurred = Else==null ? null : cg.AllocLocalTemp(typeof(bool)), dt = cg.AllocLocalTemp(typeof(DynamicType)),
         type = cg.AllocLocalTemp(typeof(object)), et = cg.AllocLocalTemp(typeof(DynamicType)),
         e = cg.AllocLocalTemp(typeof(Exception));

    if(occurred!=null)
    { cg.EmitInt(0);
      occurred.EmitSet(cg);
    }

    Label done = cg.ILG.BeginExceptionBlock();      // try {
    Body.Emit(cg);                                  //   body
    cg.ILG.BeginCatchBlock(typeof(Exception));      // } catch(Exception e) {
    cg.ILG.Emit(OpCodes.Dup);
    e.EmitSet(cg);
    if(occurred!=null)                              // occurred = true
    { cg.EmitInt(1);
      occurred.EmitSet(cg);
    }
    cg.EmitCall(typeof(Ops), "GetDynamicType");     // dt = GetDynamicType(e)
    dt.EmitSet(cg);

    foreach(ExceptClause ec in Except)              // foreach(ExceptClause ec in Except) {
    { if(ec.Type==null) break;
      Label isDT=cg.ILG.DefineLabel(), next=cg.ILG.DefineLabel();
      ec.Type.Emit(cg);                             // type = ec.Type
      cg.ILG.Emit(OpCodes.Dup);
      type.EmitSet(cg);
      cg.ILG.Emit(OpCodes.Isinst, typeof(DynamicType)); // DynamicType et = type as DynamicType
      cg.ILG.Emit(OpCodes.Dup);
      et.EmitSet(cg);
      cg.ILG.Emit(OpCodes.Brtrue, isDT);                // if(et==null) et = Ops.GetDynamicType(type)
      type.EmitGet(cg);
      cg.EmitCall(typeof(Ops), "GetDynamicType");
      et.EmitSet(cg);
      cg.ILG.MarkLabel(isDT);
      et.EmitGet(cg);                                   // if(et.IsSubclassOf(dt)) {
      dt.EmitGet(cg);
      cg.EmitCall(typeof(DynamicType), "IsSubclassOf");
      cg.ILG.Emit(OpCodes.Brfalse, next);
      if(ec.Target!=null)                               // ec.Target = e
      { e.EmitGet(cg);
        new AssignStatement(ec.Target).Emit(cg);
      }
      ec.Body.Emit(cg);                                 // ec.Body()
      cg.ILG.Emit(OpCodes.Leave, done);                 // }
      cg.ILG.MarkLabel(next);
    }

    if(Except.Length==0 || Except[Except.Length-1].Type!=null) cg.ILG.Emit(OpCodes.Rethrow);
    else
    { ExceptClause ec = Except[Except.Length-1];
      if(ec.Target!=null)
      { e.EmitGet(cg);
        new AssignStatement(ec.Target).Emit(cg);
      }
      ec.Body.Emit(cg);
    }

    if(Else!=null || Finally!=null)
    { cg.ILG.BeginFinallyBlock();
      if(Else!=null)
      { Label noElse=cg.ILG.DefineLabel();
        occurred.EmitGet(cg);
        cg.ILG.Emit(OpCodes.Brtrue, noElse);
        cg.ILG.BeginExceptionBlock();
        Else.Emit(cg);
        cg.ILG.BeginCatchBlock(typeof(object));
        cg.ILG.Emit(OpCodes.Pop);
        cg.ILG.EndExceptionBlock();
        cg.ILG.MarkLabel(noElse);
      }
      if(Finally!=null) Finally.Emit(cg);
    }
    cg.ILG.EndExceptionBlock();

    if(occurred!=null) cg.FreeLocalTemp(occurred);
    cg.FreeLocalTemp(dt);
    cg.FreeLocalTemp(type);
    cg.FreeLocalTemp(et);
    cg.FreeLocalTemp(e);
  }
  
  public override void Execute(Frame frame)
  { bool occurred=false;
    try { Body.Execute(frame); }
    catch(Exception e)
    { occurred = true;
      Boa.Modules.sys.Exceptions.Push(e);

      DynamicType dt = Ops.GetDynamicType(e);
      foreach(ExceptClause ec in Except)
      { DynamicType et;
        if(ec.Type!=null)
        { object type = ec.Type.Evaluate(frame);
          et = type as DynamicType;
          if(et==null) et = Ops.GetDynamicType(type);
        }
        else et=null;
        if(et==null || et.IsSubclassOf(dt))
        { if(ec.Target!=null) ec.Target.Assign(e, frame);
          ec.Body.Execute(frame);
          goto done;
        }
      }
      if(Else==null) throw;
      else Else.Execute(frame);
      done:;
    }
    finally
    { if(!occurred && Else!=null) try { Else.Execute(frame); } catch { }
      if(Finally!=null) Finally.Execute(frame);
      if(occurred) Boa.Modules.sys.Exceptions.Pop();
    }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("try:\n");
    StatementToCode(sb, Body, indent+Options.IndentSize);

    foreach(ExceptClause ec in Except)
    { sb.Append(' ', indent);
      ec.ToCode(sb, 0);
    }
    if(Else!=null)
    { sb.Append(' ', indent);
      sb.Append("else:");
      StatementToCode(sb, Else, indent+Options.IndentSize);
    }
    if(Finally!=null)
    { sb.Append(' ', indent);
      sb.Append("finally:");
      StatementToCode(sb, Finally, indent+Options.IndentSize);
    }
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Body.Walk(w);
      foreach(ExceptClause e in Except) e.Walk(w);
      if(Else!=null) Else.Walk(w);
      if(Finally!=null) Finally.Walk(w);
    }
  }

  public Statement Body, Else, Finally;
  public ExceptClause[] Except;
}
#endregion

#region WhileStatement
public class WhileStatement : Statement
{ public WhileStatement(Expression test, Statement body) { Test=test; Body=body; }

  public override void Emit(CodeGenerator cg)
  { if(!Options.Debug && Test.IsConstant && !Ops.IsTrue(Test.GetValue())) return;

    Label start=cg.ILG.DefineLabel(), end=cg.ILG.DefineLabel();
    Body.Walk(new JumpFinder(start, end));
    cg.ILG.MarkLabel(start);
    if(Options.Debug || !Test.IsConstant)
    { Test.Emit(cg);
      cg.EmitCall(typeof(Ops), "IsTrue");
      cg.ILG.Emit(OpCodes.Brfalse, end);
    }
    Body.Emit(cg);
    cg.ILG.Emit(OpCodes.Br, start);
    cg.ILG.MarkLabel(end);
  }

  public override void Execute(Frame frame)
  { while(Ops.IsTrue(Test.Evaluate(frame)))
      try { Body.Execute(frame); }
      catch(BreakException) { break; }
      catch(ContinueException) { }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("while ");
    Test.ToCode(sb, 0);
    sb.Append(':');
    StatementToCode(sb, Body, indent+Options.IndentSize);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Test.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Test;
  public Statement  Body;
}
#endregion

} // namespace Boa.AST
