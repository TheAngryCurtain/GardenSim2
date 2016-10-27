using UnityEngine;
using System.Collections;

public interface IControllable
{
	void AcceptAxisInput(float h, float v);
	void AcceptMouseAction(MouseAction a, Vector3 pos);
	void AcceptScrollInput(float f);
}
