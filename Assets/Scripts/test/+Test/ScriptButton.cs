using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScriptButton : MonoBehaviour
{
    public GM gm;
    public Button but;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void restart(GM gm)
    {
        gm.Restart();
    }
    public void OnButtonPress()
    {
        gm.Restart();
    }
}
