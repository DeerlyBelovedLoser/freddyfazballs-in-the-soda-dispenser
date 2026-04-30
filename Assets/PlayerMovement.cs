using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//https://www.youtube.com/watch?v=zHSWG05byEc&t=245s 11:30

public class PlayerMovement : MonoBehaviour
{
  [Header("References")]
  public PlayerMovementStats MoveStats;
  [SerializeField] private Collider2D _feetColl;
  [SerializeField] private Collider2D _bodyColl;

  private Rigidbody2D _rb;

  //movement variables
  private Vector2 _moveVelocity;
  private bool _isFacingRight;

  //collision check
  private RaycastHit2D _groundHit;
  private RaycastHit2D _headHit;
  private bool _isGrounded;
  private bool _bumpedHead;

  //jump variables
  public float VerticalVelocity { get; private set; }
  private bool _isJumping;
  private bool _isFastFalling;
  private bool _isFalling;
  private float _fastFallTime;
  private float _fastFallReleaseSpeed;
  private int _numberOfJumpsUsed;

  //apex variables
  private float _apexPoint;
  private float _timePastApexThreshold;
  private bool _isPastApexThreshold;

  //jump buffer variables
  private float _jumpBufferTimer;
  private float _jumpReleaseDuringBuffer;

  //coyote time variables
  private float _coyoteTimer;

  private void Awake()
  {
    _isFacingRight = true;

    _rb = GetComponent<Rigidbody2D>();
  }

  private void Update()
  {
    CountTimers();
    JumpChecks();
  }

  private void FixedUpdate()
  {
    CollisionChecks();
    Jump();

    if (_isGrounded)
    {
      Move(MoveStats.GroundAcceleration, MoveStats.GroundDeceleration, InputManager.Movement);
    }
    else
    {
      Move(MoveStats.AirAcceleration, MoveStats.AirDeceleration, InputManager.Movement);
    }
  }

  #region Movement

  private void Move(float acceleration, float deceleration, Vector2 moveInput)
  {
    if(moveInput != Vector2.zero)
    {
      TurnCheck(moveInput);

      Vector2 targetVelocity = Vector2.zero;
      if (InputManager.RunIsHeld)
      {
        targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.MaxRunSpeed;
      }
      else { targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.MaxWalkSpeed; }

      _moveVelocity = Vector2.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
      _rb.linearVelocity = new Vector2(_moveVelocity.x, _rb.linearVelocity.y);
    }
    
    else if (moveInput == Vector2.zero)
    {
      _moveVelocity = Vector2.Lerp(_moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
      _rb.linearVelocity = new Vector2(_moveVelocity.x, _rb.linearVelocity.y);
    }
  }

  private void TurnCheck(Vector2 moveInput)
  {
    if(_isFacingRight && moveInput.x < 0)
    {
      Turn(false);
    }
    else if (!_isFacingRight && moveInput.x > 0)
    {
      Turn(true);
    }
  }

  private void Turn(bool turnRight)
  {
    if (turnRight)
    {
      _isFacingRight = true; 
      transform.Rotate(0f, 180f, 0f);
    }
    else
    {
      _isFacingRight = false;
      transform.Rotate(0f, -180f, 0f);
    }
  }

  #endregion
 
 #region Jump

  private void JumpChecks()
  {
    //when we press jump button
    if (InputManager.JumpWasPressed)
    {
      _jumpBufferTimer = MoveStats.JumpBufferTime;
      _jumpReleasedDuringBuffer = false; 
    }

    //when we release jump button
    if (InputManager.JumpWasReleased)
    {
      if (_jumpBufferTimer > 0f)
      {
        _jumpReleasedDuringBuffer = true;
      }

      if (_isJumping &&  VerticalVelocity > 0f)
      {
        if (_isPastApexThreshold)
        {
          _isPastApexThreshold = false;
          _isFastFalling = true;
          _fastFallTime = MoveStats.TimeForUpwardsCancel;
          VerticalVelocity = 0f;
        }
        else
        {
          _isFastFalling = true;
          _fastFallReleaseSpeed = VerticalVelocity;
        }
      }
    }

    //jump buffering and coyote time
    if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
    {

    }

    //double jump

    //air jump after coyote time lapsed

    //landed
  }

  private void jump()
  {

  }

 #endregion

  #region Collision Checks

  private void IsGrounded()
  {
    Vector2 boxCastOrigin = new Vector2(_feetColl.bounds.center.x, _feetColl.bounds.min.y);
    Vector2 boxCastSize = new Vector2(_feetColl.bounds.size.x, MoveStats.GroundDetectionRayLength);

    _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);
    if (_groundHit.collider != null)
    {
      _isGrounded = true;
    }
    else { _isGrounded = false; }

    #region Debug Visualization
    if (MoveStats.DebugShowGrounding)
    {
      Color rayColor;
      if(_isGrounded)
      {
        rayColor = Color.green;
      }
      else { rayColor = Color.red; }

      Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
      Debug.DrawRay(new Vector2(boxCastOrigin.x * boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
      Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - MoveStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
    }
    #endregion
  }

  private void CollisionChecks()
  {
    IsGrounded();
  }

  #endregion

  #region Timers

  private void CountTimers()
  {
    _jumpBufferTimer -= Time.fixedDeltaTime;

    if (!_isGrounded)
    {
      _coyoteTimer -= Time.deltaTime;
    }
    else { _coyoteTimer = MoveStats.JumpCoyoteTime; }
  }

  #endregion
}
