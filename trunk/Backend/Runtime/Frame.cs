using System;
using System.Collections;

namespace Language.Runtime
{

public class Frame
{ public Frame(Module module) { Locals=Globals=module.Names; }
  public Frame(Frame parent) : this(parent, new Hashtable()) { }
  public Frame(Frame parent, IDictionary locals)
  { Locals=locals;
    if(parent!=null) { Parent=parent; Globals=Parent.Globals; }
    else Globals = Locals;
  }

  public object Get(string name) // TODO: eliminate double lookup
  { if(Locals.Contains(name)) return Locals[name];
    if(Parent!=null) return Parent.Get(name);
    throw Ops.NameError("name '{0}' is not defined", name);
  }
  public void Set(string name, object value) { Locals[name] = value; }

  public object GetGlobal(string name)
  { if(Globals.Contains(name)) return Globals[name]; // TODO: eliminate double lookup
    throw Ops.NameError("name '{0}' is not defined", name);
  }
  public void SetGlobal(string name, object value) { Globals[name] = value; }

  /*public object GetLocal(string name)
  { if(Locals.Contains(name)) return Locals[name]; // TODO: eliminate double lookup
    throw Ops.NameError("name '{0}' is not defined", name);
  }
  public void SetLocal(string name, object value) { Locals[name] = value; }*/

  public Frame Parent;
  public IDictionary Locals, Globals;
}

public abstract class FrameCode
{ public abstract object Run(Frame frame);
}

} // namespace Language.Runtime