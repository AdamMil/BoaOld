using System;
using System.Collections;
using System.Text.RegularExpressions;
using Boa.Runtime;

namespace Boa.Modules
{

// TODO: finish this by adding 'regex' and 'match' objects

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
      match = regex.Match(str, pos);
      if(match==null || !match.Success) { pos=-2; return false; }
      pos += match.Length;
      return true;
    }

    public void Reset() { pos=-1; match=null; }

    Regex regex;
    string str;
    Match match;
    int pos;
  }

  [DocString(@"Exception raised when a string passed to one of the functions here is not a
valid regular expression (for example, it might contain unmatched
parentheses) or when some other error occurs during compilation or matching.
It is never an error if a string contains no match for a pattern.")]
  public class RegexErrorException : ValueErrorException
  { public RegexErrorException(string message) : base(message) { }
  }
  #endregion

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 're' (built-in)>"; }

  [DocString(@"compile(pattern[, flags])

Compile a regular expression pattern, returning a Regex object.")]
  public static Regex compile(string pattern) { return compile(pattern, (int)RegexOptions.Singleline); }
  public static Regex compile(string pattern, int flags)
  { return MakeRegex(pattern, (RegexOptions)flags | RegexOptions.Compiled);
  }

  [DocString(@"escape(pattern)

Escape characters in pattern that may be regex metacharacters.")]
  public static string escape(string str)
  { System.Text.StringBuilder sb = new System.Text.StringBuilder(str.Length+10);
    for(int i=0; i<str.Length; i++)
    { char c = str[i];
      if(!char.IsLetterOrDigit(c) && c>32) sb.Append('\\');
      sb.Append(c);
    }
    return sb.ToString();
  }

  [DocString(@"findall(pattern, string)

Return a list of all non-overlapping matches in the string.

If one or more groups are present in the pattern, return a list of groups;
this will be a list of tuples if the pattern has more than one group.

Empty matches are included in the result.")]
  public static List findall(object pattern, string str)
  { List ret = new List();
    foreach(Match m in MakeRegex(pattern).Matches(str)) ret.append(MatchToFind(m));
    return ret;
  }
  
  [DocString(@"finditer(pattern, string)

Return an iterator over all non-overlapping matches in the string.
For each match, the iterator returns a match object.

Empty matches are included in the result.")]
  public static IEnumerator finditer(object pattern, string str)
  { return new FindEnumerator(MakeRegex(pattern), str);
  }

  [DocString(@"match(pattern, string[, flags])

Try to apply the pattern at the start of the string, returning a match
object, or null if no match was found.")]
  public static Match match(object pattern, string str) { return match(pattern, str, 0); }
  public static Match match(object pattern, string str, int flags)
  { Match m = search(pattern, str, flags);
    return m.Index==0 ? m : null;
  }

  [DocString(@"search(pattern, string[, flags])

Scan through string looking for a match to the pattern, returning a match
object, or null if no match was found.")]
  public static Match search(object pattern, string str) { return search(pattern, str, 0); }
  public static Match search(object pattern, string str, int flags)
  { Match m = MakeRegex(pattern, (RegexOptions)flags).Match(str);
    return m!=null && m.Success ? m : null;
  }

  [DocString(@"split(pattern, string[, maxsplit])

Split the source string by the occurrences of the pattern. If capturing
parentheses are used in the pattern, then the text of all groups in the
pattern are also returned as part of the resulting list. If maxsplit is
nonzero, at most maxsplit splits occur, and the remainder of the string
is returned as the final element of the list.")]
  public static List split(object pattern, string str) { return split(pattern, str, 0); }
  public static List split(object pattern, string str, int maxsplit)
  { if(maxsplit<0) throw Ops.ValueError("split(): maxsplit must be >= 0");
    if(maxsplit==0) maxsplit=int.MaxValue;
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

  [DocString(@"sub(pattern, repl, string[, int maxreplace])

Return the string obtained by replacing the leftmost non-overlapping
occurrences of the pattern in the source string by the replacement value.
If the pattern isn't found, the string is returned unchanged. The
replacement value can be a string or a function; if it is a string, any
backslash escapes in it are processed. That is, '\n' is converted to a
single newline character, '\r' is converted to a linefeed, and so forth.
Backreferences, such as '\6', are replaced with the substring matched
by group 6 in the pattern.

If repl is a function, it is called for every non-overlapping occurrence
of pattern. It will be passed a single match object argument, and should
return the replacement string.")]
  public static string sub(object pattern, object repl, string str) { return sub(pattern, repl, str, 0); }
  public static string sub(object pattern, object repl, string str, int maxreplace)
  { int dummy;
    return sub(pattern, repl, str, maxreplace, out dummy);
  }

  [DocString(@"subn(pattern, repl, string[, maxreplace])

Performs the same operation as sub(), but returns a tuple
(new_string, number_of_subs_made).")]
  public static Tuple subn(object pattern, object repl, string str) { return subn(pattern, repl, str, 0); }
  public static Tuple subn(object pattern, object repl, string str, int maxreplace)
  { int count;
    return new Tuple(sub(pattern, repl, str, maxreplace, out count), count);
  }

  [DocString(@"Specifies that the regular expression should be compiled, increasing both
creation time and performance.")]
  public const int C=(int)RegexOptions.Compiled, COMPILED=C;

  [DocString(@"Specifies that the regular expression should perform case-insensitive
matching.")]
  public const int I=(int)RegexOptions.IgnoreCase, IGNORE=I;

  [DocString(@"When specified, the pattern character '^' matches at the beginning of the
string and at the beginning of each line (immediately following each
newline); and the pattern character '$' matches at the end of the string
and at the end of each line (immediately preceding each newline). By
default, '^' matches only at the beginning of the string, and '$' only at
the end of the string and immediately before the newline (if any) at the
end of the string.")]
  public const int M=(int)RegexOptions.Multiline, MULTILINE=M;

  [DocString(@"This flag allows you to write regular expressions that look nicer.
Whitespace within the pattern is ignored, except when in a character class
or preceded by an unescaped backslash, and, when a line contains a '#'
neither in a character class or preceded by an unescaped backslash, all
characters from the leftmost such '#' through the end of the line are
ignored.")]
  public const int X=(int)RegexOptions.IgnorePatternWhitespace, VERBOSE=X;

  public static readonly ReflectedType error = ReflectedType.FromType(typeof(RegexErrorException));

  static Regex MakeRegex(object pattern) { return MakeRegex(pattern, RegexOptions.Singleline); }
  static Regex MakeRegex(object pattern, RegexOptions flags)
  { if(pattern is Regex) return (Regex)pattern;
    if(pattern is string)
      try { return new Regex((string)pattern, flags); }
      catch(ArgumentException e) { throw new RegexErrorException("regex parse error: " + e.Message); }
    throw Ops.TypeError("re: expecting either a Regex object or a regex pattern string");
  }

  static object MatchToFind(Match m)
  { GroupCollection groups = m.Groups;
    if(groups.Count<=1) return m.Value;
    else if(groups.Count==2) return groups[1].Value;
    else
    { object[] items = new object[groups.Count-1];
      for(int i=1; i<groups.Count; i++) items[i-1] = groups[i].Value;
      return new Tuple(items);
    }
  }

  static string sub(object pattern, object repl, string str, int maxreplace, out int count)
  { if(maxreplace<0) throw Ops.ValueError("sub(): maxreplace must be >= 0");
    if(maxreplace==0) maxreplace=int.MaxValue;

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