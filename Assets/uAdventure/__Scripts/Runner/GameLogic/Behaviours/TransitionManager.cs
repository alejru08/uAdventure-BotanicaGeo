﻿using System;
using System.Collections;
using uAdventure.Core;
using UnityEngine;
using Action = System.Action;

namespace uAdventure.Runner
{
    [RequireComponent(typeof(MeshRenderer))]
    public class TransitionManager : MonoBehaviour
    {
        [SerializeField] private Material transitionMaterial;
        private Texture transitionTexture;
        private RenderTexture renderTexture;
        private bool transitioning;
        private Transition transition;
        private Coroutine transitionRoutine;
        private float endTime;
        private Action<Transition, Texture> onFinish;

        void Awake()
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.Create();
            Update();
        }

        void Update()
        {
            if(!transitioning || (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height))
            {
                if (renderTexture)
                {
                    renderTexture.Release();
                    Destroy(renderTexture);
                }

                renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                renderTexture.Create();
            }

            if (transitioning && transitionRoutine != null && Time.time > endTime)
            {
                FinalizeTransition(transition, transitionTexture, onFinish);
            }
        }

        public void UseMaterial(Material material)
        {
            this.transitionMaterial = material;
        }

        public void PrepareTransition(Transition transition)
        {
            if (transitionRoutine != null)
            {
                Debug.LogError("The transition manager was already transitioning!");
                return;
            }

            CreateTransitionTexture();
            PrepareTransition(transition, renderTexture);
        }

        public void PrepareTransition(Transition transition, Texture transitionTexture)
        {
            if (transitionRoutine != null)
            {
                Debug.LogError("The transition manager was already transitioning!");
                return;
            }

            this.transitioning = true;
            this.transitionTexture = transitionTexture;
            this.transition = transition;
        }

        public void DoTransition(Action<Transition, Texture> onFinish)
        {
            if (transitionRoutine != null)
            {
                Debug.LogError("The transition was already started!");
                FinalizeTransition(transition, transitionTexture, onFinish);
                return;
            }

            if (transition == null || transition.getType() == TransitionType.NoTransition || transition.getTime() == 0)
            {
                FinalizeTransition(transition, transitionTexture, onFinish);
                return;
            }

            this.endTime = Time.time + transition.getTime() / 1000f;
            this.onFinish = onFinish;
            this.transitionRoutine = StartCoroutine(TransitionRoutine(transition));
        }

        private void ResetMaterial()
        {
            transitionMaterial.SetFloat("_DirectionX", 0);
            transitionMaterial.SetFloat("_DirectionY", 0);
            transitionMaterial.SetFloat("_Progress", 0);
            transitionMaterial.SetFloat("_Blend", 0);
        }

        private IEnumerator TransitionRoutine(Transition transition)
        {
            var timeLeft = transition.getTime()/1000f;
            var totalTime = timeLeft;
            var fade = false;
            ResetMaterial();
            transitionMaterial.SetTexture("_TransitionTex", transitionTexture);

            switch (transition.getType())
            {
                case TransitionType.FadeIn:
                    fade = true;
                    break;
                case TransitionType.BottomToTop:
                    transitionMaterial.SetFloat("_DirectionY", -1);
                    break;
                case TransitionType.TopToBottom:
                    transitionMaterial.SetFloat("_DirectionY", 1);
                    break;
                case TransitionType.RightToLeft:
                    transitionMaterial.SetFloat("_DirectionX", -1);
                    break;
                case TransitionType.LeftToRight:
                    transitionMaterial.SetFloat("_DirectionX", 1);
                    break;
            }

            while (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                transitionMaterial.SetFloat(fade ? "_Blend" : "_Progress", Mathf.Clamp01(1 - (timeLeft / totalTime)));
                yield return null;
            }
        }

        private void FinalizeTransition(Transition transition, Texture transitionTexture, Action<Transition, Texture> onFinish)
        {
            transitioning = false;
            this.transition = null;
            ResetMaterial();
            transitionRoutine = null;
            transitionMaterial.SetTexture("_MainTex", transitionTexture);
            transitionMaterial.SetTexture("_TransitionTex", null);
            onFinish(transition, transitionTexture);
        }


        private void CreateTransitionTexture()
        {
            var mainCamera = Camera.main;
            mainCamera.targetTexture = renderTexture;

            RenderTexture.active = renderTexture;
            mainCamera.Render();

            RenderTexture.active = null;
            mainCamera.targetTexture = null;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (transitioning)
            {
                transitionMaterial.SetTexture("_TransitionTex", source);
                Graphics.Blit(transitionTexture, destination, transitionMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}