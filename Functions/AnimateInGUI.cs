using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace EditorManagement.Functions
{
	public class AnimateInGUI : MonoBehaviour
	{
		private void Start()
		{
		}

		private void OnEnable()
		{
			OnEnableManual(false);
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
				transform.DOScale(Vector3.one, animateInTime).SetEase(animateInCurve).Play<Tweener>();
			}
			else
			{
				transform.DOScale(Vector3.one, animateInTime).Play<Tweener>();
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
					transform.DOScale(endValue, animateInTime).SetEase(animateInCurve).Play<Tweener>();
				}
				else
				{
					transform.DOScale(endValue, animateInTime).Play<Tweener>();
				}
				if (isActiveAndEnabled)
				{
					StartCoroutine(DisableObj(animateInTime, _destroy));
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

		public bool customEase;

		public AnimationCurve animateInCurve;

		public bool animating;

		public bool animateX = true;

		public bool animateY = true;

		public bool animateOut = true;
	}
}
