/*
    UniversalCommunicatorCodeData.cs
    - Data holder for universal communicator codes
    Contributor(s): Jake Schott
    Last Updated: 6/29/2026
*/

using UnityEngine;
using System.Collections.Generic;

public class UniversalCommunicatorCodeData : MonoBehaviour
{
    private int[] code_indexes = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
    private bool[] code_is_numeric = new bool[] { false, false, false, false, false, false, false, false };
    private int[] code_colors = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };

    public void setCodeIndexes(List<int> code)
    {
        for (int i = 0; i < code.Count; i++)
        {
            if (i > 7)
            {
                break;
            }
            code_indexes[i] = code[i];
        }
    }

    public void setCodeIndexes(int[] code)
    {
        for (int i = 0; i < code.Length; i++)
        {
            if (i > 7)
            {
                break;
            }
            code_indexes[i] = code[i];
        }
    }

    public void setCodeIsNumeric(List<bool> is_numeric)
    {
        for (int i = 0; i < is_numeric.Count; i++)
        {
            if (i > 7)
            {
                break;
            }
            code_is_numeric[i] = is_numeric[i];
        }
    }

    public void setCodeIsNumeric(bool[] is_numeric)
    {
        for (int i = 0; i < is_numeric.Length; i++)
        {
            if (i > 7)
            {
                break;
            }
            code_is_numeric[i] = is_numeric[i];
        }
    }

    public void setCodeColors(List<int> colors)
    {
        for (int i = 0; i < colors.Count; i++)
        {
            if (i > 7)
            {
                break;
            }
            if (colors[i] >= 0)
            {
                code_colors[i] = colors[i];
            }
        }
    }

    public void setCodeColors(int[] colors)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            if (i > 7)
            {
                break;
            }
            if (colors[i] >= 0)
            {
                code_colors[i] = colors[i];
            }
        }
    }

    public int[] getCodeIndexes()
    {
        return code_indexes;
    }

    public bool[] getCodeIsNumeric()
    {
        return code_is_numeric;
    }

    public int[] getCodeColors()
    {
        return code_colors;
    }
}