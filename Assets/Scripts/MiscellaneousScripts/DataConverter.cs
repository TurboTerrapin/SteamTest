/*
    DataConverter.cs
    - Used for sending strings and arrays across RPCs
    Contributor(s): Jake Schott
    Last Updated: 6/29/2026
*/

using System.Collections.Generic;

public class DataConverter
{
    public static string arrayToString(int[] to_convert)
    {
        string to_return = "";
        for (int i = 0; i < to_convert.Length; i++)
        {
            to_return += (char)to_convert[i];
        }
        return to_return;
    }
    public static string arrayToString(bool[] to_convert)
    {
        string to_return = "";
        for (int i = 0; i < to_convert.Length; i++)
        {
            if (to_convert[i] == true)
            {
                to_return += "1";
            }
            else
            {
                to_return += "0";
            }
        }
        return to_return;
    }

    public static string listToString(List<int> to_convert)
    {
        string to_return = "";
        for (int i = 0; i < to_convert.Count; i++)
        {
            to_return += (char)to_convert[i];
        }
        return to_return;
    }

    public static int[] stringToArray(string to_convert)
    {
        int[] return_array = new int[to_convert.Length];
        for (int i = 0; i < to_convert.Length; i++)
        {
            return_array[i] = (int)to_convert[i];
        }
        return return_array;
    }

    public static List<int> stringToList(string to_convert)
    {
        List<int> return_list = new List<int>();
        for (int i = 0; i < to_convert.Length; i++)
        {
            return_list.Add((int)to_convert[i]);
        }
        return return_list;
    }
}
