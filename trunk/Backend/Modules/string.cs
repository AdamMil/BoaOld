using System;
using System.Collections;
using System.Text;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class @string
{ @string() { }

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 'string' (built-in)>"; }

  public static string capitalize(string word)
  { if(word.Length==0) return word;
    StringBuilder sb = new StringBuilder(word.Length);
    sb.Append(char.ToUpper(word[0]));
    sb.Append(word.Substring(1).ToLower());
    return sb.ToString();
  }

  public static string capwords(string s)
  { List list = split(s);
    for(int i=0; i<list.Count; i++) list[i] = capitalize((string)list[i]);
    return join(list, " ");
  }

  public static string center(string s, int width)
  { width -= s.Length;
    if(width<2) return s;

    string pad = new string(' ', width/2);
    pad += s + pad;
    if((width&1)!=0) pad += ' ';
    return pad;
  }

  public static int count(string s, string sub) { return count(s, sub, 0, s.Length-1); }
  public static int count(string s, string sub, int start) { return count(s, sub, start, s.Length-1); }
  public static int count(string s, string sub, int start, int end)
  { start = Ops.FixIndex(start, s.Length);
    end   = Ops.FixIndex(end, s.Length);
    if(end<start) throw Ops.ValueError("count(): end must be >= start");
    if(start!=0 || end!=s.Length-1) s = s.Substring(start, end-start+1);

    int count=0, pos;
    while(start<=end && (pos=s.IndexOf(sub, start))!=-1) { count++; start=pos+sub.Length; }
    return count;
  }

  public static string expandtabs(string s) { return expandtabs(s, 8); }
  public static string expandtabs(string s, int tabwidth)
  { throw new NotImplementedException("this is more complicated than it seems at first");
  }

  public static string maketrans(string from, string to)
  { if(from.Length!=to.Length) throw Ops.ValueError("maketrans(): 'from' and 'to' must be the same length");
    char[] chars = new char[256];
    for(int i=0; i<256; i++) chars[i] = (char)i;
    for(int i=0; i<from.Length; i++) if(from[i]<256) chars[(int)from[i]] = to[i];
    return new string(chars);
  }

  public static int find(string s, string sub) { return find(s, sub, 0, s.Length-1); }
  public static int find(string s, string sub, int start) { return find(s, sub, start, s.Length-1); }
  public static int find(string s, string sub, int start, int end)
  { start = Ops.FixIndex(start, s.Length);
    end   = Ops.FixIndex(end, s.Length);
    if(end<start) throw Ops.ValueError("count(): end must be >= start");
    if(start!=0 || end!=s.Length) s = s.Substring(start, end-start+1);
    return s.IndexOf(sub);
  }

  public static int index(string s, string sub) { return index(s, sub, 0, s.Length-1); }
  public static int index(string s, string sub, int start) { return index(s, sub, start, s.Length-1); }
  public static int index(string s, string sub, int start, int end)
  { int pos = find(s, sub, start, end);
    if(pos==-1) throw Ops.ValueError("index(): substring not found");
    return pos;
  }

  public static string ljust(string s, int width) { return s.Length>=width ? s : s.PadRight(width); }
  public static string lower(string s) { return s.ToLower(); }

  public static string lstrip(string s) { return lstrip(s, whitespace); }
  public static string lstrip(string s, string ws) { return s.TrimStart(ws.ToCharArray()); }

  public static string join(object words) { return join(words, " "); }
  public static string join(object words, string sep)
  { IEnumerator e = Ops.GetEnumerator(words);
    StringBuilder sb = new StringBuilder();
    bool did=false;
    while(e.MoveNext())
    { if(did) sb.Append(sep);
      sb.Append(Ops.Str(e.Current));
      did = true;
    }
    return sb.ToString();
  }

  public static string replace(string s, string old, string @new) { return replace(s, old, @new, 0); }
  public static string replace(string s, string old, string @new, int maxreplace)
  { if(maxreplace<0) throw Ops.ValueError("replace(): 'maxreplace' should not be negative");
    if(maxreplace==0) return s.Replace(old, @new);
    StringBuilder sb = new StringBuilder();
    int pos=0, opos=0, reps=0;
    while(pos<s.Length && (pos=s.IndexOf(old, pos))!=-1)
    { sb.Append(s.Substring(opos, pos-opos));
      opos = pos = pos+old.Length;
      sb.Append(@new);
      if(++reps>=maxreplace) break;
    }
    if(opos<s.Length) sb.Append(s.Substring(opos));
    return sb.ToString();
  }

  public static int rfind(string s, string sub) { return rfind(s, sub, 0, s.Length-1); }
  public static int rfind(string s, string sub, int start) { return rfind(s, sub, start, s.Length-1); }
  public static int rfind(string s, string sub, int start, int end)
  { start = Ops.FixIndex(start, s.Length);
    end   = Ops.FixIndex(end, s.Length);
    if(end<start) throw Ops.ValueError("count(): end must be >= start");
    if(start!=0 || end!=s.Length) s = s.Substring(start, end-start+1);
    return s.LastIndexOf(sub);
  }

  public static int rindex(string s, string sub) { return rindex(s, sub, 0, s.Length-1); }
  public static int rindex(string s, string sub, int start) { return rindex(s, sub, start, s.Length-1); }
  public static int rindex(string s, string sub, int start, int end)
  { int pos = rfind(s, sub, start, end);
    if(pos==-1) throw Ops.ValueError("rindex(): substring not found");
    return pos;
  }

  public static string rjust(string s, int width) { return s.Length>=width ? s : s.PadLeft(width); }
  public static string rstrip(string s) { return rstrip(s, whitespace); }
  public static string rstrip(string s, string ws) { return s.TrimEnd(ws.ToCharArray()); }

  public static List split(string s) { return split(s, whitespace, 0); }
  public static List split(string s, string sep) { return split(s, sep, 0); }
  public static List split(string s, string sep, int maxsplit)
  { if(maxsplit<0) throw Ops.ValueError("split(): 'maxsplit' should not be negative");
    if(sep==null || sep=="")
    { List ret = new List(maxsplit==0 ? s.Length : maxsplit<s.Length ? maxsplit+1 : maxsplit);
      int i=0, end=Math.Min(maxsplit, s.Length);
      for(; i<end; i++) ret.append(new string(s[i], 1));
      if(i<s.Length) ret.append(s.Substring(i));
      return ret;
    }
    else
    { List ret = new List();
      int start=0, splits=0;
      for(int i=0; i<s.Length; i++)
      { char c = s[i];
        for(int j=0; j<sep.Length; j++)
          if(c==sep[j])
          { ret.append(s.Substring(start, i-start));
            start = i+1;
            if(maxsplit>0 && ++splits==maxsplit) goto done;
            break;
          }
      }
      done:
      if(start<s.Length) ret.append(start==0 ? s : s.Substring(start));
      return ret;
    }
  }

  public static string strip(string s) { return strip(s, whitespace); }
  public static string strip(string s, string ws) { return s.Trim(ws.ToCharArray()); }

  public static string swapcase(string s)
  { StringBuilder sb = new StringBuilder(s.Length);
    for(int i=0; i<s.Length; i++)
    { char u = char.ToUpper(s[i]);
      sb.Append(u==s[i] ? char.ToLower(s[i]) : u);
    }
    return sb.ToString();
  }

  public static byte[] tobytes(string s) { return Encoding.ASCII.GetBytes(s); }
  public static byte[] tobytes(string s, Encoding e) { return e.GetBytes(s); }

  public static string tostring(byte[] bytes) { return Encoding.ASCII.GetString(bytes); }
  public static string tostring(byte[] bytes, int offset, int length)
  { return Encoding.ASCII.GetString(bytes, offset, length);
  }

  public static string tostring(char[] chars) { return new string(chars); }
  public static string tostring(char[] chars, int offset, int length) { return new string(chars, offset, length); }

  public static string translate(string s, string table) { return translate(s, table, null); }
  public static string translate(string s, string table, string deletechars)
  { if(table.Length!=256) throw Ops.ValueError("translate() requires a 256-character table");
    StringBuilder sb = new StringBuilder(s.Length);
    if(deletechars==null)
      for(int i=0; i<s.Length; i++)
      { char c = s[i];
        sb.Append(table[c>255 ? 0 : (int)c]);
      }
    else
      for(int i=0; i<s.Length; i++)
      { char c = s[i];
        for(int j=0; j<deletechars.Length; i++) if(c==deletechars[j]) goto next;
        sb.Append(table[c>255 ? 0 : (int)c]);
        next:;
      }
    return sb.ToString();
  }

  public static string upper(string s) { return s.ToUpper(); }
  
  public static string zfill(string s, int width)
  { if(s.Length>=width) return s;
    string pad = new string('0', width-s.Length);
    return s[0]=='-' || s[0]=='+' ? s[0].ToString()+pad+s.Substring(1) : pad+s;
  }

  public static string ascii_lowercase = "abcdefghijklmnopqrstuvwxyz";
  public static string ascii_uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  public static string ascii_letters = ascii_lowercase+ascii_uppercase;
  public static string digits = "0123456789";
  public static string hexdigits = "0123456789abcdefABCDEF";
  public static string letters = ascii_letters; // TODO: locale-dependent
  public static string lowercase = ascii_lowercase; // TODO: locale-dependent
  public static string octdigits = "01234567";
  public static string punctuation = "~`!@#$%^&*()-=_+[]{};:,<.>'\"/?\\|";
  public static string uppercase = ascii_uppercase; // TODO: locale-dependent
  public static string whitespace = " \t\n\r\f\v";
  public static string printable = digits+letters+punctuation+whitespace; // TODO: locale-dependent
}

} // namespace Boa.Modules
