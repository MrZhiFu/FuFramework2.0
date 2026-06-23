namespace FuFramework.Utility.Math;

/// <summary>
/// 表示一个3x3矩阵。
/// </summary>
public struct FPMatrix
{
	/// <summary>
	/// 矩阵的第1行第1列元素。
	/// </summary>
	public FP M11;

	/// <summary>
	/// 矩阵的第1行第2列元素。
	/// </summary>
	public FP M12;

	/// <summary>
	/// 矩阵的第1行第3列元素。
	/// </summary>
	public FP M13;

	/// <summary>
	/// 矩阵的第2行第1列元素。
	/// </summary>
	public FP M21;

	/// <summary>
	/// 矩阵的第2行第2列元素。
	/// </summary>
	public FP M22;

	/// <summary>
	/// 矩阵的第2行第3列元素。
	/// </summary>
	public FP M23;

	/// <summary>
	/// 矩阵的第3行第1列元素。
	/// </summary>
	public FP M31;

	/// <summary>
	/// 矩阵的第3行第2列元素。
	/// </summary>
	public FP M32;

	/// <summary>
	/// 矩阵的第3行第3列元素。
	/// </summary>
	public FP M33;

	internal static FPMatrix InternalIdentity;

	/// <summary>
	/// 单位矩阵。
	/// </summary>
	public static readonly FPMatrix Identity;

	/// <summary>
	/// 零矩阵。
	/// </summary>
	public static readonly FPMatrix Zero;

	/// <summary>
	/// 获取矩阵的欧拉角。
	/// </summary>
	public FPVector3 eulerAngles
	{
		get
		{
			FPVector3 fPVector = default(FPVector3);
			fPVector.x = FPMath.Atan2(M32, M33) * FP.Rad2Deg;
			fPVector.y = FPMath.Atan2(-M31, FPMath.Sqrt(M32 * M32 + M33 * M33)) * FP.Rad2Deg;
			fPVector.z = FPMath.Atan2(M21, M11) * FP.Rad2Deg;
			return fPVector * -1;
		}
	}

	static FPMatrix()
	{
		Zero = default(FPMatrix);
		Identity = default(FPMatrix);
		Identity.M11 = FP.One;
		Identity.M22 = FP.One;
		Identity.M33 = FP.One;
		InternalIdentity = Identity;
	}

	/// <summary>
	/// 根据偏航、俯仰和滚转角度创建旋转矩阵。
	/// </summary>
	/// <param name="yaw">偏航角度。</param>
	/// <param name="pitch">俯仰角度。</param>
	/// <param name="roll">滚转角度。</param>
	/// <returns>生成的旋转矩阵。</returns>
	public static FPMatrix CreateFromYawPitchRoll(FP yaw, FP pitch, FP roll)
	{
		FPQuaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out var result);
		CreateFromQuaternion(ref result, out var result2);
		return result2;
	}

	/// <summary>
	/// 创建绕X轴旋转的矩阵。
	/// </summary>
	/// <param name="radians">旋转角度（弧度）。</param>
	/// <returns>生成的旋转矩阵。</returns>
	public static FPMatrix CreateRotationX(FP radians)
	{
		FP fP = FP.Cos(radians);
		FP fP2 = FP.Sin(radians);
		FPMatrix result = default(FPMatrix);
		result.M11 = FP.One;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = fP;
		result.M23 = fP2;
		result.M31 = FP.Zero;
		result.M32 = -fP2;
		result.M33 = fP;
		return result;
	}

	/// <summary>
	/// 创建绕X轴旋转的矩阵，并将结果输出到指定的矩阵。
	/// </summary>
	/// <param name="radians">旋转角度（弧度）。</param>
	/// <param name="result">输出的旋转矩阵。</param>
	public static void CreateRotationX(FP radians, out FPMatrix result)
	{
		FP fP = FP.Cos(radians);
		FP fP2 = FP.Sin(radians);
		result.M11 = FP.One;
		result.M12 = FP.Zero;
		result.M13 = FP.Zero;
		result.M21 = FP.Zero;
		result.M22 = fP;
		result.M23 = fP2;
		result.M31 = FP.Zero;
		result.M32 = -fP2;
		result.M33 = fP;
	}

	/// <summary>
	/// 创建绕Y轴旋转的矩阵。
	/// </summary>
	/// <param name="radians">旋转角度（弧度）。</param>
	/// <returns>生成的旋转矩阵。</returns>
	public static FPMatrix CreateRotationY(FP radians)
	{
		FP fP = FP.Cos(radians);
		FP fP2 = FP.Sin(radians);
		FPMatrix result = default(FPMatrix);
		result.M11 = fP;
		result.M12 = FP.Zero;
		result.M13 = -fP2;
		result.M21 = FP.Zero;
		result.M22 = FP.One;
		result.M23 = FP.Zero;
		result.M31 = fP2;
		result.M32 = FP.Zero;
		result.M33 = fP;
		return result;
	}

	/// <summary>
	/// 创建绕Y轴旋转的矩阵，并将结果输出到指定的矩阵。
	/// </summary>
	/// <param name="radians">旋转角度（弧度）。</param>
	/// <param name="result">输出的旋转矩阵。</param>
	public static void CreateRotationY(FP radians, out FPMatrix result)
	{
		FP fP = FP.Cos(radians);
		FP fP2 = FP.Sin(radians);
		result.M11 = fP;
		result.M12 = FP.Zero;
		result.M13 = -fP2;
		result.M21 = FP.Zero;
		result.M22 = FP.One;
		result.M23 = FP.Zero;
		result.M31 = fP2;
		result.M32 = FP.Zero;
		result.M33 = fP;
	}

	/// <summary>
	/// 创建绕Z轴旋转的矩阵。
	/// </summary>
	/// <param name="radians">旋转角度（弧度）。</param>
	/// <returns>生成的旋转矩阵。</returns>
	public static FPMatrix CreateRotationZ(FP radians)
	{
		FP fP = FP.Cos(radians);
		FP fP2 = FP.Sin(radians);
		FPMatrix result = default(FPMatrix);
		result.M11 = fP;
		result.M12 = fP2;
		result.M13 = FP.Zero;
		result.M21 = -fP2;
		result.M22 = fP;
		result.M23 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = FP.One;
		return result;
	}

	/// <summary>
	/// 创建绕Z轴旋转的矩阵，并将结果输出到指定的矩阵。
	/// </summary>
	/// <param name="radians">旋转角度（弧度）。</param>
	/// <param name="result">输出的旋转矩阵。</param>
	public static void CreateRotationZ(FP radians, out FPMatrix result)
	{
		FP fP = FP.Cos(radians);
		FP fP2 = FP.Sin(radians);
		result.M11 = fP;
		result.M12 = fP2;
		result.M13 = FP.Zero;
		result.M21 = -fP2;
		result.M22 = fP;
		result.M23 = FP.Zero;
		result.M31 = FP.Zero;
		result.M32 = FP.Zero;
		result.M33 = FP.One;
	}

	/// <summary>
	/// 初始化矩阵结构的新实例。
	/// </summary>
	/// <param name="m11">矩阵的第1行第1列元素。</param>
	/// <param name="m12">矩阵的第1行第2列元素。</param>
	/// <param name="m13">矩阵的第1行第3列元素。</param>
	/// <param name="m21">矩阵的第2行第1列元素。</param>
	/// <param name="m22">矩阵的第2行第2列元素。</param>
	/// <param name="m23">矩阵的第2行第3列元素。</param>
	/// <param name="m31">矩阵的第3行第1列元素。</param>
	/// <param name="m32">矩阵的第3行第2列元素。</param>
	/// <param name="m33">矩阵的第3行第3列元素。</param>
	public FPMatrix(FP m11, FP m12, FP m13, FP m21, FP m22, FP m23, FP m31, FP m32, FP m33)
	{
		M11 = m11;
		M12 = m12;
		M13 = m13;
		M21 = m21;
		M22 = m22;
		M23 = m23;
		M31 = m31;
		M32 = m32;
		M33 = m33;
	}

	/// <summary>
	/// 乘以两个矩阵。注意：矩阵乘法不是可交换的。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <returns>两个矩阵的乘积。</returns>
	public static FPMatrix Multiply(FPMatrix matrix1, FPMatrix matrix2)
	{
		Multiply(ref matrix1, ref matrix2, out var result);
		return result;
	}

	/// <summary>
	/// 乘以两个矩阵。注意：矩阵乘法不是可交换的。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <param name="result">两个矩阵的乘积。</param>
	public static void Multiply(ref FPMatrix matrix1, ref FPMatrix matrix2, out FPMatrix result)
	{
		FP m = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31;
		FP m2 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32;
		FP m3 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33;
		FP m4 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31;
		FP m5 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32;
		FP m6 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33;
		FP m7 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31;
		FP m8 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32;
		FP m9 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33;
		result.M11 = m;
		result.M12 = m2;
		result.M13 = m3;
		result.M21 = m4;
		result.M22 = m5;
		result.M23 = m6;
		result.M31 = m7;
		result.M32 = m8;
		result.M33 = m9;
	}

	/// <summary>
	/// 矩阵相加。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <returns>两个矩阵的和。</returns>
	public static FPMatrix Add(FPMatrix matrix1, FPMatrix matrix2)
	{
		Add(ref matrix1, ref matrix2, out var result);
		return result;
	}

	/// <summary>
	/// 矩阵相加。
	/// </summary>
	/// <param name="matrix1">第一个矩阵。</param>
	/// <param name="matrix2">第二个矩阵。</param>
	/// <param name="result">两个矩阵的和。</param>
	public static void Add(ref FPMatrix matrix1, ref FPMatrix matrix2, out FPMatrix result)
	{
		result.M11 = matrix1.M11 + matrix2.M11;
		result.M12 = matrix1.M12 + matrix2.M12;
		result.M13 = matrix1.M13 + matrix2.M13;
		result.M21 = matrix1.M21 + matrix2.M21;
		result.M22 = matrix1.M22 + matrix2.M22;
		result.M23 = matrix1.M23 + matrix2.M23;
		result.M31 = matrix1.M31 + matrix2.M31;
		result.M32 = matrix1.M32 + matrix2.M32;
		result.M33 = matrix1.M33 + matrix2.M33;
	}

	/// <summary>
	/// 计算给定矩阵的逆矩阵。
	/// </summary>
	/// <param name="matrix">要计算逆的矩阵。</param>
	/// <returns>逆矩阵。</returns>
	public static FPMatrix Inverse(FPMatrix matrix)
	{
		Inverse(ref matrix, out var result);
		return result;
	}

	/// <summary>
	/// 计算矩阵的行列式。
	/// </summary>
	/// <returns>矩阵的行列式。</returns>
	public FP Determinant()
	{
		return M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 - M31 * M22 * M13 - M32 * M23 * M11 - M33 * M21 * M12;
	}

	/// <summary>
	/// 计算给定矩阵的逆矩阵。
	/// </summary>
	/// <param name="matrix">要计算逆的矩阵。</param>
	/// <param name="result">逆矩阵。</param>
	public static void Invert(ref FPMatrix matrix, out FPMatrix result)
	{
		FP fP = 1 / matrix.Determinant();
		FP m = (matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32) * fP;
		FP m2 = (matrix.M13 * matrix.M32 - matrix.M33 * matrix.M12) * fP;
		FP m3 = (matrix.M12 * matrix.M23 - matrix.M22 * matrix.M13) * fP;
		FP m4 = (matrix.M23 * matrix.M31 - matrix.M21 * matrix.M33) * fP;
		FP m5 = (matrix.M11 * matrix.M33 - matrix.M13 * matrix.M31) * fP;
		FP m6 = (matrix.M13 * matrix.M21 - matrix.M11 * matrix.M23) * fP;
		FP m7 = (matrix.M21 * matrix.M32 - matrix.M22 * matrix.M31) * fP;
		FP m8 = (matrix.M12 * matrix.M31 - matrix.M11 * matrix.M32) * fP;
		FP m9 = (matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21) * fP;
		result.M11 = m;
		result.M12 = m2;
		result.M13 = m3;
		result.M21 = m4;
		result.M22 = m5;
		result.M23 = m6;
		result.M31 = m7;
		result.M32 = m8;
		result.M33 = m9;
	}

	/// <summary>
	/// Calculates the inverse of a give matrix.
	/// </summary>
	/// <param name="matrix">The matrix to invert.</param>
	/// <param name="result">The inverted JMatrix.</param>
	public static void Inverse(ref FPMatrix matrix, out FPMatrix result)
	{
		FP fP = 1024 * matrix.M11 * matrix.M22 * matrix.M33 - 1024 * matrix.M11 * matrix.M23 * matrix.M32 - 1024 * matrix.M12 * matrix.M21 * matrix.M33 + 1024 * matrix.M12 * matrix.M23 * matrix.M31 + 1024 * matrix.M13 * matrix.M21 * matrix.M32 - 1024 * matrix.M13 * matrix.M22 * matrix.M31;
		FP fP2 = 1024 * matrix.M22 * matrix.M33 - 1024 * matrix.M23 * matrix.M32;
		FP fP3 = 1024 * matrix.M13 * matrix.M32 - 1024 * matrix.M12 * matrix.M33;
		FP fP4 = 1024 * matrix.M12 * matrix.M23 - 1024 * matrix.M22 * matrix.M13;
		FP fP5 = 1024 * matrix.M23 * matrix.M31 - 1024 * matrix.M33 * matrix.M21;
		FP fP6 = 1024 * matrix.M11 * matrix.M33 - 1024 * matrix.M31 * matrix.M13;
		FP fP7 = 1024 * matrix.M13 * matrix.M21 - 1024 * matrix.M23 * matrix.M11;
		FP fP8 = 1024 * matrix.M21 * matrix.M32 - 1024 * matrix.M31 * matrix.M22;
		FP fP9 = 1024 * matrix.M12 * matrix.M31 - 1024 * matrix.M32 * matrix.M11;
		FP fP10 = 1024 * matrix.M11 * matrix.M22 - 1024 * matrix.M21 * matrix.M12;
		if (fP == 0)
		{
			result.M11 = FP.PositiveInfinity;
			result.M12 = FP.PositiveInfinity;
			result.M13 = FP.PositiveInfinity;
			result.M21 = FP.PositiveInfinity;
			result.M22 = FP.PositiveInfinity;
			result.M23 = FP.PositiveInfinity;
			result.M31 = FP.PositiveInfinity;
			result.M32 = FP.PositiveInfinity;
			result.M33 = FP.PositiveInfinity;
		}
		else
		{
			result.M11 = fP2 / fP;
			result.M12 = fP3 / fP;
			result.M13 = fP4 / fP;
			result.M21 = fP5 / fP;
			result.M22 = fP6 / fP;
			result.M23 = fP7 / fP;
			result.M31 = fP8 / fP;
			result.M32 = fP9 / fP;
			result.M33 = fP10 / fP;
		}
	}

	/// <summary>
	/// 将矩阵乘以一个缩放因子。
	/// </summary>
	/// <param name="matrix1">要缩放的矩阵。</param>
	/// <param name="scaleFactor">缩放因子。</param>
	/// <returns>缩放后的矩阵。</returns>
	public static FPMatrix Multiply(FPMatrix matrix1, FP scaleFactor)
	{
		Multiply(ref matrix1, scaleFactor, out var result);
		return result;
	}

	/// <summary>
	/// 根据位置和目标创建视图矩阵
	/// </summary>
	/// <param name="position">观察者的位置</param>
	/// <param name="target">观察目标的位置</param>
	/// <returns>返回一个新的视图矩阵</returns>
	public static FPMatrix CreateFromLookAt(FPVector3 position, FPVector3 target)
	{
		LookAt(target - position, FPVector3.up, out var result);
		return result;
	}

	/// <summary>
	/// 将矩阵乘以一个缩放因子，并将结果输出到指定的矩阵。
	/// </summary>
	/// <param name="matrix1">要缩放的矩阵。</param>
	/// <param name="scaleFactor">缩放因子。</param>
	/// <param name="result">缩放后的矩阵。</param>
	public static void Multiply(ref FPMatrix matrix1, FP scaleFactor, out FPMatrix result)
	{
		result.M11 = matrix1.M11 * scaleFactor;
		result.M12 = matrix1.M12 * scaleFactor;
		result.M13 = matrix1.M13 * scaleFactor;
		result.M21 = matrix1.M21 * scaleFactor;
		result.M22 = matrix1.M22 * scaleFactor;
		result.M23 = matrix1.M23 * scaleFactor;
		result.M31 = matrix1.M31 * scaleFactor;
		result.M32 = matrix1.M32 * scaleFactor;
		result.M33 = matrix1.M33 * scaleFactor;
	}

	/// <summary>
	/// 创建一个观察矩阵。
	/// </summary>
	/// <param name="forward">前向向量。</param>
	/// <param name="upwards">向上向量。</param>
	/// <returns>观察矩阵。</returns>
	public static FPMatrix LookAt(FPVector3 forward, FPVector3 upwards)
	{
		LookAt(forward, upwards, out var result);
		return result;
	}

	/// <summary>
	/// 创建一个观察矩阵。
	/// </summary>
	/// <param name="forward">前向向量。</param>
	/// <param name="upwards">向上向量。</param>
	/// <param name="result">输出的观察矩阵。</param>
	public static void LookAt(FPVector3 forward, FPVector3 upwards, out FPMatrix result)
	{
		FPVector3 fPVector = forward;
		fPVector.Normalize();
		FPVector3 vector = FPVector3.Cross(upwards, fPVector);
		vector.Normalize();
		FPVector3 fPVector2 = FPVector3.Cross(fPVector, vector);
		result.M11 = vector.x;
		result.M21 = fPVector2.x;
		result.M31 = fPVector.x;
		result.M12 = vector.y;
		result.M22 = fPVector2.y;
		result.M32 = fPVector.y;
		result.M13 = vector.z;
		result.M23 = fPVector2.z;
		result.M33 = fPVector.z;
	}

	/// <summary>
	/// 根据四元数创建表示方向的矩阵。
	/// </summary>
	/// <param name="quaternion">用于创建矩阵的四元数。</param>
	/// <returns>表示方向的矩阵。</returns>
	public static FPMatrix CreateFromQuaternion(FPQuaternion quaternion)
	{
		CreateFromQuaternion(ref quaternion, out var result);
		return result;
	}

	/// <summary>
	/// 根据四元数创建表示方向的矩阵。
	/// </summary>
	/// <param name="quaternion">用于创建矩阵的四元数。</param>
	/// <param name="result">表示方向的矩阵。</param>
	public static void CreateFromQuaternion(ref FPQuaternion quaternion, out FPMatrix result)
	{
		FP fP = quaternion.x * quaternion.x;
		FP fP2 = quaternion.y * quaternion.y;
		FP fP3 = quaternion.z * quaternion.z;
		FP fP4 = quaternion.x * quaternion.y;
		FP fP5 = quaternion.z * quaternion.w;
		FP fP6 = quaternion.z * quaternion.x;
		FP fP7 = quaternion.y * quaternion.w;
		FP fP8 = quaternion.y * quaternion.z;
		FP fP9 = quaternion.x * quaternion.w;
		result.M11 = FP.One - 2 * (fP2 + fP3);
		result.M12 = 2 * (fP4 + fP5);
		result.M13 = 2 * (fP6 - fP7);
		result.M21 = 2 * (fP4 - fP5);
		result.M22 = FP.One - 2 * (fP3 + fP);
		result.M23 = 2 * (fP8 + fP9);
		result.M31 = 2 * (fP6 + fP7);
		result.M32 = 2 * (fP8 - fP9);
		result.M33 = FP.One - 2 * (fP2 + fP);
	}

	/// <summary>
	/// 创建转置矩阵。
	/// </summary>
	/// <param name="matrix">要转置的矩阵。</param>
	/// <returns>转置后的矩阵。</returns>
	public static FPMatrix Transpose(FPMatrix matrix)
	{
		Transpose(ref matrix, out var result);
		return result;
	}

	/// <summary>
	/// 创建转置矩阵。
	/// </summary>
	/// <param name="matrix">要转置的矩阵。</param>
	/// <param name="result">转置后的矩阵。</param>
	public static void Transpose(ref FPMatrix matrix, out FPMatrix result)
	{
		result.M11 = matrix.M11;
		result.M12 = matrix.M21;
		result.M13 = matrix.M31;
		result.M21 = matrix.M12;
		result.M22 = matrix.M22;
		result.M23 = matrix.M32;
		result.M31 = matrix.M13;
		result.M32 = matrix.M23;
		result.M33 = matrix.M33;
	}

	/// <summary>
	/// 乘以两个矩阵。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>两个矩阵的乘积。</returns>
	public static FPMatrix operator *(FPMatrix value1, FPMatrix value2)
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
		return M11 + M22 + M33;
	}

	/// <summary>
	/// 将两个矩阵相加。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>两个矩阵的和。</returns>
	public static FPMatrix operator +(FPMatrix value1, FPMatrix value2)
	{
		Add(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 将两个矩阵相减。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>两个矩阵的差。</returns>
	public static FPMatrix operator -(FPMatrix value1, FPMatrix value2)
	{
		Multiply(ref value2, -FP.One, out value2);
		Add(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 判断两个矩阵是否相等。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>如果相等则返回true，否则返回false。</returns>
	public static bool operator ==(FPMatrix value1, FPMatrix value2)
	{
		if (value1.M11 == value2.M11 && value1.M12 == value2.M12 && value1.M13 == value2.M13 && value1.M21 == value2.M21 && value1.M22 == value2.M22 && value1.M23 == value2.M23 && value1.M31 == value2.M31 && value1.M32 == value2.M32)
		{
			return value1.M33 == value2.M33;
		}
		return false;
	}

	/// <summary>
	/// 判断两个矩阵是否不相等。
	/// </summary>
	/// <param name="value1">第一个矩阵。</param>
	/// <param name="value2">第二个矩阵。</param>
	/// <returns>如果不相等则返回true，否则返回false。</returns>
	public static bool operator !=(FPMatrix value1, FPMatrix value2)
	{
		if (!(value1.M11 != value2.M11) && !(value1.M12 != value2.M12) && !(value1.M13 != value2.M13) && !(value1.M21 != value2.M21) && !(value1.M22 != value2.M22) && !(value1.M23 != value2.M23) && !(value1.M31 != value2.M31) && !(value1.M32 != value2.M32))
		{
			return value1.M33 != value2.M33;
		}
		return true;
	}

	/// <summary>
	/// 判断当前矩阵是否与指定对象相等。
	/// </summary>
	/// <param name="obj">要比较的对象。</param>
	/// <returns>如果相等则返回true，否则返回false。</returns>
	public override bool Equals(object obj)
	{
		if (!(obj is FPMatrix fPMatrix))
		{
			return false;
		}
		if (M11 == fPMatrix.M11 && M12 == fPMatrix.M12 && M13 == fPMatrix.M13 && M21 == fPMatrix.M21 && M22 == fPMatrix.M22 && M23 == fPMatrix.M23 && M31 == fPMatrix.M31 && M32 == fPMatrix.M32)
		{
			return M33 == fPMatrix.M33;
		}
		return false;
	}

	/// <summary>
	/// 获取当前矩阵的哈希代码。
	/// </summary>
	/// <returns>当前矩阵的哈希代码。</returns>
	public override int GetHashCode()
	{
		return M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^ M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^ M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode();
	}

	/// <summary>
	/// 根据给定轴和角度创建旋转矩阵。
	/// </summary>
	/// <param name="axis">旋转轴。</param>
	/// <param name="angle">旋转角度。</param>
	/// <param name="result">输出的旋转矩阵。</param>
	public static void CreateFromAxisAngle(ref FPVector3 axis, FP angle, out FPMatrix result)
	{
		FP x = axis.x;
		FP y = axis.y;
		FP z = axis.z;
		FP fP = FP.Sin(angle);
		FP fP2 = FP.Cos(angle);
		FP fP3 = x * x;
		FP fP4 = y * y;
		FP fP5 = z * z;
		FP fP6 = x * y;
		FP fP7 = x * z;
		FP fP8 = y * z;
		result.M11 = fP3 + fP2 * (FP.One - fP3);
		result.M12 = fP6 - fP2 * fP6 + fP * z;
		result.M13 = fP7 - fP2 * fP7 - fP * y;
		result.M21 = fP6 - fP2 * fP6 - fP * z;
		result.M22 = fP4 + fP2 * (FP.One - fP4);
		result.M23 = fP8 - fP2 * fP8 + fP * x;
		result.M31 = fP7 - fP2 * fP7 + fP * y;
		result.M32 = fP8 - fP2 * fP8 - fP * x;
		result.M33 = fP5 + fP2 * (FP.One - fP5);
	}

	/// <summary>
	/// 根据给定轴和角度创建旋转矩阵。
	/// </summary>
	/// <param name="axis">旋转轴。</param>
	/// <param name="angle">旋转角度。</param>
	/// <returns>生成的旋转矩阵。</returns>
	public static FPMatrix AngleAxis(FP angle, FPVector3 axis)
	{
		CreateFromAxisAngle(ref axis, angle, out var result);
		return result;
	}

	/// <summary>
	/// 将矩阵转换为字符串表示形式。
	/// </summary>
	/// <returns>矩阵的字符串表示形式。</returns>
	public override string ToString()
	{
		return $"{M11.RawValue}|{M12.RawValue}|{M13.RawValue}|{M21.RawValue}|{M22.RawValue}|{M23.RawValue}|{M31.RawValue}|{M32.RawValue}|{M33.RawValue}";
	}
}
