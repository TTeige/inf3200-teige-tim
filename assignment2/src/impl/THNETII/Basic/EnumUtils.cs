using System;
using System.ComponentModel;
using System.Reflection;

namespace THNETII
{
    public static class EnumUtils
    {
        public static string GetDescription<T>(T enumValue, string defDesc = null)
        {
            FieldInfo fi = enumValue.GetType().GetField(enumValue.ToString());

            if (null != fi)
            {
                object[] attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                    return ((DescriptionAttribute)(attrs[0])).Description;
            }

            return defDesc;
        }

        public static T FromDescription<T>(string description)
        {
            Type t = typeof(T);
            foreach (FieldInfo fi in t.GetFields())
            {
                object[] attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    foreach (DescriptionAttribute attr in attrs)
                    {
                        if (attr.Description.Equals(description))
                            return (T)(fi.GetValue(null));
                    }
                }
            }
            return default(T);
        }
    }
}
