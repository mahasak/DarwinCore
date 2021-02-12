using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

namespace Darwin.Shared.Utility
{
    public static class Guard
    {
        [DebuggerStepThrough]
        public static T NotNull<T>([ValidatedNotNull]T value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        [DebuggerStepThrough]
        public static string NotNullOrEmpty([ValidatedNotNull]string value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        [DebuggerStepThrough]
        public static ICollection<T> NotNullOrEmpty<T>([ValidatedNotNull]ICollection<T> value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            if (value.Count == 0)
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        [DebuggerStepThrough]
        public static string NotNullOrWhiteSpace([ValidatedNotNull]string value, string name)
        {
            NotNullOrEmpty(value, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        [DebuggerStepThrough]
        public static bool Ensure(bool condition, string message, params object[] args)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return true;
        }

        [DebuggerStepThrough]
        public static T EnsureNotNull<T>([ValidatedNotNull]T value, string message, params object[] args)
            where T : class
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return value;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
        public ValidatedNotNullAttribute()
        {
        }
    }
}