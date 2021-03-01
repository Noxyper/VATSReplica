using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class PlayerMove : MonoBehaviour
{
    [HideInInspector]
    public CharacterController controller;
    Camera _camera;
        
    float _rotX;
    float _animTransSpeed = 5f;

    public Animator playerAnimator;

    public float playerSpeed = 5f;
    public float cameraSensitivity = 90f;
    public bool stationary = false;
    public bool mouseRestriction = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        _camera = Camera.main;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!stationary)
        {
            playerAnimator.SetFloat("MoveX", Mathf.Lerp(playerAnimator.GetFloat("MoveX"), Input.GetAxisRaw("Horizontal"), Time.deltaTime * _animTransSpeed));
            playerAnimator.SetFloat("MoveY", Mathf.Lerp(playerAnimator.GetFloat("MoveY"), Input.GetAxisRaw("Vertical"), Time.deltaTime * _animTransSpeed));
            playerAnimator.SetBool("Moving", Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);

            controller.SimpleMove(((transform.right * Input.GetAxisRaw("Horizontal")) + (transform.forward * Input.GetAxisRaw("Vertical"))) * playerSpeed);
        }

        if (!mouseRestriction)
        {
            float tempMouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * cameraSensitivity;

            _rotX -= tempMouseY;
            _rotX = Mathf.Clamp(_rotX, -90f, 90f);

            _camera.transform.localEulerAngles = new Vector3(_rotX, 0f, 0f);

            transform.Rotate(Vector3.up * (Input.GetAxisRaw("Mouse X") * Time.deltaTime * cameraSensitivity));
        }
    }
}
