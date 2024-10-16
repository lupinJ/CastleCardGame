using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
//using UnityEditor.VersionControl;

public class PlayerRPC : NetworkBehaviour
{
    // 호스트의 덱을 받고 라운드를 시작
    [Rpc(RpcSources.All, RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendDeck(string message, RpcInfo info = default)
    {
        if(Runner.GameMode == GameMode.Single)
            StartCoroutine(GameManager.Play.RoundStart(message));
        if (info.Source.PlayerId == 3)
            StartCoroutine(GameManager.Play.RoundStart(message));
    }

    // Submit시에 선택한 카드list를 전송
    [Rpc(RpcSources.All, RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendHand(string message, int result, RpcInfo info = default)
    {
        if (GameManager.Play.IsPlayerTurn(info.Source.PlayerId))
        {
            StartCoroutine(GameManager.Play.Action(info.Source.PlayerId, result, message));
        }
    }

    // 납세시에 선택한 카드list를 전송
    [Rpc(RpcSources.All, RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendTex(string message, RpcInfo info = default)
    {
        GameManager.Play.ReciveTex(info.Source.PlayerId, message);
    }

    public void DisconnectRunner()
    {
        Destroy(Runner);
        SceneManager.LoadScene(0);
    }
}
