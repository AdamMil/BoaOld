using System;
using System.Collections;
using Language.AST;

// TODO: implement these rules
// if a function doesn't assign to a variable, the variable is marked as 'free'
// a free variable will raise an error if read before it is assigned to (maybe).
// a free variable, when read, will look through parent scopes, including global ones
// find out how this can be compiled...

namespace Language.Runtime
{

public delegate object CallTarget(params object[] args);

public abstract class Function : ICallable
{ public Function(string name, string[] paramNames)
  { Name=name; Parameters=paramNames;
  }

  public abstract object Call(params object[] parms);
  
  public string Name;
  public string[] Parameters;
}

public class CompiledFunction : Function
{ public CompiledFunction(string name, string[] paramNames, CallTarget target)
    : base(name, paramNames) { Target=target; }

  public override object Call(params object[] args) { return Target(args); }

  public CallTarget Target;
}

public class InterpretedFunction : Function
{ public InterpretedFunction(Frame frame, string name, string[] paramNames, Statement body)
    : base(name, paramNames) { Frame=frame; Body=body; }

  public override object Call(params object[] parms)
  { Frame localFrame = new Frame(Frame);
    for(int i=0; i<Parameters.Length; i++) localFrame.Set(Parameters[i], parms[i]);
    return Body.Execute(localFrame);
  }

  public Frame Frame;
  public Statement Body;
}

} // namespace Language.Runtime