/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

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
using System.Text.RegularExpressions;
using Boa.Runtime;

namespace Boa.Modules
{

public sealed class re_internal
{ 
  #region match
  public sealed class match
  { public static string expand(Match m, string template) { throw new NotImplementedException(); }

    public static object group(Match m, params object[] groups)
    { if(groups.Length==0) return m.Value;
      if(groups.Length==1)
      { Group g = GetGroup(m, groups[0]);
        return g.Success ? g.Value : null;
      }

      object[] ret = new object[groups.Length];
      for(int i=0; i<groups.Length; i++)
      { Group g = GetGroup(m, groups[i]);
        ret[i] = g.Success ? g.Value : null;
      }
      return new Tuple(ret);
    }

    public static Tuple groups(Match m) { return groups(m, null); }
    public static Tuple groups(Match m, object defaultValue)
    { object[] ret = new object[m.Groups.Count-1];
      for(int i=0; i<ret.Length; i++) ret[i] = m.Groups[i+1].Success ? m.Groups[i+1].Value : null;
      return new Tuple(ret);
    }

    public static Dict groupdict(Match m) { return groupdict(m, null); }
    public static Dict groupdict(Match m, object defaultValue) { throw new NotImplementedException(); }

    public static int start(Match m) { return m.Index; }
    public static int start(Match m, object group)
    { if(group==null) return m.Index;
      Group g = GetGroup(m, group);
      return g.Success ? g.Index : -1;
    }

    public static int end(Match m) { return m.Index+m.Length; }
    public static int end(Match m, object group)
    { if(group==null) return m.Index+m.Length;
      Group g = GetGroup(m, group);
      return g.Success ? g.Index+g.Length : -1;
    }

    public static Tuple span(Match m) { return new Tuple(m.Index, m.Index+m.Length); }
    public static Tuple span(Match m, object group)
    { if(group==null) return new Tuple(m.Index, m.Index+m.Length);
      Group g = GetGroup(m, group);
      return g.Success ? new Tuple(g.Index, g.Index+g.Length) : new Tuple(-1, -1);
    }

    public static object get_lastindex(Match m)
    { for(int i=m.Groups.Count-1; i>0; i--) if(m.Groups[i].Success) return i;
      return null;
    }

    public static string get_lastgroup(Match m) { throw new NotImplementedException(); }

    static Group GetGroup(Match m, object g) { return g is int ? m.Groups[(int)g] : m.Groups[Ops.ToString(g)]; }
  }
  #endregion
}

// TODO: optimize by using Match.NextMatch()

[BoaType("module")]
public sealed class _re
{ _re() { }

  #region FindEnumerator
  public class FindEnumerator : IEnumerator
  { public FindEnumerator(Regex regex, string str) { this.regex=regex; this.str=str; state=State.BOF; }
  
    public object Current
    { get
      { if(state!=State.IN) throw new InvalidOperationException();
        return match;
      }
    }
    
    public bool MoveNext()
    { if(state==State.EOF) return false;
      if(state==State.BOF) { match=regex.Match(str); state=State.IN; }
      else match=match.NextMatch();
      if(!match.Success) { state=State.EOF; return false; }
      return true;
    }

    public void Reset() { state=State.BOF; }

    enum State { BOF, IN, EOF };

    Regex regex;
    string str;
    Match match;
    State state;
  }
  #endregion

  #region regex
  public class regex : Regex, IRepresentable
  { public regex(string pattern, RegexOptions options) : base(pattern, options) { }
    
    [DocString("flags -> int\nThe flags used to create this regex.")]
    public int flags { get { return (int)Options; } }

    [DocString("pattern -> str\nThe pattern used to create this regex.")]
    public new string pattern { get { return base.pattern; } }

    [DocString("groupindex -> dict\nA dictionary mapping group names to group numbers.")]
    public Dict groupindex
    { get
      { if(groups==null)
        { groups = new Dict();
          string[] names = GetGroupNames();
          for(int i=0; i<names.Length; i++) groups[names[i]] = GroupNumberFromName(names[i]);
        }
        return groups;
      }
    }

    [DocString(@"findall(string) -> list\n\nSee documentation for re.findall()")]
    public List findall(string str) { return Boa.Modules._re.findall(this, str); }
    [DocString(@"finditer(string) -> iter\n\nSee documentation for re.finditer()")]
    public IEnumerator finditer(string str) { return Boa.Modules._re.finditer(this, str); }

    [DocString(@"match(string[, start[, end]]) -> Match

  If zero or more characters at the beginning of 'string' match this regular
  expression, return a corresponding Match instance. Return null if the string
  does not match the pattern; note that this is different from a zero-length
  match.

  Note: If you want to locate a match anywhere in 'string', use search()
  instead.

  The optional second parameter pos gives an index in the string where the
  search is to start; it defaults to 0.

  The optional parameter endpos limits how far the string will be searched;
  it will be as if the string is endpos characters long, so only the
  characters from pos to endpos - 1 will be searched for a match. If endpos is
  less than pos, no match will be found.")]
    public Match match(string str) { return match(str, 0, str.Length); }
    public Match match(string str, int start) { return match(str, start, str.Length); }
    public Match match(string str, int start, int end)
    { Match m = end==str.Length ? Match(str, start) : Match(str, start, end-start);
      return m.Success && m.Index==start ? m : null;
    }
    
    public Match search(string str)
    { Match m = Match(str);
      return m.Success ? m : null;
    }
    public Match search(string str, int start)
    { Match m = Match(str, start);
      return m.Success ? m : null;
    }
    public Match search(string str, int start, int end)
    { Match m = Match(str, start, end-start);
      return m.Success ? m : null;
    }
    
    [DocString(@"split(string[, maxsplit=0]) -> list\n\nSee documentation for re.split()")]
    public List split(string str) { return Boa.Modules._re.split(this, str, 0); }
    public List split(string str, int maxsplit) { return Boa.Modules._re.split(this, str, maxsplit); }

    [DocString(@"sub(repl, string [, maxreplace=0]) -> str\n\nSee documentation for re.sub()")]
    public string sub(object repl, string str) { return Boa.Modules._re.sub(this, repl, str, 0); }
    public string sub(object repl, string str, int maxreplace)
    { return Boa.Modules._re.sub(this, repl, str, maxreplace);
    }

    [DocString(@"subn(repl, string [, maxreplace=0]) -> tuple\n\nSee documentation for re.subn()")]
    public Tuple subn(object repl, string str) { return Boa.Modules._re.subn(this, repl, str, 0); }
    public Tuple subn(object repl, string str, int maxreplace)
    { return Boa.Modules._re.subn(this, repl, str, maxreplace);
    }

    public string __repr__() { return string.Format("re.compile({0})", Ops.Repr(base.pattern)); }

    Dict groups;
  }
  #endregion

  #region RegexErrorException
  [DocString(@"Exception raised when a string passed to one of the functions here is not a
valid regular expression (for example, it might contain unmatched
parentheses) or when some other error occurs during compilation or matching.
It is never an error if a string contains no match for a pattern.")]
  public class RegexErrorException : ValueErrorException
  { public RegexErrorException(string message) : base(message) { }
  }
  #endregion
  
  public static readonly ReflectedType MatchObject = ReflectedType.FromType(typeof(Match));

  public static string __repr__() { return "<module 're' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  [DocString(@"compile(pattern[, flags]) -> regex

Compile a regular expression pattern, returning a Regex object.")]
  public static regex compile(string pattern) { return compile(pattern, (int)RegexOptions.Singleline); }
  public static regex compile(string pattern, int flags)
  { return MakeRegex(pattern, (RegexOptions)flags | RegexOptions.Compiled);
  }

  [DocString(@"escape(pattern) -> str

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

  [DocString(@"findall(pattern, string) -> list

Return a list of all non-overlapping matches in the string.

If one or more groups are present in the pattern, return a list of groups;
this will be a list of tuples if the pattern has more than one group.

Empty matches are included in the result.")]
  public static List findall(object pattern, string str)
  { List ret = new List();
    foreach(Match m in MakeRegex(pattern).Matches(str)) ret.append(MatchToFind(m));
    return ret;
  }
  
  [DocString(@"finditer(pattern, string) -> iter

Return an iterator over all non-overlapping matches in the string.
For each match, the iterator returns a match object.

Empty matches are included in the result.")]
  public static IEnumerator finditer(object pattern, string str)
  { return new FindEnumerator(MakeRegex(pattern), str);
  }

  [DocString(@"match(pattern, string[, flags]) -> Match

Try to apply the pattern at the start of the string, returning a match
object, or null if no match was found.")]
  public static Match match(object pattern, string str) { return match(pattern, str, 0); }
  public static Match match(object pattern, string str, int flags)
  { Match m = search(pattern, str, flags);
    return m!=null && m.Index==0 ? m : null;
  }

  [DocString(@"search(pattern, string[, flags]) -> Match

Scan through string looking for a match to the pattern, returning a match
object, or null if no match was found.")]
  public static Match search(object pattern, string str) { return search(pattern, str, 0); }
  public static Match search(object pattern, string str, int flags)
  { Match m = MakeRegex(pattern, (RegexOptions)flags).Match(str);
    return m!=null && m.Success ? m : null;
  }

  [DocString(@"split(pattern, string[, maxsplit=0]) -> list

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

  [DocString(@"sub(pattern, repl, string[, maxreplace=0]) -> str

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

  [DocString(@"subn(pattern, repl, string[, maxreplace]) -> tuple

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
  public const int I=(int)RegexOptions.IgnoreCase, IGNORECASE=I;

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

  static regex MakeRegex(object pattern) { return MakeRegex(pattern, RegexOptions.Singleline); }
  static regex MakeRegex(object pattern, RegexOptions flags)
  { if(pattern is regex) return (regex)pattern;
    if(pattern is string)
      try { return new regex((string)pattern, flags); }
      catch(ArgumentException e) { throw new RegexErrorException("regex parse error: " + e.Message); }
    throw Ops.TypeError("re: expecting either a regex object or a regex pattern string");
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
