using System;
using System.Collections;
using Boa.AST;

// TODO: implement these rules
// if a function doesn't assign to a variable, the variable is marked as 'free'
// a free variable will raise an error if read before it is assigned to (maybe).
// a free variable, when read, will look through parent scopes, including global ones
// find out how this can be compiled...

namespace Boa.Runtime
{

public delegate object CallTarget(params object[] args);

public abstract class Function : ICallable
{ public Function(string name, Parameter[] parms) { Name=name; Parameters=parms; }

  public abstract object Call(params object[] parms);
  
  public string Name;
  public Parameter[] Parameters;
}

public class CompiledFunction : Function
{ public CompiledFunction(string name, Parameter[] parms, CallTarget target)
    : base(name, parms) { Target=target; }

  public override object Call(params object[] args) { return Target(args); }

  public CallTarget Target;
}

public class InterpretedFunction : Function
{ public InterpretedFunction(Frame frame, string name, Parameter[] parms, Statement body)
    : base(name, parms) { Frame=frame; Body=body; }

  public override object Call(params object[] parms)
  { Frame localFrame = new Frame(Frame);
    for(int i=0; i<Parameters.Length; i++) localFrame.Set(Parameters[i].Name.String, parms[i]);
    return Body.Execute(localFrame);
  }

  public Frame Frame;
  public Statement Body;
}

} // namespace Boa.Runtime
