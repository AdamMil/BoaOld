using System;
using System.CodeDom.Compiler;

namespace AdamMil.Boa
{

internal class CompilerErrorException : ApplicationException
{ public CompilerErrorException(string file, int line, int column, string message)
  { Error = new CompilerError(file, line, column, "", message);
  }
  public CompilerErrorException(string file, int line, int column, string number, string message)
  { Error = new CompilerError(file, line, column, number, message);
  }
  public CompilerErrorException(CompilerError error) { Error=error; }
  public CompilerError Error;
}

public class BoaCodeProvider : CodeDomProvider
{ public override string FileExtension { get { return "boa"; } }

  public override ICodeCompiler CreateCompiler() { return new Compiler(); }
  public override ICodeGenerator CreateGenerator() { return new Generator(); }
}

} // namespace AdamMil.Boa

