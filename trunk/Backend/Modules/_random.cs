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
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class _random
{ _random() { }

  public static string __repr__() { return "<module '_random' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  public class Random
  { public unsafe object getrandbits(int bits)
    { if(bits<=0)  throw Ops.ValueError("getrandbits(): number of bits must be greater than zero");
      if(bits<=32) return genrand_int32();
      if(bits<=64) return ((ulong)genrand_int32()<<32) | genrand_int32();

      int chunks = ((bits-1)/32 + 1);
      uint[] arr = new uint[chunks];

      fixed(uint* ba=arr)
        for(uint* p=ba, e=p+chunks; p<e; bits-=32, p++)
        { uint r = genrand_int32();
          if(bits<32) r >>= (32-bits);
          *p = r;
        }
      
      return new Integer(chunks==1 && arr[0]==0 ? 0 : 1, arr);
    }

    public Tuple getstate()
    { object[] items = new object[N+1];
      for(int i=0; i<N; i++) items[i] = (int)state[i];
      items[N] = index;
      return new Tuple(items);
    }

    public void setstate(Tuple tup)
    { if(tup==null) throw Ops.TypeError("setstate(): expected a tuple but received null");
      object[] items = tup.items;
      if(items.Length != N+1) throw Ops.ValueError("state tuple has the wrong length! (should be {0} elements)", N+1);
      for(int i=0; i<N; i++) state[i] = (uint)Ops.ToInt(items[i]);
      index = Ops.ToInt(items[N]);
    }

    public unsafe void jumpahead(int n)
    { uint i, j, tmp;

      fixed(uint* mt=state)
      { for(i=N-1; i>1; i--)
        { j=(uint)n%i; tmp=mt[i]; mt[i]=mt[j]; mt[j]=tmp;
        }
        for(i=0; i<N; ) mt[i] += ++i;
        index = N;
      }
    }

    public double random()
    { uint a=genrand_int32()>>5, b=genrand_int32()>>6;
      return (a*67108864.0+b)*(1.0/9007199254740992.0);
    }
    
    public void seed(object sv)
    { if(sv==null) init_genrand((uint)(DateTime.Now.Ticks>>23));
      else if(sv is int) init_genrand((uint)Math.Abs((int)sv));
      else if(sv is Integer)
      { Integer i = (Integer)sv;
        init_by_array(i.data, i.length);
      }
      else init_genrand((uint)sv.GetHashCode()); // TODO: this might need to be changed for python compliance
    }

    unsafe uint genrand_int32()
    { uint y; 
      uint* mag01 = stackalloc uint[100];
      mag01[1] = MATRIX_A;
      
      fixed(uint* mt=state)
      { if(index>=N)
        { int kk=0;
		      for(;kk<N-M;kk++)
		      { y = (mt[kk]&UPPER_MASK)|(mt[kk+1]&LOWER_MASK);
			      mt[kk] = mt[kk+M] ^ (y >> 1) ^ mag01[y & 1];
		      }
		      for(;kk<N-1;kk++)
		      { y = (mt[kk]&UPPER_MASK)|(mt[kk+1]&LOWER_MASK);
			      mt[kk] = mt[kk+(M-N)] ^ (y >> 1) ^ mag01[y & 1];
		      }
		      y = (mt[N-1]&UPPER_MASK)|(mt[0]&LOWER_MASK);
		      mt[N-1] = mt[M-1] ^ (y >> 1) ^ mag01[y & 1];
		      index = 0;
        }

        y = mt[index++];
        y ^= (y >> 11);
        y ^= (y << 7) & 0x9d2c5680;
        y ^= (y << 15) & 0xefc60000;
        y ^= (y >> 18);
        return y;
      }
    }
    
    unsafe void init_by_array(uint[] init_key, uint key_length)
    { uint i=1, j=0, k=Math.Min(key_length, N);

      fixed(uint* mt=state)
      { init_genrand(19650218);
        for(; k!=0; k--)
        { mt[i] = (mt[i] ^ (uint)((mt[i-1] ^ (mt[i-1] >> 30)) * 1664525)) + init_key[j] + j;
  	  	  i++; j++;
	  	    if(i>=N) { mt[0] = mt[N-1]; i=1; }
		      if(j>=key_length) j=0;
        }
	      for(k=N-1; k!=0; k--)
	      { mt[i] = (mt[i] ^ (uint)((mt[i-1] ^ (mt[i-1] >> 30)) * 1566083941)) - i;
		      i++;
		      if(i>=N) { mt[0] = mt[N-1]; i=1; }
	      }
	      mt[0] = 0x80000000;
      }
    }

    unsafe void init_genrand(uint s)
    { int mti;
      fixed(uint* mt=state)
      { mt[0] = s&0xffffffff;
	      for(mti=1; mti<N; mti++) mt[mti] = (uint)(1812433253 * (mt[mti-1] ^ (mt[mti-1] >> 30)) + mti);
	      index = mti;
	    }
    }

    uint[] state = new uint[N];
    int index;

    const int  N=624, M=397;
    const uint MATRIX_A=0x9908b0df, UPPER_MASK=0x80000000, LOWER_MASK=0x7fffffff;
  }
}

} // namespace Boa.Modules
