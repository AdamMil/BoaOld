using System;

namespace Language.AST
{

public abstract class Node
{ public void SetLocation(string source, int line, int column) { Source=source; Line=line; Column=column; }
  public string Source="<unknown>";
  public int Line, Column;
}

} // namespace Language.AST
