using System;
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
  { System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length+10);
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
    { System.Text.StringBuilder sb = new System.Text.StringBuilder((stop-start+step-sign)/step);
      if(step<0) for(; start>stop; start+=step) sb.Append(s[start]);
      else for(; start<stop; start+=step) sb.Append(s[start]);
      return sb.ToString();
    }
  }
  
  public static string Unescape(string s) { return Unescape(s, null); }
  public static string Unescape(string s, System.Text.RegularExpressions.Match m)
  { throw new NotImplementedException();
  }
}

} // namespace Boa.Runtime