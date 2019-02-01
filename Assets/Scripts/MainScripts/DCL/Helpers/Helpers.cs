using DCL.Components;
using DCL.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Networking;

namespace DCL.Helpers
{
  

    public static class Utils
    {
        /**
         * Transforms a grid position into a world-relative 3d position
         */
        public static Vector3 GridToWorldPosition(float xGridPosition, float yGridPosition)
        {
            return new Vector3(
              x: xGridPosition * ParcelSettings.PARCEL_SIZE,
              y: 0f,
              z: yGridPosition * ParcelSettings.PARCEL_SIZE
            );
        }

        /**
         * Transforms a world position into a grid position
         */
        public static Vector2 worldToGrid(Vector3 vector)
        {
            return new Vector2(
              Mathf.Floor(vector.x / ParcelSettings.PARCEL_SIZE),
              Mathf.Floor(vector.z / ParcelSettings.PARCEL_SIZE)
            );
        }


        public static T GetOrCreateComponent<T>(GameObject gameObject) where T : UnityEngine.Component
        {
            T component = gameObject.GetComponent<T>();
            if (!component)
            {
                return gameObject.AddComponent<T>();
            }
            return component;
        }

        public static bool WebRequestSucceded(UnityWebRequest request)
        {
            return request != null && !request.isNetworkError && !request.isHttpError;
        }

        static IEnumerator FetchAsset(string url, UnityWebRequest request, System.Action<UnityWebRequest> OnSuccess=null, System.Action<string> OnFail = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                using (var webRequest = request)
                {
                    yield return webRequest.SendWebRequest();

                    if (!WebRequestSucceded(request))
                    {
                        Debug.LogError(string.Format("Fetching asset failed ({0}): {1} ", request.url, webRequest.error));

                        if (OnFail != null)
                        {
                            OnFail.Invoke(webRequest.error);
                        }
                    }
                    else
                    {
                        if (OnSuccess != null)
                        {
                            OnSuccess.Invoke(webRequest);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError( string.Format("Can't fetch asset as the url is empty!") );
            }
        }
        
        public static IEnumerator FetchAudioClip(string url, AudioType audioType, Action<AudioClip> OnSuccess, Action<string> OnFail)
        {
            //NOTE(Brian): This closure is called when the download is a success.
            Action<UnityWebRequest> OnSuccessInternal =
                (request) =>
                {
                    if (OnSuccess != null)
                    {
                        AudioClip ac = DownloadHandlerAudioClip.GetContent(request);
                        OnSuccess.Invoke(ac);
                    }
                };

            Action<string> OnFailInternal =
            (error) =>
            {
                if (OnFail != null)
                {
                    OnFail.Invoke(error);
                }
            };

            yield return FetchAsset(url, UnityWebRequestMultimedia.GetAudioClip(url, audioType), OnSuccessInternal, OnFailInternal);
        }

        public static IEnumerator FetchTexture(string textureURL, Action<Texture> OnSuccess)
        {
            //NOTE(Brian): This closure is called when the download is a success.
            System.Action<UnityWebRequest> OnSuccessInternal =
                (request) =>
                {
                    if (OnSuccess != null)
                    {
                        OnSuccess.Invoke(DownloadHandlerTexture.GetContent(request));
                    }
                };

            yield return FetchAsset(textureURL, UnityWebRequestTexture.GetTexture(textureURL), OnSuccessInternal);
        }

        public static AudioType GetAudioTypeFromUrlName(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("GetAudioTypeFromUrlName >>> Null url!");
                return AudioType.UNKNOWN;
            }

            string ext = url.Substring(url.Length - 3).ToLower();

            switch (ext)
            {
                case "mp3":
                    return AudioType.MPEG;
                case "wav":
                    return AudioType.WAV;
                case "ogg":
                    return AudioType.OGGVORBIS;
                default:
                    return AudioType.UNKNOWN;
            }
        }

        public static bool SafeFromJsonOverwrite(string json, object objectToOverwrite)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(json, objectToOverwrite);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError("ArgumentException Fail!... Json = " + json + " " + e.ToString());
                return false;
            }

            return true;
        }

        public static T SafeFromJson<T>(string json) where T : new()
        {
            T returningValue;

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    returningValue = JsonUtility.FromJson<T>(json);
                }
                catch (System.ArgumentException e)
                {
                    Debug.LogError("ArgumentException Fail!... Json = " + json + " " + e.ToString());

                    returningValue = new T();
                }
            }
            else
            {
                returningValue = new T();
            }

            return returningValue;
        }


        public static GameObject AttachPlaceholderRendererGameObject(UnityEngine.Transform targetTransform)
        {
            var placeholderRenderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();

            placeholderRenderer.material = Resources.Load<Material>("Materials/AssetLoading");
            placeholderRenderer.transform.SetParent(targetTransform);
            placeholderRenderer.transform.localPosition = Vector3.zero;
            placeholderRenderer.name = "PlaceholderRenderer";

            return placeholderRenderer.gameObject;
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
#else
            Destroy(obj);
#endif
        }

        // todo; check if your TEnum is enum && typeCode == TypeCode.Int
        public struct FastEnumIntEqualityComparer<TEnum> : IEqualityComparer<TEnum>
            where TEnum : struct
        {
            static class BoxAvoidance
            {
                static readonly Func<TEnum, int> _wrapper;

                public static int ToInt(TEnum enu)
                {
                    return _wrapper(enu);
                }

                static BoxAvoidance()
                {
                    var p = Expression.Parameter(typeof(TEnum), null);
                    var c = Expression.ConvertChecked(p, typeof(int));

                    _wrapper = Expression.Lambda<Func<TEnum, int>>(c, p).Compile();
                }
            }

            public bool Equals(TEnum firstEnum, TEnum secondEnum)
            {
                return BoxAvoidance.ToInt(firstEnum) ==
                    BoxAvoidance.ToInt(secondEnum);
            }

            public int GetHashCode(TEnum firstEnum)
            {
                return BoxAvoidance.ToInt(firstEnum);
            }
        }
    }
}
