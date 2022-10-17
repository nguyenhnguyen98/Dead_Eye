using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    private Rigidbody[] _rbs;
    private Animator _animator;
    private ShooterController _shooter;

    public bool aimed;
    
    // Start is called before the first frame update
    void Start()
    {
        _shooter = FindObjectOfType<ShooterController>();
        _animator = GetComponent<Animator>();
        _rbs = GetComponentsInChildren<Rigidbody>();
        Ragdoll(false, transform);
    }

    public void Ragdoll(bool state, Transform point)
    {
        _animator.enabled = !state;
        foreach (Rigidbody rigidbody in _rbs)
        {
            rigidbody.isKinematic = !state;
        }

        if (state)
        {
            point.GetComponent<Rigidbody>().AddForce(_shooter.transform.forward * 30, ForceMode.Impulse);
        }
    }
}
