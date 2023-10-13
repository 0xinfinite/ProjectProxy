using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public static class ExtendedMathmatics 
{

    public static float Remap(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
}
