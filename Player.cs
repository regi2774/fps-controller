
using UnityEngine;

public class Player : MonoBehaviour
{
    #region "Configurações Básicas do Jogador"

    [Header("Módulos de Mecânicas")]
    private bool canMove = true;
    private bool canJump = true;

    [Header("Componentes do Jogador")]
    [SerializeField] private CharacterController _player;
    [SerializeField] private MouseLook cam;
    [SerializeField] private Animator _animatorPlayer;
    [SerializeField] private PlayerDash _playerDash;
    //Ações do Jogador

    [Header("Checagem de Camada e Pulo")]
    public bool isGrounded;
    public bool isJumping;
    public Transform checkGround;
    [SerializeField] private LayerMask checkMask;
    [SerializeField] private float checkDistance = 0.5f;
    [SerializeField] private float jumpHeight = 8f; // Aumentado para estilo Quake
    [Header("Checagem de Rampa")]
    public bool onSlope;
    public float maxSlopeAngle = 40f; // Ângulo máximo da rampa
    [SerializeField] RaycastHit slopeHit;

    [Header("Ceiling Check")]
    public Transform ceilingCheck; // Crie um objeto vazio posicionado acima da cabeça do jogador
    public float ceilingCheckDistance = 0.2f;

    [Header("Posição do Jogador")]
    //Informações de Inputs
    public Vector2 input;
    public Vector3 currentVelocity;
    public Transform orientation;
    [SerializeField] public bool isMoving;
    #endregion

    #region "Configurações de Física e Movimento"
    [Header("Física - Estilo Quake")]
    public float gravity = 20f; // Gravidade mais forte
    public float friction = 6f; // Fricção no chão
    public float airAccelerate = 12f; // Aceleração no ar
    public float groundAccelerate = 10f; // Aceleração no chão
    public float maxVelocity = 30f; // Velocidade máxima
    public Vector3 velocity = Vector3.zero;

    [Header("Velocidade de Movimento")]
    [SerializeField] public bool isRunning;
    [SerializeField] public bool isCrouching;
    [SerializeField] public bool isWalking;
    [SerializeField] private bool alwaysRun = true;
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float crouchMultiplier = 1.5f;
    float speedMultiplier;
    [Header("Rocket Jump Settings")]
    [SerializeField] private float rocketJumpMultiplier = 1.5f;
    [SerializeField] private float minJumpHeight = 2f;
    [SerializeField] private float maxJumpHeight = 10f;
    [SerializeField] private float horizontalForceMultiplier = 1.2f; // Controla força horizontal
    [SerializeField] private float minVerticalForce = 3f; // Força vertical mínima garantida
    [SerializeField] private float wallJumpVerticalBoost = 2f; // Bônus ao pular de paredes

    [Header("Wall Climb/Jump Settings")]
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private float wallRunSpeed = 5f;
    [SerializeField] private float wallClimbSpeed= 4f;
    [SerializeField] private float wallJumpForce = 10f;
    [SerializeField] private float wallJumpVerticalForce = 7f;
    [SerializeField] private float wallStickForce;
    [SerializeField] private float wallStickTime = 0.5f;
    [SerializeField] private int maxWallJumps = 3;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private AudioClip wallJumpSound;
    [SerializeField] private AudioClip wallSlideSound;

    private float wallStickTimer;
    private int wallJumpsRemaining;
    private Vector3 wallNormal;
    public bool isWallSliding;
    private bool isTouchingWall;
    private bool wasWallSliding;
    public bool wallRunning = false;
    public bool wallRight;
    public bool wallLeft;
    private bool upwardsRunning;
    private bool downwardsRunning;

    #endregion
    [Header(" Agachar ")]
    [SerializeField] private float originalHeight;
    [SerializeField] private float crouchHeight;
    [SerializeField] private float speed;

    [Header("Configuração de Sons")]
    [SerializeField] private float footstepCheckDistance = 3f;
    [SerializeField] private float _baseStepSpeed = 0.5f;
    [SerializeField] private float _runningStepMultiplier = 0.6f;
    [SerializeField] private float _crouchingStepMultiplier = 0.6f;
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] concreteClips = default;
    [SerializeField] private AudioClip[] defaultClips = default;
    [Header("Sons de Pulo e Aterrisagem")]
    public float minFallHeightForLandingSound = 3f; // Altura mínima para tocar o som de queda
    private float fallStartHeight; // Armazena a altura quando começou a cair
    public AudioClip jumpClip = default;
    [SerializeField] private AudioClip landingClip = default;
    private float footstepTimer = 0;

    private float GetCurrentOffset => isCrouching ? _baseStepSpeed * _crouchingStepMultiplier : isRunning ? _baseStepSpeed * _runningStepMultiplier : _baseStepSpeed;
    
     
    private void OnEnable()
    {
        EventHandler.OnRocketJump += HandleRocketJump;
    }

    private void OnDisable()
    {
        EventHandler.OnRocketJump -= HandleRocketJump;
    }
    void Awake()
    {
        originalHeight = _player.height;
        Cursor.lockState = CursorLockMode.Locked;    
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = isWallSliding ? Color.green : Color.red;

        Gizmos.DrawWireSphere(wallCheck.position, 0.1f);
        Gizmos.DrawLine(wallCheck.position, wallCheck.position + transform.forward * wallCheckDistance);

        Gizmos.DrawWireSphere(wallCheck.position + (transform.right - wallCheck.forward * 0.5f), 0.1f);
        Gizmos.DrawLine(wallCheck.position + (transform.right - wallCheck.forward * 0.5f), wallCheck.position + (transform.right - wallCheck.forward * 0.5f) + transform.right * wallCheckDistance);

        Gizmos.DrawWireSphere(wallCheck.position + (-transform.right - wallCheck.forward * 0.5f), 0.1f);
        Gizmos.DrawLine(wallCheck.position + (-transform.right - wallCheck.forward * 0.5f), wallCheck.position + (-transform.right - wallCheck.forward * 0.5f) - transform.right * wallCheckDistance);

        Gizmos.DrawWireSphere(wallCheck.position + -wallCheck.forward, 0.1f);
        Gizmos.DrawLine(wallCheck.position, wallCheck.position + -wallCheck.forward - transform.forward * wallCheckDistance);
        
        Gizmos.color = isGrounded ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(checkGround.position, checkDistance);
    
    }

    void Update()
    {       
        HandleGravity();
        CheckWall();

        if (canMove)
        {
            HandleMovement();
            HandleFootsteps();
        }

        HandleCrouch();
        ApplyMovement();

        
    }
    void FixedUpdate()
    {
        if(wallRunning){
            WallRunningMovement();
        }
    }

    void HandleGravity()
    {
        bool wasGrounded = isGrounded;

        isGrounded = CheckGround();

        // Monitora a altura de queda quando não está no chão
        if (!isGrounded && !isJumping && velocity.y < 0)
        {
            // Registra a altura máxima alcançada durante a queda
            if (transform.position.y > fallStartHeight)
            {
                fallStartHeight = transform.position.y;
            }
        }

        // Reset vertical velocity when grounded
        if (isGrounded)
        {
            wallJumpsRemaining = maxWallJumps;

            if(velocity.y < 0){

                velocity.y = -2f;
                isJumping = false;
            }

            if (!wasGrounded)
            {
                // Calcula a altura da queda
                float fallHeight = fallStartHeight - transform.position.y;
                
                // Toca o som apenas se a queda foi significativa
                if (fallHeight >= minFallHeightForLandingSound)
                {
                    EventHandler.LandingEvent();
                    AudioManager.Instance._playerGruntAudioSource.PlayOneShot(landingClip);
                    
                    // Opcional: tocar um som diferente baseado na altura
                    PlayAppropriateLandingSound(fallHeight);
                }
                
                // Toca sempre o som de passos (ou condicional)
                AudioManager.Instance._playerFootstepsAudioSource.PlayOneShot(defaultClips[Random.Range(0, defaultClips.Length)]);
                
                // Reseta a altura de queda
                fallStartHeight = transform.position.y;
            }
        }

        // Verificação do teto
        bool hitCeiling = Physics.CheckSphere(ceilingCheck.position, ceilingCheckDistance, checkMask);
        
        if (hitCeiling && velocity.y > 0)
        {
            velocity.y = -2f;
            isJumping = false;
        }

        // Handle wall sliding
        if (isWallSliding)
        {
            AudioManager.Instance.HandleWallSliding(wallSlideSound);
            velocity.y = -wallSlideSpeed;
            velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 5f);
            velocity.z = Mathf.Lerp(velocity.z, 0, Time.deltaTime * 5f);
        }
       

        // Apply normal gravity if not wall sliding
        else if (!isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        // State 1 - Wallrunning
        if((wallLeft || wallRight) && input.y > 0 && AboveGround())
        {
            if (!wallRunning)

                StartWallRun();
        }
        if ((!wallLeft && !wallRight) || input.y <= 0 || !AboveGround() || PlayerInputManager.Instance.JumpPressed)
            {
                if (wallRunning)
                    StopWallRun();
            }

        // Handle jumping
        if (canJump)
        {
            if (PlayerInputManager.Instance.JumpPressed && isGrounded)
            {
                PerformJump();
                fallStartHeight = transform.position.y; // Reseta ao pular
            }
            else if (PlayerInputManager.Instance.JumpPressed && (isWallSliding || wallStickTimer > 0) && wallJumpsRemaining > 0)
            {
                PerformWallJump();
                fallStartHeight = transform.position.y; // Reseta ao wall jump
            }
        }
    }
    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, checkMask);
    }
    bool CheckGround()
    {
        // Checagem padrão com esfera
        bool sphereCheck = Physics.CheckSphere(checkGround.position, checkDistance, checkMask);

        // Combina as checagens
        return sphereCheck;
    }

    private void PlayAppropriateLandingSound(float fallHeight)
    {
        if (fallHeight > minFallHeightForLandingSound * 2f)
        {
            // Toca um som mais forte para quedas muito altas
            AudioManager.Instance._playerGruntAudioSource.PlayOneShot(landingClip);
        }
        else
        {
            // Toca o som normal de aterrissagem
            AudioManager.Instance._playerGruntAudioSource.PlayOneShot(landingClip);
        }
    }
    private void PerformJump()
    {
        AudioManager.Instance._playerGruntAudioSource.PlayOneShot(jumpClip);
        isJumping = true;
        velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
    }
    private void PerformWallJump()
    {
        AudioManager.Instance._playerGruntAudioSource.PlayOneShot(wallJumpSound);
        
        // Calculate jump direction away from wall
        Vector3 jumpDirection = wallNormal * wallJumpForce;
        jumpDirection.y = wallJumpVerticalForce;
        
        velocity = jumpDirection;
        wallJumpsRemaining--;
        wallStickTimer = 0;
        isJumping = true;
    }
    private void CheckWall()
    {
        wasWallSliding = isWallSliding;
        isTouchingWall = false;

        RaycastHit hit;

        // Verifica parede à frente
        if (Physics.Raycast(transform.position, transform.forward, out hit, wallCheckDistance, wallLayer) &&
        PlayerInputManager.Instance.Vertical > 0)
        {
            isTouchingWall = true;
            wallNormal = hit.normal;
        }
        // Verifica parede à direita
        else if (Physics.Raycast(transform.position, transform.right, out hit, wallCheckDistance, wallLayer) &&
        PlayerInputManager.Instance.Horizontal > 0)
        {
            wallRight = true;
            isTouchingWall = true;
            wallNormal = hit.normal;
        }
        // Verifica parede à esquerda
        else if (Physics.Raycast(transform.position, -transform.right, out hit, wallCheckDistance, wallLayer) && 
        PlayerInputManager.Instance.Horizontal < 0)
        {
            wallLeft = true;
            isTouchingWall = true;
            wallNormal = hit.normal;
        }
        else if (Physics.Raycast(transform.position, -transform.forward, out hit, wallCheckDistance, wallLayer) &&
        PlayerInputManager.Instance.Vertical < 0)
        {
            isTouchingWall = true;
            wallNormal = hit.normal;
        }

        if (isTouchingWall && !isGrounded && velocity.y < 0)
        {
            wallStickTimer = wallStickTime;
            isWallSliding = true;
            
            if (!wasWallSliding)
            {
                AudioManager.Instance.HandleWallSliding(wallSlideSound);
            }
        }
        else
        {
            wallLeft = false;
            wallRight = false;
            wallStickTimer -= Time.deltaTime;
            isWallSliding = false;
            AudioManager.Instance.HandleStopWallSliding();
        }
    }
    void StartWallRun()
    {
        wallRunning = true;
    } 
    void StopWallRun()
    {
        wallRunning = false;
    } 
    private void WallRunningMovement()
    {
        // Reset vertical velocity while wall running
        velocity = new Vector3(velocity.x, 0f, velocity.z);
        float verticalInput = PlayerInputManager.Instance.Vertical;
        float horizontalInput = PlayerInputManager.Instance.Horizontal;

        //Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        // Determine the correct wall forward direction
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        // Calculate movement direction
        Vector3 moveDirection = verticalInput * wallRunSpeed * wallForward;

        // Apply upwards/downwards movement
        if (upwardsRunning)
            moveDirection.y = wallClimbSpeed;
        if (downwardsRunning)
            moveDirection.y = -wallClimbSpeed;

        // Apply push to wall force
        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
            moveDirection += -wallNormal * 5f;

        if (Vector3.Dot(velocity, wallNormal) < 0.1f)
        {
            moveDirection += -wallNormal * wallStickForce;
        }
        // Apply the movement
        velocity = moveDirection;
    }
       
    void HandleMovement()
    {
        isWalking = isMoving && !PlayerInputManager.Instance.SprintHeld;
        isRunning = isMoving && PlayerInputManager.Instance.SprintHeld || alwaysRun == true;
        isCrouching = PlayerInputManager.Instance.CrouchPressed;
        input = new Vector2(PlayerInputManager.Instance.Horizontal, PlayerInputManager.Instance.Vertical).normalized;

        isMoving = input.magnitude > 0;

        // Adicione esta verificação no início do método
        if (_playerDash.isSliding)
        {
            isMoving = false;
            input = Vector2.zero;
            velocity = new Vector3(0, velocity.y, 0); // Reseta a velocidade do jogador
            return; // Sai do método completamente
        }

        if(!alwaysRun){

            speedMultiplier = isCrouching ? crouchMultiplier : isRunning ? sprintMultiplier : 1f;
        }
        else{

            speedMultiplier = isCrouching ? crouchMultiplier : sprintMultiplier;
        }

        float currentSpeed = walkSpeed * speedMultiplier;

        Vector3 wishDir = transform.TransformDirection(new Vector3(input.x, 0, input.y));
        wishDir.Normalize();

        if (isGrounded)
        {
            // Aplica fricção no chão

            float speed = velocity.magnitude;
            if (speed > 0)
            {
                float drop = speed * friction * Time.deltaTime;
                velocity *= Mathf.Max(speed - drop, 0) / speed;
            }

            Accelerate(wishDir, groundAccelerate, currentSpeed);
        }
        else
        {
            // Movimento aéreo (estilo Quake)
            Accelerate(wishDir, airAccelerate, currentSpeed);
        }

       
    }
    private void Accelerate(Vector3 wishDir, float accelerate, float maxSpeed)
    {
        float currentSpeed = Vector3.Dot(velocity, wishDir);
        float addSpeed = maxSpeed - currentSpeed;
        
        if (addSpeed <= 0) return;
            
        float accelSpeed = accelerate * maxSpeed * Time.deltaTime;

        accelSpeed = Mathf.Min(accelSpeed, addSpeed);
        
        velocity += wishDir * accelSpeed;
        
        // Limita velocidade máxima
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }
    }
    void ApplyMovement()
    {
        _player.Move(velocity * Time.deltaTime);  
    }
    void HandleFootsteps()
    {
        if (isGrounded == false && wallRunning != true || _playerDash.isSliding) return; // Adicione a verificação do slide
        if (isMoving == false) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if(SlowMo.inSlowMotion != true){
                AudioManager.Instance._playerFootstepsAudioSource.pitch = Random.Range(0.95f, 1f);
            }
            if(wallRunning){
                AudioManager.Instance._playerFootstepsAudioSource.PlayOneShot(defaultClips[Random.Range(0, defaultClips.Length)]);
                footstepTimer = GetCurrentOffset;
            }
            else{
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, footstepCheckDistance))
                {
                    switch (hit.collider.tag)
                    {
                        case "Footsteps/Metal":
                            AudioManager.Instance._playerFootstepsAudioSource.PlayOneShot(metalClips[Random.Range(0, metalClips.Length)]);
                            break;
                        case "Footsteps/Concrete":
                            AudioManager.Instance._playerFootstepsAudioSource.PlayOneShot(concreteClips[Random.Range(0, concreteClips.Length)]);
                            break;
                        default:
                            AudioManager.Instance._playerFootstepsAudioSource.PlayOneShot(defaultClips[Random.Range(0, defaultClips.Length)]);
                            break;
                    }

                    footstepTimer = GetCurrentOffset;
                }
            }

        }
    }
    private void HandleRocketJump(Vector3 explosionPoint, float radius, float force)
    {
        Vector3 playerPosition = transform.position;
        float distance = Vector3.Distance(explosionPoint, playerPosition);
        
        // Calcula a força baseada na distância da explosão
        float distanceFactor = Mathf.Clamp01(1 - (distance / radius));
        float calculatedForce = Mathf.Lerp(minJumpHeight, maxJumpHeight, distanceFactor) * rocketJumpMultiplier;
        
        // Calcula a direção do impulso (oposta ao ponto de impacto)
        Vector3 direction = (playerPosition - explosionPoint).normalized;
        
        // Suaviza a componente vertical para evitar impulsos muito bruscos para baixo
        direction.y = Mathf.Clamp(direction.y + 0.5f, 0.2f, 1f);
        direction.Normalize();
        
        // Aplica o impulso considerando CharacterController
        velocity.x = direction.x * calculatedForce * horizontalForceMultiplier;
        velocity.z = direction.z * calculatedForce * horizontalForceMultiplier;
        velocity.y = direction.y * calculatedForce;
        
        // Limita a força vertical mínima para garantir que sempre dê algum impulso para cima
        velocity.y = Mathf.Max(velocity.y, minVerticalForce);
        // Se o impacto foi basicamente na frente do jogador (parede)
        if (Vector3.Dot(direction, transform.forward) < -0.7f)
        {
            // Adiciona um bônus vertical extra para wall jumps
            direction.y += wallJumpVerticalBoost;
            direction.Normalize();
        }
    }
    private void HandleCrouch()
    {
       if(isGrounded == false){ return;}

        _player.height =
        PlayerInputManager.Instance.CrouchPressed ? 

        (isCrouching = true,
        velocity.y = -2f,
        Mathf.Clamp(Mathf.Lerp(_player.height, crouchHeight,  speed * Time.deltaTime),  0.5f, 2.5f)).Item3:
        
        (isCrouching = false,
        Mathf.Clamp(Mathf.Lerp(_player.height, originalHeight,  speed * Time.deltaTime), 0.5f, 2.5f)).Item2;
        cam.HandleCrouch();
    }  
    
}
