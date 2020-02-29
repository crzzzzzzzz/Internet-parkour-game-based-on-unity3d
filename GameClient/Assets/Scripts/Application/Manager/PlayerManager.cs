﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Common;

public class PlayerManager : BaseManager
{
    //本地玩家的状态
    private GameObject hostPlayerState;
    private Text clientName;
    private Transform hostHealthTran;
    private Text hostCoinText;
    private Text hostItemText;
    //远程玩家的状态
    private GameObject clientPlayerState;
    private Text hostName;
    private Transform clientHealthTran;
    private Text clientCoinText;
    private Text clientItemText;
    //client在左边
    private Vector3 clientPos = new Vector3(-4.968f, 0.31f, 0.8f);
    //host在右边
    private Vector3 hostPos = new Vector3(4.968f, 0.31f, 0.8f);
    private Transform players;
    private UserData userData;
    private GameData gameData;
    private int hostHealth = -1;
    private float hostSkillTime = -1;
    private int clientHealth = -1;
    private float clientSkillTime = -1;
    public UserData UserData
    {
        get => userData;
        set => userData = value;
    }
    public GameData GameData
    {
        get => gameData;
        set => gameData = value;
    }
    private Dictionary<Role_ResultRoleType, RoleData> roleDataDict = new Dictionary<Role_ResultRoleType, RoleData>();
    private Role_ResultRoleType localRoleType;
    private GameObject localRoleGameObject;
    private GameObject remoteRoleGameObject;
    private GameObject gamePanel;
    private CamFollowPlayer camFollowPlayer;
    private bool isEnterPlaying = false;
    private bool canPlayPlayingBG = false;
    //初次跑道序号
    private int index1 = -1;
    private int index2 = -1;
    public override void Update()
    {
        if (isEnterPlaying)
        {
            EnterPlaying();
            isEnterPlaying = false;
        }
        if (canPlayPlayingBG)
        {
            Game.Instance.sound.PlayBg("GamePlaying");
            canPlayPlayingBG = false;
        }
    }
    public void SetShopState(int healthTime, int bigHealthTime, int skillTimeTime, int bigSkillTimeTime)
    {
        userData.SetShopState(healthTime, bigHealthTime, skillTimeTime, bigSkillTimeTime);
    }
    public void UpdateResult(int totalCount, int winCount)
    {
        userData.TotalCount = totalCount;
        userData.WinCount = winCount;
    }
    public void UpdateCoin(int coinNum)
    {
        userData.CoinNum = coinNum;
    }
    public void EnterPlayingSync(int index1, int index2, int hostHealth, float hostSkillTime, int clientHealth, float clientSkillTime)
    {
        isEnterPlaying = true;
        this.index1 = index1;
        this.index2 = index2;
        this.hostHealth = hostHealth;
        this.hostSkillTime = hostSkillTime;
        this.clientHealth = clientHealth;
        this.clientSkillTime = clientSkillTime;
    }
    public void GameStart()
    {
        //开始游戏
        gameData.IsPlay = true;
        canPlayPlayingBG = true;
    }
    private void EnterPlaying()
    {
        //生成角色
        SpawnRoles();
        //相机跟随
        camFollowPlayer.FollowRole();
    }
    public void SetLocalRoleType(Role_ResultRoleType type)
    {
        localRoleType = type;
    }

    public GameObject LocalRoleGameObject { get => localRoleGameObject; set => localRoleGameObject = value; }

    public override void OnInit()
    {
        //游戏初始状态：非暂停，未开始
        gameData = new GameData(false);

        players = GameObject.Find("Players").transform;
        camFollowPlayer = GameObject.Find("CameraAndOthers/Camera").GetComponent<CamFollowPlayer>();
        clientPlayerState = GameObject.Find("Canvas/BackGround/Left").transform.Find("ClientPlayerState").gameObject;
        hostPlayerState = GameObject.Find("Canvas/BackGround/Right").transform.Find("HostPlayerState").gameObject;
        hostName = hostPlayerState.transform.Find("HostName").GetComponent<Text>();
        hostHealthTran = hostPlayerState.transform.Find("Health");
        hostCoinText = hostPlayerState.transform.Find("Coin").GetComponent<Text>();
        hostItemText = hostPlayerState.transform.Find("State").GetComponent<Text>();

        clientName = clientPlayerState.transform.Find("ClientName").GetComponent<Text>();
        clientHealthTran = clientPlayerState.transform.Find("Health");
        clientCoinText = clientPlayerState.transform.Find("Coin").GetComponent<Text>();
        clientItemText = clientPlayerState.transform.Find("State").GetComponent<Text>();
    }

    private void InitRoleDataDict()
    {
        //这里设置为可选
        roleDataDict.Add(Role_ResultRoleType.Host, new RoleData(Role_ResultRoleType.Host, "Players/HostPlayer", hostPos, hostHealth,hostSkillTime));
        roleDataDict.Add(Role_ResultRoleType.Client, new RoleData(Role_ResultRoleType.Client, "Players/ClientPlayer", clientPos, clientHealth,clientSkillTime));
    }
    //设置玩家的姓名
    public void SetPlayersName(string hostName, string clientName)
    {
        this.hostName.text = hostName;
        this.clientName.text = clientName;
    }
    //初始化角色时脚本添加顺序十分重要，为了防止空指针，不再使用生命周期中的Start或Awake，改为手动赋值。
    public void SpawnRoles()
    {
        gamePanel = GameObject.FindGameObjectWithTag(Tag.GamePanel);
        InitRoleDataDict();
        LocalMoveRequest localMoveRequest = gamePanel.GetComponent<LocalMoveRequest>();
        RemoteMoveRequest remoteMoveRequest = gamePanel.GetComponent<RemoteMoveRequest>();
        TakeDamageRequest takeDamageRequest = gamePanel.GetComponent<TakeDamageRequest>();
        GetCoinRequest getCoinRequest = gamePanel.GetComponent<GetCoinRequest>();
        GameOverRequest gameOverRequest = gamePanel.GetComponent<GameOverRequest>();
        LocalPlayerMove localPlayerMove = null;
        RemotePlayerMove remotePlayerMove = null;

        //生成玩家的时候添加脚本，一定要注意添加脚本时的初始化操作
        foreach (RoleData roleData in roleDataDict.Values)
        {
            GameObject go = GameObject.Instantiate(roleData.RolePrefab, roleData.SpawnPos, Quaternion.identity);
            //设置父物体
            go.transform.SetParent(players);
            if (roleData.Type == localRoleType)
            {
                //增强可读性
                localRoleGameObject = go;
                //生成角色的时候顺便先生成两条跑道
                RoadChange roadChange = localRoleGameObject.AddComponent<RoadChange>();
                //添加本地玩家控制脚本
                localPlayerMove = localRoleGameObject.AddComponent<LocalPlayerMove>();
                localMoveRequest.SetLocalPlayerMove(localPlayerMove);
                localPlayerMove.SetGameDataAndRoleDataAndRequests(
                    gameData,
                    roleData,
                    localMoveRequest,
                    takeDamageRequest,
                    getCoinRequest,
                    gameOverRequest);
                CreateRoadRequest createRoadRequest = localRoleGameObject.AddComponent<CreateRoadRequest>();
                roadChange.SetCreateRoadRequest(createRoadRequest, index1, index2);
                //设置UI信息
                switch (roleData.Type)
                {
                    case Role_ResultRoleType.Host:
                        localPlayerMove.SetLocalPlayerState(hostName.text, clientName.text, hostHealthTran, hostCoinText, hostItemText);
                        break;
                    case Role_ResultRoleType.Client:
                        localPlayerMove.SetLocalPlayerState(clientName.text, hostName.text, clientHealthTran, clientCoinText, clientItemText);
                        break;
                }
            }
            else
            {
                remoteRoleGameObject = go;
                //添加远程玩家同步脚本
                remotePlayerMove = remoteRoleGameObject.AddComponent<RemotePlayerMove>();
                remotePlayerMove.SetGameDataAndRoleData(gameData, roleData);
                remoteMoveRequest.SetRemotePlayerMove(remotePlayerMove);
                takeDamageRequest.SetRemotePlayerMove(remotePlayerMove);
                getCoinRequest.SetRemotePlayerMove(remotePlayerMove);
                //设置UI信息
                switch (roleData.Type)
                {
                    case Role_ResultRoleType.Host:
                        remotePlayerMove.SetRemotePlayerState(hostHealthTran, hostCoinText, hostItemText);
                        break;
                    case Role_ResultRoleType.Client:
                        remotePlayerMove.SetRemotePlayerState(clientHealthTran, clientCoinText, clientItemText);
                        break;
                }
            }
        }
        //其它初始化操作
        localPlayerMove.SetRemotePlayerMove(remoteRoleGameObject.GetComponent<RemotePlayerMove>());
        remotePlayerMove.SetLocalPlayerMove(localRoleGameObject.GetComponent<LocalPlayerMove>());
    }
    public void DestroyRoles()
    {
        //相机复位
        camFollowPlayer.transform.localPosition = new Vector3(0, 8.97f, -8.73f);
        camFollowPlayer.StopFollow();
        GameObject.Destroy(localRoleGameObject);
        GameObject.Destroy(remoteRoleGameObject);
        //这里设置false是给中途就退出用的
        gameData.IsPlay = false;
        //清空dict
        roleDataDict.Clear();
    }
}