using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class ProgressCircleController : MonoBehaviour
{
    [SerializeField] GameObject[] ProgressBeanObjects;
    [SerializeField] Image[] ProgressBeanImages;
    Coroutine coroutine;
    [SerializeField] [Range(1f, 120f)] float speed = 30f;
    [SerializeField] [Range(2, 20)] int gradient = 10;
    // Start is called before the first frame update
    void Start()
    {
        foreach(var obj in ProgressBeanObjects)
        {
            obj.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        //StartProgress();
    }
    private void OnDisable()
    {
        //StopProgress();
    }
    public void StartProgress()
    {
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        //coroutine = StartCoroutine(ProgressCircleSequenceBySetActive());
        //coroutine = StartCoroutine(ProgressCircleSequenceByChangeAlpha());
        coroutine = StartCoroutine(BecomingTransparentAndOpaqueAllTogetherProcess(ProgressBeanImages));
    }
    public void StopProgress()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }
    public void SetByPercentage(float percentage)
    {
        float unit = 100f / (float)(ProgressBeanObjects.Length - 1);
        int index = (int)(percentage / unit);
        for (int i = 0; i < ProgressBeanObjects.Length; i++)
        {
            if (i <= index && percentage > 0)
            {
                if (ProgressBeanObjects[i].activeSelf != true) ProgressBeanObjects[i].SetActive(true);
            }
            else
            {
                if (ProgressBeanObjects[i].activeSelf != false) ProgressBeanObjects[i].SetActive(false);
            }
        }
    }
    IEnumerator ProgressCircleSequenceBySetActive()
    {
        int i = 0;
        while (true)
        {
            ProgressBeanObjects[i].SetActive(false);
            if (i - 1 < 0)
            {
                ProgressBeanObjects[ProgressBeanObjects.Length - 1].SetActive(true);
            }
            else
            {
                ProgressBeanObjects[i - 1].SetActive(true);
            }
            i++;
            if (i > ProgressBeanObjects.Length - 1) i = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }
    IEnumerator ProgressCircleSequenceByChangeAlpha()
    {
        int i = 0;
        //Image image = ProgressBeanImages[ProgressBeanImages.Length - 1];
        //image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
        while (true)
        {
            //yield return StartCoroutine(BecomingTransparentProcess(ProgressBeanImages[i]));
            //if (i - 1 < 0)
            //{
            //    yield return StartCoroutine(BecomingOpaqueProcess(ProgressBeanImages[ProgressBeanImages.Length - 1]));
            //}
            //else
            //{
            //    yield return StartCoroutine(BecomingOpaqueProcess(ProgressBeanImages[i - 1]));
            //}
            if (i - 1 < 0)
            {
                yield return StartCoroutine(BecomingTransparentAndOpaqueProcess(ProgressBeanImages[i], ProgressBeanImages[ProgressBeanImages.Length - 1]));
                //yield return StartCoroutine(BecomingOpaqueProcess(ProgressBeanImages[ProgressBeanImages.Length - 1]));
            }
            else
            {
                yield return StartCoroutine(BecomingTransparentAndOpaqueProcess(ProgressBeanImages[i], ProgressBeanImages[i - 1]));
            }
            i++;
            if (i > ProgressBeanImages.Length - 1) i = 0;
        }
    }
    IEnumerator BecomingTransparentAndOpaqueAllTogetherProcess(Image[] images)
    {
        //List<float> floats = Enumerable.Repeat(0f, images.Length).ToList();
        //float step = 1f / (float)(images.Length - 1);
        //for (int i = 0; i < images.Length; i++)
        //{
        //    floats[i] = i * step; // // ¶¶®É°w
        //    //floats[images.Length - 1 - i] = i * step; // °f®É°w
        //    images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, floats[i]);
        //}

        while (true)
        {
            //{   // ¤£°µ´¡­È
            //    for (int j = 0; j < images.Length; j++)
            //    {
            //        for (int i = 0; i < images.Length; i++)
            //        {
            //            float targetAlpha;
            //            targetAlpha = i - j < 0 ? floats[images.Length + (i - j)] : floats[i - j];  // ¶¶®É°w
            //            //targetAlpha = i + j > images.Length - 1 ? floats[(i + j) - images.Length] : floats[i + j];  // °f®É°w

            //            images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, targetAlpha);
            //        }
            //        yield return new WaitForSeconds(10f / (float)(images.Length - 1));
            //    }
            //}

            //{  // ´¡­È
            //    for (int j = 0; j < images.Length; j++)
            //    {
            //        for (int k = 0; k < images.Length; k++)
            //        {
            //            float t = (float)k / (float)(images.Length - 1);
            //            for (int i = 0; i < images.Length; i++)
            //            {
            //                float currentAlpha = images[i].color.a;
            //                float targetAlpha;
            //                targetAlpha = i - j < 0 ? floats[images.Length + (i - j)] : floats[i - j];  // ¶¶®É°w
            //                //targetAlpha = i + j > images.Length - 1 ? floats[(i + j) - images.Length] : floats[i + j];  // °f®É°w
            //                float lerpAlpha = Mathf.Lerp(currentAlpha, targetAlpha, t);
            //                //Debug.Log($"{currentAlpha},{targetAlpha},{t},{lerpAlpha}");
            //                images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, lerpAlpha);
            //            }
            //            yield return new WaitForSeconds(1f / (float)(images.Length - 1));
            //        }
            //    }
            //}
            for (int k = 0; k < images.Length; k++)
            {
                for (int j = 0; j < gradient; j++)
                {
                    for (int i = 0; i < images.Length; i++)
                    {
                        int lTarget = i - k < 0 ? images.Length + (i - k) : i - k;
                        float tTarget = (float)lTarget / (float)(images.Length - 1);
                        float targetAlpha = Mathf.Lerp(0f, 1f, tTarget);

                        int lOrigin = i - k + 1 < 0 ? images.Length + (i - k + 1) : i - k + 1 > images.Length - 1 ? (i - k + 1) - images.Length : i - k + 1;
                        float tOrigin = (float)lOrigin / (float)(images.Length - 1);
                        float originAlpha = Mathf.Lerp(0f, 1f, tOrigin);

                        float lerpAlpha = Mathf.Lerp(originAlpha, targetAlpha, (float)j / (float)(gradient - 1));
                        images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, lerpAlpha);
                        if (i == 15)
                        {
                            Debug.Log($"{originAlpha},{targetAlpha},{lerpAlpha},{k},{j},{i}");
                        }
                    }
                    //yield return new WaitForSeconds(1f / (float)(images.Length - 1));
                    yield return new WaitForSeconds(1 / speed);
                }
            }
        }
    }
    IEnumerator BecomingTransparentAndOpaqueProcess(Image image1, Image image2)
    {
        float f1 = 1f;
        float f2 = 0f;
        for (int i = 0; i < 10; i++)
        {
            f1 -= 0.1f;
            f2 += 0.1f;
            if (f1 < 0.1)
            {
                f1 = 0f;
            }
            if (f2 > 0.9)
            {
                f2 = 1f;
            }
            image1.color = new Color(image1.color.r, image1.color.g, image1.color.b, f1);
            image2.color = new Color(image2.color.r, image2.color.g, image2.color.b, f2);
            yield return new WaitForSeconds(0.05f);
        }
    }
    IEnumerator BecomingTransparentProcess(Image image)
    {
        float f = 1f;
        for (int i = 0; i < 10; i++)
        {
            f -= 0.1f;
            if (f < 0.1)
            {
                f = 0f;
            }
            image.color = new Color(image.color.r, image.color.g, image.color.b, f);
            yield return new WaitForSeconds(0.025f);
        }
    }
    IEnumerator BecomingOpaqueProcess(Image image)
    {
        float f = 0f;
        for (int i = 0; i < 10; i++)
        {
            f += 0.1f;
            if (f > 0.9)
            {
                f = 1f;
            }
            image.color = new Color(image.color.r, image.color.g, image.color.b, f);
            yield return new WaitForSeconds(0.025f);
        }
    }
}
