using System;
using System.Collections;

namespace Boa.Runtime
{

public class Module
{ public Module() { Names = new Hashtable(); }

  public object Get(string name)
  { if(Names.Contains(name)) return Names[name]; // TODO: eliminate double lookup
    throw Ops.AttributeError("no such name '{0}'", name);
  }

  public IDictionary Names;
}

} // namespace Boa.Runtime
