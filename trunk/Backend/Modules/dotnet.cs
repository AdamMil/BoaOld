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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class dotnet
{ dotnet() { }

  public static string __repr__() { return "<module 'dotnet' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static bool access(string path, int mode)
  { throw new NotImplementedException();
  }

  public static void chdir(string path)
  { if(path==null || path=="") throw Ops.ValueError("chdir(): path cannot be null or empty");
    try { Environment.CurrentDirectory = path; }
    catch(FileNotFoundException) { throw NotFound(path); }
    catch(DirectoryNotFoundException) { throw NotFound(path); }
    catch(IOException e) { throw Ops.OSError(e.Message); }
  }
  
  public static void chmod(string path, int mode)
  { throw new NotImplementedException();
  }

  public static string getcwd() { return Environment.CurrentDirectory; }
  public static string getcwdu() { return Environment.CurrentDirectory; }

  public static int getpid() { return Process.GetCurrentProcess().Id; }

  public static object getenv(string name) { return environ.get(name); }
  public static object getenv(string name, object defaultValue) { return environ.get(name, defaultValue); }

  public static List listdir(string path)
  { if(path==null || path=="") throw Ops.ValueError("listdir(): path cannot be null or empty");
    List ret = new List();
    try
    { foreach(string s in Directory.GetDirectories(path)) ret.append(Path.GetFileName(s));
      foreach(string s in Directory.GetFiles(path)) ret.append(Path.GetFileName(s));
    }
    catch(DirectoryNotFoundException) { throw NotFound(path); }
    catch(IOException e) { throw Ops.OSError(e.Message); }
    return ret;
  }

  public static void makedirs(string path) { makedirs(path, 0777); }
  public static void makedirs(string path, int mode)
  { if(path==null || path=="") throw Ops.ValueError("makedirs(): path cannot be null or empty");
    try { Directory.CreateDirectory(path); }
    catch(DirectoryNotFoundException) { throw Invalid(path); }
    catch(IOException e) { throw Ops.OSError(e.Message); }
  }

  public static void mkdir(string path) { makedirs(path, 0777); }
  public static void mkdir(string path, int mode) { makedirs(path, mode); }

  public static BoaFile popen(string command) { return popen(command, "t", -1); }
  public static BoaFile popen(string command, string mode) { return popen(command, mode, -1); }
  public static BoaFile popen(string command, string mode, int bufsize)
  { throw new NotImplementedException();
  }

  public static Tuple popen2(string command) { return popen2(command, "t", -1); }
  public static Tuple popen2(string command, string mode) { return popen2(command, mode, -1); }
  public static Tuple popen2(string command, string mode, int bufsize)
  { throw new NotImplementedException();
  }

  public static Tuple popen3(string command) { return popen3(command, "t", -1); }
  public static Tuple popen3(string command, string mode) { return popen3(command, mode, -1); }
  public static Tuple popen3(string command, string mode, int bufsize)
  { throw new NotImplementedException();
  }

  public static Tuple popen4(string command) { return popen4(command, "t", -1); }
  public static Tuple popen4(string command, string mode) { return popen4(command, mode, -1); }
  public static Tuple popen4(string command, string mode, int bufsize)
  { throw new NotImplementedException();
  }

  public static void remove(string path)
  { if(path==null || path=="") throw Ops.ValueError("remove(): path cannot be null or empty");
    try { File.Delete(path); }
    catch(DirectoryNotFoundException) { throw Invalid(path); }
    catch(UnauthorizedAccessException) { throw Ops.OSError("{0} is a directory", Ops.Repr(path)); }
    catch(IOException e) { throw Ops.OSError(e.Message); }
  }

  public static void removedirs(string path)
  { if(path==null || path=="") throw Ops.ValueError("removedirs(): path cannot be null or empty");
    DirectoryInfo d;
    try { d = new DirectoryInfo(path); }
    catch(ArgumentException) { throw Invalid(path); }
    catch(IOException e) { throw Ops.OSError(e.Message); }
    
    try { d.Delete(); } catch(IOException e) { throw Ops.OSError(e.Message); }
    try { while((d=d.Parent)!=null) d.Delete(); } catch { }
  }

  public static void rmdir(string path)
  { if(path==null || path=="") throw Ops.ValueError("rmdir(): path cannot be null or empty");
    try { Directory.Delete(path); }
    catch(DirectoryNotFoundException) { throw NotFound(path); }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public static BoaFile tmpfile()
  { throw new NotImplementedException();
  }

  public static string tmpnam() { return Path.GetTempFileName(); }

  public static IEnumerator walk(string path) { return walk(path, true, null); }
  public static IEnumerator walk(string path, bool topDown) { return walk(path, topDown, null); }
  public static IEnumerator walk(string path, bool topDown, object onError)
  { throw new NotImplementedException();
  }

  public static void unlink(string path) { remove(path); }

  public static Dict environ = new Dict(Environment.GetEnvironmentVariables());
  public const int F_OK=1, R_OK=2, W_OK=4, X_OK=8;

  static OSErrorException NotFound(string path)
  { return Ops.OSError("no such file or directory {0}", Ops.Repr(path));
  }
  static OSErrorException Invalid(string path)
  { return Ops.OSError("invalid component in path {0}", Ops.Repr(path));
  }
}

} // namespace Boa.Modules