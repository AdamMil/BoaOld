using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;

namespace AdamMil.Boa
{

internal class Generator : ICodeGenerator
{
  #region ICodeGenerator
  public string CreateEscapedIdentifier(string value) { return CreateValidIdentifier(value); }

  public string CreateValidIdentifier(string value)
  { if(!IsValidIdentifier(value)) value += '_';
    return value;
  }

  public void GenerateCodeFromCompileUnit(CodeCompileUnit e, System.IO.TextWriter w, CodeGeneratorOptions o)
  { try
    { Init(w, o);
      WriteUnit(e);
    }
    finally { Deinit(); }
  }

  public void GenerateCodeFromExpression(CodeExpression e, System.IO.TextWriter w, CodeGeneratorOptions o)
  { try
    { Init(w, o);
      WriteExpression(e);
      Deinit();
    }
    finally { Deinit(); }
  }

  public void GenerateCodeFromNamespace(CodeNamespace e, System.IO.TextWriter w, CodeGeneratorOptions o)
  { try
    { Init(w, o);
      WriteNamespace(e);
      Deinit();
    }
    finally { Deinit(); }
  }

  public void GenerateCodeFromStatement(CodeStatement e, System.IO.TextWriter w, CodeGeneratorOptions o)
  { try
    { Init(w, o);
      WriteStatement(e);
      Deinit();
    }
    finally { Deinit(); }
  }

  public void GenerateCodeFromType(CodeTypeDeclaration e, System.IO.TextWriter w, CodeGeneratorOptions o)
  { try
    { Init(w, o);
      WriteTypeDecl(e);
      Deinit();
    }
    finally { Deinit(); }
  }

  public string GetTypeOutput(CodeTypeReference type)
  { string btype;
    switch(type.BaseType)
    { case "System.Int32":  btype = "int"; break;
      case "System.String": btype = "string"; break;
      case "System.Object": btype = "var"; break;
      case "System.Byte":   btype = "byte"; break;
      case "System.Single": btype = "float"; break;
      case "System.Double": btype = "double"; break;
      case "System.Void":   btype = "void"; break;
      case "System.Int16":  btype = "short"; break;
      case "System.Int64":  btype = "long"; break;
      case "System.UInt32": btype = "uint"; break;
      case "System.UInt16": btype = "ushort"; break;
      case "System.UInt64": btype = "ulong"; break;
      case "System.SByte":  btype = "sbyte"; break;
      default: btype = type.BaseType; break;
    }
    if(type.ArrayRank>0) btype += TypeRefArr(type);
    return btype;
  }

  public bool IsValidIdentifier(string value) // TODO: does this have to handle fully-qualified identifiers?
  { return !Parser.stringTokens.Contains(value) && !Parser.typeTokens.Contains(value);
  }
  
  public bool Supports(GeneratorSupport supports) { return (supports&LanguageSupport) == supports; }

  public void ValidateIdentifier(string value)
  { if(!IsValidIdentifier(value)) throw new ArgumentException("This is a reserved keyword", "value");
  }
  #endregion

  const GeneratorSupport LanguageSupport = GeneratorSupport.ArraysOfArrays | GeneratorSupport.GotoStatements |
    GeneratorSupport.ChainedConstructorArguments | GeneratorSupport.ComplexExpressions |
    GeneratorSupport.DeclareDelegates | GeneratorSupport.ReturnTypeAttributes | GeneratorSupport.DeclareEvents |
    GeneratorSupport.DeclareInterfaces | GeneratorSupport.DeclareValueTypes | GeneratorSupport.EntryPointMethod |
    GeneratorSupport.MultidimensionalArrays | GeneratorSupport.NestedTypes | GeneratorSupport.StaticConstructors |
    GeneratorSupport.MultipleInterfaceMembers | GeneratorSupport.PublicStaticMembers |
    GeneratorSupport.ReferenceParameters | GeneratorSupport.DeclareEnums | GeneratorSupport.TryCatchStatements;

  void Deinit() { writer=null; opts=null; }

  void Indent() { indent++; }

  void Init(System.IO.TextWriter w, CodeGeneratorOptions o) { writer=w; opts=o; indent=0; }

  string TypeRefArr(CodeTypeReference type)
  { string ret = type.ArrayRank==0 ? string.Empty : TypeRefArr(type.ArrayElementType);
    ret += '[';
    for(int i=1; i<type.ArrayRank; i++) ret += ',';
    return ret += ']';
  }
  
  void Unindent() { indent--; }

  void WriteComment(CodeCommentStatement cs)
  { string str = cs.Comment.Text.Replace("\r", "");
    if(str.IndexOf('\n')==-1) WriteLine("# "+str);
    else
    { string[] strs = str.Replace("*/", "* /").Split('\n');
      for(int i=0; i<strs.Length; i++) WriteLine((i==0 ? "/* " : "   ")+strs[i]);
      WriteLine("*/");
    }
  }

  void WriteComments(CodeCommentStatementCollection csc) { foreach(CodeCommentStatement cs in csc) WriteComment(cs); }

  void Write(string str)
  { for(int i=0; i<indent; i++) writer.Write(opts.IndentString);
    writer.Write(str);
  }

  void WriteAccess(CodeTypeDeclaration td)
  { switch(td.TypeAttributes&TypeAttributes.VisibilityMask)
    { case TypeAttributes.Public: case TypeAttributes.NestedPublic: Write("public: "); break;
      case TypeAttributes.NestedAssembly: case TypeAttributes.NestedFamANDAssem: Write("internal: "); break;
      case TypeAttributes.NestedFamORAssem: Write("family: "); break;
      case TypeAttributes.NestedFamily: Write("protected: "); break;
      default: Write("private: "); break;
    }
  }
  
  void WriteAttributes(CodeAttributeDeclarationCollection adc)
  { if(adc.Count==0) return;
    writer.Write("with ");
    bool wrote=false;
    foreach(CodeAttributeDeclaration ad in adc)
    { if(wrote) writer.Write(", ");
      else wrote=true;
      writer.Write(ad.Name);
      if(ad.Arguments.Count>0)
      { writer.Write('(');
        bool wa=false;
        foreach(CodeAttributeArgument aa in ad.Arguments)
        { if(wa) writer.Write(", ");
          else wa=true;
          writer.Write(aa.Name);
          writer.Write('=');
          WriteExpression(aa.Value);
        }
        writer.Write(')');
      }
    }
  }

  void WriteBlock(CodeStatementCollection sc, bool allowEmpty)
  { if(sc.Count==0) writer.WriteLine(allowEmpty ? ";" : ": pass");
    else
    { writer.WriteLine();
      Indent();
      foreach(CodeStatement s in sc) WriteStatement(s);
      Unindent();
    }
  }

  void WriteClass(CodeTypeDeclaration td)
  { WriteAccess(td);
    if(td.IsClass) writer.Write("class ");
    else if(td.IsStruct) writer.Write("struct ");
    else if(td.IsInterface) writer.Write("interface ");
    else throw new ArgumentException("Invalid type declaration for '"+td.Name+'\'');
    WriteAttributes(td.CustomAttributes);
    WriteIdent(td.Name);
    if(td.BaseTypes.Count>0)
    { writer.Write(" (");
      bool wrote=false;
      foreach(CodeTypeReference tr in td.BaseTypes)
      { if(wrote) writer.Write(',');
        else wrote=true;
        writer.Write(GetTypeOutput(tr));
      }
      writer.Write(") ");
    }
    writer.WriteLine();
    Indent();
    foreach(CodeTypeMember tm in td.Members)
    { WriteComments(tm.Comments);
      WriteFlags(tm.Attributes & MemberAttributes.AccessMask);
      if(tm is CodeMemberMethod) WriteMethod((CodeMemberMethod)tm);
      else if(tm is CodeMemberProperty)
      { CodeMemberProperty mp = (CodeMemberProperty)tm;

        if(mp.Parameters.Count>0) // handle indexer
        { CodeMemberMethod mm = new CodeMemberMethod();
          mm.Attributes = mp.Attributes;
          mm.CustomAttributes = mp.CustomAttributes;
          mm.Parameters.AddRange(mp.Parameters);
          mm.ReturnType = mp.Type;
          if(mp.HasGet)
          { mm.Name = "__getitem__";
            mm.Statements.AddRange(mp.GetStatements);
            WriteMethod(mm);
          }
          if(mp.HasSet)
          { if(mp.HasGet) WriteFlags(tm.Attributes & MemberAttributes.AccessMask);
            mm.Name = "__setitem__";
            mm.Statements.Clear();
            mm.Statements.AddRange(mp.SetStatements);
            mm.Parameters.Add(new CodeParameterDeclarationExpression(mp.Type, "value"));
            WriteMethod(mm);
          }
          continue;
        }

        writer.Write(": prop ");
        WriteFlags(mp.Attributes & (MemberAttributes.Abstract | MemberAttributes.New | MemberAttributes.Override |
                                    MemberAttributes.Static));
        WriteType(mp.Type);
        WriteIdent(mp.Name);
        writer.WriteLine();
        if(!mp.HasSet && !mp.HasGet)
          throw new ArgumentException(string.Format("Property '{0}' has no get or set accessors", mp.Name));
        Indent();
        if(mp.HasGet)
        { Write("get");
          WriteBlock(mp.GetStatements, true);
        }
        if(mp.HasSet)
        { Write("set");
          WriteBlock(mp.SetStatements, true);
        }
        Unindent();
      }
      else if(tm is CodeMemberField)
      { CodeMemberField mf = (CodeMemberField)tm;
        WriteFlags(mf.Attributes & (MemberAttributes.Const|MemberAttributes.Static));
        writer.Write(": ");
        WriteType(mf.Type);
        WriteAttributes(mf.CustomAttributes);
        WriteIdent(mf.Name);
        if(mf.InitExpression!=null)
        { writer.Write('=');
          WriteExpression(mf.InitExpression);
        }
        writer.WriteLine();
      }
      else if(tm is CodeMemberEvent)
      { CodeMemberEvent me = (CodeMemberEvent)tm;
        writer.Write(": event ");
        WriteFlags(me.Attributes & MemberAttributes.Static);
        WriteAttributes(me.CustomAttributes);
        WriteType(me.Type);
        WriteIdent(me.Name);
        writer.WriteLine();
      }
      else throw new ArgumentException(string.Format("Unknown member type {0} for {1}", tm.GetType(), td.Name));
      if(opts.BlankLinesBetweenMembers) writer.WriteLine();
    }
    Unindent();
  }

  void WriteEnum(CodeTypeDeclaration td)
  { WriteAccess(td);
    writer.Write("enum ");
    if(td.BaseTypes.Count>0) writer.Write(GetTypeOutput(td.BaseTypes[0])+' ');
    WriteAttributes(td.CustomAttributes);
    WriteIdent(td.Name);
    Indent();
    bool wrote=false;
    foreach(CodeTypeMember tm in td.Members)
    { if(!(tm is CodeMemberField))
        throw new ArgumentException(string.Format("A non-field '{0}' found inside enum '{1}'", tm.Name, td.Name));
      CodeMemberField mf = (CodeMemberField)tm;
      if(wrote) writer.Write(",\n");
      else { writer.Write('\n'); wrote=true; }
      WriteIndent();
      WriteAttributes(mf.CustomAttributes);
      WriteIdent(mf.Name);
      if(mf.InitExpression!=null)
      { writer.Write("= ");
        WriteExpression(mf.InitExpression);
      }
    }
    writer.WriteLine();
    Unindent();
  }

  void WriteExpression(CodeExpression e)
  { if(e is CodeBinaryOperatorExpression)
    { writer.Write('(');
      CodeBinaryOperatorExpression bo = (CodeBinaryOperatorExpression)e;
      WriteExpression(bo.Left);
      switch(bo.Operator)
      { case CodeBinaryOperatorType.Add: writer.Write("+ "); break;
        case CodeBinaryOperatorType.Assign: writer.Write("= "); break;
        case CodeBinaryOperatorType.BitwiseAnd: writer.Write("& "); break;
        case CodeBinaryOperatorType.BitwiseOr: writer.Write("| "); break;
        case CodeBinaryOperatorType.BooleanAnd: writer.Write("&& "); break;
        case CodeBinaryOperatorType.BooleanOr: writer.Write("|| "); break;
        case CodeBinaryOperatorType.Divide: writer.Write("/ "); break;
        case CodeBinaryOperatorType.GreaterThan: writer.Write("> "); break;
        case CodeBinaryOperatorType.GreaterThanOrEqual: writer.Write(">= "); break;
        case CodeBinaryOperatorType.IdentityEquality: writer.Write("=== "); break;
        case CodeBinaryOperatorType.IdentityInequality: writer.Write("!== "); break;
        case CodeBinaryOperatorType.LessThan: writer.Write("< "); break;
        case CodeBinaryOperatorType.LessThanOrEqual: writer.Write("<= "); break;
        case CodeBinaryOperatorType.Modulus: writer.Write("% "); break;
        case CodeBinaryOperatorType.Multiply: writer.Write("* "); break;
        case CodeBinaryOperatorType.Subtract: writer.Write("- "); break;
        case CodeBinaryOperatorType.ValueEquality: writer.Write("== "); break;
      }
      WriteExpression(bo.Right);
      writer.Write(')');
    }
    else if(e is CodeArgumentReferenceExpression) WriteIdent(((CodeArgumentReferenceExpression)e).ParameterName);
    else if(e is CodeArrayCreateExpression)
    { CodeArrayCreateExpression ac = (CodeArrayCreateExpression)e;
      if(ac.Initializers.Count==0)
      { writer.Write("new ");
        WriteType(ac.CreateType);
        writer.Write('[');
        if(ac.SizeExpression!=null) WriteExpression(ac.SizeExpression);
        else writer.Write(ac.Size);
        writer.Write(']');
      }
      else
      { writer.Write("[ ");
        WriteExpressionList(ac.Initializers);
        writer.Write(']');
      }
    }
    else if(e is CodeArrayIndexerExpression)
    { CodeArrayIndexerExpression ai = (CodeArrayIndexerExpression)e;
      WriteExpression(ai.TargetObject);
      writer.Write('[');
      WriteExpressionList(ai.Indices);
      writer.Write(']');
    }
    else if(e is CodeBaseReferenceExpression) writer.Write("super");
    else if(e is CodeCastExpression)
    { CodeCastExpression c = (CodeCastExpression)e;
      WriteType(c.TargetType);
      writer.Write('(');
      WriteExpression(c.Expression);
      writer.Write(')');
    }
    else if(e is CodeDelegateCreateExpression)
    { CodeDelegateCreateExpression dc = (CodeDelegateCreateExpression)e;
      WriteExpression(dc.TargetObject);
      writer.Write('.');
      writer.Write(dc.MethodName);
    }
    else if(e is CodeDelegateInvokeExpression)
    { CodeDelegateInvokeExpression di = (CodeDelegateInvokeExpression)e;
      WriteExpression(di.TargetObject);
      writer.Write('(');
      WriteExpressionList(di.Parameters);
      writer.Write(')');
    }
    else if(e is CodeDirectionExpression)
    { CodeDirectionExpression d = (CodeDirectionExpression)e;
      if(d.Direction==FieldDirection.Ref) writer.Write("ref ");
      else if(d.Direction==FieldDirection.Out) writer.Write("out ");
      WriteExpression(d.Expression);
    }
    else if(e is CodeEventReferenceExpression)
    { CodeEventReferenceExpression er = (CodeEventReferenceExpression)e;
      WriteExpression(er.TargetObject);
      writer.Write('.');
      writer.Write(er.EventName);
    }
    else if(e is CodeFieldReferenceExpression)
    { CodeFieldReferenceExpression fr = (CodeFieldReferenceExpression)e;
      WriteExpression(fr.TargetObject);
      writer.Write('.');
      writer.Write(fr.FieldName);
    }
    else if(e is CodeIndexerExpression)
    { CodeIndexerExpression ie = (CodeIndexerExpression)e;
      WriteExpression(ie.TargetObject);
      writer.Write('[');
      WriteExpressionList(ie.Indices);
      writer.Write(']');
    }
    else if(e is CodeMethodInvokeExpression)
    { CodeMethodInvokeExpression mi = (CodeMethodInvokeExpression)e;
      WriteExpression(mi.Method);
      writer.Write('(');
      WriteExpressionList(mi.Parameters);
      writer.Write(')');
    }
    else if(e is CodeMethodReferenceExpression)
    { CodeMethodReferenceExpression mr = (CodeMethodReferenceExpression)e;
      WriteExpression(mr.TargetObject);
      writer.Write('.');
      writer.Write(mr.MethodName);
    }
    else if(e is CodeObjectCreateExpression)
    { CodeObjectCreateExpression oc = (CodeObjectCreateExpression)e;
      writer.Write("new ");
      WriteType(oc.CreateType);
      writer.Write('(');
      WriteExpressionList(oc.Parameters);
      writer.Write(')');
    }
    else if(e is CodeParameterDeclarationExpression)
    { CodeParameterDeclarationExpression pd = (CodeParameterDeclarationExpression)e;
      if(pd.Direction==FieldDirection.Ref) writer.Write("ref ");
      else if(pd.Direction==FieldDirection.Out) writer.Write("out ");
      WriteType(pd.Type);
      WriteAttributes(pd.CustomAttributes);
      WriteIdent(pd.Name);
    }
    else if(e is CodePrimitiveExpression)
    { CodePrimitiveExpression p = (CodePrimitiveExpression)e;
      if(p.Value==null) writer.Write("null");
      else if(p.Value is bool) writer.Write((bool)p.Value ? "true" : "false");
      else if(p.Value is string)
      { string str = (string)p.Value, nstr = "\"";
        for(int i=0; i<str.Length; i++)
        { char c = str[i];
          switch(c)
          { case '\\': nstr += @"\\"; break;
            case '\"': nstr += @"\"""; break;
            case '\n': nstr += @"\n"; break;
            case '\t': nstr += @"\t"; break;
            case '\r': nstr += @"\r"; break;
            case '\b': nstr += @"\b"; break;
            case (char)27: nstr += @"\e"; break;
            case '\a': nstr += @"\a"; break;
            case '\f': nstr += @"\f"; break;
            case '\v': nstr += @"\v"; break;
            default:
              if(c<32 || c>127) nstr += string.Format(@"\u{X4:0}", (int)c);
              else nstr += c;
              break;
          }
        }
      }
      else writer.Write(p.Value.ToString());
    }
    else if(e is CodePropertyReferenceExpression)
    { CodePropertyReferenceExpression pr = (CodePropertyReferenceExpression)e;
      WriteExpression(pr.TargetObject);
      writer.Write('.');
      writer.Write(pr.PropertyName);
    }
    else if(e is CodePropertySetValueReferenceExpression) writer.Write("value");
    else if(e is CodeSnippetExpression)
    { writer.Write('(');
      writer.Write(((CodeSnippetExpression)e).Value);
      writer.Write(')');
    }
    else if(e is CodeThisReferenceExpression) writer.Write("self");
    else if(e is CodeTypeOfExpression)
    { writer.Write("typeof ");
      WriteType(((CodeTypeOfExpression)e).Type);
    }
    else if(e is CodeTypeReferenceExpression) WriteType(((CodeTypeReferenceExpression)e).Type);
    else if(e is CodeVariableReferenceExpression) WriteIdent(((CodeVariableReferenceExpression)e).VariableName);
    writer.Write(' ');
  }

  void WriteExpressionList(CodeExpressionCollection ec)
  { bool wrote=false;
    foreach(CodeExpression e in ec)
    { if(wrote) writer.Write(", ");
      else wrote=true;
      WriteExpression(e);
    }
  }

  void WriteFlags(MemberAttributes ma)
  { switch(ma&MemberAttributes.AccessMask)
    { case MemberAttributes.Public: writer.Write("public "); break;
      case MemberAttributes.Family: writer.Write("protected "); break;
      case MemberAttributes.Assembly: case MemberAttributes.FamilyAndAssembly: writer.Write("internal "); break;
      case MemberAttributes.FamilyOrAssembly: writer.Write("family "); break;
      case MemberAttributes.Private: writer.Write("private "); break;
    }
    if((ma&MemberAttributes.Abstract) != 0) writer.Write("abstract ");
    if((ma&MemberAttributes.Const) != 0) writer.Write("const ");
    if((ma&MemberAttributes.New) != 0) writer.Write("new ");
    if((ma&MemberAttributes.Override) != 0) writer.Write("override ");
    if((ma&MemberAttributes.Static) != 0) writer.Write("static ");
  }

  void WriteIdent(string ident)
  { writer.Write(CreateValidIdentifier(ident));
    writer.Write(' ');
  }

  void WriteIndent() { for(int i=0; i<indent; i++) writer.Write(opts.IndentString); }

  void WriteLine(string str)
  { for(int i=0; i<indent; i++) writer.Write(opts.IndentString);
    writer.WriteLine(str);
  }

  void WriteMethod(CodeMemberMethod mm)
  { writer.Write(": def ");
    WriteFlags(mm.Attributes & (MemberAttributes.Abstract | MemberAttributes.New | MemberAttributes.Override |
                                MemberAttributes.Static));
    WriteAttributes(mm.ReturnTypeCustomAttributes);
    WriteType(mm.ReturnType);
    WriteAttributes(mm.CustomAttributes);
    WriteIdent(mm.Name);
    writer.Write('(');
    WriteParameters(mm.Parameters);
    writer.Write(')');
    WriteBlock(mm.Statements, true);
  }

  void WriteNamespace(CodeNamespace ns)
  { WriteComments(ns.Comments);
    WriteLine("package "+ns.Name);
    Indent();
    foreach(CodeNamespaceImport ni in ns.Imports) WriteLine("import "+ni.Namespace);
    foreach(CodeTypeDeclaration td in ns.Types) WriteTypeDecl(td);
    Unindent();
  }

  void WriteParameters(CodeParameterDeclarationExpressionCollection dec)
  { bool wrote=false;
    foreach(CodeParameterDeclarationExpression de in dec)
    { if(wrote) writer.Write(", ");
      else wrote=true;
      if(de.Direction==FieldDirection.Ref) writer.Write("ref ");
      else if(de.Direction==FieldDirection.Out) writer.Write("out ");
      WriteType(de.Type);
      WriteAttributes(de.CustomAttributes);
      WriteIdent(de.Name);
      foreach(CodeAttributeDeclaration ad in de.CustomAttributes)
        if(ad.Name=="DefaultValueAttribute" || ad.Name=="DefaultValue")
          foreach(CodeAttributeArgument aa in ad.Arguments)
            if(aa.Name=="Value")
            { writer.Write('=');
              WriteExpression(aa.Value);
              goto next;
            }
      next:;
    }
  }

  void WriteStatement(CodeStatement stmt)
  { if(stmt is CodeIterationStatement)
    { CodeIterationStatement it = (CodeIterationStatement)stmt;
      if(it.Statements.Count!=0 || it.IncrementStatement!=null)
      { if(it.InitStatement!=null) WriteStatement(it.InitStatement);
        Write("while ");
        if(it.TestExpression==null) writer.WriteLine("true");
        else
        { WriteExpression(it.TestExpression);
          writer.WriteLine();
        }
        Indent();
        foreach(CodeStatement s in it.Statements) WriteStatement(s);
        if(it.IncrementStatement!=null) WriteStatement(it.IncrementStatement);
        Unindent();
      }
      else if(it.InitStatement==null) WriteLine("pass");
    }
    else if(stmt is CodeSnippetStatement) writer.Write(((CodeSnippetStatement)stmt).Value);
    else if(stmt is CodeCommentStatement) WriteComment((CodeCommentStatement)stmt);
    else
    { WriteIndent();
      if(stmt is CodeExpressionStatement) WriteExpression(((CodeExpressionStatement)stmt).Expression);
      else if(stmt is CodeAssignStatement)
      { CodeAssignStatement a = (CodeAssignStatement)stmt;
        WriteExpression(a.Left);
        writer.Write('=');
        WriteExpression(a.Right);
      }
      else if(stmt is CodeConditionStatement)
      { CodeConditionStatement c = (CodeConditionStatement)stmt;
        writer.Write(c.TrueStatements.Count==0 ? "if ! " : "if ");
        WriteExpression(c.Condition);
        if(c.TrueStatements.Count==0) WriteBlock(c.FalseStatements, false);
        else
        { WriteBlock(c.TrueStatements, false);
          if(c.FalseStatements.Count>0)
          { Write("else");
            WriteBlock(c.FalseStatements, false);
          }
        }
      }
      else if(stmt is CodeVariableDeclarationStatement)
      { CodeVariableDeclarationStatement vd = (CodeVariableDeclarationStatement)stmt;
        WriteType(vd.Type);
        WriteIdent(vd.Name);
        if(vd.InitExpression!=null)
        { writer.Write('=');
          WriteExpression(vd.InitExpression);
        }
      }
      else if(stmt is CodeMethodReturnStatement)
      { CodeMethodReturnStatement mr = (CodeMethodReturnStatement)stmt;
        writer.Write("return ");
        if(mr.Expression!=null) WriteExpression(mr.Expression);
      }
      else if(stmt is CodeAttachEventStatement)
      { CodeAttachEventStatement ae = (CodeAttachEventStatement)stmt;
        WriteExpression(ae.Event);
        writer.Write(" += ");
        WriteExpression(ae.Listener);
      }
      else if(stmt is CodeRemoveEventStatement)
      { CodeRemoveEventStatement re = (CodeRemoveEventStatement)stmt;
        WriteExpression(re.Event);
        writer.Write(" -= ");
        WriteExpression(re.Listener);
      }
      else if(stmt is CodeThrowExceptionStatement)
      { CodeThrowExceptionStatement te = (CodeThrowExceptionStatement)stmt;
        writer.Write("raise ");
        WriteExpression(te.ToThrow);
      }
      else if(stmt is CodeTryCatchFinallyStatement)
      { CodeTryCatchFinallyStatement tcf = (CodeTryCatchFinallyStatement)stmt;
        writer.Write("try");
        WriteBlock(tcf.TryStatements, false);
        foreach(CodeCatchClause cc in tcf.CatchClauses)
        { Write("except ");
          if(cc.CatchExceptionType!=null)
          { WriteType(cc.CatchExceptionType);
            if(cc.LocalName!=null) WriteIdent(cc.LocalName);
          }
          WriteBlock(cc.Statements, false);
        }
        if(tcf.FinallyStatements.Count>0)
        { Write("finally");
          WriteBlock(tcf.FinallyStatements, false);
        }
      }
      else if(stmt is CodeGotoStatement)
        writer.Write("goto "+CreateValidIdentifier(((CodeGotoStatement)stmt).Label));
      else if(stmt is CodeLabeledStatement)
        writer.Write("label "+CreateValidIdentifier(((CodeLabeledStatement)stmt).Label));
      writer.WriteLine();
    }
  }

  void WriteType(CodeTypeReference tr)
  { writer.Write(GetTypeOutput(tr));
    writer.Write(' ');
  }

  void WriteTypeDecl(CodeTypeDeclaration td)
  { WriteComments(td.Comments);
    if(td.IsEnum) WriteEnum(td);
    else WriteClass(td);
  }

  void WriteUnit(CodeCompileUnit unit)
  { foreach(string ra in unit.ReferencedAssemblies) writer.WriteLine("import "+ra);
    foreach(CodeNamespace cn in unit.Namespaces) WriteNamespace(cn);
  }

  System.IO.TextWriter writer;
  CodeGeneratorOptions opts;
  int indent;
  
  const string HexDigits = "0123456789ABCDEF";
}

} // namespace AdamMil.Boa

