﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DelftTools.Tests.Controls.Swf.Table.TestClasses
{
    class ClassWithEnum
    {
        public FruitType Type {
            get;
            set;}
    }

    //Type converter is needed when the enum is used in a datatable for xtragrid
    [TypeConverter(typeof(EnumToInt32TypeConverter<FruitType>))]
    internal enum FruitType
    {
        Appel,Peer,Banaan
    }


    public class EnumToInt32TypeConverter<TEnum> : EnumConverter
    {

        public EnumToInt32TypeConverter(Type type)
            : base(type)
        {
            // since generic type parameters can't be constrained to enums,
            //  this asset is required to perform the logic check

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(int))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(int))
                return (TEnum)value;
            return base.ConvertFrom(context, culture, value);
        }
    }
}
