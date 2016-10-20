using UnityEngine;
using System.Collections;
using System;

public enum Button { A, B, X, Y };

public class InputController : MonoBehaviour
{
	public System.Action<float, float> AxisInput;
	public System.Action<Button> ButtonInput;

	private float hComponent;
	private float vComponent;

    void Awake()
    {
        GameManager.Instance.InputController = this;
    }

	void FixedUpdate()
	{
		GetAxisInput();
		GetButtonInput();
		GetMouseInput();
	}

	private void GetAxisInput()
	{
		hComponent = Input.GetAxis("Horizontal");
		vComponent = Input.GetAxis("Vertical");

		if (AxisInput != null)
		{
            AxisInput(hComponent, vComponent);
		}
	}

	private void GetButtonInput()
	{
		if (Input.GetButtonDown("Button_A"))
		{
			if (ButtonInput != null)
			{
                ButtonInput(Button.A);
			}
		}
	}

	private void GetMouseInput()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Debug.Log(Input.mousePosition);
		}
	}
}
