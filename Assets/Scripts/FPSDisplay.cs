using UnityEngine;
using System.Collections;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI textObject;
	[SerializeField]
	private float updateFrequency = 0.2f;

	float deltaTime = 0.0f;
	float timer = 0;

	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		timer += Time.deltaTime;
		if(timer > updateFrequency)
		{
			float msec = deltaTime * 1000.0f;
			float fps = 1.0f / deltaTime;
			string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
			textObject.text = text;

			timer = 0;
		}
	}
}