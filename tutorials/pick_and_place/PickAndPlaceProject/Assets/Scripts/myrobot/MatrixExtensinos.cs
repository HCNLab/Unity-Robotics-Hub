using UnityEngine;

public static class MatrixExtensions
{
    public static Matrix4x4 Subtract(this Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                result[row, col] = a[row, col] - b[row, col];
            }
        }
        return result;
    }

    public static Matrix4x4 FloatDivide(this Matrix4x4 a, float divisor)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                result[row, col] = a[row, col] / divisor;
            }
        }
        return result;
    }
}
