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

  public static IEnumerator GetEnumerator(string s) { return new BoaCharEnumerator(s); }
  public static string Quote(string s)
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
}

} // namespace Boa.Runtime