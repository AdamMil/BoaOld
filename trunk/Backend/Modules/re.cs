using System;
using System.Collections;
using System.Text.RegularExpressions;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class re
{ re() { }

  #region Support classes
  public class FindEnumerator : IEnumerator
  { public FindEnumerator(Regex regex, string str) { this.regex=regex; this.str=str; pos=-1; }
  
    public object Current
    { get
      { if(pos<0) throw new InvalidOperationException();
        return match;
      }
    }
    
    public bool MoveNext()
    { if(pos==-2) return false;
      if(pos==-1) pos=0;
      Match m = regex.Match(str, pos);
      if(m==null || !m.Success) { pos=-2; return false; }
      match = re.MatchToFind(m);
      return true;
    }
    
    public void Reset() { pos=-1; match=null; }

    Regex regex;
    string str;
    object match;
    int pos;
  }

  public class RegexErrorException : ValueErrorException
  { public RegexErrorException(string message) : base(message) { }
  }
  #endregion

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 're' (built-in)>"; }

  public static Regex compile(string pattern) { return compile(pattern, (int)RegexOptions.Singleline); }
  public static Regex compile(string pattern, int flags)
  { return MakeRegex(pattern, (RegexOptions)flags | RegexOptions.Compiled);
  }

  public static string escape(string str)
  { System.Text.StringBuilder sb = new System.Text.StringBuilder(str.Length+10);
    for(int i=0; i<str.Length; i++)
    { char c = str[i];
      if(!char.IsLetterOrDigit(c)) sb.Append('\\');
      sb.Append(c);
    }
    return sb.ToString();
  }

  public static List findall(string pattern, string str)
  { List ret = new List();
    foreach(Match m in MakeRegex(pattern).Matches(str)) ret.append(MatchToFind(m));
    return ret;
  }
  
  public static IEnumerator finditer(string pattern, string str)
  { return new FindEnumerator(MakeRegex(pattern), str);
  }

  public static Match match(string pattern, string str) { return match(pattern, str, (int)RegexOptions.Singleline); }
  public static Match match(string pattern, string str, int flags)
  { Match m = search(pattern, str, flags);
    return m.Index==0 ? m : null;
  }

  public static Match search(string pattern, string str)
  { return search(pattern, str, (int)RegexOptions.Singleline);
  }
  public static Match search(string pattern, string str, int flags)
  { Match m = MakeRegex(pattern, (RegexOptions)flags).Match(str);
    return m!=null && m.Success ? m : null;
  }

  public static List split(string pattern, string str) { return split(pattern, str, 0); }
  public static List split(string pattern, string str, int maxsplit)
  { if(maxsplit<0) throw Ops.ValueError("split(): maxsplit must be >= 0");
    if(maxsplit==0) maxsplit=-1;
    MatchCollection matches = MakeRegex(pattern).Matches(str);

    List ret = new List();
    int i=0, pos=0;
    for(; i<matches.Count && i<maxsplit; i++)
    { Match m = matches[i];
      ret.append(str.Substring(pos, m.Index-pos));
      pos = m.Index+m.Length;
      for(int g=1; g<m.Groups.Count; g++) if(m.Groups[g].Success) ret.append(m.Groups[g].Value);
    }
    ret.append(pos==0 ? str : str.Substring(pos));
    return ret;
  }

  public static string sub(string pattern, object repl, string str) { return sub(pattern, repl, str, 0); }
  public static string sub(string pattern, object repl, string str, int maxreplace)
  { int dummy;
    return sub(pattern, repl, str, maxreplace, out dummy);
  }

  public static Tuple subn(string pattern, object repl, string str) { return subn(pattern, repl, str, 0); }
  public static Tuple subn(string pattern, object repl, string str, int maxreplace)
  { int count;
    return new Tuple(sub(pattern, repl, str, maxreplace, out count), count);
  }

  public const int I=(int)RegexOptions.IgnoreCase, IGNORE=I;
  public const int M=(int)RegexOptions.Multiline, MULTILINE=M;
  public const int X=(int)RegexOptions.IgnorePatternWhitespace, VERBOSE=X;

  public static readonly ReflectedType error = ReflectedType.FromType(typeof(RegexErrorException));

  static Regex MakeRegex(string pattern) { return MakeRegex(pattern, RegexOptions.Singleline); }
  static Regex MakeRegex(string pattern, RegexOptions flags)
  { try { return new Regex(pattern, flags); }
    catch(ArgumentException e) { throw new RegexErrorException("regex parse error: " + e.Message); }
  }
  
  static object MatchToFind(Match m)
  { GroupCollection groups = m.Groups;
    if(groups.Count<=1) return m.Value;
    else
    { object[] items = new object[groups.Count-1];
      for(int i=1; i<groups.Count; i++) items[i-1] = groups[i].Value;
      return new Tuple(items);
    }
  }
  
  static string sub(string pattern, object repl, string str, int maxreplace, out int count)
  { if(maxreplace<0) throw Ops.ValueError("sub(): maxreplace must be >= 0");
    if(maxreplace==0) maxreplace=-1;

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    MatchCollection matches = MakeRegex(pattern).Matches(str);
    int i=0, pos=0;

    if(repl is string)
    { string rep = (string)repl;
      bool unescape = rep.IndexOf('\\')!=-1;

      for(; i<matches.Count && i<maxreplace; i++)
      { Match m = matches[i];
        if(m.Index!=pos) sb.Append(str.Substring(pos, m.Index-pos));
        sb.Append(unescape ? StringOps.Unescape(rep, m) : rep);
        pos = m.Index+m.Length;
      }
    }
    else
      for(; i<matches.Count && i<maxreplace; i++)
      { Match m = matches[i];
        if(m.Index!=pos) sb.Append(str.Substring(pos, m.Index-pos));
        sb.Append(Ops.Str(Ops.Call(repl, m)));
        pos = m.Index+m.Length;
      }
    count = i;
    sb.Append(pos==0 ? str : str.Substring(pos));
    return sb.ToString();
  }
}

} // namespace Boa.Modules