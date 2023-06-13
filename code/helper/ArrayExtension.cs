using System.Collections;
using System.Collections.Generic;

public static class ArrayExtension
{
    public static List<T> Take<T>(this T[] array, int count) {
        List <T> list = new List<T>();

        if(count < 0) { throw new System.ArgumentException("Count can not be less than 0!"); }

        for (int i = 0; i < count; i++) {
            list.Add(array[i]);
        }

        return list;

    }

}
