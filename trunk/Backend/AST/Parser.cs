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
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Boa.Runtime;

// TODO: add switch?
// TODO: add <=> operator
// TODO: implement sets
// TODO: try to make precedence match python's where it makes sense
// TODO: support unicode string parsing
// FIXME: make this parse:   (lambda: print)
// TODO: disallow assignment to constants

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
  Def, Print, Return, While, If, Elif, Else, Pass, Break, Continue, Global, Import, From, For, In, Not,
  Lambda, Try, Except, Finally, Raise, Class, Assert, Is, Del, Yield, Exec,
  
  // abstract
  Identifier, Literal, Assign, Compare, Call, Member, Index, Slice, Hash, List, Tuple, Suite,
  Module, Assembly, EOL, EOF
}
#endregion

#region Parser
public class Parser
{ public Parser(Stream data) : this("<unknown>", data) { }
  public Parser(string source, Stream data) : this(source, new StreamReader(data), false) { }
  public Parser(string source, TextReader data) : this(source, data, false) { }
  public Parser(string source, TextReader data, bool autoclose)
  { sourceFile = source; this.data = data.ReadToEnd();
    if(autoclose) data.Close();
    NextToken();
  }

  static Parser()
  { stringTokens = new Hashtable();
    Token[] tokens =
    { Token.Def, Token.Print, Token.Return, Token.While, Token.Import, Token.From,
      Token.For, Token.If,  Token.Elif, Token.Else, Token.Pass, Token.Break, Token.Continue, Token.Global, Token.In,
      Token.Lambda, Token.Try, Token.Except, Token.Finally, Token.Raise, Token.Class, Token.Assert, Token.Is,
      Token.Del, Token.Yield, Token.Exec, Token.Not,
    };
    foreach(Token token in tokens) stringTokens.Add(Enum.GetName(typeof(Token), token).ToLower(), token);
    stringTokens.Add("and", Token.LogAnd);
    stringTokens.Add("or",  Token.LogOr);
  }

  public static Parser FromFile(string filename) { return new Parser(filename, new StreamReader(filename), true); }
  public static Parser FromStream(Stream stream) { return new Parser("<stream>", new StreamReader(stream)); }
  public static Parser FromString(string text) { return new Parser("<string>", new StringReader(text)); }

  public Statement Parse()
  { ArrayList stmts = new ArrayList();
    while(true)
    { if(TryEat(Token.EOL)) continue;
      if(TryEat(Token.EOF)) break;
      int line = this.line, column = this.column;
      Statement stmt = ParseStatement();
      stmt.SetLocation(sourceFile, line, column);
      stmts.Add(stmt);
    }
    return stmts.Count==0 ? new PassStatement() : (Statement)new Suite((Statement[])stmts.ToArray(typeof(Statement)));
  }

  // expression := <ternary> | <lambda> (',' <expression>)?
  public Expression ParseExpression()
  { if(token==Token.Lambda) return ParseLambda();
    Expression expr = ParseTernary();
    if(bareTuples && token==Token.Comma)
    { ArrayList exprs = new ArrayList();
      exprs.Add(expr);
      bareTuples = false;
      while(TryEat(Token.Comma) && token!=Token.EOL && token!=Token.Assign) exprs.Add(ParseExpression());
      bareTuples = true;
      expr = AP(new TupleExpression((Expression[])exprs.ToArray(typeof(Expression))));
    }
    return expr;
  }

  // statement     := <stmt_line> | <compound_stmt>
  // compount_stmt := <if_stmt> | <while_stmt> | <for_stmt> | <def_stmt> | <try_stmt> | <global_stmt> |
  //                  <import_stmt> | <class_stmt>
  public Statement ParseStatement()
  { switch(token)
    { case Token.If:     return ParseIf();
      case Token.While:  return ParseWhile();
      case Token.For:    return ParseFor();
      case Token.Def:    return ParseDef();
      case Token.Try:    return ParseTry();
      case Token.Global: return ParseGlobal();
      case Token.Class:  return ParseClass();
      case Token.Import: case Token.From: return ParseImport();
      default: return ParseStmtLine();
    }
  }

  bool InLoop { get { return loopDepth>0; } }

  Expression AP(Expression e) { e.SetLocation(sourceFile, line, column); return e; }
  Node AP(Node n) { n.SetLocation(sourceFile, line, column); return n; }
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
  char GetEscapeChar() // keep in sync with StringOps.Unescape
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
          else num = (num<<4) | (char.ToUpper(c)-'A'+10);
        }
        return (char)num;
      }
      case 'c':
        c = ReadChar();
        if(!char.IsLetter(c)) SyntaxError("expected letter");
        return (char)(char.ToUpper(c)-64);
      case '\n': return '\0';
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

  // argument_list := <argument> (',' <argument>)*
  // argument := ('*' | '**')? <expression> | <identifier> '=' <expression>
  Argument[] ParseArguments()
  { bool obt=bareTuples, owe=wantEOL;
    bareTuples=wantEOL=false;

    try
    { Eat(Token.LParen);
      if(token==Token.RParen) return new Argument[0];
      ArrayList args = new ArrayList();
      do
      { if(TryEat(Token.Asterisk)) args.Add(new Argument(ParseExpression(), ArgType.List));
        else if(TryEat(Token.Power)) args.Add(new Argument(ParseExpression(), ArgType.Dict));
        else if(token!=Token.Identifier) args.Add(new Argument(ParseExpression()));
        else
        { Expression e = ParseExpression();
          if(TryEat(Token.Assign))
          { if(!(e is NameExpression)) Unexpected(Token.Assign);
            args.Add(new Argument(((NameExpression)e).Name.String, ParseExpression()));
          }
          else args.Add(new Argument(e));
        }
      } while(TryEat(Token.Comma));

      ListDictionary ld = new ListDictionary();
      foreach(Argument a in args)
        if(a.Name!=null)
        { if(ld.Contains(a.Name)) SyntaxError("duplicate keyword argument '{0}'", a.Name);
          else ld[a.Name] = null;
        }

      return (Argument[])args.ToArray(typeof(Argument));
    }
    finally { bareTuples=obt; wantEOL=owe; }
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
      expr = new BinaryOpExpression(op, expr, ParseShift());
    }
  }
  
  // cim_expr := <primary> ('(' <argument_list> ')' | '[' <index> ']' | '.' <identifier>)*
  Expression ParseCIM()
  { Expression expr = ParsePrimary();
    while(true)
    { if(token==Token.LParen)
      { expr = AP(new CallExpression(expr, ParseArguments()));
        Eat(Token.RParen);
      }
      else if(TryEat(Token.LBracket))
      { Expression start = token==Token.Colon ? null : ParseExpression();
        if(TryEat(Token.Colon))
        { Expression stop = token==Token.Colon || token==Token.RBracket ? null : ParseExpression();
          Expression step = TryEat(Token.Colon) ? ParseExpression() : null;
          start = AP(new SliceExpression(start, stop, step));
        }
        Eat(Token.RBracket);
        expr = AP(new IndexExpression(expr, start));
      }
      else if(TryEat(Token.Period)) expr = AP(new AttrExpression(expr, ParseIdentifier()));
      else return expr;
    }
  }

  // class_stmt  := 'class' <identifier> <inheritance>? <suite>
  // inheritance := '(' <expr_list> ')'
  Statement ParseClass()
  { Eat(Token.Class);
    string name = ParseIdentifier();
    Expression[] bases=null;
    if(TryEat(Token.LParen))
    { if(TryEat(Token.RParen)) bases = new Expression[0];
      else
      { ArrayList inherit = new ArrayList();
        bool obt = bareTuples;
        bareTuples = false;

        do inherit.Add(ParseExpression()); while(TryEat(Token.Comma));
        Eat(Token.RParen);
        bases = (Expression[])inherit.ToArray(typeof(Expression));

        bareTuples = obt;
      }
    }
    else bases = new Expression[0];
    return AP(new ClassStatement(name, bases, ParseSuite()));
  }

  // compare    := <isin> (<compare_op> <isin>)*
  // compare_op := '==' | '!=' | '<' | '>' | '<=' | '>=' | '<>' | 'is' | 'is not'
  Expression ParseCompare()
  { Expression expr = ParseIsIn();
    ArrayList comps = null;
    while(true)
    { if(token==Token.Compare || token==Token.Is)
      { if(comps==null)
        { comps = new ArrayList();
          comps.Add(expr);
        }

        if(token==Token.Compare)
        { comps.Add(value);
          NextToken();
        }
        else // token==Token.Is
        { if(comps==null) comps = new ArrayList();
          bool not = NextToken()==Token.Not;
          comps.Add(not ? BinaryOperator.NotIdentical : (BinaryOperator)BinaryOperator.Identical);
          if(not) NextToken();
        }
        comps.Add(ParseIsIn());
      }
      else break;
    }
    if(comps!=null)
    { if(comps.Count==3)
        expr = new BinaryOpExpression((BinaryOperator)comps[1], (Expression)comps[0], (Expression)comps[2]);
      else
      { Expression[] exprs = new Expression[(comps.Count+1)/2];
        BinaryOperator[] ops = new BinaryOperator[comps.Count/2];
        exprs[0] = (Expression)comps[0];
        for(int i=1,j=0; i<comps.Count; )
        { ops[j] = (BinaryOperator)comps[i++];
          exprs[++j] = (Expression)comps[i++];
        }
        expr = new CompareExpression(exprs, ops);
      }
    }
    return expr;
  }

  // def_stmt := 'def' <identifier> '(' <param_list> ')' ':' <suite>
  Statement ParseDef()
  { Eat(Token.Def);
    string name = ParseIdentifier();
    Eat(Token.LParen);
    Parameter[] parms = ParseParamList(Token.RParen);
    Eat(Token.RParen);
    return AP(new DefStatement(name, parms, ParseSuite()));
  }

  // module := <identifier> ('.' <identifier>)*
  string ParseDotted()
  { string ret = ParseIdentifier();
    while(TryEat(Token.Period)) ret += '.' + ParseIdentifier();
    return ret;
  }

  // expr_stmt  := (<lvalue> '=')* <expression>
  // assignable := <name> | <index> | <slice>
  // lvalue     := <assignable> | <tuple of <assignable>>
  Statement ParseExprStmt()
  { Expression lhs = ParseExpression();
    if(token==Token.Assign)
    { ArrayList list = new ArrayList();
      AssignStatement ass = (AssignStatement)AP(new AssignStatement());
      ass.Op = (BinaryOperator)value;

      while(TryEat(Token.Assign))
      { if(ass.Op!=null)
        { if(list.Count>0) SyntaxError("can't chain in-place assignment");
          if(!(lhs is NameExpression || lhs is AttrExpression || lhs is IndexExpression))
            SyntaxError("can't do in-place assignment with {0}", lhs.GetType());
        }
        else if(!(lhs is NameExpression || lhs is AttrExpression || lhs is TupleExpression || lhs is IndexExpression))
          SyntaxError("can't assign to {0}", lhs.GetType());
        list.Add(lhs);
        lhs = ParseExpression();
      }
      ass.LHS = (Expression[])list.ToArray(typeof(Expression));
      ass.RHS = lhs;
      return ass;
    }
    else return new ExpressionStatement(lhs);
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
      expr = new BinaryOpExpression(op, expr, ParsePower());
    }
  }

  // for_stmt := 'for' <namelist> 'in' <expression> <suite>
  Statement ParseFor()
  { Eat(Token.For);
    Name[] names = ParseNameList();
    Eat(Token.In);
    Expression loopExp = ParseExpression();
    loopDepth++;
    Statement body = ParseSuite();
    loopDepth--;
    return AP(new ForStatement(names, loopExp, body));
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
  //                'from' <dotted> 'import' <import_ident> (',' <import_ident>)* EOL |
  //                'from' <dotted> 'import' '*' EOL
  // import_package := <dotted> ('as' <identifier>)?
  // import_ident   := <identifier> ('as' <identifier>)?
  Statement ParseImport()
  { Statement stmt;
    if(TryEat(Token.From))
    { string module = ParseDotted();
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
      { string module = ParseDotted();
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

  // isin := <bitwise> ('not'? 'in' <bitwise>)*
  Expression ParseIsIn()
  { Expression expr = ParseBitwise();
    while(true)
    { if(TryEat(Token.In)) expr = AP(new InExpression(expr, ParseBitwise(), false));
      else if(TryEat(Token.Not))
      { Eat(Token.In);
        expr = AP(new InExpression(expr, ParseBitwise(), true));
      }
      else return expr;
    }
  }

  // lambda := 'lambda' <namelist> ':' <lambda_body>
  // lambda_body := <simple_stmt> (';' <simple_stmt>)* <lambda_end>
  // lambda_end := EOL | ',' | ')' | ']' | '}' | 'for'
  Expression ParseLambda()
  { Eat(Token.Lambda);
    Parameter[] parms = ParseParamList(Token.Colon);
    Eat(Token.Colon);

    ArrayList list = new ArrayList();
    do
    { switch(token)
      { case Token.EOL: case Token.Comma: case Token.RParen: case Token.RBrace: case Token.RBracket: case Token.For:
          goto done;
        case Token.Return:
          switch(NextToken())
          { case Token.EOL: case Token.Comma: case Token.RParen: case Token.RBrace: case Token.RBracket:
            case Token.For:
              list.Add(AP(new ReturnStatement())); break;
            default: list.Add(AP(new ReturnStatement(ParseExpression()))); break;
          }
          break;
        default: list.Add(ParseSimpleStmt()); break;
      }
    } while(TryEat(Token.Semicolon));

    done:
    if(list.Count==0) Unexpected(token);
    return AP(new LambdaExpression(parms, new Suite((Statement[])list.ToArray(typeof(Statement)))));
  }

  // list_comprehension := <expression> ('for' <namelist> 'in' <expression> ('if' <expression>)?)+
  Expression ParseListComprehension(Expression expr)
  { ArrayList list = new ArrayList();
    do
    { Eat(Token.For);
      Name[] names = ParseNameList();
      Eat(Token.In);
      list.Add(new ListCompFor(names, ParseExpression(), TryEat(Token.If) ? ParseExpression() : null));
    } while(token==Token.For);
    return AP(new ListCompExpression(expr, (ListCompFor[])list.ToArray(typeof(ListCompFor))));
  }

  // logand := <lognot> ('&&' <lognot>)*
  Expression ParseLogAnd()
  { Expression expr = ParseLogNot();
    while(TryEat(Token.LogAnd)) expr = AP(new AndExpression(expr, ParseLogNot()));
    return expr;
  }

  // lognot := 'not' <lognot> | <compare>
  Expression ParseLogNot()
  { return TryEat(Token.Not) ? new UnaryExpression(ParseLogNot(), UnaryOperator.LogicalNot) : ParseCompare();
  }
  
  // logor := <logand> ('||' <logand>)*
  Expression ParseLogOr()
  { Expression expr = ParseLogAnd();
    while(TryEat(Token.LogOr)) expr = AP(new OrExpression(expr, ParseLogAnd()));
    return expr;
  }

  // primary := LITERAL | <ident> | '(' <expression> ')' | '[' <array_list> ']' | '{' <hash_list> '}' |
  //            '[' <list_comprehension> ']' | <tuple of <expression>> | '`' <expression> '`'
  // tuple of T := '(' (<T> ',')+ <T>? ')'
  Expression ParsePrimary()
  { Expression expr;
    bool obt=bareTuples, owe=wantEOL;

    switch(token)
    { case Token.Literal:
        if(value is string)
        { ConstantExpression e = (ConstantExpression)AP(new ConstantExpression(null));
          string s = (string)value;
          while(NextToken()==Token.Literal && value is string) s += (string)value;
          if(token==Token.Literal) SyntaxError("unexpected literal '{0}' in string concatenation");
          e.Value = s;
          return e;
        }
        else expr = AP(new ConstantExpression(value));
        break;
      case Token.Identifier: expr = AP(new NameExpression(new Name((string)value))); break;
      case Token.LParen:
        bareTuples=wantEOL=false;
        NextToken();
        if(token==Token.RParen) expr = AP(new TupleExpression());
        else
        { expr = ParseExpression();
          if(token==Token.Comma)
          { NextToken();
            ArrayList list = new ArrayList();
            list.Add(expr);
            while(token!=Token.RParen)
            { bareTuples = false;
              list.Add(ParseExpression());
              if(!TryEat(Token.Comma)) break;
            }
            expr = AP(new TupleExpression((Expression[])list.ToArray(typeof(Expression))));
          }
          else if(token==Token.For) // hijack ParseListComprehension()
          { ListCompExpression lc = (ListCompExpression)ParseListComprehension(expr);
            expr = new GeneratorExpression(lc.Item, lc.Fors);
            expr.SetLocation(lc);
          }
          else expr = AP(new ParenExpression(expr));
        }
        Expect(Token.RParen);
        break;
      case Token.LBracket:
        bareTuples=wantEOL=false;
        NextToken();
        if(token==Token.RBracket) expr = AP(new ListExpression());
        else
        { Expression fe = ParseExpression();
          if(token==Token.For) expr = ParseListComprehension(fe);
          else
          { ArrayList list = new ArrayList();
            list.Add(fe);
            TryEat(Token.Comma);
            while(token!=Token.RBracket)
            { bareTuples = false;
              list.Add(ParseExpression());
              if(!TryEat(Token.Comma)) break;
            }
            expr = AP(new ListExpression((Expression[])list.ToArray(typeof(Expression))));
          }
        }
        Expect(Token.RBracket);
        break;
      case Token.LBrace:
        bareTuples=wantEOL=false;
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
      case Token.BackQuote:
        NextToken();
        expr = ParseExpression();
        Expect(Token.BackQuote);
        expr = AP(new ReprExpression(expr));
        break;
      default: Unexpected(token); return null;
    }
    bareTuples=obt; wantEOL=owe;
    NextToken();
    return expr;
  }

  // print_stmt := 'print' (<expression> (',' <expression>)* ','?)?
  Statement ParsePrintStmt()
  { Eat(Token.Print);
    if(token==Token.EOL) return AP(new PrintStatement());

    ArrayList stmts = new ArrayList();
    bool comma, old=bareTuples;
    bareTuples = false;
    do
    { comma=false;
      stmts.Add(ParseExpression());
      if(TryEat(Token.Comma)) comma=true;
    } while(token!=Token.EOL && token!=Token.Semicolon);
    bareTuples = old;
    return AP(new PrintStatement((Expression[])stmts.ToArray(typeof(Expression)), !comma));
  }

  // namelist := <identifier> (',' <identifier>)*
  Name[] ParseNameList()
  { ArrayList list = new ArrayList();
    do list.Add(new Name(ParseIdentifier())); while(TryEat(Token.Comma));
    return (Name[])list.ToArray(typeof(Name));
  }

  // param_list := <required_params>? <pcomma> <optional_params>? <pcomma> ('*' <identifier>)? <pcomma>
  //               ('**' <identifier>)?
  // required_params := <identifier> (',' <identifier>)
  // optional_params := <optional_param> (',' <optional_param>)*
  // optional_param  := <identifier> '=' <expression>
  // pcomma := ','?    (comma required to separate argument/parameter groups)
  Parameter[] ParseParamList(Token end)
  { if(token==end) return new Parameter[0];
    ArrayList parms = new ArrayList();
    string ident;
    bool obt=bareTuples, owe=wantEOL;
    bareTuples=wantEOL=false;

    while(true) // required identifiers
    { if(TryEat(Token.Asterisk)) goto list;
      if(token==Token.Power) goto dict;
      ident = ParseIdentifier();
      if(TryEat(Token.Assign)) break;
      parms.Add(new Parameter(ident));
      if(token==end) goto done;
      Eat(Token.Comma);
    }
    while(true) // positional parameters
    { parms.Add(new Parameter(ident, ParseExpression()));
      if(token==end) goto done;
      Eat(Token.Comma);
      if(TryEat(Token.Asterisk)) break;
      if(token==Token.Power) goto dict;
      ident = ParseIdentifier();
      Eat(Token.Assign);
    }
    list: if(token==Token.Identifier) parms.Add(new Parameter(ParseIdentifier(), ParamType.List));
    if(token==end) goto done;
    Eat(Token.Comma);
    dict: Eat(Token.Power);
    parms.Add(new Parameter(ParseIdentifier(), ParamType.Dict));

    done:
    ListDictionary ld = new ListDictionary();
    foreach(Parameter p in parms)
      if(ld.Contains(p.Name.String)) SyntaxError("duplicate parameter name '{0}'", p.Name.String);
      else ld[p.Name.String] = null;

    bareTuples=obt; wantEOL=owe;
    return (Parameter[])parms.ToArray(typeof(Parameter));
  }

  // power := <unary> ('**' <unary>)*
  Expression ParsePower()
  { Expression expr = ParseUnary();
    while(TryEat(Token.Power)) expr = new BinaryOpExpression(BinaryOperator.Power, expr, ParseUnary());
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
      expr = new BinaryOpExpression(op, expr, ParseTerm());
    }
  }

  // simple_stmt := <expr_stmt> | <print_stmt> | <break_stmt> | <continue_stmt> | <pass_stmt> | <return_stmt>
  //                <assert_stmt> | <del_stmt> | <yield_stmt>
  // break_stmt    := 'break' EOL
  // continue_stmt := 'continue' EOL
  // pass_stmt     := 'pass' EOL
  // raise_stmt    := 'raise' <expression>?
  // return_stmt   := 'return' <expression>?
  // assert_stmt   := 'assert' <expression>
  // yield_stmt    := 'yield' <expression>
  // del_stmt      := 'del' <lvalue> (',' <lvalue>)*
  // exec_stmt     := 'exec' <expression> ('in' <expression> (',' <expression>)?)?
  Statement ParseSimpleStmt()
  { switch(token)
    { case Token.Print: return ParsePrintStmt();
      case Token.Break:
        // FIXME: this doesn't work if you have a break in a function nested in a loop
        if(!InLoop) SyntaxError("'break' encountered outside loop");
        NextToken();
        return AP(new BreakStatement());
      case Token.Continue:
        if(!InLoop) SyntaxError("'continue' encountered outside loop");
        NextToken();
        return AP(new ContinueStatement());
      case Token.Pass: NextToken(); return AP(new PassStatement());
      case Token.Return:
        NextToken();
        return AP(token==Token.EOL || token==Token.Semicolon ? new ReturnStatement()
                                                             : new ReturnStatement(ParseExpression()));
      case Token.Raise:
        NextToken();
        return AP(token==Token.EOL || token==Token.Semicolon ? new RaiseStatement()
                                                             : new RaiseStatement(ParseExpression()));
      case Token.Assert: NextToken(); return AP(new AssertStatement(ParseExpression()));
      case Token.Yield: NextToken(); return AP(new YieldStatement(ParseExpression()));
      case Token.Del:
      { NextToken();
        ArrayList list = new ArrayList();
        do
        { Expression e = ParseExpression();
          if(!(e is NameExpression || e is AttrExpression || e is TupleExpression || e is IndexExpression))
            SyntaxError("can't delete {0}", e.GetType());
          list.Add(e);
        } while(TryEat(Token.Comma));
        return AP(new DelStatement((Expression[])list.ToArray(typeof(Expression))));
      }
      case Token.Exec:
      { NextToken();
        Expression e = ParseExpression(), globals=null, locals=null;
        if(TryEat(Token.In))
        { bool old = bareTuples;
          bareTuples = false;
          globals = ParseExpression();
          if(TryEat(Token.Comma)) locals = ParseExpression();
          bareTuples = old;
        }
        return new ExecStatement(e, globals, locals);
      }
      default: return ParseExprStmt();
    }
  }

  // stmt_line := <simple_stmt> (';' <simple_stmt>)* (NEWLINE | EOF)
  Statement ParseStmtLine()
  { Statement stmt = ParseSimpleStmt();
    if(token==Token.Semicolon)
    { ArrayList stmts = new ArrayList();
      stmts.Add(stmt);
      while(TryEat(Token.Semicolon)) stmts.Add(ParseSimpleStmt());
      stmt = new Suite((Statement[])stmts.ToArray(typeof(Statement)));
    }
    Eat(Token.EOL);
    return stmt;
  }

  // suite := ':' stmt_line | ':'? NEWLINE INDENT <statement>+ UNINDENT
  Statement ParseSuite()
  { if(TryEat(Token.Colon) && token!=Token.EOL) return ParseStmtLine();
    int indent=this.indent;
    Eat(Token.EOL);
    if(this.indent<=indent) SyntaxError("expected indent");
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
      expr = new BinaryOpExpression(op, expr, ParseFactor());
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

  // try := 'try' <suite> NEWLINE ('except' <expression>? ',' <ident> <suite>)*
  //        ('except' <suite>)? ('else' <suite>)? ('finally' <suite>)?
  Statement ParseTry()
  { int indent=this.indent;
    Eat(Token.Try);
    Statement body = ParseSuite(), elze=null, final=null;
    ArrayList list = new ArrayList();
    while(indent==this.indent && TryEat(Token.Except))
    { bool obt=bareTuples;
      bareTuples=false;
      Expression     type = token==Token.Comma || token==Token.Colon || token==Token.EOL ? null : ParseExpression();
      bareTuples=true;
      NameExpression name = TryEat(Token.Comma) ? (NameExpression)AP(new NameExpression(ParseIdentifier())) : null;
      list.Add(AP((Node)new ExceptClause(type, name, ParseSuite())));
      if(type==null) break;
    }
    if(indent==this.indent && TryEat(Token.Else)) elze = ParseSuite();
    if(indent==this.indent && TryEat(Token.Finally)) final = ParseSuite();
    if(list.Count==0 && elze==null && final==null) SyntaxError("expecting 'except', 'else', or 'finally'");
    return AP(new TryStatement(body, (ExceptClause[])list.ToArray(typeof(ExceptClause)), elze, final));
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
    return ParseCIM();
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
    if(lastChar!=0) { c=lastChar; lastChar='\0'; return c; }
    else if(pos>=data.Length) { indent=-1; return '\0'; }
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
      { indent=0; c=ReadChar();
        if(wantEOL)
          while(c!=0 && char.IsWhiteSpace(c))
          { if(c=='\n') indent=0;
            else indent++;
            c=ReadChar();
          }
        else while(c!=0 && char.IsWhiteSpace(c)) c=ReadChar();
      }
      else do c=ReadChar(); while(c!='\n' && c!=0 && char.IsWhiteSpace(c));

      if(char.IsDigit(c) || c=='.')
      { if(c=='.')
        { lastChar = ReadChar();
          if(!char.IsDigit(lastChar)) return Token.Period;
        }

        string s=string.Empty;
        bool period=false;

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
        { if(char.ToUpper(c)=='J') value = new Complex(0, double.Parse(s));
          else if(period)
          { if(char.ToUpper(c)=='L') throw new NotImplementedException("decimal type");
            else { lastChar=c; value = double.Parse(s); }
          }
          else
          { if(char.ToUpper(c)!='L') lastChar=c;
            try { value = int.Parse(s); }
            catch(OverflowException)
            { try { value = long.Parse(s); }
              catch(OverflowException) { value = Integer.Parse(s); }
            }
          }
          return Token.Literal;
        }
        catch(FormatException) { SyntaxError("invalid number"); }
      }
      else if(c=='_' || char.IsLetter(c))
      { StringBuilder sb = new StringBuilder();

        if(c=='r')
        { char temp=c; c=ReadChar();
          if(c=='\"' || c=='\'')
          { char delim = c;
            while((c=ReadChar())!=0 && c!=delim) sb.Append(c);
            if(c==0) SyntaxError("unterminated string constant");
            value = sb.ToString();
            return Token.Literal;
          }
          else sb.Append(temp);
        }

        while(c=='_' || char.IsLetterOrDigit(c)) { sb.Append(c); c = ReadChar(); }
        lastChar = c;

        string s = sb.ToString();
        if(s=="null"  || s=="None") { value=null;  return Token.Literal; }
        if(s=="true"  || s=="True")  { value=true;  return Token.Literal; }
        if(s=="false" || s=="False") { value=false; return Token.Literal; }
        value = stringTokens[s];
        if(value!=null) return (Token)value;
        
        if(s=="r" && (c=='\"' || c=='\''))
        { char delim = c;
          sb.Remove(0, sb.Length);
          while(true)
          { c = ReadChar();
            if(c==delim) break;
            if(c==0) SyntaxError("unexpected EOF in string literal");
            sb.Append(c);
          }
          value = sb.ToString();
          return Token.Literal;
        }

        value = s;
        return Token.Identifier;
      }
      else switch(c)
      { case '\n': newline: if(wantEOL) return Token.EOL; else { token=Token.EOL; break; }
        case '\"': case '\'':
        { StringBuilder sb = new StringBuilder();
          char delim = c;
          bool triple = false;

          c = ReadChar();
          if(c==delim)
          { c = ReadChar();
            if(c==delim) triple = true;
            else { lastChar=c; value=string.Empty; return Token.Literal; }
          }
          else if(c=='\\') { char e = GetEscapeChar(); if(e!=0) sb.Append(e); }
          else sb.Append(c);

          while(true)
          { c = ReadChar();
            if(c=='\\') { char e = GetEscapeChar(); if(e!=0) sb.Append(e); }
            else if(c==delim)
            { if(!triple) break;
              if((c=ReadChar())==delim)
              { if((c=ReadChar())==delim) break;
                else
                { sb.Append(delim, 2);
                  if(c=='\\') { char e = GetEscapeChar(); if(e!=0) sb.Append(e); }
                  else sb.Append(c);
                }
              }
              else
              { sb.Append(delim);
                if(c=='\\') { char e = GetEscapeChar(); if(e!=0) sb.Append(e); }
                else sb.Append(c);
              }
            }
            else if(c==0) SyntaxError("unexpected EOF in string literal");
            else sb.Append(c);
          }
          value = sb.ToString();
          return Token.Literal;
        }
        case '<':
          c = ReadChar();
          if(c=='<') return Token.LeftShift;
          if(c=='=') value=BinaryOperator.LessEqual;
          else if(c=='>') value = BinaryOperator.NotEqual;
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
          if(c=='=') { value=BinaryOperator.Equal; return Token.Compare; }
          else { lastChar=c; value=null; return Token.Assign; }
        case '!':
          c = ReadChar();
          if(c=='=') { value=BinaryOperator.NotEqual; return Token.Compare; }
          else { lastChar = c; return Token.LogNot; }
        case '&':
          c = ReadChar();
          if(c=='&')
          { c = ReadChar();
            if(c=='=') { value=BinaryOperator.LogicalAnd; return Token.Assign; }
            lastChar = c; return Token.LogAnd;
          }
          if(c=='=') { value=BinaryOperator.BitwiseAnd; return Token.Assign; }
          lastChar = c; return Token.BitAnd;
        case '|':
          c = ReadChar();
          if(c=='|')
          { c = ReadChar();
            if(c=='=') { value=BinaryOperator.LogicalOr; return Token.Assign; }
            lastChar = c; return Token.LogOr;
          }
          if(c=='=') { value=BinaryOperator.BitwiseOr; return Token.Assign; }
          lastChar = c; return Token.BitOr;
        case '^':
          c = ReadChar();
          if(c=='=') { value=BinaryOperator.BitwiseAnd; return Token.Assign; }
          lastChar = c; return Token.BitXor;
        case '+':
          c = ReadChar();
          if(c=='=') { value=BinaryOperator.Add; return Token.Assign; }
          lastChar = c; return Token.Plus;
        case '-':
          c = ReadChar();
          if(c=='=') { value=BinaryOperator.Subtract; return Token.Assign; }
          lastChar = c; return Token.Minus;
        case '*':
          c = ReadChar();
          if(c=='=') { value=BinaryOperator.Multiply; return Token.Assign; }
          if(c=='*')
          { c = ReadChar();
            if(c=='=') { value=BinaryOperator.Power; return Token.Assign; }
            lastChar = c; return Token.Power;
          }
          lastChar = c; return Token.Asterisk;
        case '/':
          c = ReadChar();
          if(c=='/')
          { c = ReadChar();
            if(c=='=') { value=BinaryOperator.FloorDivide; return Token.Assign; }
            lastChar = c; return Token.FloorDivide;
          }
          if(c=='=') { value=BinaryOperator.Divide; return Token.Assign; }
          if(c=='*')
          { do c = ReadChar(); while(c!=0 && (c!='*' || (c=ReadChar())!='/'));
            break;
          }
          lastChar = c; return Token.Slash;
        case '%':
          c = ReadChar();
          if(c=='=') { value=BinaryOperator.Modulus; return Token.Assign; }
          lastChar = c; return Token.Percent;
        case '~': return Token.BitNot;
        case ':': return Token.Colon;
        case '`': return Token.BackQuote;
        case ',': return Token.Comma;
        case '(': return Token.LParen;
        case ')': return Token.RParen;
        case '[': return Token.LBracket;
        case ']': return Token.RBracket;
        case '{': return Token.LBrace;
        case '}': return Token.RBrace;
        case '?': return Token.Question;
        case ';': return Token.Semicolon;
        case '#':
          do c = ReadChar(); while(c!='\n' && c!=0);
          goto newline;
        case '\\':
          c = ReadChar();
          if(c=='\n') break;
          goto default;
        case '\0': if(wantEOL) { nextToken=Token.EOF; return Token.EOL; } else return Token.EOF;
        default: SyntaxError(string.Format("unexpected character '{0}' (0x{1:X})", c, (int)c)); break;
      }
    }
  }
  #endregion

  void SyntaxError(string format, params object[] args)
  { throw Ops.SyntaxError("{0}({1},{2}): {3}", sourceFile, line, column, string.Format(format, args));
  }

  bool TryEat(Token type)
  { if(token==type) { NextToken(); return true; }
    return false;
  }
  
  void Unexpected(Token token) { SyntaxError("unexpected token {0}", token, sourceFile, line, column); }
  void Unexpected(Token got, Token expect)
  { SyntaxError("unexpected token {0} (expecting {1})", got, expect, sourceFile, line, column);
  }

  string     sourceFile, data;
  Token      token=Token.EOL, nextToken=Token.None;
  object     value, nextValue;
  int        line=1, column=1, pos, indent, loopDepth;
  char       lastChar;
  bool       bareTuples=true, wantEOL=true;
  
  static Hashtable stringTokens;
}
#endregion

} // namespace Boa.AST
