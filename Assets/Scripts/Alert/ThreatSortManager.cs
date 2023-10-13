using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityTemplateProjects.Visuals;

public class ThreatSortManager : MonoBehaviour
{
    public static ThreatSortManager manager;
    
    private List<HighThreat> _highThreats;

    [SerializeField] private Transform detector;
    [SerializeField] private VolumetricMeshRenderer volumeRenderer;
    
    private bool detectorExists;
    
    // Start is called before the first frame update
    void Awake()
    {
        if (manager == null)
        {
            manager = this;
            _highThreats = new List<HighThreat>();
            //DontDestroyOnLoad(this.gameObject);
            return;
        }
        Destroy(this);
    }

    private void Start()
    {
        detectorExists = detector != null&& volumeRenderer != null ? true:false;
     
    }

    private void OnDestroy()
    {
        _highThreats.Clear();
    }

    public void AddThreat(HighThreat threat)
    {
        if (!_highThreats.Contains(threat))
        {
            _highThreats.Add(threat);
        }
    }

    public void RemoveThreat(HighThreat threat)
    {
        if (_highThreats.Contains(threat))
        {
            _highThreats.Remove(threat);
        }
    }
    
    // Update is called once per frame
    void LateUpdate()
    {
        if (!detectorExists) return;
        
        foreach (var threat in _highThreats)
        {
            threat.EvaluateThreat(detector);
        }
        
        _highThreats.Sort(delegate(HighThreat threat, HighThreat highThreat)
        {
            return highThreat.threatRate.CompareTo(threat.threatRate);
        } );

        for (int i = 0; i < Mathf.Min(4, _highThreats.Count); ++i)
        {
            volumeRenderer.SetCamera(i+1, _highThreats[i].GetCamera());
        }
        
    }
}
