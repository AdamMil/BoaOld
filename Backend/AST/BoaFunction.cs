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

public sealed class BoaFunction : Node
{ public BoaFunction(Node from, Parameter[] parms, Statement body)
  { Parameters=parms; Body=body; SetLocation(from.Source, from.Line, from.Column);
    Yields = YieldFinder.Find(body);
    if(Yields!=null) body.Walk(new ReturnMarker());
    docstring = Misc.BodyToDocString(Body);
  }
  public BoaFunction(Node from, string name, Parameter[] parms, Statement body) : this(from, parms, body)
  { Name=new Name(name);
  }

  public Name   Name;
  public Name[] Inherit, Globals;
  public Parameter[] Parameters;
  public YieldStatement[] Yields;
  public Statement Body;

  public string FuncName { get { return Name==null ? "lambda" : Name.String; } }

  public void Emit(CodeGenerator cg)
  { Initialize();
    index = Misc.NextIndex;
    CodeGenerator impl = MakeImplMethod(cg);

    cg.EmitString(Name==null ? null : Name.String);
    EmitNames(cg);
    EmitDefaults(cg);
    cg.EmitBool(hasList);
    cg.EmitBool(hasDict);
    cg.EmitInt(numRequired);
    if(Inherit==null)
    { cg.ILG.Emit(OpCodes.Ldnull); // create delegate
      cg.ILG.Emit(OpCodes.Ldftn, (MethodInfo)impl.MethodBase);
      cg.EmitNew((ConstructorInfo)typeof(CallTargetN).GetMember(".ctor")[0]);
      cg.EmitNew(typeof(CompiledFunctionN),
                 new Type[] { typeof(string), typeof(string[]), typeof(object[]), typeof(bool), typeof(bool),
                              typeof(int), typeof(CallTargetN) });
    }
    else
    { Type type = impl.TypeGenerator.TypeBuilder;
      cg.EmitNew(type, new Type[] { typeof(string), typeof(string[]), typeof(object[]), typeof(bool), typeof(bool),
                                    typeof(int) });
      for(int i=0; i<Inherit.Length; i++)
      { cg.ILG.Emit(OpCodes.Dup);
        cg.Namespace.GetSlotForGet(Inherit[i]).EmitGet(cg);
        cg.EmitFieldSet(((FieldSlot)impl.Namespace.GetLocalSlot(Inherit[i])).Info);
      }
    }

    if(docstring!=null)
    { cg.ILG.Emit(OpCodes.Dup);
      cg.EmitString(docstring);
      cg.EmitFieldSet(typeof(Function), "__doc__");
    }
  }

  public Function MakeFunction(Frame frame)
  { Initialize();
    object[] defaults = numOptional==0 ? null : new object[numOptional]; // TODO: optimize this if expressions are constant?
    for(int i=0; i<numOptional; i++) defaults[i] = Parameters[i+optionalStart].Default.Evaluate(frame);
    return new InterpretedFunction(Name==null ? null : Name.String, names, defaults, hasList, hasDict, numRequired,
                                   Globals, frame, Body, docstring);
  }
  
  public override void Optimize()
  { Optimizer o = new Optimizer();
    for(int i=0; i<Parameters.Length; i++) if(Parameters[i].Default!=null) Parameters[i].Default.Walk(o);
  }

  public override void ToCode(System.Text.StringBuilder sb, int indent) { throw new NotSupportedException(); }

  public override void Walk(IWalker w)
  { if(w.Walk(this))
    { for(int i=0; i<Parameters.Length; i++) if(Parameters[i].Default!=null) Parameters[i].Default.Walk(w);
      Body.Walk(w);
    }
    w.PostWalk(this);
  }

  #region Walkers
  class LabelMaker : IWalker
  { public LabelMaker(CodeGenerator cg) { this.cg=cg; }

    public void PostWalk(Node n) { }

    public bool Walk(Node n)
    { if(n is TryStatement)
      { TryStatement ts = (TryStatement)n;
        foreach(YieldStatement ys in ts.Yields) DefineLabels(ys);
        return false;
      }
      else if(n is YieldStatement) { DefineLabels((YieldStatement)n); return false; }
      return true;
    }

    void DefineLabels(YieldStatement ys)
    { for(int i=0; i<ys.Targets.Length; i++) ys.Targets[i].Label = cg.ILG.DefineLabel();
    }

    CodeGenerator cg;
  }

  class ReturnMarker : IWalker
  { public void PostWalk(Node n) { }

    public bool Walk(Node n)
    { if(n is BoaFunction) return false;
      else if(n is ReturnStatement)
      { ReturnStatement rs = (ReturnStatement)n;
        if(rs.Expression!=null) throw Ops.SyntaxError(n, "'return expression' not allowed in a generator function");
        rs.InGenerator = true;
        return false;
      }
      return true;
    }
  }

  class YieldFinder : IWalker
  { public static YieldStatement[] Find(Node n)
    { YieldFinder yf = new YieldFinder();
      n.Walk(yf);
      return yf.yields.Count==0 ? null : (YieldStatement[])yf.yields.ToArray(typeof(YieldStatement));
    }

    public void PostWalk(Node n)
    { if(n is TryStatement)
      { tries.RemoveAt(tries.Count-1);
        int start = (int)locals.Pop();
        if(localYields.Count==start) return;
        else
        { TryStatement ts = (TryStatement)n;
          if(ts.Finally!=null)
            throw Ops.SyntaxError(ts.Finally, "'finally' clause not allowed on a try block that contains a 'yield'");
          YieldStatement[] ys = ((TryStatement)n).Yields = new YieldStatement[localYields.Count-start];
          for(int i=0; i<ys.Length; i++) ys[i] = (YieldStatement)localYields[i+start];
        }
      }
    }

    public bool Walk(Node n)
    { if(n is BoaFunction) return false;
      else if(n is TryStatement)
      { TryStatement ts = (TryStatement)n;
        bool invalid=false;
        if(ts.Else!=null && HasYield.Check(ts.Else) || ts.Finally!=null && HasYield.Check(ts.Finally)) invalid=true;
        else foreach(ExceptClause ec in ts.Except) if(HasYield.Check(ec.Body)) { invalid=true; break; }
        if(invalid) throw Ops.SyntaxError(ts, "'yield' not allowed in the 'else', 'except', or 'finally' "+
                                              "section of a try block");
        tries.Add(n);
        locals.Push(localYields.Count);
      }
      else if(n is YieldStatement)
      { YieldStatement ys = (YieldStatement)n;
        ys.YieldNumber = yields.Count;
        ys.Targets = new YieldStatement.YieldTarget[tries.Count+1];
        for(int i=0; i<tries.Count; i++) ys.Targets[i].Statement = (TryStatement)tries[i];
        yields.Add(n); localYields.Add(n);
        return false;
      }
      return true;
    }

    class HasYield : IWalker
    { public static bool Check(Node n)
      { HasYield w = new HasYield();
        n.Walk(w);
        return w.found;
      }

      public void PostWalk(Node n) { }
      public bool Walk(Node n)
      { if(n is TryStatement || n is BoaFunction) return false;
        if(n is YieldStatement) found=true;
        return !found;
      }

      bool found;
    }

    ArrayList tries=new ArrayList(), yields=new ArrayList(), localYields=new ArrayList();
    Stack locals = new Stack();
  }
  #endregion

  long index;

  void EmitBody(CodeGenerator cg, Name[] parmNames)
  { bool interactive = Options.Interactive;
    Options.Interactive = false;
    try
    { if(Yields==null)
      { Body.Emit(cg);
        cg.EmitReturn(null);
      }
      else
      { TypeGenerator tg = cg.TypeGenerator.DefineNestedType(TypeAttributes.Sealed, FuncName+index,
                                                             typeof(Generator));
        CodeGenerator ncg = tg.DefineMethodOverride(typeof(Generator), "InnerNext");
        ncg.IsGenerator = true;
        ncg.Namespace   = new FieldNamespace(cg.Namespace, "_", ncg, new ThisSlot(tg.TypeBuilder));
        ncg.Namespace.SetArgs(parmNames, 0, ncg.MethodBase);
        Body.Walk(new LabelMaker(ncg));
        ncg.ILG.BeginExceptionBlock();

        Label[] jumps = new Label[Yields.Length];
        for(int i=0; i<Yields.Length; i++) jumps[i] = Yields[i].Targets[0].Label;

        ncg.ILG.Emit(OpCodes.Ldarg_0);
        ncg.EmitFieldGet(typeof(Generator).GetField("jump", BindingFlags.Instance|BindingFlags.NonPublic));
        ncg.ILG.Emit(OpCodes.Switch, jumps);
        Body.Emit(ncg);
        ncg.ILG.BeginCatchBlock(typeof(StopIterationException));
        ncg.ILG.Emit(OpCodes.Pop);
        ncg.ILG.EndExceptionBlock();
        ncg.ILG.Emit(OpCodes.Ldc_I4_0);
        ncg.EmitReturn();
        ncg.Finish();

        cg.EmitNew(tg.TypeBuilder.DefineDefaultConstructor(MethodAttributes.Public));
        for(int i=0; i<parmNames.Length; i++)
        { cg.ILG.Emit(OpCodes.Dup);
          cg.EmitGet(parmNames[i]);
          cg.EmitFieldSet(((FieldSlot)ncg.Namespace.GetSlotForSet(parmNames[i])).Info);
        }
        cg.EmitReturn();
        cg.Finish();
      }
    }
    finally { Options.Interactive = interactive; }
  }

  void EmitDefaults(CodeGenerator cg)
  { if(numOptional==0) { cg.ILG.Emit(OpCodes.Ldnull); return; }
    if(defaultSlot!=null) { defaultSlot.EmitGet(cg); return; }

    bool constant = true;
    for(int i=0; i<numOptional; i++) if(!Parameters[i+optionalStart].Default.IsConstant) { constant=false; break; }
  
    if(constant)
    { defaultSlot = cg.TypeGenerator.DefineStaticField(FuncName + "$d" + index, typeof(object[]));
      CodeGenerator icg = cg.TypeGenerator.GetInitializer();
      icg.EmitNewArray(typeof(object), numOptional);
      for(int i=0; i<numOptional; i++)
      { icg.ILG.Emit(OpCodes.Dup);
        icg.EmitInt(i);
        icg.EmitConstant(Parameters[i+optionalStart].Default.GetValue());
        icg.ILG.Emit(OpCodes.Stelem_Ref);
      }
      defaultSlot.EmitSet(icg);
      defaultSlot.EmitGet(cg);
      return;
    }
    
    cg.EmitNewArray(typeof(object), numOptional);
    for(int i=0; i<numOptional; i++)
    { cg.ILG.Emit(OpCodes.Dup);
      cg.EmitInt(i);
      Parameters[i+optionalStart].Default.Emit(cg);
      cg.ILG.Emit(OpCodes.Stelem_Ref);
    }
  }

  void EmitNames(CodeGenerator cg)
  { if(namesSlot==null)
    { namesSlot = cg.TypeGenerator.DefineStaticField(FuncName + "$n" + index, typeof(string[]));
      CodeGenerator icg = cg.TypeGenerator.GetInitializer();
      icg.EmitStringArray(names);
      namesSlot.EmitSet(icg);
    }
    namesSlot.EmitGet(cg);
  }

  void Initialize()
  { if(names!=null) return;
    bool os = false;
    names = new string[Parameters.Length];

    for(int i=0; i<Parameters.Length; i++)
    { names[i] = Parameters[i].Name.String;
      switch(Parameters[i].Type)
      { case ParamType.Required: numRequired++; break;
        case ParamType.Optional:
          if(os) numOptional++;
          else { optionalStart=i; numOptional=1; os=true; }
          break;
        case ParamType.List: hasList=true; break;
        case ParamType.Dict: hasDict=true; break;
      }
    }
  }

  CodeGenerator MakeImplMethod(CodeGenerator cg)
  { Name[] names = new Name[Parameters.Length]; 
    for(int i=0; i<Parameters.Length; i++) names[i] = Parameters[i].Name;

    CodeGenerator icg;
    Slot[] closedSlots=null;
    if(Inherit==null || Inherit.Length==0)
      icg = cg.TypeGenerator.DefineMethod(FuncName + "$f" + index, typeof(object), new Type[] { typeof(object[]) });
    else
    { closedSlots = new Slot[Inherit.Length];
      TypeGenerator tg = SnippetMaker.Assembly.DefineType(TypeAttributes.Public|TypeAttributes.Sealed,
                                                          FuncName+"$cf"+index, typeof(CompiledFunction));
      for(int i=0; i<Inherit.Length; i++) closedSlots[i] = tg.DefineField(Inherit[i].String+"$cv", typeof(object));

      // make the constructor and the MakeMarked function
      { CodeGenerator ccg = tg.DefineChainedConstructor(typeof(CompiledFunction).GetConstructors()[0]);
        ccg.EmitReturn();
        ccg.Finish();

        icg = tg.DefineMethodOverride(typeof(Function).GetMethod("MakeMarked"), true);
        icg.EmitThis();
        icg.EmitFieldGet(typeof(Function), "Name");
        icg.EmitThis();
        icg.EmitFieldGet(typeof(Function), "ParamNames");
        icg.EmitThis();
        icg.EmitFieldGet(typeof(Function), "Defaults");
        icg.EmitThis();
        icg.EmitFieldGet(typeof(Function), "HasList");
        icg.EmitThis();
        icg.EmitFieldGet(typeof(Function), "HasDict");
        icg.EmitThis();
        icg.EmitFieldGet(typeof(Function), "NumRequired");
        icg.EmitNew((ConstructorInfo)ccg.MethodBase);
        icg.ILG.Emit(OpCodes.Dup);
        icg.EmitArgGet(0);
        icg.EmitFieldSet(typeof(Function), "Type");
        icg.EmitReturn();
        icg.Finish();
      }

      icg = tg.DefineMethodOverride(typeof(CompiledFunction), "DoCall", true);
    }

    LocalNamespace ns = new LocalNamespace(cg.Namespace, icg);
    icg.Namespace = ns;
    ns.SetArgs(names, icg, new ArgSlot((MethodBuilder)icg.MethodBase, 0, "$names", typeof(object[])));
    if(Inherit!=null) ns.AddClosedVars(Inherit, closedSlots);
    EmitBody(icg, names);
    icg.Finish();
    if(Inherit!=null) icg.TypeGenerator.FinishType();
    return icg;
  }

  Slot namesSlot, defaultSlot;
  string[] names;
  string docstring;
  int optionalStart, numOptional, numRequired;
  bool hasList, hasDict;
}

} // namespace Boa.AST
