using System;
using System.Text;
using System.Collections;

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
    char lastChar=(char)0;
    for(int pos=0; pos<s.Length || lastChar!=0; )
    { char c;
      if(lastChar==0) c = s[pos++];
      else { c=lastChar; lastChar=(char)0; }

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
        { case (char)0: throw Ops.ValueError("unterminated escape sequence");
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

  static char ReadChar(string str, ref int pos) { return pos>=str.Length ? (char)0 : str[pos++]; }
}

} // namespace Boa.Runtime