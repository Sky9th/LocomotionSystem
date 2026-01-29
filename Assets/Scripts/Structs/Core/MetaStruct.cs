using System;
using UnityEngine;

/// <summary>
/// Base metadata snapshot embedded in every struct that lives under
/// Assets/Scripts/Structs. Keeps shared bookkeeping (timestamp, frame index)
/// without requiring inheritance.
/// </summary>
[Serializable]
public struct MetaStruct
{
	public MetaStruct(float timestamp, uint frameIndex = 0)
	{
		Timestamp = timestamp;
		FrameIndex = frameIndex;
	}

	/// <summary>
	/// Timestamp (seconds) captured by the producing subsystem.
	/// </summary>
	public float Timestamp { get; set;}

	/// <summary>
	/// Frame index associated with the snapshot (optional but handy for diffing).
	/// </summary>
	public uint FrameIndex { get; set;}

	public bool IsValid => Timestamp >= 0f;
}
