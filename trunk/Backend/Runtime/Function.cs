using System;
using System.Collections;
using Boa.AST;

namespace Boa.Runtime
{

public abstract class Function : IFancyCallable
{ public Function(string name, string[] names, object[] defaults, bool list, bool dict, int required)
  { Name=name; ParamNames=names; Defaults=defaults; HasList=list; HasDict=dict; NumRequired=required;
  }

  public string FuncName { get { return Name==null ? "<lambda>" : Name; } }

  public abstract object Call(params object[] args);
  public abstract object Call(object[] args, string[] names, object[] values);

  public override string ToString() { return Name==null ? "<lambda>" : string.Format("<function '{0}'>", Name); }

  public string Name;
  public string[] ParamNames;
  public object[] Defaults;
  public int NumRequired;
  public bool HasList, HasDict;
  
  protected unsafe object[] MakeArgs(object[] positional, string[] names, object[] values)
  { object[] newargs = new object[ParamNames.Length];
    bool* done = stackalloc bool[newargs.Length];
    List  list = HasList ? new List() : null;
    Dict  dict = HasDict ? new Dict() : null;
    int pi=0, js=0, plen=newargs.Length;

    // do the positional arguments first
    if(positional!=null)
      for(int end=Math.Min(NumRequired, positional.Length); pi<end; pi++)
      { newargs[pi] = positional[pi];
        done[pi] = true;
      }

    // do named arguments
    for(int i=0; i<names.Length; i++)
    { string name = names[i];
      for(int j=js; j<plen; j++)
        if(ParamNames[j]==names[i])
        { if(done[j]) throw Ops.TypeError("'{0}()' got duplicate values for parameter '{1}'", FuncName, name);
          newargs[j] = values[i];
          done[j] = true;
          if(j==js)
          { do j++; while(j<plen && done[j]);
            if(j<plen) js=j;
          }
          goto next;
        }
      if(dict!=null) dict[name] = values[i];
      else throw Ops.TypeError("'{0}()' got an unexpected keyword parameter '{1}'", FuncName, name);
      next:;
    }

    if(HasDict) plen--;
    if(HasList)
    { for(; pi<positional.Length; pi++) list.append(positional[pi]);
      plen--;
    }

    for(pi=0; pi<NumRequired; pi++)
      if(!done[pi]) throw Ops.TypeError("No value given for parameter '{0}'", ParamNames[pi]);
    for(; pi<plen; pi++) if(!done[pi]) newargs[pi] = Defaults[pi-NumRequired];

    return newargs;
  }
}

#region Compiled functions
public delegate object CallTargetN(params object[] args);
public delegate object CallTargetFN(CompiledFunction func, params object[] args);

public abstract class CompiledFunction : Function
{ public CompiledFunction(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                          ClosedVar[] closed)
    : base(name, names, defaults, list, dict, required) { Closed=closed; }

  public ClosedVar[] Closed;
}

public class CompiledFunctionN : CompiledFunction
{ public CompiledFunctionN(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                           ClosedVar[] closed, CallTargetN target)
    : base(name, names, defaults, list, dict, required, closed) { Target=target; }

  public override object Call(params object[] args)
  { if(args.Length<NumRequired) Ops.TooFewArgs(FuncName, NumRequired, args.Length);
    return Target(args);
  }

  public override object Call(object[] positional, string[] names, object[] values)
  { return Target(MakeArgs(positional, names, values));
  }

  CallTargetN Target;
}

public class CompiledFunctionFN : CompiledFunction
{ public CompiledFunctionFN(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                            ClosedVar[] closed, CallTargetFN target)
    : base(name, names, defaults, list, dict, required, closed) { Target=target; }

  public override object Call(params object[] args)
  { if(args.Length<NumRequired) Ops.TooFewArgs(FuncName, NumRequired, args.Length);
    return Target(this, args);
  }

  public override object Call(object[] positional, string[] names, object[] values)
  { return Target(this, MakeArgs(positional, names, values));
  }

  CallTargetFN Target;
}
#endregion

public class InterpretedFunction : Function
{ public InterpretedFunction(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                             Name[] globals, Frame frame, Statement body)
    : base(name, names, defaults, list, dict, required) { Globals=globals; Frame=frame; Body=body; }

  public override object Call(params object[] args)
  { if(args.Length<NumRequired) Ops.TooFewArgs(FuncName, NumRequired, args.Length);
    Frame localFrame = new Frame(Frame);
    for(int i=0; i<Parameters.Length; i++) localFrame.Set(names[i], args[i]);
    if(Globals!=null) for(int i=0; i<Globals.Length; i++) localFrame.MarkGlobal(Globals[i].String);
    try { Body.Execute(localFrame); }
    catch(ReturnException e) { return e.Value; }
    return null;
  }

  public override object Call(object[] positional, string[] names, object[] values)
  { return Call(MakeArgs(positional, names, values));
  }

  public Name[] Globals;
  public Frame Frame;
  public Statement Body;
}

} // namespace Boa.Runtime
