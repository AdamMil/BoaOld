using System;

namespace Boa.Runtime
{

public class ClosedVar
{ public ClosedVar(string name) { Name=name; }
  public string Name;
  public object Value;
}

} // namespace Boa.Runtime