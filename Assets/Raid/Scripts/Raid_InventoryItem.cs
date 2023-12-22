using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Photon.Pun;
using static Battle0.Scripts.Lobby.InRoom.RoomSetupManager;

public class Raid_InventoryItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image ItemImage;
    [SerializeField] public float ItemWeight;
    [SerializeField] private GameObject Lock;
    [SerializeField] private GameObject Bomb;
    [SerializeField] private GameObject BombIndicator;
    [SerializeField] private GameObject ItemBall;
    [SerializeField] private GameObject Heart;

    private RectTransform target;
    Vector2 endLoc;
    Vector3 offset;
    private float startTime;
    private float journeyLength;
    private float t = 0f;
    [SerializeField] private float speed = 15f;
    // [SerializeField] private TMP_Text ItemWeightText;

    public event Action<Raid_InventoryItem> OnItemClicked;

    private bool moving = false;

    private bool empty = true;

    private bool locked = false;

    private bool spectator = false;

    private bool active = true;

    private bool triggered = false;

    public bool bomb = false;

    private bool timeEnded = false;

    private AudioSource audioSource;
    public AudioClip pickUp;
    public AudioClip explosion;

    //type 0: default, type 1: lock
    public int _bombType = 0; 

    public PhotonView _photonView { get; set; }
    public void Awake()
    {
        Heart = GameObject.FindWithTag("Heart");
        target = Heart.GetComponent<RectTransform>();
        

        Raid_Timer raidTimer = FindObjectOfType<Raid_Timer>();
        if (raidTimer != null)
        {
            raidTimer.TimeEnded += OnTimeEnded;
        }
        audioSource = GetComponent<AudioSource>();
        this.ItemImage.gameObject.SetActive(false);
        empty = true;

        if ((PlayerRole)PhotonNetwork.LocalPlayer.CustomProperties["Role"] == PlayerRole.Spectator)
            spectator = true;
    }
    public void Update()
    {
        if (moving)
            BallToHeart();
    }

    public void SetData(Sprite ItemSprite, float LootItemWeight)
    {
        this.ItemImage.gameObject.SetActive(true);
        this.ItemImage.sprite = ItemSprite;
        // this.ItemWeightText.text = ItemWeight + "kg";
        empty = false;
    }

    public void RemoveData()
    {
        this.ItemImage.gameObject.SetActive(false);
    }
    public void SetBomb(int bombType)
    {
        _bombType = bombType;
        bomb = true;
        if (spectator)
            BombIndicator.SetActive(true);
    }
    public void TriggerBomb()
    {
        if(!triggered && !timeEnded)
        {
            if(_bombType == 1)
            {
                Bomb.transform.localScale = new Vector2(3f, 3f);
            }
            Bomb.SetActive(true);
            triggered = true;
            audioSource.PlayOneShot(explosion, SettingsCarrier.Instance.SentVolume(GetComponent<SetVolume>()._soundType));
        }
        //BombIndicator.SetActive(false);
    }
    public void LaunchBall()
    {
        audioSource.PlayOneShot(pickUp, SettingsCarrier.Instance.SentVolume(GetComponent<SetVolume>()._soundType));
        offset = new Vector3(target.rect.width / 2, -target.rect.height / 2, 0f);
        endLoc = target.position + offset;

        ItemBall.transform.SetParent(Heart.transform);
        moving = true;
    }
    public void BallToHeart()
    {
        t += speed * Time.deltaTime;
        float step = Mathf.SmoothStep(0f, 1f, t * Time.deltaTime);
        Vector2 newPosition = Vector2.Lerp(ItemBall.GetComponent<RectTransform>().anchoredPosition, endLoc, step);
        ItemBall.GetComponent<RectTransform>().anchoredPosition = newPosition;

        //WIP, korjaa kovakoodaus
        if (Vector2.Distance(ItemBall.GetComponent<RectTransform>().anchoredPosition, endLoc) <= 20f)
        {
            //Vector2.Distance(ItemBall.transform.position, endLoc)
            moving = false;
            Heart.GetComponent<HeartScript>().UpdateColor();
            ItemBall.SetActive(false);
        }
    }
    public void SetLocked()
    {
        Lock.SetActive(true);
        locked = true;
    }
    void OnTimeEnded()
    {
        timeEnded = true;
        active = false;
    }

    public void OnPointerClick(PointerEventData pointerData)
    {
        if (empty || locked)
        {
            return;
        }
        if (pointerData.button == PointerEventData.InputButton.Left)
        {
            if (!spectator && active)
            {
                OnItemClicked?.Invoke(this);
            }       
        }
        else
        {
            return;
        }
    }
}
