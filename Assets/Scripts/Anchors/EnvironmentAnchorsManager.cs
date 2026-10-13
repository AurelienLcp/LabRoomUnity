using Oculus.Avatar2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


//La classe EnvironmentManager gère la relocalisation du monde en utilisant des ancres spatiales.
//Elle trouve les ancres spatiales, désactive leurs rendus, et ajuste la position et l'orientation d'un repère pour aligner le monde virtuel avec les ancres détectées.

public class EnvironmentAnchorsManager : NetworkBehaviour
{
    // Transformations représentant les positions et directions de repère
    [SerializeField]
    private Transform reperePos;
    // Transformation représentant l'angle de repère
    [SerializeField]
    private Transform repereAngle;
    [SerializeField]
    private Transform repereDir;
    
    

    // Listes des positions et des ancres
    private List<Transform> anchorsPos = new List<Transform>();
    private List<Transform> anchorsList = new List<Transform>();

    // Liste des objets à relocaliser
    private List<GameObject> relocateObjectsList = new List<GameObject>();

    // Booléen pour déterminer si c'est le premier démarrage
    bool start = true;

    // Méthode appelée lors de l'initialisation de l'objet
    private void Awake()
    {
        var anchors = FindObjectsByType<OVRSpatialAnchor>(FindObjectsSortMode.None);

            // Trouver toutes les ancres spatiales dans la scène
            //var anchorsID = FindObjectsByType<AnchorsID>(FindObjectsSortMode.None);
            //var anchors = new OVRSpatialAnchor[3];

            /*for (int i = 0; i < anchorsID.Length; i++)
            {
                anchors[i] = anchorsID[i].gameObject.GetComponent<OVRSpatialAnchor>();
            }*/
            // Ajouter chaque ancre à la liste des ancres et désactiver leurs rendus et canvas
            foreach (var anchor in anchors)
            {
                // Trier les ancres par leur numéro d'ordre
                anchorsList.Add(anchor.transform);
                MeshRenderer[] mesh = anchor.GetComponentsInChildren<MeshRenderer>();
                foreach (var meshRenderer in mesh)
                {
                    meshRenderer.gameObject.SetActive(false);
                }
                if (anchor.transform.GetChild(1).gameObject.activeSelf)
                {
                    Canvas canvas = anchor.GetComponentInChildren<Canvas>();
                    canvas.gameObject.SetActive(false);
                }
            }

            // Trier les ancres par leur numéro d'ordre
            anchorsList.Sort((anchor1, anchor2) =>
            {
                int anchor1Num = int.Parse(anchor1.transform.GetChild(1).transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
                int anchor2Num = int.Parse(anchor2.transform.GetChild(1).transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
                return anchor1Num.CompareTo(anchor2Num);
            });
            if (anchorsList.Count > 0)
            {
                // Ajouter les deux premières ancres à la liste des positions des ancres
                anchorsPos.Add(anchorsList[0]);
                anchorsPos.Add(anchorsList[1]);
            }


            // Relocaliser le monde virtuel
            RelocateWorld();
    }
        // Méthode pour obtenir la liste des objets à relocaliser
    private void GetRelocateObjectsList()
    {
        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(relocateObjectsList);

        // Retirer les objets avec les tags "repere" et "Player" de la liste
        relocateObjectsList.RemoveAll(gameObject => gameObject.transform.tag == "repere" || gameObject.transform.tag == "Player");
    }

    // Méthode pour démarrer la relocalisation du monde virtuel
    private void RelocateWorld() //InputAction.CallbackContext obj
    {
        InvokeRepeating("RelocateRepere", 0, 1f);
    }

    // Méthode pour assigner les objets relocalisés au repère de position
    private void AssigneGameObj()
    {
        foreach (var obj in relocateObjectsList)
        {
            NetworkObject netObj = null;
            try
            {
                netObj = obj.GetComponentOrNull<NetworkObject>();
                if (IsServer)
                    netObj.TrySetParent(reperePos);
            }
            catch { }
            if (obj.transform.parent != reperePos && netObj == null)
                obj.transform.SetParent(reperePos);
        }
    }

    // Méthode pour relocaliser le repère de position
    private void RelocateRepere()
    {
        // Si le client et le serveur ne sont pas actifs, ne rien faire
        //if (!networkManager.IsClient &&  !networkManager.IsServer)
        //    return;

        GetRelocateObjectsList();
        AssigneGameObj();

        if(anchorsPos.Count >= 2)
        {
            // Calculer la nouvelle position du repère
            Vector3 newPosition = new Vector3(anchorsPos.Average(t => t.position.x), anchorsPos.Average(t => t.position.y), anchorsPos.Average(t => t.position.z));
            reperePos.position = new Vector3(newPosition.x, reperePos.position.y, newPosition.z);

            // Calculer les vecteurs pour le repère et les ancres
            Vector3 vecteurRepere = reperePos.position - repereAngle.position;
            Vector3 vecteurAnchor = anchorsList[1].position - anchorsList[0].position;
        
        

            // Calculer le nouvel angle entre le repère et les ancres
            //float direction = vecteurRepere - vecteurAnchor;


            //reperePos.Rotate(Vector3.up, newAngle);

            //float AngleSigne = Vector3.Angle(anchorsList[2].position - reperePos.position, vecteurRepere);
            float newAngle = Vector3.Angle(vecteurRepere, vecteurAnchor);

            //Debug.Log("Angle newAngle: " + newAngle);

            // Mettre à jour la rotation du repère
            reperePos.localRotation = Quaternion.FromToRotation(vecteurRepere, vecteurAnchor) * reperePos.localRotation;
            reperePos.localEulerAngles = new Vector3(0, reperePos.localEulerAngles.y, 0);

            // Ajuster la rotation initiale au démarrage
            if (start)
            {
                start = false;
                float rAngle = Vector2.SignedAngle(new Vector2(repereDir.position.x - anchorsList[0].position.x, repereDir.position.z - anchorsList[0].position.z), new Vector2(vecteurAnchor.x, vecteurAnchor.z));
                float aAngle = Vector2.SignedAngle(new Vector2(anchorsList[2].position.x - anchorsList[0].position.x, anchorsList[2].position.z - reperePos.position.z), new Vector2(vecteurAnchor.x, vecteurAnchor.z));

                //Debug.Log("Angle rAngle: " + rAngle);
                //Debug.Log("Angle aAngle: " + aAngle);

                // Inverser la position du repère si nécessaire
                if (rAngle > 0 && aAngle < 0 || rAngle < 0 && aAngle > 0)
                {
                    repereAngle.localPosition = -repereAngle.localPosition;
                    vecteurRepere = reperePos.position - repereAngle.position;
                }
                reperePos.localRotation = Quaternion.FromToRotation(vecteurRepere, vecteurAnchor) * reperePos.localRotation;
                reperePos.localEulerAngles = new Vector3(0, reperePos.localEulerAngles.y, 0);

            }
        }

    }

}
