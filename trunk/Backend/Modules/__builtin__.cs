using System;
using System.Collections;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Modules
{

public class __builtin__
{ 
  public static object abs(object o)
  { if(o is int) return Math.Abs((int)o);
    if(o is double) return Math.Abs((double)o);
    return Ops.Invoke(o, "__abs__");
  }

  public static object apply(object func, object args) { return Ops.CallWithArgsSequence(func, args); }
  public static string chr(int value) { return new string((char)value, 1); }
  public static int cmp(object a, object b) { return Ops.Compare(a, b); }
  public static void delattr(object o, string name) { Ops.DelAttr(o, name); }
  public static List dir() { throw new NotImplementedException(); }
  public static List dir(object o) { return Ops.GetAttrNames(o); }
  public static Tuple divmod(object a, object b) { return new Tuple(Ops.Divide(a, b), Ops.Modulus(a, b)); }

  public static object eval(string expr)
  { IDictionary dict = (IDictionary)globals();
    return eval(expr, dict, dict);
  }
  public static object eval(string expr, IDictionary globals) { return eval(expr, globals, globals); }
  public static object eval(string expr, IDictionary globals, IDictionary locals)
  { return Parser.FromString(expr).ParseExpression().Evaluate(new Frame(locals, globals));
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
      else { ret=new List(); e=Ops.GetEnumerator(list); }

      if(function==null)
      { while(e.MoveNext()) if(Ops.IsTrue(e.Current)) ret.append(Ops.ToBoa(e.Current));
      }
      else while(e.MoveNext()) if(Ops.IsTrue(Ops.Call(function, e.Current))) ret.append(Ops.ToBoa(e.Current));
      return ret;
    }
  }

  public static object getattr(object o, string name) { return Ops.GetAttr(o, name); }
  public static object getattr(object o, string name, object defaultValue)
  { object ret;
    return Ops.GetAttr(o, name, out ret) ? ret : defaultValue;
  }

  public static object globals() { throw new NotImplementedException(); }
  public static object hasattr(object o, string name) { object dummy; return Ops.GetAttr(o, name, out dummy); }
  public static int hash(object o) { return o.GetHashCode(); }

  public static object hex(object o)
  { if(o is int) return "0x" + ((int)o).ToString("x");
    if(o is long) return "0x" + ((int)o).ToString("x") + "L";
    return Ops.Invoke(o, "__hex__");
  }

  public static string intern(string s) { return string.Intern(s); }
  public static bool isInstance(object o, object type) { throw new NotImplementedException(); }
  public static bool isSubClass(object XX, object YY) { throw new NotImplementedException(); }
  public static IEnumerator iter(object o) { return Ops.GetEnumerator(o); }

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
  { if(stop==0) throw Ops.ValueError("step of 0 passed to range()");
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

  public static object sum(object seq) { return sum(seq, 0); }
  public static object sum(object seq, object start)
  { IEnumerator e = Ops.GetEnumerator(seq);
    while(e.MoveNext()) start = Ops.Add(start, e.Current);
    return start;
  }

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
    return ret;
  }

  #region Data types
  public static object @bool   = ReflectedType.FromType(typeof(bool));
  public static object dict    = ReflectedType.FromType(typeof(Dict));
  public static object @float  = ReflectedType.FromType(typeof(double));
  public static object @int    = ReflectedType.FromType(typeof(int));
  public static object list    = ReflectedType.FromType(typeof(List));
  public static object @object = ReflectedType.FromType(typeof(object));
  public static object @string = ReflectedType.FromType(typeof(string));
  public static object tuple   = ReflectedType.FromType(typeof(Tuple));
  #endregion

  #region Exceptions
  public static object StopIteration = ReflectedType.FromType(typeof(StopIterationException));
  public static object ValueError = ReflectedType.FromType(typeof(ValueErrorException));
  //public static object FloatingPointError = ReflectedType.FromType(typeof(FloatingPointErrorException));
  //public static object ImportError = ReflectedType.FromType(typeof(ImportErrorException));
  public static object IndexError = ReflectedType.FromType(typeof(IndexErrorException));
  public static object KeyError = ReflectedType.FromType(typeof(KeyErrorException));
  //public static object LookupError = ReflectedType.FromType(typeof(LookupErrorException));
  public static object NameError = ReflectedType.FromType(typeof(NameErrorException));
  //public static object NotImplementedError = ReflectedType.FromType(typeof(NotImplementedErrorException));
  //public static object OverflowError = ReflectedType.FromType(typeof(OverflowErrorException));
  public static object SyntaxError = ReflectedType.FromType(typeof(SyntaxErrorException));
  public static object TypeError = ReflectedType.FromType(typeof(TypeErrorException));
  #endregion
}
  
} // namespace Boa.Modules