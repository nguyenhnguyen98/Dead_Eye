using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System;
using DG.Tweening;
using UnityEngine.Rendering;

using UnityEngine.Rendering.HighDefinition;

public class ShooterController : MonoBehaviour
{
    private MovementInput _input;
    private Animator _animator;

    [Header("Cinemachine")]
    public CinemachineFreeLook thirdPersonCam;
    public Volume volume;
    private CinemachineImpulseSource _impulse;
    
    [Space]
    [Header("Booleans")]
    public bool aiming = false;
    public bool deadEye = false;

    [Space]
    [Header("Settings")]
    private float _originalZoom;
    public float originalOffsetAmount;
    public float zoomOffsetAmount;
    public float aimTime;

    [Space]
    [Header("Targets")]
    public List<Transform> targets = new List<Transform>();

    [Space]
    [Header("UI")]
    public GameObject aimPrefab;
    public List<Transform> crossList = new List<Transform>();
    public Transform canvas;
    public Image reticle;

    [Space]
    [Header("Gun")]
    public Transform gun;
    private Vector3 _gunIdlePos;
    private Vector3 _gunIdleRot;
    [SerializeField]
    private Vector3 _gunAimPos = new Vector3(-0.0655034f, 0.2626379f, 0.03993922f);
    [SerializeField]
    private Vector3 _gunAimRot = new Vector3(-176.99f, 98.733f, 97.122f);

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        _input = GetComponent<MovementInput>();
        _originalZoom = thirdPersonCam.m_Orbits[1].m_Radius;
        _impulse = thirdPersonCam.GetComponent<CinemachineImpulseSource>();

        _gunIdlePos = gun.localPosition;
        _gunIdleRot = gun.localEulerAngles;

        HorizontalOffset(originalOffsetAmount);
    }

    // Update is called once per frame
    void Update()
    {
        if (aiming)
        {
            if (targets.Count > 0)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    crossList[i].position = Camera.main.WorldToScreenPoint(targets[i].position);
                }
            }
        }

        if (deadEye)
            return;

        if (!aiming)
            WeaponPosition();

        if (Input.GetMouseButtonDown(1) && !deadEye)
            Aim(true);

        if (Input.GetMouseButtonUp(1) && aiming)
        {
            if (targets.Count > 0)
            {
                DeadEye(true);

                Sequence s = DOTween.Sequence();
                for (int i = 0; i < targets.Count; i++)
                {
                    s.Append(transform.DOLookAt(targets[i].GetComponentInParent<EnemyScript>().transform.position, .05f).SetUpdate(true));
                    s.AppendCallback(() => _animator.SetTrigger("Fire"));
                    int x = i;
                    s.AppendInterval(0.05f);
                    s.AppendCallback(() => FirePolish());
                    s.AppendCallback(() => targets[x].GetComponentInParent<EnemyScript>().Ragdoll(true, targets[x]));
                    s.AppendCallback(() => crossList[x].GetComponent<Image>().color = Color.clear);
                    s.AppendInterval(0.45f);
                }

                s.AppendCallback(() => Aim(false));
                s.AppendCallback(() => DeadEye(false));
            } else
            {
                Aim(false);
            }
        }

        if (aiming)
        {
            _input.LookAt(Camera.main.transform.forward + (Camera.main.transform.right * .1f));

            RaycastHit hit;
            Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit);

            if (!deadEye)
            {
                reticle.color = Color.white;
            }

            if (hit.transform == null)
                return;

            if (!hit.collider.CompareTag("Enemy"))
                return;

            reticle.color = Color.red;

            if (Input.GetMouseButtonDown(0))
            {
                if (!targets.Contains(hit.transform) && !hit.transform.GetComponentInParent<EnemyScript>().aimed)
                {
                    hit.transform.GetComponentInParent<EnemyScript>().aimed = true;
                    targets.Add(hit.transform);

                    Vector3 convertedPos = Camera.main.WorldToScreenPoint(hit.transform.position);
                    GameObject cross = Instantiate(aimPrefab, canvas);
                    cross.transform.position = convertedPos;
                    crossList.Add(cross.transform);
                }
            }
        }
    }
    private void WeaponPosition()
    {
        bool state = _input.speed > 0;

        Vector3 pos = state ? _gunIdlePos : _gunIdlePos;
        Vector3 rot = state ? _gunIdleRot : _gunIdleRot; 
        gun.DOLocalMove(pos, .3f);
        gun.DOLocalRotate(rot, .3f);
    }

    private void FirePolish()
    {
        _impulse.GenerateImpulse();

        foreach (ParticleSystem particle in gun.GetComponentsInChildren<ParticleSystem>())
        {
            particle.Play();
        }
    }

    public void DeadEye(bool state)
    {
        deadEye = state;

        float animationSpeed = state ? 3 : 1;
        _animator.speed = animationSpeed;

        if (state)
            reticle.DOColor(Color.clear, .05f);

        if (!state)
        {
            targets.Clear();

            foreach (Transform transform in crossList)
            {
                Destroy(transform.gameObject);
            }
            crossList.Clear();
        }

        _input.enabled = !state;
    }

    public void Aim(bool state)
    {
        aiming = state;

        float xOgOffset = state ? originalOffsetAmount : zoomOffsetAmount;
        float xCurrentOffset = state ? zoomOffsetAmount : originalOffsetAmount;
        float yOgOffset = state ? 1.5f : 1.5f - .1f;
        float yCurrentOffset = state ? 1.5f - .1f : 1.5f;
        float zoom = state ? 20f : 40f;

        DOVirtual.Float(xOgOffset, xCurrentOffset, aimTime, HorizontalOffset);
        DOVirtual.Float(thirdPersonCam.m_Lens.FieldOfView, zoom, aimTime, CameraZoom);

        _animator.SetBool("Aiming", state);

        float timeScale = state ? .4f : 1f;
        float ogTimeScale = state ? 1f : .4f;
        DOVirtual.Float(ogTimeScale, timeScale, .2f, SetTimeScale);

        if (!state)
            transform.DORotate(new Vector3(0, transform.eulerAngles.y, transform.eulerAngles.z), .2f);

        Vector3 pos = state ? _gunAimPos : _gunIdlePos;
        Vector3 rot = state ? _gunAimRot : _gunIdleRot;
        gun.DOComplete();
        gun.DOLocalMove(pos, .1f);
        gun.DOLocalRotate(rot, .1f);

        float ogWeight = state ? 0f : 1f;
        float newWeight = state ? 1f : 0f;

        DOVirtual.Float(ogWeight, newWeight, aimTime, WeightAdjustment);

        Color c = state ? Color.white : Color.clear;
        reticle.color = c;
    }

    void CameraZoom(float x)
    {
        thirdPersonCam.m_Lens.FieldOfView = x;
    }

    void HorizontalOffset(float x)
    {
        for (int i = 0; i < 3; i++)
        {
            CinemachineComposer c = thirdPersonCam.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
            c.m_TrackedObjectOffset.x = x;
        }
    }

    void SetTimeScale(float x)
    {
        Time.timeScale = x;
    }

    void WeightAdjustment(float x)
    {
        volume.weight = x;
    }
}
