using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

// FIXME: 'while' and 'for' don't catch StopIterationException in the compiled versions
// FIXME: update post processing to know about 'for' loops and tuple assignment
// TODO: using exceptions is extremely slow! stop it!
#region Exceptions (used to aid implementation)
public class ContinueException : Exception
{ public static ContinueException Value = new ContinueException();
}
public class ReturnException : Exception
{ public ReturnException(object value) { Value=value; }
  public object Value;
}
#endregion

#region JumpFinder
class JumpFinder : IWalker
{ public JumpFinder(Label start, Label end) { this.start=start; this.end=end; }

  public void PostWalk(Node node) { }

  public bool Walk(Node node)
  { if(node is BreakStatement) ((BreakStatement)node).Label = end;
    else if(node is ContinueStatement) ((ContinueStatement)node).Label = start;
    else if(node is WhileStatement) return false;
    return true;
  }

  Label start, end;
}
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
          { def.Inherit = (Name[])inherit.ToArray(typeof(Name));
            inherit.Clear();
          }
        }
      }
    }

    public bool Walk(Node node)
    { if(inDef)
      { if(node is DefStatement)
        { DefStatement de = (DefStatement)node;
          innerFuncs.Add(node);
          de.Name = AddName(de.Name);
          de.Name.Scope = Scope.Local;
          return false;
        }
        else if(node is NameExpression)
        { NameExpression ne = (NameExpression)node;
          ne.Name = AddName(ne.Name);
        }
        else if(node is AssignStatement)
        { AssignStatement ass = (AssignStatement)node;
          if(ass.LHS is NameExpression)
          { NameExpression ne = (NameExpression)ass.LHS;
            ne.Name = AddName(ne.Name);
            ne.Name.Scope = Scope.Local;
          }
          else throw Ops.NotImplemented("Unhandled type '{0}' in NameDecorator", node.GetType());
          Walk(ass.RHS);
          return false;
        }
      }
      else if(node is DefStatement)
      { if(innerFuncs==null)
        { innerFuncs = new ArrayList();
          names = new SortedList();
        }

        DefStatement def = (DefStatement)node;
        foreach(Parameter p in def.Parameters)
        { if(def.Globals!=null)
            for(int i=0; i<def.Globals.Length; i++) if(def.Globals[i].String==p.Name.String)
              throw Ops.SyntaxError("'{0}' is both local and global", p.Name.String);
          names[p.Name.String] = p.Name;
        }
        current=def; inDef=true;
      }
      return true;
    }
    
    Name AddName(Name name)
    { Name lname = (Name)names[name.String];
      if(lname==null) names[name.String] = lname = name;
      return lname;
    }

    DefStatement current;
    ArrayList  innerFuncs;
    SortedList names;
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

  public override void Execute(Frame frame) { foreach(Statement stmt in Statements) stmt.Execute(frame); }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Statement stmt in Statements) stmt.Walk(w);
    w.PostWalk(this);
  }

  public Statement[] Statements;
}
#endregion

#region AssignStatement
public class AssignStatement : Statement
{ public AssignStatement(Expression lhs) { LHS=lhs; }
  public AssignStatement(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { if(LHS is TupleExpression)
    { TupleExpression lhs = (TupleExpression)LHS;
      if(false/*RHS.IsConstant*/) // TODO: implement this optimization
      { 
      }
      else
      { 
        #region lots of MSIL code here
        Label notseq = cg.ILG.DefineLabel(), notstr = cg.ILG.DefineLabel(), badlen = cg.ILG.DefineLabel(),
              end = cg.ILG.DefineLabel();
        Slot lenslot = cg.AllocLocalTemp(typeof(int)), seqslot = cg.AllocLocalTemp(typeof(ISequence)),
             strslot = cg.AllocLocalTemp(typeof(string)), objslot = cg.AllocLocalTemp(typeof(object));

        if(RHS!=null) RHS.Emit(cg);
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
          cg.EmitCall(typeof(ISequence), "__getitem__");
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
        cg.EmitString("too many values to unpack (expected {0}, got {1})");
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
        cg.EmitCall(typeof(Ops), "ValueError");
        cg.ILG.Emit(OpCodes.Throw);
        cg.ILG.MarkLabel(end); // phew!
        #endregion
      }
    }
    else
    { if(RHS!=null) RHS.Emit(cg);
      LHS.EmitSet(cg);
    }
  }

  public override void Execute(Frame frame) { LHS.Assign(RHS.Evaluate(frame), frame); }

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

#region BreakStatement
public class BreakStatement : Statement
{ public override void Emit(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Br, Label); }
  public override void Execute(Frame frame) { throw new StopIterationException(); }

  public Label Label;
}
#endregion

#region ContinueStatement
public class ContinueStatement : Statement
{ public override void Emit(CodeGenerator cg) { cg.ILG.Emit(OpCodes.Br, Label); }
  public override void Execute(Frame frame) { throw ContinueException.Value; }

  public Label Label;
}
#endregion

#region DefStatement
public class DefStatement : Statement
{ public DefStatement(string name, Parameter[] parms, Statement body)
  { Name=new Name(name); Parameters=parms; Body=body;
    GlobalFinder gf = new GlobalFinder();
    Body.Walk(gf);
    Globals = gf.Globals==null || gf.Globals.Count==0 ? null : (Name[])gf.Globals.ToArray(typeof(Name));
  }

  public override void Emit(CodeGenerator cg)
  { CodeGenerator impl = MakeImplMethod(cg);

    Type targetType = Inherit==null ? typeof(CallTargetN) : typeof(CallTargetFN);
    Type funcType   = Inherit==null ? typeof(CompiledFunctionN) : typeof(CompiledFunctionFN);
    Slot funcSlot   = cg.Namespace.GetSlotForSet(Name);

    cg.EmitString(Name.String);
    GetParmsSlot(cg).EmitGet(cg);
    EmitClosedGet(cg);
    cg.ILG.Emit(OpCodes.Ldnull); // create delegate
    cg.ILG.Emit(OpCodes.Ldftn, impl.MethodBuilder);
    cg.EmitNew((ConstructorInfo)targetType.GetMember(".ctor")[0]);
    cg.EmitNew(funcType, new Type[] { typeof(string), typeof(Parameter[]), typeof(ClosedVar[]), targetType });
    funcSlot.EmitSet(cg);
    index++;
  }

  public override void Execute(Frame frame) { frame.Set(Name.String, MakeFunction(frame)); }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this)) Body.Walk(w);
    w.PostWalk(this);
  }

  public Name[] Inherit, Globals;
  public Parameter[] Parameters;
  public Name Name;
  public Statement Body;

  class GlobalFinder : IWalker
  { public void PostWalk(Node node) { }

    public bool Walk(Node node)
    { if(node is DefStatement) return false;
      else if(node is GlobalStatement)
      { if(Globals==null) Globals = new ArrayList();
        foreach(Name n in ((GlobalStatement)node).Names)
          if(assigned!=null && assigned.Contains(n.String))
            throw Ops.SyntaxError("'{0}' assigned to before associated 'global' statement", n.String);
          else Globals.Add(n);
        return false;
      }
      else if(node is AssignStatement)
      { AssignStatement ass = (AssignStatement)node;
        if(ass.LHS is NameExpression)
        { if(assigned==null) assigned=new HybridDictionary();
          assigned[((NameExpression)ass.LHS).Name.String] = null;
        }
        return false;
      }
      return true;
    }
    
    public ArrayList Globals;
    HybridDictionary assigned;
  }

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

  object MakeFunction(Frame frame) { return new InterpretedFunction(frame, Name.String, Parameters, Globals, Body); }
  
  Slot namesSlot;
  static int index;
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

  public override void Execute(Frame frame)
  { if(Ops.IsTrue(Test.Evaluate(frame))) Body.Execute(frame);
    else if(Else!=null) Else.Execute(frame);
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

    Walk(new JumpFinder(start, end));
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

    IEnumerator e = Ops.GetEnumerator(Expression.Evaluate(frame));
    while(e.MoveNext())
    { ce.Value = e.Current;
      ass.Execute(frame);
      try { Body.Execute(frame); }
      catch(StopIterationException) { break; }
      catch(ContinueException) { }
    }
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

  public Name[] Names;
}
#endregion

#region PassStatement
public class PassStatement : Statement
{ public override void Emit(CodeGenerator cg) { }
  public override void Execute(Frame frame) { }
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

  public override void Walk(IWalker w)
  { if(w.Walk(this) && Expressions!=null) foreach(Expression e in Expressions) e.Walk(w);
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
  public override void Execute(Frame frame)
  { throw new ReturnException(Expression==null ? null : Expression.Evaluate(frame));
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this) && Expression!=null) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}
#endregion

#region WhileStatement
public class WhileStatement : Statement
{ public WhileStatement(Expression test, Statement body) { Test=test; Body=body; }

  public override void Emit(CodeGenerator cg)
  { Label start=cg.ILG.DefineLabel(), end=cg.ILG.DefineLabel();
    Walk(new JumpFinder(start, end));
    cg.ILG.MarkLabel(start);
    Test.Emit(cg);
    cg.EmitCall(typeof(Ops), "IsTrue");
    cg.ILG.Emit(OpCodes.Brfalse, end);
    Body.Emit(cg);
    cg.ILG.Emit(OpCodes.Br, start);
    cg.ILG.MarkLabel(end);
  }

  public override void Execute(Frame frame)
  { while(Ops.IsTrue(Test.Evaluate(frame)))
      try { Body.Execute(frame); }
      catch(StopIterationException) { break; }
      catch(ContinueException) { }
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
