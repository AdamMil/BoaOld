/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;

namespace Boa.Runtime
{

// TODO: don't duplicate exceptions that already exist (NotImplemented, IOError, etc...)
// TODO: implement python-like exception handling (with the throw type,value / except type,value form)
// TODO: make exceptions accept the same constructor arguments as python's

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
