using System;
using System.Runtime.CompilerServices;

namespace CorsairLink;

public static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CreateRequest(byte command, int length)
    {
        var writeBuf = new byte[length];
        writeBuf[1] = command;
        return writeBuf;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CreateResponse(int length)
    {
        return new byte[length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }

        return value;
    }
}
