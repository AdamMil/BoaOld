using System;
using System.Collections;
using Boa.AST;

namespace Boa.Runtime
{

public struct CallArg
{ public CallArg(object value, object type) { Value=value; Type=type; }
  public object Value, Type;
  
  public static readonly object DictType="<dict>", ListType="<list>";
}

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

  protected object[] FixArgs(object[] args)
  { if(args.Length<NumRequired) throw Ops.TooFewArgs(FuncName, NumRequired, args.Length);
    if(HasList)
    { if(!HasDict && ParamNames.Length==1) return new object[] { new Tuple(args) };
    }
    else if(args.Length==ParamNames.Length) return args;
    else if(args.Length>ParamNames.Length) throw Ops.TooManyArgs(FuncName, ParamNames.Length, args.Length);

    object[] newargs = new object[ParamNames.Length];
    int plen=ParamNames.Length, offset=HasList ? 1 : 0;
    if(HasDict) newargs[--plen] = new Dict();
    int pos=plen-offset, ai=Math.Min(pos, args.Length);

    Array.Copy(args, 0, newargs, 0, ai);
    for(; ai<pos; ai++) newargs[ai] = Defaults[ai-NumRequired];

    if(HasList)
    { Tuple tup;
      if(ai>=args.Length) tup = new Tuple();
      else
      { object[] items = new object[args.Length-ai];
        for(int i=0; i<items.Length; i++) items[i] = args[ai+i];
        tup = new Tuple(items);
      }
      newargs[plen-1] = tup;
    }

    return newargs;
  }

  protected unsafe object[] MakeArgs(object[] positional, string[] names, object[] values)
  { object[] newargs = new object[ParamNames.Length];
    bool* done = stackalloc bool[newargs.Length];
    Dict  dict = HasDict ? new Dict() : null;
    int pi=0, js=0, plen=newargs.Length;

    for(int i=0; i<newargs.Length; i++) done[i] = false;

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

    if(HasDict) newargs[--plen] = dict;
    if(HasList)
    { Tuple tup;
      if(pi==0) tup = new Tuple(positional==null ? Misc.EmptyArray : positional);
      else
      { object[] items = new object[positional.Length-pi];
        Array.Copy(positional, pi, items, 0, items.Length);
        tup = new Tuple(items);
      }
      newargs[--plen] = tup;
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

public sealed class CompiledFunctionN : CompiledFunction
{ public CompiledFunctionN(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                           ClosedVar[] closed, CallTargetN target)
    : base(name, names, defaults, list, dict, required, closed) { Target=target; }

  public override object Call(params object[] args) { return Target(FixArgs(args)); }

  public override object Call(object[] positional, string[] names, object[] values)
  { return Target(MakeArgs(positional, names, values));
  }

  CallTargetN Target;
}

public sealed class CompiledFunctionFN : CompiledFunction
{ public CompiledFunctionFN(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                            ClosedVar[] closed, CallTargetFN target)
    : base(name, names, defaults, list, dict, required, closed) { Target=target; }

  public override object Call(params object[] args) { return Target(this, FixArgs(args)); }

  public override object Call(object[] positional, string[] names, object[] values)
  { return Target(this, MakeArgs(positional, names, values));
  }

  CallTargetFN Target;
}
#endregion

public sealed class InterpretedFunction : Function
{ public InterpretedFunction(string name, string[] names, object[] defaults, bool list, bool dict, int required,
                             Name[] globals, Frame frame, Statement body)
    : base(name, names, defaults, list, dict, required) { Globals=globals; Frame=frame; Body=body; }

  public override object Call(params object[] args) { return DoCall(FixArgs(args)); }
  public override object Call(object[] positional, string[] names, object[] values)
  { return DoCall(MakeArgs(positional, names, values));
  }

  public Name[] Globals;
  public Frame Frame;
  public Statement Body;

  object DoCall(object[] args)
  { Frame localFrame = new Frame(Frame);
    for(int i=0; i<args.Length; i++) localFrame.Set(ParamNames[i], args[i]);
    if(Globals!=null) for(int i=0; i<Globals.Length; i++) localFrame.MarkGlobal(Globals[i].String);
    try { Body.Execute(localFrame); }
    catch(ReturnException e) { return e.Value; }
    return null;
  }
}

} // namespace Boa.Runtime
