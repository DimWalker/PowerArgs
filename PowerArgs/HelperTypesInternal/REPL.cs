﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    internal static class REPL
    {
        internal static T DriveREPL<T>(TabCompletion t, Func<string[], T> eval, string[] args, Func<T> defaultEval) where T : class
        {
            T ret = null;

            bool first = true;
            do
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    args = string.IsNullOrWhiteSpace(t.Indicator) ? new string[0] : new string[] { t.Indicator };
                }

                try
                {
                    ret = eval(args);
                }
                catch (REPLExitException)
                {
                    return ret ?? defaultEval();
                }
                catch (REPLContinueException)
                {
                    if(ConsoleProvider.Current.CursorLeft > 0)
                    {
                        ConsoleProvider.Current.WriteLine();
                    }
                }
            }
            while (t != null && t.REPL);

            return ret;
        }


        internal static async Task<T> DriveREPLAsync<T>(TabCompletion t, Func<string[], Task<T>> eval, string[] args, Func<T> defaultEval) where T : class
        {
            T ret = null;

            bool first = true;
            do
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    args = string.IsNullOrWhiteSpace(t.Indicator) ? new string[0] : new string[] { t.Indicator };
                }

                try
                {
                    ret = await eval(args);
                }
                catch (REPLExitException)
                {
                    return ret ?? defaultEval();
                }
                catch (REPLContinueException)
                {
                    if (ConsoleProvider.Current.CursorLeft > 0)
                    {
                        ConsoleProvider.Current.WriteLine();
                    }
                }
            }
            while (t != null && t.REPL);

            return ret;
        }
    }
}
