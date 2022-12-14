using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class CharacterHandler : MonoBehaviour
{
    #region Delegations And Events

    public delegate void CharacterJumpedDelegate();
    public static event CharacterJumpedDelegate characterJumpedEvent;

    public delegate void CharacterRunDelegate();
    public static event CharacterRunDelegate characterRunEvent;

    #endregion

    #region Instances
    MapGenerator mapGenerator;
    CharacterSpawner characterSpawner;
    GameManager gameManager;
    #endregion

    #region Character
    [Header("Character:")]
    [Space]
    [Space]

    [SerializeField]
    Animator characterAnimator;
    bool isCharacterDead = false;
    Vector2 characterPosition;
    BoxCollider2D characterCollider;
    GameObject characterPrefab;
    float characterSpeed;
    SpriteRenderer characterSpriteRenderer;
    #endregion

    #region Touches
    [Header("Touches:")]
    [Space]
    [Space]

    [SerializeField]
    float dragLength;
    Vector2 touchFirstPos;
    Vector2 touchLastPos;
    bool characterMovementTransitionIsOver = true;
    #endregion

    #region MapInfo
    [Header("MapInfo:")]
    [Space]
    [Space]

    [SerializeField]
    Vector2 roadLeftSide;

    [SerializeField]
    Vector2 roadRightSide;
    Vector2 slope;
    #endregion

    #region SortingLayers
    string characterLeftSortingLayer = "CharacterLeft";
    string characterRightSortingLayer = "CharacterRight";
    string deadCharacterSortingLayer = "DeadCharacter";
    #endregion

    void Awake()
    {
        AssignValuesInAwake();
    }

    void Start()
    {
        characterRunEvent?.Invoke();

        AssignValuesInStart();
    }

    void Update()
    {
        MoveCharacterAloneXAxis();
        CharacterController();

        //if (IsCharacterCollided())
        //    CharacterDied();
    }


    void AssignValuesInAwake()
    {
        characterCollider = GetComponent<BoxCollider2D>();
        characterSpriteRenderer = GetComponent<SpriteRenderer>();
        characterSpriteRenderer.sortingLayerName = characterRightSortingLayer;
    }

    void AssignValuesInStart()
    {
        if (!MapGenerator.Instance)
        {
            throw new Exception("MapGenerator Instance is not found!");
        }
        mapGenerator = MapGenerator.Instance;

        if (!CharacterSpawner.Instance)
        {
            throw new Exception("CharacterSpawner Instance is not found!");
        }

        characterSpawner = CharacterSpawner.Instance;

        characterPrefab = characterSpawner.Character;
        //if (!GameManager.Instance)
        //{
        //    throw new Exception("gameManager is not found!");
        //}
        //gameManager = GameManager.Instance;
        GameManager.characterDiedEvent += CharacterDied;

        slope = mapGenerator.MovementSlope;
        characterSpeed = mapGenerator.cameraInitialSpeed;
    }

    bool IsCharacterCollided()
    {
        //if (resultsColliders.Count != 0)

        if (characterCollider.IsTouchingLayers() && !isCharacterDead)
        {
            List<Collider2D> resultsColliders = new List<Collider2D>();
            ContactFilter2D contactFilter = new ContactFilter2D();
            characterCollider.OverlapCollider(contactFilter, resultsColliders);
            string colliderTag = resultsColliders[0].tag;

            if (colliderTag == "Gem" || colliderTag == "SilverCoin" || colliderTag == "GoldenCoin")
            {

                return false;
            }
            isCharacterDead = true;
        }
        return isCharacterDead;
    }

    void MoveCharacterAloneXAxis()
    {
        
        characterPosition = characterPrefab.transform.position;
        characterPosition.x += slope.x * Time.deltaTime * characterSpeed;
        characterPrefab.transform.position = characterPosition;
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
        if (Input.GetKeyDown(KeyCode.Q))
            StartCoroutine(MoveLeft());
        if (Input.GetKeyDown(KeyCode.E))
            StartCoroutine(MoveRight());

    }

    void CharacterController()
    {
        if (Input.touchCount != 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchFirstPos = Camera.main.ScreenToWorldPoint(touch.position);
                touchLastPos = Camera.main.ScreenToWorldPoint(touch.position);
            }

            if (touch.phase == TouchPhase.Moved)
            {
                touchLastPos = Camera.main.ScreenToWorldPoint(touch.position);
            }

            if (touch.phase == TouchPhase.Ended)
            {
                touchLastPos = Camera.main.ScreenToWorldPoint(touch.position);
            }

            if (touchLastPos.y - touchFirstPos.y >= dragLength && characterMovementTransitionIsOver)
                StartCoroutine(MoveLeft());

            else if (touchLastPos.y - touchFirstPos.y <= -dragLength && characterMovementTransitionIsOver)
                StartCoroutine(MoveRight());

            if (touch.tapCount == 2)
            {
                Jump();
            }
        }

    }

    IEnumerator MoveLeft()
    {
        characterMovementTransitionIsOver = false;
        while (true)
        {
            yield return null;
            characterPosition.y += Time.deltaTime * characterSpeed * 2.5f;
            if (characterPosition.y >= roadLeftSide.y) // character is in leftside of road
            {
                characterPosition.y = roadLeftSide.y;
                characterSpriteRenderer.sortingLayerName = characterLeftSortingLayer;
                characterPrefab.transform.position = new Vector2(characterPrefab.transform.position.x, characterPosition.y);
                break;
            }

            characterPrefab.transform.position = new Vector2(characterPrefab.transform.position.x, characterPosition.y);
        }
        characterMovementTransitionIsOver = true;
    }

    IEnumerator MoveRight()
    {
        characterMovementTransitionIsOver = false;
        while (true)
        {
            yield return null;
            characterPosition.y -= Time.deltaTime * characterSpeed * 2.5f;
            if (characterPosition.y <= roadRightSide.y) // character is in rightside of road
            {
                characterPosition.y = roadRightSide.y;
                characterSpriteRenderer.sortingLayerName = characterRightSortingLayer;
                characterPrefab.transform.position = new Vector2(characterPrefab.transform.position.x, characterPosition.y);

                break;
            }

            characterPrefab.transform.position = new Vector2(characterPrefab.transform.position.x, characterPosition.y);
        }
        characterMovementTransitionIsOver = true;
    }

    void Jump()
    {
        characterJumpedEvent?.Invoke();
        characterAnimator.SetBool("IsJumping", true);
        characterCollider.enabled = false;
    }

    public void SetLandingTrue()
    {
        characterAnimator.SetBool("IsJumping", false);
        characterCollider.enabled = true;
        characterRunEvent?.Invoke();
    }


    void CharacterDied()
    {
        characterSpeed = 0f;
        characterSpriteRenderer.sortingLayerName = deadCharacterSortingLayer;
        GameManager.characterDiedEvent -= CharacterDied;
    }
}
