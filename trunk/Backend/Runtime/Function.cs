using System;
using System.Collections;
using Boa.AST;

namespace Boa.Runtime
{

public abstract class Function : ICallable
{ public Function(string name, Parameter[] parms) { Name=name; Parameters=parms; }

  public abstract object Call(params object[] args);

  public override string ToString() { return Name==null ? "<lambda>" : string.Format("<function '{0}'>", Name); }

  public string Name;
  public Parameter[] Parameters;
}

#region Compiled functions
public delegate object CallTargetN(params object[] args);
public delegate object CallTargetFN(CompiledFunction func, params object[] args);

public abstract class CompiledFunction : Function
{ public CompiledFunction(string name, Parameter[] parms, ClosedVar[] closed) : base(name, parms) { Closed=closed; }
  public ClosedVar[] Closed;
}

public class CompiledFunctionN : CompiledFunction
{ public CompiledFunctionN(string name, Parameter[] parms, ClosedVar[] closed, CallTargetN target)
    : base(name, parms, closed) { Target=target; }

  public override object Call(params object[] args) { return Target(args); }
  CallTargetN Target;
}

public class CompiledFunctionFN : CompiledFunction
{ public CompiledFunctionFN(string name, Parameter[] parms, ClosedVar[] closed, CallTargetFN target)
    : base(name, parms, closed) { Target=target; }

  public override object Call(params object[] args) { return Target(this, args); }
  CallTargetFN Target;
}
#endregion

public class InterpretedFunction : Function
{ public InterpretedFunction(Frame frame, string name, Parameter[] parms, Name[] globals, Statement body)
    : base(name, parms) { Frame=frame; Body=body; }

  public override object Call(params object[] args)
  { Frame localFrame = new Frame(Frame);
    for(int i=0; i<Parameters.Length; i++) localFrame.Set(Parameters[i].Name.String, args[i]);
    if(Globals!=null) for(int i=0; i<Globals.Length; i++) localFrame.MarkGlobal(Globals[i].String);
    try { Body.Execute(localFrame); }
    catch(ReturnException e) { return e.Value; }
    return null;
  }

  public Name[] Globals;
  public Frame Frame;
  public Statement Body;
}

} // namespace Boa.Runtime
