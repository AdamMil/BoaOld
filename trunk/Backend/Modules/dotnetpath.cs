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
using System.IO;
using System.Text.RegularExpressions;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class dotnetpath
{ dotnetpath() { }

  public static string __repr__() { return "<module 'dotnetpath' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static string abspath(string path) { return normpath(join(dotnet.getcwd(), path)); }

  public static string basename(string path)
  { string basePart, namePart;
    split(path, out basePart, out namePart);
    return namePart;
  }
  
  public static string commonprefix(object seq)
  { ArrayList list = new ArrayList();
    IEnumerator e = Ops.GetEnumerator(seq);
    while(e.MoveNext()) list.Add(Ops.ToString(e.Current));
    if(list.Count==0) return string.Empty;

    string[] strings = (string[])list.ToArray(typeof(string));
    string ret = string.Empty;

    for(int ci=0; ci<strings.Length; ci++)
    { if(strings[0].Length==ci) goto done;
      char c = strings[0][ci];

      for(int i=1; i<strings.Length; i++)
      { string s = strings[i];
        if(s.Length==ci || s[ci]!=c) goto done;
      }

      ret += c;
    }
    done: return ret;
  }
  
  public static string dirname(string path)
  { string basePart, namePart;
    split(path, out basePart, out namePart);
    return basePart;
  }
  
  public static bool exists(string path) { return File.Exists(path) || Directory.Exists(path); }

  public static string expanduser(string path)
  { string home = dotnet.environ["HOME"] as string;
    if(home==null) return path;
    if(path=="~") return home;
    if(path.StartsWith("~"+Path.DirectorySeparatorChar) || path.StartsWith("~"+Path.AltDirectorySeparatorChar))
      return home + path.Substring(1);
    // TODO: implement user directory lookups
    return path;
  }
  
  public static string expandvars(string path) { return varre.Replace(path, new MatchEvaluator(VarReplace)); }

  public static long getatime(string path)
  { if(File.Exists(path)) return (long)_time.fromDateTime(File.GetLastAccessTime(path));
    else if(Directory.Exists(path)) return (long)_time.fromDateTime(Directory.GetLastAccessTime(path));
    else throw new FileNotFoundException("path not found: "+path);
  }
  
  public static long getctime(string path)
  { if(File.Exists(path)) return (long)_time.fromDateTime(File.GetCreationTime(path));
    else if(Directory.Exists(path)) return (long)_time.fromDateTime(Directory.GetCreationTime(path));
    else throw new FileNotFoundException("path not found: "+path);
  }

  public static long getmtime(string path)
  { if(File.Exists(path)) return (long)_time.fromDateTime(File.GetLastWriteTime(path));
    else if(Directory.Exists(path)) return (long)_time.fromDateTime(Directory.GetLastWriteTime(path));
    else throw new FileNotFoundException("path not found: "+path);
  }
  
  public static long getsize(string path) { return new FileInfo(path).Length; }
  public static bool isabs(string path) { return Path.IsPathRooted(path); }
  public static bool isdir(string path) { return Directory.Exists(path); }
  public static bool isfile(string path) { return File.Exists(path); }
  public static bool islink(string path) { return false; }
  public static bool ismount(string path) { return false; }
  public static bool lexists(string path) { return exists(path); }
  
  public static string join(string path, params string[] paths)
  { for(int i=0; i<paths.Length; i++) path = Path.Combine(path, paths[i]);
    return path;
  }
  
  public static string normcase(string path)
  { return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLower();
  }
  
  public static string normpath(string path)
  { return dotre.Replace(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), "");
  }
  
  public static string realpath(string path) { return path; }

  public static bool samefile(string path1, string path2)
  { if(path1==null || path2==null) throw Ops.OSError("samefile(): path cannot be null");

    try { path1 = Path.GetFullPath(normpath(path1)).ToLower(); }
    catch(ArgumentException) { throw dotnet.Invalid(path1); }
    try { path2 = Path.GetFullPath(normpath(path2)).ToLower(); }
    catch(ArgumentException) { throw dotnet.Invalid(path2); }

    return path1==path2;
  }

  public static Tuple split(string path)
  { string basePart, namePart;
    split(path, out basePart, out namePart);
    return new Tuple(basePart, namePart);
  }

  public static Tuple splitdrive(string path)
  { if(path.Length<2 || !char.IsLetter(path[0]) || path[1]!=':') return new Tuple(string.Empty, path);
    return new Tuple(path.Substring(0, 2), path.Substring(2));
  }
  
  public static Tuple splitext(string path)
  { int index = path.LastIndexOf('.');
    if(index==-1 ||
       index<path.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }))
      return new Tuple(path, string.Empty);
    return index==0 ? new Tuple(string.Empty, path) : new Tuple(path.Substring(0, index), path.Substring(index));
  }
  
  public void walk(string path, object visit, object arg) { throw new NotImplementedException(); }

  static void split(string path, out string basePart, out string namePart)
  { path = normpath(path);
    int index = path.LastIndexOf(Path.DirectorySeparatorChar);
    if(index==-1)
    { basePart=string.Empty;
      namePart=path;
    }
    else if(index==path.Length-1)
    { basePart=path;
      namePart=string.Empty;
    }
    else
    { index++;
      basePart=path.Substring(0, index);
      namePart=path.Substring(index);
    }
  }

  static string VarReplace(Match m)
  { string var = dotnet.environ[m.Groups["var"].Value] as string;
    return var==null ? string.Empty : var;
  }

  public static bool supports_unicode_filenames = true;

  static readonly Regex dotre =
    new Regex(@"(?<=\X)(?:\.\X|\.$|\X)|^\.\X|[^\X]+\X\.\.\X|[^\X]+\X\.\.$".Replace('X', Path.DirectorySeparatorChar),
              RegexOptions.Compiled|RegexOptions.Singleline);
  static readonly Regex varre = new Regex(@"\$(?:\{(?<var>.*?)\}|(?<var>\w+))",
                                          RegexOptions.Compiled|RegexOptions.Singleline);
}

} // namespace Boa.Modules