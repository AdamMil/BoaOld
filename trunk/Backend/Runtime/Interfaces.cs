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

// TODO: respect this: http://rgruet.free.fr/PQR2.3.html

namespace Boa.Runtime
{

public interface ICallable
{ object Call(params object[] args);
}
public interface IFancyCallable : ICallable
{ object Call(object[] positional, string[] names, object[] values);
}

public interface IContainer
{ int __len__();
  bool __contains__(object value);
}

public interface IDescriptor
{ object __get__(object instance);
}

public interface IDataDescriptor : IDescriptor
{ void __set__(object instance, object value);
  void __delete__(object instance);
}

public interface IDynamicObject
{ DynamicType GetDynamicType();
}

#region IFile
public interface IFile
{ bool canread { get; }
  bool canseek { get; }
  bool canwrite { get; }
  bool closed { get; }
  System.Text.Encoding encoding { get; set; }
  int  length { get; }
  void close();
  void flush();
  bool isatty();
  string next();
  byte[] read();
  byte[] read(int bytes);
  string readstr();
  string readstr(int bytes);
  int readbyte();
  string readline();
  string readline(int size);
  List readlines();
  List readlines(int sizehint);
  int seek(int offset);
  int seek(int offset, int whence);
  int tell();
  void truncate();
  void truncate(int size);
  void write(byte[] bytes);
  void write(string str);
  void writebyte(int value);
  void writelines(object sequence);
}
#endregion

public interface IHasAttributes
{ List __attrs__();
  object __getattr__(string key);
  void __setattr__(string key, object value);
  void __delattr__(string key);
}

public interface IInstance : IDynamicObject
{ Dict __dict__ { get; }
  UserType __class__ { get; set; }
}

public interface IRepresentable
{ string __repr__();
}

public interface ISequence : IContainer
{ object __add__(object o);
  object __getitem__(int index);
  object __getitem__(Slice slice);
}

public interface IMutableSequence : ISequence
{ void __delitem__(int index);
  void __delitem__(Slice slice);
  void __setitem__(int index, object value);
  void __setitem__(Slice slice, object value);
}

public interface IMapping : IContainer
{ void clear();
  object copy();
  
  object get(object key);
  object get(object key, object defaultValue);

  bool has_key(object key);
  
  //static object fromkeys(object seq);
  //static object fromkeys(object seq, object value);

  object pop(object key);
  object pop(object key, object defaultValue);
  Tuple popitem();
  
  object setdefault(object key);
  object setdefault(object key, object defaultValue);
  
  void update(object dict);

  List items();
  List keys();
  List values();

  IEnumerator iteritems();
  IEnumerator iterkeys();
  IEnumerator itervalues();

  void __delitem__(object key);
  object __getitem__(object key);
  void __setitem__(object key, object value);
}

} // namespace Boa.Runtime
