using System;

namespace Boa.Runtime
{

public abstract class BoaException : Exception
{ public BoaException() { }
  public BoaException(string message) : base(message) { }
}

public class AttributeErrorException : RuntimeException
{ public AttributeErrorException(string message) : base(message) { }
}

public abstract class CompileTimeException : BoaException
{ public CompileTimeException(string message) : base(message) { }
}

public class IndexErrorException : RuntimeException
{ public IndexErrorException(string message) : base(message) { }
}

public class KeyErrorException : RuntimeException
{ public KeyErrorException(string message) : base(message) { }
}

public class NameErrorException : RuntimeException
{ public NameErrorException(string message) : base(message) { }
}

public abstract class RuntimeException : BoaException
{ public RuntimeException() { }
  public RuntimeException(string message) : base(message) { }
}

public class StopIterationException : RuntimeException { }

public class SyntaxErrorException : CompileTimeException
{ public SyntaxErrorException(string message) : base(message) { }
}

public class TypeErrorException : RuntimeException
{ public TypeErrorException(string message) : base(message) { }
}

public class ValueErrorException : RuntimeException
{ public ValueErrorException(string message) : base(message) { }
}

} // namespace Boa.Runtime
