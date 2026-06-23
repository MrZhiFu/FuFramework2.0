namespace FuFramework.Utility.Math;

/// <summary>
/// 4x4 矩阵结构体，用于表示三维空间中的变换。
/// </summary>
public struct FPMatrix4x4
{
	/// <summary>
	/// 第一行第一列的元素。
	/// </summary>
	public FP M11;

	/// <summary>
	/// 第一行第二列的元素。
	/// </summary>
	public FP M12;

	/// <summary>
	/// 第一行第三列的元素。
	/// </summary>
	public FP M13;

	/// <summary>
	/// 第一行第四列的元素。
	/// </summary>
	public FP M14;

	/// <summary>
	/// 第二行第一列的元素。
	/// </summary>
	public FP M21;

	/// <summary>
	/// 第二行第二列的元素。
	/// </summary>
	public FP M22;

	/// <summary>
	/// 第二行第三列的元素。
	/// </summary>
	public FP M23;

	/// <summary>
	/// 第二行第四列的元素。
	/// </summary>
	public FP M24;

	/// <summary>
	/// 第三行第一列的元素。
	/// </summary>
	public FP M31;

	/// <summary>
	/// 第三行第二列的元素。
	/// </summary>
	public FP M32;

	/// <summary>
	/// 第三行第三列的元素。
	/// </summary>
	public FP M33;

	/// <summary>
	/// 第三行第四列的元素。
	/// </summary>
	public FP M34;

	/// <summary>
	/// 第四行第一列的元素。
	/// </summary>
	public FP M41;

	/// <summary>
	/// 第四行第二列的元素。
	/// </summary>
	public FP M42;

	/// <summary>
	/// 第四行第三列的元素。
	/// </summary>
	public FP M43;

	/// <summary>
	/// 第四行第四列的元素。
	/// </summary>
	public FP M44;

	internal static FPMatrix4x4 InternalIdentity;

	/// <summary>
	/// 单位矩阵。
	/// </summary>
	public static readonly FPMatrix4x4 Identity;

	/// <summary>
	/// 零矩阵。
	/// </summary>
	public static readonly FPMatrix4x4 Zero;

	/// <summary>
	/// 决定因素
	/// </summary>
	public FP determinant
	{
		get
		{
			FP m = M11;
			FP m2 = M12;
			FP m3 = M13;
			FP m4 = M14;
			FP m5 = M21;
			FP m6 = M22;
			FP m7 = M23;
			FP m8 = M24;
			FP m9 = M31;
			FP m10 = M32;
			FP m11 = M33;
			FP m12 = M34;
			FP m13 = M41;
			FP m14 = M42;
			FP m15 = M43;
			FP m16 = M44;
			FP fP = m11 * m16 - m12 * m15;
			FP fP2 = m10 * m16 - m12 * m14;
			FP fP3 = m10 * m15 - m11 * m14;
			FP fP4 = m9 * m16 - m12 * m13;
			FP fP5 = m9 * m15 - m11 * m13;
			FP fP6 = m9 * m14 - m10 * m13;
			return m * (m6 * fP - m7 * fP2 + m8 * fP3) - m2 * (m5 * fP - m7 * fP4 + m8 * fP5) + m3 * (m5 * fP2 - m6 * fP4 + m8 * fP6) - m4 * (m5 * fP3 - m6 * fP5 + m7 * fP6);
		}
	}

	static FPMatrix4x4()
	{
		Zero = default(FPMatrix4x4);
		Identity = default(FPMatrix4x4);
		Identity.M11 = FP.One;
		Identity.M22 = FP.One;
		Identity.M33 = FP.One;
		Identity.M44 = FP.One;
		InternalIdentity = Identity;
	}

	/// <summary>
	/// 初始化矩阵结构的新实例。
	/// </summary>
	/// <param name="m11">第一行第一列的值。</param>
	/// <param name="m12">第一行第二列的值。</param>
	/// <param name="m13">第一行第三列的值。</param>
	/// <param name="m14">第一行第四列的值。</param>
	/// <param name="m21">第二行第一列的值。</param>
	/// <param name="m22">第二行第二列的值。</param>
	/// <param name="m23">第二行第三列的值。</param>
	/// <param name="m24">第二行第四列的值。</param>
	/// <param name="m31">第三行第一列的值。</param>
	/// <param name="m32">第三行第二列的值。</param>
	/// <param name="m33">第三行第三列的值。</param>
	/// <param name="m34">第三行第四列的值。</param>
	/// <param name="m41">第四行第一列的值。</param>
	/// <param name="m42">第四行第二列的值。</param>
	/// <param name="m43">第四行第三列的值。</param>
	/// <param name="m44">第四行第四列的值。</param>
	public FPMatrix4x4(FP m11, FP m12, FP m13, FP m14, FP m21, FP m22, FP m23, FP m24, FP m31, FP m32, FP m33, FP m34, FP m41, FP m42, FP m43, FP m44)
	{
		M11 = m11;
		M12 = m12;
		M13 = m13;
		M14 = m14;
		M21 = m21;
		M22 = m22;
		M23 = m23;
		M24 = m24;
		M31 = m31;
		M32 = m32;
		M33 = m33;
		M34 = m34;
		M41 = m41;
		M42 = m42;
		M43 = m43;
		M44 = m44;
	}

	/// <summary>
	/// 计算两个矩阵的乘积。注意：矩阵乘法不是交换的。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <returns>两个矩阵的乘积。</returns>
	public static FPMatrix4x4 Multiply(FPMatrix4x4 matrix1, FPMatrix4x4 matrix2)
	{
		Multiply(ref matrix1, ref matrix2, out var result);
		return result;
	}

	/// <summary>
	/// 计算两个矩阵的乘积。注意：矩阵乘法不是交换的。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <param name="result">两个矩阵的乘积。</param>
	public static void Multiply(ref FPMatrix4x4 matrix1, ref FPMatrix4x4 matrix2, out FPMatrix4x4 result)
	{
		result.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
		result.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
		result.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
		result.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
		result.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
		result.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
		result.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
		result.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
		result.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
		result.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
		result.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
		result.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
		result.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
		result.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
		result.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
		result.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
	}

	/// <summary>
	/// 将两个矩阵相加。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <returns>两个矩阵的和。</returns>
	public static FPMatrix4x4 Add(FPMatrix4x4 matrix1, FPMatrix4x4 matrix2)
	{
		Add(ref matrix1, ref matrix2, out var result);
		return result;
	}

	/// <summary>
	/// 将两个矩阵相加。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <param name="result">两个矩阵的和。</param>
	public static void Add(ref FPMatrix4x4 matrix1, ref FPMatrix4x4 matrix2, out FPMatrix4x4 result)
	{
		result.M11 = matrix1.M11 + matrix2.M11;
		result.M12 = matrix1.M12 + matrix2.M12;
		result.M13 = matrix1.M13 + matrix2.M13;
		result.M14 = matrix1.M14 + matrix2.M14;
		result.M21 = matrix1.M21 + matrix2.M21;
		result.M22 = matrix1.M22 + matrix2.M22;
		result.M23 = matrix1.M23 + matrix2.M23;
		result.M24 = matrix1.M24 + matrix2.M24;
		result.M31 = matrix1.M31 + matrix2.M31;
		result.M32 = matrix1.M32 + matrix2.M32;
		result.M33 = matrix1.M33 + matrix2.M33;
		result.M34 = matrix1.M34 + matrix2.M34;
		result.M41 = matrix1.M41 + matrix2.M41;
		result.M42 = matrix1.M42 + matrix2.M42;
		result.M43 = matrix1.M43 + matrix2.M43;
		result.M44 = matrix1.M44 + matrix2.M44;
	}

	/// <summary>
	/// 计算给定矩阵的逆矩阵。
	/// </summary>
	/// <param name="matrix">要计算逆的矩阵。</param>
	/// <returns>逆矩阵。</returns>
	public static FPMatrix4x4 Inverse(FPMatrix4x4 matrix)
	{
		Inverse(ref matrix, out var result);
		return result;
	}

	/// <summary>
	/// 计算给定矩阵的逆矩阵。
	/// </summary>
	/// <param name="matrix">要计算逆的矩阵。</param>
	/// <param name="result">逆矩阵。</param>
	public static void Inverse(ref FPMatrix4x4 matrix, out FPMatrix4x4 result)
	{
		FP m = matrix.M11;
		FP m2 = matrix.M12;
		FP m3 = matrix.M13;
		FP m4 = matrix.M14;
		FP m5 = matrix.M21;
		FP m6 = matrix.M22;
		FP m7 = matrix.M23;
		FP m8 = matrix.M24;
		FP m9 = matrix.M31;
		FP m10 = matrix.M32;
		FP m11 = matrix.M33;
		FP m12 = matrix.M34;
		FP m13 = matrix.M41;
		FP m14 = matrix.M42;
		FP m15 = matrix.M43;
		FP m16 = matrix.M44;
		FP fP = m11 * m16 - m12 * m15;
		FP fP2 = m10 * m16 - m12 * m14;
		FP fP3 = m10 * m15 - m11 * m14;
		FP fP4 = m9 * m16 - m12 * m13;
		FP fP5 = m9 * m15 - m11 * m13;
		FP fP6 = m9 * m14 - m10 * m13;
		FP fP7 = m6 * fP - m7 * fP2 + m8 * fP3;
		FP fP8 = -(m5 * fP - m7 * fP4 + m8 * fP5);
		FP fP9 = m5 * fP2 - m6 * fP4 + m8 * fP6;
		FP fP10 = -(m5 * fP3 - m6 * fP5 + m7 * fP6);
		FP fP11 = m * fP7 + m2 * fP8 + m3 * fP9 + m4 * fP10;
		if (fP11 == FP.Zero)
		{
			result.M11 = FP.PositiveInfinity;
			result.M12 = FP.PositiveInfinity;
			result.M13 = FP.PositiveInfinity;
			result.M14 = FP.PositiveInfinity;
			result.M21 = FP.PositiveInfinity;
			result.M22 = FP.PositiveInfinity;
			result.M23 = FP.PositiveInfinity;
			result.M24 = FP.PositiveInfinity;
			result.M31 = FP.PositiveInfinity;
			result.M32 = FP.PositiveInfinity;
			result.M33 = FP.PositiveInfinity;
			result.M34 = FP.PositiveInfinity;
			result.M41 = FP.PositiveInfinity;
			result.M42 = FP.PositiveInfinity;
			result.M43 = FP.PositiveInfinity;
			result.M44 = FP.PositiveInfinity;
		}
		else
		{
			FP fP12 = FP.One / fP11;
			result.M11 = fP7 * fP12;
			result.M21 = fP8 * fP12;
			result.M31 = fP9 * fP12;
			result.M41 = fP10 * fP12;
			result.M12 = -(m2 * fP - m3 * fP2 + m4 * fP3) * fP12;
			result.M22 = (m * fP - m3 * fP4 + m4 * fP5) * fP12;
			result.M32 = -(m * fP2 - m2 * fP4 + m4 * fP6) * fP12;
			result.M42 = (m * fP3 - m2 * fP5 + m3 * fP6) * fP12;
			FP fP13 = m7 * m16 - m8 * m15;
			FP fP14 = m6 * m16 - m8 * m14;
			FP fP15 = m6 * m15 - m7 * m14;
			FP fP16 = m5 * m16 - m8 * m13;
			FP fP17 = m5 * m15 - m7 * m13;
			FP fP18 = m5 * m14 - m6 * m13;
			result.M13 = (m2 * fP13 - m3 * fP14 + m4 * fP15) * fP12;
			result.M23 = -(m * fP13 - m3 * fP16 + m4 * fP17) * fP12;
			result.M33 = (m * fP14 - m2 * fP16 + m4 * fP18) * fP12;
			result.M43 = -(m * fP15 - m2 * fP17 + m3 * fP18) * fP12;
			FP fP19 = m7 * m12 - m8 * m11;
			FP fP20 = m6 * m12 - m8 * m10;
			FP fP21 = m6 * m11 - m7 * m10;
			FP fP22 = m5 * m12 - m8 * m9;
			FP fP23 = m5 * m11 - m7 * m9;
			FP fP24 = m5 * m10 - m6 * m9;
			result.M14 = -(m2 * fP19 - m3 * fP20 + m4 * fP21) * fP12;
			result.M24 = (m * fP19 - m3 * fP22 + m4 * fP23) * fP12;
			result.M34 = -(m * fP20 - m2 * fP22 + m4 * fP24) * fP12;
			result.M44 = (m * fP21 - m2 * fP23 + m3 * fP24) * fP12;
		}
	}

	/// <summary>
	/// 将矩阵乘以一个缩放因子。
	/// </summary>
	/// <param name="matrix1">要缩放的矩阵。</param>
	/// <param name="scaleFactor">缩放因子。</param>
	/// <returns>缩放后的矩阵。</returns>
	public static FPMatrix4x4 Multiply(FPMatrix4x4 matrix1, FP scaleFactor)
	{
		Multiply(ref matrix1, scaleFactor, out var result);
		return result;
	}

	/// <summary>
	/// 将矩阵乘以一个缩放因子。
	/// </summary>
	/// <param name="matrix1">要缩放的矩阵。</param>
	/// <param name="scaleFactor">缩放因子。</param>
	/// <param name="result">缩放后的矩阵。</param>
	public static void Multiply(ref FPMatrix4x4 matrix1, FP scaleFactor, out FPMatrix4x4 result)
	{
		result.M11 = matrix1.M11 * scaleFactor;
		result.M12 = matrix1.M12 * scaleFactor;
		result.M13 = matrix1.M13 * scaleFactor;
		result.M14 = matrix1.M14 * scaleFactor;
		result.M21 = matrix1.M21 * scaleFactor;
		result.M22 = matrix1.M22 * scaleFactor;
		result.M23 = matrix1.M23 * scaleFactor;
		result.M24 = matrix1.M24 * scaleFactor;
		result.M31 = matrix1.M31 * scaleFactor;
		result.M32 = matrix1.M32 * scaleFactor;
		result.M33 = matrix1.M33 * scaleFactor;
		result.M34 = matrix1.M34 * scaleFactor;
		result.M41 = matrix1.M41 * scaleFactor;
		result.M42 = matrix1.M42 * scaleFactor;
		result.M43 = matrix1.M43 * scaleFactor;
		result.M44 = matrix1.M44 * scaleFactor;
	}

	/// <summary>
	/// 根据四元数创建旋转矩阵。
	/// </summary>
	/// <param name="quaternion">用于创建矩阵的四元数。</param>
	/// <returns>表示方向的旋转矩阵。</returns>
	public static FPMatrix4x4 Rotate(FPQuaternion quaternion)
	{
		Rotate(ref quaternion, out var result);
		return result;
	}

	/// <summary>
	/// 根据四元数创建旋转矩阵。
	/// </summary>
	/// <param name="quaternion">用于创建矩阵的四元数。</param>
	/// <param name="result">表示方向的旋转矩阵。</param>
	public static void Rotate(ref FPQuaternion quaternion, out FPMatrix4x4 result)
	{
		FP fP = quaternion.x * 2;
		FP fP2 = quaternion.y * 2;
		FP fP3 = quaternion.z * 2;
		FP fP4 = quaternion.x * fP;
		FP fP5 = quaternion.y * fP2;
		FP fP6 = quaternion.z * fP3;
		FP fP7 = quaternion.x * fP2;
		FP fP8 = quaternion.x * fP3;
		FP fP9 = quaternion.y * fP3;
		FP fP10 = quaternion.w * fP;
		FP fP11 = quaternion.w * fP2;
		FP fP12 = quaternion.w * fP3;
		result.M11 = FP.One - (fP5 + fP6);
		result.M21 = fP7 + fP12;
		result.M31 = fP8 - fP11;
		result.M41 = FP.Zero;
		result.M12 = fP7 - fP12;
		result.M22 = FP.One - (fP4 + fP6);
		result.M32 = fP9 + fP10;
		result.M42 = FP.Zero;
		result.M13 = fP8 + fP11;
		result.M23 = fP9 - fP10;
		result.M33 = FP.One - (fP4 + fP5);
		result.M43 = FP.Zero;
		result.M14 = FP.Zero;
		result.M24 = FP.Zero;
		result.M34 = FP.Zero;
		result.M44 = FP.One;
	}

	/// <summary>
	/// 创建转置矩阵。
	/// </summary>
	/// <param name="matrix">要转置的矩阵。</param>
	/// <returns>转置后的矩阵。</returns>
	public static FPMatrix4x4 Transpose(FPMatrix4x4 matrix)
	{
		Transpose(ref matrix, out var result);
		return result;
	}

	/// <summary>
	/// 创建转置矩阵。
	/// </summary>
	/// <param name="matrix">要转置的矩阵。</param>
	/// <param name="result">转置后的矩阵。</param>
	public static void Transpose(ref FPMatrix4x4 matrix, out FPMatrix4x4 result)
	{
		result.M11 = matrix.M11;
		result.M12 = matrix.M21;
		result.M13 = matrix.M31;
		result.M14 = matrix.M41;
		result.M21 = matrix.M12;
		result.M22 = matrix.M22;
		result.M23 = matrix.M32;
		result.M24 = matrix.M42;
		result.M31 = matrix.M13;
		result.M32 = matrix.M23;
		result.M33 = matrix.M33;
		result.M34 = matrix.M43;
		result.M41 = matrix.M14;
		result.M42 = matrix.M24;
		result.M43 = matrix.M34;
		result.M44 = matrix.M44;
	}

	/// <summary>
	/// 重载乘法运算符，计算两个矩阵的乘积。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>两个矩阵的乘积。</returns>
	public static FPMatrix4x4 operator *(FPMatrix4x4 value1, FPMatrix4x4 value2)
	{
		Multiply(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 计算矩阵的迹。
	/// </summary>
	/// <returns>矩阵的迹。</returns>
	public FP Trace()
	{
		return M11 + M22 + M33 + M44;
	}

	/// <summary>
	/// 重载加法运算符，计算两个矩阵的和。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>两个矩阵的和。</returns>
	public static FPMatrix4x4 operator +(FPMatrix4x4 value1, FPMatrix4x4 value2)
	{
		Add(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 返回给定矩阵的元素取反后的新矩阵。
	/// </summary>
	/// <param name="value">源矩阵。</param>
	/// <returns>取反后的矩阵。</returns>
	public static FPMatrix4x4 operator -(FPMatrix4x4 value)
	{
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = -value.M11;
		result.M12 = -value.M12;
		result.M13 = -value.M13;
		result.M14 = -value.M14;
		result.M21 = -value.M21;
		result.M22 = -value.M22;
		result.M23 = -value.M23;
		result.M24 = -value.M24;
		result.M31 = -value.M31;
		result.M32 = -value.M32;
		result.M33 = -value.M33;
		result.M34 = -value.M34;
		result.M41 = -value.M41;
		result.M42 = -value.M42;
		result.M43 = -value.M43;
		result.M44 = -value.M44;
		return result;
	}

	/// <summary>
	/// 重载减法运算符，计算两个矩阵的差。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>两个矩阵的差。</returns>
	public static FPMatrix4x4 operator -(FPMatrix4x4 value1, FPMatrix4x4 value2)
	{
		Multiply(ref value2, -FP.One, out value2);
		Add(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 重载相等运算符，判断两个矩阵是否相等。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>如果两个矩阵相等，则返回 true；否则返回 false。</returns>
	public static bool operator ==(FPMatrix4x4 value1, FPMatrix4x4 value2)
	{
		if (value1.M11 == value2.M11 && value1.M12 == value2.M12 && value1.M13 == value2.M13 && value1.M14 == value2.M14 && value1.M21 == value2.M21 && value1.M22 == value2.M22 && value1.M23 == value2.M23 && value1.M24 == value2.M24 && value1.M31 == value2.M31 && value1.M32 == value2.M32 && value1.M33 == value2.M33 && value1.M34 == value2.M34 && value1.M41 == value2.M41 && value1.M42 == value2.M42 && value1.M43 == value2.M43)
		{
			return value1.M44 == value2.M44;
		}
		return false;
	}

	/// <summary>
	/// 重载不相等运算符，判断两个矩阵是否不相等。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>如果两个矩阵不相等，则返回 true；否则返回 false。</returns>
	public static bool operator !=(FPMatrix4x4 value1, FPMatrix4x4 value2)
	{
		if (!(value1.M11 != value2.M11) && !(value1.M12 != value2.M12) && !(value1.M13 != value2.M13) && !(value1.M14 != value2.M14) && !(value1.M21 != value2.M21) && !(value1.M22 != value2.M22) && !(value1.M23 != value2.M23) && !(value1.M24 != value2.M24) && !(value1.M31 != value2.M31) && !(value1.M32 != value2.M32) && !(value1.M33 != value2.M33) && !(value1.M34 != value2.M34) && !(value1.M41 != value2.M41) && !(value1.M42 != value2.M42) && !(value1.M43 != value2.M43))
		{
			return value1.M44 != value2.M44;
		}
		return true;
	}

	/// <summary>
	/// 重写 Equals 方法，判断当前矩阵是否与指定对象相等。
	/// </summary>
	/// <param name="obj">要比较的对象。</param>
	/// <returns>如果当前矩阵与指定对象相等，则返回 true；否则返回 false。</returns>
	public override bool Equals(object obj)
	{
		if (!(obj is FPMatrix4x4 fPMatrix4x))
		{
			return false;
		}
		if (M11 == fPMatrix4x.M11 && M12 == fPMatrix4x.M12 && M13 == fPMatrix4x.M13 && M14 == fPMatrix4x.M14 && M21 == fPMatrix4x.M21 && M22 == fPMatrix4x.M22 && M23 == fPMatrix4x.M23 && M24 == fPMatrix4x.M24 && M31 == fPMatrix4x.M31 && M32 == fPMatrix4x.M32 && M33 == fPMatrix4x.M33 && M34 == fPMatrix4x.M34 && M41 == fPMatrix4x.M41 && M42 == fPMatrix4x.M42 && M43 == fPMatrix4x.M43)
		{
			return M44 == fPMatrix4x.M44;
		}
		return false;
	}

	/// <summary>
	/// 重写 GetHashCode 方法，返回当前矩阵的哈希代码。
	/// </summary>
	/// <returns>当前矩阵的哈希代码。</returns>
	public override int GetHashCode()
	{
		return M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^ M14.GetHashCode() ^ M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^ M24.GetHashCode() ^ M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode() ^ M34.GetHashCode() ^ M41.GetHashCode() ^ M42.GetHashCode() ^ M43.GetHashCode() ^ M44.GetHashCode();
	}

	/// <summary>
	/// 创建平移矩阵。
	/// </summary>
	/// <param name="xPosition">在 X 轴上的平移量。</param>
	/// <param name="yPosition">在 Y 轴上的平移量。</param>
	/// <param name="zPosition">在 Z 轴上的平移量。</param>
	/// <returns>平移矩阵。</returns>
	public static FPMatrix4x4 Translate(FP xPosition, FP yPosition, FP zPosition)
	{
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = FP.One;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M14 = xPosition;
		result.M21 = FP.Zero;
		result.M22 = FP.One;
		result.M23 = FP.Zero;
		result.M24 = yPosition;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = FP.One;
		result.M34 = zPosition;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 根据三维向量创建平移矩阵。
	/// </summary>
	/// <param name="translation">平移向量。</param>
	/// <returns>平移矩阵。</returns>
	public static FPMatrix4x4 Translate(FPVector3 translation)
	{
		return Translate(translation.x, translation.y, translation.z);
	}

	/// <summary>
	/// 创建缩放矩阵。
	/// </summary>
	/// <param name="xScale">在 X 轴上的缩放值。</param>
	/// <param name="yScale">在 Y 轴上的缩放值。</param>
	/// <param name="zScale">在 Z 轴上的缩放值。</param>
	/// <returns>缩放矩阵。</returns>
	public static FPMatrix4x4 Scale(FP xScale, FP yScale, FP zScale)
	{
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = xScale;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M14 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = yScale;
		result.M23 = FP.Zero;
		result.M24 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = zScale;
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建带有中心点的缩放矩阵。
	/// </summary>
	/// <param name="xScale">在 X 轴上的缩放值。</param>
	/// <param name="yScale">在 Y 轴上的缩放值。</param>
	/// <param name="zScale">在 Z 轴上的缩放值。</param>
	/// <param name="centerPoint">缩放的中心点。</param>
	/// <returns>缩放矩阵。</returns>
	public static FPMatrix4x4 Scale(FP xScale, FP yScale, FP zScale, FPVector3 centerPoint)
	{
		FP m = centerPoint.x * (FP.One - xScale);
		FP m2 = centerPoint.y * (FP.One - yScale);
		FP m3 = centerPoint.z * (FP.One - zScale);
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = xScale;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M14 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = yScale;
		result.M23 = FP.Zero;
		result.M24 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = zScale;
		result.M34 = FP.Zero;
		result.M41 = m;
		result.M42 = m2;
		result.M43 = m3;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建缩放矩阵。
	/// </summary>
	/// <param name="scales">包含每个轴的缩放值的向量。</param>
	/// <returns>缩放矩阵。</returns>
	public static FPMatrix4x4 Scale(FPVector3 scales)
	{
		return Scale(scales.x, scales.y, scales.z);
	}

	/// <summary>
	/// 创建带有中心点的缩放矩阵。
	/// </summary>
	/// <param name="scales">包含每个轴的缩放值的向量。</param>
	/// <param name="centerPoint">缩放的中心点。</param>
	/// <returns>缩放矩阵。</returns>
	public static FPMatrix4x4 Scale(FPVector3 scales, FPVector3 centerPoint)
	{
		return Scale(scales.x, scales.y, scales.z, centerPoint);
	}

	/// <summary>
	/// 创建均匀缩放矩阵，使每个轴的缩放相等。
	/// </summary>
	/// <param name="scale">均匀缩放因子。</param>
	/// <returns>缩放矩阵。</returns>
	public static FPMatrix4x4 Scale(FP scale)
	{
		return Scale(scale, scale, scale);
	}

	/// <summary>
	/// 创建均匀缩放矩阵，使每个轴的缩放相等，并指定中心点。
	/// </summary>
	/// <param name="scale">均匀缩放因子。</param>
	/// <param name="centerPoint">缩放的中心点。</param>
	/// <returns>缩放矩阵。</returns>
	public static FPMatrix4x4 Scale(FP scale, FPVector3 centerPoint)
	{
		return Scale(scale, scale, scale, centerPoint);
	}

	/// <summary>
	/// 创建围绕 X 轴旋转的矩阵。
	/// </summary>
	/// <param name="radians">围绕 X 轴旋转的弧度。</param>
	/// <returns>旋转矩阵。</returns>
	public static FPMatrix4x4 RotateX(FP radians)
	{
		FP fP = FPMath.Cos(radians);
		FP fP2 = FPMath.Sin(radians);
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = FP.One;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M14 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = fP;
		result.M23 = fP2;
		result.M24 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = -fP2;
		result.M33 = fP;
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建围绕 X 轴旋转的矩阵，并指定中心点。
	/// </summary>
	/// <param name="radians">围绕 X 轴旋转的弧度。</param>
	/// <param name="centerPoint">旋转的中心点。</param>
	/// <returns>旋转矩阵。</returns>
	public static FPMatrix4x4 RotateX(FP radians, FPVector3 centerPoint)
	{
		FP fP = FPMath.Cos(radians);
		FP fP2 = FPMath.Sin(radians);
		FP m = centerPoint.y * (FP.One - fP) + centerPoint.z * fP2;
		FP m2 = centerPoint.z * (FP.One - fP) - centerPoint.y * fP2;
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = FP.One;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M14 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = fP;
		result.M23 = fP2;
		result.M24 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = -fP2;
		result.M33 = fP;
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = m;
		result.M43 = m2;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建围绕 Y 轴旋转的矩阵。
	/// </summary>
	/// <param name="radians">围绕 Y 轴旋转的弧度。</param>
	/// <returns>旋转矩阵。</returns>
	public static FPMatrix4x4 RotateY(FP radians)
	{
		FP fP = FPMath.Cos(radians);
		FP fP2 = FPMath.Sin(radians);
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = fP;
		result.M12 = FP.Zero;
		result.M13 = -fP2;
		result.M14 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = FP.One;
		result.M23 = FP.Zero;
		result.M24 = FP.Zero;
		result.M31 = fP2;
		result.M32 = FP.Zero;
		result.M33 = fP;
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建围绕 Y 轴旋转的矩阵，并指定中心点。
	/// </summary>
	/// <param name="radians">围绕 Y 轴旋转的弧度。</param>
	/// <param name="centerPoint">旋转的中心点。</param>
	/// <returns>旋转矩阵。</returns>
	public static FPMatrix4x4 RotateY(FP radians, FPVector3 centerPoint)
	{
		FP fP = FPMath.Cos(radians);
		FP fP2 = FPMath.Sin(radians);
		FP m = centerPoint.x * (FP.One - fP) - centerPoint.z * fP2;
		FP m2 = centerPoint.x * (FP.One - fP) + centerPoint.x * fP2;
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = fP;
		result.M12 = FP.Zero;
		result.M13 = -fP2;
		result.M14 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = FP.One;
		result.M23 = FP.Zero;
		result.M24 = FP.Zero;
		result.M31 = fP2;
		result.M32 = FP.Zero;
		result.M33 = fP;
		result.M34 = FP.Zero;
		result.M41 = m;
		result.M42 = FP.Zero;
		result.M43 = m2;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建围绕 Z 轴旋转的矩阵。
	/// </summary>
	/// <param name="radians">围绕 Z 轴旋转的弧度。</param>
	/// <returns>旋转矩阵。</returns>
	public static FPMatrix4x4 RotateZ(FP radians)
	{
		FP fP = FPMath.Cos(radians);
		FP fP2 = FPMath.Sin(radians);
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = fP;
		result.M12 = fP2;
		result.M13 = FP.Zero;
		result.M14 = FP.Zero;
		result.M21 = -fP2;
		result.M22 = fP;
		result.M23 = FP.Zero;
		result.M24 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = FP.One;
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建围绕 Z 轴旋转的矩阵，并指定中心点。
	/// </summary>
	/// <param name="radians">围绕 Z 轴旋转的弧度。</param>
	/// <param name="centerPoint">旋转的中心点。</param>
	/// <returns>旋转矩阵。</returns>
	public static FPMatrix4x4 RotateZ(FP radians, FPVector3 centerPoint)
	{
		FP fP = FPMath.Cos(radians);
		FP fP2 = FPMath.Sin(radians);
		_ = centerPoint.x * (1 - fP) + centerPoint.y * fP2;
		_ = centerPoint.y * (1 - fP) - centerPoint.x * fP2;
		FPMatrix4x4 result = default(FPMatrix4x4);
		result.M11 = fP;
		result.M12 = fP2;
		result.M13 = FP.Zero;
		result.M14 = FP.Zero;
		result.M21 = -fP2;
		result.M22 = fP;
		result.M23 = FP.Zero;
		result.M24 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = FP.One;
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建围绕给定轴旋转的矩阵。
	/// </summary>
	/// <param name="axis">旋转轴。</param>
	/// <param name="angle">旋转角度。</param>
	/// <param name="result">结果旋转矩阵。</param>
	public static void AxisAngle(ref FPVector3 axis, FP angle, out FPMatrix4x4 result)
	{
		FP x = axis.x;
		FP y = axis.y;
		FP z = axis.z;
		FP fP = FPMath.Sin(angle);
		FP fP2 = FPMath.Cos(angle);
		FP fP3 = x * x;
		FP fP4 = y * y;
		FP fP5 = z * z;
		FP fP6 = x * y;
		FP fP7 = x * z;
		FP fP8 = y * z;
		result.M11 = fP3 + fP2 * (FP.One - fP3);
		result.M12 = fP6 - fP2 * fP6 + fP * z;
		result.M13 = fP7 - fP2 * fP7 - fP * y;
		result.M14 = FP.Zero;
		result.M21 = fP6 - fP2 * fP6 - fP * z;
		result.M22 = fP4 + fP2 * (FP.One - fP4);
		result.M23 = fP8 - fP2 * fP8 + fP * x;
		result.M24 = FP.Zero;
		result.M31 = fP7 - fP2 * fP7 + fP * y;
		result.M32 = fP8 - fP2 * fP8 - fP * x;
		result.M33 = fP5 + fP2 * (FP.One - fP5);
		result.M34 = FP.Zero;
		result.M41 = FP.Zero;
		result.M42 = FP.Zero;
		result.M43 = FP.Zero;
		result.M44 = FP.One;
	}

	/// <summary>
	/// 创建围绕给定轴旋转的矩阵。
	/// </summary>
	/// <param name="axis">旋转轴。</param>
	/// <param name="angle">旋转角度。</param>
	/// <returns>结果旋转矩阵。</returns>
	public static FPMatrix4x4 AngleAxis(FP angle, FPVector3 axis)
	{
		AxisAngle(ref axis, angle, out var result);
		return result;
	}

	/// <summary>
	/// 返回矩阵的字符串表示形式。
	/// </summary>
	/// <returns>矩阵的字符串表示形式。</returns>
	public override string ToString()
	{
		return $"{M11.RawValue}|{M12.RawValue}|{M13.RawValue}|{M14.RawValue}|{M21.RawValue}|{M22.RawValue}|{M23.RawValue}|{M24.RawValue}|{M31.RawValue}|{M32.RawValue}|{M33.RawValue}|{M34.RawValue}|{M41.RawValue}|{M42.RawValue}|{M43.RawValue}|{M44.RawValue}";
	}

	/// <summary>
	/// 创建平移、旋转和缩放的组合矩阵。
	/// </summary>
	/// <param name="translation">平移向量。</param>
	/// <param name="rotation">旋转四元数。</param>
	/// <param name="scale">缩放向量。</param>
	/// <param name="matrix">组合后的矩阵。</param>
	public static void TRS(FPVector3 translation, FPQuaternion rotation, FPVector3 scale, out FPMatrix4x4 matrix)
	{
		matrix = Translate(translation) * Rotate(rotation) * Scale(scale);
	}

	/// <summary>
	/// 创建平移、旋转和缩放的组合矩阵。
	/// </summary>
	/// <param name="translation">平移向量。</param>
	/// <param name="rotation">旋转四元数。</param>
	/// <param name="scale">缩放向量。</param>
	/// <returns>组合后的矩阵。</returns>
	public static FPMatrix4x4 TRS(FPVector3 translation, FPQuaternion rotation, FPVector3 scale)
	{
		TRS(translation, rotation, scale, out var matrix);
		return matrix;
	}
}
