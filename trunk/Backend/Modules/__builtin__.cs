using System;
using System.Collections;
using Boa.AST;
using Boa.Runtime;

// TODO: make these conform to python specs
// TODO: allow functions to return 'longint' if they'd overflow an 'int'
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
  public class XRange : IEnumerable, ISequence, IRepresentable
  { public XRange(int stop) : this(0, stop, 1) { }
    public XRange(int start, int stop) : this(start, stop, 1) { }
    public XRange(int start, int stop, int step)
    { if(step==0) throw Ops.ValueError("step of 0 passed to xrange()");
      this.start=start; this.stop=stop; this.step=step;
      if(start<=stop && step<0 || start>=stop && step>0) length = 0;
      else length = (stop-start)/step;
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

  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module '__builtin__' (built-in)>"; }

  public static object abs(object o)
  { if(o is int) return Math.Abs((int)o);
    if(o is double) return Math.Abs((double)o);
    return Ops.Invoke(o, "__abs__");
  }

  public static object apply(object func, object args) { return Ops.CallWithArgsSequence(func, args); }
  public static object @bool(object o) { return Ops.FromBool(Ops.IsTrue(o)); }
  public static object callable(object o) { return o is ICallable ? Ops.TRUE : hasattr(o, "__call__"); }
  public static string chr(int value) { return new string((char)value, 1); }
  public static int cmp(object a, object b) { return Ops.Compare(a, b); }
  public static void delattr(object o, string name) { Ops.DelAttr(o, name); }
  public static List dir() { return dir(Ops.GetExecutingModule()); }
  public static List dir(object o) { return Ops.GetAttrNames(o); }
  public static Tuple divmod(object a, object b) { return new Tuple(Ops.Divide(a, b), Ops.Modulus(a, b)); }
  public static IEnumerator enumerate(object o) { return new EnumerateEnumerator(Ops.GetEnumerator(o)); }

  public static object eval(string expr)
  { IDictionary dict = (IDictionary)globals();
    return eval(expr, dict, dict);
  }
  public static object eval(string expr, IDictionary globals) { return eval(expr, globals, globals); }
  public static object eval(string expr, IDictionary globals, IDictionary locals)
  { Frame frame = new Frame(locals, globals);
    Ops.Frames.Push(frame);
    try { return Parser.FromString(expr).ParseExpression().Evaluate(frame); }
    finally { Ops.Frames.Pop(); }
  }

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
      { while(e.MoveNext()) if(Ops.IsTrue(e.Current)) ret.append(Ops.ToBoa(e.Current));
      }
      else while(e.MoveNext()) if(Ops.IsTrue(Ops.Call(function, e.Current))) ret.append(Ops.ToBoa(e.Current));
      return ret;
    }
  }

  public static double @float(string s) { return double.Parse(s); }
  public static double @float(object o) { return Ops.ToFloat(o); }

  public static object getattr(object o, string name) { return Ops.GetAttr(o, name); }
  public static object getattr(object o, string name, object defaultValue)
  { object ret;
    return Ops.GetAttr(o, name, out ret) ? ret : defaultValue;
  }

  public static object globals() { return Ops.GetExecutingModule().__dict__; }
  public static object hasattr(object o, string name) { object dummy; return Ops.GetAttr(o, name, out dummy); }

  // FIXME: python says: Numeric values that compare equal have the same hash value (even if they are of
  //                     different types, as is the case for 1 and 1.0).
  public static int hash(object o) { return o.GetHashCode(); }

  public static void help() { }
  public static void help(object o)
  { object doc;
    if(Ops.GetAttr(o, "__doc__", out doc)) Console.WriteLine(doc);
    else Console.WriteLine("No help available for {0}.", Ops.GetDynamicType(o).__name__);
  }

  public static object hex(object o)
  { if(o is int) return "0x" + ((int)o).ToString("x");
    if(o is long) return "0x" + ((int)o).ToString("x") + "L";
    return Ops.Invoke(o, "__hex__");
  }

  public static int id(object o) { throw new NotImplementedException(); }

  public static string input() { return input(null); }
  public static string input(string prompt)
  { if(prompt!=null) Console.Write(prompt);
    string line = Console.ReadLine();
    if(line==null) throw Ops.EOFError("raw_input() reached EOF");
    return line;
  }

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

  public static string intern(string s) { return string.Intern(s); }
  public static bool isInstance(object o, object type) { throw new NotImplementedException(); }
  public static bool isSubClass(object XX, object YY) { throw new NotImplementedException(); }

  public static IEnumerator iter(object o) { return Ops.GetEnumerator(o); }
  public static IEnumerator iter(object o, object sentinel)
  { return new SentinelEnumerator(Ops.GetEnumerator(o), sentinel);
  }

  public static int len(object o)
  { string s = o as string;
    if(s!=null) return s.Length;

    ICollection col = o as ICollection;
    if(col!=null) return col.Count;

    ISequence seq = o as ISequence;
    if(seq!=null) return seq.__len__();

    return Ops.ToInt(Ops.Invoke(o, "__len__"));    
  }

  public static List map(object function, object seq)
  { List ret;
    IEnumerator e;
    ICollection col = seq as ICollection;
    if(col!=null) { ret = new List(col.Count); e = col.GetEnumerator(); }
    else { ret = new List(); e = Ops.GetEnumerator(seq); }

    if(function==null) while(e.MoveNext()) ret.append(Ops.ToBoa(e.Current));
    else while(e.MoveNext()) ret.append(Ops.ToBoa(Ops.Call(function, e.Current)));
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
    while(!done)
    { done=true;
      for(int i=0; i<enums.Length; i++)
        if(enums[i].MoveNext()) { items[i]=enums[i].Current; done=false; }
        else items[i]=null;
      ret.append(function==null ? new Tuple(items) : Ops.CallWithArgsSequence(function, items));
    }
    return ret;
  }

  public static object max(object seq)
  { IEnumerator e = Ops.GetEnumerator(seq);
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

  public static object min(object seq)
  { IEnumerator e = Ops.GetEnumerator(seq);
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

  public static object oct(object o)
  { throw new NotImplementedException();
    return Ops.Invoke(o, "__oct__");
  }

  public static BoaFile open(string filename, string made)
  { throw new NotImplementedException();
  }

  public static int ord(string s)
  { if(s.Length!=1) throw Ops.TypeError("ord() expected a character but got string of length {0}", s.Length);
    return (int)s[0];
  }

  public static object pow(object value, object power) { return Ops.Power(value, power); }
  public static object pow(object value, object power, object mod) { return Ops.PowerMod(value, power, mod); }

  public static List range(int stop)
  { List ret = new List(stop);
    for(int i=0; i<stop; i++) ret.append(i);
    return ret;
  }
  public static List range(int start, int stop)
  { List ret = new List(stop-start);
    for(; start<stop; start++) ret.append(start);
    return ret;
  }
  public static List range(int start, int stop, int step)
  { if(step==0) throw Ops.ValueError("step of 0 passed to range()");
    if(start<=stop && step<0 || start>=stop && step>0) return new List();
    List ret = new List((stop-start)/step);
    for(; start<stop; start += step) ret.append(start);
    return ret;
  }
  
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
  public static string repr(object o) { return Ops.Repr(o); }

  public static double round(double value) { return Math.Round(value); }
  public static double round(double value, int ndigits)
  { if(ndigits<0)
    { double factor = Math.Pow(10, -ndigits);
      return factor*Math.Round(value/factor);
    }
    return Math.Round(value, Math.Min(ndigits, 15));
  }

  public static void setattr(object o, string name, object value) { Ops.SetAttr(value, o, name); }

  public static string str() { return string.Empty; }
  public static string str(object o) { return Ops.Str(o); }

  public static object sum(object seq) { return sum(seq, 0); }
  public static object sum(object seq, object start)
  { IEnumerator e = Ops.GetEnumerator(seq);
    while(e.MoveNext()) start = Ops.Add(start, e.Current);
    return start;
  }
  
  public static DynamicType type(object obj) { return Ops.GetDynamicType(obj); }

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

  public static object _;

  // TODO: figure out how to handle these types that collide with the functions
  // (perhaps by modifying ReflectedType.cons)
  #region Data types
  //public static readonly object @bool   = ReflectedType.FromType(typeof(bool));
  public static readonly object dict    = ReflectedType.FromType(typeof(Dict));
  //public static readonly object @float  = ReflectedType.FromType(typeof(double));
  //public static readonly object @int    = ReflectedType.FromType(typeof(int));
  public static readonly object list    = ReflectedType.FromType(typeof(List));
  public static readonly object @object = ReflectedType.FromType(typeof(object));
  public static readonly object @string = ReflectedType.FromType(typeof(string));
  public static readonly object tuple   = ReflectedType.FromType(typeof(Tuple));
  public static readonly object xrange  = ReflectedType.FromType(typeof(XRange));
  #endregion

  #region Exceptions
  public static readonly object EOFError = ReflectedType.FromType(typeof(EOFErrorException));
  //public static readonly object FloatingPointError = ReflectedType.FromType(typeof(FloatingPointErrorException));
  public static readonly object ImportError = ReflectedType.FromType(typeof(ImportErrorException));
  public static readonly object IndexError = ReflectedType.FromType(typeof(IndexErrorException));
  public static readonly object KeyError = ReflectedType.FromType(typeof(KeyErrorException));
  //public static readonly object LookupError = ReflectedType.FromType(typeof(LookupErrorException));
  public static readonly object NameError = ReflectedType.FromType(typeof(NameErrorException));
  //public static readonly object NotImplementedError = ReflectedType.FromType(typeof(NotImplementedErrorException));
  //public static readonly object OverflowError = ReflectedType.FromType(typeof(OverflowErrorException));
  public static readonly object RuntimeError = ReflectedType.FromType(typeof(RuntimeException));
  public static readonly object StopIteration = ReflectedType.FromType(typeof(StopIterationException));
  public static readonly object SyntaxError = ReflectedType.FromType(typeof(SyntaxErrorException));
  public static readonly object SystemExit = ReflectedType.FromType(typeof(SystemExitException));
  public static readonly object TypeError = ReflectedType.FromType(typeof(TypeErrorException));
  public static readonly object ValueError = ReflectedType.FromType(typeof(ValueErrorException));
  #endregion
}
  
} // namespace Boa.Modules