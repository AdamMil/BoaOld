using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;

// TODO: make some keywords unreserved (case, others?)
// TODO: allow more errors (only throw on critical ones)
// TODO: implement assembly attributes
// TODO: implement "out" parameters
// TODO: implement enum value attributes
// TODO: implement return value attributes
// TODO: implement backquote operator
// TODO: think about metaclasses (calling a class object returns an instance. calling a metaclass object returns a class object)

namespace AdamMil.Boa
{

internal class Parser
{ 
  #region Static constructor
  static Parser()
  { stringTokens = new Hashtable();
    Token[] tokens = new Token[]
    { Token.If, Token.Elif, Token.Else, Token.Try, Token.Except, Token.Finally, Token.For, Token.While,
      Token.Do, Token.Def, Token.Is, Token.Typeof, Token.New, Token.In, Token.Break, Token.Raise, Token.Pass,
      Token.Print, Token.Return, Token.Continue, Token.Yield, Token.Import, Token.As, Token.Exec, Token.Class,
      Token.Struct, Token.Self, Token.Static, Token.Virtual, Token.Override, Token.With, Token.Prop, Token.Lambda,
      Token.And, Token.Or, Token.Not, Token.Goto, Token.Label, Token.Ref, Token.Interface, Token.Enum, Token.Sealed,
      Token.Package, Token.Switch, Token.Lock, Token.Event, Token.Delegate, Token.Const, Token.Abstract
    };
    foreach(Token token in tokens) stringTokens.Add(Enum.GetName(typeof(Token), token).ToLower(), token);

    typeTokens = new Hashtable();
    BoaType[] types = new BoaType[]
    { BoaType.Var, BoaType.Void, BoaType.Byte, BoaType.Sbyte, BoaType.Char, BoaType.Short, BoaType.Ushort,
      BoaType.Int, BoaType.Uint, BoaType.Long, BoaType.Ulong, BoaType.Float, BoaType.Double, BoaType.Function,
      BoaType.Hash, BoaType.List, BoaType.String
    };
    foreach(BoaType type in types) typeTokens.Add(Enum.GetName(typeof(BoaType), type).ToLower(), type);
  }
  #endregion

  public Parser() { }

  public Parser(string input)
  { Reset();
    state.indent.Push(-1);
    text = input;
  }

  public Node ParseUnit(CompilerResults results, string filename, string input)
  { Reset();
    state.indent.Push(-1);
    file = filename;
    text = input;

    Node tree;
    try { tree = ParseUnit(); }
    // TODO: exceptions are too slow for this job!
    // TODO: only one parse error can be handled! this sucks for the developer!
    catch(CompilerErrorException e) { results.Errors.Add(e.Error); tree=null; }
    Reset();
    return tree;
  }

  enum Context { Package, Type, Function }

  struct EscapeChar
  { public char Char;
    public bool Valid;
  }

  int Level { get { return (int)state.indent.Peek(); } }
  bool NewBlock { get { return state.lineBeg || state.token==Token.Colon; } } // doesn't check indentation level
  bool NewIndent { get { return state.newBlock; } }
  bool StatementEnd { get { return state.lineBeg || state.token==Token.Semicolon; } }
  Token lastToken { get { return state.lastToken; } }
  Token token { get { return state.token; } }
  bool lineBeg { get { return state.lineBeg; } }
  object value { get { return state.value; } }

  Node AssertLValue(Node node)
  { if(node.Token==Token.Tuple) for(int i=0; i<node.Count; i++) AssertLValue(node[i]);
    else if(node.Token!=Token.Identifier && node.Token!=Token.Member && node.Token!=Token.Index)
      RaiseError("Only identifiers, members, and sequence elements can be altered.");
    return node;
  }

  void AssertNotNL() { if(lineBeg) RaiseError("Unexpected newline"); }

  void Consume(Token token)
  { Expect(token);
    NextToken();
  }

  void CreateRestorePoint() { restores.Push(state.Clone()); }
  
  void DropRestorePoint() { restores.Pop(); }

  void Expect(Token token)
  { if(this.token!=token) RaiseError(string.Format("Expected {0} but found {1}", token, this.token));
  }
  
  void ExpectNext(Token token)
  { NextToken();
    Expect(token);
  }

  void Expected(string what) { RaiseError("Expected "+what);  }

  #region GetEscapeChar
  /*
  \newline  Ignored
  \\        Backslash
  \"        Double quotation mark
  \n        Newline
  \t        Tab
  \r        Carriage return
  \b        Backspace
  \e        Escape
  \a        Bell
  \f        Form feed
  \v        Vertical tab
  \xHH      Up to 2 hex digits -> byte value
  \uHHHH    Up to 4 hex digits -> 16-bit unicode value
  \cC       Control code (eg, \cC is ctrl-c)
  \OOO      Up to 3 octal digits -> byte value
  */
  EscapeChar GetEscapeChar()
  { EscapeChar ret = new EscapeChar();
    ret.Valid = true;

    char c = ReadChar();
    if(char.IsDigit(c))
    { if(c>'7') RaiseError("Invalid octal digit");
      int num = (c-'0');
      for(int i=1; i<3; i++)
      { c = ReadChar();
        if(!char.IsDigit(c) || c>'7') { state.pushBack=true; break; }
        num = (num<<3) | (c-'0');
      }
      ret.Char = (char)num;
    }
    else switch(c)
    { case '\n': ret.Valid=false; break;
      case '\\': ret.Char = '\\'; break;
      case '\"': ret.Char = '\"'; break;
      case 'n': ret.Char = '\n'; break;
      case 't': ret.Char = '\t'; break;
      case 'r': ret.Char = '\r'; break;
      case 'b': ret.Char = '\b'; break;
      case 'e': ret.Char = (char)27; break;
      case 'a': ret.Char = '\a'; break;
      case 'f': ret.Char = '\f'; break;
      case 'v': ret.Char = '\v'; break;
      case 'x': case 'u':
        { int num = 0;
          for(int i=0,limit=(c=='x'?2:4); i<limit; i++)
          { c = ReadChar();
            if(char.IsDigit(c)) num = (num<<4) | (c-'0');
            else if((c<'A' || c>'F') && (c<'a' || c>'f'))
            { if(i==0) Expected("hex digit");
              state.pushBack=true;
              break;
            }
            num = (num<<4) | (char.ToUpper(c)-'A'+10);
          }
          ret.Char = (char)num;
        }
        break;
      case 'c':
        c = ReadChar();
        if(!char.IsLetter(c)) Expected("letter");
        ret.Char = (char)(char.ToUpper(c)-64);
        break;
      case '\r': c=ReadChar(); if(c=='\n') ret.Valid=false; else state.pushBack=true; break;
      default: RaiseError(string.Format("Unknown escape character '{0}'", c)); break;
    }
    return ret;
  }
  #endregion

  Token NextToken()
  { // lineBeg should only be true if the last token was the first token on the line
    if(state.lineBeg && state.lastToken!=Token.EOF) state.lineBeg=false;
    if(state.newBlock) state.newBlock=false; // doesn't stay new for long!
    state.lastToken=state.token;
    return state.token=ReadToken();
  }

  // argument_list ::= [ <positional_args> ] <pcomma> [ <keyword_args> ] <pcomma>
  //                   [ '*' <expression> ] <pcomma> [ "**" <expression> ]
  // positional_args ::= <expression> ( ',' <expression> )*
  // keyword_args ::= <keyword_arg> ( ',' <keyword_arg> )*
  // keyword_arg ::= IDENTIFIER ':' <expression>
  // pcomma ::= [ ',' ] # comma required to separate arguments/parameters
  Node ParseArguments(Node container)
  { Node node=null;
    if(token!=Token.RParen)
      do
      { node = ParseExpression();
        if(token==Token.Colon) break;
        container.Add(node);
        if(token==Token.RParen) return container;
        Consume(Token.Comma);
      } while(token!=Token.Times && token!=Token.Power);
    if(token==Token.Colon)
      while(true)
      { if(node.Token!=Token.Identifier) Expected("identifier on the leftside of keyword arguments");
        Consume(Token.Colon);
        container.Add(new Node(Token.Pair, node, ParseExpression()));
        if(token==Token.RParen) return container;
        Consume(Token.Comma);
        if(token==Token.Times || token==Token.Power) break;
        node = ParseExpression();
      }
    if(token==Token.Times)
    { NextToken();
      container.Add(new Node(Token.Pair, new Node(Token.List), ParseExpression()));
      if(token==Token.RParen) return container;
      Consume(Token.Comma);
    }
    if(token==Token.Power)
    { NextToken();
      container.Add(new Node(Token.Pair, new Node(Token.Hash), ParseExpression()));
    }
    Expect(Token.RParen);
    return container;
  }

  Node ParseKeywordArgs(Node container)
  { while(true)
    { Node node = ParseIdentifier();
      Consume(Token.Colon);
      container.Add(new Node(Token.Pair, node, ParseExpression()));
      if(token!=Token.Comma) return container;
      NextToken();
    }
  }

  // array_list ::= [ <expression> ( ',' <expression> )* ]
  Node ParseArrayList()
  { Node node = new Node(Token.List);
    while(token!=Token.RBracket)
    { node.Add(ParseExpression());
      if(token==Token.Comma) NextToken();
    }
    return node;
  }

  // assign_expr ::= <comma_expr> [ <assign_op> <assign_expr> ]
  // assign_op   ::= '=' | ":=" | "+=" | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "^=" | "<<=" | ">>="
  Node ParseAssignment()
  { Node node = ParseTypeLow();
    if(token==Token.Assign)
    { for(int i=0; i<node.Count; i++) AssertLValue(node[i]);
      Assign assign = (Assign)value;
      NextToken();
      return new Node(Token.Assign, node, ParseAssignment());
    }
    return node;
  }

  // attribute_list ::= <attribute> ( ',' <attribute> )*
  // attribute ::= <fullident> [ '(' [ <keyword_args> ] ')' ]
  Node ParseAttributeList()
  { Node node = new Node(Token.With);
    while(true)
    { Node attr = ValueNode(Token.Attribute, ReadFullIdent());
      if(token==Token.LParen)
      { NextToken();
        if(token!=Token.RParen) ParseKeywordArgs(attr);
        Consume(Token.RParen);
      }
      node.Add(attr);
      if(token!=Token.Comma) return node;
      NextToken();
    }
  }

  // bitwise_expr ::= <shift_expr> ( <bitwise_op> <shift_expr> )*
  // bitwise_op   ::= '&' | '|' | '^'
  Node ParseBitwise()
  { Node node = ParseShift();
    while(true)
      switch(token)
      { case Token.BitAnd: case Token.BitOr: case Token.BitXor:
          NextToken(); node = new Node(lastToken, node, ParseShift()); break;
        default: return node;
      }
  }

  // block ::= ':' <statement_line> | NEWLINE STARTINDENT <statement_line>+ ENDINDENT
  // statement_line  ::= <statement> ( ';' <statement_line> )* EOL | <declaration> EOL
  // declaration ::= <var_decl> | <type_decl> | <function_decl> | <enum_decl> | <import_decl>
  Node ParseBlock()
  { Node node = new Node(Token.Block);
    bool inline = token==Token.Colon;
    if(inline)
    { NextToken();
      if(lineBeg) inline=false;
    }
    if(!inline && !NewIndent) Expected("Block");
    int level = Level;

    do
    { switch(token)
      { case Token.Def: node.Add(ParseDef(Access.None, Context.Function)); break;
        case Token.Class: case Token.Struct: case Token.Interface:
          node.Add(ParseTypeDecl(Access.None, Context.Function)); break;
        case Token.TypeName: case Token.With: node.Add(ParseDeclare(Access.None, Context.Function)); break;
        case Token.Enum: node.Add(ParseEnum(Access.None, Context.Function)); break;
        case Token.Delegate: node.Add(ParseDelegate(Access.None)); break;
        case Token.Import: node.Add(ParseImport()); break;
        default:
          Node decl = ParseDeclare(Access.None, Context.Function);
          if(decl==null)
          { do
            { node.Add(ParseStatement());
              if(token==Token.Semicolon) { if(lineBeg) RaiseError("Unexpected semicolon"); NextToken(); }
            } while(!lineBeg);
          }
          else node.Add(decl);
          break;
      }
    } while(!inline && level==Level || inline && !lineBeg);
    return node;
  }

  // compare_expr ::= <bitwise_expr> ( <compare_op> <bitwise_expr> )*
  // compare_op   ::= "==" | "!=" | '<' | '>' | "<=" | ">=" | "===" | "!=="
  Node ParseCompare()
  { Node node = ParseBitwise();
    while(token==Token.Compare)
    { Compare cmp = (Compare)value;
      NextToken();
      node = new Node(Token.Compare, node, ParseBitwise());
      node.Value = cmp;
    }
    return node;
  }

  /* var_decl ::= <var_attr>* <type> ["with" <attribute_list>] <idents> [ '=' <values> ]
     idents ::= IDENTIFIER ( ',' IDENTIFIER )*
     values ::= <expression> ( ',' <expression> )*
     var_attr ::= "static" | "const"
     <var_attr> disallowed in function context

     NOTE THAT THIS FUNCTION CAN RETURN NULL IF THE INPUT DOESN'T MATCH A VALID VARIABLE DECLARATION!
  */
  DeclNode ParseDeclare(Access access, Context context)
  { Node attrs=null;

    while(true)
    { if(token==Token.Static) access |= Access.Static;
      else if(token==Token.Const) access |= Access.Const;
      else break;
      if(context==Context.Function) UnexpectedToken();
      NextToken();
    }

    if(token!=Token.With && token!=Token.TypeName && token!=Token.Identifier) return null;
    int offset = state.offset;
    object type = ParseTypeBeforeWithIdent();
    if(lineBeg || state.offset==offset && token==Token.Identifier || token!=Token.Identifier && token!=Token.With)
      return null;
    if(token==Token.With) { NextToken(); attrs=ParseAttributeList(); }

    Node names = ParseNameList(), values = null;
    if(token==Token.Assign)
    { if((Assign)value!=Assign.Assign) UnexpectedToken();
      NextToken();
      values = ParseExpressionList();
    }

    return values==null ? new DeclNode(Token.Declare, access, type, null, names)
                        : new DeclNode(Token.Declare, access, type, null, names, values);
  }

  // decl_block ::= NEWLINE STARTINDENT <decl_block_line>+ ENDINDENT
  // decl_block_line ::= <var_decl> | <type_decl> | <function_decl> | <property_decl> |
  //                     <enum_decl> | <event_decl> | <delegate_decl> | <access_block>
  // access_block ::= <access_attr> ':' <decl_block>
  // access_attr  ::= "public" | "protected" | "private" | "internal" | "family"
  // <property_decl> and <event_decl> are only allowed within a type context
  Node ParseDeclBlock(Node container, Context context) { return ParseDeclBlock(container, context, Access.Private); }
  Node ParseDeclBlock(Node container, Context context, Access access)
  { bool inline = token==Token.Colon;

    if(inline)
    { NextToken();
      if(lineBeg) inline=false;
    }
    int level = Level;

    do
    { switch(token)
      { case Token.Access:
          Access acc = (Access)value;
          NextToken();
          ParseDeclBlock(container, context, acc);
          break;
        case Token.Def: container.Add(ParseDef(access, context)); break;
        case Token.Class: case Token.Struct: case Token.Interface:
          container.Add(ParseTypeDecl(access, context)); break;
        case Token.Enum: container.Add(ParseEnum(access, context)); break;
        case Token.Delegate: container.Add(ParseDelegate(access)); break;
        case Token.Prop: case Token.Event:
          if(context!=Context.Type) RaiseError(token.ToString()+" only allowed within type declarations");
          container.Add(token==Token.Prop ? ParseProperty(access) : ParseEvent(access));
          break;
        case Token.Package: container.Add(ParsePackage()); break;
        default:
          Node decl = ParseDeclare(access, context);
          if(decl==null) UnexpectedToken();
          container.Add(decl);
          break;
      }
    } while(!inline && level==Level || inline && !lineBeg);

    return container;
  }

  // def_expr ::= "def" <def_attr>* [ <return_type> ] [ "with" <attribute_list> ]
  //              IDENTIFIER '(' <parameter_list> ')' ( <block> | ';' )
  // def_attr  ::= "static" | "virtual" | "override" | "new"
  // <def_attr> disallowed in function context
  // block-less functions only allowed inside type declarations
  DeclNode ParseDef(Access access, Context context)
  { Consume(Token.Def);

    while(true)
    { if(token==Token.Static) access |= Access.Static;
      else if(token==Token.Virtual) access |= Access.Virtual;
      else if(token==Token.Override) access |= Access.Override;
      else if(token==Token.Abstract) access |= Access.Abstract;
      else if(token==Token.New) access |= Access.New;
      else break;
      if(context==Context.Function) UnexpectedToken();
      NextToken();
    }

    string name;
    Node   attrs = null, parms;
    object type  = ParseTypeBeforeWithIdent(true);
    if(token==Token.With) { NextToken(); attrs=ParseAttributeList(); }
    name = ReadIdentifier();
    Consume(Token.LParen);
    parms = ParseParameters(new Node(Token.List), Token.RParen);
    Consume(Token.RParen);
    Node block;
    if(token==Token.Semicolon)
    { if(context!=Context.Type) RaiseError("Body-less functions can only occur within type declarations");
      NextToken(); block=null;
    }
    else block = ParseBlock();
    DeclNode node = new DeclNode(Token.Def, access, type, name, attrs, parms, block);
    return node;
  }

  // delegate_decl ::= "delegate" [<type>] [ "with" <attribute_list> ] IDENTIFIER '(' [<parameter_list>] ')'
  DeclNode ParseDelegate(Access access)
  { Consume(Token.Delegate);
    
    Node attrs=null;
    object type = ParseTypeBeforeWithIdent();
    if(token==Token.With) { NextToken(); attrs=ParseAttributeList(); }
    DeclNode node = new DeclNode(Token.Delegate, access, type, ReadIdentifier());
    Consume(Token.LParen);
    ParseParameters(node, Token.RParen);
    Consume(Token.RParen);
    return node;
  }

  // enum_decl ::= "enum" [<integral_type>] [ "with" <attribute_list> ] IDENTIFIER <enum_block>
  // enum_block ::= ( NEWLINE STARTINDENT | ':' ) [<enum_items>]
  // enum_items ::= ( <enum_item> [','] )*
  // enum_item ::= IDENTIFIER [ '=' <expression> ]
  DeclNode ParseEnum(Access access, Context context)
  { Consume(Token.Enum);

    Node attrs = null;
    object type = null;
    if(token==Token.TypeName)
    { BoaType t = (BoaType)value;
      if(t<BoaType.IntegralStart || t>BoaType.IntegralEnd) RaiseError("Enums must be of an integral type");
      type = t;
      NextToken();
    }
    if(token==Token.With) { NextToken(); attrs = ParseAttributeList(); }
    DeclNode node = new DeclNode(Token.Enum, access, type, ReadIdentifier(), attrs);

    bool inline = token==Token.Colon;
    if(inline)
    { NextToken();
      if(lineBeg) inline=false;
    }
    else if(!NewIndent) return node;

    while(!inline || inline && !lineBeg)
    { Node item = ParseIdentifier();
      if(token==Token.Assign)
      { if((Assign)value!=Assign.Assign) UnexpectedToken();
        NextToken();
        item = new Node(Token.Pair, item, ParseExpression());
      }
      node.Add(item);
      if(token!=Token.Comma) break;
      NextToken();
    }
    return node;
  }

  // event_decl ::= "event" [ "static" ] <type> [ "with" <attribute_list> ] IDENTIFIER [<event_block>]
  // event_block ::= ':' <event_changers> | NEWLINE BEGININDENT <event_changers> ENDINDENT
  // event_changers ::= <add_changer> [ <rem_changer> ] | <rem_changer> [ <add_changer> ]
  // add_changer ::= "add" <block>
  // rem_changer ::= "rem" <block>
  DeclNode ParseEvent(Access access)
  { Consume(Token.Event);
    if(token==Token.Static) { NextToken(); access |= Access.Static; }

    Node attrs=null;
    object type = ParseType(false);
    if(token==Token.With) { NextToken(); attrs=ParseAttributeList(); }
    DeclNode node = new DeclNode(Token.Event, access, type, ReadIdentifier(), attrs);
    if(token==Token.Colon || (NewIndent && token==Token.Identifier))
    { bool inline = token==Token.Colon;
      if(inline)
      { NextToken();
        if(lineBeg) inline=false;
      }
      int level = Level;
      do
      { string acc = value as string;
        if(token!=Token.Identifier || acc!="add" && acc!="rem") Expected("'add' or 'rem' event block");
        NextToken();
        node.Add(new Node(acc=="add" ? Token.EventAdd : Token.EventRemove, ParseBlock()));
      } while(!inline && level==Level || inline && !lineBeg);
    }
    return node;
  }

  // expression ::= <lowlogand_expr> ( "or" <lowlogand_expr> )*
  Node ParseExpression() { return ParseExpression(false); }
  Node ParseExpression(bool assertNotEnd) // ParseLowLogOr()
  { if(assertNotEnd && StatementEnd) Expected("statement");
    Node node = ParseLowLogAnd();
    while(token==Token.Or) { NextToken(); node = new Node(Token.LogOr, node, ParseLowLogAnd()); }
    return node;
  }

  // expression_list ::= <expression> ( ',' <expression> )*
  Node ParseExpressionList() { return ParseExpressionList(new Node(Token.List)); }
  Node ParseExpressionList(Node node)
  { node.Add(ParseExpression());
    while(token==Token.Comma)
    { NextToken();
      node.Add(ParseExpression());
    }
    return node;
  }

  // hash_list ::= [ <hash_item> ( ',' <hash_item> )* ]
  // hash_item ::= <expression> ':' <expression>
  Node ParseHashList()
  { Node node = new Node(Token.Hash);
    while(token!=Token.RBrace)
    { Node item = new Node(Token.Pair, ParseExpression());
      Consume(Token.Colon);
      item.Add(ParseExpression());
      node.Add(item);
      if(token==Token.Comma) NextToken();
    }
    return node;
  }

  // factor_expr ::= <power_expr> ( <factor_op> <power_expr> )*
  // factor_op   ::= '*' | '/' | '%' | "//"
  Node ParseFactor()
  { Node node = ParsePower();
    while(true)
      switch(token)
      { case Token.Times: case Token.Divide: case Token.Percent: case Token.FloorDiv:
          NextToken(); node = new Node(lastToken, node, ParsePower()); break;
        default: return node;
      }
  }

  // fullident ::= IDENTIFIER ( '.' IDENTIFIER )*
  Node ParseFullIdent() { return IdentifierNode(ReadFullIdent()); }

  // fullident_list ::= <fullident> ( ',' <fullident> )*
  Node ParseFullIdentList()
  { Node node = new Node(Token.List);
    while(true)
    { node.Add(ParseFullIdent());
      if(token!=Token.Comma) return node;
      NextToken();
    }
  }

  // IDENTIFIER
  Node ParseIdentifier() { return IdentifierNode(ReadIdentifier()); }

  // import_decl ::= "import" <fullident> [ '=' IDENTIFIER ]
  ImportNode ParseImport()
  { Consume(Token.Import);
    if(lineBeg) Expected("identifier");
    ImportNode node = new ImportNode(ReadFullIdent());
    if(token==Token.Assign)
    { if((Assign)value!=Assign.Assign) UnexpectedToken();
      NextToken();
      node.Alias = ReadIdentifier();
    }
    return node;
  }

  // incdec_expr ::= <incdec_op> <member_expr> | <member_expr> [ <incdec_op> ]
  // incdec_op   ::= "++" | "--"
  Node ParseIncDec()
  { if(token==Token.Increment || token==Token.Decrement)
    { NextToken();
      return new Node(lastToken, AssertLValue(ParseMember()));
    }
    Node node = ParseMember();
    if(token==Token.Increment) { NextToken(); node = new Node(Token.PostIncrement, AssertLValue(node)); }
    else if(token==Token.Decrement) { NextToken(); node = new Node(Token.PostDecrement, AssertLValue(node)); }
    return node;
  }

  // index ::= <slice> ( ',' <slice> )*
  Node ParseIndex(Node index)
  { Node node = ParseSlice();
    while(token==Token.Comma)
    { index.Add(node);
      Consume(Token.Comma);
      node = ParseSlice();
    }
    index.Add(node);
    return index;
  }

  // lambda_expr ::= "lambda" [<type>] '(' [<parameter_list>] ')' ':' <expression>
  DeclNode ParseLambda()
  { Consume(Token.Lambda);
    Node parms = new Node(Token.List);
    object type = token==Token.LParen ? BoaType.Var : ParseType(true);
    Consume(Token.LParen);
    if(token!=Token.RParen) ParseParameters(parms, Token.RParen);
    Consume(Token.RParen);
    Consume(Token.Colon);
    return new DeclNode(Token.Lambda, Access.None, type, null, parms, ParseExpression());
  }

  // logand_expr ::= <compare_expr> ( "&&" <compare_expr> )*
  Node ParseLogAnd()
  { Node node = ParseCompare();
    while(token==Token.LogAnd) { NextToken(); node = new Node(Token.LogAnd, node, ParseCompare()); }
    return node;
  }

  // logor_expr ::= <logand_expr> ( "||" <logand_expr> )*
  Node ParseLogOr()
  { Node node = ParseLogAnd();
    while(token==Token.LogOr) { NextToken(); node = new Node(Token.LogOr, node, ParseLogAnd()); }
    return node;
  }

  // lowlogand_expr ::= <lowlognot_expr> ( "and" <lowlognot_expr> )*
  Node ParseLowLogAnd()
  { Node node = ParseLowLogNot();
    while(token==Token.And) { NextToken(); node = new Node(Token.LogAnd, node, ParseLowLogNot()); }
    return node;
  }
  
  // lowlognot_expr ::= "not" <lowlognot_expr> | <assign_expr>
  Node ParseLowLogNot()
  { if(token==Token.Not) { NextToken(); return new Node(Token.LogNot, ParseLowLogNot()); }
    else return ParseAssignment();
  }

  // match_expr ::= <factor_expr> ( <match_op> <regex> )*
  // match_op   ::= "=~" | "!~"
  Node ParseMatch()
  { Node node = ParseFactor();
    if(token==Token.Match || token==Token.NotMatch)
    { Token t = token;
      RegexNode regex = ParseRegex();
      if(regex.Replace!=null)
      { if(t==Token.NotMatch) RaiseError("Cannot combine !~ with s///");
        node = new Node(Token.Replace, node, regex);
      }
      else node = new Node(t, node, regex);
    }
    return node;
  }

  // member_expr ::= <typehigh_expr> <member_access>* | <builtin_vtype> '(' <expression> ')
  // member_access ::= '.' LITERAL              |
  //                   '(' <argument_list> ')'  |
  //                   '[' <index> ']'          |
  Node ParseMember()
  { if(token==Token.TypeName)
    { Node node = ValueNode(Token.Cast, ParseType(false));
      Consume(Token.LParen);
      node.Add(ParseExpression());
      Consume(Token.RParen);
      return node;
    }
    else
    { Node node = ParseTypeHigh();
      if(lineBeg) return node;
      while(true)
        switch(token)
        { case Token.Period:
            ExpectNext(Token.Identifier); node=new Node(Token.Member, node); node.Value=value; NextToken(); break;
          case Token.LParen:
            NextToken(); node = ParseArguments(new Node(Token.Call, node)); Consume(Token.RParen); break;
          case Token.LBracket:
            NextToken(); node = ParseIndex(new Node(Token.Index, node)); Consume(Token.RBracket); break;
          default: return node;
        }
    }
  }

  // name_list ::= IDENTIFIER ( ',' IDENTIFIER )*
  Node ParseNameList()
  { ArrayList names = new ArrayList();
    names.Add(value);
    Consume(Token.Identifier);
    while(token==Token.Comma)
    { ExpectNext(Token.Identifier);
      names.Add(value);
      NextToken();
    }
    return LiteralNode(names.ToArray(typeof(string)));
  }

  // package ::= "package" <fullident> <decl_block>
  Node ParsePackage()
  { Consume(Token.Package);
    Node node = ValueNode(Token.Package, ReadFullIdent());
    return ParseDeclBlock(node, Context.Package);
  }

  // parameter_list ::= [ <positional_params> ] <pcomma> [ <keyword_params> ] <pcomma>
  //                    [ '*' [<type>] IDENTIFIER ] <pcomma> [ "**" [<type>] IDENTIFIER ]
  // positional_params ::= <positional_param> ( ',' <positional_param> )*
  // positional_param  ::= [ "ref" ] [ <type> ] [ "with" <attribute_list> ] IDENTIFIER
  // keyword_params ::= <keyword_param> ( ',' <keyword_param> )*
  // keyword_param ::= [ <type> ] [ "with" <attribute_list> ] IDENTIFIER '=' <expression>
  // pcomma ::= [ ',' ] # comma required to separate arguments/parameters
  Node ParseParameters(Node container, Token stop)
  { ParamNode node=null;
    if(token!=Token.Times && token!=Token.Power && token!=stop)
      do
      { bool isref=false;
        if(token==Token.Ref) { isref=true; NextToken(); }
        node = new ParamNode(isref, ParseTypeBeforeWithIdent(), ReadIdentifier());
        if(token==Token.Assign)
        { if(isref) RaiseError("'ref' not allowed on keyword parameters");
          node.Token=Token.Pair;
          break;
        }
        container.Add(node);
        if(token==stop) return container;
        Consume(Token.Comma);
      } while(token!=Token.Times && token!=Token.Power);
    if(token==Token.Assign)
      while(true)
      { if((Assign)value!=Assign.Assign) UnexpectedToken();
        Consume(Token.Assign);
        node.Add(ParseExpression());
        container.Add(node);
        if(token==stop) return container;
        Consume(Token.Comma);
        if(token==Token.Times || token==Token.Power) break;
        node = new ParamNode(Token.Pair, ParseTypeBeforeWithIdent(), ReadIdentifier());
        Expect(Token.Assign);
      }
    if(token==Token.Times)
    { NextToken();
      container.Add(new ParamNode(Token.List, ParseTypeBeforeWithIdent(), ReadIdentifier()));
      if(token==stop) return container;
      Consume(Token.Comma);
    }
    if(token==Token.Power)
    { NextToken();
      container.Add(new ParamNode(Token.Hash, ParseTypeBeforeWithIdent(), ReadIdentifier()));
    }
    Expect(stop);
    return container;
  }

  // power_expr ::= <unary_expr> ( "**" <power_expr> )*
  Node ParsePower()
  { Node node = ParseUnary();
    if(token==Token.Power) { NextToken(); node = new Node(Token.Power, node, ParsePower()); }
    return node;
  }

  /* primary_expr ::= LITERAL | "self" | <fullident> |
                      '(' <expression> ')' |
                      '[' <array_list> ']' |
                      '{' <hash_list>  '}' |
                      <tuple>
     tuple ::= '(' ( <expression> ',' )* ')'
  */
  Node ParsePrimary()
  { Node node;
    switch(token)
    { case Token.Literal: node = LiteralNode(value); break;
      case Token.Self: node = new Node(Token.Self); break;
      case Token.Identifier: node = ParseFullIdent(); return node;
      case Token.LParen:
        NextToken();
        node = ParseExpression();
        if(token==Token.Comma) 
        { NextToken();
          node = new Node(Token.Tuple, node);
          while(token!=Token.RParen)
          { node.Add(ParseExpression());
            if(token==Token.Comma) NextToken();
          }
        }
        Expect(Token.RParen);
        break;
      case Token.LBracket: NextToken(); node = ParseArrayList(); break;
      case Token.LBrace:   NextToken(); node = ParseHashList();  break;
      default: UnexpectedToken(); return null;
    }
    NextToken();
    return node;
  }

  // property_decl ::= "prop" <def_attr>* [ "with" <attribute_list> ] [ <type> ] IDENTIFIER <prop_block>
  // prop_block ::= ':' <prop_accessors> | NEWLINE STARTINDENT <prop_accessors> ENDINDENT
  // prop_accessors ::= <get_accessor> [ <set_accessor> ] | <set_accessor> [ <get_accessor> ]
  // get_accessor ::= "get" ( <block> | ';' )
  // set_accessor ::= "set" ( <block> | ';' )
  DeclNode ParseProperty(Access access)
  { Consume(Token.Prop);

    while(true)
    { if(token==Token.Static) access |= Access.Static;
      else if(token==Token.Virtual) access |= Access.Virtual;
      else if(token==Token.Override) access |= Access.Override;
      else if(token==Token.Abstract) access |= Access.Abstract;
      else if(token==Token.New) access |= Access.New;
      else break;
      NextToken();
    }
    
    Node attrs=null;
    if(token==Token.With) { NextToken(); attrs=ParseAttributeList(); }
    DeclNode node = new DeclNode(Token.Prop, access, ParseType(false), ReadIdentifier(), attrs);
    if(token==Token.Colon || (NewIndent && token==Token.Identifier))
    { bool inline = token==Token.Colon;
      if(inline)
      { NextToken();
        if(lineBeg) inline=false;
      }
      int level = Level;
      do
      { string type = value as string;
        if(token!=Token.Identifier || type!="get" && type!="set") Expected("'get' or 'set' property block");
        NextToken();
        Node block;
        if(token==Token.Semicolon) { NextToken(); block=null; }
        else block = ParseBlock();
        node.Add(new Node(type=="get" ? Token.Get : Token.Set, block));
      } while(!inline && level==Level || inline && !lineBeg);
    }
    return node;
  }

  // regex ::= [ 'm' ] <regex_delim> <regex_pattern> <regex_delim> <regex_flag>* |
  //           '/' <regex_pattern> '/' <regex_flag>* |
  //           's' <regex_delim> <regex_pattern> <regex_delim> <replacement> <regex_delim> <regex_flag>*
  // regex_delim ::= CHARACTER
  // regex_flag  ::= 'g' | 'i' | 'm' | 's' | 'o' | 'x' | 'p' | 'c' | 'X' | 'C'
  // SECOND AND THIRD <regex_delim> MUST MATCH FIRST ONE
  RegexNode ParseRegex()
  { char c = ReadChar();

    bool linebeg = state.lineBeg;
    while(true)
    { if(c=='\n') state.NewLine();
      else if(!char.IsWhiteSpace(c)) break;
      c = ReadChar();
    }
    if(c!='/' && c!='s' && c!='m') RaiseError("Unexpected character "+c);

    char delim;
    bool repl = c=='s';
    if(repl || c=='m') c = ReadChar();
    if(char.IsWhiteSpace(c)) RaiseError("Unexpected character "+c);
    delim = c;

    string match=ReadRegexTo(delim), replace=repl ? ReadRegexTo(delim) : null, flags=ReadRegexFlags();
    Node repnode=null;

    try { new System.Text.RegularExpressions.Regex(match); }
    catch(ArgumentException e) { RaiseError(string.Format("Invalid regular expression. {0}", e.Message)); }

    if(repl && replace!="" && flags.IndexOf('x') != -1)
      try { repnode = new Parser(replace).ParseExpression(); }
      catch(CompilerErrorException e) { RaiseError(e.Message); }
    else if(repl) repnode = LiteralNode(replace);
    
    state.lineBeg = linebeg;
    NextToken();
    return new RegexNode(match, repnode, flags);
  }
  
  // shift_expr ::= <term_expr> ( <shift_op> <term_expr> )*
  // shift_op   ::= "<<" | ">>"
  Node ParseShift()
  { Node node = ParseTerm();
    while(true)
      switch(token)
      { case Token.LeftShift: case Token.RightShift: NextToken(); node = new Node(lastToken, node, ParseTerm()); break;
        default: return node;
      }
  }

  // slice ::= <expression> [ ':' <expression> [ ':' <expression> ] ]
  Node ParseSlice()
  { Node node = ParseExpression();
    if(token==Token.Colon)
    { NextToken();
      Node expr;
      if(token==Token.RBracket) { expr=null; }
      else if(token==Token.Colon) { NextToken(); expr=null; }
      else expr = ParseExpression();
      node = new Node(Token.Slice, node, expr);
      if(token==Token.Colon) { NextToken(); node.Add(ParseExpression()); }
    }
    return node;
  }

  /* statement ::= <if_stmt> | <for_stmt> | <while_stmt> | <do_stmt> | <try_block> | <print_stmt> |
                   <return_stmt> | <raise_stmt> | <yield_stmt> | <label_stmt> | <goto_stmt> | <lock_stmt> |
                   <switch_stmt> |<exec_stmt> | "pass" | "break" | "continue" | <expression>
  
     if_stmt ::= "if" <expression> <block> ( "elif" <expression> <block> )* [ "else" <block> ]
     for_stmt ::= "for" [ "sticky" ] [<type>] <name_list> "in" <expression> <block> |
                  "for" '(' [ <expression> ] ';' [ <expression> ] ';' [ <expression> ] ')' <block>
     while_stmt ::= "while" <expression> <block>
     do_stmt ::= "do" <block> "while" <expression>
     try_block ::= "try" <block> ( "except" <type> [ IDENTIFIER ] <block> )* [ "finally" <block> ]
     type_list ::= <type> ( ',' <type> )*
     print_stmt ::= "print" <expression_list> [ ',' ] |
                        "print" ">>" <expression> [ ',' <expression_list> [ ',' ] ]
     return_stmt ::= "return" [ <expression> ]
     raise_stmt ::= "raise" [ <expression> ]
     yield_stmt ::= "yield" <expression>
     label_stmt ::= "label" IDENTIFIER
     goto_stmt  ::= "goto" IDENTIFIER
     lock_stmt  ::= "lock" <expression> <block>
     switch_stmt ::= "switch" <expression> NEWLINE BEGININDENT <switch_case>+ ENDINDENT
     switch_case ::= "case" <expression_list> <block> | "else" <block>
     exec_stmt ::= "exec" <expression> [ "in" <expression> [ ',' <expression> ] ]
  */
  Node ParseStatement()
  { Node node=null;
    int level;
    switch(token)
    { case Token.If:
        NextToken();
        node = new Node(Token.If, ParseExpression(true), ParseBlock());
        if(token==Token.Elif) { state.token=Token.If; node.Add(ParseStatement()); }
        else if(token==Token.Else) { NextToken(); node.Add(ParseBlock()); }
        break;

      case Token.For:
        NextToken();
        if(token==Token.LParen) // c-style for
        { node = new Node(Token.CFor);
          for(int i=0; i<2; i++)
            if(token==Token.Semicolon) { NextToken(); node.Add(null); }
            else node.Add(ParseExpression());
          node.Add(token==Token.RParen ? null : ParseExpression());
          Consume(Token.RParen);
          node.Add(ParseBlock());
        }
        else
        { bool sticky=false;
          if(token==Token.Identifier && (string)value=="sticky") { sticky=true; NextToken(); }
          object type = ParseTypeBeforeWithIdent();
          if(token==Token.With) UnexpectedToken();
          node = ParseNameList();
          node.Type = type;
          Consume(Token.In);
          node = new Node(Token.For, node, ParseExpression(true), ParseBlock());
          node.Value = sticky;
        }
        break;

      case Token.While:
        NextToken();
        node = ParseExpression(true);
        node = new Node(Token.While, node, ParseBlock());
        break;

      case Token.Do:
        NextToken();
        node = ParseBlock();
        Consume(Token.While);
        node = new Node(Token.Do, node, ParseExpression(true));
        break;

      case Token.Try:
        level = Level;
        NextToken();
        node = new Node(Token.Try, ParseBlock());
        while(token==Token.Except && Level==level)
        { NextToken();
          Node type=null;
          if(!NewBlock)
          { type = ParseFullIdent();
            if(token==Token.Identifier && !NewBlock) type = new Node(Token.Pair, ParseIdentifier(), type);
          }
          node.Add(new Node(Token.Except, type, ParseBlock()));
        }
        if(token==Token.Finally && Level==level)
        { NextToken();
          node.Add(new Node(Token.Finally, ParseBlock()));
        }
        break;

      case Token.Print:
        NextToken();
        Node dest=null;
        bool lastComma=false;
        if(token==Token.RightShift)
        { NextToken();
          dest = ParseExpression();
          if(token==Token.Comma) { NextToken(); lastComma=true; }
        }
        node = new Node(Token.List);
        while(!StatementEnd)
        { node.Add(ParseExpression(true));
          lastComma=false;
          if(token==Token.Comma) { NextToken(); lastComma=true; }
        }
        node = dest==null ? new Node(Token.Print, node) : new Node(Token.Print, node, dest);
        node.Value = lastComma;
        break;

      case Token.Return: case Token.Raise:
        NextToken();
        node = StatementEnd ? new Node(lastToken) : new Node(lastToken, ParseExpression());
        break;

      case Token.Switch:
        NextToken();
        AssertNotNL();
        node = new Node(Token.Switch, ParseExpression());
        if(!NewIndent || token!=Token.Case) Expected("switch block");
        level = Level;
        do
        { NextToken();
          node.Add(new Node(Token.Case, ParseExpressionList(), ParseBlock()));
        } while(token==Token.Identifier && Level==level && (string)value=="case");
        break;

      case Token.Goto: case Token.Label:
        ExpectNext(Token.Identifier);
        node = ValueNode(lastToken, value);
        NextToken();
        break;

      case Token.Lock:
        NextToken();
        node = new Node(Token.Lock, ParseExpression(), ParseBlock());
        break;

      case Token.Yield:
        NextToken();
        node = new Node(Token.Yield, ParseExpression(true));
        break;

      case Token.Exec:
        NextToken();
        node = new Node(Token.Exec, ParseExpression(true));
        if(token==Token.In)
        { NextToken(); node.Add(ParseExpression());
          if(token==Token.Comma) { NextToken(); node.Add(ParseExpression()); }
        }
        break;

      case Token.Pass: case Token.Break: case Token.Continue: node=new Node(lastToken); NextToken(); break;
      default: node = ParseExpression(); break;
    }
    return node;
  }

  // term_expr ::= <match_expr> ( <term_op> <match_expr> )*
  // term_op   ::= '+' | '-'
  Node ParseTerm()
  { Node node = ParseMatch();
    while(true)
      switch(token)
      { case Token.Plus: case Token.Minus: NextToken(); node = new Node(lastToken, node, ParseMatch()); break;
        default: return node;
      }
  }

  // ternary_expr ::= <logor_expr> [ '?' <expression> ':' <expression> ]
  Node ParseTernary()
  { Node node = ParseLogOr();
    if(token==Token.Question)
    { NextToken();
      Node exp = ParseExpression();
      Consume(Token.Colon);
      node = new Node(Token.Question, node, exp, ParseExpression());
    }
    return node;
  }

  // type ::= ( <builtin_vtype> | <fullident> ) ( '[' ','* ']' )*
  // builtin_type ::= <builtin_vtype> | "void"
  // builtin_vtype ::= <integral_type> | "var" | "char" | "float" | "double" | "func"  | "hash" | "list" | "string"
  // integral_type ::= "byte" | "sbyte" | "short" | "ushort" | "int"  | "uint" | "long" | "ulong"
  object ParseType(bool allowVoid)
  { object type;
    if(token==Token.TypeName)
    { if(!allowVoid && (BoaType)value==BoaType.Void) RaiseError("Void type disallowed here");
      type = value;
      NextToken();
    }
    else type = ReadFullIdent();
    if(token==Token.LBracket)
    { int num=0, max=-1;
      object otype = type;
      CreateRestorePoint();
      while((max==-1 || num<max) && token==Token.LBracket)
      { ArrayOf ao = new ArrayOf(type);
        NextToken();
        while(token==Token.Comma) { ao.Dimensions++; NextToken(); }
        if(token!=Token.RBracket)
        { Restore(); type=otype; max=num;
          if(num==0) break;
          num=0; ao.Dimensions=1; continue;
        }
        Consume(Token.RBracket);
        type = ao; num++;
      }
      if(max==-1) DropRestorePoint();
    }
    return type;
  }

  // [<type>] [ "with" <attribute_list> ] IDENTIFIER
  object ParseTypeBeforeWithIdent() { return ParseTypeBeforeWithIdent(false); }
  object ParseTypeBeforeWithIdent(bool allowVoid)
  { if(token==Token.With) return BoaType.Var;

    bool rp = token!=Token.TypeName;
    if(rp) CreateRestorePoint();
    object type = ParseType(allowVoid);
    if(token!=Token.Identifier && token!=Token.With) { Restore(); type=BoaType.Var; }
    else if(rp) DropRestorePoint();
    return type;
  }

  /* type_decl  ::= <type_type> <type_attr>* [ "with" <attribute_list> ] IDENTIFIER
                [ '(' <fullident_list> ')' ] <decl_block>
     type_type  ::= "class" | "struct" | "interface"
     type_attr  ::= "static" | "sealed" | "abstract"
     <type_attr> disallowed in function context
  */
  DeclNode ParseTypeDecl(Access access, Context context)
  { Token type = token;
    NextToken();

    while(true)
    { if(token==Token.Static) access |= Access.Static;
      else if(token==Token.Sealed) access |= Access.Sealed;
      else if(token==Token.Abstract)
      { if(type!=Token.Class) RaiseError("Only classes can be abstract");
        access |= Access.Abstract;
      }
      else break;
      if(context==Context.Function) UnexpectedToken();
      NextToken();
    }

    Node attrs=null, bases=null;
    if(token==Token.With) { NextToken(); attrs=ParseAttributeList(); }
    string name = ReadIdentifier();
    if(token==Token.LParen && !lineBeg)
    { NextToken();
      if(token!=Token.RParen) bases=ParseFullIdentList();
      Consume(Token.RParen);
    }

    DeclNode node = new DeclNode(type, access, null, name, attrs);
    return token==Token.Colon || NewIndent ? (DeclNode)ParseDeclBlock(node, Context.Type) : node;
  }

  // typehigh_expr ::= "typeof" <typehigh_expr> | <lambda_expr> |
  //                   "new" <type> '(' [ <argument_list> ] ')' |
  //                   "new" <type> '[' <expression_list> ']'
  Node ParseTypeHigh()
  { Node node = null;
    if(token==Token.New)
    { NextToken();
      node = new Node(Token.New);
      node.Type = ParseType(false);
      if(token==Token.LParen)
      { if(node.Type is ArrayOf) RaiseError("Can't call an array!");
        NextToken();
        ParseArguments(node);
        Consume(Token.RParen);
      }
      else if(token==Token.LBracket)
      { ArrayOf ao = new ArrayOf(node.Type);
        node.Type = ao;
        NextToken();
        ParseExpressionList(node);
        Consume(Token.RBracket);
        ao.Dimensions=node.Count;
      }
      else RaiseError("Invalid 'new' usage");
      return node;
    }
    else if(token==Token.Typeof) { NextToken(); node = new Node(Token.Typeof, ParseTypeHigh()); }
    else if(token==Token.Lambda) node = ParseLambda();
    else node = ParsePrimary();
    return node;
  }

  // typelow_expr ::= <ternary_expr> [ <typelow_ops> ]
  // typelow_ops  ::= "in" <ternary_expr> | ( "as" | "is" ) <fullident>
  Node ParseTypeLow()
  { Node node = ParseTernary();
    while(true)
      switch(token)
      { case Token.In: NextToken(); node = new Node(Token.In, node, ParseTernary()); break;
        case Token.As: case Token.Is: NextToken(); node = new Node(lastToken, node, ParseFullIdent()); break;
        default: return node;
      }
  }

  // unary_expr ::= <unary_op> <unary_expr> | [ "ref" ] <incdec_expr>
  // unary_op   ::= '!' | '~' | '-' | '+'
  Node ParseUnary()
  { if(token==Token.LogNot || token==Token.Minus || token==Token.BitNot || token==Token.Plus)
    { NextToken();
      return new Node(lastToken, ParseUnary());
    }
    else if(token==Token.Ref)
    { NextToken();
      return new Node(Token.Ref, AssertLValue(ParseIncDec()));
    }
    return ParseIncDec();
  }

  // unit ::= <package>* | <decl_block_line>+
  Node ParseUnit()
  { NextToken();
    if(token==Token.EOF) return null;

    Node node=new Node(Token.Unit), app=null;
    do
    { if(token==Token.Package) node.Add(ParsePackage());
      else if(token==Token.Import) node.Add(ParseImport());
      else
      { if(app==null) node.Add(app = ValueNode(Token.Package, "__App"));
        ParseDeclBlock(app, Context.Package);
      }
    } while(token!=Token.EOF);
    return node;
  }

  void RaiseError(string message)
  { throw new CompilerErrorException(file, state.curLine, state.curChar, "BP0", message);
  }

  char ReadChar()
  { state.lastRead = state.read;
    state.read     = (state.offset==text.Length ? -1 : text[state.offset++]);
    state.curChar++;
    return (char)state.read;
  }

  string ReadFullIdent()
  { string name = ReadIdentifier();
    while(token==Token.Period)
    { ExpectNext(Token.Identifier);
      name += '.';
      name += (string)value;
      NextToken();
    }
    return name;
  }

  string ReadIdentifier()
  { Expect(Token.Identifier);
    string ident = (string)value;
    NextToken();
    return ident;
  }

  string ReadRegexFlags()
  { string str=string.Empty;
    while(true)
    { char c = ReadChar();
      switch(c)
      { case 'g': case 'i': case 'm': case 's': case 'o': case 'x': case 'p': case 'c': case 'X': case 'C':
          str += c; break;
        default: state.pushBack=true; return str;
      }
    }
  }

  string ReadRegexTo(char delim)
  { string str=string.Empty;
    bool linebeg=state.lineBeg;
    while(true)
    { char c = ReadChar();
      if(c=='\\')
      { c = ReadChar();
        if(c==delim) { str += delim; continue; }
        else str += '\\';
      }
      if(c==delim) break;
      if(c=='\n') state.NewLine();
      str += c;
    }
    state.lineBeg = linebeg;
    return str;
  }

  #region ReadToken
  Token ReadToken()
  { if(token==Token.EOF) return token;
    char c;
    while(true)
    { if(lineBeg)
      { int space=0;
        while(true)
        { c=ReadChar();
          if(c=='\r') continue;
          if(c=='\n') { state.NewLine(); space=0; continue; }
          if(!char.IsWhiteSpace(c)) break;
          space++;
        }
        state.pushBack = true;
        if(space>Level)
        { state.indent.Push(space);
          state.newBlock=true;
        }
        else if(space<Level)
        { state.newBlock=false;
          do state.indent.Pop(); while(space<Level);
        }
      }

      if(state.pushBack) state.pushBack=false;
      else ReadChar();
      if(state.read==-1)
      { state.lineBeg=true; state.newBlock=false;
        state.indent.Clear(); state.indent.Push(-1);
        return Token.EOF;
      }
      c = (char)state.read;
      if(c!='\n' && char.IsWhiteSpace(c)) continue;

      if(char.IsDigit(c)) // TODO: support long (arbitrary length) integers, scientific notation, and complex numbers
      { string s = string.Empty; 
        bool period = false;
        while(true)
        { if(c=='.')
          { if(period) RaiseError("Invalid number");
            period=true;
          }
          else if(!char.IsDigit(c)) break;
          s += c;
          c = ReadChar();
        }
        try
        { if(char.ToUpper(c)=='F') state.value = float.Parse(s);
          else if(char.ToUpper(c)=='L') // TODO: support long (arbitrary length) integers
          { throw new NotImplementedException();
          }
          else
          { state.pushBack=true;
            state.value = period ? (object)double.Parse(s)
                                 : s.Length==10 && s.CompareTo(int.MaxValue.ToString())>0 ? (object)long.Parse(s)
                                                                                          : (object)int.Parse(s);
          }
        }
        catch(FormatException) { RaiseError("Invalid number"); }
        return Token.Literal;
      }
      else if(c=='_' || char.IsLetter(c))
      { string s = string.Empty;
        do
        { s += c;
          c = ReadChar();
        } while(c=='_' || char.IsLetterOrDigit(c));
        state.pushBack = true;
        if(s=="null")  { state.value=null;  return Token.Literal; }
        if(s=="true")  { state.value=true;  return Token.Literal; }
        if(s=="false") { state.value=false; return Token.Literal; }
        if(s=="public")    { state.value=Access.Public;    return Token.Access; }
        if(s=="protected") { state.value=Access.Protected; return Token.Access; }
        if(s=="private")   { state.value=Access.Private;   return Token.Access; }
        if(s=="internal")  { state.value=Access.Internal;  return Token.Access; }
        if(s=="family")    { state.value=Access.Family;    return Token.Access; }
        state.value = stringTokens[s];
        if(value!=null) return (Token)value;
        state.value = typeTokens[s];
        if(value!=null) return Token.TypeName;

        if(s=="r" && (c=='\"' || c=='\''))
        { char delim = c;
          bool linebeg = state.lineBeg;
          state.pushBack=false;
          s = string.Empty;
          while(true)
          { c = ReadChar();
            if(c=='\n') state.NewLine();
            else if(c==delim) break;
            s += c;
          }
          state.value = s;
          state.lineBeg = linebeg;
          return Token.Literal;
        }

        state.value = s;
        return Token.Identifier;
      }
      else switch(c)
      { case ' ': case '\t': break;
        case '\n': state.NewLine(); break;
        case '\"':
          string s = string.Empty;
          while(true)
          { if(state.pushBack) state.pushBack = false;
            else c = ReadChar();
            if(state.read==-1) RaiseError("Unterminated string");
            if(c=='\\')
            { EscapeChar ec = GetEscapeChar();
              if(ec.Valid) s += ec.Char;
              c = (char)state.read;
            }
            else if(c=='\"') break;
            else s += c;
          }
          state.value = s;
          return Token.Literal;
        case '\'':
          c = ReadChar();
          if(state.read==-1) RaiseError("Unterminated character constant");
          if(c=='\\')
          { EscapeChar ec = GetEscapeChar();
            if(state.pushBack) { c=(char)state.read; state.pushBack=false; }
            else c = ReadChar();
            if(!ec.Valid || c!='\'') RaiseError("Unterminated character constant");
            state.value = ec.Char;
            return Token.Literal;
          }
          else
          { state.value = c;
            if(ReadChar()!='\'') RaiseError("Unterminated character constant");
            return Token.Literal;
          }
        case '<':
          c = ReadChar();
          if(c=='<')
          { c = ReadChar();
            if(c=='=') { state.value=Assign.LeftShift; return Token.Assign; }
            state.pushBack=true; return Token.LeftShift;
          }
          if(c=='=') { state.value=Compare.LessEqual; return Token.Compare; }
          state.pushBack=true;  state.value=Compare.Less; return Token.Compare;
        case '>':
          c = ReadChar();
          if(c=='>')
          { c = ReadChar();
            if(c=='=') { state.value=Assign.RightShift; return Token.Assign; }
            state.pushBack=true; return Token.RightShift;
          }
          if(c=='=') { state.value=Compare.MoreEqual; return Token.Compare; }
          state.pushBack=true; state.value=Compare.More; return Token.Compare;
        case '=':
          c = ReadChar();
          if(c=='~') return Token.Match;
          if(c=='=')
          { c = ReadChar();
            if(c=='=') state.value=Compare.Identical;
            else { state.pushBack=true; state.value=Compare.Equal; }
            return Token.Compare;
          }
          state.pushBack=true; state.value=Assign.Assign; return Token.Assign;
        case '!':
          c = ReadChar();
          if(c=='~') return Token.NotMatch;
          if(c=='=')
          { c = ReadChar();
            if(c=='=') state.value=Compare.NotIdentical;
            else { state.pushBack=true; state.value=Compare.NotEqual; }
            return Token.Compare;
          }
          state.pushBack=true; return Token.LogNot;
        case '&':
          c = ReadChar();
          if(c=='&') return Token.LogAnd;
          if(c=='=') { state.value=Assign.BitAnd; return Token.Assign; }
          state.pushBack=true; return Token.BitAnd;
        case '|':
          c = ReadChar();
          if(c=='|') return Token.LogOr;
          if(c=='=') { state.value=Assign.BitOr; return Token.Assign; }
          state.pushBack=true; return Token.BitOr;
        case '+':
          c = ReadChar();
          if(c=='+') return Token.Increment;
          if(c=='=') { state.value=Assign.Plus; return Token.Assign; }
          state.pushBack=true; return Token.Plus;
        case '-':
          c = ReadChar();
          if(c=='-') return Token.Decrement;
          if(c=='=') { state.value=Assign.Minus; return Token.Assign; }
          state.pushBack=true; return Token.Minus;
        case '*':
          c = ReadChar();
          if(c=='*') return Token.Power;
          if(c=='=') { state.value=Assign.Times; return Token.Assign; }
          state.pushBack=true; return Token.Times;
        case '/':
          c = ReadChar();
          if(c=='*')
          { bool linebeg = state.lineBeg;
            do
            { c = ReadChar();
              if(c=='\n') state.NewLine();
            } while(c!='/' || (char)state.lastRead!='*');
            state.lineBeg = linebeg; // don't let C-style comments alter this
            break;
          }
          if(c=='/') return Token.FloorDiv;
          if(c=='=') { state.value=Assign.Divide; return Token.Assign; }
          state.pushBack=true; return Token.Divide;
        case '%':
          c = ReadChar();
          if(c=='=') { state.value=Assign.Modulus; return Token.Assign; }
          state.pushBack=true; return Token.Percent;
        case '~':
          c = ReadChar();
          if(c=='=') { state.value=Assign.BitNot; return Token.Assign; }
          state.pushBack=true; return Token.BitNot;
        case ':':
          c = ReadChar();
          if(c=='=') { state.value=Assign.LetStar; return Token.Assign; }
          state.pushBack=true; return Token.Colon;
        case '`': return Token.BackQuote;
        case ',': return Token.Comma;
        case '.': return Token.Period;
        case '(': return Token.LParen;
        case ')': return Token.RParen;
        case '[': return Token.LBracket;
        case ']': return Token.RBracket;
        case '{': return Token.LBrace;
        case '}': return Token.RBrace;
        case '?': return Token.Question;
        case ';': return Token.Semicolon;
        case '#': do c = ReadChar(); while(c!='\n'); state.NewLine(); break;
        case '\\':
          c = ReadChar();
          if(c=='\r') c = ReadChar();
          if(c=='\n') { state.curLine++; state.curChar=0; }
          else goto default;
          break;
        default: RaiseError(string.Format("Unexpected character '{0}'", c)); break;
      }
    }
  }
  #endregion

  void Reset()
  { restores = new Stack();
    state    = new State();
    text     = null;
  }

  void Restore() { state = (State)restores.Pop(); }

  void UnexpectedToken() { RaiseError(string.Format("Unexpected token {0}", token)); }

  static Node IdentifierNode(object value) { return ValueNode(Token.Identifier, value); }

  static Node LiteralNode(object value)
  { Node node = ValueNode(Token.Literal, value);
    if(value!=null)
    { System.Type type = value.GetType();
      if(type==typeof(int))         node.Type = BoaType.Int;
      else if(type==typeof(string)) node.Type = BoaType.String;
      else if(type==typeof(double)) node.Type = BoaType.Double;
      else if(type==typeof(float))  node.Type = BoaType.Float;
      else if(type==typeof(char))   node.Type = BoaType.Char;
      else if(type==typeof(long))   node.Type = BoaType.Long;
      else if(type==typeof(ulong))  node.Type = BoaType.Ulong;
      else if(type==typeof(byte))   node.Type = BoaType.Byte;
      else if(type==typeof(sbyte))  node.Type = BoaType.Sbyte;
      else if(type==typeof(short))  node.Type = BoaType.Short;
      else if(type==typeof(ushort)) node.Type = BoaType.Ushort;
      else if(type==typeof(uint))   node.Type = BoaType.Uint;
    }
    return node;
  }
  static Node ValueNode(Token token, object value)
  { Node node  = new Node(token);
    node.Value = value;
    return node;
  }
  
  class State : ICloneable
  { public State()
    { indent=new Stack();
      curLine=1; curChar=offset=0; lastRead=read=0; lineBeg=true; lastToken=Token.EOF;
    }

    public Stack  indent;
    public object value;
    public Token  token, lastToken;
    public int    curLine, curChar, lastRead, read, offset;
    public bool   newBlock, lineBeg, pushBack;

    public object Clone() { return new State(this); }
    
    public void NewLine() { curLine++; curChar=0; lineBeg=true; }

    internal State(State copy) // this code sucks
    { indent = (Stack)copy.indent.Clone();
      value  = copy.value;
      token  = copy.token;
      lastToken = copy.lastToken;
      curLine = copy.curLine;
      curChar = copy.curChar;
      lastRead = copy.lastRead;
      read = copy.read;
      offset = copy.offset;
      newBlock = copy.newBlock;
      lineBeg = copy.lineBeg;
      pushBack = copy.pushBack;
    }
  }

  Stack restores;
  State state;
  string text, file;

  internal static Hashtable stringTokens, typeTokens;
}

} // namespace AdamMil.Boa