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
using System.Collections.Specialized;

// TODO: don't allow __repr__ to go into an infinite loop with circular references
// TODO: make IDictionary.GetEnumerator() (returns DictionaryEntry) different from
//       IEnumerator.GetEnumerator() (returns keys), if possible
// TODO: make objects that boa considers equal be the same key
namespace Boa.Runtime
{

[BoaType("dict")]
public class Dict : HybridDictionary, IComparable, IMapping, ICloneable, IRepresentable
{ public Dict() { }
  public Dict(IDictionary dict) { foreach(DictionaryEntry e in dict) Add(e.Key, e.Value); }
  public Dict(int size) : base(size) { }

  #region DictEnumerator
  public class DictEnumerator : IEnumerator
  { public DictEnumerator(IDictionary dict) { e=dict.GetEnumerator(); }

    public object Current
    { get
      { if(current!=null) return current;
        DictionaryEntry e = (DictionaryEntry)this.e.Current;
        return current=new Tuple(e.Key, e.Value);
      }
    }

    public bool MoveNext() { current=null; return e.MoveNext(); }
    public void Reset() { current=null; e.Reset(); }

    IDictionaryEnumerator e;
    Tuple current;
  }
  #endregion

  public override bool Equals(object o)
  { if(o is Dict)
    { List l1 = items();
      l1.sort();
      List l2 = ((Dict)o).items();
      l2.sort();
      return l1.Equals(l2);
    }
    else return false;
  }
  
  public override int GetHashCode() { throw Ops.TypeError("dict objects are unhashable"); }
  public override string ToString() { return __repr__(); }

  #region IComparable Members
  public int CompareTo(object o)
  { if(o is Dict)
    { List l1 = items();
      l1.sort();
      List l2 = ((Dict)o).items();
      l2.sort();
      return l1.CompareTo(l2);
    }
    else return -1; // FIXME: compare by type names, like python does
  }
  #endregion

  #region IMapping Members
  public void clear() { Clear(); }
  public object copy() { return Clone(); }

  public object get(object key) { return this[key]; }
  public object get(object key, object defaultValue)
  { return Contains(key) ? this[key] : defaultValue;
  }

  public bool has_key(object key) { return Contains(key); }

  static Dict fromkeys(object seq) { return fromkeys(seq, null); }
  static Dict fromkeys(object seq, object value)
  { Dict d = new Dict();
    IEnumerator e = Ops.GetEnumerator(seq);
    while(e.MoveNext()) d[e.Current] = value;
    return d;
  }

  public object pop(object key)
  { object value = __getitem__(key);
    Remove(key);
    return value;
  }
  public object pop(object key, object defaultValue)
  { object value = Contains(key) ? this[key] : defaultValue;
    Remove(key);
    return value;
  }

  public Tuple popitem() // TODO: this is inefficient. any way to speed it up?
  { if(Count==0) throw Ops.KeyError("popitem(): dictionary is empty");
    IDictionaryEnumerator e = GetEnumerator();
    e.MoveNext();
    return new Tuple(e.Key, e.Value);
  }

  public object setdefault(object key) { return setdefault(key, null); }
  public object setdefault(object key, object defaultValue)
  { if(Contains(key)) return this[key];
    this[key] = defaultValue;
    return defaultValue;
  }
  
  public void update(object dict)
  { IDictionary d = dict as IDictionary;
    if(d!=null) foreach(DictionaryEntry e in d) this[e.Key] = e.Value;
    else
    { IMapping m = dict as IMapping;
      if(m!=null)
      { IEnumerator e = Ops.GetEnumerator(m.keys());
        while(e.MoveNext()) this[e.Current] = m.get(e.Current);
      }
      else
      { object getitem = Ops.GetAttr(dict, "__getitem__");
        IEnumerator e = Ops.GetEnumerator(Ops.Invoke(dict, "keys"));
        while(e.MoveNext()) this[e.Current] = Ops.Call(getitem, e.Current);
      }
    }
  }

  public List items()
  { List ret = new List(Count);
    foreach(DictionaryEntry e in this) ret.Add(new Tuple(e.Key, e.Value));
    return ret;
  }
  public List keys() { return new List(Keys); }
  public List values() { return new List(Values); }
  
  public IEnumerator iteritems() { return new DictEnumerator(this); }
  public IEnumerator iterkeys() { return Keys.GetEnumerator(); }
  public IEnumerator itervalues() { return Values.GetEnumerator(); }

  public void __delitem__(object key)
  { if(!Contains(key)) throw Ops.KeyError(Ops.Repr(key));
    Remove(key);
  }
  public object __getitem__(object key)
  { if(Contains(key)) return this[key];
    else throw Ops.KeyError(Ops.Repr(key));
  }
  public void __setitem__(object key, object value) { this[key]=value; }
  public int __len__() { return Count; }
  public bool __contains__(object key) { return Contains(key); }
  #endregion

  #region ICloneable Members
  public object Clone() { return new Dict(this); }
  #endregion

  #region IRepresentable Members
  public string __repr__()
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append('{');
    bool first=true;
    foreach(DictionaryEntry e in this)
    { if(first) first=false;
      else sb.Append(", ");
      sb.Append(Ops.Repr(e.Key));
      sb.Append(": ");
      sb.Append(Ops.Repr(e.Value));
    }
    sb.Append('}');
    return sb.ToString();
  }
  #endregion
}

} // namespace Boa.Runtime