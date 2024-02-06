using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Altzone.Scripts.Model.Poco.Player;
using System.Globalization;
using Altzone.Scripts;
using Altzone.Scripts.Config;
using Altzone.Scripts.Model.Poco.Clan;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }

    private ServerPlayer _player;
    private ServerClan _clan;
    private ServerStock _stock;

    private int _accessTokenExpiration;

    public delegate void LogInStatusChanged(bool isLoggedIn);
    public static event LogInStatusChanged OnLogInStatusChanged;

    public delegate void ClanChanged(ServerClan clan);
    public static event ClanChanged OnClanChanged;

    public delegate void ClanInventoryChanged();
    public static event ClanInventoryChanged OnClanInventoryChanged;

    public bool isLoggedIn = false;

    public string AccessToken { get => PlayerPrefs.GetString("accessToken", string.Empty); set => PlayerPrefs.SetString("accessToken", value); }
    public int AccessTokenExpiration { get => _accessTokenExpiration; set => _accessTokenExpiration = value; }
    public ServerPlayer Player { get => _player; set => _player = value; }
    public ServerClan Clan
    {
        get => _clan; set
        {
            _clan = value;

            if (Player != null)
                Player.clan_id = Clan._id;
        }
    }
    public ServerStock Stock { get => _stock; set => _stock = value; }

    public static string ADDRESS = "https://altzone.fi/api/";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private void Start()
    {
        LogIn();
    }

    public void Reset()
    {
        Player = null;
        Clan = null;
        Stock = null;

        PlayerPrefs.SetString("accessToken", string.Empty);
        PlayerPrefs.SetString("playerId", string.Empty);
        PlayerPrefs.SetString("profileId", string.Empty);
    }

    public void RaiseClanChangedEvent()
    {
        if (OnClanChanged != null)
            OnClanChanged(Clan);
    }
    public void RaiseClanInventoryChangedEvent()
    {
        if (OnClanInventoryChanged != null)
            OnClanInventoryChanged();
    }

    private void LogIn()
    {
        if (AccessToken == string.Empty)
        {
            return;
        }
        else
        {
            StartCoroutine(GetPlayerFromServer(player =>
            {
                if (player == null)
                {
                    return;
                }

                SetPlayerValues(player);

                if (Clan == null)
                {
                    StartCoroutine(GetClanFromServer(clan =>
                    {
                        if (clan == null)
                        {
                            return;
                        }

                        RaiseClanChangedEvent();
                        RaiseClanInventoryChangedEvent();
                    }));
                }

            }));
        }
    }
    public void LogOut()
    {
        Reset();

        var playerSettings = GameConfig.Get().PlayerSettings;
        playerSettings.PlayerGuid = "12345";                    //Default player
        isLoggedIn = false;

        if (OnLogInStatusChanged != null)
            OnLogInStatusChanged(false);

        if (OnClanChanged != null)
            OnClanChanged(null);
    }

    public void SetProfileValues(JObject profileJSON)
    {
        AccessToken = profileJSON["accessToken"].ToString();
        AccessTokenExpiration = int.Parse(profileJSON["tokenExpires"].ToString());
        PlayerPrefs.SetString("playerId", profileJSON["Player"]["_id"].ToString());

        LogIn();
    }

    public void SetPlayerValues(ServerPlayer player)
    {
        string clanId = player.clan_id;

        if (clanId == null)
            clanId = "12345";

        // 12345 is DemoClan in DataStore

        // Check if the customplayer index is in DataStorage
        var storefront = Storefront.Get();
        PlayerData playerData = null;

        storefront.GetPlayerData(player.uniqueIdentifier, p => playerData = p);

        string currentCustomCharacterId = playerData == null ? "1" : playerData.CurrentCustomCharacterId;

        PlayerData newPlayerData = null;
        newPlayerData = new PlayerData(player._id, player.clan_id, currentCustomCharacterId, player.name, player.backpackCapacity, player.uniqueIdentifier);

        PlayerPrefs.SetString("profileId", player.profile_id);

        Storefront.Get().SavePlayerData(newPlayerData, null);
        var playerSettings = GameConfig.Get().PlayerSettings;

        playerSettings.PlayerGuid = player.uniqueIdentifier;

        isLoggedIn = true;

        if (OnLogInStatusChanged != null)
            OnLogInStatusChanged(true);
    }
    public IEnumerator SaveClanFromServerToDataStorage(ServerClan clan)
    {
        PlayerData playerData = null;
        ClanData clanData = null;

        var gameConfig = GameConfig.Get();
        var playerSettings = gameConfig.PlayerSettings;
        var playerGuid = playerSettings.PlayerGuid;
        var store = Storefront.Get();

        store.GetPlayerData(playerGuid, playerDataFromStorage =>
        {
            if (playerDataFromStorage == null)
            {
                return;
            }

            playerData = playerDataFromStorage;

            playerData.ClanId = clan._id;
            store.SavePlayerData(playerData, null);

            store.GetClanData(playerData.ClanId, clanDataFromStorage =>
            {
                if (clanDataFromStorage == null)
                {
                    clanData = new ClanData(clan._id, clan.name, clan.tag, clan.gameCoins);
                    return;
                }
                else
                {
                    clanData = clanDataFromStorage;
                }

            });
        });

        if (Stock == null)
        {
            yield return StartCoroutine(GetStockFromServer(clan, stock =>
            {
                if (stock == null)
                {
                    StartCoroutine(PostStockToServer(stock =>
                    {
                        if (stock != null)
                        {
                            Stock = stock;
                        }
                    }));
                }
            }));
        }

        yield return StartCoroutine(GetStockItemsFromServer(Stock, new List<ServerItem>(), null, 0, items =>
        {
            if (items != null)
            {
                ClanInventory inventory = new ClanInventory();
                List<ClanFurniture> clanFurniture = new List<ClanFurniture>();

                foreach (var item in items)
                {
                    clanFurniture.Add(new ClanFurniture(item._id, item.name.Trim().ToLower(CultureInfo.GetCultureInfo("en-US")).Replace(" ", ".")));
                }

                inventory.Furniture = clanFurniture;
                clanData.Inventory = inventory;
            }
        }));

        store.SaveClanData(clanData, null);
    }

    #region Server

    #region Player
    public IEnumerator GetPlayerFromServer(Action<ServerPlayer> callback)
    {
        if (Player != null)
            Debug.LogWarning("Player already exists. Consider using ServerManager.Instance.Player if the most up to data data from server is not needed.");

        yield return StartCoroutine(WebRequests.Get(ADDRESS + "player/" + PlayerPrefs.GetString("playerId", string.Empty), AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject result = JObject.Parse(request.downloadHandler.text);
                ServerPlayer player = result["data"]["Player"].ToObject<ServerPlayer>();
                Player = player;

                if (callback != null)
                    callback(player);
            }
            else
            {
                if (callback != null)
                    callback(null);
            }
        }));
    }

    #endregion

    #region Clan
    public IEnumerator GetClanFromServer(Action<ServerClan> callback)
    {
        if (Clan != null)
            Debug.LogWarning("Clan already exists. Consider using ServerManager.Instance.Clan if the most up to data data from server is not needed.");

        if (Player.clan_id == null)
            yield break;


        yield return StartCoroutine(WebRequests.Get(ADDRESS + "clan/" + Player.clan_id, AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject result = JObject.Parse(request.downloadHandler.text);
                ServerClan clan = result["data"]["Clan"].ToObject<ServerClan>();
                Clan = clan;

                StartCoroutine(SaveClanFromServerToDataStorage(Clan));

                if (callback != null)
                    callback(Clan);
            }
            else
            {
                if (callback != null)
                    callback(null);
            }
        }));
    }

    public IEnumerator GetAllClans(int page, Action<List<ServerClan>, PaginationData> callback)
    {
        string query = ADDRESS + "clan?page=" + page + "&limit=5";

        StartCoroutine(WebRequests.Get(query, AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject result = JObject.Parse(request.downloadHandler.text);
                JArray clans = (JArray)result["data"]["Clan"];

                PaginationData paginationData = result["paginationData"].ToObject<PaginationData>();

                if (callback != null)
                    callback(clans.ToObject<List<ServerClan>>(), paginationData);
            }
            else
            {
                if (callback != null)
                    callback(null, null);
            }
        }));

        yield break;
    }

    public IEnumerator JoinClan(ServerClan clanToJoin, Action<ServerClan> callback)
    {
        string body = @$"{{""clan_id"":""{clanToJoin.id}"",""player_id"":""{Player._id}""}}";

        StartCoroutine(WebRequests.Post(ADDRESS + "clan/join", body, AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success || request.responseCode == 500)
            {
                JObject result = JObject.Parse(request.downloadHandler.text);
                string clanId = result["data"]["Join"]["clan_id"].ToString();

                StartCoroutine(WebRequests.Get(ADDRESS + "clan/" + clanId, AccessToken, request =>
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        JObject result = JObject.Parse(request.downloadHandler.text);
                        ServerClan clan = result["data"]["Clan"].ToObject<ServerClan>();
                        Clan = clan;

                        StartCoroutine(SaveClanFromServerToDataStorage(Clan));

                        if (callback != null)
                            callback(Clan);
                    }
                    else
                    {
                        if (callback != null)
                            callback(null);
                    }
                }));
            }
            else
            {
                if (callback != null)
                    callback(null);
            }
        }));

        yield break;
    }

    public IEnumerator PostClanToServer(string name, string tag, int coins, bool isOpen, Action<ServerClan> callback)
    {
        string body = @$"{{""name"":""{name}"",""tag"":""{tag}"",""gameCoins"":{coins},""isOpen"":{isOpen.ToString().ToLower()}}}";

        yield return StartCoroutine(WebRequests.Post(ADDRESS + "clan", body, AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject result = JObject.Parse(request.downloadHandler.text);
                ServerClan clan = result["data"]["Clan"].ToObject<ServerClan>();
                Clan = clan;
                Player.clan_id = Clan._id;

                ClanData clanData = null;
                clanData = new ClanData(Clan._id, Clan.name, Clan.tag, Clan.gameCoins);
                Storefront.Get().SaveClanData(clanData, null);

                if (callback != null)
                {
                    callback(Clan);
                    RaiseClanChangedEvent();
                }
            }
            else
            {
                if (callback != null)
                {
                    callback(null);
                }
            }
        }));

        if (Clan != null && Clan.stockCount == 0)
        {
            yield return StartCoroutine(PostStockToServer(stock =>
            {
                if (callback != null)
                {
                    Stock = stock;
                }
            }));
        }
    }

    #endregion


    #region Stock
    public IEnumerator GetStockFromServer(ServerClan clan, Action<ServerStock> callback)
    {
        if (Stock != null)
            Debug.LogWarning("Stock already exists. Consider using ServerManager.Instance.Stock if the most up to data data from server is not needed.");

        yield return StartCoroutine(WebRequests.Get(ADDRESS + "stock?search=clan_id=\"" + clan._id + "\"", AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject result = JObject.Parse(request.downloadHandler.text);
                Stock = result["data"]["Stock"][0].ToObject<ServerStock>();     // Clan can have multiple stock but for now we get only the first

                if (callback != null)
                {
                    callback(Stock);
                }
                else
                {
                    if (callback != null)
                    {
                        callback(null);
                    }
                }
            }
        }));
    }
    public IEnumerator GetStockItemsFromServer(ServerStock stock, List<ServerItem> serverItems, PaginationData paginationData, int pageCount, Action<List<ServerItem>> callback)
    {
        if (stock == null)
            yield break;

        bool lastPage = true;
        string query = string.Empty;

        if (paginationData == null)
            query = ADDRESS + "item?limit=10&search=stock_id=\"" + stock._id + "\"";
        else
            query = ADDRESS + "item?page=" + ++paginationData.currentPage + "&limit=10&search=stock_id=\"" + stock._id + "\"";

        yield return StartCoroutine(WebRequests.Get(query, AccessToken, request =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                List<ServerItem> requestItems = new List<ServerItem>();
                JObject jObject = JObject.Parse(request.downloadHandler.text);
                JArray array = (JArray)jObject["data"]["Item"];
                requestItems = array.ToObject<List<ServerItem>>();

                foreach (var item in requestItems)
                    serverItems.Add(item);

                paginationData = jObject["paginationData"].ToObject<PaginationData>();

                if (paginationData.pageCount != 0)
                    pageCount = paginationData.pageCount;

                if (paginationData != null && paginationData.currentPage < pageCount)
                    lastPage = false;
            }
            else
            {
                string debugString = "Could not fetch items from stock!";

                if(request.responseCode == 404)
                {
                    debugString += " The stock might not have any items.";
                }

                Debug.Log(debugString);

                return;
            }
        }));

        if (!lastPage)
            yield return StartCoroutine(GetStockItemsFromServer(stock, serverItems, paginationData, pageCount, null));

        if (callback != null)
        {
            callback(serverItems);
        }
        else
        {
            if (callback != null)
            {
                callback(null);
            }
        }
    }
    public IEnumerator PostStockToServer(Action<ServerStock> callback)
    {
        if (Stock != null)
            yield break;

        string body = string.Empty;

        if (Clan.stockCount == 0)
        {
            body = @$"{{""type"":0,""rowCount"":5,""columnCount"":10,""clan_id"":""{Clan._id}""}}";

            StartCoroutine(WebRequests.Post(ADDRESS + "stock", body, AccessToken, request =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    JObject result = JObject.Parse(request.downloadHandler.text);
                    Stock = result["data"]["Stock"].ToObject<ServerStock>();

                    if (callback != null)
                        callback(Stock);
                }
                else
                {
                    if (callback != null)
                        callback(null);
                }
            }));
        }

        yield break;
    }

    #endregion

    #endregion
}
