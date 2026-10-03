using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MoskowGameJam.NPC
{
    [RequireComponent(typeof(Animator))]
    public class NPCMovement : MonoBehaviour
    {
        public enum NPCMode
        {
            IdleOnly,          // Berdiri diam di tempat (contoh: Penjaga Stall)
            PatrolWaypoints,   // Berjalan berurutan melewati waypoint tertentu
            RandomWander,      // Berjalan bebas di sekitar area
            MarketVisitor      // Pengunjung Pasar: Berkeliling bebas & mampir ke stall makanan secara berkala
        }

        [Header("Mode Perilaku")]
        [Tooltip("Mode pergerakan NPC")]
        [SerializeField] private NPCMode mode = NPCMode.MarketVisitor;

        [Header("Kecepatan & Rotasi")]
        [Tooltip("Kecepatan jalan NPC")]
        [SerializeField] private float walkSpeed = 1.6f;

        [Tooltip("Kecepatan rotasi NPC saat berbelok")]
        [SerializeField] private float rotationSpeed = 6.0f;

        [Header("Pengaturan Waktu Diam (Idle)")]
        [Tooltip("Waktu minimal diam saat tiba di titik wander/waypoint (detik)")]
        [SerializeField] private float minIdleTime = 1.5f;

        [Tooltip("Waktu maksimal diam (detik)")]
        [SerializeField] private float maxIdleTime = 4.0f;

        [Header("Mode Market Visitor (Pengunjung Pasar)")]
        [Tooltip("Daftar titik depan stall makanan yang akan dikunjungi NPC secara berkala")]
        [SerializeField] private Transform[] stallWaypoints;

        [Tooltip("Berapa kali jalan-jalan acak sebelum mampir ke salah satu stall makanan")]
        [SerializeField] private int wandersBeforeVisitingStall = 2;

        [Tooltip("Waktu minimal diam/melihat-lihat saat berada di depan stall makanan (detik)")]
        [SerializeField] private float minStallBrowseTime = 4.0f;

        [Tooltip("Waktu maksimal diam di depan stall makanan (detik)")]
        [SerializeField] private float maxStallBrowseTime = 8.0f;

        [Header("Patroli Waypoints (Mode PatrolWaypoints)")]
        [Tooltip("Daftar titik waypoint rute patroli")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointReachedDistance = 0.6f;

        [Header("Jalan Bebas (Area Wander)")]
        [Tooltip("Radius area jalan bebas dari posisi awal")]
        [SerializeField] private float wanderRadius = 12.0f;

        [Header("Anti Tabrakan & Penghindaran Bangunan / Rintangan")]
        [Tooltip("Jarak deteksi rintangan / dinding bangunan di depan")]
        [SerializeField] private float obstacleCheckDistance = 2.0f;

        [Tooltip("Jarak pemisah antar-NPC agar tidak saling bertabrakan atau beradu di tengah jalan")]
        [SerializeField] private float npcSeparationDistance = 1.8f;

        [Tooltip("Radius ukuran tubuh NPC untuk deteksi tabrakan fisik")]
        [SerializeField] private float characterBodyRadius = 0.35f;

        [Tooltip("Layer mask untuk rintangan fisik (dinding bangunan, meja stall, props, pagar)")]
        [SerializeField] private LayerMask obstacleLayer = ~0;

        [Header("Parameter Animator")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string isMovingParameter = "isMoving";

        [Header("Suara Langkah (Opsional)")]
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private AudioClip landSound;
        [Range(0f, 1f)][SerializeField] private float audioVolume = 0.5f;

        // Registry statis seluruh NPC aktif untuk penghindaran tabrakan antar-NPC berkecepatan tinggi tanpa GC Alloc
        private static readonly List<NPCMovement> allActiveNPCs = new List<NPCMovement>();

        // Komponen internal
        private Animator animator;
        private NavMeshAgent navAgent;
        private CharacterController characterController;

        // Status pergerakan
        private Vector3 startPosition;
        private Vector3 currentDestination;
        private int currentWaypointIndex = 0;
        private bool isIdle = false;
        private float idleTimer = 0f;
        private float currentSpeed = 0f;
        private int speedHash;
        private int isMovingHash;

        // Status khusus Market Visitor
        private int wanderCounter = 0;
        private bool isVisitingStall = false;
        private Transform currentStallTarget = null;

        // Status Anti-Stuck (mencegah macet jika terhalang)
        private Vector3 lastCheckedPosition;
        private float stuckCheckTimer = 0f;
        private float wallContactTimer = 0f;

        // Status Interaksi Player
        private bool isInteracting = false;
        private Transform interactor;

        public bool IsInteracting => isInteracting;
        public NPCMode CurrentMode => mode;
        public Transform[] Waypoints => waypoints;
        public Transform[] StallWaypoints => stallWaypoints;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            navAgent = GetComponent<NavMeshAgent>();
            characterController = GetComponent<CharacterController>();

            // Konfigurasi CharacterController yang presisi jika terpasang
            if (characterController != null)
            {
                if (characterController.radius < 0.25f) characterController.radius = characterBodyRadius;
                if (characterController.height < 1.5f) characterController.height = 1.8f;
                if (characterController.center == Vector3.zero) characterController.center = new Vector3(0f, 0.9f, 0f);
                characterController.skinWidth = 0.05f;
                characterController.stepOffset = 0.35f;
                characterController.slopeLimit = 45f;
            }

            speedHash = Animator.StringToHash(speedParameter);
            isMovingHash = Animator.StringToHash(isMovingParameter);
            startPosition = transform.position;
            lastCheckedPosition = transform.position;
        }

        private void OnEnable()
        {
            if (!allActiveNPCs.Contains(this))
            {
                allActiveNPCs.Add(this);
            }
        }

        private void OnDisable()
        {
            allActiveNPCs.Remove(this);
        }

        private void Start()
        {
            // Konfigurasi NavMeshAgent jika ada dan aktif
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.speed = walkSpeed;
                navAgent.angularSpeed = rotationSpeed * 60f;
                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                navAgent.avoidancePriority = Random.Range(20, 80);
                navAgent.radius = characterBodyRadius;
            }

            // Inisialisasi awal dengan jeda santai acak agar semua NPC tidak bergerak bersamaan
            isIdle = true;
            idleTimer = Random.Range(0.5f, 2.5f);
            wanderCounter = Random.Range(0, Mathf.Max(1, wandersBeforeVisitingStall));
        }

        private void Update()
        {
            if (isInteracting)
            {
                UpdateInteracting();
                UpdateAnimator();
                return;
            }

            switch (mode)
            {
                case NPCMode.IdleOnly:
                    UpdateIdle();
                    break;

                case NPCMode.PatrolWaypoints:
                    UpdatePatrol();
                    break;

                case NPCMode.RandomWander:
                    UpdateWander();
                    break;

                case NPCMode.MarketVisitor:
                    UpdateMarketVisitor();
                    break;
            }

            UpdateAnimator();
        }

        private void UpdateInteracting()
        {
            StopMovement();
            currentSpeed = 0f;

            if (interactor != null)
            {
                Vector3 lookDir = interactor.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
            }
        }

        private void UpdateIdle()
        {
            currentSpeed = 0f;
        }

        private void UpdatePatrol()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                currentSpeed = 0f;
                return;
            }

            if (isIdle)
            {
                currentSpeed = 0f;
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    isIdle = false;
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                    if (waypoints[currentWaypointIndex] != null)
                    {
                        currentDestination = waypoints[currentWaypointIndex].position;
                    }
                }
                return;
            }

            if (waypoints[currentWaypointIndex] == null)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            currentDestination = waypoints[currentWaypointIndex].position;
            float distSq = GetFlatDistanceSqr(transform.position, currentDestination);

            if (distSq <= waypointReachedDistance * waypointReachedDistance)
            {
                isIdle = true;
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                StopMovement();
            }
            else
            {
                MoveTowards(currentDestination);
                CheckIfStuck();
            }
        }

        private void UpdateWander()
        {
            if (isIdle)
            {
                currentSpeed = 0f;
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    isIdle = false;
                    currentDestination = GetValidRandomPoint(startPosition, wanderRadius);
                }
                return;
            }

            float distSq = GetFlatDistanceSqr(transform.position, currentDestination);

            if (distSq <= waypointReachedDistance * waypointReachedDistance)
            {
                isIdle = true;
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                StopMovement();
            }
            else
            {
                MoveTowards(currentDestination);
                CheckIfStuck();
            }
        }

        /// <summary>
        /// Logika Pengunjung Pasar: Berkeliling bebas & mampir ke stand makanan seperti pembeli
        /// </summary>
        private void UpdateMarketVisitor()
        {
            if (isIdle)
            {
                currentSpeed = 0f;
                idleTimer -= Time.deltaTime;

                // Jika sedang diam di depan stall, hadap ke arah stall / penjaga stall
                if (isVisitingStall && currentStallTarget != null)
                {
                    Vector3 lookDir = (currentStallTarget.position - transform.position);
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion rot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * (rotationSpeed * 0.5f));
                    }
                }

                if (idleTimer <= 0f)
                {
                    isIdle = false;
                    PickNextMarketVisitorDestination();
                }
                return;
            }

            float distSq = GetFlatDistanceSqr(transform.position, currentDestination);

            if (distSq <= waypointReachedDistance * waypointReachedDistance)
            {
                isIdle = true;

                if (isVisitingStall)
                {
                    // Diam lebih lama di depan stall (layaknya pembeli yang memesan / melihat makanan)
                    idleTimer = Random.Range(minStallBrowseTime, maxStallBrowseTime);
                }
                else
                {
                    // Diam sejenak saat jalan-jalan santai di pasar
                    idleTimer = Random.Range(minIdleTime, maxIdleTime);
                }

                StopMovement();
            }
            else
            {
                MoveTowards(currentDestination);
                CheckIfStuck();
            }
        }

        private void PickNextMarketVisitorDestination()
        {
            // Jika ada titik stall dan counter wander sudah tercapai -> saatnya mampir ke stall makanan!
            if (stallWaypoints != null && stallWaypoints.Length > 0 && wanderCounter >= wandersBeforeVisitingStall)
            {
                wanderCounter = 0;
                isVisitingStall = true;

                // Pilih salah satu stall secara acak
                int stallIndex = Random.Range(0, stallWaypoints.Length);
                if (stallWaypoints[stallIndex] != null)
                {
                    currentStallTarget = stallWaypoints[stallIndex];
                    currentDestination = stallWaypoints[stallIndex].position;
                    return;
                }
            }

            // Jika belum mampir ke stall atau baru selesai dari stall -> jalan-jalan bebas berkeliling pasar
            isVisitingStall = false;
            currentStallTarget = null;
            wanderCounter++;
            currentDestination = GetValidRandomPoint(startPosition, wanderRadius);
        }

        /// <summary>
        /// Penggerak utama dengan sistem penghindaran dinding bangunan dan adu-banteng antar NPC
        /// </summary>
        private void MoveTowards(Vector3 destination)
        {
            // 1. Jika ada NavMeshAgent yang aktif
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(destination);
                currentSpeed = navAgent.velocity.magnitude;
                return;
            }

            // 2. Gerakan manual dengan CharacterController atau Transform
            Vector3 desiredDirection = (destination - transform.position);
            desiredDirection.y = 0;

            if (desiredDirection.sqrMagnitude > 0.01f)
            {
                desiredDirection.Normalize();

                // Hitung arah penghindaran samping (Lateral Steering) dan faktor kecepatan (Yielding)
                float speedMultiplier = 1.0f;
                bool isWallDirectlyInFront = false;
                Vector3 avoidanceSteering = CalculateSmartAvoidance(desiredDirection, out speedMultiplier, out isWallDirectlyInFront);

                // Jika terbentur langsung dinding di depan dalam jarak sangat dekat, putar cepat ke arah samping
                if (isWallDirectlyInFront)
                {
                    wallContactTimer += Time.deltaTime;
                    if (wallContactTimer > 0.8f)
                    {
                        // Sudah menempel dinding lebih dari 0.8 detik -> langsung cari tujuan baru
                        wallContactTimer = 0f;
                        OnEncounteredBlockingObstacle();
                        return;
                    }
                }
                else
                {
                    wallContactTimer = 0f;
                }

                // Gabungkan arah tujuan dengan arah penghindaran lateral
                Vector3 finalMoveDir = (desiredDirection + avoidanceSteering).normalized;

                // Putar badan NPC dengan halus menghadap arah jalan yang sudah menghindari rintangan
                Quaternion targetRot = Quaternion.LookRotation(finalMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

                // Kecepatan yang disesuaikan
                float effectiveSpeed = walkSpeed * speedMultiplier;
                Vector3 moveDelta = transform.forward * (effectiveSpeed * Time.deltaTime);

                if (characterController != null && characterController.enabled)
                {
                    moveDelta.y = Physics.gravity.y * Time.deltaTime;
                    characterController.Move(moveDelta);
                }
                else
                {
                    transform.position += moveDelta;
                }

                currentSpeed = effectiveSpeed;
            }
            else
            {
                currentSpeed = 0f;
            }
        }

        /// <summary>
        /// Algoritma Penghindaran Pintar (Bangunan / Props & Antar-Sesama NPC):
        /// 1. SphereCast & Whisker Raycast: Mendeteksi dinding bangunan dan mengarahkan NPC meluncur mulus sejajar dinding (wall sliding).
        /// 2. Anti Adu-Banteng: NPC saling menghindar ke kanan masing-masing dan mengalah jika terlalu dekat.
        /// </summary>
        private Vector3 CalculateSmartAvoidance(Vector3 moveDir, out float speedMultiplier, out bool isWallDirectlyInFront)
        {
            Vector3 steering = Vector3.zero;
            speedMultiplier = 1.0f;
            isWallDirectlyInFront = false;

            // --- A. Penghindaran Dinding Bangunan, Meja Stall & Rintangan Fisik ---
            Vector3 castOrigin = transform.position + Vector3.up * 0.85f;
            float castRadius = Mathf.Max(0.2f, characterBodyRadius);

            // 1. SphereCast tebal ke arah depan (mendeteksi rintangan sebelum tubuh menabrak)
            if (Physics.SphereCast(castOrigin, castRadius, transform.forward, out RaycastHit hitFront, obstacleCheckDistance, obstacleLayer))
            {
                if (hitFront.transform != transform && !hitFront.transform.IsChildOf(transform))
                {
                    Vector3 wallNormal = hitFront.normal;
                    wallNormal.y = 0;
                    wallNormal.Normalize();

                    // Vektor tangen (sejajar permukaan dinding)
                    Vector3 wallTangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
                    if (Vector3.Dot(wallTangent, moveDir) < 0f)
                    {
                        wallTangent = -wallTangent;
                    }

                    // Arahkan pergerakan meluncur sejajar permukaan dinding (Wall Sliding Steering)
                    float urgency = Mathf.Clamp01(1.0f - (hitFront.distance / obstacleCheckDistance));
                    steering += (wallTangent * 3.5f + wallNormal * 2.0f) * urgency;

                    if (hitFront.distance < 0.65f)
                    {
                        isWallDirectlyInFront = true;
                        speedMultiplier = 0.5f;
                    }
                }
            }

            // 2. Sensor Whisker Kiri & Kanan (Sudut 35 derajat) untuk menjaga jarak aman dari sudut bangunan
            Vector3 leftRayDir = Quaternion.Euler(0, -35f, 0) * transform.forward;
            Vector3 rightRayDir = Quaternion.Euler(0, 35f, 0) * transform.forward;

            if (Physics.Raycast(castOrigin, leftRayDir, out RaycastHit hitLeft, obstacleCheckDistance * 0.8f, obstacleLayer))
            {
                if (hitLeft.transform != transform && !hitLeft.transform.IsChildOf(transform))
                {
                    steering += transform.right * 2.5f;
                }
            }

            if (Physics.Raycast(castOrigin, rightRayDir, out RaycastHit hitRight, obstacleCheckDistance * 0.8f, obstacleLayer))
            {
                if (hitRight.transform != transform && !hitRight.transform.IsChildOf(transform))
                {
                    steering += -transform.right * 2.5f;
                }
            }

            // --- B. Penghindaran Antar-Sesama NPC (Pedestrian Mutual Avoidance) ---
            for (int i = 0; i < allActiveNPCs.Count; i++)
            {
                NPCMovement other = allActiveNPCs[i];
                if (other == null || other == this || !other.gameObject.activeInHierarchy) continue;

                Vector3 toOther = other.transform.position - transform.position;
                toOther.y = 0;
                float dist = toOther.magnitude;

                if (dist < npcSeparationDistance && dist > 0.05f)
                {
                    Vector3 toOtherNorm = toOther / dist;
                    float forwardDot = Vector3.Dot(transform.forward, toOtherNorm);

                    if (forwardDot > 0.15f)
                    {
                        float facingDot = Vector3.Dot(transform.forward, other.transform.forward);
                        float rightDot = Vector3.Dot(transform.right, toOtherNorm);

                        Vector3 sideDir;
                        if (Mathf.Abs(rightDot) < 0.2f)
                        {
                            // Tepat berhadapan lurus -> keduanya belok ke KANAN masing-masing
                            sideDir = transform.right;
                        }
                        else if (rightDot > 0f)
                        {
                            sideDir = -transform.right;
                        }
                        else
                        {
                            sideDir = transform.right;
                        }

                        float urgency = Mathf.Clamp01(1.0f - (dist / npcSeparationDistance));
                        steering += sideDir * (urgency * 3.2f);

                        // Yielding / Melambat jika berpapasan sangat dekat
                        if (dist < 1.0f && facingDot < -0.2f)
                        {
                            if (GetHashCode() < other.GetHashCode())
                            {
                                speedMultiplier = Mathf.Min(speedMultiplier, 0.35f);
                            }
                        }
                    }
                    else
                    {
                        Vector3 pushAway = -toOtherNorm;
                        float urgency = Mathf.Clamp01(1.0f - (dist / npcSeparationDistance));
                        steering += pushAway * (urgency * 0.8f);
                    }
                }
            }

            return steering;
        }

        private void OnEncounteredBlockingObstacle()
        {
            if (mode == NPCMode.MarketVisitor)
            {
                PickNextMarketVisitorDestination();
            }
            else if (mode == NPCMode.RandomWander)
            {
                currentDestination = GetValidRandomPoint(startPosition, wanderRadius);
            }
            else if (mode == NPCMode.PatrolWaypoints && waypoints != null && waypoints.Length > 0)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }

        /// <summary>
        /// Mengambil titik acak yang valid di ruang terbuka (tidak menembus dinding bangunan)
        /// </summary>
        private Vector3 GetValidRandomPoint(Vector3 center, float radius)
        {
            Vector3 origin = transform.position;

            // Coba sampai 6 kali mencari titik terbuka yang tidak dihalangi dinding bangunan
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(randomCircle.x, 0, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    candidate = hit.position;
                }

                // Cek Linecast apakah ada dinding bangunan langsung di antara posisi sekarang dan target
                Vector3 checkStart = origin + Vector3.up * 0.8f;
                Vector3 checkEnd = candidate + Vector3.up * 0.8f;

                if (!Physics.Linecast(checkStart, checkEnd, out RaycastHit wallHit, obstacleLayer))
                {
                    // Jalur terbuka bersih tanpa terhalang dinding!
                    return candidate;
                }
                else if (wallHit.distance > 3.0f)
                {
                    // Jika terhalang tapi jaraknya masih jauh, ambil titik sebelum dinding
                    return wallHit.point - (checkEnd - checkStart).normalized * 1.5f;
                }
            }

            // Fallback: ambil titik acak terdekat
            Vector2 fallbackCircle = Random.insideUnitCircle * (radius * 0.5f);
            return center + new Vector3(fallbackCircle.x, 0, fallbackCircle.y);
        }

        /// <summary>
        /// Deteksi otomatis jika NPC terhalang atau macet di sudut, agar segera mencari jalur baru secara mulus
        /// </summary>
        private void CheckIfStuck()
        {
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= 2.0f)
            {
                stuckCheckTimer = 0f;
                float movedDistSq = (transform.position - lastCheckedPosition).sqrMagnitude;

                // Jika selama 2 detik bergerak kurang dari 0.15 meter saat sedang tidak idle -> Berarti macet
                if (movedDistSq < 0.0225f && !isIdle && !isInteracting)
                {
                    OnEncounteredBlockingObstacle();
                }

                lastCheckedPosition = transform.position;
            }
        }

        private void StopMovement()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }
            currentSpeed = 0f;
        }

        private float GetFlatDistanceSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            animator.SetFloat(speedHash, currentSpeed, 0.15f, Time.deltaTime);
            animator.SetBool(isMovingHash, currentSpeed > 0.1f);
        }

        // --- Kontrol Publik ---
        public void SetMode(NPCMode newMode)
        {
            mode = newMode;
            isIdle = false;
            idleTimer = 0f;
        }

        public void SetWaypoints(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
            currentWaypointIndex = 0;
            isIdle = false;
            idleTimer = 0f;
        }

        public void SetStallWaypoints(Transform[] newStallWaypoints)
        {
            stallWaypoints = newStallWaypoints;
        }

        public void PauseForInteraction(Transform player)
        {
            isInteracting = true;
            interactor = player;
            StopMovement();
        }

        public void ResumeFromInteraction()
        {
            isInteracting = false;
            interactor = null;
            isIdle = true;
            idleTimer = Random.Range(1.0f, 2.5f);
        }

        // --- Event Receiver Animasi Footsteps (Starter Assets Animation Events) ---
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent != null && animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (footstepSounds != null && footstepSounds.Length > 0)
                {
                    int index = Random.Range(0, footstepSounds.Length);
                    AudioSource.PlayClipAtPoint(footstepSounds[index], transform.position, audioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent != null && animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (landSound != null)
                {
                    AudioSource.PlayClipAtPoint(landSound, transform.position, audioVolume);
                }
            }
        }

        // --- Gizmos di Scene View ---
        private void OnDrawGizmos()
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = new Color(0f, 0.7f, 1f, 0.6f);
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null) continue;
                    Transform next = waypoints[(i + 1) % waypoints.Length];
                    if (next != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position + Vector3.up * 0.15f, next.position + Vector3.up * 0.15f);
                    }
                }
            }

            if (stallWaypoints != null && stallWaypoints.Length > 0)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.7f);
                for (int i = 0; i < stallWaypoints.Length; i++)
                {
                    if (stallWaypoints[i] == null) continue;
                    Gizmos.DrawWireSphere(stallWaypoints[i].position + Vector3.up * 0.2f, 0.4f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);

            if (stallWaypoints != null && stallWaypoints.Length > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
                for (int i = 0; i < stallWaypoints.Length; i++)
                {
                    if (stallWaypoints[i] == null) continue;
                    Gizmos.DrawSphere(stallWaypoints[i].position + Vector3.up * 0.25f, 0.4f);
                }
            }

            if (waypoints != null && waypoints.Length > 0)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null) continue;
                    Gizmos.color = (i == currentWaypointIndex && Application.isPlaying) ? Color.green : Color.yellow;
                    Gizmos.DrawSphere(waypoints[i].position + Vector3.up * 0.2f, 0.35f);
                }
            }
        }
    }
}
