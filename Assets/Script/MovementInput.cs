using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementInput : MonoBehaviour
{
    [Header("Public Variables")]
    public float velocity;
    public float desiredRotationSpeed = 0.1f;
    public float speed;
    public float allowPlayerRotation = 0.1f;
    public bool blockRotationPlayer;

    [Space]
    [Header("States")]
    [SerializeField]
    private bool _isGrounded;

    [Space]
    [Header("Animation Smoothing")]
    [Range(0f, 1f)]
    public float hozAnimSmoothTime = 0.2f;
    [Range(0f, 1f)]
    public float vertAnimTime = 0.2f;
    [Range(0f, 1f)]
    public float startAnimTime = 0.3f;
    [Range(0f, 1f)]
    public float stopAnimTime = 0.15f;

    private float _inputX;
    private float _inputZ;
    private Vector3 _desiredMoveDirection;
    private float _verticalVel;
    private Vector3 _moveVector;

    private Animator _animator;
    private Camera _camera;
    private CharacterController _characterController;


    void Start()
    {
        _animator = GetComponent<Animator>();
        _camera = Camera.main;
        _characterController = GetComponent<CharacterController>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        InputMagnitude();

        _isGrounded = _characterController.isGrounded;
        if (_isGrounded)
        {
            _verticalVel -= 0;
        }
        else
        {
            _verticalVel -= 2;
        }
        _moveVector = new Vector3(0, _verticalVel, 0);
        _characterController.Move(_moveVector);
    }

    void InputMagnitude()
    {
        _inputX = Input.GetAxis("Horizontal");
        _inputZ = Input.GetAxis("Vertical");

        speed = new Vector2(_inputX, _inputZ).sqrMagnitude;

        if (speed > allowPlayerRotation)
        {
            _animator.SetFloat("InputMagnitude", speed, startAnimTime, Time.deltaTime);
            PlayerMoveAndRotation();
        }
        else if (speed < allowPlayerRotation)
        {
            _animator.SetFloat("InputMagnitude", speed, stopAnimTime, Time.deltaTime);
        }
    }

    void PlayerMoveAndRotation()
    {
        _inputX = Input.GetAxis("Horizontal");
        _inputZ = Input.GetAxis("Vertical");

        var forward = _camera.transform.forward;
        var right = _camera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        _desiredMoveDirection = forward * _inputZ + right * _inputX;

        if (blockRotationPlayer == false)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_desiredMoveDirection), desiredRotationSpeed);
            _characterController.Move(_desiredMoveDirection * Time.deltaTime * velocity);
        }
    }

    public void LookAt(Vector3 pos)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {
        var forward = _camera.transform.forward;
        var right = _camera.transform.right;

        _desiredMoveDirection = forward;
        t.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_desiredMoveDirection), desiredRotationSpeed);
        t.rotation = Quaternion.Euler(new Vector3(0f, t.rotation.eulerAngles.y, 0f));
    }
}
