using System;

namespace Boa.Runtime
{

// TODO: don't duplicate exceptions that already exist (NotImplemented, IOError, etc...)

public class ArithmeticErrorException : RuntimeException
{ public ArithmeticErrorException(string message) : base(message) { }
}

public class AssertionErrorException : RuntimeException
{ public AssertionErrorException(string message) : base(message) { }
}

public class AttributeErrorException : RuntimeException
{ public AttributeErrorException(string message) : base(message) { }
}

public abstract class CompileTimeException : StandardErrorException
{ public CompileTimeException(string message) : base(message) { }
}

public class EnvironmentErrorException : RuntimeException
{ public EnvironmentErrorException(string message) : base(message) { }
}

public class FloatingPointErrorException : RuntimeException
{ public FloatingPointErrorException(string message) : base(message) { }
}

public class ImportErrorException : RuntimeException
{ public ImportErrorException(string message) : base(message) { }
}

public class IndexErrorException : LookupErrorException
{ public IndexErrorException(string message) : base(message) { }
}

public class KeyErrorException : LookupErrorException
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

public class RuntimeException : StandardErrorException
{ public RuntimeException() { }
  public RuntimeException(string message) : base(message) { }
}

public class StandardErrorException : SystemException
{ public StandardErrorException() { }
  public StandardErrorException(string message) : base(message) { }
}

public class StopIterationException : ApplicationException { }

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
