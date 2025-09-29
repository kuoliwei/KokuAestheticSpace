using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonInit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }
    private void OnEnable()
    {
        transform.Find("After").gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
