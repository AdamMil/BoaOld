using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

// TODO: add versions optimized for System.Array ?

#region Expression
public abstract class Expression : Node
{ public virtual void Assign(object value, Frame frame) { throw new NotSupportedException("Assign: "+GetType()); }
  public virtual void Delete(Frame frame) { throw new NotSupportedException("Delete: "+GetType()); }
  public abstract void Emit(CodeGenerator cg);
  public virtual void EmitSet(CodeGenerator cg) { throw new NotSupportedException("EmitSet: "+GetType()); }
  public virtual void EmitDel(CodeGenerator cg) { throw new NotSupportedException("EmitDel: "+GetType()); }
  public abstract object Evaluate(Frame frame);

  public override object GetValue()
  { if(!IsConstant) throw new InvalidOperationException();
    return Evaluate(null);
  }
}
#endregion

#region AndExpression
public class AndExpression : BinaryExpression
{ public AndExpression(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { LHS.Emit(cg);
      cg.ILG.Emit(OpCodes.Dup);
      cg.EmitIsTrue();
      Label lab = cg.ILG.DefineLabel();
      cg.ILG.Emit(OpCodes.Brfalse, lab);
      cg.ILG.Emit(OpCodes.Pop);
      RHS.Emit(cg);
      cg.ILG.MarkLabel(lab);
    }
  }

  public override object Evaluate(Frame frame)
  { object value = LHS.Evaluate(frame);
    return Ops.IsTrue(value) ? RHS.Evaluate(frame) : value;
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { LHS.ToCode(sb, 0);
    sb.Append(" && ");
    RHS.ToCode(sb, 0);
  }
}
#endregion

#region AttrExpression
public class AttrExpression : Expression
{ public AttrExpression(Expression o, string attr) { Object=o; Attribute=attr; }

  public override void Assign(object value, Frame frame) { Ops.SetAttr(value, Object.Evaluate(frame), Attribute); }

  public override void Delete(Frame frame) { Ops.DelAttr(Object.Evaluate(frame), Attribute); }

  public override void Emit(CodeGenerator cg)
  { Object.Emit(cg);
    cg.EmitString(Attribute);
    cg.EmitCall(typeof(Ops), "GetAttr", new Type[] { typeof(object), typeof(string) });
  }

  public override void EmitDel(CodeGenerator cg)
  { Object.Emit(cg);
    cg.EmitString(Attribute);
    cg.EmitCall(typeof(Ops), "DelAttr", new Type[] { typeof(object), typeof(string) });
  }

  public override void EmitSet(CodeGenerator cg)
  { Object.Emit(cg);
    cg.EmitString(Attribute);
    cg.EmitCall(typeof(Ops), "SetAttr", new Type[] { typeof(object), typeof(object), typeof(string) });
  }

  public override object Evaluate(Frame frame) { return Ops.GetAttr(Object.Evaluate(frame), Attribute); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Object.ToCode(sb, 0);
    sb.Append('.');
    sb.Append(Attribute);
  }

  public Expression Object;
  public string Attribute;
}
#endregion

#region BinaryExpression
public abstract class BinaryExpression : Expression
{ public override void Optimize() { IsConstant = LHS.IsConstant && RHS.IsConstant; }

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

#region BinaryOpExpression
public class BinaryOpExpression : BinaryExpression
{ public BinaryOpExpression(BinaryOperator op, Expression lhs, Expression rhs) { Op=op; LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { LHS.Emit(cg);
      RHS.Emit(cg);
      Op.Emit(cg);
    }
  }

  public override object Evaluate(Frame frame) { return Op.Evaluate(LHS.Evaluate(frame), RHS.Evaluate(frame)); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { LHS.ToCode(sb, 0);
    sb.Append(Op.ToString());
    RHS.ToCode(sb, 0);
  }

  BinaryOperator Op;
}
#endregion

#region CallExpression
public class CallExpression : Expression
{ public CallExpression(Expression target, Argument[] args) { Arguments=args; Target=target; }

  public override void Emit(CodeGenerator cg)
  { int numlist=0, numdict=0, numruns=0, numnamed=0, runlen=0;
    for(int i=0; i<Arguments.Length; i++)
      switch(Arguments[i].Type)
      { case ArgType.Normal:
          if(Arguments[i].Name==null) runlen++;
          else numnamed++;
          break;
        case ArgType.List: 
          if(runlen>0) { numruns++; runlen=0; }
          numlist++;
          break;
        case ArgType.Dict: numdict++; break;
      }
    if(runlen>0) { numruns++; runlen=0; }
    if(numnamed>0) numruns++;
    numruns += numlist + numdict;

    Target.Emit(cg);
    if(numlist==0 && numdict==0)
    { if(numnamed==0)
      { EmitRun(cg, Arguments.Length, 0, Arguments.Length);
        cg.EmitCall(typeof(Ops), "Call", new Type[] { typeof(object), typeof(object[]) });
      }
      else
      { EmitRun(cg, Arguments.Length-numnamed, 0, Arguments.Length);
        EmitNamed(cg, numnamed);
        cg.EmitCall(typeof(Ops), "Call",
                    new Type[] { typeof(object), typeof(object[]), typeof(string[]), typeof(object[]) });
      }
    }
    else // TODO: is it worth optimizing constant list args?
    { ConstructorInfo ci = typeof(CallArg).GetConstructor(new Type[] { typeof(object), typeof(int) });
      int ri=0, rsi=0;

      cg.EmitNewArray(typeof(CallArg), numruns);
      
      for(int i=0; i<Arguments.Length; i++) // first we'll do positional arguments
      { if(Arguments[i].Name!=null || Arguments[i].Type==ArgType.Dict) continue;
        if(Arguments[i].Type==ArgType.Normal) runlen++;
        else
        { if(runlen>1)
          { cg.ILG.Emit(OpCodes.Dup);
            cg.EmitInt(ri++);
            cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
            EmitRun(cg, runlen, rsi, i);
            cg.EmitInt(runlen);
            cg.ILG.Emit(OpCodes.Box, typeof(int));
            cg.EmitNew(ci);
            cg.ILG.Emit(OpCodes.Stobj, typeof(CallArg));
          }
          else if(runlen==1)
          { cg.ILG.Emit(OpCodes.Dup);
            cg.EmitInt(ri++);
            cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
            for(; rsi<i; rsi++) if(Arguments[rsi].Name==null) { Arguments[rsi].Expression.Emit(cg); break; }
            cg.ILG.Emit(OpCodes.Ldnull);
            cg.EmitNew(ci);
            cg.ILG.Emit(OpCodes.Stobj, typeof(CallArg));
          }

          cg.ILG.Emit(OpCodes.Dup);
          cg.EmitInt(ri++);
          cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
          Arguments[i].Expression.Emit(cg);
          cg.EmitFieldGet(typeof(CallArg), "ListType");
          cg.EmitNew(ci);
          cg.ILG.Emit(OpCodes.Stobj, typeof(CallArg));

          runlen=0; rsi=i;
        }
      }
      
      if(numnamed>0) // then named arguments
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(ri++);
        cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
        EmitNamed(cg, numnamed);
        cg.EmitNew(ci);
        cg.ILG.Emit(OpCodes.Stobj, typeof(CallArg));
      }
      
      for(int i=0; i<Arguments.Length; i++)
        if(Arguments[i].Type==ArgType.Dict)
        { cg.ILG.Emit(OpCodes.Dup);
          cg.EmitInt(ri++);
          cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
          Arguments[i].Expression.Emit(cg);
          cg.EmitFieldGet(typeof(CallArg), "DictType");
          cg.EmitNew(ci);
          cg.ILG.Emit(OpCodes.Stobj, typeof(CallArg));
        }
      
      cg.EmitCall(typeof(Ops), "Call", new Type[] { typeof(object), typeof(CallArg[]) });
    }
  }

  public override object Evaluate(Frame frame)
  { object callee = Target.Evaluate(frame);
    if(Arguments.Length==0) return Ops.Call0(callee);

    int numlist=0, numdict=0, numruns=0, numnamed=0, runlen=0;
    for(int i=0; i<Arguments.Length; i++)
      switch(Arguments[i].Type)
      { case ArgType.Normal:
          if(Arguments[i].Name==null) runlen++;
          else numnamed++;
          break;
        case ArgType.List: 
          if(runlen>0) { numruns++; runlen=0; }
          numlist++;
          break;
        case ArgType.Dict: numdict++; break;
      }
    if(runlen>0) { numruns++; runlen=0; }
    if(numnamed>0) numruns++;
    numruns += numlist + numdict;

    if(numlist==0 && numdict==0)
    { if(numnamed==0) return Ops.Call(callee, EvaluateRun(frame, Arguments.Length, 0, Arguments.Length));
      else
      { string[] names;
        object[] values;
        EvaluateNamed(frame, numnamed, out names, out values);
        return Ops.Call(callee, EvaluateRun(frame, Arguments.Length-numnamed, 0, Arguments.Length), names, values);
      }
    }
    else // TODO: is it worth optimizing constant list args?
    { int ri=0, rsi=0;
      CallArg[] cargs = new CallArg[numruns];

      for(int i=0; i<Arguments.Length; i++) // first we'll do positional arguments
      { if(Arguments[i].Name!=null || Arguments[i].Type==ArgType.Dict) continue;
        if(Arguments[i].Type==ArgType.Normal) runlen++;
        else
        { if(runlen>1) cargs[ri++] = new CallArg(EvaluateRun(frame, runlen, rsi, i), runlen);
          else if(runlen==1)
          { for(; rsi<i; rsi++)
              if(Arguments[rsi].Name==null)
              { cargs[ri++] = new CallArg(Arguments[rsi].Expression.Evaluate(frame), null);
                break;
              }
          }

          cargs[ri++] = new CallArg(Arguments[i].Expression.Evaluate(frame), CallArg.ListType);
          runlen=0; rsi=i;
        }
      }
      
      if(numnamed>0) // then named arguments
      { string[] names;
        object[] values;
        EvaluateNamed(frame, numnamed, out names, out values);
        cargs[ri++] = new CallArg(names, values);
      }
      
      for(int i=0; i<Arguments.Length; i++)
        if(Arguments[i].Type==ArgType.Dict)
          cargs[ri++] = new CallArg(Arguments[i].Expression.Evaluate(frame), CallArg.DictType);

      return Ops.Call(callee, cargs);
    }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Target.ToCode(sb, 0);
    sb.Append('(');
    for(int i=0; i<Arguments.Length; i++)
    { if(i!=0) sb.Append(", ");
      Arguments[i].ToCode(sb);
    }
    sb.Append(')');
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Target.Walk(w);
    w.PostWalk(this);
  }

  public Argument[] Arguments;
  public Expression Target;
  
  void EmitNamed(CodeGenerator cg, int num)
  { cg.EmitNewArray(typeof(string), num);
    for(int i=0,ai=0; i<Arguments.Length; i++)
      if(Arguments[i].Name!=null)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(ai++);
        cg.EmitString(Arguments[i].Name);
        cg.ILG.Emit(OpCodes.Stelem_Ref);
      }
    cg.EmitNewArray(typeof(object), num);
    for(int i=0,ai=0; i<Arguments.Length; i++)
      if(Arguments[i].Name!=null)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(ai++);
        Arguments[i].Expression.Emit(cg);
        cg.ILG.Emit(OpCodes.Stelem_Ref);
      }
  }

  void EmitRun(CodeGenerator cg, int length, int start, int end)
  { if(length==0) { cg.ILG.Emit(OpCodes.Ldnull); return; }
    cg.EmitNewArray(typeof(object), length);
    for(int ai=0; start<end; start++)
      if(Arguments[start].Name==null)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(ai++);
        Arguments[start].Expression.Emit(cg);
        cg.ILG.Emit(OpCodes.Stelem_Ref);
      }
  }
  
  void EvaluateNamed(Frame frame, int num, out string[] names, out object[] values)
  { names = new string[num];
    values = new object[num];
    for(int i=0,ai=0; i<Arguments.Length; i++)
      if(Arguments[i].Name!=null)
      { names[ai] = Arguments[i].Name;
        values[ai] = Arguments[i].Expression.Evaluate(frame);
        ai++;
      }
  }
  
  object[] EvaluateRun(Frame frame, int length, int start, int end)
  { if(length==0) return null;
    object[] values = new object[length];
    for(int ai=0; start<end; start++)
      if(Arguments[start].Name==null) values[ai++] = Arguments[start].Expression.Evaluate(frame);
    return values;
  }
}
#endregion

#region ConstantExpression
public class ConstantExpression : Expression
{ public ConstantExpression(object value) { Value=value; IsConstant=true; }
  
  public override void Emit(CodeGenerator cg) { cg.EmitConstant(Value); }
  public override object Evaluate(Frame frame) { return Value; }
  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { if(Value==null) sb.Append("null");
    else if(Value is bool) sb.Append((bool)Value ? "true" : "false");
    else if(Value is string) sb.Append(StringOps.Escape((string)Value));
    else sb.Append(Value);
  }

  public object Value;
}
#endregion

#region HashExpression
// TODO: check for duplicate entries
public class HashExpression : Expression
{ public HashExpression() : this(new DictionaryEntry[0]) { }
  public HashExpression(DictionaryEntry[] entries) { Entries=entries; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant)
    { cg.EmitConstant(GetValue());
      cg.EmitCall(typeof(Dict), "Clone");
    }
    else
    { MethodInfo mi = typeof(Dict).GetMethod("set_Item");
      cg.EmitNew(typeof(Dict));
      foreach(DictionaryEntry e in Entries)
      { cg.ILG.Emit(OpCodes.Dup);
        ((Expression)e.Key).Emit(cg);
        ((Expression)e.Value).Emit(cg);
        cg.EmitCall(mi);
      }
    }
  }

  public override object Evaluate(Frame frame)
  { Dict dict = new Dict();
    foreach(DictionaryEntry e in Entries)
      dict[((Expression)e.Key).Evaluate(frame)] = ((Expression)e.Value).Evaluate(frame);
    return dict;
  }

  public override void Optimize()
  { foreach(DictionaryEntry de in Entries)
      if(!((Expression)de.Key).IsConstant || !((Expression)de.Value).IsConstant) return;
    IsConstant = true;
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('{');
    for(int i=0; i<Entries.Length; i++)
    { if(i!=0) sb.Append(", ");
      ((Expression)Entries[i].Key).ToCode(sb, 0);
      sb.Append(':');
      ((Expression)Entries[i].Value).ToCode(sb, 0);
    }
    sb.Append('}');
  }

  public DictionaryEntry[] Entries;
}
#endregion

#region IndexExpression
public class IndexExpression : Expression
{ public IndexExpression(Expression obj, Expression index) { Object=obj; Index=index; }

  public override void Assign(object value, Frame frame)
  { Ops.SetIndex(value, Object.Evaluate(frame), Index.Evaluate(frame));
  }

  public override void Delete(Frame frame) { Ops.DelIndex(Object.Evaluate(frame), Index.Evaluate(frame)); }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { Object.Emit(cg);
      Index.Emit(cg);
      cg.EmitCall(typeof(Ops), "GetIndex");
    }
  }

  public override void EmitDel(CodeGenerator cg)
  { Object.Emit(cg);
    Index.Emit(cg);
    cg.EmitCall(typeof(Ops), "DelIndex");
  }

  public override void EmitSet(CodeGenerator cg)
  { Object.Emit(cg);
    Index.Emit(cg);
    cg.EmitCall(typeof(Ops), "SetIndex");
  }
  
  public override object Evaluate(Frame frame) { return Ops.GetIndex(Object.Evaluate(frame), Index.Evaluate(frame)); }
  public override void Optimize() { IsConstant = Object.IsConstant && Index.IsConstant; }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Object.ToCode(sb, 0);
    sb.Append('[');
    Index.ToCode(sb, 0);
    sb.Append(']');
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Object.Walk(w);
      Index.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Object, Index;
}
#endregion

#region LambdaExpression
public class LambdaExpression : Expression
{ public LambdaExpression(Parameter[] parms, Statement body)
  { if(body is ExpressionStatement) body = new ReturnStatement(((ExpressionStatement)body).Expression);
    else if(body is Suite)
    { Suite suite = (Suite)body;
      if(suite.Statements.Length==1 && suite.Statements[0] is ExpressionStatement)
        body = new ReturnStatement(((ExpressionStatement)suite.Statements[0]).Expression);
    }
    Function = new BoaFunction(this, parms, body);
  }

  public override void Emit(CodeGenerator cg) { Function.Emit(cg); }

  public override object Evaluate(Frame frame) { return Function.MakeFunction(frame); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append(Function.Parameters.Length==0 ? "lambda" : "lambda ");
    for(int i=0; i<Function.Parameters.Length; i++)
    { if(i!=0) sb.Append(", ");
      Function.Parameters[i].ToCode(sb);
    }
    sb.Append(": ");
    if(Function.Body is Suite)
    { Suite suite = (Suite)Function.Body;
      for(int i=0; i<suite.Statements.Length; i++)
      { if(i!=0) sb.Append("; ");
        suite.Statements[i].ToCode(sb, 0);
      }
    }
    else Function.Body.ToCode(sb, 0);
  }

  public override void Walk(IWalker w) { Function.Walk(w); }

  public BoaFunction Function;
}
#endregion

#region ListExpression
public class ListExpression : Expression
{ public ListExpression() : this(new Expression[0]) { }
  public ListExpression(Expression[] expressions) { Expressions=expressions; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant)
    { cg.EmitConstant(GetValue());
      cg.EmitCall(typeof(List), "Clone");
    }
    else
    { MethodInfo mi = typeof(List).GetMethod("append");
      cg.EmitInt(Expressions.Length);
      cg.EmitNew(typeof(List), new Type[] { typeof(int) });
      foreach(Expression e in Expressions)
      { cg.ILG.Emit(OpCodes.Dup);
        e.Emit(cg);
        cg.EmitCall(mi);
      }
    }
  }
  
  public override object Evaluate(Frame frame)
  { List list = new List(Expressions.Length);
    foreach(Expression e in Expressions) list.append(e.Evaluate(frame));
    return list;
  }

  public override void Optimize()
  { foreach(Expression e in Expressions) if(!e.IsConstant) return;
    IsConstant = true;
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('[');
    for(int i=0; i<Expressions.Length; i++)
    { if(i!=0) sb.Append(", ");
      Expressions[i].ToCode(sb, 0);
    }
    sb.Append(']');
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
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { AssignStatement ass = new AssignStatement(Names);
      Label next = cg.ILG.DefineLabel(), end = cg.ILG.DefineLabel();
      Slot  list = cg.AllocLocalTemp(typeof(List));

      cg.EmitNew(typeof(List), Type.EmptyTypes);
      list.EmitSet(cg);
      List.Emit(cg);
      cg.EmitCall(typeof(Ops), "GetEnumerator", new Type[] { typeof(object) });
      cg.ILG.MarkLabel(next);
      cg.ILG.Emit(OpCodes.Dup);
      cg.EmitCall(typeof(IEnumerator), "MoveNext");
      cg.ILG.Emit(OpCodes.Brfalse, end);
      cg.ILG.Emit(OpCodes.Dup);
      cg.EmitPropGet(typeof(IEnumerator), "Current");
      ass.Emit(cg);
      if(Test!=null)
      { Test.Emit(cg);
        cg.EmitIsTrue();
        cg.ILG.Emit(OpCodes.Brfalse, next);
      }
      list.EmitGet(cg);
      Item.Emit(cg);
      cg.EmitCall(typeof(List), "append");
      cg.ILG.Emit(OpCodes.Br, next);
      cg.ILG.MarkLabel(end);
      cg.ILG.Emit(OpCodes.Pop);
      list.EmitGet(cg);
      cg.FreeLocalTemp(list);
    }
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

  public override void Optimize()
  { // TODO: implement this
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('[');
    Item.ToCode(sb, 0);
    sb.Append(" for ");
    Names.ToCode(sb, 0);
    sb.Append(" in ");
    List.ToCode(sb, 0);
    if(Test!=null)
    { sb.Append(" if ");
      Test.ToCode(sb, 0);
    }
    sb.Append(']');
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
  public override void Delete(Frame frame) { frame.Delete(Name.String); }
  public override void Emit(CodeGenerator cg) { cg.EmitGet(Name); }
  public override void EmitDel(CodeGenerator cg) { cg.EmitDel(Name); }
  public override void EmitSet(CodeGenerator cg) { cg.EmitSet(Name); }
  public override object Evaluate(Frame frame) { return frame.Get(Name.String); }
  public override void ToCode(System.Text.StringBuilder sb, int indent) { sb.Append(Name.String); }

  public Name Name;
}
#endregion

#region OrExpression
public class OrExpression : BinaryExpression
{ public OrExpression(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { LHS.Emit(cg);
      cg.ILG.Emit(OpCodes.Dup);
      cg.EmitIsTrue();
      Label lab = cg.ILG.DefineLabel();
      cg.ILG.Emit(OpCodes.Brtrue, lab);
      cg.ILG.Emit(OpCodes.Pop);
      RHS.Emit(cg);
      cg.ILG.MarkLabel(lab);
    }
  }

  public override object Evaluate(Frame frame)
  { object value = LHS.Evaluate(frame);
    return Ops.IsTrue(value) ? value : RHS.Evaluate(frame);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { LHS.ToCode(sb, 0);
    sb.Append(" || ");
    RHS.ToCode(sb, 0);
  }
}
#endregion

#region ParenExpression
public class ParenExpression : Expression
{ public ParenExpression(Expression e) { Expression=e; }

  public override void Assign(object value, Frame frame) { Expression.Assign(value, frame); }
  public override void Delete(Frame frame) { Expression.Delete(frame); }
  public override void Emit(CodeGenerator cg) { Expression.Emit(cg); }
  public override void EmitDel(CodeGenerator cg) { Expression.EmitDel(cg); }
  public override void EmitSet(CodeGenerator cg) { Expression.EmitSet(cg); }
  public override object Evaluate(Frame frame) { return Expression.Evaluate(frame); }
  public override void Optimize() { IsConstant = Expression.IsConstant; }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('(');
    Expression.ToCode(sb, 0);
    sb.Append(')');
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) Expression.Walk(w);
    w.PostWalk(this);
  }

  public Expression Expression;
}

#endregion

#region SliceExpression
public class SliceExpression : Expression
{ public SliceExpression(Expression start, Expression stop) : this(start, stop, null) { }
  public SliceExpression(Expression start, Expression stop, Expression step) { Start=start; Stop=stop; Step=step; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { Start.Emit(cg);
      Stop.Emit(cg);
      if(Step==null) cg.ILG.Emit(OpCodes.Ldnull);
      else Step.Emit(cg);
      cg.EmitNew(typeof(Slice), new Type[] { typeof(object), typeof(object), typeof(object) });
    }
  }
  
  public override object Evaluate(Frame frame)
  { return new Slice(Start.Evaluate(frame), Stop.Evaluate(frame), Step==null ? null : Step.Evaluate(frame));
  }

  public override void Optimize()
  { IsConstant = Start.IsConstant && Stop.IsConstant && Step==null || Step.IsConstant;
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Start.ToCode(sb, 0);
    sb.Append(':');
    Stop.ToCode(sb, 0);
    if(Step!=null)
    { sb.Append(':');
      Step.ToCode(sb, 0);
    }
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Start.Walk(w);
      Stop.Walk(w);
      if(Step!=null) Step.Walk(w);
    }
    w.PostWalk(this);
  }

  public Expression Start, Stop, Step;
}
#endregion

#region TernaryExpression
public class TernaryExpression : Expression
{ public TernaryExpression(Expression test, Expression ifTrue, Expression ifFalse)
  { Test=test; IfTrue=ifTrue; IfFalse=ifFalse;
  }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
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
  }
  
  public override object Evaluate(Frame frame)
  { return Ops.IsTrue(Test.Evaluate(frame)) ? IfTrue.Evaluate(frame) : IfFalse.Evaluate(frame);
  }
  
  public override void Optimize()
  { if(Test.IsConstant)
    { bool isTrue = Ops.IsTrue(Test.GetValue());
      IsConstant = isTrue && IfTrue.IsConstant || !isTrue && IfFalse.IsConstant;
    }
    else IsConstant = false;
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Test.ToCode(sb, 0);
    sb.Append(" ? ");
    IfTrue.ToCode(sb, 0);
    sb.Append(" : ");
    IfFalse.ToCode(sb, 0);
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
      if(Ops.ToInt(Ops.Call(Ops.GetAttr(value, "__len__")))!=Expressions.Length)
        throw Ops.ValueError("too many values to unpack");
      for(int i=0; i<Expressions.Length; i++) Expressions[i].Assign(Ops.Call(getitem, i), frame);
    }
  }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { cg.EmitObjectArray(Expressions);
      cg.EmitNew(typeof(Tuple), new Type[] { typeof(object[]) });
    }
  }

  public override object Evaluate(Frame frame)
  { object[] arr = new object[Expressions.Length];
    for(int i=0; i<arr.Length; i++) arr[i] = Expressions[i].Evaluate(frame);
    return new Tuple(arr);
  }

  public override void Optimize()
  { foreach(Expression e in Expressions) if(!e.IsConstant) return;
    IsConstant = true;
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('(');
    for(int i=0; i<Expressions.Length; i++)
    { if(i!=0) sb.Append(", ");
      Expressions[i].ToCode(sb, 0);
    }
    if(Expressions.Length==1) sb.Append(',');
    sb.Append(')');
  }

  public Expression[] Expressions;
}
#endregion

#region UnaryExpression
public class UnaryExpression : Expression
{ public UnaryExpression(Expression expr, UnaryOperator op) { Expression=expr; Op=op; }

  public override object Evaluate(Frame frame) { return Op.Evaluate(Expression.Evaluate(frame)); }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { Expression.Emit(cg);
      Op.Emit(cg);
    }
  }

  public override void Optimize() { IsConstant=Expression.IsConstant; }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append(Op.ToString());
    Expression.ToCode(sb, 0);
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
