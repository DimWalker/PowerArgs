﻿using PowerArgs;
using System;

namespace PowerArgs.Samples
{
    public class SearchSample
    {
        public static void Run()
        {
            // by default, the user can cancel via the escape key
            var result = new StatePickerAssistant().Search();
            
            // variation that disallows cancellation via the escape key.
            // var result = new StatePickerAssistant().Search(allowCancel: false);

            if (result == null)
            {
                ConsoleString.WriteLine("You cancelled the search", RGB.Yellow);
            }
            else
            {
                ConsoleString.WriteLine(new ConsoleString("You picked ") + result.DisplayText.ToConsoleString(RGB.Cyan));
            }
        }
    }  
}
