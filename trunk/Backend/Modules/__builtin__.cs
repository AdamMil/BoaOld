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
using Boa.AST;
using Boa.Runtime;

// TODO: make these functions conform to their docstrings
// TODO: allow functions to return 'longint' if they'd overflow an 'int'
// TODO: docstrings on fields and simple properties don't work because evaluating the attribute retrieves the value
// TODO: implement reversed(): http://python.org/peps/pep-0322.html
// TODO: add help() for functions created in boa
// TODO: make range() and xrange() work on arbitrary integers, not only int32s
namespace Boa.Modules
{

[BoaType("module")]
public sealed class __builtin__
{ __builtin__() { }

  #region EnumerateEnumerator
  public class EnumerateEnumerator : IEnumerator
  { public EnumerateEnumerator(IEnumerator e) { this.e = e; }

    public object Current
    { get
      { if(current==null) throw new InvalidOperationException();
        return current;
      }
    }

    public bool MoveNext()
    { if(!e.MoveNext()) { current=null; return false; }
      current = new Tuple(index++, e.Current);
      return true;
    }

    public void Reset()
    { e.Reset();
      index = 0;
    }
    
    IEnumerator e;
    Tuple current;
    int index;
  }
  #endregion
  
  #region SentinelEnumerator
  public class SentinelEnumerator : IEnumerator
  { public SentinelEnumerator(IEnumerator e, object sentinel) { this.e=e; this.sentinel=sentinel; }

    public object Current
    { get
      { if(done) throw new InvalidOperationException();
        return e.Current;
      }
    }

    public bool MoveNext()
    { if(!e.MoveNext() || Ops.Compare(e.Current, sentinel)==0) { done=true; return false; }
      return true;
    }

    public void Reset() { e.Reset(); done = false; }

    IEnumerator e;
    object sentinel;
    bool done;
  }
  #endregion
  
  #region XRange
  [BoaType("xrange")]
  [DocString(@"xrange([start,] stop[, step])
This function is very similar to range(), but returns an 'xrange object'
instead of a list. This is an opaque sequence type which yields the same
values as the corresponding list, without actually storing them all
simultaneously. xrange() is useful when when working with very large
sequences or when many of a range's elements are never used.")]
  public class XRange : IEnumerable, ISequence, IRepresentable
  { public XRange(int stop) : this(0, stop, 1) { }
    public XRange(int start, int stop) : this(start, stop, 1) { }
    public XRange(int start, int stop, int step)
    { if(step==0) throw Ops.ValueError("step of 0 passed to xrange()");
      this.start=start; this.stop=stop; this.step=step;
      if(step<0 && start<=stop || step>0 && start>=stop) length = 0;
      else
      { int sign = Math.Sign(step);
        length = (stop-start+step-sign)/step;
      }
    }

    public override string ToString() { return __repr__(); }

    #region IEnumerable Members
    public IEnumerator GetEnumerator() { return new XRangeEnumerator(start, stop, step); }
    #endregion
    
    #region ISequence Members
    public object __add__(object o) { throw Ops.TypeError("xrange concatenation is not supported"); }

    public object __getitem__(int index)
    { if(index<0 || index>=length) throw Ops.IndexError("xrange object index out of range");
      return start + step*index;
    }

    public object __getitem__(Slice slice) { return Ops.SequenceSlice(this, slice); }

    public int __len__() { return length; }

    public bool __contains__(object value)
    { int val   = Ops.ToInt(value);
      int index = (val-start)/step;
      if(index<0 || index>=length || index*step!=val-start) return false;
      return true;
    }
    #endregion

    #region IRepresentable Members
    public string __repr__() { return string.Format("xrange({0}, {1}, {2})", start, stop, step); }
    #endregion

    int start, stop, step, length;
  }

  public class XRangeEnumerator : IEnumerator
  { public XRangeEnumerator(int start, int stop, int step)
    { this.start=start; this.stop=stop; this.step=step; current=start-step;
    }

    public object Current
    { get
      { if(step<0)
        { if(current<=stop) throw new InvalidOperationException();
        }
        else if(current>=stop) throw new InvalidOperationException();
        return current;
      }
    }
    
    public bool MoveNext()
    { if(step<0)
      { if(current<=stop+step) return false;
      }
      else if(current>=stop-step) return false;
      current += step;
      return true;
    }

    public void Reset() { current=start-step; }

    internal int start, stop, step, current;
  }
  #endregion

  public static string __repr__() { return "<module '__builtin__' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  [DocString(@"abs(object) -> object

Return the absolute value of a number. The argument may be a plain or long
integer or a floating point number. If the argument is a complex number, its
magnitude is returned.")]
  public static object abs(object o)
  { switch(Convert.GetTypeCode(o))
    { case TypeCode.Boolean: return (bool)o ? 1 : 0;
      case TypeCode.Byte: case TypeCode.UInt16: case TypeCode.UInt32: case TypeCode.UInt64: return o;
      case TypeCode.Decimal: return Math.Abs((Decimal)o);
      case TypeCode.Double: return Math.Abs((double)o);
      case TypeCode.Int16: return Math.Abs((short)o);
      case TypeCode.Int32: return Math.Abs((int)o);
      case TypeCode.Int64: return Math.Abs((long)o);
      case TypeCode.Object:
        if(o is Integer) return ((Integer)o).abs();
        if(o is Complex) return ((Complex)o).abs();
        return Ops.Invoke(o, "__abs__");
      case TypeCode.SByte: return Math.Abs((sbyte)o);
      case TypeCode.Single: return Math.Abs((float)o);
      default: throw Ops.TypeError("invalid operand type for abs(): got '{0}'", Ops.TypeName(o));
    }
  }

  [DocString(@"bool([x]) -> bool

Convert a value to a Boolean, using the standard truth testing procedure.
If x is false or omitted, this returns false; otherwise it returns true.")]
  public static object @bool() { return Ops.FALSE; }
  public static object @bool(object o) { return Ops.FromBool(Ops.IsTrue(o)); }

  [DocString(@"callable(object) -> bool

Return true if the object argument appears callable, false if not. If this
returns true, it is still possible that a call fails, but if it is false,
calling object will never succeed. Note that classes are callable (calling
a class returns a new instance); class instances are callable if they have
a __call__()  method.")]
  public static object callable(object o) { return o is ICallable ? Ops.TRUE : hasattr(o, "__call__"); }

  
  [DocString(@"chr(i) -> str

Return a string of one character whose ASCII code is the integer passed.
For example, chr(97) returns the string 'a'. This is the inverse of ord().")]
  public static string chr(int value) { return new string((char)value, 1); }

  [DocString(@"cmp(x, y) -> int

Compare the two objects x and y and return an integer according to the
outcome. The return value is negative if x<y, zero if x==y and strictly
positive if x>y.")]
  public static int cmp(object a, object b) { return Ops.Compare(a, b); }

  // TODO: compile(string, filename, kind[, flags[, dont_inherit]])

  [DocString(@"delattr(object, name)

This is a relative of setattr(). The arguments are an object and a string.
The string must be the name of one of the object's attributes. The function
deletes the named attribute, provided the object allows it. For example,
delattr(x, 'foobar') is equivalent to del x.foobar.")]
  public static void delattr(object o, string name) { Ops.DelAttr(o, name); }

  // FIXME: dir() without args should return local variables, not module variables (but this is complicated)
  [DocString(@"dir([object]) -> list

Without arguments, return the list of names in the current local symbol
table. With an argument, attempts to return a list of valid attributes
for that object. This information is gleaned from the object's __dict__
attribute, if defined, and from the class or type object. The list is
not necessarily complete. If the object is a module object, the list
contains the names of the module's attributes. If the object is a type or
class object, the list contains the names of its attributes, and recursively
of the attributes of its bases. Otherwise, the list contains the object's
attributes' names, the names of its class's attributes, and recursively of
the attributes of its class's base classes. The resulting list is sorted
alphabetically.")]
  public static List dir() { return dir(Ops.GetExecutingModule()); }
  public static List dir(object o)
  { List list = Ops.GetAttrNames(o);
    list.sort();
    return list;
  }

  // FIXME: make this conform to the docstring
  [DocString(@"divmod(a, b) -> object

Take two (non complex) numbers as arguments and return a pair of numbers
consisting of their quotient and remainder when using long division. With
mixed operand types, the rules for binary arithmetic operators apply. For
plain and long integers, the result is the same as (a / b, a % b). For
floating point numbers the result is (q, a % b), where q is usually
math.floor(a / b) but may be 1 less than that. In any case q * b + a % b
is very close to a, if a % b is non-zero it has the same sign as b, and
0 <= abs(a % b) < abs(b).")]
  public static Tuple divmod(object a, object b) { return Ops.DivMod(a, b); }

  [DocString(@"enumerate(object) -> iter

Return an enumerator object. The argument must be a sequence, an iterator,
or some other object which supports iteration. The next() method of the
iterator returned by enumerate() returns a tuple containing a count (from
zero) and the corresponding value obtained from iterating over iterable.
enumerate() is useful for obtaining an indexed series:
(0, seq[0]), (1, seq[1]), (2, seq[2])")]
  public static IEnumerator enumerate(object o) { return new EnumerateEnumerator(Ops.GetEnumerator(o)); }

  [DocString(@"eval(expression[, globals[, locals]]) -> object

The arguments are a string and two optional dictionaries. The expression
argument is parsed and evaluated as a Boa expression using the globals and
locals dictionaries as global and local name space. If the locals dictionary
is omitted it defaults to the globals dictionary. If both dictionaries are
omitted, the expression is executed in the environment where eval is called.
The return value is the result of the evaluated expression. Syntax errors
are reported as exceptions. Example:

  >>> x = 1
  >>> print eval('x+1')
  2

This function can also be used to execute arbitrary code objects (such as
those created by compile()). In this case pass a code object instead of a
string.

Hints: dynamic execution of statements is supported by the exec statement.
Execution of statements from a file is supported by the execfile() function.
The globals() and locals() functions returns the current global and local
dictionary, respectively, which may be useful to pass around for use by
eval() or execfile().")]
  public static object eval(object expr) { return eval(expr, globals(), locals()); }
  public static object eval(object expr, IDictionary globals) { return eval(expr, globals, globals); }
  public static object eval(object expr, IDictionary globals, IDictionary locals)
  { Frame frame = new Frame(locals, globals);
    try
    { Ops.Frames.Push(frame);
      if(expr is string) return Parser.FromString((string)expr).ParseExpression().Evaluate(frame);
      else throw new NotImplementedException();
    }
    finally { Ops.Frames.Pop(); }
  }

  [DocString(@"exec(code[, globals[, locals]])

This function is similar implements the exec statement.
The arguments are an object and two optional dictionaries.
The return value is null.

'code' should be either a string, an open file object, or a code object.
If it is a string, the string is parsed as a suite of Python statements
which is then executed (unless a syntax error occurs). If it is an open
file, the file is parsed until EOF and executed. If it is a code object,
it is simply executed.

The execution uses the globals and locals dictionaries as the global and
local namespaces. If the locals dictionary is omitted it defaults to the
globals dictionary. If both dictionaries are omitted, the expression is
executed in the environment where execfile() is called. The return value
is null.

Warning: The default locals act as described for function locals():
modifications to the default locals dictionary should not be attempted.
Pass an explicit locals dictionary if you need to see effects of the code on
locals after function execfile() returns. exec() cannot be used reliably
to modify a function's locals.")]
public static void exec(object code) { exec(code, globals(), locals()); }
public static void exec(object code, IDictionary globals) { exec(code, globals, globals); }
public static void exec(object code, IDictionary globals, IDictionary locals)
{ Frame frame = new Frame(locals, globals);
  try
  { Ops.Frames.Push(frame);
    if(code is string) Parser.FromString((string)code).Parse().Execute(frame);
    else throw new NotImplementedException();
  }
  finally { Ops.Frames.Pop(); }
}

  [DocString(@"execfile(filename[, globals[, locals]])

This function is similar to the exec statement, but parses a file instead of
a string. It is different from the import statement in that it does not use
the module administration -- it reads the file unconditionally and does not
create a new module.

The arguments are a file name and two optional dictionaries. The file is
parsed and evaluated as a sequence of Boa statements (similarly to a module)
using the globals and locals dictionaries as global and local namespace. If
the locals dictionary is omitted it defaults to the globals dictionary. If
both dictionaries are omitted, the expression is executed in the environment
where execfile() is called. The return value is null.

Warning: The default locals act as described for function locals():
modifications to the default locals dictionary should not be attempted.
Pass an explicit locals dictionary if you need to see effects of the code on
locals after function execfile() returns. execfile() cannot be used reliably
to modify a function's locals.")]
public static void execfile(string filename) { execfile(filename, globals(), locals()); }
public static void execfile(string filename, IDictionary globals) { execfile(filename, globals, globals); }
public static void execfile(string filename, IDictionary globals, IDictionary locals)
{ Frame frame = new Frame(locals, globals);
  try
  { Ops.Frames.Push(frame);
    Parser.FromFile(filename).Parse().Execute(frame);
  }
  finally { Ops.Frames.Pop(); }
}

  [DocString(@"filter(function, sequence) -> sequence

Construct a sequence from those elements of sequence for which function
returns true. 'sequence' may be either a sequence, a container which
supports iteration, or an iterator, If 'sequence' is a string or a tuple,
the result also has that type; otherwise it is always a list. If function
is null, the truth function is assumed, that is, all elements of list
that are false (zero or empty) are removed.

Note that filter(function, list) is equivalent to
[item for item in list if function(item)] if function is not null and
[item for item in list if item] if function is null.")]
  public static object filter(object function, object seq)
  { if(seq is string)
    { if(function==null) return seq;

      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      string str = (string)seq;
      for(int i=0; i<str.Length; i++)
        if(Ops.IsTrue(Ops.Call(function, new string(str[i], 1)))) sb.Append(str[i]);
      return sb.ToString();
    }
    else
    { List ret;
      ICollection col = seq as ICollection;
      IEnumerator e;
      if(col!=null) { ret=new List(Math.Max(col.Count/2, 16)); e=col.GetEnumerator(); }
      else { ret=new List(); e=Ops.GetEnumerator(seq); }

      if(function==null)
      { while(e.MoveNext()) if(Ops.IsTrue(e.Current)) ret.append(e.Current);
      }
      else while(e.MoveNext()) if(Ops.IsTrue(Ops.Call(function, e.Current))) ret.append(e.Current);
      return seq is Tuple ? ret.ToTuple() : (object)ret;
    }
  }

  [DocString(@"float([value]) -> float

Convert a string or a number to floating point. If the argument is a string,
it must contain a possibly signed decimal or floating point number, possibly
embedded in whitespace. Otherwise, the argument may be a plain or long
integer or a floating point number, and a floating point number with the same
value (within Boa's floating point precision) is returned. If no argument is
given, returns 0.")]
  public static double @float() { return 0.0; }
  public static double @float(string s) { return double.Parse(s); }
  public static double @float(object o) { return Ops.ToFloat(o); }

  [DocString(@"getattr(object, name[, default]) -> object

Return the value of the named attribute of an object. The name must be a
string. If the string is the name of one of the object's attributes, the
result is the value of that attribute. For example, getattr(x, 'foobar') is
equivalent to x.foobar. If the named attribute does not exist, default is
returned if provided, otherwise AttributeError is raised.")]
  public static object getattr(object o, string name) { return Ops.GetAttr(o, name); }
  public static object getattr(object o, string name, object defaultValue)
  { object ret;
    return Ops.GetAttr(o, name, out ret) ? ret : defaultValue;
  }

  [DocString(@"globals() -> dict

Return a dictionary representing the current global symbol table. This is
always the dictionary of the current module (inside a function or method,
this is the module where it is defined, not the module from which it is
called).")]
  public static IDictionary globals() { return Ops.GetExecutingModule().__dict__; }

  [DocString(@"hasattr(object, name) -> bool

The arguments are an object and a string. The result is true if the string
is the name of one of the object's attributes, false if not.")]
  public static object hasattr(object o, string name)
  { object dummy;
    return Ops.FromBool(Ops.GetAttr(o, name, out dummy));
  }

  // FIXME: python says: Numeric values that compare equal have the same hash value (even if they are of different types, as is the case for 1 and 1.0).
  [DocString(@"hash(object) -> int

Return the hash value of the object (if it has one). Hash values are
integers. They are used to quickly compare dictionary keys during a
dictionary lookup.")]
  public static int hash(object o) { return o.GetHashCode(); }

  [DocString(@"help([object])

Invoke the built-in help system (intended for interactive use). If no
argument is given, the interactive help system starts on the interpreter
console. If the argument is a string, then the string is looked up as the
name of a module, function, class, method, keyword, or documentation topic,
and a help page is printed on the console. If the argument is any other kind
of object, a help page on the object is generated.")]
  public static void help() { throw new NotImplementedException(); }
  public static void help(object o)
  { object doc;
    if(Ops.GetAttr(o, "__doc__", out doc) && doc!=null && Ops.Str(doc)!="") Console.WriteLine(doc);
    else if(o is ReflectedEvent)
    { ReflectedEvent re = (ReflectedEvent)o;
      Console.WriteLine("'{0}' is an event that takes a '{1}' object",
                        re.info.Name, typeName(re.info.EventHandlerType));
    }
    else if(o is ReflectedField)
    { ReflectedField rf = (ReflectedField)o;
      Console.WriteLine("'{0}' is a field of type '{1}'", rf.info.Name, typeName(rf.info.FieldType));
    }
    else if(o is ReflectedMethodBase)
    { ReflectedMethodBase rm = (ReflectedMethodBase)o;
      foreach(System.Reflection.MethodBase mb in rm.sigs)
      { Console.Write(rm.__name__);
        Console.Write('(');
        bool first=true;
        foreach(System.Reflection.ParameterInfo pi in mb.GetParameters())
        { if(!first) Console.Write(", ");
          Console.Write("{0} {1}{2}", typeName(pi.ParameterType),
                        pi.IsDefined(typeof(ParamArrayAttribute), false) ? "*" : "", pi.Name);
          if(pi.IsOptional) Console.Write("="+Ops.Repr(pi.DefaultValue));
          first=false;
        }
        Console.Write(mb.IsStatic ? ")" : ") (method)");
        if(!mb.IsConstructor)
        { Type ret = ((System.Reflection.MethodInfo)mb).ReturnType;
          if(ret!=typeof(void)) Console.WriteLine(" -> "+typeName(ret));
        }
        else Console.WriteLine();
      }
    }
    else if(o is ReflectedProperty)
    { ReflectedProperty rp = (ReflectedProperty)o;
      Console.Write("{0} is a property with ", rp.state.info.Name);
      if(rp.state.get==null) Console.Write("no get accessors ");
      else { Console.WriteLine("the following get accessors:"); help(rp.state.get); }
      if(rp.state.set==null) Console.WriteLine("and no set accessors");
      else { Console.WriteLine("and the following set accessors:"); help(rp.state.set); }
    }
    else if(o is ReflectedType)
    { Type type = ((ReflectedType)o).Type;
      if(type.IsEnum)
      { int nameLen=0;
        string[] names = Enum.GetNames(type);
        Array values = Enum.GetValues(type);
        for(int i=0; i<names.Length; i++) if(names[i].Length>nameLen) nameLen=names[i].Length;
        nameLen += 2;

        Console.WriteLine("{0} is an enum with the following values:", type.FullName);
        for(int i=0; i<names.Length; i++)
        { Console.Write(names[i].PadRight(nameLen));
          Console.WriteLine(Ops.ToInt(values.GetValue(i)));
        }
      }
      else goto noHelp;
    }
    else goto noHelp;
    return;
    noHelp: Console.WriteLine("No help available for {0}.", Ops.GetDynamicType(o).__name__);
  }

  [DocString(@"hex(number) -> str

Convert an integer number (of any size) to a hexadecimal string. The result
is a valid Boa expression. Note: this always yields an unsigned literal.
For example, on a 32-bit machine, hex(-1) yields '0xffffffff'. When
evaluated on a machine with the same word size, this literal is evaluated
as -1; at a different word size, it may turn up as a large positive number
or raise an OverflowError exception.")]
  public static object hex(object o) // TODO: should this be implemented this way? or using Ops.ToInt() ?
  { if(o is int) return "0x" + ((int)o).ToString("x");
    if(o is long) return "0x" + ((long)o).ToString("x") + "L";
    return Ops.Invoke(o, "__hex__");
  }

  public static int id(object o) { throw new NotImplementedException(); }

  [DocString(@"input([prompt]) -> str

If the prompt argument is present, it is written to standard output without
a trailing newline. The function then reads a line from input, converts it
to a string (stripping a trailing newline), and returns that. When EOF is
read, EOFError is raised.")]
  public static string input() { return input(null); }
  public static string input(string prompt)
  { if(prompt!=null) Console.Write(prompt);
    string line = Console.ReadLine();
    if(line==null) throw Ops.EOFError("input() reached EOF");
    return line;
  }

  [DocString(@"int([value[, radix]) -> int

Convert a string or number to a plain integer. If the argument is a string,
it must contain a possibly signed decimal number representable as a Boa
integer, possibly embedded in whitespace. The radix parameter gives the base
for the conversion, or zero. If radix is zero, the proper radix is guessed
based on the contents of string; the interpretation is the same as for
integer literals. If radix is specified and x is not a string, TypeError is
raised. Otherwise, the argument may be a plain or long integer or a floating
point number. Conversion of floating point numbers to integers truncates
(towards zero). If the argument is outside the integer range a long object
will be returned instead. If no arguments are given, returns 0.")]
  public static int @int() { return 0; }
  public static int @int(string s) { return int.Parse(s); }
  public static int @int(object o) { return Ops.ToInt(o); }
  public static int @int(string s, int radix)
  { if(radix==0)
    { s = s.ToLower();
      if(s.IndexOf('.')!=-1 || s.IndexOf('e')!=-1) radix=10;
      else if(s.StartsWith("0x")) { radix=16; s=s.Substring(2); }
      else if(s.StartsWith("0")) radix=8;
      else radix=10;
    }
    return Convert.ToInt32(s, radix);
  }

  [DocString(@"intern(string) -> str

Enters the given string into the 'interned' string table. Interned strings
that compare equal will be shared. That is, they will be the same object.")]
  public static string intern(string s) { return string.Intern(s); }

  [DocString(@"isinstance(object, type) -> bool

Return true if the object argument is an instance of the type argument,
or of a (direct or indirect) subclass thereof. Also return true if type
is a type object and object is an object of that type. If object is not a
class instance or an object of the given type, the function always returns
false. If type is neither a class object nor a type object, it may be
a tuple of class or type objects, or may recursively contain other such
tuples (other sequence types are not accepted). If type is not a class,
type, or tuple of classes, types, and such tuples, a TypeError exception is
raised.")]
  public static bool isinstance(object o, object type) { return issubclass(Ops.GetDynamicType(o), type); }

  [DocString(@"issubclass(type, parent) -> bool

Return true if the type argument is a subclass (direct or indirect) of the
parent argument. A class is considered a subclass of itself. The parent may
be a tuple of class objects, or may recursively contain other such tuples
(other sequence types are not accepted), in which case every entry in will
be checked. In any other case, a TypeError exception is raised.")]
  public static bool issubclass(DynamicType type, object parentType)
  { Tuple tup = parentType as Tuple;
    if(tup==null) return type.IsSubclassOf(parentType);
    for(int i=0; i<tup.items.Length; i++) if(issubclass(type, tup.items[i])) return true;
    return false;
  }

  [DocString(@"iter(object[, sentinel]) -> iter

Return an iterator object. The first argument is interpreted very differently
depending on the presence of the second argument. Without a second argument,
the first argument must be a collection object which supports the iteration
protocol (the __iter__() method), or it must support the sequence protocol
(the __getitem__()  method with integer arguments starting at 0). If it does
not support either of those protocols, TypeError is raised. If the second
argument, sentinel, is given, then the object must be callable. The iterator
created in this case will call the object with no arguments for each call to
its next() method; if the value returned is equal to sentinel, StopIteration
will be raised, otherwise the value will be returned.")]
  public static IEnumerator iter(object o) { return Ops.GetEnumerator(o); }
  public static IEnumerator iter(object o, object sentinel)
  { return new SentinelEnumerator(Ops.GetEnumerator(o), sentinel);
  }

  [DocString(@"len(object) -> int

Return the length (the number of items) of an object. The argument may be a
sequence (string, tuple or list) or a mapping (dictionary).")]
  public static int len(object o)
  { string s = o as string;
    if(s!=null) return s.Length;

    ICollection col = o as ICollection;
    if(col!=null) return col.Count;

    ISequence seq = o as ISequence;
    if(seq!=null) return seq.__len__();

    return Ops.ToInt(Ops.Invoke(o, "__len__"));    
  }

  [DocString(@"locals() -> dict

Update and return a dictionary representing the current local symbol table.
Warning: The contents of this dictionary should not be modified; changes
may not affect the values of local variables used by the interpreter.")]
  public static IDictionary locals() { throw new NotImplementedException(); }

  [DocString(@"map(function, seq1, ...) -> list

Takes a function object (or null) and one or more sequences, and applies the
function to the items of the sequences and return a list of the results. The
function must take as many arguments as the number of sequences passed. The
function will be applied to the items of all lists in parallel; if a list is
shorter than another it is assumed to be extended with null items. If
the function is null, the identity function is assumed; if there are multiple
list arguments, map() returns a list consisting of tuples containing the
corresponding items from all lists (a kind of transpose operation). The list
arguments may be any kind of sequence; the result is always a list.")]
  public static List map(object function, object seq)
  { List ret;
    IEnumerator e;
    ICollection col = seq as ICollection;
    if(col!=null) { ret = new List(col.Count); e = col.GetEnumerator(); }
    else { ret = new List(); e = Ops.GetEnumerator(seq); }

    if(function==null) while(e.MoveNext()) ret.append(e.Current);
    else while(e.MoveNext()) ret.append(Ops.Call(function, e.Current));
    return ret;
  }

  public static List map(object function, params object[] seqs)
  { if(seqs.Length==0) throw Ops.TypeError("at least 2 arguments required to map");
    if(seqs.Length==1) return map(function, seqs[0]);

    List ret = new List();
    IEnumerator[] enums = new IEnumerator[seqs.Length];
    for(int i=0; i<enums.Length; i++) enums[i] = Ops.GetEnumerator(seqs[i]);

    object[] items = new object[enums.Length];
    bool done=false;
    while(true)
    { done=true;
      for(int i=0; i<enums.Length; i++)
        if(enums[i].MoveNext()) { items[i]=enums[i].Current; done=false; }
        else items[i]=null;
      if(done) break;
      ret.append(function==null ? new Tuple((object[])items.Clone()) : Ops.CallWithArgsSequence(function, items));
    }
    return ret;
  }

  [DocString(@"max(sequence) -> object
max(value1, value1, ...) -> object

With a single argument s, returns the largest item of a non-empty sequence
(such as a string, tuple or list). With more than one argument, returns the
largest of the arguments.")]
  public static object max(object sequence)
  { IEnumerator e = Ops.GetEnumerator(sequence);
    if(!e.MoveNext()) throw Ops.ValueError("sequence is empty");
    object ret = e.Current;
    while(e.MoveNext()) if(Ops.IsTrue(Ops.More(e.Current, ret))) ret = e.Current;
    return ret;
  }
  public static object max(object a, object b) { return Ops.IsTrue(Ops.More(a, b)) ? a : b; }
  public static object max(params object[] args)
  { if(args.Length==0) throw Ops.TooFewArgs("max()", 1, 0);
    object ret = args[0];
    for(int i=1; i<args.Length; i++) if(Ops.IsTrue(Ops.More(args[i], ret))) ret = args[i];
    return ret;
  }

  [DocString(@"min(sequence) -> object
min(value1, value2, ...) -> object

With a single argument s, returns the smallest item of a non-empty sequence
(such as a string, tuple or list). With more than one argument, returns the
smallest of the arguments.")]
  public static object min(object sequence)
  { IEnumerator e = Ops.GetEnumerator(sequence);
    if(!e.MoveNext()) throw Ops.ValueError("sequence is empty");
    object ret = e.Current;
    while(e.MoveNext()) if(Ops.IsTrue(Ops.Less(e.Current, ret))) ret = e.Current;
    return ret;
  }
  public static object min(object a, object b) { return Ops.IsTrue(Ops.Less(a, b)) ? a : b; }
  public static object min(params object[] args)
  { if(args.Length==0) throw Ops.TooFewArgs("min()", 1, 0);
    object ret = args[0];
    for(int i=1; i<args.Length; i++) if(Ops.IsTrue(Ops.Less(args[i], ret))) ret = args[i];
    return ret;
  }

  [DocString(@"oct(value) -> str

Convert an integer number (of any size) to an octal string. The result is a
valid Boa expression. Note: this always yields an unsigned literal. For
example, on a 32-bit machine, oct(-1) yields '037777777777'. When evaluated
on a machine with the same word size, this literal is evaluated as -1; at a
different word size, it may turn up as a large positive number or raise an
OverflowError exception.")]
  public static object oct(object o)
  { throw new NotImplementedException();
    return Ops.Invoke(o, "__oct__");
  }

  [DocString(@"open(filename[, mode[, bufsize]])
This function has been superceded by the file object constructor.
See the documentation for 'file'.")]
  public static BoaFile open(string filename) { return new BoaFile(filename); }
  public static BoaFile open(string filename, string mode) { return new BoaFile(filename, mode); }
  public static BoaFile open(string filename, string mode, int bufsize)
  { return new BoaFile(filename, mode, bufsize);
  }

  [DocString(@"ord(char) -> int

Return the ASCII value of a string of one character. Eg, ord('a') returns
the integer 97, ord('\u2020') returns 8224. This is the inverse of chr().")]
  public static int ord(string s)
  { if(s.Length!=1) throw Ops.TypeError("ord() expected a character but got string of length {0}", s.Length);
    return (int)s[0];
  }

  [DocString(@"pow(x, y[, z]) -> object

Returns x to the power y; if z is present, returns x to the power y,
modulo z (possibly computed more efficiently than pow(x, y) % z). The
arguments must have numeric types. With mixed operand types, the coercion
rules for binary arithmetic operators apply. For int and long int operands,
the result has the same type as the operands (after coercion) unless the
second argument is negative; in that case, all arguments are converted to
float and a float result is delivered. For example, 10**2 returns 100,
but 10**-2 returns 0.01.")]
  public static object pow(object value, object power) { return Ops.Power(value, power); }
  public static object pow(object value, object power, object mod) { return Ops.PowerMod(value, power, mod); }

  [DocString(@"range([start,] stop[, step]) -> list

This is a versatile function to create lists containing arithmetic
progressions. It is most often used in for loops. The arguments must be
plain integers. If the step argument is omitted, it defaults to 1. If the
start argument is omitted, it defaults to 0. The full form returns a list
of plain integers [start, start + step, start + 2 * step, ...]. If step is
positive, the last element is the largest start + i * step less than stop;
if step is negative, the last element is the largest start + i * step
greater than stop. step must not be zero (or else ValueError is raised).
Example:

>>> range(10)
[0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
>>> range(1, 11)
[1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
>>> range(0, 30, 5)
[0, 5, 10, 15, 20, 25]
>>> range(0, 10, 3)
[0, 3, 6, 9]
>>> range(0, -10, -1)
[0, -1, -2, -3, -4, -5, -6, -7, -8, -9]
>>> range(0)
[]
>>> range(1, 0)
[]")]
  public static List range(int stop) { return range(0, stop, 1); }
  public static List range(int start, int stop) { return range(start, stop, 1); }
  public static List range(int start, int stop, int step)
  { if(step==0) throw Ops.ValueError("step of 0 passed to range()");
    if(step<0 && start<=stop || step>0 && start>=stop) return new List();
    int sign = Math.Sign(step);
    List ret = new List((stop-start+step-sign)/step);
    for(; start<stop; start += step) ret.append(start);
    return ret;
  }

  [DocString(@"reduce(function, sequence[, initializer]) -> object

Apply function of two arguments cumulatively to the items of sequence, from
left to right, so as to reduce the sequence to a single value. For example,
reduce(lambda x,y: x+y, [1, 2, 3, 4, 5]) calculates ((((1+2)+3)+4)+5). The
left argument, x, is the accumulated value and the right argument, y, is the
update value from the sequence. If the optional initializer is present, it
is placed before the items of the sequence in the calculation, and serves as
a default when the sequence is empty. If initializer is not given and
sequence contains only one item, the first item is returned.")]
  public static object reduce(object function, object seq)
  { IEnumerator e = Ops.GetEnumerator(seq);
    if(!e.MoveNext()) throw Ops.TypeError("reduce() of empty sequence with no initial value");
    object ret = e.Current;
    while(e.MoveNext()) ret = Ops.Call(function, ret, e.Current);
    return ret;
  }
  public static object reduce(object function, object seq, object initial)
  { IEnumerator e = Ops.GetEnumerator(seq);
    while(e.MoveNext()) initial = Ops.Call(function, initial, e.Current);
    return initial;
  }

  public static Module reload(Module module) { throw new NotImplementedException(); }

  [DocString(@"repr(object) -> str

Return a string containing a printable representation of an object. This is
the same value yielded by conversions (reverse quotes). It is sometimes
useful to be able to access this operation as an ordinary function. For many
types, this function makes an attempt to return a string that would yield an
object with the same value when passed to eval().")]
  public static string repr(object o) { return Ops.Repr(o); }

  [DocString(@"round(x[, n]) -> float

Returns the floating point value x rounded to n digits after the decimal
point. If n is omitted, it defaults to zero. The result is a floating point
number. Values are rounded to the closest multiple of 10 to the power minus
n; if two multiples are equally close, rounding is done away from odd
integers (so, for example, both 1.5 and 2.5 round to 2.0).")]
  public static double round(double value) { return Math.Round(value); }
  public static double round(double value, int ndigits)
  { if(ndigits<0)
    { double factor = Math.Pow(10, -ndigits);
      return factor*Math.Round(value/factor);
    }
    return Math.Round(value, Math.Min(ndigits, 15));
  }

  [DocString(@"setattr(object, name, value)

This is the counterpart of getattr(). The arguments are an object, a string
and an arbitrary value. The string may name an existing attribute or a new
attribute. The function assigns the value to the attribute, provided the
object allows it. For example, setattr(x, 'foobar', 123) is equivalent to
x.foobar = 123.")]
  public static void setattr(object o, string name, object value) { Ops.SetAttr(value, o, name); }

  [DocString(@"str([object]) -> str

Return a string containing a nicely printable representation of an object.
For strings, this returns the string itself. The difference with
repr(object) is that str(object) does not always attempt to return a string
that is acceptable to eval(); its goal is to return a printable string. If
no argument is given, returns the empty string, ''.")]
  public static string str() { return string.Empty; }
  public static string str(object o) { return Ops.Str(o); }

  [DocString(@"sum(sequence[, start]) -> object

Sums start and the items of a sequence, from left to right, and returns the
total. start defaults to 0. The sequence's items are normally numbers, but
could be strings. However, the fast, correct way to concatenate sequence of
strings is by calling string.join(). Note that sum(range(n), m) is equivalent
to reduce(operator.add, range(n), m).")]
  public static object sum(object seq) { return sum(seq, 0); }
  public static object sum(object seq, object start)
  { IEnumerator e = Ops.GetEnumerator(seq);
    while(e.MoveNext()) start = Ops.Add(start, e.Current);
    return start;
  }
  
  [DocString(@"type(object) -> type

Returns the type of an object. The return value is a type object.")]
  public static DynamicType type(object obj) { return Ops.GetDynamicType(obj); }

  // TODO: vars()

  [DocString(@"zip(seq1, ...) -> list

This function returns a list of tuples, where the i-th tuple contains the
i-th element from each of the argument sequences. At least one sequence is
required, otherwise a TypeError is raised. The returned list is truncated
in length to the length of the shortest argument sequence. When there are
multiple argument sequences which are all of the same length, zip() is
similar to map() with an initial argument of null. With a single sequence
argument, it returns a list of 1-tuples.")]
  public static List zip(params object[] seqs)
  { if(seqs.Length==0) throw Ops.TypeError("zip() requires at least one sequence");

    List ret = new List();
    IEnumerator[] enums = new IEnumerator[seqs.Length];
    for(int i=0; i<enums.Length; i++) enums[i] = Ops.GetEnumerator(seqs[i]);
    object[] items = new object[enums.Length];
    while(true)
    { for(int i=0; i<enums.Length; i++)
        if(!enums[i].MoveNext()) return ret;
        else items[i]=enums[i].Current;
      ret.append(new Tuple(items));
    }
  }

  [DocString(@"This value is set to the result of the last expression evaluated in
interactive mode by the default sys.displayhook handler.")]
  public static object _;

  [DocString(@"This value is 1 when the interpreter/compiler is running in debug mode and
0 otherwise. Debug mode alters many facets of Boa's internal operation,
including whether or not 'assert' statements will be executed and whether
or not optimizations will be performed. This value cannot be altered at
runtime.")]
  public static int __debug__ { get { return Options.Debug ? 1 : 0; } }

  public static object exit = "Use Ctrl-Z plus Return (eg EOF) to exit.";
  public static object quit = "Use Ctrl-Z plus Return (eg EOF) to exit.";

  // TODO: figure out how to handle these types that collide with the functions
  // (perhaps by modifying ReflectedType.cons)
  #region Data types
  //public static readonly object @bool   = ReflectedType.FromType(typeof(bool));
  public static readonly object complex = ReflectedType.FromType(typeof(Complex));
  public static readonly object dict    = ReflectedType.FromType(typeof(Dict));
  public static readonly object file    = ReflectedType.FromType(typeof(BoaFile));
  //public static readonly object @float  = ReflectedType.FromType(typeof(double));
  //public static readonly object @int    = ReflectedType.FromType(typeof(int));
  //public static readonly object iter    = ReflectedType.FromType(typeof(IEnumerator));
  public static readonly object list    = ReflectedType.FromType(typeof(List));
  public static readonly object @long   = ReflectedType.FromType(typeof(Integer));
  public static readonly object @object = ReflectedType.FromType(typeof(object));
  public static readonly object slice   = ReflectedType.FromType(typeof(Slice));
  public static readonly object @string = ReflectedType.FromType(typeof(string)); // FIXME: this should be 'str'
  public static readonly object tuple   = ReflectedType.FromType(typeof(Tuple));
  public static readonly object xrange  = ReflectedType.FromType(typeof(XRange));
  #endregion

  // TODO: add the proper python base types to these (eg, IOError should derive from EnvironmentError)
  #region Exceptions
  public static readonly object ApplicationError = ReflectedType.FromType(typeof(ApplicationException));
  public static readonly object ArithmeticError = ReflectedType.FromType(typeof(ArithmeticException));
  public static readonly object AssertionError = ReflectedType.FromType(typeof(AssertionErrorException));
  public static readonly object EOFError = ReflectedType.FromType(typeof(System.IO.EndOfStreamException));
  public static readonly object Exception = ReflectedType.FromType(typeof(Exception));
  public static readonly object EnvironmentError = ReflectedType.FromType(typeof(EnvironmentErrorException));
  public static readonly object FloatingPointError = ReflectedType.FromType(typeof(FloatingPointErrorException));
  public static readonly object ImportError = ReflectedType.FromType(typeof(ImportErrorException));
  public static readonly object IndexError = ReflectedType.FromType(typeof(IndexErrorException));
  public static readonly object IOError = ReflectedType.FromType(typeof(System.IO.IOException));
  public static readonly object KeyError = ReflectedType.FromType(typeof(KeyErrorException));
  public static readonly object LookupError = ReflectedType.FromType(typeof(LookupErrorException));
  public static readonly object MemoryError = ReflectedType.FromType(typeof(OutOfMemoryException));
  public static readonly object NameError = ReflectedType.FromType(typeof(NameErrorException));
  public static readonly object NotImplementedError = ReflectedType.FromType(typeof(NotImplementedException));
  public static readonly object OSError = ReflectedType.FromType(typeof(OSErrorException));
  public static readonly object OverflowError = ReflectedType.FromType(typeof(OverflowException));
  public static readonly object RuntimeError = ReflectedType.FromType(typeof(RuntimeException));
  public static readonly object StandardError = ReflectedType.FromType(typeof(StandardErrorException));
  public static readonly object StopIteration = ReflectedType.FromType(typeof(StopIterationException));
  public static readonly object SyntaxError = ReflectedType.FromType(typeof(SyntaxErrorException));
  public static readonly object SystemExit = ReflectedType.FromType(typeof(SystemExitException));
  public static readonly object TypeError = ReflectedType.FromType(typeof(TypeErrorException));
  public static readonly object ValueError = ReflectedType.FromType(typeof(ValueErrorException));
  public static readonly object ZeroDivisionError = ReflectedType.FromType(typeof(DivideByZeroException));
  #endregion
  
  static string typeName(Type type)
  { if(type.IsArray) return typeName(type.GetElementType())+"[]";
    if(type==typeof(object)) return "object";
    if(type==typeof(int)) return "int";
    if(type==typeof(string)) return "str";
    if(type==typeof(char)) return "char";
    return type.FullName;
  }
}
  
} // namespace Boa.Modules