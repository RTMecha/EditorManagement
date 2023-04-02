using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace EditorManagement.Functions
{
	public class AnimateInGUI : MonoBehaviour
	{
		//Work on making this work.
		
		private void Start()
		{
		}

		private void OnEnable()
		{
			OnEnableManual(force);
		}

		private void OnDisable()
        {
			OnDisableManual();
        }

		public void SetEasingIn(int _in)
		{
			if (DataManager.inst != null)
			{
				int easeIn = Mathf.Clamp(_in, 0, DataManager.inst.AnimationList.Count - 1);

				animateInCurve = DataManager.inst.AnimationList[easeIn].Animation;
				return;
			}
			Debug.LogError("DataManager Missing");
		}

		public void SetEasingOut(int _out)
		{
			if (DataManager.inst != null)
			{
				int easeOut = Mathf.Clamp(_out, 0, DataManager.inst.AnimationList.Count - 1);

				animateOutCurve = DataManager.inst.AnimationList[easeOut].Animation;
				return;
			}
			Debug.LogError("DataManager Missing");
		}

		public void SetEasing(int _in, int _out)
		{
			if (DataManager.inst != null)
			{
				int easeIn = Mathf.Clamp(_in, 0, DataManager.inst.AnimationList.Count - 1);
				int easeOut = Mathf.Clamp(_out, 0, DataManager.inst.AnimationList.Count - 1);

				animateInCurve = DataManager.inst.AnimationList[easeIn].Animation;
				animateOutCurve = DataManager.inst.AnimationList[easeOut].Animation;
				return;
			}
			Debug.LogError("DataManager Missing");
		}

		public void SetEasing(int _easing)
        {
			//InSine / OutSine
			if (_easing == 2 || _easing == 3)
            {
				animateInCurve = DataManager.inst.AnimationList[3].Animation;
				animateOutCurve = DataManager.inst.AnimationList[2].Animation;
				return;
            }

			//InElastic / OutElastic
			if (_easing == 5 || _easing == 6)
            {
				animateInCurve = DataManager.inst.AnimationList[6].Animation;
				animateOutCurve = DataManager.inst.AnimationList[5].Animation;
				return;
			}

			//InBack / OutBack
			if (_easing == 8 || _easing == 9)
            {
				animateInCurve = DataManager.inst.AnimationList[9].Animation;
				animateOutCurve = DataManager.inst.AnimationList[8].Animation;
				return;
			}

			//InBounce / OutBounce
			if (_easing == 11 || _easing == 12)
            {
				animateInCurve = DataManager.inst.AnimationList[12].Animation;
				animateOutCurve = DataManager.inst.AnimationList[11].Animation;
				return;
			}

			//InQuad / OutQuad
			if (_easing == 14 || _easing == 15)
            {
				animateInCurve = DataManager.inst.AnimationList[15].Animation;
				animateOutCurve = DataManager.inst.AnimationList[14].Animation;
				return;
			}

			//InCirc / OutCirc
			if (_easing == 17 || _easing == 18)
            {
				animateInCurve = DataManager.inst.AnimationList[18].Animation;
				animateOutCurve = DataManager.inst.AnimationList[17].Animation;
				return;
			}

			//InExpo / OutExpo
			if (_easing == 20 || _easing == 21)
            {
				animateInCurve = DataManager.inst.AnimationList[21].Animation;
				animateOutCurve = DataManager.inst.AnimationList[20].Animation;
				return;
			}

			//InOut Easings
			animateInCurve = DataManager.inst.AnimationList[_easing].Animation;
			animateOutCurve = DataManager.inst.AnimationList[_easing].Animation;
		}

		public void OnEnableManual(bool _force = false)
		{
			if (animating)
			{
				return;
			}
			StopAnimating();
			transform.DOKill(false);
			gameObject.SetActive(true);
			if (_force)
			{
				transform.localScale = new Vector3((float)(animateX ? 0 : 1), (float)(animateY ? 0 : 1), 0f);
			}
			if (customEase)
			{
				transform.DOScale(Vector3.one, animateInTime).SetEase(animateInCurve).Play();
			}
			else
			{
				transform.DOScale(Vector3.one, animateInTime).Play();
			}
			Debug.Log("Play animate in");
		}

		public void OnDisableManualDelay(float _time = 0.2f, bool _destroy = false)
		{
			StartCoroutine(OnDisableManualDelayLoop(_time, _destroy));
		}

		private IEnumerator OnDisableManualDelayLoop(float _time = 0.2f, bool _destroy = false)
		{
			yield return new WaitForSeconds(_time);
			OnDisableManual(_destroy);
			yield break;
		}

		public void OnDisableManual(bool _destroy = false)
		{
			if (animateOut)
			{
				if (animating)
				{
					return;
				}
				StopAnimating();
				transform.DOKill(false);
				Vector3 endValue = new Vector3((float)(animateX ? 0 : 1), (float)(animateY ? 0 : 1), 0f);
				if (customEase)
				{
					transform.DOScale(endValue, animateOutTime).SetEase(animateOutCurve).Play();
				}
				else
				{
					transform.DOScale(endValue, animateOutTime).Play();
				}
				if (isActiveAndEnabled)
				{
					StartCoroutine(DisableObj(animateOutTime, _destroy));
					return;
				}
			}
			else
			{
				StartCoroutine(DisableObj(0f, _destroy));
			}
		}

		public IEnumerator DisableObj(float _delay = 0f, bool _destroy = false)
		{
			yield return new WaitForSeconds(_delay);
			if (_destroy)
			{
				Destroy(gameObject);
			}
			else
			{
				gameObject.SetActive(false);
			}
			yield break;
		}

		public IEnumerator StopAnimating()
		{
			animating = true;
			yield return new WaitForSeconds(animateInTime);
			animating = false;
			yield break;
		}

		public float animateInTime = 0.2f;
		public float animateOutTime = 0.2f;

		public bool customEase = true;

		public AnimationCurve animateOutCurve;
		public AnimationCurve animateInCurve;

		public bool animating;

		public bool animateX = true;

		public bool animateY = true;

		public bool animateOut = true;

		public bool force = true;
	}
}
