﻿using LibProtection.Injections.Caching;
using System;

namespace LibProtection.Injections
{
    public static class SafeString<T> where T : LanguageProvider
    {
        public static void SetCustomCache(ICustomCache customCache)
            => FormatProvider.SetCustomCache<T>(customCache);

        public static void SetDefaultCacheSize(int cacheTableSize)
            => FormatProvider.SetDefaultCacheSize<T>(cacheTableSize);

        public static string Format(FormattableString formattable) 
        {
            if (TryFormat(formattable, out var formatted))
            {
                return formatted;
            }
            
            throw new AttackDetectedException();
        }

        public static string Format(string format, params object[] args) 
        {
            if (TryFormat(format, out var formatted, args))
            {
                return formatted;
            }
            
            throw new AttackDetectedException();
        }

        public static bool TryFormat(FormattableString formattable, out string formatted)
        {
            return FormatProvider.TryFormat<T>(formattable.Format, out formatted, formattable.GetArguments());
        }

        public static bool TryFormat(string format, out string formatted, params object[] args) 
        {
            return FormatProvider.TryFormat<T>(format, out formatted, args);
        }
    }
}