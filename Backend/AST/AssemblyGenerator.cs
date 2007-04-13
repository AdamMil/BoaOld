/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

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
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using Boa.Runtime;

namespace Boa.AST
{

public sealed class AssemblyGenerator
{ public AssemblyGenerator(string moduleName, string outFileName) : this(moduleName, outFileName, Options.Debug) { }
  public AssemblyGenerator(string moduleName, string outFileName, bool debug)
  { string dir = System.IO.Path.GetDirectoryName(outFileName);
    if(dir=="") dir=null;
    outFileName = System.IO.Path.GetFileName(outFileName);

    AssemblyName an = new AssemblyName();
    an.Name  = moduleName;
    Assembly = AppDomain.CurrentDomain
                 .DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave, dir, null, null, null, null, true);
    Module   = Assembly.DefineDynamicModule(outFileName, outFileName, debug);
    Symbols  = debug ? Module.DefineDocument(outFileName, Guid.Empty, Guid.Empty, SymDocumentType.Text) : null;
    OutFileName = outFileName;
  }

  public TypeGenerator DefineType(string name) { return DefineType(TypeAttributes.Public, name, null); }
  public TypeGenerator DefineType(string name, Type parent)
  { return DefineType(TypeAttributes.Public, name, parent);
  }
  public TypeGenerator DefineType(TypeAttributes attrs, string name, Type parent)
  { return new TypeGenerator(this, Module.DefineType(name, attrs, parent));
  }

  public void Save() { Assembly.Save(OutFileName); }

  public AssemblyBuilder Assembly;
  public ModuleBuilder   Module;
  public ISymbolDocumentWriter Symbols;
  public string OutFileName;
}

} // namespace Boa.AST
