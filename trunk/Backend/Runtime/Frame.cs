using System;
using System.Collections;
using System.Collections.Specialized;

namespace Boa.Runtime
{

public class Frame
{ public Frame(IDictionary locals, IDictionary globals) { Locals=locals; Globals=globals; }
  public Frame(Module module) { Locals=Globals=module.Names; }
  public Frame(Frame parent) : this(parent, new HybridDictionary()) { }
  public Frame(Frame parent, IDictionary locals)
  { Locals=locals;
    if(parent!=null) { Parent=parent; Globals=Parent.Globals; }
    else Globals = Locals;
  }

  public object Get(string name) // TODO: eliminate double lookup
  { if(globalNames!=null && globalNames.Contains(name)) return GetGlobal(name);
    if(Locals.Contains(name)) return Locals[name];
    return Parent==null ? GetGlobal(name) : Parent.Get(name);
  }

  public void Set(string name, object value)
  { if(globalNames!=null && globalNames.Contains(name)) Globals[name] = value;
    else Locals[name] = value;
  }

  public object GetGlobal(string name)
  { if(Globals.Contains(name)) return Globals[name]; // TODO: eliminate double lookup
    throw Ops.NameError("name '{0}' is not defined", name);
  }

  public void MarkGlobal(string name)
  { if(globalNames==null) globalNames = new HybridDictionary();
    globalNames[name] = name;
  }

  public void SetGlobal(string name, object value) { Globals[name] = value; }

  public Frame Parent;
  public IDictionary Locals, Globals;
  
  HybridDictionary globalNames;
}

public abstract class FrameCode
{ public abstract object Run(Frame frame);
}

} // namespace Boa.Runtime
