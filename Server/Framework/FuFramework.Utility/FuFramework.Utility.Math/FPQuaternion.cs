using System;

namespace FuFramework.Utility.Math;

/// <summary>
/// 表示一个四元数，用于表示三维空间中的方向和旋转。
/// </summary>
[Serializable]
public struct FPQuaternion
{
	/// <summary>四元数的 X 分量。</summary>
	public FP x;

	/// <summary>四元数的 Y 分量。</summary>
	public FP y;

	/// <summary>四元数的 Z 分量。</summary>
	public FP z;

	/// <summary>四元数的 W 分量。</summary>
	public FP w;

	/// <summary>表示单位四元数的静态只读字段。</summary>
	public static readonly FPQuaternion identity;

	/// <summary>
	/// 获取四元数对应的欧拉角。
	/// </summary>
	/// <returns>表示欧拉角的向量。</returns>
	public FPVector3 eulerAngles
	{
		get
		{
			FPVector3 fPVector = default(FPVector3);
			FP fP = y * y;
			FP fP2 = -2 * FP.One * (fP + z * z) + FP.One;
			FP fP3 = 2 * FP.One * (x * y - w * z);
			FP fP4 = -2 * FP.One * (x * z + w * y);
			FP fP5 = 2 * FP.One * (y * z - w * x);
			FP fP6 = -2 * FP.One * (x * x + fP) + FP.One;
			fP4 = ((fP4 > FP.One) ? FP.One : fP4);
			fP4 = ((fP4 < -FP.One) ? (-FP.One) : fP4);
			fPVector.x = FP.Atan2(fP5, fP6) * FP.Rad2Deg;
			fPVector.y = FP.Asin(fP4) * FP.Rad2Deg;
			fPVector.z = FP.Atan2(fP3, fP2) * FP.Rad2Deg;
			return fPVector * -1;
		}
	}

	/// <summary>
	/// 获取四元数对应的欧拉角（另一种实现）。
	/// </summary>
	/// <returns>表示欧拉角的向量。</returns>
	public FPVector3 eulerAnglesNew
	{
		get
		{
			FP fP = 2 * (x * z + w * y);
			FP fP2 = w * w - x * x - y * y + z * z;
			FP value = -2 * (y * z - w * x);
			FP fP3 = 2 * (x * y + w * z);
			return new FPVector3(z: FP.Atan2(fP3, w * w - x * x + y * y - z * z), x: FP.Atan2(fP, fP2), y: FP.Asin(value));
		}
	}

	/// <summary>静态构造函数，初始化单位四元数。</summary>
	static FPQuaternion()
	{
		identity = new FPQuaternion(0, 0, 0, 1);
	}

	/// <summary>
	/// 初始化一个新的四元数实例。
	/// </summary>
	/// <param name="x">四元数的 X 分量。</param>
	/// <param name="y">四元数的 Y 分量。</param>
	/// <param name="z">四元数的 Z 分量。</param>
	/// <param name="w">四元数的 W 分量。</param>
	public FPQuaternion(FP x, FP y, FP z, FP w)
	{
		this.x = x;
		this.y = y;
		this.z = z;
		this.w = w;
	}

	/// <summary>
	/// 设置四元数的各个分量。
	/// </summary>
	/// <param name="new_x">新的 X 分量。</param>
	/// <param name="new_y">新的 Y 分量。</param>
	/// <param name="new_z">新的 Z 分量。</param>
	/// <param name="new_w">新的 W 分量。</param>
	public void Set(FP new_x, FP new_y, FP new_z, FP new_w)
	{
		x = new_x;
		y = new_y;
		z = new_z;
		w = new_w;
	}

	/// <summary>
	/// 根据从一个方向到另一个方向的旋转设置四元数。
	/// </summary>
	/// <param name="fromDirection">起始方向。</param>
	/// <param name="toDirection">目标方向。</param>
	public void SetFromToRotation(FPVector3 fromDirection, FPVector3 toDirection)
	{
		FPQuaternion fPQuaternion = FromToRotation(fromDirection, toDirection);
		Set(fPQuaternion.x, fPQuaternion.y, fPQuaternion.z, fPQuaternion.w);
	}

	/// <summary>
	/// 计算两个四元数之间的夹角。
	/// </summary>
	/// <param name="a">第一个四元数。</param>
	/// <param name="b">第二个四元数。</param>
	/// <returns>两个四元数之间的夹角（以度为单位）。</returns>
	public static FP Angle(FPQuaternion a, FPQuaternion b)
	{
		FPQuaternion fPQuaternion = Inverse(a);
		FP fP = FP.Acos((b * fPQuaternion).w) * 2 * FP.Rad2Deg;
		if (fP > 180)
		{
			fP = 360 - fP;
		}
		return fP;
	}

	/// <summary>
	/// 计算两个四元数的和。
	/// </summary>
	/// <param name="quaternion1">第一个四元数。</param>
	/// <param name="quaternion2">第二个四元数。</param>
	/// <returns>两个四元数的和。</returns>
	public static FPQuaternion Add(FPQuaternion quaternion1, FPQuaternion quaternion2)
	{
		Add(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	/// <summary>
	/// 根据指定的前方向量创建一个四元数，使用默认的向上向量。
	/// </summary>
	/// <param name="forward">前方向量。</param>
	/// <returns>表示旋转的四元数。</returns>
	public static FPQuaternion LookRotation(FPVector3 forward)
	{
		return CreateFromMatrix(FPMatrix.LookAt(forward, FPVector3.up));
	}

	/// <summary>
	/// 根据指定的前方向量和向上向量创建一个四元数。
	/// </summary>
	/// <param name="forward">前方向量。</param>
	/// <param name="upwards">向上向量。</param>
	/// <returns>表示旋转的四元数。</returns>
	public static FPQuaternion LookRotation(FPVector3 forward, FPVector3 upwards)
	{
		return CreateFromMatrix(FPMatrix.LookAt(forward, upwards));
	}

	/// <summary>
	/// 在两个四元数之间进行球面线性插值。
	/// </summary>
	/// <param name="from">起始四元数。</param>
	/// <param name="to">目标四元数。</param>
	/// <param name="t">插值参数，范围在 0 到 1 之间。</param>
	/// <returns>插值结果四元数。</returns>
	public static FPQuaternion Slerp(FPQuaternion from, FPQuaternion to, FP t)
	{
		t = FPMath.Clamp(t, 0, 1);
		FP fP = Dot(from, to);
		if (fP < FP.Zero)
		{
			to = Multiply(to, -1);
			fP = -fP;
		}
		FP fP2 = FP.Acos(fP);
		return Multiply(Multiply(from, FP.Sin((1 - t) * fP2)) + Multiply(to, FP.Sin(t * fP2)), 1 / FP.Sin(fP2));
	}

	/// <summary>
	/// 将一个四元数朝向另一个四元数旋转，但不超过指定的最大角度。
	/// </summary>
	/// <param name="from">起始四元数。</param>
	/// <param name="to">目标四元数。</param>
	/// <param name="maxDegreesDelta">最大旋转角度（以度为单位）。</param>
	/// <returns>旋转后的四元数。</returns>
	public static FPQuaternion RotateTowards(FPQuaternion from, FPQuaternion to, FP maxDegreesDelta)
	{
		FP fP = Dot(from, to);
		if (fP < FP.Zero)
		{
			to = Multiply(to, -1);
			fP = -fP;
		}
		FP fP2 = FP.Acos(fP);
		FP fP3 = fP2 * 2;
		maxDegreesDelta *= FP.Deg2Rad;
		if (maxDegreesDelta >= fP3)
		{
			return to;
		}
		maxDegreesDelta /= fP3;
		return Multiply(Multiply(from, FP.Sin((1 - maxDegreesDelta) * fP2)) + Multiply(to, FP.Sin(maxDegreesDelta * fP2)), 1 / FP.Sin(fP2));
	}

	/// <summary>
	/// 根据欧拉角创建一个四元数。
	/// </summary>
	/// <param name="x">绕 X 轴的旋转角度（以度为单位）。</param>
	/// <param name="y">绕 Y 轴的旋转角度（以度为单位）。</param>
	/// <param name="z">绕 Z 轴的旋转角度（以度为单位）。</param>
	/// <returns>表示旋转的四元数。</returns>
	public static FPQuaternion Euler(FP x, FP y, FP z)
	{
		x *= FP.Deg2Rad;
		y *= FP.Deg2Rad;
		z *= FP.Deg2Rad;
		CreateFromYawPitchRoll(y, x, z, out var result);
		return result;
	}

	/// <summary>
	/// 根据欧拉角向量创建一个四元数。
	/// </summary>
	/// <param name="eulerAngles">欧拉角向量。</param>
	/// <returns>表示旋转的四元数。</returns>
	public static FPQuaternion Euler(FPVector3 eulerAngles)
	{
		return Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
	}

	/// <summary>
	/// 根据指定的角度和轴创建一个四元数。
	/// </summary>
	/// <param name="angle">旋转角度（以度为单位）。</param>
	/// <param name="axis">旋转轴。</param>
	/// <returns>表示旋转的四元数。</returns>
	public static FPQuaternion AngleAxis(FP angle, FPVector3 axis)
	{
		axis *= FP.Deg2Rad;
		axis.Normalize();
		FP fP = angle * FP.Deg2Rad * FP.Half;
		FP fP2 = FP.Sin(fP);
		FPQuaternion result = default(FPQuaternion);
		result.x = axis.x * fP2;
		result.y = axis.y * fP2;
		result.z = axis.z * fP2;
		result.w = FP.Cos(fP);
		return result;
	}

	/// <summary>
	/// 根据指定的偏航角、俯仰角和翻滚角创建一个四元数。
	/// </summary>
	/// <param name="yaw">偏航角（绕 Y 轴的旋转角度）。</param>
	/// <param name="pitch">俯仰角（绕 X 轴的旋转角度）。</param>
	/// <param name="roll">翻滚角（绕 Z 轴的旋转角度）。</param>
	/// <param name="result">表示旋转的四元数。</param>
	public static void CreateFromYawPitchRoll(FP yaw, FP pitch, FP roll, out FPQuaternion result)
	{
		FP fP = roll * FP.Half;
		FP fP2 = FP.Sin(fP);
		FP fP3 = FP.Cos(fP);
		FP fP4 = pitch * FP.Half;
		FP fP5 = FP.Sin(fP4);
		FP fP6 = FP.Cos(fP4);
		FP fP7 = yaw * FP.Half;
		FP fP8 = FP.Sin(fP7);
		FP fP9 = FP.Cos(fP7);
		result.x = fP9 * fP5 * fP3 + fP8 * fP6 * fP2;
		result.y = fP8 * fP6 * fP3 - fP9 * fP5 * fP2;
		result.z = fP9 * fP6 * fP2 - fP8 * fP5 * fP3;
		result.w = fP9 * fP6 * fP3 + fP8 * fP5 * fP2;
	}

	/// <summary>
	/// 计算两个四元数的和。
	/// </summary>
	/// <param name="quaternion1">第一个四元数。</param>
	/// <param name="quaternion2">第二个四元数。</param>
	/// <param name="result">两个四元数的和。</param>
	public static void Add(ref FPQuaternion quaternion1, ref FPQuaternion quaternion2, out FPQuaternion result)
	{
		result.x = quaternion1.x + quaternion2.x;
		result.y = quaternion1.y + quaternion2.y;
		result.z = quaternion1.z + quaternion2.z;
		result.w = quaternion1.w + quaternion2.w;
	}

	/// <summary>
	/// 计算四元数的共轭。
	/// </summary>
	/// <param name="value">要计算共轭的四元数。</param>
	/// <returns>四元数的共轭。</returns>
	public static FPQuaternion Conjugate(FPQuaternion value)
	{
		FPQuaternion result = default(FPQuaternion);
		result.x = -value.x;
		result.y = -value.y;
		result.z = -value.z;
		result.w = value.w;
		return result;
	}

	/// <summary>
	/// 计算两个四元数的点积。
	/// </summary>
	/// <param name="a">第一个四元数。</param>
	/// <param name="b">第二个四元数。</param>
	/// <returns>两个四元数的点积。</returns>
	public static FP Dot(FPQuaternion a, FPQuaternion b)
	{
		return a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z;
	}

	/// <summary>
	/// 计算四元数的逆。
	/// </summary>
	/// <param name="rotation">要计算逆的四元数。</param>
	/// <returns>四元数的逆。</returns>
	public static FPQuaternion Inverse(FPQuaternion rotation)
	{
		FP scaleFactor = FP.One / (rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
		return Multiply(Conjugate(rotation), scaleFactor);
	}

	/// <summary>
	/// 计算从一个向量到另一个向量的旋转四元数。
	/// </summary>
	/// <param name="fromVector3">起始向量。</param>
	/// <param name="toVector3">目标向量。</param>
	/// <returns>表示从一个向量到另一个向量的旋转的四元数。</returns>
	public static FPQuaternion FromToRotation(FPVector3 fromVector3, FPVector3 toVector3)
	{
		FPVector3 fPVector = FPVector3.Cross(fromVector3, toVector3);
		FPQuaternion result = new FPQuaternion(fPVector.x, fPVector.y, fPVector.z, FPVector3.Dot(fromVector3, toVector3));
		result.w += FP.Sqrt(fromVector3.sqrMagnitude * toVector3.sqrMagnitude);
		result.Normalize();
		return result;
	}

	/// <summary>
	/// 在两个四元数之间进行线性插值。
	/// </summary>
	/// <param name="a">起始四元数。</param>
	/// <param name="b">目标四元数。</param>
	/// <param name="t">插值参数，范围在 0 到 1 之间。</param>
	/// <returns>插值结果四元数。</returns>
	public static FPQuaternion Lerp(FPQuaternion a, FPQuaternion b, FP t)
	{
		t = FPMath.Clamp(t, FP.Zero, FP.One);
		return LerpUnclamped(a, b, t);
	}

	/// <summary>
	/// 在两个四元数之间进行线性插值，不进行参数限制。
	/// </summary>
	/// <param name="a">起始四元数。</param>
	/// <param name="b">目标四元数。</param>
	/// <param name="t">插值参数。</param>
	/// <returns>插值结果四元数。</returns>
	public static FPQuaternion LerpUnclamped(FPQuaternion a, FPQuaternion b, FP t)
	{
		FPQuaternion result = Multiply(a, 1 - t) + Multiply(b, t);
		result.Normalize();
		return result;
	}

	/// <summary>
	/// 计算两个四元数的差。
	/// </summary>
	/// <param name="quaternion1">第一个四元数。</param>
	/// <param name="quaternion2">第二个四元数。</param>
	/// <returns>两个四元数的差。</returns>
	public static FPQuaternion Subtract(FPQuaternion quaternion1, FPQuaternion quaternion2)
	{
		Subtract(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	/// <summary>
	/// 计算两个四元数的差。
	/// </summary>
	/// <param name="quaternion1">第一个四元数。</param>
	/// <param name="quaternion2">第二个四元数。</param>
	/// <param name="result">两个四元数的差。</param>
	public static void Subtract(ref FPQuaternion quaternion1, ref FPQuaternion quaternion2, out FPQuaternion result)
	{
		result.x = quaternion1.x - quaternion2.x;
		result.y = quaternion1.y - quaternion2.y;
		result.z = quaternion1.z - quaternion2.z;
		result.w = quaternion1.w - quaternion2.w;
	}

	/// <summary>
	/// 计算两个四元数的乘积。
	/// </summary>
	/// <param name="quaternion1">第一个四元数。</param>
	/// <param name="quaternion2">第二个四元数。</param>
	/// <returns>两个四元数的乘积。</returns>
	public static FPQuaternion Multiply(FPQuaternion quaternion1, FPQuaternion quaternion2)
	{
		Multiply(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	/// <summary>
	/// 计算两个四元数的乘积。
	/// </summary>
	/// <param name="quaternion1">第一个四元数。</param>
	/// <param name="quaternion2">第二个四元数。</param>
	/// <param name="result">两个四元数的乘积。</param>
	public static void Multiply(ref FPQuaternion quaternion1, ref FPQuaternion quaternion2, out FPQuaternion result)
	{
		FP fP = quaternion1.x;
		FP fP2 = quaternion1.y;
		FP fP3 = quaternion1.z;
		FP fP4 = quaternion1.w;
		FP fP5 = quaternion2.x;
		FP fP6 = quaternion2.y;
		FP fP7 = quaternion2.z;
		FP fP8 = quaternion2.w;
		FP fP9 = fP2 * fP7 - fP3 * fP6;
		FP fP10 = fP3 * fP5 - fP * fP7;
		FP fP11 = fP * fP6 - fP2 * fP5;
		FP fP12 = fP * fP5 + fP2 * fP6 + fP3 * fP7;
		result.x = fP * fP8 + fP5 * fP4 + fP9;
		result.y = fP2 * fP8 + fP6 * fP4 + fP10;
		result.z = fP3 * fP8 + fP7 * fP4 + fP11;
		result.w = fP4 * fP8 - fP12;
	}

	/// <summary>
	/// 计算四元数与缩放因子的乘积。
	/// </summary>
	/// <param name="quaternion1">要缩放的四元数。</param>
	/// <param name="scaleFactor">缩放因子。</param>
	/// <returns>缩放后的四元数。</returns>
	public static FPQuaternion Multiply(FPQuaternion quaternion1, FP scaleFactor)
	{
		Multiply(ref quaternion1, scaleFactor, out var result);
		return result;
	}

	/// <summary>
	/// 缩放一个四元数。
	/// </summary>
	/// <param name="quaternion1">要缩放的四元数。</param>
	/// <param name="scaleFactor">缩放因子。</param>
	/// <param name="result">缩放后的四元数。</param>
	public static void Multiply(ref FPQuaternion quaternion1, FP scaleFactor, out FPQuaternion result)
	{
		result.x = quaternion1.x * scaleFactor;
		result.y = quaternion1.y * scaleFactor;
		result.z = quaternion1.z * scaleFactor;
		result.w = quaternion1.w * scaleFactor;
	}

	/// <summary>
	/// 对当前四元数进行归一化。
	/// </summary>
	/// <remarks>
	/// 归一化会将四元数的模长变为1，确保其表示一个有效的旋转。
	/// </remarks>
	public void Normalize()
	{
		FP fP = x * x + y * y + z * z + w * w;
		FP fP2 = 1 / FP.Sqrt(fP);
		x *= fP2;
		y *= fP2;
		z *= fP2;
		w *= fP2;
	}

	/// <summary>
	/// 从轴和角度创建一个四元数。
	/// </summary>
	/// <param name="axis">旋转轴，必须是单位向量。</param>
	/// <param name="angle">旋转角度，以弧度为单位。</param>
	/// <returns>表示旋转的 FPQuaternion。</returns>
	public static FPQuaternion CreateFromAxisAngle(FPVector3 axis, FP angle)
	{
		axis = axis.normalized;
		FP value = angle * FP.Half;
		FP fP = FPMath.Sin(value);
		FP fP2 = FPMath.Cos(value);
		return new FPQuaternion(axis.x * fP, axis.y * fP, axis.z * fP, fP2);
	}

	/// <summary>
	/// 从矩阵创建一个四元数。
	/// </summary>
	/// <param name="matrix">表示方向的矩阵。</param>
	/// <returns>表示方向的 FPQuaternion。</returns>
	public static FPQuaternion CreateFromMatrix(FPMatrix matrix)
	{
		CreateFromMatrix(ref matrix, out var result);
		return result;
	}

	/// <summary>
	/// 从矩阵创建一个四元数。
	/// </summary>
	/// <param name="matrix">表示方向的矩阵。</param>
	/// <param name="result">表示方向的 FPQuaternion。</param>
	public static void CreateFromMatrix(ref FPMatrix matrix, out FPQuaternion result)
	{
		FP fP = matrix.M11 + matrix.M22 + matrix.M33;
		if (fP > FP.Zero)
		{
			FP fP2 = FP.Sqrt(fP + FP.One);
			result.w = fP2 * FP.Half;
			fP2 = FP.Half / fP2;
			result.x = (matrix.M23 - matrix.M32) * fP2;
			result.y = (matrix.M31 - matrix.M13) * fP2;
			result.z = (matrix.M12 - matrix.M21) * fP2;
		}
		else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
		{
			FP fP3 = FP.Sqrt(FP.One + matrix.M11 - matrix.M22 - matrix.M33);
			FP fP4 = FP.Half / fP3;
			result.x = FP.Half * fP3;
			result.y = (matrix.M12 + matrix.M21) * fP4;
			result.z = (matrix.M13 + matrix.M31) * fP4;
			result.w = (matrix.M23 - matrix.M32) * fP4;
		}
		else if (matrix.M22 > matrix.M33)
		{
			FP fP5 = FP.Sqrt(FP.One + matrix.M22 - matrix.M11 - matrix.M33);
			FP fP6 = FP.Half / fP5;
			result.x = (matrix.M21 + matrix.M12) * fP6;
			result.y = FP.Half * fP5;
			result.z = (matrix.M32 + matrix.M23) * fP6;
			result.w = (matrix.M31 - matrix.M13) * fP6;
		}
		else
		{
			FP fP7 = FP.Sqrt(FP.One + matrix.M33 - matrix.M11 - matrix.M22);
			FP fP8 = FP.Half / fP7;
			result.x = (matrix.M31 + matrix.M13) * fP8;
			result.y = (matrix.M32 + matrix.M23) * fP8;
			result.z = FP.Half * fP7;
			result.w = (matrix.M12 - matrix.M21) * fP8;
		}
	}

	/// <summary>
	/// 乘以两个四元数。
	/// </summary>
	/// <param name="value1">第一个四元数。</param>
	/// <param name="value2">第二个四元数。</param>
	/// <returns>两个四元数的乘积。</returns>
	public static FPQuaternion operator *(FPQuaternion value1, FPQuaternion value2)
	{
		Multiply(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 加上两个四元数。
	/// </summary>
	/// <param name="value1">第一个四元数。</param>
	/// <param name="value2">第二个四元数。</param>
	/// <returns>两个四元数的和。</returns>
	public static FPQuaternion operator +(FPQuaternion value1, FPQuaternion value2)
	{
		Add(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 减去两个四元数。
	/// </summary>
	/// <param name="value1">第一个四元数。</param>
	/// <param name="value2">第二个四元数。</param>
	/// <returns>两个四元数的差。</returns>
	public static FPQuaternion operator -(FPQuaternion value1, FPQuaternion value2)
	{
		Subtract(ref value1, ref value2, out var result);
		return result;
	}

	/// <summary>
	/// 使用四元数旋转一个三维向量。
	/// </summary>
	/// <param name="quat">要应用的四元数。</param>
	/// <param name="vec">要旋转的三维向量。</param>
	/// <returns>旋转后的三维向量。</returns>
	public static FPVector3 operator *(FPQuaternion quat, FPVector3 vec)
	{
		FP fP = quat.x * 2 * FP.One;
		FP fP2 = quat.y * 2 * FP.One;
		FP fP3 = quat.z * 2 * FP.One;
		FP fP4 = quat.x * fP;
		FP fP5 = quat.y * fP2;
		FP fP6 = quat.z * fP3;
		FP fP7 = quat.x * fP2;
		FP fP8 = quat.x * fP3;
		FP fP9 = quat.y * fP3;
		FP fP10 = quat.w * fP;
		FP fP11 = quat.w * fP2;
		FP fP12 = quat.w * fP3;
		FPVector3 result = default(FPVector3);
		result.x = (FP.One - (fP5 + fP6)) * vec.x + (fP7 - fP12) * vec.y + (fP8 + fP11) * vec.z;
		result.y = (fP7 + fP12) * vec.x + (FP.One - (fP4 + fP6)) * vec.y + (fP9 - fP10) * vec.z;
		result.z = (fP8 - fP11) * vec.x + (fP9 + fP10) * vec.y + (FP.One - (fP4 + fP5)) * vec.z;
		return result;
	}

	/// <summary>
	/// 返回四元数的字符串表示形式。
	/// </summary>
	/// <returns>四元数的字符串表示形式。</returns>
	public override string ToString()
	{
		return $"({x.AsFloat():f5}, {y.AsFloat():f5}, {z.AsFloat():f5}, {w.AsFloat():f5})";
	}

	/// <summary>
	/// 判断两个四元数是否相等。
	/// </summary>
	/// <param name="value1">第一个四元数。</param>
	/// <param name="value2">第二个四元数。</param>
	/// <returns>如果两个四元数相等，则返回 true；否则返回 false。</returns>
	public static bool operator ==(FPQuaternion value1, FPQuaternion value2)
	{
		if (value1.x == value2.x && value1.y == value2.y && value1.z == value2.z)
		{
			return value1.w == value2.w;
		}
		return false;
	}

	/// <summary>
	/// 判断两个四元数是否不相等。
	/// </summary>
	/// <param name="value1">第一个四元数。</param>
	/// <param name="value2">第二个四元数。</param>
	/// <returns>如果两个四元数不相等，则返回 true；否则返回 false。</returns>
	public static bool operator !=(FPQuaternion value1, FPQuaternion value2)
	{
		if (value1.x == value2.x && value1.y == value2.y)
		{
			if (value1.z != value2.z)
			{
				return value1.w != value2.w;
			}
			return false;
		}
		return true;
	}

	/// <summary>
	/// 判断当前四元数是否与指定对象相等。
	/// </summary>
	/// <param name="obj">要比较的对象。</param>
	/// <returns>如果当前四元数与指定对象相等，则返回 true；否则返回 false。</returns>
	public override bool Equals(object obj)
	{
		if (!(obj is FPQuaternion fPQuaternion))
		{
			return false;
		}
		if (x == fPQuaternion.x && y == fPQuaternion.y && z == fPQuaternion.z)
		{
			return w == fPQuaternion.w;
		}
		return false;
	}

	/// <summary>
	/// 返回当前四元数的哈希代码。
	/// </summary>
	/// <returns>当前四元数的哈希代码。</returns>
	public override int GetHashCode()
	{
		return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
	}
}
