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
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public sealed class TypeMaker
{ TypeMaker() { }

  // TODO: override __str__ and GetHashCode() (?) and stuff
  public static Type MakeType(string module, string name, object[] bases, IDictionary dict)
  { Type baseType = FindBaseType(bases), type = (Type)types[baseType];
    if(type!=null) return type;

    TypeGenerator typeGen = SnippetMaker.Assembly.DefineType("Boa.Types."+baseType.FullName, baseType);;
    CodeGenerator cg;
    Slot classField, dictField;

    if(typeof(IInstance).IsAssignableFrom(baseType))
    { classField = new FieldSlot(new ThisSlot(typeGen.TypeBuilder),
                                 baseType.GetField("__class__", BindingFlags.NonPublic|BindingFlags.Instance));
      dictField  = new FieldSlot(new ThisSlot(typeGen.TypeBuilder),
                                 baseType.GetField("__dict__", BindingFlags.NonPublic|BindingFlags.Instance));
    }
    else
    { // add fields
      classField = typeGen.DefineField("__class__", typeof(Dict), FieldAttributes.Family);
      dictField  = typeGen.DefineField("__dict__", typeof(UserType), FieldAttributes.Family);

      // implement IInterface
      typeGen.TypeBuilder.AddInterfaceImplementation(typeof(IInstance));
      
      cg = typeGen.DefineMethodOverride(typeof(IDynamicObject).GetMethod("GetDynamicType"));
      classField.EmitGet(cg);
      cg.EmitReturn();
      cg.Finish();
      
      cg = typeGen.DefineMethodOverride(typeof(IInstance).GetProperty("__class__").GetGetMethod());
      classField.EmitGet(cg);
      cg.EmitReturn();
      cg.Finish();
      
      cg = typeGen.DefineMethodOverride(typeof(IInstance).GetProperty("__class__").GetSetMethod());
      Label first = cg.ILG.DefineLabel();
      classField.EmitGet(cg);
      cg.ILG.Emit(OpCodes.Brfalse_S, first);
      cg.EmitString("__class__ can only be set once");
      cg.EmitNew(typeof(InvalidOperationException), new Type[] { typeof(string) });
      cg.ILG.Emit(OpCodes.Throw);
      cg.ILG.MarkLabel(first);
      cg.EmitArgGet(0);
      classField.EmitSet(cg);
      cg.EmitReturn();
      cg.Finish();

      cg = typeGen.DefineMethodOverride(typeof(IInstance).GetProperty("__dict__").GetGetMethod());
      dictField.EmitGet(cg);
      cg.EmitReturn();
      cg.Finish();
    }

    // TODO: think about this. if we're deriving from multiple real base classes, what should we do?
    //       also, should we be exposing protected constructors directly like this?
    // create constructors
    foreach(ConstructorInfo ci in
            baseType.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))
    { cg = typeGen.DefineChainedConstructor(ci);
      cg.EmitNew(typeof(Dict));
      dictField.EmitSet(cg);
      cg.EmitReturn();
      cg.Finish();
    }

    // ToString()
    /*cg = typeGen.DefineMethodOverride(baseType.GetMethod("ToString", Type.EmptyTypes));
    Label isinit = cg.ILG.DefineLabel();
    classField.EmitGet(cg);
    cg.ILG.Emit(OpCodes.Brtrue_S, isinit);
    cg.EmitString("<object instance>");
    cg.EmitReturn();
    cg.ILG.MarkLabel(isinit);
    cg.EmitString("<'");
    classField.EmitGet(cg);
    cg.EmitFieldGet(typeof(UserType), "__name__");
    cg.EmitCall(typeof(Ops), "Str");
    cg.EmitString("' instance>");
    cg.EmitCall(typeof(String), "Concat", new Type[] { typeof(string), typeof(string), typeof(string) });
    cg.EmitReturn();
    cg.Finish();*/

    types[baseType] = type = typeGen.FinishType();
    return type;
  }

  // TODO: make this more intelligent (ie, check ancestors higher up the tree ?)
  //       also, we can't REALLY derive from multiple .NET classes, so we'll need to do something like
  //       only inherit the static methods 
  static Type FindBaseType(object[] bases)
  { foreach(BoaType t in bases)
      if(typeof(Exception).IsAssignableFrom(t.TypeToInheritFrom)) return t.TypeToInheritFrom;

    foreach(BoaType t in bases)
      if(t is ReflectedType && !t.RealType.IsValueType && !t.RealType.IsSealed) return t.RealType;
    return typeof(object);
  }

  static HybridDictionary types = new HybridDictionary();
}

} // namespace Boa.AST