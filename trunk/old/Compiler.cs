using System;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace AdamMil.Boa
{

internal class Compiler : ICodeCompiler
{
  public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
  { return CompileAssemblyFromDomBatch(options, new CodeCompileUnit[] { compilationUnit });
  }

  public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
  { System.Collections.Specialized.StringCollection sc = options.ReferencedAssemblies;
    foreach(string a in options.ReferencedAssemblies) if(!sc.Contains(a)) sc.Add(a);

    Node tree = new Node(Token.Assembly);
    Parser parser = new Parser();
    CompilerResults cr = new CompilerResults(new TempFileCollection());

    int i=1;
    foreach(CodeCompileUnit unit in compilationUnits)
    { foreach(string a in unit.ReferencedAssemblies) if(!sc.Contains(a)) sc.Add(a);
      tree.Add(parser.ParseUnit(cr, "dom" + i++.ToString(), DomToString(unit)));
    }
    return cr.Errors.HasErrors ? cr : CompileTree(options, cr, tree);
  }

  public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
  { return CompileAssemblyFromFileBatch(options, new string[] { fileName });
  }

  public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
  { Node tree = new Node(Token.Assembly);
    Parser parser = new Parser();
    CompilerResults cr = new CompilerResults(new TempFileCollection());

    foreach(string filename in fileNames) tree.Add(parser.ParseUnit(cr, filename, FileToString(filename)));
    return cr.Errors.HasErrors ? cr : CompileTree(options, cr, tree);
  }

  public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
  { return CompileAssemblyFromSourceBatch(options, new string[] { source });
  }

  public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
  { Node tree = new Node(Token.Assembly);
    Parser parser = new Parser();
    CompilerResults cr = new CompilerResults(new TempFileCollection());

    int i=1;
    foreach(string source in sources) tree.Add(parser.ParseUnit(cr, "source" + i++.ToString(), source));
    return cr.Errors.HasErrors ? cr : CompileTree(options, cr, tree);
  }

  TypeBuilder CurrentTB { get { return curTB.Count==0 ? null : (TypeBuilder)curTB.Peek(); } }

  // TODO: support class attributes (packing, layout, etc)
  // TODO: support inheritance and interfaces
  void CompileClass(DeclNode node)
  { TypeAttributes ta = TypeAttributes.Class | AttrFromAccess(node.Access);
    if((node.Access&Access.Abstract)!=0) ta |= TypeAttributes.Abstract;
    if((node.Access&Access.Sealed)!=0) ta |= TypeAttributes.Sealed;
    TypeBuilder tb = mod.DefineType((string)node.Value, ta);
    curTB.Push(tb);

    for(int i=0; i<node.Count; i++)
      switch(node[i].Token)
      { case Token.Class: CompileClass((DeclNode)node[i]); break;
        case Token.Declare: CompileVar((DeclNode)node[i]); break;
        case Token.Def: CompileFunc((DeclNode)node[i]); break;
        case Token.Delegate: CompileDelegate((DeclNode)node[i]); break;
        case Token.Enum: CompileEnum((DeclNode)node[i]); break;
        default: UnexpectedNode(node[i]); break;
      }

    curTB.Pop();
  }

  void CompileDelegate(DeclNode node)
  { TypeAttributes ta = AttrFromAccess(node.Access)|TypeAttributes.Sealed|TypeAttributes.Class;
    TypeBuilder tb = CurrentTB;
    tb = tb==null ? mod.DefineType((string)node.Value, ta, typeof(MulticastDelegate))
                  : tb.DefineNestedType((string)node.Value, ta, typeof(MulticastDelegate));

    MethodAttributes ma = MethodAttributes.Public|MethodAttributes.SpecialName|MethodAttributes.RTSpecialName|
                          MethodAttributes.HideBySig;
    MethodBuilder mb = tb.DefineMethod(".ctor", ma, null, new Type[] { typeof(object), typeof(IntPtr) });
    mb.SetImplementationFlags(MethodImplAttributes.Managed|MethodImplAttributes.Runtime);

    Type[] parms = ParametersFromNode(node);
    Type   rtype = TypeFromNode(node);

    ma = MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.Virtual;
    mb = tb.DefineMethod("Invoke", ma, rtype, parms);
    mb.SetImplementationFlags(MethodImplAttributes.Managed|MethodImplAttributes.Runtime);

    Type[] biparms = new Type[parms.Length+2];
    Array.Copy(parms, biparms, parms.Length);
    biparms[parms.Length]   = typeof(AsyncCallback);
    biparms[parms.Length+1] = typeof(object);
    ma = MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.NewSlot|MethodAttributes.Virtual;
    mb = tb.DefineMethod("BeginInvoke", ma, typeof(IAsyncResult), biparms);
    mb.SetImplementationFlags(MethodImplAttributes.Managed|MethodImplAttributes.Runtime);

    mb = tb.DefineMethod("EndInvoke", ma, rtype, new Type[] { typeof(IAsyncResult) });
    mb.SetImplementationFlags(MethodImplAttributes.Managed|MethodImplAttributes.Runtime);
  }

  void CompileEnum(DeclNode node)
  {
  }

  void CompileFunc(DeclNode node)
  {
  }

  void CompileNamespace(Node tree)
  { for(int i=0; i<tree.Count; i++)
      switch(tree[i].Token)
      { case Token.Class: CompileClass((DeclNode)tree[i]); break;
        case Token.Declare: CompileVar((DeclNode)tree[i]); break;
        case Token.Def: CompileFunc((DeclNode)tree[i]); break;
        case Token.Delegate: CompileDelegate((DeclNode)tree[i]); break;
        case Token.Enum: CompileEnum((DeclNode)tree[i]); break;
        case Token.Package: CompileNamespace(tree[i]); break;
        default: UnexpectedNode(tree[i]); break;
      }
  }

  void CompileVar(DeclNode node)
  {
  }

  CompilerResults CompileTree(CompilerParameters options, CompilerResults results, Node tree)
  { AssemblyName an = new AssemblyName();
    an.Name = "BoaAssembly";

    AssemblyBuilder ab = System.AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);
    mod = ab.DefineDynamicModule(an.Name);
    res = results;

    try
    { for(int i=0; i<tree.Count; i++)
      { Node child = tree[i];
        if(child.Token != Token.Unit) UnexpectedNode(child);
        for(int j=0; j<child.Count; j++)
          switch(child[j].Token)
          { case Token.Package: CompileNamespace(child[j]); break;
            case Token.Import: break; // TODO: implement me
            default: UnexpectedNode(child[j]); break;
          }
      }
      ab.Save(an.Name+".dll");
    }
    catch(CompilerErrorException) { }

    res=null; mod=null;
    return results;
  }

  Type[] ParametersFromNode(Node node) { }
  Type TypeFromNode(Node node) { }

  void UnexpectedNode(Node node)
  { CompilerError ce = new CompilerError("", 0, 0, "0", "Unexpected token: "+node.Token);
    res.Errors.Add(ce);
    throw new CompilerErrorException(ce);
  }

  CompilerResults res;
  ModuleBuilder   mod;
  Stack           curTB=new Stack();

  static TypeAttributes AttrFromAccess(Access access)
  { if((access&Access.AccessMask)==Access.Family) return TypeAttributes.NestedFamORAssem;
    else if((access&Access.AccessMask)==Access.Internal) return TypeAttributes.NestedAssembly;
    else if((access&Access.AccessMask)==Access.Private) return TypeAttributes.NotPublic;
    else if((access&Access.AccessMask)==Access.Protected) return TypeAttributes.NestedFamily;
    else if((access&Access.AccessMask)==Access.Public) return TypeAttributes.Public;
    throw new ApplicationException("Impossible");
  }

  static string DomToString(CodeCompileUnit dom)
  { StringWriter sw = new StringWriter();
    CodeGeneratorOptions cgo = new CodeGeneratorOptions();
    cgo.BlankLinesBetweenMembers = true;
    cgo.ElseOnClosing = false;
    cgo.IndentString = "  ";
    new BoaCodeProvider().CreateGenerator().GenerateCodeFromCompileUnit(dom, sw, cgo);
    return sw.ToString();
  }

  static string FileToString(string filename)
  { StreamReader sr = new StreamReader(filename);
    string ret = sr.ReadToEnd();
    sr.Close();
    return ret;
  }
}

} // namespace AdamMil.Boa
