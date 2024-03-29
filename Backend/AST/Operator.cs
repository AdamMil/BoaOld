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
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

#region Base classes
public abstract class Operator
{ public abstract void Emit(CodeGenerator cg);
}

public abstract class UnaryOperator : Operator
{ public abstract object Evaluate(object value);

  public static readonly BitwiseNotOperator BitwiseNot = new BitwiseNotOperator();
  public static readonly LogicalNotOperator LogicalNot = new LogicalNotOperator();
  public static readonly UnaryMinusOperator UnaryMinus = new UnaryMinusOperator();
}

public abstract class BinaryOperator : Operator
{ public abstract object Evaluate(object lhs, object rhs);

  public static readonly EqualOperator        Equal = new EqualOperator();
  public static readonly NotEqualOperator     NotEqual = new NotEqualOperator();
  public static readonly IdenticalOperator    Identical = new IdenticalOperator();
  public static readonly NotIdenticalOperator NotIdentical = new NotIdenticalOperator();
  public static readonly LessOperator         Less = new LessOperator();
  public static readonly LessEqualOperator    LessEqual = new LessEqualOperator();
  public static readonly MoreOperator         More = new MoreOperator();
  public static readonly MoreEqualOperator    MoreEqual = new MoreEqualOperator();

  public static readonly AddOperator          Add = new AddOperator();
  public static readonly SubtractOperator     Subtract = new SubtractOperator();
  public static readonly MultiplyOperator     Multiply = new MultiplyOperator();
  public static readonly DivideOperator       Divide = new DivideOperator();
  public static readonly FloorDivideOperator  FloorDivide = new FloorDivideOperator();
  public static readonly ModulusOperator      Modulus = new ModulusOperator();
  public static readonly PowerOperator        Power = new PowerOperator();
  public static readonly LeftShiftOperator    LeftShift = new LeftShiftOperator();
  public static readonly RightShiftOperator   RightShift = new RightShiftOperator();
  public static readonly BitwiseAndOperator   BitwiseAnd = new BitwiseAndOperator();
  public static readonly BitwiseOrOperator    BitwiseOr = new BitwiseOrOperator();
  public static readonly BitwiseXorOperator   BitwiseXor = new BitwiseXorOperator();
  public static readonly LogicalAndOperator   LogicalAnd = new LogicalAndOperator();
  public static readonly LogicalOrOperator    LogicalOr = new LogicalOrOperator();
}
#endregion

#region Unary operators
public sealed class BitwiseNotOperator : UnaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "BitwiseNegate"); }
  public override object Evaluate(object value) { return Ops.BitwiseNegate(value); }
  public override string ToString() { return "~"; }
}

public sealed class LogicalNotOperator : UnaryOperator
{ public override void Emit(CodeGenerator cg)
  { cg.EmitIsFalse();
    cg.EmitCall(typeof(Ops), "FromBool");
  }
  public override object Evaluate(object value) { return Ops.FromBool(!Ops.IsTrue(value)); }
  public override string ToString() { return "!"; }
}

public class UnaryMinusOperator : UnaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Negate"); }
  public override object Evaluate(object value) { return Ops.Negate(value); }
  public override string ToString() { return " -"; }
}
#endregion

#region Comparison operators
public sealed class EqualOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Equal"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Equal(lhs, rhs); }
  public override string ToString() { return " == "; }
}

public sealed class NotEqualOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "NotEqual"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.NotEqual(lhs, rhs); }
  public override string ToString() { return " != "; }
}

public sealed class IdenticalOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg)
  { cg.ILG.Emit(OpCodes.Ceq);
    cg.EmitCall(typeof(Ops), "FromBool");
  }
  public override object Evaluate(object lhs, object rhs) { return Ops.FromBool(lhs==rhs); }
  public override string ToString() { return " is "; }
}

public sealed class NotIdenticalOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg)
  { cg.ILG.Emit(OpCodes.Ceq); cg.EmitInt(0); cg.ILG.Emit(OpCodes.Ceq);
    cg.EmitCall(typeof(Ops), "FromBool");
  }
  public override object Evaluate(object lhs, object rhs) { return Ops.FromBool(lhs!=rhs); }
  public override string ToString() { return " is not "; }
}

public sealed class LessOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Less"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Less(lhs, rhs); }
  public override string ToString() { return " < "; }
}

public sealed class LessEqualOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Add"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.LessEqual(lhs, rhs); }
  public override string ToString() { return " <= "; }
}

public sealed class MoreOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "More"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.More(lhs, rhs); }
  public override string ToString() { return " > "; }
}

public sealed class MoreEqualOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "MoreEqual"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.MoreEqual(lhs, rhs); }
  public override string ToString() { return " >= "; }
}
#endregion

#region Binary operators
public sealed class AddOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Add"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Add(lhs, rhs); }
  public override string ToString() { return " + "; }
}

public sealed class SubtractOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Subtract"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Subtract(lhs, rhs); }
  public override string ToString() { return " - "; }
}

public sealed class MultiplyOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Multiply"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Multiply(lhs, rhs); }
  public override string ToString() { return " * "; }
}

public sealed class DivideOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Divide"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Divide(lhs, rhs); }
  public override string ToString() { return " / "; }
}

public sealed class FloorDivideOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "FloorDivide"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.FloorDivide(lhs, rhs); }
  public override string ToString() { return " // "; }
}

public sealed class ModulusOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Modulus"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Modulus(lhs, rhs); }
  public override string ToString() { return " % "; }
}

public sealed class PowerOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "Power"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.Power(lhs, rhs); }
  public override string ToString() { return " ** "; }
}

public sealed class LeftShiftOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "LeftShift"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.LeftShift(lhs, rhs); }
  public override string ToString() { return " << "; }
}

public sealed class RightShiftOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "RightShift"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.RightShift(lhs, rhs); }
  public override string ToString() { return " >> "; }
}

public sealed class BitwiseAndOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "BitwiseAnd"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.BitwiseAnd(lhs, rhs); }
  public override string ToString() { return " & "; }
}

public sealed class BitwiseOrOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "BitwiseOr"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.BitwiseOr(lhs, rhs); }
  public override string ToString() { return " | "; }
}

public sealed class BitwiseXorOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "BitwiseXor"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.BitwiseXor(lhs, rhs); }
  public override string ToString() { return " ^ "; }
}

public sealed class LogicalAndOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "LogicalAnd"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.LogicalAnd(lhs, rhs); }
  public override string ToString() { return " && "; }
}

public sealed class LogicalOrOperator : BinaryOperator
{ public override void Emit(CodeGenerator cg) { cg.EmitCall(typeof(Ops), "LogicalOr"); }
  public override object Evaluate(object lhs, object rhs) { return Ops.LogicalOr(lhs, rhs); }
  public override string ToString() { return " || "; }
}
#endregion

} // namespace Boa.AST
