using System;

namespace Boa.Runtime
{

public struct Complex : IRepresentable
{ public Complex(double real) { this.real=real; }
  public Complex(double real, double imag) { this.real=real; this.imag=imag; }

  public Complex Conjugate { get { return new Complex(real, -imag); } }

  public string __repr__() { return ToString("R"); }

  public override bool Equals(object obj)
  { Complex c = obj as Complex;
    return c==null ? false : c.real=real && c.imag==imag;
  }

  public override int GetHashCode() { return read.GetHashCode() + imag.GetHashCode()*1000003; }

  public double abs() { return Math.Sqrt(real*real+imag*imag); }
  public Complex conjugate() { return new Complex(real, -imag); }

  public override string ToString()
  { return '(' + real.ToString() + '+' + imag.ToString() + "j)";
  }
  
  public string ToString(string s)
  { return '(' + real.ToString(s) + '+' + imag.ToString(s) + "j)";
  }

  public double real, imag;

  public static Complex operator+(Complex a, Complex b) { return new Complex(a.real+b.real, a.imag+b.imag); }
  public static Complex operator+(Complex a, double  b) { return new Complex(a.real+b, a.imag); }
  public static Complex operator+(double  a, Complex b) { return new Complex(a+b.real, b.imag); }

  public static Complex operator-(Complex a, Complex b) { return new Complex(a.real-b.real, a.imag-b.imag); }
  public static Complex operator-(Complex a, double  b) { return new Complex(a.real-b, a.imag); }
  public static Complex operator-(double  a, Complex b) { return new Complex(a-b.real, -b.imag); }

  public static Complex operator*(Complex a, Complex b)
  { return new Complex(a.real*b.real - a.imag*b.imag, a.real*b.imag + a.imag*b.real);
  }
  public static Complex operator*(Complex a, double b)  { return new Complex(a.real*b, a.imag*b); }
  public static Complex operator*(double  a, Complex b) { return new Complex(a*b.real, a*b.imag); }

  public static Complex operator/(Complex a, Complex b)
  { const double abs_breal = b.real < 0 ? -b.real : b.real;
	  const double abs_bimag = b.imag < 0 ? -b.imag : b.imag;
	  double real, imag;

  	if(abs_breal >= abs_bimag)
  	{ if(abs_breal == 0.0) throw Ops.DivideByZeroError("attempted complex division by zero");
	 	  else
	 	  { const double ratio = b.imag / b.real;
	 		  const double denom = b.real + b.imag * ratio;
	 		  real = (a.real + a.imag * ratio) / denom;
	 		  imag = (a.imag - a.real * ratio) / denom;
	 	  }
  	}
	  else
	  { const double ratio = b.real / b.imag;
		  const double denom = b.real * ratio + b.imag;
		  real = (a.real * ratio + a.imag) / denom;
		  imag = (a.imag * ratio - a.real) / denom;
	  }
	  return new Complex(real, imag);
  }
  public static Complex operator/(Complex a, double b) { return new Complex(a.real/b, a.imag/b); }
  public static Complex operator/(double a, Complex b) { return new Complex(a)/b; }

  public static Complex operator-(Complex a) { return new Complex(-a.real, -a.imag); }

  public static Complex operator==(Complex a, Complex b) { return a.real==b.real && a.imag==b.imag; }
  public static Complex operator==(Complex a, double b)  { return a.real==b && a.imag==0; }
  public static Complex operator==(double a, Complex b)  { return a==b.real && b.imag==0; }

  public static Complex operator!=(Complex a, Complex b) { return a.real!=b.real || a.imag!=b.imag; }
  public static Complex operator!=(Complex a, double b)  { return a.real!=b || a.imag!=0; }
  public static Complex operator!=(double a, Complex b)  { return a!=b.real || b.imag!=0; }
  
  internal Complex Pow(Complex power)
  { double r, i;
	  if(power.real==0 && power.imag==0) { r=1; i=0; }
	  else if(real==0 && imag==0)
	  { if(power.imag!=0 || power.real<0) throw Ops.DivideByZero("Complex Pow(): division by zero");
	    r=i=0;
	  }
	  else
	  { double vabs=Math.Sqrt(real*real, imag*imag), len=Math.Pow(vabs, power.real), at=Math.Atan2(imag, real),
	           phase=at*power.real;
		  if(power.imag!=0)
		  { len /= Math.Exp(at*power.imag);
			  phase += power.imag*Math.Log(vabs);
			}
  		r = len*Math.Cos(phase);
	  	i = len*Math.Sin(phase);
	  }
	  return new Complex(r, i);
  }
  
  internal Complex Pow(int power)
  { if(power>100 || power<-100) return pow(new Complex(power));
    else if(power>0) return powu(power);
	  else return new Complex(1, 0) / powu(-power);
  }

  internal Complex Pow(double power)
  { int p = (int)power;
    return p==power ? pow(p) : pow(new Complex(power));
  }

  internal static Complex Pow(double a, Complex b) { return new Complex(a).pow(b); }

  void powu(int power)
  { Complex r = new Complex(1, 0);
	  int mask = 1;
	  while(mask>0 && power>=mask)
	  { if((power&mask)!=0) r = r*this;
		  mask <<= 1;
		  this *= this;
	  }
	  return r;
  }
}

} // namespace Boa.Runtime