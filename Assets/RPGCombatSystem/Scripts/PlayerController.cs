using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [HideInInspector] public CharacterController charCont;
    [HideInInspector] public Animator anim;
    public GameObject childPlayer;
    public Camera cam;
    public GameObject movIndicator; //Where is the character moving to

    [HideInInspector] public SoundManager soundMan;

    public float speed = 6.0f; //Speed of the player
    public float jumpSpeed = 8.0f; //How high does the player jump
    public float gravity = 20.0f; //Gravity applied to the player
    private bool canJump = true;
    private bool wasGrounded = false; //Dettects the first time the player is falling

    private float mass = 60.0f; // Defines the character mass
    private Vector3 impact = Vector3.zero;

    public bool airControl = true; //If player can control the direction of the movement on falling
    private float fallTime = 0f; //Time the player is falling

    public float maxDashTime = 0.25f; //Time that the player is dashing
    private float currentDashTime;
    public float dashSpeed = 20; //Dash speed of the player
    public float dashCooldown = 2f; //Cooldown between dashes
    private float dashCooldownTimer = 0f;
    private Vector3 dashDir;
    private bool canDash = true;

    private Vector3 moveDirection = Vector3.zero;

    private float distToGround; //Distance to the ground for check if there is ground under the character
    private Vector3 groundNormal;

    [HideInInspector] public bool canMove = true; //If player can move or not because is attaking or hitted
    private bool hit = false; //If player is hitted it will be true
    [HideInInspector] public bool isMoving = false; //Tracks locomotion state for animation

    void Awake()
    {
        charCont = GetComponent<CharacterController>();
        soundMan = GetComponent<SoundManager>();
        anim = GetComponentInChildren<Animator>();
        currentDashTime = maxDashTime;

        // CharacterController Center가 바닥(0)이면 캡슐 하단이 발에 오도록 조정
        if (charCont.center.y < 0.01f)
        {
            charCont.center = new Vector3(0, charCont.height / 2f, 0);
        }

        distToGround = charCont.bounds.extents.y;

        // Auto-find camera if not assigned
        if (cam == null)
            cam = Camera.main;

        // Auto-find child player model (first child with Animator)
        if (childPlayer == null && anim != null)
            childPlayer = anim.gameObject;

        // movIndicator auto-create if not assigned
        if (movIndicator == null)
        {
            movIndicator = new GameObject("MovIndicator");
            movIndicator.transform.SetParent(transform);
            movIndicator.transform.localPosition = Vector3.zero;
        }
    }

    void Start()
    {
        StartCoroutine(SetInitialFacing());
    }

    IEnumerator SetInitialFacing()
    {
        // 1프레임 대기 (카메라 LateUpdate 이후 최종 위치 확정)
        yield return null;

        if (cam != null && childPlayer != null)
        {
            // 카메라→플레이어 방향 = 캐릭터가 등을 보여야 할 방향
            Vector3 lookDir = transform.position - cam.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
                childPlayer.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    void Update()
    {
        if (charCont.isGrounded)
        {
            if (!wasGrounded) //If it is the frame when player touches the ground
            {
                canJump = true;
                if (fallTime > 0.2f )
                {
                    if (soundMan != null) soundMan.PlaySound("Land");
                    if(!hit)
                    {
                        ResetAnimParams();
                        anim.CrossFade("idle", 0.1f);
                        isMoving = false;
                    }
                }
                fallTime = 0f;
            }
        }
        else
        {
            if (wasGrounded && canJump) //If it is the first frame of falling
            {
                if (DistToGround() > 0.3f) //If the ground is far enough
                {
                    moveDirection.y = 0f;
                    wasGrounded = false;
                    ResetAnimParams();
                    anim.SetBool("param_idletojump", true);
                    anim.CrossFade("jump", 0.2f);
                }
            }
            if (charCont.velocity.y < 0) //If player is falling down
                fallTime += Time.deltaTime;
        }
        wasGrounded = charCont.isGrounded;

        if (!canMove)
        {
            if (hit)
            {
                moveDirection.y -= gravity * Time.deltaTime;
                Vector3 impactGrav = new Vector3(impact.x, impact.y + moveDirection.y, impact.z); //Adds gravity to the impact force
                                                                                                  // apply the impact force:
                if (impact.magnitude > 0.2f || !charCont.isGrounded) charCont.Move(impactGrav * Time.deltaTime);
                // consumes the impact energy each cycle:
                impact = Vector3.Lerp(impact, Vector3.zero, 5 * Time.deltaTime);

                if (charCont.isGrounded && impact.magnitude <= 0.2f)
                {
                    hit = false;
                    canMove = true;
                    isMoving = false;
                    ResetAnimParams();
                    anim.Play("idle");
                }
            }
            return;
        }

        if (charCont.isGrounded)
        {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            if (moveDirection.magnitude < 0.1f) //Because if velocity is minor than 0.1 the animator dont play the correct animation
                moveDirection = Vector3.zero;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1); //Limits the vector magnitude to 1

            // SapphiArt locomotion: idle / running (파라미터 동기화)
            if (moveDirection.magnitude > 0)
            {
                if (!isMoving)
                {
                    anim.SetBool("param_idletorunning", true);
                    anim.CrossFade("running", 0.1f);
                    isMoving = true;
                }
            }
            else
            {
                if (isMoving)
                {
                    anim.SetBool("param_idletorunning", false);
                    anim.CrossFade("idle", 0.1f);
                    isMoving = false;
                }
            }

            movIndicator.transform.localPosition = moveDirection; //Positionate the reference that indicates the direction of the movement

            if (dashCooldownTimer > 0f)
                dashCooldownTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && dashCooldownTimer <= 0f) //Dash
            {
                currentDashTime = 0;
                canDash = false;
                dashCooldownTimer = dashCooldown;
                anim.Play("running");
                if (soundMan != null) soundMan.PlaySound("Dash");
                if (moveDirection != Vector3.zero)
                {
                    dashDir = transform.TransformDirection(moveDirection).normalized;

                    //Guide the player to where they are going to move
                    Vector3 targetActPosition = new Vector3(movIndicator.transform.position.x, childPlayer.transform.position.y, movIndicator.transform.position.z);
                    childPlayer.transform.rotation = Quaternion.LookRotation(targetActPosition - childPlayer.transform.position);
                }
                else
                    dashDir = childPlayer.transform.forward;
            }
            if (currentDashTime < maxDashTime)
            {
                dashDir.y = -10f;
                currentDashTime += Time.deltaTime;

                charCont.Move(dashDir * Time.deltaTime * dashSpeed);
                return;
            }
            canDash = true;

            if (moveDirection.magnitude > 0) //Fixes the problem when there is no movement
            {
                //To rotate the controller when moving and position it correctly relative to the camera
                charCont.transform.rotation = new Quaternion(charCont.transform.rotation.x, cam.transform.rotation.y, charCont.transform.rotation.z, cam.transform.rotation.w);

                //Smoothly rotate the character in the xz plane towards the direction of movement
                Vector3 targetActPosition = new Vector3(movIndicator.transform.position.x, childPlayer.transform.position.y, movIndicator.transform.position.z);
                Quaternion rotation = Quaternion.LookRotation(targetActPosition - childPlayer.transform.position);
                childPlayer.transform.rotation = Quaternion.Slerp(childPlayer.transform.rotation, rotation, Time.deltaTime * 10);
            }

            //Rotate it to the player orientation
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed; // apply the horizontal speed


            moveDirection.y = -10f; //To prevent the controller from taking off when going down ramps
            if (!IsGrounded()) //If the character controller says is grounded but the raycast to the ground no
            {
                moveDirection.x += (1f - groundNormal.y) * groundNormal.x;
                moveDirection.z += (1f - groundNormal.y) * groundNormal.z;
            }

            if (Input.GetButtonDown("Jump") && canJump)
            {
                moveDirection.y = jumpSpeed;
                canJump = false;
                ResetAnimParams();
                anim.SetBool("param_idletojump", true);
                anim.Play("jump");
                isMoving = false;
                if (soundMan != null) soundMan.PlaySound("Jump");
            }

        }
        else
        {
            if (currentDashTime < maxDashTime) //If player was dashing
            {
                currentDashTime = maxDashTime;
                canDash = true;
            }
            if (airControl)
            {
                Vector3 moveDirectionTemp = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                moveDirectionTemp = Vector3.ClampMagnitude(moveDirectionTemp, 1); //Limits the vector magnitude to 1
                moveDirection = new Vector3(moveDirectionTemp.x, moveDirection.y, moveDirectionTemp.z);

                movIndicator.transform.localPosition = new Vector3(moveDirection.x, 0, moveDirection.z); //Positionate the reference that indicates the direction of the movement

                if (moveDirectionTemp.magnitude > 0) //Fixes the problem when there is no movement
                {
                    //To rotate the controller when moving and position it correctly relative to the camera
                    charCont.transform.rotation = new Quaternion(charCont.transform.rotation.x, cam.transform.rotation.y, charCont.transform.rotation.z, cam.transform.rotation.w);

                    //Smoothly rotate the character in the xz plane towards the direction of movement
                    Vector3 targetActPosition = new Vector3(movIndicator.transform.position.x, childPlayer.transform.position.y, movIndicator.transform.position.z);
                    Quaternion rotation = Quaternion.LookRotation(targetActPosition - childPlayer.transform.position);
                    childPlayer.transform.rotation = Quaternion.Slerp(childPlayer.transform.rotation, rotation, Time.deltaTime * 10);
                }
                // rotate it to the player orientation
                moveDirection = transform.TransformDirection(moveDirection);
                moveDirection = new Vector3(moveDirection.x * speed * 0.8f, moveDirection.y, moveDirection.z * speed * 0.8f);
            }
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the controller
        charCont.Move(moveDirection * Time.deltaTime);
    }

    public void AddImpact(Vector3 dir, float force) //Apply a force to the player
    {
        moveDirection = Vector3.zero;
        ResetAnimParams();
        anim.SetBool("param_idletodamage", true);
        anim.Play("damage");

        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; //Reflect down force on the ground
        impact += dir.normalized * force / mass;
    }

    public void ApplyDMG(Vector3 dir, float force) //Apply damage to the player
    {
        if (!hit)
        {
            hit = true;
            canMove = false;
            isMoving = false;
            if (soundMan != null) soundMan.PlaySound("Hit");
            currentDashTime = maxDashTime; //Cancels dash if was pressed
            AnimatorEvents animEvents = anim.GetComponent<AnimatorEvents>();
            if (animEvents != null)
                animEvents.DisableWeaponColl();
            AddImpact(dir, force);
        }
    }

    float DistToGround() //Calculates the distance to the ground when starts to fall
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, distToGround + 999))
            return hit.distance - distToGround;
        else return 999;
    }

    public bool IsGrounded() //Check if ground is under the character
    {
        return charCont.isGrounded || Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.3f);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        groundNormal = hit.normal;
        //groundAngle = Vector3.Angle(groundNormal, Vector3.up);
    }

    public bool IsDashing() //Checks if player if dashing
    {
        return currentDashTime < maxDashTime;
    }

    public float GetDashCooldownPercent()
    {
        if (dashCooldown <= 0f) return 0f;
        return Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    }

    public float GetDashCooldownRemaining()
    {
        return Mathf.Max(0f, dashCooldownTimer);
    }

    public void EnableMove(bool camMoveT) //Enables or disables the character movement
    {
        if(!hit)
            canMove = camMoveT;
    }

    // SapphiArt 애니메이터 파라미터 전부 리셋
    void ResetAnimParams()
    {
        anim.SetBool("param_idletorunning", false);
        anim.SetBool("param_idletojump", false);
        anim.SetBool("param_idletodamage", false);
        anim.SetBool("param_idletohit01", false);
        anim.SetBool("param_idletohit02", false);
        anim.SetBool("param_idletohit03", false);
        anim.SetBool("param_idletohit04", false);
        anim.SetBool("param_idletoko_big", false);
    }
}
