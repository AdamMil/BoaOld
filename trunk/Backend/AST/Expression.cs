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
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

// TODO: implement decimal: http://python.org/peps/pep-0327.html
// TODO: implement other stuff here: http://python.org/2.4/highlights.html
// TODO: implement sets
// FIXME: allow generator functions/expressions to access closed variables
//   eg: def fun(n): return (i+n for i in range(10)) # should be able to access 'n' after return
// TODO: implement interpreted generator functions/expressions

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
{ public AndExpression(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; SetLocation(lhs); }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else if(LHS.IsConstant && Ops.IsTrue(LHS.GetValue())) RHS.Emit(cg);
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

  public override void Optimize()
  { IsConstant = LHS.IsConstant ? Ops.IsTrue(LHS.GetValue()) ? RHS.IsConstant : true : false;
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
  
  public override void Walk(IWalker w)
  { if(w.Walk(this)) Object.Walk(w);
    w.PostWalk(this);
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
{ public BinaryOpExpression(BinaryOperator op, Expression lhs, Expression rhs)
  { Op=op; LHS=lhs; RHS=rhs;
    SetLocation(lhs);
  }

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
      { if(Arguments.Length==0) cg.EmitFieldGet(typeof(Misc), "EmptyArray");
        else EmitRun(cg, Arguments.Length, 0, Arguments.Length);
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
        { if(runlen>0)
          { cg.ILG.Emit(OpCodes.Dup);
            cg.EmitInt(ri++);
            cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
            if(runlen>1)
            { EmitRun(cg, runlen, rsi, i);
              cg.EmitInt(runlen);
              cg.ILG.Emit(OpCodes.Box, typeof(int));
            }
            else
            { for(; rsi<i; rsi++) if(Arguments[rsi].Name==null) { Arguments[rsi].Expression.Emit(cg); break; }
              cg.ILG.Emit(OpCodes.Ldnull);
            }
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

          runlen=0; rsi=i+1;
        }
      }
      if(runlen>0)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.EmitInt(ri++);
        cg.ILG.Emit(OpCodes.Ldelema, typeof(CallArg));
        if(runlen>1)
        { EmitRun(cg, runlen, rsi, Arguments.Length);
          cg.EmitInt(runlen);
          cg.ILG.Emit(OpCodes.Box, typeof(int));
        }
        else
        { for(; rsi<Arguments.Length; rsi++)
            if(Arguments[rsi].Name==null) { Arguments[rsi].Expression.Emit(cg); break; }
          cg.ILG.Emit(OpCodes.Ldnull);
        }
        cg.EmitNew(ci);
        cg.ILG.Emit(OpCodes.Stobj, typeof(CallArg));
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
    { if(numnamed==0)
      { object[] args = Arguments.Length==0 ? Misc.EmptyArray
                                            : EvaluateRun(frame, Arguments.Length, 0, Arguments.Length);
        return Ops.Call(callee, args);
      }
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
          runlen=0; rsi=i+1;
        }
      }
      if(runlen>1) cargs[ri++] = new CallArg(EvaluateRun(frame, runlen, rsi, Arguments.Length), runlen);
      else if(runlen==1) cargs[ri++] = new CallArg(Arguments[rsi].Expression.Evaluate(frame), null);

      if(numnamed>0) // then named arguments
      { string[] names;
        object[] values;
        EvaluateNamed(frame, numnamed, out names, out values);
        cargs[ri++] = new CallArg(names, values);
      }
      
      if(numdict>0)
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
  { if(w.Walk(this))
    { Target.Walk(w);
      for(int i=0; i<Arguments.Length; i++) if(Arguments[i].Expression!=null) Arguments[i].Expression.Walk(w);
    }
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
    for(int ai=0; ai<length; start++)
      if(Arguments[start].Type==ArgType.Normal && Arguments[start].Name==null)
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
    for(int ai=0; ai<length; start++)
      if(Arguments[start].Type==ArgType.Normal && Arguments[start].Name==null)
        values[ai++] = Arguments[start].Expression.Evaluate(frame);
    return values;
  }
}
#endregion

#region CompareExpression
public class CompareExpression : Expression
{ public CompareExpression(Expression[] exprs, BinaryOperator[] ops)
  { Expressions=exprs; Ops=ops; SetLocation(exprs[0]);
  }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { Slot tmp = cg.AllocLocalTemp(typeof(object));
      MethodInfo istrue = typeof(Ops).GetMethod("IsTrue");
      Label end = cg.ILG.DefineLabel();
      int i=0;

      Expressions[0].Emit(cg);
      Expressions[1].Emit(cg);
      cg.ILG.Emit(OpCodes.Dup);
      tmp.EmitSet(cg);
      while(true)
      { Ops[i].Emit(cg);
        if(++i==Ops.Length) break;
        cg.ILG.Emit(OpCodes.Dup);
        cg.EmitCall(istrue);
        cg.ILG.Emit(OpCodes.Brfalse, end);
        cg.ILG.Emit(OpCodes.Pop);
        tmp.EmitGet(cg);
        Expressions[i+1].Emit(cg);
        if(i<Ops.Length-1)
        { cg.ILG.Emit(OpCodes.Dup);
          tmp.EmitSet(cg);
        }
      }
      cg.ILG.MarkLabel(end);
      
      cg.FreeLocalTemp(tmp);
    }
  }

  public override object Evaluate(Frame frame)
  { object a=Expressions[0].Evaluate(frame), b=Expressions[1].Evaluate(frame);
    int i=0;
    while(true)
    { if(!Boa.Runtime.Ops.IsTrue(Ops[i].Evaluate(a, b))) return Boa.Runtime.Ops.FALSE;
      a = b;
      if(++i==Ops.Length) break;
      b = Expressions[i+1].Evaluate(frame);
    }
    return Boa.Runtime.Ops.TRUE;
  }

  public override void Optimize()
  { bool isconst = true;
    for(int i=0; i<Expressions.Length; i++) if(!Expressions[i].IsConstant) { isconst=false; break; }
    IsConstant = isconst;
  }
  
  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Expressions[0].ToCode(sb, 0);
    for(int i=0; i<Ops.Length; )
    { sb.Append(Ops[i].ToString());
      Expressions[++i].ToCode(sb, 0);
    }
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Expression e in Expressions) e.Walk(w);
    w.PostWalk(this);
  }

  public Expression[] Expressions;
  public BinaryOperator[] Ops;
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

#region GeneratorExpression
public class GeneratorExpression : ListGenExpression
{ public GeneratorExpression(Expression item, ListCompFor[] fors) : base(item, fors) { IsGenerator=true; }

  public override void Emit(CodeGenerator cg)
  { TypeGenerator tg = cg.TypeGenerator.DefineNestedType(TypeAttributes.Sealed, "ge$f"+index++, typeof(Generator));
    CodeGenerator nc = tg.DefineMethod(MethodAttributes.Virtual|MethodAttributes.Family,
                                       "InnerNext", typeof(bool), new Type[] { Misc.TypeOfObjectRef });
    Label yield = nc.ILG.DefineLabel();
    Name[] args = new Name[1] { new Name("genarg$", Scope.Local) };
    Slot argslot;

    nc.IsGenerator = true;
    nc.Namespace = new FieldNamespace(cg.Namespace, "_", nc, new ThisSlot(tg.TypeBuilder));
    nc.Namespace.SetArgs(args, 0, nc.MethodBase);
    argslot = nc.Namespace.GetSlotForSet(args[0]);

    nc.EmitPosition(this); // TODO: make sure this is done elsewhere, too
    nc.ILG.BeginExceptionBlock();

    FieldInfo jump = typeof(Generator).GetField("jump", BindingFlags.Instance|BindingFlags.NonPublic);
    nc.ILG.Emit(OpCodes.Ldarg_0);
    nc.EmitFieldGet(jump);
    nc.EmitInt(0);
    nc.ILG.Emit(OpCodes.Ceq);
    nc.ILG.Emit(OpCodes.Brtrue, yield);
    nc.ILG.Emit(OpCodes.Ldarg_0);
    nc.EmitInt(0);
    nc.EmitFieldSet(jump);

    base.YieldLabel = yield;
    EmitFors(nc, argslot);

    nc.ILG.BeginCatchBlock(typeof(StopIterationException));
    nc.ILG.EndExceptionBlock();
    nc.ILG.Emit(OpCodes.Ldc_I4_0);
    nc.ILG.Emit(OpCodes.Ret);
    nc.Finish();

    cg.EmitNew(tg.TypeBuilder.DefineDefaultConstructor(MethodAttributes.Public));
    cg.ILG.Emit(OpCodes.Dup);
    Fors[0].List.Emit(cg);
    cg.EmitCall(typeof(Ops), "GetEnumerator", new Type[] { typeof(object) });
    cg.EmitFieldSet(((FieldSlot)argslot).Info);
  }
  
  public override object Evaluate(Frame frame)
  { throw new NotImplementedException();
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('(');
    base.ToCode(sb, 0);
    sb.Append(')');
  }

  static int index;
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

#region InExpression
public class InExpression : BinaryExpression
{ public InExpression(Expression lhs, Expression rhs, bool not) { LHS=lhs; RHS=rhs; Not=not; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { LHS.Emit(cg);
      RHS.Emit(cg);
      cg.EmitBool(Not);
      cg.EmitCall(typeof(Ops), "IsIn");
    }
  }
  
  public override object Evaluate(Frame frame) { return Ops.IsIn(LHS.Evaluate(frame), RHS.Evaluate(frame), Not); }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { LHS.ToCode(sb, 0);
    sb.Append(Not ? " not in " : " in ");
    RHS.ToCode(sb, 0);
  }

  public bool Not;
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
public class ListCompExpression : ListGenExpression
{ public ListCompExpression(Expression item, ListCompFor[] fors) : base(item, fors) { }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { Slot list = cg.AllocLocalTemp(typeof(List), true);
      cg.EmitNew(typeof(List), Type.EmptyTypes);
      list.EmitSet(cg);
// FIXME: this should catch StopIteration, but try blocks empty the evaluation stack...
//      cg.ILG.BeginExceptionBlock();
      EmitFors(cg, list);
//      cg.ILG.BeginCatchBlock(typeof(StopIterationException));
//      cg.ILG.EndExceptionBlock();
      list.EmitGet(cg);
      cg.FreeLocalTemp(list);
    }
  }

  public override object Evaluate(Frame frame)
  { List list = new List();
    try { EvaluateFor(frame, list, 0); } catch(StopIterationException) { }
    return list;
  }

  public override void Optimize()
  { // TODO: implement this
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('[');
    base.ToCode(sb, 0);
    sb.Append(']');
  }

  void EvaluateFor(Frame frame, List list, int n)
  { ConstantExpression ce = new ConstantExpression(null);
    AssignStatement ass = new AssignStatement(Fors[n].Names, ce);
    IEnumerator e = Ops.GetEnumerator(Fors[n].List.Evaluate(frame));
    bool last = n==Fors.Length-1;
    while(e.MoveNext())
    { ce.Value = e.Current;
      ass.Execute(frame);
      if(Fors[n].Test==null || Ops.IsTrue(Fors[n].Test.Evaluate(frame)))
      { if(last) list.append(Item.Evaluate(frame));
        else EvaluateFor(frame, list, n+1);
      }
    }
  }
}
#endregion

#region ListGenExpression
public abstract class ListGenExpression : Expression
{ public ListGenExpression(Expression item, ListCompFor[] fors)
  { Item=item; Fors=fors;

    Name[] names = NameFinder.Find(item);
    foreach(Name itemname in names)
      foreach(ListCompFor f in fors)
      { NameExpression n = f.Names as NameExpression;
        if(n==null)
        { TupleExpression te = (TupleExpression)f.Names;
          foreach(NameExpression ne in te.Expressions)
            if(ne.Name.String==itemname.String) { itemname.Scope=Scope.Private; break; }
        }
        else if(n.Name.String==itemname.String) itemname.Scope=Scope.Private;
      }
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Item.ToCode(sb, 0);
    for(int i=0; i<Fors.Length; i++) Fors[i].ToCode(sb);
  }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { Item.Walk(w);
      foreach(ListCompFor f in Fors)
      { f.List.Walk(w);
        if(f.Test!=null) f.Test.Walk(w);
      }
    }
    w.PostWalk(this);
  }

  public Expression Item;
  public ListCompFor[] Fors;
  
  protected void EmitFor(CodeGenerator cg, Slot slot, int n)
  { AssignStatement ass = new AssignStatement(Fors[n].Names);
    Label next = cg.ILG.DefineLabel(), end = cg.ILG.DefineLabel();
    Slot e = cg.AllocLocalTemp(typeof(IEnumerator), true);

    if(!IsGenerator || n>0)
    { Fors[n].List.Emit(cg);
      cg.EmitCall(typeof(Ops), "GetEnumerator", new Type[] { typeof(object) });
    }
    else slot.EmitGet(cg);
    e.EmitSet(cg);

    cg.ILG.MarkLabel(next);
    e.EmitGet(cg);
    cg.EmitCall(typeof(IEnumerator), "MoveNext");
    cg.ILG.Emit(OpCodes.Brfalse, end);
    e.EmitGet(cg);
    cg.EmitPropGet(typeof(IEnumerator), "Current");
    ass.Emit(cg);

    if(Fors[n].Test!=null)
    { Fors[n].Test.Emit(cg);
      cg.EmitIsTrue();
      cg.ILG.Emit(OpCodes.Brfalse, next);
    }

    if(n==Fors.Length-1)
    { if(IsGenerator)
      { cg.ILG.Emit(OpCodes.Ldarg_1);
        Item.Emit(cg);
        cg.ILG.Emit(OpCodes.Stind_Ref);
        cg.ILG.Emit(OpCodes.Ldc_I4_1);
        cg.ILG.Emit(OpCodes.Ret);
        cg.ILG.MarkLabel(YieldLabel);
      }
      else
      { slot.EmitGet(cg);
        Item.Emit(cg);
        cg.EmitCall(typeof(List), "append");
      }
    }
    else EmitFor(cg, slot, n+1);

    cg.ILG.Emit(OpCodes.Br, next);
    cg.ILG.MarkLabel(end);
    cg.FreeLocalTemp(e);
  }

  protected void EmitFors(CodeGenerator cg, Slot slot) { EmitFor(cg, slot, 0); }

  protected Label YieldLabel;
  protected bool  IsGenerator;
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
{ public OrExpression(Expression lhs, Expression rhs) { LHS=lhs; RHS=rhs; SetLocation(lhs); }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else if(LHS.IsConstant && !Ops.IsTrue(LHS.GetValue())) RHS.Emit(cg);
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

  public override void Optimize()
  { IsConstant = LHS.IsConstant ? Ops.IsTrue(LHS.GetValue()) ? true : RHS.IsConstant : false;
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

#region ReprExpression
public class ReprExpression : Expression
{ public ReprExpression(Expression expr) { Expression=expr; }

  public override void Emit(CodeGenerator cg)
  { if(IsConstant) cg.EmitConstant(GetValue());
    else
    { Expression.Emit(cg);
      cg.EmitCall(typeof(Ops), "Repr");
    }
  }
  
  public override object Evaluate(Frame frame) { return Ops.Repr(Expression.Evaluate(frame)); }
  public override void Optimize() { IsConstant = Expression.IsConstant; }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { sb.Append('`');
    Expression.ToCode(sb, 0);
    sb.Append('`');
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
    { cg.EmitExpression(Start);
      cg.EmitExpression(Stop);
      cg.EmitExpression(Step);
      cg.EmitNew(typeof(Slice), new Type[] { typeof(object), typeof(object), typeof(object) });
    }
  }
  
  public override object Evaluate(Frame frame)
  { return new Slice(Start==null ? null : Start.Evaluate(frame), Stop==null ? null : Stop.Evaluate(frame),
                     Step ==null ? null : Step.Evaluate(frame));
  }

  public override void Optimize()
  { IsConstant = (Start==null || Start.IsConstant) && (Stop==null || Stop.IsConstant) &&
                 (Step==null || Step.IsConstant);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent)
  { Start.ToCode(sb, 0);
    sb.Append(':');
    if(Stop!=null) Stop.ToCode(sb, 0);
    if(Step!=null)
    { sb.Append(':');
      Step.ToCode(sb, 0);
    }
  }
  
  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { if(Start!=null) Start.Walk(w);
      if(Stop!=null) Stop.Walk(w);
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
  { IEnumerator e = Ops.PrepareTupleAssignment(value, Expressions.Length);
    for(int i=0; i<Expressions.Length; i++)
    { e.MoveNext();
      Expressions[i].Assign(e.Current, frame);
    }
  }

  public override void EmitDel(CodeGenerator cg) { foreach(Expression e in Expressions) e.EmitDel(cg); }

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

  public override void Walk(IWalker w)
  { if(w.Walk(this)) foreach(Expression e in Expressions) e.Walk(w);
    w.PostWalk(this);
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
