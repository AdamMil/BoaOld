/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

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

  public string Name, __doc__;
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

    for(int i=0; i<ai; i++) newargs[i] = args[i];
    for(; ai<pos; ai++) newargs[ai] = Defaults[ai-NumRequired];

    if(HasList)
    { Tuple tup;
      if(ai>=args.Length) tup = new Tuple();
      else
      { object[] items = new object[args.Length-ai];
        Array.Copy(args, ai, items, 0, items.Length);
        tup = new Tuple(items);
      }
      newargs[plen-1] = tup;
    }

    return newargs;
  }

  protected unsafe object[] MakeArgs(object[] positional, string[] names, object[] values)
  { object[] newargs = new object[ParamNames.Length];
    bool* done = stackalloc bool[newargs.Length];
    Dict  dict = null;
    int pi=0, js=0, plen=newargs.Length;

    for(int i=0; i<newargs.Length; i++) done[i] = false;

    // do the positional arguments first
    if(positional!=null)
      for(int end=Math.Min(plen-(HasDict?1:0)-(HasList?1:0), positional.Length); pi<end; pi++)
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
      if(HasDict)
      { if(dict==null) dict = new Dict();
        dict[name] = values[i];
      }
      else throw Ops.TypeError("'{0}()' got an unexpected keyword parameter '{1}'", FuncName, name);
      next:;
    }

    if(HasDict)
    { if(done[--plen])
      { if(dict!=null)
          throw Ops.TypeError("'{0}()' got duplicate values for parameter '{1}'", FuncName, names[plen-1]);
      }
      else newargs[plen] = (dict==null ? new Dict() : dict);
    }
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
    for(; pi<plen; pi++) newargs[pi] = Defaults[pi-NumRequired];
    for(pi=0; pi<NumRequired; pi++)
      if(!done[pi]) throw Ops.TypeError("No value given for parameter '{0}'", ParamNames[pi]);

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
                             Name[] globals, Frame frame, Statement body, string docstring)
    : base(name, names, defaults, list, dict, required)
  { Globals=globals; Frame=frame; Body=body; __doc__=docstring;
  }

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
