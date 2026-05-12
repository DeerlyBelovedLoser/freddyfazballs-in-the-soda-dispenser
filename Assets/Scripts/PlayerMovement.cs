using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
  [Header("References")]
  public PlayerMovementStats MoveStats;
  [SerializeField] private Collider2D _feetColl;
  [SerializeField] private Collider2D _bodyColl;
  public Animator animator;


  private Rigidbody2D _rb;
  //movement variables
  public float HorizontalVelocity { get; private set; }
  private bool _isFacingRight;

  //collision check
  private RaycastHit2D _groundHit;
  private RaycastHit2D _headHit;
  private RaycastHit2D _wallHit;
  private RaycastHit2D _lastWallHit;
  private bool _isGrounded;
  private bool _bumpedHead;
  private bool _isTouchingWall;

  //jump variables
  public float VerticalVelocity { get; private set; }
  public float _jumpTime;
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
  private bool _jumpReleasedDuringBuffer;

  //coyote time variables
  private float _coyoteTimer;

  //wall slide variables
  private bool _isWallSliding;
  private bool _isWallSlideFalling;

  //wall jump variables
  private bool _useWallJumpMoveStats;
  private bool _isWallJumping;
  private float _wallJumpTime;
  public float _wallJumpTimer;

  private bool _isWallJumpFastFalling;
  private bool _isWallJumpFalling;
  private float _wallJumpFastFallTime;
  private float _wallJumpFastFallReleaseSpeed;

  private float _wallJumpPostBufferTimer;

  private float _wallJumpApexPoint;
  private float _timePastWallJumpApexThreshold;
  private bool _isPastWallJumpApexThreshold;

  private void Awake()
  {
    _isFacingRight = true;

    _rb = GetComponent<Rigidbody2D>();
  }

  private void Update()
  {
    CountTimers();
    JumpChecks();
    LandCheck();
    WallJumpCheck();
    animator.SetFloat("magnitude", _rb.linearVelocity.magnitude);
    animator.SetBool("isGrounded", _isGrounded);
    animator.SetBool("isWallSlide", _isWallSliding);

  }

  private void FixedUpdate()
  {
    CollisionChecks();
    WallSlideCheck();
    Jump();
    Fall();
    WallJump();
    
    if(_isWallSliding)
    {
      WallSlide();
    }
    else if (_isGrounded)
    {
      _jumpTime = 0f;
      _numberOfJumpsUsed = 0;
    }

    if (_isGrounded)
    {
      Move(MoveStats.GroundAcceleration, MoveStats.GroundDeceleration, InputManager.Movement);
      _jumpTime = 0f;
    }
    else
    {
      if (_useWallJumpMoveStats)
      {
        Move(MoveStats.WallJumpMoveAcceleration, MoveStats.WallJumpMoveDeceleration, InputManager.Movement);
      }
      else
      {
        Move(MoveStats.AirAcceleration, MoveStats.AirDeceleration, InputManager.Movement);
      }
    }
    if(_isGrounded)
    {
      _isWallSliding = false;
      _isWallJumpFalling = false;
      _isWallJumpFastFalling = false;
      _isWallSlideFalling = false;
    }

    ApplyVelocity();
  }

  private void ApplyVelocity()
  {
    //clamp fall speed
    VerticalVelocity = Mathf.Clamp(VerticalVelocity, -MoveStats.MaxFallSpeed, 50f);
    _rb.linearVelocity = new Vector2(HorizontalVelocity, VerticalVelocity);
  }

  #region Movement

  private void Move(float acceleration, float deceleration, Vector2 moveInput)
  {
    if(Mathf.Abs(moveInput.x) >= MoveStats.MoveThreshold)
    {
      TurnCheck(moveInput);

      float targetVelocity = 0f;
      if (InputManager.RunIsHeld)
      {
        targetVelocity = moveInput.x * MoveStats.MaxRunSpeed;
      }
      else { targetVelocity = moveInput.x * MoveStats.MaxWalkSpeed; }

      HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }
    
    else if (Mathf.Abs(moveInput.x) < MoveStats.MoveThreshold)
    {
      HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, 0f, deceleration * Time.fixedDeltaTime);
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
 
 #region land/fall

    private void LandCheck()
  {
  //landed
  if ((_isJumping || _isFalling || _isWallJumpFalling || _isWallJumping || _isWallSlideFalling || _isWallSliding) && _isGrounded && VerticalVelocity <= 0f)
  {
    ResetJumpValues();
    StopWallSlide();
    ResetWallJumpValues();

    _numberOfJumpsUsed = 0;

    VerticalVelocity = 0f;

  }

  }

  private void Fall()
  {
  //normal gravity
  if(!_isGrounded && !_isJumping && !_isWallSliding && !_isWallJumping)
  {
    if (!_isFalling)
    {
      _isFalling = true;
    }

      VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
    }
  }


 #endregion

 #region Jump

  private void ResetJumpValues()
  {
    _isJumping = false;
    _isFalling = false;
    _isFastFalling = false;
    _fastFallTime = 0f;
    _isPastApexThreshold = false;
    _jumpTime = 0f;
  }

  private void JumpChecks()
  {
    //when we press jump button
    if (InputManager.JumpWasPressed)
    {
      if (_isWallSlideFalling && _wallJumpPostBufferTimer >= 0f)
      {
        return;
      }

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
      InitiateJump();

      if (_jumpReleasedDuringBuffer)
      {
        _isFastFalling = true;
        _fastFallReleaseSpeed = VerticalVelocity;
      }
    }

    //double jump
    else if (_jumpBufferTimer > 0f && (_isJumping || _isWallJumping || _isWallSlideFalling) && !_isTouchingWall && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed)
    {
      _isFastFalling = false;
      InitiateJump();
    }

    //air jump after coyote time lapsed
    else if (_jumpBufferTimer > 0f && _isFalling && !_isWallSliding && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed - 1)
    {
      InitiateJump();
      _isFastFalling = false;
    }
  }

  private void InitiateJump ()
  {
    if(! _isJumping)
    {
      _isJumping = true;
    }

    ResetWallJumpValues();

    _jumpBufferTimer = 0f;
    _numberOfJumpsUsed ++;
    VerticalVelocity = MoveStats.InitialJumpVelocity;
  }

  private void Jump()
  {
    //Apply Gravity While jumping
    if(_isJumping)
    {
      //check for head bump
      if(_bumpedHead)
      {
        _isFastFalling = true;
      }

     _jumpTime += Time.fixedDeltaTime;

      if (_jumpTime > MoveStats.MaxJumpTime)
      { 
        _isJumping = false;
        _jumpTime = 0f;
      }
    }

    //gravity on ascending
    if(VerticalVelocity >= 0f)
    {
      //apex controls
      _apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, VerticalVelocity);

      if (_isFastFalling)
        {
          VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
        }
      else if (_apexPoint > MoveStats.ApexThreshold)
      {
        if(!_isPastApexThreshold)
        {
          _isPastApexThreshold = true;
          _timePastApexThreshold = 0f;
        }

        if (_isPastApexThreshold) 
        {
          _timePastApexThreshold += Time.fixedDeltaTime;
          if (_timePastApexThreshold < MoveStats.ApexHangTime)
          {
            VerticalVelocity = -0f;
          }
          else
          {
            VerticalVelocity = -0.01f;
          }
        }
        else if (VerticalVelocity < 0f)
        {
          if(! _isFalling)
          {
            _isFalling = true;
          }
        }
        else
        {
          VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
          if(_isPastApexThreshold)
          {
            _isPastApexThreshold = false;
          }
        }
      }

    }   

    //jump cut
    if(_isFastFalling)
    {
      if(_fastFallTime >= MoveStats.TimeForUpwardsCancel)
      {
        VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
      }
      else if(_fastFallTime < MoveStats.TimeForUpwardsCancel)
      {
        VerticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, (_fastFallTime / MoveStats.TimeForUpwardsCancel));
      }

      _fastFallTime += Time.fixedDeltaTime;
    }

  }

  private void BumpedHead()
  {
    Vector2 boxCastOrigin = new Vector2(_feetColl.bounds.center.x, _bodyColl.bounds.max.y);
    Vector2 boxCastSize = new Vector2(_feetColl.bounds.size.x * MoveStats.HeadWidth, MoveStats.HeadDetectionRayLength);

    _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, MoveStats.HeadDetectionRayLength, MoveStats.GroundLayer);
    if(_headHit.collider != null)
    {
      _bumpedHead = true;
    }
    else { _bumpedHead = false; }
  }

 #endregion

#region Wall Slide

  private void WallSlideCheck()
  {
    if (_isTouchingWall && !_isGrounded)
    {
     if(VerticalVelocity < 0f && !_isWallSliding)
      {
        ResetJumpValues();
        ResetWallJumpValues();
        _isWallSlideFalling = false;
        _isWallSliding = true;
      }
    } 
    else if (_isWallSliding && !_isTouchingWall && !_isGrounded && !_isWallSlideFalling)
    {
      _isWallSlideFalling = true;
      StopWallSlide();
    } 

    else
    {
      StopWallSlide();
    }
  }

  private void StopWallSlide()
  {
    if(_isWallSliding)
    {
        _isWallSliding = false;
        _numberOfJumpsUsed = 0;
        _jumpTime = 0f;
    }
  }
  

  private void WallSlide()
  {
    VerticalVelocity = Mathf.Lerp(VerticalVelocity, -MoveStats.WallSlideSpeed, MoveStats.WallSlideDecelerationSpeed * Time.fixedDeltaTime);
  }

#endregion

#region Wall Jump

  private void WallJumpCheck()
  {
    if (ShouldApplyPostWallJumpBuffer())
    {
      _wallJumpPostBufferTimer = MoveStats.WallJumpPostBufferTime;
    }

    //wall jump fast falling
    if(InputManager.JumpWasReleased && !_isWallSliding && !_isTouchingWall && _isWallJumping)
    {
      if (VerticalVelocity > 0f)
      {
        if(_isPastApexThreshold)
        {
          _isPastWallJumpApexThreshold = false;
          _isWallJumpFastFalling = true;
          _wallJumpFastFallTime = MoveStats.TimeForUpwardsCancel; 

          VerticalVelocity = 0f; 
        }
        else
        {
          _isWallJumpFastFalling = true;
          _wallJumpFastFallReleaseSpeed = VerticalVelocity;
        }
      }
    }
    //actual jump with post wall jump buffer time
    if (InputManager.JumpWasPressed && _wallJumpPostBufferTimer > 0f)
    {
      InitiateWallJump();
    }
  }

  private void InitiateWallJump()
  {
    if (!_isWallJumping)
    {
      _isWallJumping = true;
      _useWallJumpMoveStats = true;
    }

    StopWallSlide();
    ResetJumpValues();
    _wallJumpTime = 0f;

    VerticalVelocity = MoveStats.InitiateWallJumpVelocity;

    int dirMulitiplier = 0;
    Vector2 hitPoint = _lastWallHit.collider.ClosestPoint(_bodyColl.bounds.center);

    if (hitPoint.x > transform.position.x)
    {
      dirMulitiplier = -1;
    }
    else { dirMulitiplier = 1; }

    HorizontalVelocity = Mathf.Abs(MoveStats.WallJumpDirection.x) * dirMulitiplier;
  }

  private void WallJump()
  {
    //Apply wall jump velocity
    if (_isWallJumping)
    {
      //time to take over movement controls while wall jumping
      _wallJumpTime += Time.fixedDeltaTime;
      if(_wallJumpTime >= MoveStats.TimeTillJumpApex)
      {
        _useWallJumpMoveStats = false;
      }

      //hit head
      if(_bumpedHead)
      {
        _isWallJumpFastFalling = true;
        _useWallJumpMoveStats = false;
      }

      _wallJumpTimer += Time.fixedDeltaTime;

      if (_wallJumpTimer > MoveStats.MaxWallJumpTime)
      { 
        _isWallJumping = false;
        _wallJumpTimer = 0f;
      }

      //gravity in ascending
      if (VerticalVelocity >= 0f)
      {
        //apex controls
        _wallJumpApexPoint = Mathf.InverseLerp(MoveStats.WallJumpDirection.y, 0f, VerticalVelocity);

        if (_wallJumpApexPoint > MoveStats.ApexThreshold)
        {
          if (!_isPastWallJumpApexThreshold)
          {
            _isPastApexThreshold = true;
            _timePastApexThreshold = 0f;
          }

          if(_isPastApexThreshold)
          {
            _timePastApexThreshold += Time.fixedDeltaTime;
            if (_timePastWallJumpApexThreshold < MoveStats.ApexHangTime)
            {
              VerticalVelocity = 0f;
            }
            else
            {
              VerticalVelocity = -0.01f;
            }
          }
        }

        //gravity on ascending but not past apex
        else if (!_isWallJumpFastFalling)
        {
          VerticalVelocity += MoveStats.WallJumpGravity * Time.fixedDeltaTime;

          if (_isPastWallJumpApexThreshold)
          {
            _isPastWallJumpApexThreshold = false;
          }
        }
      }

      //gravity on descending
      else if (!_isWallJumpFastFalling)
      {
        VerticalVelocity += MoveStats.WallJumpGravity * Time.fixedDeltaTime;
      }

      else if (VerticalVelocity < 0f)
      {
        if(!_isWallJumpFalling)
          _isWallJumpFalling = true;

      }
    }

    //handle wall jump cut time
    if(_isWallJumpFastFalling)
    {
      if(_wallJumpFastFallTime >= MoveStats.TimeForUpwardsCancel)
      {
        VerticalVelocity += MoveStats.WallJumpGravity * MoveStats.WallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
      }
      else if (_wallJumpFastFallTime < MoveStats.TimeForUpwardsCancel)
      {
        VerticalVelocity = Mathf.Lerp(_wallJumpFastFallReleaseSpeed, 0f, (_wallJumpFastFallTime / MoveStats.TimeForUpwardsCancel));
      }

      _wallJumpFastFallTime += Time.fixedDeltaTime;
    }
  }

  private bool ShouldApplyPostWallJumpBuffer()
  {
    if (!_isGrounded && (_isTouchingWall || _isWallSliding))
    {
      return true;
    }
    else { return false; }
  }

  private void ResetWallJumpValues()
  {
    _isWallSliding = false;
    _useWallJumpMoveStats = false;
    _isWallJumping =false;
    _isWallJumpFastFalling = false;
    _isWallJumpFalling = false;
    _isPastWallJumpApexThreshold = false;

    _wallJumpFastFallTime = 0f;
    _wallJumpTime = 0f;
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
      _numberOfJumpsUsed = 0;
      _jumpTime = 0f;
    }
    else { _isGrounded = false; }
  }
  
  private void IsTouchingWall()
  {

    float OriginEndPoint;

      if (_isFacingRight)
      {
        OriginEndPoint = _bodyColl.bounds.max.x;
      }
      else { OriginEndPoint = _bodyColl.bounds.min.x; }

      float adjustedHeight = _bodyColl.bounds.size.y * MoveStats.WallDetectionRayLengthMultiplier;

      Vector2 boxCastOrigin = new Vector2(OriginEndPoint, _bodyColl.bounds.center.y);
      Vector2 boxCastSize = new Vector2(MoveStats.WallDetectionRayLength, adjustedHeight);

      _wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, MoveStats.WallDetectionRayLength, MoveStats.GroundLayer);
      if(_wallHit.collider != null)
      {
        _lastWallHit = _wallHit;
        _isTouchingWall = true;
      }
      else { _isTouchingWall = false; }
}
    
    

  private void CollisionChecks()
  {
    IsGrounded();
    BumpedHead();
    IsTouchingWall();
  }

  #endregion

  #region Timers

  private void CountTimers()
  {
    //jump buffer
    _jumpBufferTimer -= Time.fixedDeltaTime;

    //jump coyote time
    if (!_isGrounded)
    {
      _coyoteTimer -= Time.deltaTime;
    }
    else { _coyoteTimer = MoveStats.JumpCoyoteTime; }
  

  //wall jump buffer
  if (!ShouldApplyPostWallJumpBuffer())
  {
    _wallJumpPostBufferTimer -= Time.deltaTime;
  }
  }
  #endregion
}
