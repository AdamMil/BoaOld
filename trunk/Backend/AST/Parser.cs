using System;
using System.Collections;
using System.IO;
using Boa.Runtime;

// TODO: allow nonparenthesized tuples
// TODO: change === and !== to 'is' and 'is not'
namespace Boa.AST
{

#region Tokens
enum Token
{ None,

  // punctuation
  Period, Comma, BackQuote, LParen, RParen, LBrace, RBrace, LBracket, RBracket, Question, Colon,
  Semicolon, Percent, Plus, Minus, Asterisk, Slash,
  
  // punctuation operators
  Power, FloorDivide, LeftShift, RightShift,
  BitAnd, BitOr, BitNot, BitXor, LogAnd, LogOr, LogNot,
  
  // keywords
  Def, Print, Return, And, Or, Not, While, If, Elif, Else, Pass, Break, Continue, Global, Import, From, For, In,
  Lambda,
  
  // abstract
  Identifier, Literal, Assign, Compare, Call, Member, Index, Slice, Hash, List, Tuple, Suite,
  Module, Assembly, EOL, EOF
}
#endregion

#region Parser
public class Parser
{ public Parser(Stream data) : this("<unknown>", data) { }
  public Parser(string source, Stream data) : this(source, new StreamReader(data)) { }
  public Parser(string source, TextReader data)
  { sourceFile = source; this.data = data.ReadToEnd();
    NextToken();
  }

  static Parser()
  { stringTokens = new Hashtable();
    Token[] tokens =
    { Token.Def, Token.Print, Token.Return, Token.And, Token.Or, Token.Not, Token.While, Token.Import, Token.From,
      Token.For, Token.If,  Token.Elif, Token.Else, Token.Pass, Token.Break, Token.Continue, Token.Global, Token.In,
      Token.Lambda,
    };
    foreach(Token token in tokens) stringTokens.Add(Enum.GetName(typeof(Token), token).ToLower(), token);
  }

  public static Parser FromFile(string filename) { return new Parser(filename, new StreamReader(filename)); }
  public static Parser FromStream(Stream stream) { return new Parser("<stream>", new StreamReader(stream)); }
  public static Parser FromString(string text) { return new Parser("<string>", new StringReader(text)); }

  public Suite Parse()
  { ArrayList stmts = new ArrayList();
    while(true)
    { if(TryEat(Token.EOL)) continue;
      if(TryEat(Token.EOF)) break;
      int line = this.line, column = this.column;
      Statement stmt = ParseStatement();
      stmt.SetLocation(sourceFile, line, column);
      stmts.Add(stmt);
    }
    return new Suite((Statement[])stmts.ToArray(typeof(Statement)));
  }
  
  // expression := <lowlogand> ('or' <lowlogand>)* | <lambda>
  public Expression ParseExpression()
  { if(token==Token.Lambda) return ParseLambda();
    Expression expr = ParseLowLogAnd();
    while(TryEat(Token.Or)) expr = AP(new OrExpression(expr, ParseLowLogAnd()));
    return expr;
  }
  // statement     := <stmt_line> | <compound_stmt>
  // compount_stmt := <if_stmt> | <while_stmt> | <for_stmt> | <funcdef> | <global_stmt> | <import_stmt>
  public Statement ParseStatement()
  { switch(token)
    { case Token.If:     return ParseIf();
      case Token.While:  return ParseWhile();
      case Token.For:    return ParseFor();
      case Token.Def:    return ParseDef();
      case Token.Global: return ParseGlobal();
      case Token.Import: case Token.From: return ParseImport();
      default: return ParseStmtLine();
    }
  }

  bool InLoop { get { return loopDepth>0; } }

  Expression AP(Expression e) { e.SetLocation(sourceFile, line, column); return e; }
  Statement AP(Statement s) { s.SetLocation(sourceFile, line, column); return s; }

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
  char GetEscapeChar()
  { char c = ReadChar();
    if(char.IsDigit(c))
    { if(c>'7') SyntaxError("invalid octal digit");
      int num = (c-'0');
      for(int i=1; i<3; i++)
      { c = ReadChar();
        if(!char.IsDigit(c) || c>'7') { lastChar=c; break; }
        num = (num<<3) | (c-'0');
      }
      return (char)num;
    }
    else switch(c)
    { case '\\': return '\\';
      case '\"': return '\"';
      case 'n': return '\n';
      case 't': return '\t';
      case 'r': return '\r';
      case 'b': return '\b';
      case 'e': return (char)27;
      case 'a': return '\a';
      case 'f': return '\f';
      case 'v': return '\v';
      case 'x': case 'u':
        { int num = 0;
          for(int i=0,limit=(c=='x'?2:4); i<limit; i++)
          { c = ReadChar();
            if(char.IsDigit(c)) num = (num<<4) | (c-'0');
            else if((c<'A' || c>'F') && (c<'a' || c>'f'))
            { if(i==0) SyntaxError("expected hex digit");
              lastChar = c;
              break;
            }
            num = (num<<4) | (char.ToUpper(c)-'A'+10);
          }
          return (char)num;
        }
      case 'c':
        c = ReadChar();
        if(!char.IsLetter(c)) SyntaxError("expected letter");
        return (char)(char.ToUpper(c)-64);
      default: SyntaxError(string.Format("unknown escape character '{0}'", c)); return c;
    }
  }
  #endregion

  void Eat(Token type) { if(token!=type) Unexpected(token, type); NextToken(); }
  void Expect(Token type) { if(token!=type) Unexpected(token); }

  Token NextToken()
  { if(nextToken!=Token.None)
    { token = nextToken;
      value = nextValue;
      nextToken = Token.None;
    }
    else token = ReadToken();
    return token;
  }

  // argument_list := <expression> (',' <expression>)*
  Argument[] ParseArguments()
  { if(token==Token.RParen) return new Argument[0];
    ArrayList args = new ArrayList();
    do args.Add(new Argument(ParseExpression())); while(TryEat(Token.Comma));
    return (Argument[])args.ToArray(typeof(Argument));
  }

  // bitwise    := <shift> (<bitwise_op> <shift>)*
  // bitwise_op := '&' | '|' | '^'
  Expression ParseBitwise()
  { Expression expr = ParseShift();
    while(true)
    { BinaryOperator op;
      switch(token)
      { case Token.BitAnd: op = BinaryOperator.BitwiseAnd; break;
        case Token.BitOr:  op = BinaryOperator.BitwiseOr;  break;
        case Token.BitXor: op = BinaryOperator.BitwiseXor; break;
        default: return expr;
      }
      NextToken();
      expr = AP(new BinaryOpExpression(op, expr, ParseShift()));
    }
  }
  
  // <member> '(' <argument_list> ')' | '[' <index> ']'
  Expression ParseCallIndex()
  { Expression expr = ParseMember();
    while(true)
    { if(TryEat(Token.LParen))
      { expr = AP(new CallExpression(expr, ParseArguments()));
        Eat(Token.RParen);
      }
      else if(TryEat(Token.LBracket)) throw new NotImplementedException();
      else return expr;
    }
  }

  // compare    := <bitwise> (<compare_op> <bitwise>)*
  // compare_op := '==' | '!=' | '<' | '>' | '<=' | '>=' | '===' | '!=='
  Expression ParseCompare()
  { Expression expr = ParseBitwise();
    while(token==Token.Compare)
    { BinaryOperator op = (BinaryOperator)value;
      NextToken();
      expr = AP(new BinaryOpExpression(op, expr, ParseBitwise()));
    }
    return expr;
  }

  // def := 'def' <identifier> '(' <param_list> ')' ':' <suite>
  Statement ParseDef()
  { Eat(Token.Def);
    string name = ParseIdentifier();
    Eat(Token.LParen);
    Parameter[] parms = ParseParamList(Token.RParen);
    Eat(Token.RParen);
    return AP(new DefStatement(name, parms, ParseSuite()));
  }

  // expr_stmt  := <expression> | <lvalue> '=' (<expression> | <tuple>)
  // assignable := <name> | <index> | <slice>
  // lvalue     := <assignable> | <tuple of <assignable>>
  Statement ParseExprStmt()
  { Expression lhs = ParseExpression();
    if(TryEat(Token.Assign))
    { if(lhs is NameExpression || lhs is AttrExpression || lhs is TupleExpression)
        return AP(new AssignStatement(lhs, ParseExpression()));
      throw Ops.SyntaxError("can't assign to {0}", lhs.GetType());
    }
    else return AP(new ExpressionStatement(lhs));
  }

  // factor    := <power> (<factor_op> <power>)*
  // factor_op := '*' | '/' | '%' | '//'
  Expression ParseFactor()
  { Expression expr = ParsePower();
    while(true)
    { BinaryOperator op;
      switch(token)
      { case Token.Asterisk:    op = BinaryOperator.Multiply; break;
        case Token.Slash:       op = BinaryOperator.Divide; break;
        case Token.Percent:     op = BinaryOperator.Modulus; break;
        case Token.FloorDivide: op = BinaryOperator.FloorDivide; break;
        default: return expr;
      }
      NextToken();
      expr = AP(new BinaryOpExpression(op, expr, ParsePower()));
    }
  }

  // for_stmt := 'for' <namelist> 'in' <expression> <suite>
  Statement ParseFor()
  { Eat(Token.For);
    Name[] names = ParseNameList();
    Eat(Token.In);
    return AP(new ForStatement(names, ParseExpression(), ParseSuite()));
  }

  // global_stmt := 'global' <namelist> EOL
  Statement ParseGlobal()
  { Eat(Token.Global);
    ArrayList names = new ArrayList();
    do names.Add(ParseIdentifier()); while(TryEat(Token.Comma));
    Eat(Token.EOL);
    return AP(new GlobalStatement((string[])names.ToArray(typeof(string))));
  }

  string ParseIdentifier()
  { Expect(Token.Identifier);
    string ret = (string)value;
    NextToken();
    return ret;
  }

  // if_stmt := 'if' <expression> <suite> ('elif' <expression> <suite>)* ('else' <suite>)?
  Statement ParseIf()
  { if(token!=Token.If && token!=Token.Elif) Unexpected(token);
    int indent=this.indent;
    NextToken();
    Expression test = ParseExpression();
    Statement  body = ParseSuite(), elze=null;
    if(this.indent==indent)
    { if(token==Token.Elif) elze = ParseIf();
      else if(TryEat(Token.Else)) elze = ParseSuite();
    }
    return AP(new IfStatement(test, body, elze));
  }

  // import_stmt := 'import' <import_package> (',' <import_package>)* EOL |
  //                'from' <module> 'import' <import_ident> (',' <import_ident>)* EOL |
  //                'from' <module> 'import' '*' EOL
  // import_package := <module> ('as' <identifier>)?
  // import_ident   := <identifier> ('as' <identifier>)?
  Statement ParseImport()
  { Statement stmt;
    if(TryEat(Token.From))
    { string module = ParseModule();
      Eat(Token.Import);
      if(TryEat(Token.Asterisk)) stmt = AP(new ImportFromStatement(module, new ImportName("*")));
      else
      { Expect(Token.Identifier);
        ArrayList list = new ArrayList();
        do
        { string ident = ParseIdentifier();
          if(token==Token.Identifier && (string)value=="as")
          { NextToken();
            list.Add(new ImportName(ident, ParseIdentifier()));
          }
          else list.Add(new ImportName(ident));
        } while(token!=Token.EOL && TryEat(Token.Comma));
        stmt = AP(new ImportFromStatement(module, (ImportName[])list.ToArray(typeof(ImportName))));
      }
    }
    else
    { Eat(Token.Import);
      ArrayList list = new ArrayList();
      do
      { string module = ParseModule();
        if(token==Token.Identifier && (string)value=="as")
        { NextToken();
          list.Add(new ImportName(module, ParseIdentifier()));
        }
        else list.Add(new ImportName(module));
      } while(token!=Token.EOL && TryEat(Token.Comma));
      stmt = AP(new ImportStatement((ImportName[])list.ToArray(typeof(ImportName))));
    }
    Eat(Token.EOL);
    return stmt;
  }

  // lambda := 'lambda' <namelist> ':' <lambda_body>
  Expression ParseLambda()
  { Eat(Token.Lambda);
    Parameter[] parms = ParseParamList(Token.Colon);
    Eat(Token.Colon);
    return AP(new LambdaExpression(parms, ParseLambdaBody()));
  }
  
  // lambda_body := <simple_stmt> (';' <simple_stmt>)* (';' <lambda_end>?) | <lambda_end>
  // lambda_end := <return_stmt> | <pass_stmt>
  Statement ParseLambdaBody() { return ParseLambdaBody(false); }
  Statement ParseLambdaBody(bool inside)
  { if(TryEat(Token.Return))
      switch(token)
      { case Token.EOL: case Token.Comma: case Token.RParen: case Token.RBrace: case Token.RBracket: case Token.For:
          return AP(new ReturnStatement());
        default: return AP(new ReturnStatement(ParseExpression()));
      }
    else if(TryEat(Token.Pass)) return AP(new PassStatement());

    Statement stmt = ParseSimpleStmt();
    if(!inside && TryEat(Token.Semicolon))
    { ArrayList list = new ArrayList();
      list.Add(stmt);
      do
      { stmt = ParseLambdaBody(true);
        list.Add(stmt);
      } while(!(stmt is ReturnStatement || stmt is PassStatement) && TryEat(Token.Semicolon));
      stmt = new Suite((Statement[])list.ToArray(typeof(Statement)));
    }
    return stmt;
  }

  // list_comprehension := <expression> 'for' <namelist> 'in' <expression> ('if' <expression>)?
  Expression ParseListComprehension(Expression expr)
  { Eat(Token.For);
    Name[] names = ParseNameList();
    Eat(Token.In);
    return AP(new ListCompExpression(expr, names, ParseExpression(), TryEat(Token.If) ? ParseExpression() : null));
  }

  // logand := <compare> ('&&' <compare>)*
  Expression ParseLogAnd()
  { Expression expr = ParseCompare();
    while(TryEat(Token.LogAnd)) expr = AP(new AndExpression(expr, ParseCompare()));
    return expr;
  }

  // logor := <logand> ('||' <logand>)*
  Expression ParseLogOr()
  { Expression expr = ParseLogAnd();
    while(TryEat(Token.LogOr)) expr = AP(new OrExpression(expr, ParseLogAnd()));
    return expr;
  }

  // lowlogand := <lowlognot> ("and" <lowlognot>)*
  Expression ParseLowLogAnd()
  { Expression expr = ParseLowLogNot();
    while(TryEat(Token.And)) expr = AP(new AndExpression(expr, ParseLowLogNot()));
    return expr;
  }

  // lowlognot := "not" <lowlognot> | <typelow>
  Expression ParseLowLogNot()
  { if(TryEat(Token.Not)) return AP(new UnaryExpression(ParseLowLogNot(), UnaryOperator.LogicalNot));
    return ParseTypeLow();
  }
  
  // member := <primary> <member_access>*
  // member_access ::= '.' LITERAL
  Expression ParseMember()
  { Expression expr = ParsePrimary();
    while(TryEat(Token.Period)) expr = AP(new AttrExpression(expr, ParseIdentifier()));
    return expr;
  }

  // module := <identifier> ('.' <identifier>)*
  string ParseModule()
  { string ret = ParseIdentifier();
    while(TryEat(Token.Period)) ret += '.' + ParseIdentifier();
    return ret;
  }

  // primary := LITERAL | <ident> | '(' <expression> ')' | '[' <array_list> ']' | '{' <hash_list> '}' |
  //            '[' <list_comprehension> ']' | <tuple of <expression>>
  // tuple of T := '(' (<T> ',')+ <T>? ')'
  Expression ParsePrimary()
  { Expression expr;
    switch(token)
    { case Token.Literal: expr = AP(new ConstantExpression(value)); break;
      case Token.Identifier: expr = AP(new NameExpression(new Name((string)value))); break;
      case Token.LParen:
        NextToken();
        if(token==Token.RParen) expr = AP(new TupleExpression());
        else
        { expr = ParseExpression();
          if(token==Token.Comma)
          { NextToken();
            ArrayList list = new ArrayList();
            list.Add(expr);
            while(token!=Token.RParen) { list.Add(ParseExpression()); if(!TryEat(Token.Comma)) break; }
            expr = AP(new TupleExpression((Expression[])list.ToArray(typeof(Expression))));
          }
        }
        Expect(Token.RParen);
        break;
      case Token.LBracket:
        NextToken();
        if(token==Token.RBracket) expr = AP(new ListExpression());
        else
        { Expression fe = ParseExpression();
          if(token==Token.For) expr = ParseListComprehension(fe);
          else
          { ArrayList list = new ArrayList();
            list.Add(fe);
            TryEat(Token.Comma);
            while(token!=Token.RBracket) { list.Add(ParseExpression()); if(!TryEat(Token.Comma)) break; }
            expr = AP(new ListExpression((Expression[])list.ToArray(typeof(Expression))));
          }
        }
        Expect(Token.RBracket);
        break;
      case Token.LBrace:
        NextToken();
        if(token==Token.RBrace) expr = AP(new HashExpression());
        else
        { ArrayList list = new ArrayList();
          while(token!=Token.RBrace)
          { Expression e = ParseExpression();
            Eat(Token.Colon);
            list.Add(new DictionaryEntry(e, ParseExpression()));
            if(!TryEat(Token.Comma)) break;
          }
          expr = AP(new HashExpression((DictionaryEntry[])list.ToArray(typeof(DictionaryEntry))));
        }
        Expect(Token.RBrace);
        break;
      default: Unexpected(token); return null;
    }
    NextToken();
    return expr;
  }

  // print_stmt := 'print' (<expression> (',' <expression>)* ','?)?
  Statement ParsePrintStmt()
  { Eat(Token.Print);
    if(token==Token.EOL) return AP(new PrintStatement());

    ArrayList stmts = new ArrayList();
    bool comma;
    do
    { comma=false;
      stmts.Add(ParseExpression());
      if(TryEat(Token.Comma)) comma=true;
    } while(token!=Token.EOL && token!=Token.Semicolon);
    
    return AP(new PrintStatement((Expression[])stmts.ToArray(typeof(Expression)), !comma));
  }

  // namelist := <identifier> (',' <identifier>)*
  Name[] ParseNameList()
  { ArrayList list = new ArrayList();
    do list.Add(new Name(ParseIdentifier())); while(TryEat(Token.Comma));
    return (Name[])list.ToArray(typeof(Name));
  }

  // param_list := (<identifier> (',' <identifier>)*)?
  Parameter[] ParseParamList(Token end)
  { if(token==end) return new Parameter[0];
    ArrayList parms = new ArrayList();
    while(true)
    { parms.Add(new Parameter(ParseIdentifier()));
      if(token==end) break;
      Eat(Token.Comma);
    }
    return (Parameter[])parms.ToArray(typeof(Parameter));
  }

  // power := <unary> ('**' <unary>)*
  Expression ParsePower()
  { Expression expr = ParseUnary();
    while(TryEat(Token.Power)) expr = AP(new BinaryOpExpression(BinaryOperator.Power, expr, ParseUnary()));
    return expr;
  }

  // shift := <term> (('<<' | '>>') <term>)*
  Expression ParseShift()
  { Expression expr = ParseTerm();
    while(true)
    { BinaryOperator op;
      switch(token)
      { case Token.LeftShift:  op = BinaryOperator.LeftShift; break;
        case Token.RightShift: op = BinaryOperator.RightShift; break;
        default: return expr;
      }
      NextToken();
      expr = AP(new BinaryOpExpression(op, expr, ParseTerm()));
    }
  }

  // simple_stmt := <expr_stmt> | <print_stmt>
  Statement ParseSimpleStmt()
  { if(token==Token.Print) return ParsePrintStmt();
    return ParseExprStmt();
  }

  // stmt_line := <break_stmt> | <continue_stmt> | <pass_stmt>  | <return_stmt> |
  //              <simple_stmt> (';' <simple_stmt>)* [';'] (NEWLINE | EOF)
  // break_stmt    := 'break' EOL
  // continue_stmt := 'continue' EOL
  // pass_stmt     := 'pass' EOL
  // return_stmt := return <expression>?
  Statement ParseStmtLine()
  { Statement stmt;
    switch(token)
    { case Token.Break:
        // FIXME: this doesn't work if you have a break in a function nested in a loop
        if(!InLoop) throw Ops.SyntaxError("'break' encountered outside loop");
        NextToken();
        stmt = AP(new BreakStatement());
        break;
      case Token.Continue:
        if(!InLoop) throw Ops.SyntaxError("'break' encountered outside loop");
        NextToken();
        stmt = AP(new ContinueStatement());
        break;
      case Token.Pass:
        NextToken();
        stmt = AP(new PassStatement());
        break;
      case Token.Return:
        NextToken();
        stmt = AP(token==Token.EOL ? new ReturnStatement() : new ReturnStatement(ParseExpression()));
        break;
      default:
        stmt = ParseSimpleStmt();
        if(TryEat(Token.Semicolon))
        { ArrayList stmts = new ArrayList();
          stmts.Add(stmt);
          while(token!=Token.EOL)
          { stmts.Add(ParseSimpleStmt());
            if(!TryEat(Token.Semicolon)) break;
          }
          stmt = new Suite((Statement[])stmts.ToArray(typeof(Statement)));
        }
        break;
    }
    Eat(Token.EOL);
    return stmt;
  }

  // suite := ':' stmt_line | ':'? NEWLINE INDENT <statement>+ UNINDENT
  public Statement ParseSuite()
  { if(TryEat(Token.Colon) && token!=Token.EOL) return ParseStmtLine();
    int indent=this.indent;
    Eat(Token.EOL);
    if(this.indent<=indent) throw Ops.SyntaxError("expected indent");
    ArrayList stmts = new ArrayList();
    while(this.indent>indent)
    { if(TryEat(Token.EOL)) continue;
      stmts.Add(ParseStatement());
    }
    return new Suite((Statement[])stmts.ToArray(typeof(Statement)));
  }

  // term := <factor> (('+' | '-') <factor>)*
  Expression ParseTerm()
  { Expression expr = ParseFactor();
    while(true)
    { BinaryOperator op;
      switch(token)
      { case Token.Plus:  op = BinaryOperator.Add; break;
        case Token.Minus: op = BinaryOperator.Subtract; break;
        default: return expr;
      }
      NextToken();
      expr = AP(new BinaryOpExpression(op, expr, ParseFactor()));
    }
  }

  // ternary := <logor> ('?' <expression> ':' <expression>)
  Expression ParseTernary()
  { Expression expr = ParseLogOr();
    if(TryEat(Token.Question))
    { Expression it = ParseExpression();
      Eat(Token.Colon);
      expr = AP(new TernaryExpression(expr, it, ParseExpression()));
    }
    return expr;
  }

  // typelow := <ternary>
  Expression ParseTypeLow()
  { return ParseTernary();
  }

  // unary     := <unary_op> <unary>
  // unary_op  := '!' | '~' | '-' | '+'
  Expression ParseUnary()
  { UnaryOperator op=null;
    switch(token)
    { case Token.LogNot: op = UnaryOperator.LogicalNot; break;
      case Token.Minus:  op = UnaryOperator.UnaryMinus; break;
      case Token.BitNot: op = UnaryOperator.BitwiseNot; break;
      case Token.Plus:   NextToken(); return ParseUnary();
    }
    if(op!=null)
    { NextToken();
      return AP(new UnaryExpression(ParseUnary(), op));
    }
    return ParseCallIndex();
  }

  // while_stmt := 'while' <expression> <suite>
  Statement ParseWhile()
  { Eat(Token.While);
    Expression expr = ParseExpression();
    loopDepth++;
    Statement body = ParseSuite();
    loopDepth--;
    return AP(new WhileStatement(expr, body));
  }

  Token PeekToken()
  { if(nextToken!=Token.None) return nextToken;
    return nextToken = ReadToken(ref nextValue);
  }

  char ReadChar()
  { char c;
    if(lastChar!=0) { c=lastChar; lastChar=(char)0; return c; }
    else if(pos>=data.Length) { indent=-1; return (char)0; }
    c = data[pos++]; column++;
    if(c=='\n') { line++; column=1; }
    else if(c=='\r')
    { if(pos<data.Length && data[pos]=='\n') pos++;
      c='\n'; line++; column=1;
    }
    else if(c==0) c = ' ';
    return c;
  }

  #region ReadToken
  Token ReadToken() { return ReadToken(ref value); }
  Token ReadToken(ref object value)
  { char c;
    while(true)
    { if(token==Token.EOL)
      { indent=0;
        do { c=ReadChar(); indent++; } while(c!='\n' && c!=0 && char.IsWhiteSpace(c));
        if(c=='\n') indent--;
      }
      else do c=ReadChar(); while(c!='\n' && c!=0 && char.IsWhiteSpace(c));

      if(char.IsDigit(c))
      { string s = string.Empty;
        bool period = false;
        while(true)
        { if(c=='.')
          { if(period) SyntaxError("invalid number");
            period = true;
          }
          else if(!char.IsDigit(c)) break;
          s += c;
          c = ReadChar();
        }
        try
        { if(char.ToUpper(c)=='F') value = float.Parse(s);
          else lastChar=c;
          value = period ? (object)double.Parse(s) : (object)int.Parse(s);
          return Token.Literal;
        }
        catch(FormatException) { SyntaxError("invalid number"); }
      }
      else if(c=='_' || char.IsLetter(c))
      { string s = string.Empty;
        do { s += c; c = ReadChar(); } while(c=='_' || char.IsLetterOrDigit(c));
        lastChar = c;
        if(s=="null")  { value=null;  return Token.Literal; }
        if(s=="true")  { value=true;  return Token.Literal; }
        if(s=="false") { value=false; return Token.Literal; }
        value = stringTokens[s];
        if(value!=null) return (Token)value;
        
        if(s=="r" && (c=='\"' || c=='\''))
        { char delim = c;
          s = string.Empty;
          while(true)
          { c = ReadChar();
            if(c==delim) break;
            if(c==0) SyntaxError("unexpected EOF in string literal");
            s += c;
          }
          value = s;
          return Token.Literal;
        }
        
        value = s;
        return Token.Identifier;
      }
      else switch(c)
      { case '\n': return Token.EOL;
        case '\"': case '\'':
          string s = string.Empty;
          char delim = c;
          while(true)
          { c = ReadChar();
            if(c=='\\') s += GetEscapeChar();
            else if(c==delim) break;
            else if(c==0) SyntaxError("unexpected EOF in string literal");
            else s += c;
          }
          value = s;
          return Token.Literal;
        case '<':
          c = ReadChar();
          if(c=='<') return Token.LeftShift;
          if(c=='=') value=BinaryOperator.LessEqual;
          else { lastChar = c; value = BinaryOperator.Less; }
          return Token.Compare;
        case '>':
          c = ReadChar();
          if(c=='>') return Token.RightShift;
          if(c=='=') value=BinaryOperator.MoreEqual;
          else { lastChar = c; value = BinaryOperator.More; }
          return Token.Compare;
        case '=':
          c = ReadChar();
          if(c=='=')
          { c = ReadChar();
            if(c=='=') value = BinaryOperator.Identical;
            else { lastChar=c; value=BinaryOperator.Equal; }
            return Token.Compare;
          }
          lastChar=c; value=null; return Token.Assign;
        case '!':
          c = ReadChar();
          if(c=='=')
          { c = ReadChar();
            if(c=='=') value=BinaryOperator.NotIdentical;
            else { lastChar=c; value=BinaryOperator.NotEqual; }
            return Token.Compare;
          }
          lastChar = c; return Token.LogNot;
        case '&':
          c = ReadChar();
          if(c=='&') return Token.LogAnd;
          lastChar = c; return Token.BitAnd;
        case '|':
          c = ReadChar();
          if(c=='|') return Token.LogOr;
          lastChar = c; return Token.BitOr;
        case '^': return Token.BitXor;
        case '+': return Token.Plus;
        case '-': return Token.Minus;
        case '*':
          c = ReadChar();
          if(c=='*') return Token.Power;
          lastChar = c; return Token.Asterisk;
        case '/':
          c = ReadChar();
          if(c=='*')
          { do c = ReadChar(); while(c!=0 && c!='*' && (c=ReadChar())!='/');
            lastChar = c;
            break;
          }
          if(c=='/') return Token.FloorDivide;
          lastChar = c; return Token.Slash;
        case '%': return Token.Percent;
        case '~': return Token.BitNot;
        case ':': return Token.Colon;
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
        case (char)0: nextToken=Token.EOF; return Token.EOL;
        default: SyntaxError(string.Format("unexpected character '{0}'", c)); break;
      }
    }
  }
  #endregion

  void SyntaxError(string err) { throw Ops.SyntaxError("{0} near {1}({2},{3})", err, sourceFile, line, column); }

  bool TryEat(Token type)
  { if(token==type) { NextToken(); return true; }
    return false;
  }
  
  void Unexpected(Token token)
  { throw Ops.SyntaxError("unexpected token {0} near {1}({2},{3})", token, sourceFile, line, column);
  }
  void Unexpected(Token got, Token expect)
  { throw Ops.SyntaxError("unexpected token {0} (expecting {1}) near {2}({3},{4})",
                          got, expect, sourceFile, line, column);
  }

  string     sourceFile, data;
  Token      token=Token.None, nextToken=Token.None;
  object     value, nextValue;
  int        line=1, column=1, pos, indent, loopDepth;
  char       lastChar;
  
  static Hashtable stringTokens;
}
#endregion

} // namespace Boa.AST
