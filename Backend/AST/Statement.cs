/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

// TODO: possibly rework compiled closures
// TODO: add versions optimized for System.Array?
// TODO: disallow 'yield' and 'return' outside of functions
/* TODO:
If a variable is referenced in an enclosing scope, it is illegal
to delete the name. An error will be reported at compile time.

If the wild card form of import -- "import *" -- is used in a function and the function
contains or is a nested block with free variables, the compiler will raise a SyntaxError.

If exec is used in a function and the function contains or is a nested block with free
variables, the compiler will raise a SyntaxError unless the exec explicitly specifies the local
namespace for the exec. (In other words, "exec obj" would be illegal, but "exec obj in ns" would be legal.)
*/
// TODO: make sure all enumerators can handle the underlying collection being changed, if possible
// TODO: disallow 'break' and 'continue' inside 'finally' blocks (difficult to implement)
// TODO: allow 'from xxx import yyy/*' inside functions/class defs

// TODO: using exceptions is very slow
#region Exceptions (used to aid implementation)
public class BreakException : Exception
{ public BreakException() { }
  public BreakException(string name) { Name=name; }

  public string Name;

  public static BreakException Value = new BreakException();
}
public class ContinueException : Exception
{ public ContinueException() { }
  public ContinueException(string name) { Name=name; }

  public string Name;

  public static ContinueException Value = new ContinueException();
}
public class ReturnException : Exception
{ public ReturnException(object value) { Value=value; }
  public object Value;
}
#endregion

#region Walkers
#region JumpFinder
sealed class JumpFinder : IWalker
{ public JumpFinder(string name, Label start, Label end) { this.name=name; this.start=start; this.end=end; }

  public void PostWalk(Node node)
  { if(node is ExceptionStatement) inTry--;
  }

  public bool Walk(Node node)
  { if(node is BreakStatement)
    { BreakStatement bs = (BreakStatement)node;
      if(bs.Name==name)
      { bs.Label = end;
        bs.NeedsLeave = InTry;
      }
    }
    else if(node is ContinueStatement)
    { ContinueStatement cs = (ContinueStatement)node;
      if(cs.Name==name)
      { cs.Label = start;
        cs.NeedsLeave = InTry;
      }
    }
    else if(name==null && (node is WhileStatement || node is ForStatement) || node is DefStatement) return false;
    else if(node is ExceptionStatement) inTry++;
    return true;
  }

  bool InTry { get { return inTry>0; } }

  Label start, end;
  string name;
  int inTry;
}
#endregion

#region NameFinder
sealed class NameFinder : IWalker
{ public static Name[] Find(Node n)
  { NameFinder nf = new NameFinder();
    n.Walk(nf);
    return (Name[])nf.names.ToArray(typeof(Name));
  }
  
  public void PostWalk(Node n) { }

  public bool Walk(Node n)
  { if(n is NameExpression) names.Add(((NameExpression)n).Name);
    return true;
  }

  ArrayList names = new ArrayList();
}
#endregion

#region Optimizer
sealed class Optimizer : IWalker
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
  { Walk(new JumpChecker());
    Walk(new TryChecker());
    if(Options.Optimize) Walk(new Optimizer());
  }

  #region JumpChecker
  sealed class JumpChecker : IWalker
  { public void PostWalk(Node node)
    { if(node is Suite)
      { if(((Suite)node).Name!=null) blocks.RemoveAt(blocks.Count-1);
      }
      else if(node is WhileStatement || node is ForStatement) blocks.RemoveAt(blocks.Count-1);
      else if(node is DefStatement)
      { if(blocks!=null)
        { blocks.Clear();
          if(flist!=null) flist = new Stack();
          flist.Push(blocks);
          blocks = (ArrayList)lists.Pop();
        }
      }
    }

    public bool Walk(Node node)
    { if(node is Suite)
      { if(((Suite)node).Name!=null)
        { if(blocks==null) blocks = new ArrayList();
          blocks.Add(node);
        }
      }
      else if(node is WhileStatement || node is ForStatement)
      { if(blocks==null) blocks = new ArrayList();
        blocks.Add(node);
      }
      else if(node is JumpStatement)
      { string name = ((JumpStatement)node).Name;
        if(name!=null)
        { if(blocks!=null)
            foreach(object n in blocks)
              if(n is Suite && ((Suite)n).Name==name) return false;
          Ops.SyntaxError(node, "break/continue: unable to find a valid jump target named '{0}'", name);
        }
        else
        { if(blocks!=null)
            for(int i=blocks.Count-1; i>=0; i--)
              if(blocks[i] is WhileStatement || blocks[i] is ForStatement) return false;
          Ops.SyntaxError(node, "break/continue: unable to find a valid enclosing loop");
        }
      }
      else if(node is DefStatement)
      { if(lists==null) lists = new Stack();
        lists.Push(blocks);
        blocks = flist==null || flist.Count==0 ? null : (ArrayList)flist.Pop();
      }
      return true;
    }

    ArrayList blocks;
    Stack lists, flist;
  }
  #endregion

  #region NameDecorator
  sealed class NameDecorator : IWalker
  { public NameDecorator() { }
    NameDecorator(SortedList names) { parentNames=names; }

    public void PostWalk(Node node)
    { if(node==current)
      { inDef=false;

        ArrayList inherit = innerFuncs.Count==0 ? null : new ArrayList();
        foreach(BoaFunction func in innerFuncs)
        { NameDecorator dec = new NameDecorator(names);
          func.Walk(dec);
          foreach(Name dname in dec.names.Values)
            if(dname.Scope==Scope.Free)
            { Name name = (Name)names[dname.String];
              if(name==null) names[dname.String] = dname;
              else if(name.Scope==Scope.Local) inherit.Add(name);
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
          for(int i=0; i<fun.Parameters.Length; i++)
            if(fun.Parameters[i].Default!=null) fun.Parameters[i].Default.Walk(this);
          return false;
        }
        else if(node is AssignStatement)
          foreach(Expression e in ((AssignStatement)node).LHS) HandleAssignment(e);
        else if(node is ForStatement) HandleAssignment(((ForStatement)node).Names);
        else if(node is ListCompExpression)
          foreach(ListCompFor f in ((ListCompExpression)node).Fors) HandleAssignment(f.Names);
        else if(node is ExceptionStatement)
        { foreach(ExceptClause ec in ((ExceptionStatement)node).Except)
            if(ec.Target!=null) HandleAssignment(ec.Target);
        }
        else if(node is ImportStatement)
        { ImportStatement imp = (ImportStatement)node;
          foreach(ImportName n in imp.Names) AddName(new Name(n.AsName==null ? n.SlotName : n.AsName, Scope.Local));
        }
        else if(node is ImportFromStatement)
        { ImportFromStatement imp = (ImportFromStatement)node;
          if(imp.Names[0].Name=="*") // TODO: perhaps this should be disallowed within other contexts as well
            Ops.SyntaxError(node, "'from ... import *' is disallowed within function definitions");
          foreach(ImportName n in imp.Names) AddName(new Name(n.AsName==null ? n.Name : n.AsName, Scope.Local));
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
        if(ne.Name.Scope==Scope.Free && (parentNames==null || parentNames[ne.Name.String]==null))
          ne.Name.Scope=Scope.Local;
      }
      else if(assignedTo is TupleExpression)
        foreach(Expression e in ((TupleExpression)assignedTo).Expressions) HandleAssignment(e);
    }

    BoaFunction current;
    ArrayList  innerFuncs;
    SortedList names, parentNames;
    bool inDef;
  }
  #endregion
  
  #region TryChecker
  sealed class TryChecker : IWalker // checks for exceptionless 'raise' outside of a try statement
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
      cg.EmitNew(typeof(AssertionErrorException), new Type[] { typeof(string) });
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

// FIXME: make this work: ((a,b)) = ((1,2))
#region AssignStatement
public class AssignStatement : Statement
{ public AssignStatement() { }
  public AssignStatement(Expression lhs) { LHS=new Expression[] { lhs }; }
  public AssignStatement(Expression lhs, Expression rhs) : this(lhs) { RHS=rhs; }
  public AssignStatement(Expression[] lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { if(Op!=null)
    { new BinaryOpExpression(Op, LHS[0], RHS).Emit(cg);
      LHS[0].EmitSet(cg);
      return;
    }

    bool leftAllTups = RHS is TupleExpression;
    if(leftAllTups) foreach(Expression e in LHS) if(!(e is TupleExpression)) { leftAllTups=false; break; }
    if(leftAllTups) // RHS is TupleExpression
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
          { if(!emitted) { RHS.Emit(cg); emitted=true; }
            if(ti!=0) cg.ILG.Emit(OpCodes.Dup);
            
            Slot eslot = cg.AllocLocalTemp(typeof(IEnumerator));
            cg.EmitInt(lhs.Expressions.Length);
            cg.EmitCall(typeof(Ops), "PrepareTupleAssignment");

            for(int i=0; i<lhs.Expressions.Length; i++)
            { if(i!=lhs.Expressions.Length-1) cg.ILG.Emit(OpCodes.Dup);
              cg.ILG.Emit(OpCodes.Dup);
              cg.EmitCall(typeof(IEnumerator), "MoveNext");
              cg.ILG.Emit(OpCodes.Pop); // ignore return value
              cg.EmitPropGet(typeof(IEnumerator), "Current");
              lhs.Expressions[i].EmitSet(cg);
            }

            cg.FreeLocalTemp(eslot);
          }
        }
        else
        { if(!emitted) { RHS.Emit(cg); emitted=true; }
          if(ti!=0) cg.ILG.Emit(OpCodes.Dup);
          LHS[ti].EmitSet(cg);
        }
      }
    }
  }

  public override void Execute(Frame frame)
  { if(Op!=null) LHS[0].Assign(new BinaryOpExpression(Op, LHS[0], RHS).Evaluate(frame), frame);
    else
    { object value = RHS.Evaluate(frame);
      for(int i=LHS.Length-1; i>=0; i--) LHS[i].Assign(value, frame);
    }
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
  public BinaryOperator Op;
}
#endregion

#region BreakStatement
public class BreakStatement : JumpStatement
{ public BreakStatement() { }
  public BreakStatement(string name) : base(name) { }
  public override void Execute(Frame frame)
  { throw Name==null ? BreakException.Value : new BreakException(Name);
  }
  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("break");
    if(Name!=null) sb.Append(' ').Append(Name);
  }
}
#endregion

// TODO: http://www.python.org/2.2.3/descrintro.html#metaclasses
#region ClassStatement
public class ClassStatement : Statement
{ public ClassStatement(string name, Expression[] bases, Statement body)
  { Name  = new Name(name);
    Bases = bases;
    Body  = body;
    docstring = Misc.BodyToDocString(body);
  }

  public override void Emit(CodeGenerator cg)
  { bool interactive = Options.Interactive;
    Options.Interactive = false;
    try
    { CodeGenerator icg = cg.TypeGenerator.DefineMethod(Name.String+"$maker"+Misc.NextIndex,
                                                        typeof(IDictionary), Type.EmptyTypes);
      LocalNamespace ns = new LocalNamespace(cg.Namespace, icg);
      icg.Namespace = ns;
      Body.Emit(icg);
      ns.EmitLocalsDict(icg);
      icg.EmitReturn();
      icg.Finish();

      cg.EmitGet(new Name("__name__"));
      cg.EmitCall(typeof(Ops), "ToString", new Type[] { typeof(object) }); // FIXME: doesn't handle non-existant __name__
      cg.EmitString(Name.String);
      cg.EmitObjectArray(Bases);
      cg.EmitNew(typeof(Tuple), new Type[] { typeof(object[]) });
      cg.EmitCall((MethodInfo)icg.MethodBase);
      cg.EmitCall(typeof(Ops), "MakeClass");
      if(docstring!=null)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitString("__doc__");
        cg.EmitString(docstring);
        cg.EmitCall(typeof(IHasAttributes), "__setattr__");
      }
      cg.EmitSet(Name);
    }
    finally { Options.Interactive = interactive; }
  }

  public override void Execute(Frame frame)
  { Frame dict = new Frame(frame);
    Body.Execute(dict);
    object[] bases = new object[Bases.Length];
    for(int i=0; i<bases.Length; i++) bases[i] = Bases[i].Evaluate(frame);
    frame.Set(Name.String,
              Ops.MakeClass(Ops.ToString(frame.Get("__name__")), Name.String, new Tuple(bases), dict.Locals));
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
  
  string docstring;
}
#endregion

#region ContinueStatement
public class ContinueStatement : JumpStatement
{ public ContinueStatement() { }
  public ContinueStatement(string name) : base(name) { }
  public override void Execute(Frame frame)
  { throw Name==null ? ContinueException.Value : new ContinueException(Name);
  }
  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("continue");
    if(Name!=null) sb.Append(' ').Append(Name);
  }
}
#endregion

#region DefStatement
public class DefStatement : Statement
{ public DefStatement(string name, Parameter[] parms, Statement body)
  { Function = new BoaFunction(this, name, parms, body);
    Function.Globals = GlobalFinder.Find(Function.Body);
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
  sealed class GlobalFinder : IWalker
  { public static Name[] Find(Node node)
    { GlobalFinder gf = new GlobalFinder();
      node.Walk(gf);
      return gf.Globals==null || gf.Globals.Count==0 ? null : (Name[])gf.Globals.ToArray(typeof(Name));
    }

    public void PostWalk(Node node) { }

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
      else if(node is ListCompExpression)
        foreach(ListCompFor f in ((ListCompExpression)node).Fors) HandleAssignment(f.Names);
      else if(node is ExceptionStatement)
        foreach(ExceptClause ec in ((ExceptionStatement)node).Except) if(ec.Target!=null) HandleAssignment(ec.Target);
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

    ArrayList Globals;
    HybridDictionary assigned;
  }
  #endregion
}
#endregion

#region DelStatement
public class DelStatement : Statement
{ public DelStatement(Expression[] exprs) { Expressions=exprs; }

  public override void Emit(CodeGenerator cg) { foreach(Expression e in Expressions) e.EmitDel(cg); }
  public override void Execute(Frame frame) { foreach(Expression e in Expressions) e.Delete(frame); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("del ");
    for(int i=0; i<Expressions.Length; i++)
    { if(i!=0) sb.Append(", ");
      Expressions[i].ToCode(sb, 0);
    }
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Expression e in Expressions) e.Walk(w);
    w.PostWalk(this);
  }

  public Expression[] Expressions;
}
#endregion

#region ExceptionStatement
public abstract class ExceptionStatement : Statement
{ public ExceptionStatement(Statement body, ExceptClause[] except) { Body=body; Except=except; }

  public override void Emit(CodeGenerator cg)
  { bool haveExcept = Except!=null && Except.Length!=0;
    Slot fromElse = NeedElse && haveExcept ? cg.AllocLocalTemp(typeof(bool)) : null;

    if(fromElse!=null)
    { cg.EmitInt(0);
      fromElse.EmitSet(cg);
    }

    Slot choice=null;
    if(Yields!=null)
    { choice = cg.AllocLocalTemp(typeof(int));
      Label label = cg.ILG.DefineLabel();
      cg.EmitInt(int.MaxValue);
      choice.EmitSet(cg);
      cg.ILG.Emit(OpCodes.Br, label);
      for(int i=0; i<Yields.Length; i++)
      { YieldStatement ys = Yields[i];
        for(int j=0; j<ys.Targets.Length; j++)
          if(ys.Targets[j].Statement==this) { cg.ILG.MarkLabel(ys.Targets[j].Label); break; }
        cg.EmitInt(i);
        choice.EmitSet(cg);
        if(i!=Yields.Length-1) cg.ILG.Emit(OpCodes.Br, label);
      }
      cg.ILG.MarkLabel(label);
    }

    EmitPreTry(cg);
    Label done = cg.ILG.BeginExceptionBlock();      // try {
    if(Yields!=null)
    { Label[] jumps = new Label[Yields.Length];
      for(int i=0; i<Yields.Length; i++)
      { YieldStatement ys = (YieldStatement)Yields[i];
        for(int j=0; j<ys.Targets.Length; j++)
          if(ys.Targets[j].Statement==this) { jumps[i] = ys.Targets[j+1].Label; break; }
      }
      choice.EmitGet(cg);
      cg.ILG.Emit(OpCodes.Switch, jumps);
      cg.FreeLocalTemp(choice);
    }
    Body.Emit(cg);                                  //   body
    if(NeedElse)                                    //   else
    { if(fromElse!=null)
      { cg.EmitInt(1);
        fromElse.EmitSet(cg);
      }
      EmitElse(cg);
    }

    if(haveExcept)
    { cg.ILG.BeginCatchBlock(typeof(Exception));      // } catch(Exception e) {
      Slot dt = cg.AllocLocalTemp(typeof(DynamicType)), type = cg.AllocLocalTemp(typeof(object)),
          et = cg.AllocLocalTemp(typeof(DynamicType)), e = cg.AllocLocalTemp(typeof(Exception));

      if(fromElse!=null)                              // if(fromElse) throw;
      { Label prop = cg.ILG.DefineLabel();
        fromElse.EmitGet(cg);
        cg.ILG.Emit(OpCodes.Brfalse_S, prop);
        cg.ILG.Emit(OpCodes.Rethrow);
        cg.ILG.MarkLabel(prop);
      }

      if(Except[0].Type!=null)
      { cg.ILG.Emit(OpCodes.Dup);
        e.EmitSet(cg);
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
          cg.EmitCall(typeof(DynamicType), "IsTryMatch");
          cg.ILG.Emit(OpCodes.Brfalse, next);
          if(ec.Target!=null)                               // ec.Target = e
          { e.EmitGet(cg);
            new AssignStatement(ec.Target).Emit(cg);
          }
          ec.Body.Emit(cg);                                 // ec.Body()
          cg.ILG.Emit(OpCodes.Leave, done);                 // }
          cg.ILG.MarkLabel(next);
        }
      }

      if(Except[Except.Length-1].Type!=null) cg.ILG.Emit(OpCodes.Rethrow);
      else
      { ExceptClause ec = Except[Except.Length-1];
        if(ec.Target!=null)
        { e.EmitGet(cg);
          new AssignStatement(ec.Target).Emit(cg);
        }
        ec.Body.Emit(cg);
      }
      cg.FreeLocalTemp(dt);
      cg.FreeLocalTemp(type);
      cg.FreeLocalTemp(et);
      cg.FreeLocalTemp(e);
    }

    if(NeedFinally)
    { cg.ILG.BeginFinallyBlock();
      EmitFinally(cg);
    }
    cg.ILG.EndExceptionBlock();

    if(fromElse!=null) cg.FreeLocalTemp(fromElse);
  }
  
  public override void Execute(Frame frame)
  { ExecutePreTry(frame);

    bool fromElse=false, occurred=false;
    try
    { Body.Execute(frame);
      if(NeedElse) { fromElse=true; ExecuteElse(frame); }
    }
    catch(BreakException) { throw; }
    catch(ContinueException) { throw; }
    catch(Exception e)
    { if(fromElse) throw;
      occurred = true;
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
      done:;
    }
    finally
    { if(NeedFinally) ExecuteFinally(frame);
      if(occurred) Boa.Modules.sys.Exceptions.Pop();
    }
  }

  public Statement Body;
  public ExceptClause[] Except;
  public YieldStatement[] Yields;
  
  protected virtual void EmitPreTry(CodeGenerator cg) { }
  protected virtual void EmitBody(CodeGenerator cg) { Body.Emit(cg); }
  protected virtual void EmitElse(CodeGenerator cg) { }
  protected virtual void EmitExcept(CodeGenerator cg) { }
  protected virtual void EmitFinally(CodeGenerator cg) { }
  protected virtual void PostEmit(CodeGenerator cg) { }

  protected virtual void ExecutePreTry(Frame frame) { }
  protected virtual void ExecuteElse(Frame frame) { }
  protected virtual void ExecuteFinally(Frame frame) { }

  protected bool NeedElse, NeedFinally;
}
#endregion

#region ExpressionStatement
public class ExpressionStatement : Statement
{ public ExpressionStatement(Expression expr) { Expression=expr; SetLocation(expr); }

  public override void Emit(CodeGenerator cg)
  { // optimize away expressions that do nothing
    if(Options.Optimize && Expression.IsConstant && !Options.Interactive) return;
    Expression.Emit(cg);
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
  { if(Options.Optimize && Expression.IsConstant) return;
    object ret = Expression.Evaluate(frame);
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
  { if(Options.Optimize && Test.IsConstant)
    { if(Ops.IsTrue(Test.GetValue())) Body.Emit(cg);
      else if(Else!=null) Else.Emit(cg);
    }
    else
    { Label endLab = cg.ILG.DefineLabel(), elseLab = Else==null ? new Label() : cg.ILG.DefineLabel();
      if(Options.Optimize && Test is UnaryExpression && ((UnaryExpression)Test).Op==UnaryOperator.LogicalNot)
      { cg.EmitIsTrue(((UnaryExpression)Test).Expression);
        cg.ILG.Emit(OpCodes.Brtrue, Else==null ? endLab : elseLab);
      }
      else
      { cg.EmitIsTrue(Test);
        cg.ILG.Emit(OpCodes.Brfalse, Else==null ? endLab : elseLab);
      }

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

// FIXME: make this be scoped properly in functions, classes, etc
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
  { foreach(ImportName n in Names)
    { cg.EmitString(n.Name);
      cg.EmitCall(typeof(Importer), n.AsName==null ? "ImportTop" : "Import", new Type[] { typeof(string) });
      cg.EmitSet(new Name(n.AsName==null ? n.SlotName : n.AsName, Scope.Local));
    }
  }

  public override void Execute(Frame frame)
  { foreach(ImportName n in Names)
      if(n.AsName==null) frame.Set(n.SlotName, Importer.ImportTop(n.Name));
      else frame.Set(n.AsName, Importer.Import(n.Name));
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
{ public ForStatement(Name[] names, Expression expr, Statement body, Statement elze)
  { if(names.Length==1) Names = new NameExpression(names[0]);
    else
    { Expression[] ne = new Expression[names.Length];
      for(int i=0; i<names.Length; i++) ne[i] = new NameExpression(names[i]);
      Names=new TupleExpression(ne);
    }
    Expression=expr; Body=body; Else=elze;
  }

  public override void Emit(CodeGenerator cg)
  { AssignStatement ass = new AssignStatement(Names);
    Slot list = cg.AllocLocalTemp(typeof(IEnumerator), true);
    Label start=cg.ILG.DefineLabel(), end=cg.ILG.DefineLabel(), elze=(Else==null ? end : cg.ILG.DefineLabel());

    Body.Walk(new JumpFinder(null, start, end));
    Expression.Emit(cg);
    cg.EmitCall(typeof(Ops), "GetEnumerator", new Type[] { typeof(object) });
    list.EmitSet(cg);
    cg.ILG.BeginExceptionBlock();
    cg.ILG.MarkLabel(start);
    list.EmitGet(cg);
    cg.EmitCall(typeof(IEnumerator), "MoveNext");
    cg.ILG.Emit(OpCodes.Brfalse, elze);
    list.EmitGet(cg);
    cg.EmitPropGet(typeof(IEnumerator), "Current");
    ass.Emit(cg);
    Body.Emit(cg);
    cg.ILG.Emit(OpCodes.Br, start);
    if(Else!=null)
    { cg.ILG.MarkLabel(elze);
      Else.Emit(cg);
    }
    cg.ILG.MarkLabel(end);
    cg.ILG.BeginCatchBlock(typeof(StopIterationException));
    cg.ILG.EndExceptionBlock();
    cg.FreeLocalTemp(list);
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
      catch(BreakException ex) { if(ex.Name!=null) throw; goto done; }
      catch(StopIterationException) { goto done; }
      catch(ContinueException ex) { if(ex.Name!=null) throw; }
    }
    if(Else!=null) Else.Execute(frame);
    done:;
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
      if(Else!=null) Else.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Names;
  public Expression Expression;
  public Statement  Body, Else;
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
{ public JumpStatement() { }
  public JumpStatement(string name) { Name=name; }

  public override void Emit(CodeGenerator cg) { cg.ILG.Emit(NeedsLeave ? OpCodes.Leave : OpCodes.Br, Label); }
  public Label Label;
  public string Name;
  public bool   NeedsLeave;
}
#endregion

#region LockStatement
public class LockStatement : ExceptionStatement
{ public LockStatement(Expression expr, Statement body) : base(body, null) { Expression=expr; NeedFinally=true; }

  public override void Execute(Frame frame)
  { if(Expression is TupleExpression)
    { TupleExpression tup = (TupleExpression)Expression;
      object[] arr = new object[tup.Expressions.Length];
      for(int i=0; i<arr.Length; i++)
      { arr[i] = tup.Expressions[i].Evaluate(frame);
        if(arr[i]==null) throw Ops.ValueError(this, "cannot lock a null value");
      }
      for(int i=0; i<arr.Length; i++) System.Threading.Monitor.Enter(arr[i]);
      try { Body.Execute(frame); }
      finally { foreach(object o in arr) if(o!=null) System.Threading.Monitor.Exit(o); }
    }
    else
    { object o = Expression.Evaluate(frame);
      if(o==null) throw Ops.ValueError(this, "cannot lock a null value");
      lock(o) Body.Execute(frame);
    }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("lock ");
    Expression.ToCode(sb, 0);
    sb.Append(":\n");
    StatementToCode(sb, Body, indent+Options.IndentSize);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Expression.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Expression;

  protected override void EmitPreTry(CodeGenerator cg)
  { Expression[] exprs = Expression is TupleExpression ? ((TupleExpression)Expression).Expressions
                                                       : new Expression[] { Expression };
    Label good=cg.ILG.DefineLabel(), evil=exprs.Length==1 ? new Label() : cg.ILG.DefineLabel();

    slots = new Slot[exprs.Length];
    for(int i=0; i<exprs.Length; i++)
    { slots[i] = cg.AllocLocalTemp(typeof(object), true);
      exprs[i].Emit(cg);
      cg.ILG.Emit(OpCodes.Dup);
      slots[i].EmitSet(cg);
      if(i==exprs.Length-1) cg.ILG.Emit(OpCodes.Brtrue_S, good);
      else cg.ILG.Emit(OpCodes.Brfalse, evil);
    }

    if(exprs.Length!=1) cg.ILG.MarkLabel(evil);
    cg.EmitString("cannot lock a null value");
    cg.EmitNew(typeof(ValueErrorException), new Type[] { typeof(string) });
    cg.ILG.Emit(OpCodes.Throw);

    cg.ILG.MarkLabel(good);
    foreach(Slot s in slots)
    { s.EmitGet(cg);
      cg.EmitCall(typeof(System.Threading.Monitor), "Enter");
    }
  }

  protected override void EmitFinally(CodeGenerator cg)
  { foreach(Slot s in slots)
    { s.EmitGet(cg);
      cg.EmitCall(typeof(System.Threading.Monitor), "Exit");
    }
  }

  protected override void PostEmit(CodeGenerator cg) { foreach(Slot s in slots) cg.FreeLocalTemp(s); }

  Slot[] slots;
}
#endregion

#region PassStatement
public class PassStatement : Statement
{ public override void Emit(CodeGenerator cg) { }
  public override void Execute(Frame frame) { }
  public override void ToCode(System.Text.StringBuilder sb, int indent) { sb.Append("pass"); }
}
#endregion

// FIXME: python puts spaces between arguments
#region PrintStatement
public class PrintStatement : Statement
{ public PrintStatement() { Expressions=null; TrailingNewline=true; }
  public PrintStatement(Expression file) : this() { File=file; }
  public PrintStatement(Expression file, Expression[] exprs, bool trailingNewline)
  { File=file; Expressions=exprs; TrailingNewline=trailingNewline;
  }

  public override void Emit(CodeGenerator cg)
  { Slot file = File==null ? null : cg.AllocLocalTemp(typeof(object));
    if(file!=null)
    { File.Emit(cg);
      file.EmitSet(cg);
    }

    if(Expressions!=null)
      foreach(Expression e in Expressions)
      { if(file==null) cg.ILG.Emit(OpCodes.Ldnull);
        else file.EmitGet(cg);
        e.Emit(cg);
        cg.EmitCall(typeof(Ops), "Print");
      }

    if(TrailingNewline || Expressions.Length==0)
    { if(file==null) cg.ILG.Emit(OpCodes.Ldnull);
      else file.EmitGet(cg);
      cg.EmitCall(typeof(Ops), "PrintNewline");
    }

    if(file!=null) cg.FreeLocalTemp(file);
  }

  public override void Execute(Frame frame)
  { object file = File==null ? null : File.Evaluate(frame);
    if(Expressions!=null) foreach(Expression e in Expressions) Ops.Print(file, e.Evaluate(frame));
    if(TrailingNewline || Expressions.Length==0) Ops.PrintNewline(file);
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
  public Expression   File;
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
      cg.EmitNew(typeof(TypeErrorException), new Type[] { typeof(string) });
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

  public override void Emit(CodeGenerator cg)
  { if(!InGenerator) cg.EmitReturn(Expression);
    else
    { cg.ILG.Emit(OpCodes.Ldc_I4_0);
      cg.EmitReturn();
    }
  }

  public override void Execute(Frame frame)
  { if(InGenerator) throw new NotImplementedException();
    throw new ReturnException(Expression==null ? null : Expression.Evaluate(frame));
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
  public bool InGenerator;
}
#endregion

#region Suite
public class Suite : Statement
{ public Suite(Statement[] stmts) { Statements=stmts; SetLocation(stmts[0].Source, stmts[0].Line, stmts[0].Column); }

  public override void Emit(CodeGenerator cg)
  { Label start, end;
    if(Name!=null)
    { start=cg.ILG.DefineLabel(); end=cg.ILG.DefineLabel();
      Walk(new JumpFinder(Name, start, end));
      cg.ILG.MarkLabel(start);
    }
    else end=new Label();

    foreach(Statement stmt in Statements)
    { cg.EmitPosition(stmt);
      stmt.Emit(cg);
    }

    if(Name!=null) cg.ILG.MarkLabel(end);
  }

  public override void Execute(Frame frame)
  { if(Name==null) foreach(Statement stmt in Statements) stmt.Execute(frame);
    else
    { restart:
      try { foreach(Statement stmt in Statements) stmt.Execute(frame); }
      catch(BreakException e) { if(e.Name!=Name) throw; }
      catch(ContinueException e) { if(e.Name==Name) goto restart; throw; }
    }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('\n');

    if(Name!=null)
    { sb.Append(Name).Append(":\n");
      indent += Options.IndentSize;
    }

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
  public string Name;
}
#endregion

#region TryStatement
public class TryStatement : ExceptionStatement
{ public TryStatement(Statement body, ExceptClause[] except, Statement elze, Statement final)
    : base(body, except) { NeedElse=(elze!=null); NeedFinally=(final!=null); Else=elze; Finally=final; }

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
    w.PostWalk(this);
  }

  public Statement Else, Finally;

  protected override void EmitElse(CodeGenerator cg) { Else.Emit(cg); }
  protected override void EmitFinally(CodeGenerator cg) { Finally.Emit(cg); }
  protected override void ExecuteElse(Frame frame) { Else.Execute(frame); }
  protected override void ExecuteFinally(Frame frame) { Finally.Execute(frame); }
}
#endregion

#region UsingStatement
public class UsingStatement : ExceptionStatement
{ public UsingStatement(Expression expr, Statement body) : base(body, null) { Expression=expr; NeedFinally=true; }

  public override void Execute(Frame frame)
  { if(Expression is TupleExpression)
    { TupleExpression tup = (TupleExpression)Expression;
      IDisposable[] arr = new IDisposable[tup.Expressions.Length];
      for(int i=0; i<arr.Length; i++) arr[i] = (IDisposable)tup.Expressions[i].Evaluate(frame);
      try { Body.Execute(frame); }
      finally { foreach(IDisposable o in arr) if(o!=null) o.Dispose(); }
    }
    else using((IDisposable)Expression.Evaluate(frame)) Body.Execute(frame);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("using ");
    Expression.ToCode(sb, 0);
    sb.Append(":\n");
    StatementToCode(sb, Body, indent+Options.IndentSize);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Expression.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Expression;

  protected override void EmitPreTry(CodeGenerator cg)
  { Expression[] exprs = Expression is TupleExpression ? ((TupleExpression)Expression).Expressions
                                                       : new Expression[] { Expression };
    slots = new Slot[exprs.Length];
    for(int i=0; i<exprs.Length; i++)
    { slots[i] = cg.AllocLocalTemp(typeof(IDisposable), true);
      exprs[i].Emit(cg);
      cg.ILG.Emit(OpCodes.Castclass, typeof(IDisposable));
      slots[i].EmitSet(cg);
    }
  }

  protected override void EmitFinally(CodeGenerator cg)
  { foreach(Slot s in slots)
    { Label skip = cg.ILG.DefineLabel();
      s.EmitGet(cg);
      cg.ILG.Emit(OpCodes.Brfalse_S, skip);
      s.EmitGet(cg);
      cg.EmitCall(typeof(IDisposable), "Dispose");
      cg.ILG.MarkLabel(skip);
    }
  }

  protected override void PostEmit(CodeGenerator cg) { foreach(Slot s in slots) cg.FreeLocalTemp(s); }

  Slot[] slots;
}
#endregion

#region WhileStatement
public class WhileStatement : Statement
{ public WhileStatement(Expression test, Statement body, Statement elze) { Test=test; Body=body; Else=elze; }

  public override void Emit(CodeGenerator cg)
  { if(Options.Optimize && Test.IsConstant && !Ops.IsTrue(Test.GetValue())) return;

    Label start=cg.ILG.DefineLabel(), end=cg.ILG.DefineLabel(), elze=(Else==null ? end : cg.ILG.DefineLabel());
    Body.Walk(new JumpFinder(null, start, end));
    cg.ILG.BeginExceptionBlock();
    cg.ILG.MarkLabel(start);
    if(!Options.Optimize || !Test.IsConstant)
    { Test.Emit(cg);
      cg.EmitCall(typeof(Ops), "IsTrue");
      cg.ILG.Emit(OpCodes.Brfalse, elze);
    }
    Body.Emit(cg);
    cg.ILG.Emit(OpCodes.Br, start);
    if(Else!=null)
    { cg.ILG.MarkLabel(elze);
      Else.Emit(cg);
    }
    cg.ILG.MarkLabel(end);
    cg.ILG.BeginCatchBlock(typeof(StopIterationException));
    cg.ILG.EndExceptionBlock();
  }

  public override void Execute(Frame frame)
  { try
    { while(Ops.IsTrue(Test.Evaluate(frame)))
        try { Body.Execute(frame); }
        catch(BreakException e) { if(e.Name!=null) throw; goto done; }
        catch(StopIterationException) { goto done; }
        catch(ContinueException e) { if(e.Name!=null) throw; }
      if(Else!=null) Else.Execute(frame);
      done:;
    }
    catch(StopIterationException) { }
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
      if(Else!=null) Else.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Test;
  public Statement  Body, Else;
}
#endregion

#region YieldStatement
public class YieldStatement : Statement
{ public YieldStatement(Expression e) { Expression=e; }

  public struct YieldTarget
  { public Statement Statement;
    public Label Label;
  }

  public override void Emit(CodeGenerator cg)
  { cg.ILG.Emit(OpCodes.Ldarg_0);
    cg.EmitInt(YieldNumber);
    cg.EmitFieldSet(typeof(Generator).GetField("jump", BindingFlags.Instance|BindingFlags.NonPublic));
    cg.ILG.Emit(OpCodes.Ldarg_1);
    Expression.Emit(cg);
    cg.ILG.Emit(OpCodes.Stind_Ref);
    cg.ILG.Emit(OpCodes.Ldc_I4_1);
    cg.EmitReturn();
    cg.ILG.MarkLabel(Targets[Targets.Length-1].Label);
  }

  public override void Execute(Frame frame)
  { throw new NotImplementedException();
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append("yield ");
    Expression.ToCode(sb, 0);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
  public YieldTarget[] Targets;
  public int YieldNumber;
}
#endregion

} // namespace Boa.AST
