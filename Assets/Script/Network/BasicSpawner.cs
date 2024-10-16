using Fusion.Sockets;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    
    public async void StartGame(GameMode mode, string roomname) // 네트워크 시작
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomname,
            Scene = 1, // GameScene
            PlayerCount = 4
        }); 

    }
    public void DisconnectRunner() // 강제종료
    {
        if (null == _runner)
            return;
        Destroy(_runner);
    }

    /************** callback func **************/

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
    {
        int pid = player.PlayerId;
        GameManager.Play.SetMainText(string.Format("현재인원 ({0}/{1})",
            _runner.SessionInfo.PlayerCount, _runner.SessionInfo.MaxPlayers));
       
        if (runner.GameMode == GameMode.Single) // 싱글 모드라면
        {
            pid = _runner.LocalPlayer.PlayerId;
            GameManager.Play.GameSet(pid, true);
            StartCoroutine(GameManager.Play.RoundPrepare());
        }

        if (pid == 2) // 모두 참가했다면
        {
            pid = _runner.LocalPlayer.PlayerId;     
            GameManager.Play.GameSet(pid, false);
            StartCoroutine(GameManager.Play.RoundPrepare());
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        DisconnectRunner();
        SceneManager.LoadScene(0);
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) 
    {
        if (UIManager.UI != null)
        {
            UIManager.UI.SetNetText("Connet Failed");
            UIManager.UI.NetButtonActive(true);
        }
        DisconnectRunner();
        SceneManager.LoadScene(0);

    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (UIManager.UI != null)
        {
            UIManager.UI.SetNetText("Connet Failed");
            UIManager.UI.NetButtonActive(true);
        }
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)    { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    
}
