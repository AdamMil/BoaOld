using System;

namespace Boa.Runtime
{

// TODO: don't duplicate exceptions that already exist (NotImplemented, IOError, etc...)

public abstract class BoaException : Exception
{ public BoaException() { }
  public BoaException(string message) : base(message) { }

  public void SetPosition(Boa.AST.Node node) { SourceFile=node.Source; Line=node.Line; Column=node.Column; }
  public void SetPosition(string source, int line, int column) { SourceFile=source; Line=line; Column=column; }

  public override string Message
  { get { return string.Format("{0}({1},{2}): {3}", SourceFile, Line, Column, base.Message); }
  }

  public string SourceFile;
  public int Line, Column;
}

public class AssertionErrorException : RuntimeException
{ public AssertionErrorException(string message) : base(message) { }
}

public class AttributeErrorException : RuntimeException
{ public AttributeErrorException(string message) : base(message) { }
}

public abstract class CompileTimeException : BoaException
{ public CompileTimeException(string message) : base(message) { }
}

public class EOFErrorException : IOErrorException
{ public EOFErrorException(string message) : base(message) { }
}

public class ImportErrorException : RuntimeException
{ public ImportErrorException(string message) : base(message) { }
}

public class IndexErrorException : RuntimeException
{ public IndexErrorException(string message) : base(message) { }
}

public class IOErrorException : RuntimeException
{ public IOErrorException(string message) : base(message) { }
}

public class KeyErrorException : RuntimeException
{ public KeyErrorException(string message) : base(message) { }
}

public class LookupErrorException : RuntimeException
{ public LookupErrorException(string message) : base(message) { }
}

public class NameErrorException : RuntimeException
{ public NameErrorException(string message) : base(message) { }
}

public class OSErrorException : RuntimeException
{ public OSErrorException(string message) : base(message) { }
}

public class RuntimeException : BoaException
{ public RuntimeException() { }
  public RuntimeException(string message) : base(message) { }
}

public class StopIterationException : RuntimeException { }

public class SyntaxErrorException : CompileTimeException
{ public SyntaxErrorException(string message) : base(message) { }
}

public class SystemExitException : RuntimeException
{ public SystemExitException(object exitarg) { ExitArg=exitarg; }

  public object ExitArg;
}

public class TypeErrorException : RuntimeException
{ public TypeErrorException(string message) : base(message) { }
}

public class ValueErrorException : RuntimeException
{ public ValueErrorException(string message) : base(message) { }
}

} // namespace Boa.Runtime
