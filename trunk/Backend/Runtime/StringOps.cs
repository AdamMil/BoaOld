using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Boa.Runtime
{

public sealed class StringOps
{ StringOps() { }

  #region BoaCharEnumerator
  public class BoaCharEnumerator : IEnumerator
  { public BoaCharEnumerator(string s) { str=s; index=-1; }

    public object Current
    { get
      { if(index<0 || index>=str.Length) throw new InvalidOperationException();
        return new string(str[index], 1);
      }
    }

    public bool MoveNext()
    { if(index>=str.Length-1) return false;
      index++;
      return true;
    }

    public void Reset() { index=-1; }
    
    string str;
    int index;
  }
  #endregion
  
  #region SequenceWrapper
  public class SequenceWrapper : ISequence
  { public SequenceWrapper(string str) { this.str = str; }

    #region ISequence Members
    public object __add__(object o) { throw Ops.TypeError("strings are immutable"); }
    public object __getitem__(int index) { return new string(str[index], 1); }
    object Boa.Runtime.ISequence.__getitem__(Slice slice) { return StringOps.Slice(str, slice); }
    public int __len__() { return str.Length; }
    public bool __contains__(object value)
    { if(value is string)
      { string needle = (string)value;
        return (needle.Length==0 ? str.IndexOf(needle[0]) : str.IndexOf(needle)) != -1;
      }
      if(value is char) return str.IndexOf((char)value)!=-1;
      return false;
    }
    #endregion
    
    string str;
  }

  #endregion

  #region StringFormatter
  sealed class StringFormatter
  { public StringFormatter(string source, object args) { this.source=source; this.args=args; tup=args as Tuple; }

    public string Format()
    { MatchCollection matches = StringOps.printfre.Matches(source);
      Code[] formats = new Code[matches.Count];

      int argwant=0, arggot, pos=0;
      bool dict=false, nodict=false;
      for(int i=0; i<formats.Length; i++)
      { formats[i] = new Code(matches[i]);
        argwant += formats[i].Args;
        if(formats[i].Key==null) nodict=true;
        else if(formats[i].Length==-2 || formats[i].Precision==-2) nodict=true;
        else dict=true;
      }
      if(dict && nodict) throw Ops.TypeError("keyed format codes and non-keyed format codes (or codes with a "+
                                             "length/precision of '#') mixed in format string");

      arggot = tup==null ? 1 : tup.items.Length;
      if(tup==null && dict) getitem = Ops.GetAttr(args, "__getitem__");
      else if(dict) throw Ops.TypeError("format requires a mapping");
      else if(argwant!=arggot) throw Ops.TypeError("incorrect number of arguments for string formatting "+
                                                   "(expected {0}, but got {1})", argwant, arggot);

      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      for(int fi=0; fi<formats.Length; fi++)
      { Code f = formats[fi];
        if(f.Match.Index>pos) sb.Append(source.Substring(pos, f.Match.Index-pos));
        pos = f.Match.Index+f.Match.Length;        

        if(f.Length==-2) f.Length = Ops.ToInt(NextArg());
        if(f.Precision==-2) f.Precision = Ops.ToInt(NextArg());
        
        char type = f.Type;
        switch(type) // TODO: support long integers
        { case 'd': case 'i': case 'u': case 'x': case 'X':
          { int i = Ops.ToInt(GetArg(f.Key));
            string s = null;
            bool neg, prefix=false, althex=false;
            if(type=='d' || type=='i') { s=i.ToString(); neg=prefix=i<0; }
            else
            { uint ui = (uint)i;
              if(type=='u') s=ui.ToString();
              else if(type=='x' || type=='X')
              { s = ui.ToString("x");
                althex = f.HasFlag('#');
              }
              neg = false;
            }

            int ptype = f.HasFlag('+') ? 1 : f.HasFlag(' ') ? 2 : 0;
            if(!neg && ptype!=0) { s=(ptype==1 ? '+' : ' ') + s; prefix=true; }
            if(althex) f.Length -= 2;
            if(f.Length>0 && s.Length<f.Length)
            { if(f.HasFlag('-')) s = s.PadRight(f.Length, ' ');
              else if(f.HasFlag('0'))
              { if(prefix) s = s[0] + s.Substring(1).PadLeft(f.Length-1, '0');
                else s = s.PadLeft(f.Length, '0');
              }
              else
              { if(althex) { s = "0x"+s; prefix=true; f.Length += 2; }
                s = s.PadLeft(f.Length, ' ');
              }
            }
            if(!prefix && althex) s = "0x"+s;
            if(type=='x') s = s.ToLower();
            else if(type=='X') s = s.ToUpper();
            sb.Append(s);
            break;
          }

          case 'o': throw new NotImplementedException();
          case 'e': case 'E': case 'f': case 'F': case 'g': case 'G':
          { string fmt = f.Match.Groups[5].Value;
            string s;
            double d = Ops.ToFloat(GetArg(f.Key));
            bool neg = d<0;
            if((type=='f' || type=='F') && f.Precision<0)
            { s = d.ToString("f15");
              if(s.IndexOf('.')!=-1)
              { s = s.TrimEnd('0');
                if(s[s.Length-1]=='.') s = s.Substring(0, s.Length-1);
              }
            }
            else s = d.ToString(f.Precision>0 ? fmt+f.Precision : fmt);

            if(!neg)
            { if(f.HasFlag('+')) s = '+'+s;
              else if(f.HasFlag(' ')) s = ' '+s;
            }
            if(f.Length>0 && s.Length<f.Length)
            { if(f.HasFlag('-')) s = s.PadRight(f.Length, ' ');
              else if(f.HasFlag('0')) s = Boa.Modules.@string.zfill(s, f.Length);
              else s = s.PadLeft(f.Length, ' ');
            }
            sb.Append(s);
            break;
          }

          case 'c':
          { object c = GetArg(f.Key);
            string s = c as string;
            sb.Append(s==null ? (char)Ops.ToInt(c) : s[0]);
            break;
          }

          case 'r': sb.Append(Ops.Repr(GetArg(f.Key))); break;
          case 's': sb.Append(Ops.Str(GetArg(f.Key))); break;
          case '%': sb.Append('%'); break;
          default: throw Ops.ValueError("unsupported format character '{0}' (0x{1:X})", f.Type, (int)f.Type);
        }
      }
      if(pos<source.Length) sb.Append(source.Substring(pos));
      return sb.ToString();
    }
    
    #region Code
    struct Code
    { public Code(Match m)
      { Match     = m;
        Length    = m.Groups[3].Success ? m.Groups[3].Value=="*" ? -2 : Ops.ToInt(m.Groups[3].Value) : -1;
        Precision = m.Groups[4].Success ? m.Groups[4].Value=="*" ? -2 : Ops.ToInt(m.Groups[4].Value) : -1;
      }

      public int    Args  { get { return 1 + (Length==-2 ? 1 : 0) + (Precision==-2 ? 1 : 0); } }
      public string Flags { get { return Match.Groups[2].Value; } }
      public string Key   { get { return Match.Groups[1].Success ? Match.Groups[1].Value : null; } }
      public char   Type  { get { return Match.Groups[5].Value[0]; } }
      
      public bool HasFlag(char c) { return Flags.IndexOf(c)!=-1; }

      public Match Match;
      public int   Length, Precision;
    }
    #endregion
    
    object GetArg(string name) { return name==null ? NextArg() : Ops.Call(getitem, name); }
    object NextArg() { return tup==null ? args : tup.items[argi++]; }

    string source;
    int argi;
    Tuple tup;
    object args, getitem;
  }
  #endregion

  public static IEnumerator GetEnumerator(string s) { return new BoaCharEnumerator(s); }

  public static string Escape(string s)
  { StringBuilder sb = new StringBuilder(s.Length+10);
    char quote = '\'';
    if(s.IndexOf('\'')!=-1 && s.IndexOf('\"')==-1) quote = '\"';
    sb.Append(quote);
    for(int i=0; i<s.Length; i++)
    { char c = s[i];
      switch(c)
      { case '\\': sb.Append(@"\\"); break;
        case '\t': sb.Append(@"\t"); break;
        case '\n': sb.Append(@"\n"); break;
        case '\r': sb.Append(@"\r"); break;
        case (char)27: sb.Append(@"\e"); break;
        case '\a': sb.Append(@"\a"); break;
        case '\f': sb.Append(@"\f"); break;
        case '\v': sb.Append(@"\v"); break;
        default: 
          if(c==quote) { sb.Append('\\'); sb.Append(c); }
          else if(c<32 || c>=0x7f) sb.AppendFormat(c>0xff ? @"\x{0:x4}" : @"\x{0:x2}", (int)c);
          else sb.Append(c);
          break;
      }
    }
    sb.Append(quote);
    return sb.ToString();
  }
  
  public static string PrintF(string format, object args) { return new StringFormatter(format, args).Format(); }

  public static string Slice(string s, Slice slice)
  { Tuple tup = slice.indices(s.Length);
    int start=(int)tup.items[0], stop=(int)tup.items[1], step=(int)tup.items[2], sign=Math.Sign(step);
    if(step<0 && start<=stop || step>0 && start>=stop) return string.Empty;
    if(step==1) return s.Substring(start, stop-start);
    else
    { StringBuilder sb = new StringBuilder((stop-start+step-sign)/step);
      if(step<0) for(; start>stop; start+=step) sb.Append(s[start]);
      else for(; start<stop; start+=step) sb.Append(s[start]);
      return sb.ToString();
    }
  }
  
  // keep in sync with Parser.GetEscapeChar()
  public static string Unescape(string s) { return Unescape(s, null); }
  public static string Unescape(string s, System.Text.RegularExpressions.Match m)
  { StringBuilder sb = new StringBuilder();
    char lastChar='\0';
    for(int pos=0; pos<s.Length || lastChar!=0; )
    { char c;
      if(lastChar==0) c = s[pos++];
      else { c=lastChar; lastChar='\0'; }

      if(c!='\\') sb.Append(c);
      else
      { c = ReadChar(s, ref pos);
        if(char.IsDigit(c))
        { int num = c-'0';
          if(m==null)
          { if(c>'7') throw Ops.ValueError("invalid octal digit near string index {0}", pos);
            for(int i=1; i<3; i++)
            { c = ReadChar(s, ref pos);
              if(!char.IsDigit(c) || c>'7') { lastChar=c; break; }
              num = (num<<3) | (c-'0');
            }
            sb.Append((char)num);
          }
          else
          { while(true)
            { c = ReadChar(s, ref pos);
              if(!char.IsDigit(c)) { lastChar=c; break; }
              num = num*10 + (c-'0');
            }
            if(num<m.Groups.Count && m.Groups[num].Success) sb.Append(m.Groups[num].Value);
            else throw Ops.ValueError("reference to group {0} found near string index {1}, "+
                                      "but there are only {2} groups", num, pos, m.Groups.Count);
          }
        }
        else switch(c)
        { case '\0': throw Ops.ValueError("unterminated escape sequence");
          case 'n': sb.Append('\n'); break;
          case 't': sb.Append('\t'); break;
          case 'g':
            if(m==null) sb.Append(c);
            else
            { c = ReadChar(s, ref pos);
              if(c=='<') lastChar=c;
              else
              { string name = string.Empty;
                while((c=ReadChar(s, ref pos))!='>' && c!=0) name += c;
                if(c==0) throw Ops.ValueError("unterminated group name near string index {0}", pos);
                System.Text.RegularExpressions.Group g = m.Groups[name];
                if(g==null) throw Ops.ValueError("nonexistant group '{0}' referenced near string index {1}", pos);
                if(g.Success) sb.Append(g.Value);
              }
            }
            break;
          case 'r': sb.Append('\r'); break;
          case 'b': sb.Append('\b'); break;
          case 'e': sb.Append((char)27); break;
          case 'a': sb.Append('\a'); break;
          case 'f': sb.Append('\f'); break;
          case 'v': sb.Append('\v'); break;
          case 'x': case 'u':
          { int num = 0;
            for(int i=0,limit=(c=='x'?2:4); i<limit; i++)
            { c = ReadChar(s, ref pos);
              if(char.IsDigit(c)) num = (num<<4) | (c-'0');
              else if((c<'A' || c>'F') && (c<'a' || c>'f'))
              { if(i==0) throw Ops.ValueError("expected hex digit near string index {0}", pos);
                lastChar = c;
                break;
              }
              num = (num<<4) | (char.ToUpper(c)-'A'+10);
            }
            sb.Append((char)num);
            break;
          }
          case 'c':
            c = ReadChar(s, ref pos);
            if(!char.IsLetter(c)) throw Ops.ValueError("expected letter for \\c near string index {0}", pos);
            sb.Append((char)(char.ToUpper(c)-64));
            break;
          default: sb.Append(c); break;
        }
      }
    }
    if(lastChar!=0) sb.Append(lastChar);
    return sb.ToString();
  }

  static char ReadChar(string str, ref int pos) { return pos>=str.Length ? '\0' : str[pos++]; }

  static readonly Regex printfre =
    new Regex(@"%(?:\(([^)]+)\))?([#0 +-]*)(\d+|\*)?(?:.(\d+|\*))?[hlL]?(.)",
              RegexOptions.Compiled|RegexOptions.Singleline);
}

} // namespace Boa.Runtime