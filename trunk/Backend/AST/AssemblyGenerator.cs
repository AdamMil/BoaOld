using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using Boa.Runtime;

namespace Boa.AST
{

public class AssemblyGenerator
{ public AssemblyGenerator(string moduleName, string outFileName) : this(moduleName, outFileName, Options.Debug) { }
  public AssemblyGenerator(string moduleName, string outFileName, bool debug)
  { AssemblyName an = new AssemblyName();
    an.Name = moduleName;
    Assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
    Module   = Assembly.DefineDynamicModule(outFileName, outFileName, debug);
    Symbols  = debug ? Module.DefineDocument(outFileName, Guid.Empty, Guid.Empty, SymDocumentType.Text) : null;
    OutFileName = outFileName;
  }

  public TypeGenerator DefineType(string name) { return DefineType(name, null); }
  public TypeGenerator DefineType(string name, Type parent)
  { return new TypeGenerator(this, Module.DefineType(name, TypeAttributes.Public, parent));
  }

  public void Save() { Assembly.Save(OutFileName); }
  public void Save(string fileName) { Assembly.Save(fileName); }

  public AssemblyBuilder Assembly;
  public ModuleBuilder   Module;
  public ISymbolDocumentWriter Symbols;
  public string OutFileName;
}

} // namespace Boa.AST
