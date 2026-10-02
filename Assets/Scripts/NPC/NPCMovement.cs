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
            IdleOnly,
            PatrolWaypoints,
            RandomWander,
            FollowTarget
        }

        [Header("Mode Perilaku")]
        [Tooltip("Mode pergerakan NPC")]
        [SerializeField] private NPCMode mode = NPCMode.PatrolWaypoints;

        [Header("Kecepatan")]
        [Tooltip("Kecepatan jalan NPC")]
        [SerializeField] private float walkSpeed = 1.8f;

        [Tooltip("Kecepatan rotasi NPC saat berbelok")]
        [SerializeField] private float rotationSpeed = 5.0f;

        [Header("Pengaturan Waktu Diam (Idle)")]
        [Tooltip("Waktu minimal diam sebelum jalan ke titik berikutnya (detik)")]
        [SerializeField] private float minIdleTime = 1.5f;

        [Tooltip("Waktu maksimal diam (detik)")]
        [SerializeField] private float maxIdleTime = 3.5f;

        [Header("Patroli Waypoints (Mode PatrolWaypoints)")]
        [Tooltip("Daftar titik waypoint yang akan dilalui NPC")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointReachedDistance = 0.5f;

        [Header("Jalan Acak (Mode RandomWander)")]
        [Tooltip("Radius area jalan acak dari posisi awal")]
        [SerializeField] private float wanderRadius = 8.0f;

        [Header("Ikuti Target (Mode FollowTarget)")]
        [Tooltip("Target yang diikuti (misal Transform Player)")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private float stopDistance = 2.0f;

        [Header("Parameter Animator")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string isMovingParameter = "isMoving";

        [Header("Suara Langkah (Opsional)")]
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private AudioClip landSound;
        [Range(0f, 1f)][SerializeField] private float audioVolume = 0.5f;

        // Komponen internal
        private Animator animator;
        private NavMeshAgent navAgent;
        private CharacterController characterController;
        private Vector3 startPosition;
        private Vector3 wanderDestination;
        private int currentWaypointIndex = 0;
        private bool isIdle = false;
        private float idleTimer = 0f;
        private float currentSpeed = 0f;
        private int speedHash;
        private int isMovingHash;

        // Status Interaksi
        private bool isInteracting = false;
        private Transform interactor;

        public bool IsInteracting => isInteracting;
        public NPCMode CurrentMode => mode;
        public Transform[] Waypoints => waypoints;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            navAgent = GetComponent<NavMeshAgent>();
            characterController = GetComponent<CharacterController>();

            speedHash = Animator.StringToHash(speedParameter);
            isMovingHash = Animator.StringToHash(isMovingParameter);
            startPosition = transform.position;
        }

        private void Start()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.speed = walkSpeed;
                navAgent.angularSpeed = rotationSpeed * 60f;
            }

            isIdle = true;
            idleTimer = Random.Range(1.0f, 2.0f);
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

                case NPCMode.FollowTarget:
                    UpdateFollow();
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
                        MoveTowards(waypoints[currentWaypointIndex].position);
                    }
                }
                return;
            }

            if (waypoints[currentWaypointIndex] == null)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            Vector3 target = waypoints[currentWaypointIndex].position;
            float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                          new Vector3(target.x, 0, target.z));

            if (dist <= waypointReachedDistance)
            {
                isIdle = true;
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                StopMovement();
            }
            else
            {
                MoveTowards(target);
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
                    wanderDestination = GetRandomPoint(startPosition, wanderRadius);
                    MoveTowards(wanderDestination);
                }
                return;
            }

            float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                          new Vector3(wanderDestination.x, 0, wanderDestination.z));

            if (dist <= waypointReachedDistance)
            {
                isIdle = true;
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                StopMovement();
            }
            else
            {
                MoveTowards(wanderDestination);
            }
        }

        private void UpdateFollow()
        {
            if (followTarget == null)
            {
                currentSpeed = 0f;
                return;
            }

            float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                          new Vector3(followTarget.position.x, 0, followTarget.position.z));

            if (dist > stopDistance)
            {
                MoveTowards(followTarget.position);
            }
            else
            {
                StopMovement();
                currentSpeed = 0f;
                Vector3 lookDir = (followTarget.position - transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion rot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);
                }
            }
        }

        private void MoveTowards(Vector3 destination)
        {
            // Jika ada NavMeshAgent aktif
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(destination);
                currentSpeed = navAgent.velocity.magnitude;
                return;
            }

            // Gerakan langsung (CharacterController atau Transform)
            Vector3 dir = (destination - transform.position);
            dir.y = 0;

            if (dir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

                Vector3 moveDelta = transform.forward * (walkSpeed * Time.deltaTime);

                if (characterController != null && characterController.enabled)
                {
                    moveDelta.y = Physics.gravity.y * Time.deltaTime;
                    characterController.Move(moveDelta);
                }
                else
                {
                    transform.position += moveDelta;
                }

                currentSpeed = walkSpeed;
            }
            else
            {
                currentSpeed = 0f;
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

        private Vector3 GetRandomPoint(Vector3 center, float radius)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 target = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return target;
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

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
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
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);

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
