using System;

namespace Boa.Runtime
{

public abstract class LanguageException : Exception
{ public LanguageException(string message) : base(message) { }
}

public class AttributeErrorException : RuntimeException
{ public AttributeErrorException(string message) : base(message) { }
}

public abstract class CompileTimeException : LanguageException
{ public CompileTimeException(string message) : base(message) { }
}

public class NameErrorException : RuntimeException
{ public NameErrorException(string message) : base(message) { }
}

public abstract class RuntimeException : LanguageException
{ public RuntimeException(string message) : base(message) { }
}

public class SyntaxErrorException : CompileTimeException
{ public SyntaxErrorException(string message) : base(message) { }
}

public class TypeErrorException : RuntimeException
{ public TypeErrorException(string message) : base(message) { }
}

} // namespace Boa.Runtime
