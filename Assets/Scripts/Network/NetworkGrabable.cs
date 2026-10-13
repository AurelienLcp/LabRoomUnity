using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkGrabable : NetworkBehaviour
{
    public NetworkVariable<bool> grabbed = new NetworkVariable<bool>();
    public NetworkVariable<ulong> lastGrabUserId = new NetworkVariable<ulong>();
    public NetworkObject networkObject;// Référence à l'objet réseau.
    public Rigidbody rigidbody; // Référence au composant Rigidbody de l'objet.
    DataCollectionManager dataCollectionManager; // Référence à l'exportateur d'état des pièces.// Start is called once before the first execution of Update after the MonoBehaviour is created
    List<PsiExporterPiecesState> exportersPiecesState = new List<PsiExporterPiecesState>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkObject = GetComponent<NetworkObject>();
        rigidbody = GetComponent<Rigidbody>();
        //grabbed.OnValueChanged += OnValueGrabbedChanged;
        dataCollectionManager = FindAnyObjectByType<DataCollectionManager>();
    }

    public void Grab()
    {
        if (networkObject.OwnerClientId != NetworkManager.LocalClientId)
        {
            rigidbody.isKinematic = false; // Met à jour l'état du Rigidbody (devrait être déplacé dans le script du Rigidbody réseau).
            //rigidbody.useGravity = true; // Met à jour l'état du Rigidbody (devrait être déplacé dans le script du Rigidbody réseau).
        }
        GrabObjRpc();
    }

    // RPC envoyé au serveur pour saisir l'objet.
    [Rpc(SendTo.Server)]
    public void GrabObjRpc(RpcParams rpcParams = default)
    {
        grabbed.Value = true;// Met à jour l'état de saisie.
        var senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Grabbed by client {senderClientId}");
        lastGrabUserId.Value = senderClientId; // Met à jour l'ID de l'utilisateur qui a saisi l'objet.
        if (!dataCollectionManager._isServerSetup) return;
        PieceStatus status = new PieceStatus((int)senderClientId, gameObject.name, State.Grab, true, "", Location.Hand);
        if (dataCollectionManager.avatarInteractionsExporters.Count >= (int) senderClientId && dataCollectionManager.avatarInteractionsExporters[(int)senderClientId - 1] != null) CheckForPostPieceState(rpcParams.Receive.SenderClientId, status);
        //GrabObjClientRpc(rpcParams.Receive.SenderClientId);// Appelle le RPC client pour synchroniser l'état.
    }

    [Rpc(SendTo.NotServer)]
    public void GrabObjClientRpc(ulong senderClientId)
    {
        /*PieceStatus status = new PieceStatus((int)NetworkManager.LocalClientId, gameObject.name, State.Grab, true, "", Location.Hand);
        if (psiPieceStateExporter != null) CheckForPostPieceState(senderClientId, status);*/
    }

    // Méthode pour relâcher l'objet.
    public void Released()
    {
        if (networkObject.OwnerClientId != NetworkManager.LocalClientId)
        {
            rigidbody.isKinematic = true; // Met à jour l'état du Rigidbody (devrait être déplacé dans le script du Rigidbody réseau).
        }
        ReleasedServerRpc();
    }

    // RPC envoyé au serveur pour relâcher l'objet.
    [Rpc(SendTo.Server)]
    public void ReleasedServerRpc(RpcParams rpcParams = default)
    {
        grabbed.Value = false;// Met à jour l'état de saisie.
        var senderClientId = (int)rpcParams.Receive.SenderClientId;
        Debug.Log($"Released by client {senderClientId}");
        if (!dataCollectionManager._isServerSetup) return;
        PieceStatus status = new PieceStatus((int)senderClientId, gameObject.name, State.Ungrab, true, "", Location.Hand);
        if (dataCollectionManager.avatarInteractionsExporters[senderClientId - 1] != null) CheckForPostPieceState(rpcParams.Receive.SenderClientId, status);
        //ReleasedClientRpc(rpcParams.Receive.SenderClientId);// Appelle le RPC client pour synchroniser l'état.
    }

    // RPC envoyé aux clients pour synchroniser l'état de relâchement.
    [Rpc(SendTo.NotServer)]
    public void ReleasedClientRpc(ulong senderClientId)
    {
        /*PieceStatus status = new PieceStatus((int)NetworkManager.LocalClientId, gameObject.name, State.Ungrab, true, "", Location.Hand);
        if (psiPieceStateExporter != null) CheckForPostPieceState(senderClientId, status);*/
    }

    // Méthode pour vérifier et poster l'état de la pièce.
    private void CheckForPostPieceState(ulong senderClientId, PieceStatus state)
    {
        dataCollectionManager.avatarInteractionsExporters[(int)senderClientId - 1].Post(state);
       /* if (senderClientId == NetworkManager.LocalClientId)
        {
            
        }*/
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
