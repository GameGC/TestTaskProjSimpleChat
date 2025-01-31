using Data;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI
{
    public class DiscoverView : MonoBehaviour
    {
        [SerializeField] private RoomDiscoverController roomDiscoverController;
    
        [Header("UI")]
    
        [SerializeField] private Transform contentParent;
        [SerializeField] private RoomInfoView roomInfoPrefab;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Button createRoomButton;

        [SerializeField] private ChatView chatView;
        private void Start()
        {
            createRoomButton.onClick.AddListener(CreateRoom);
            roomDiscoverController.OnRoomAdded += AddRoom;
        }

        private void CreateRoom()
        {
            int port = Random.Range(1000, 12344);
            string ip = "127.0.0.1";
            roomDiscoverController.RegisterRoom(roomNameInput.text,ip,port);
        
            var server = new RoomServer();
            server.Connect(new RoomInfo(name,ip,port));
            chatView.Initialise(server);
            chatView.gameObject.SetActive(true);
        }

        private void AddRoom(RoomInfo roomInfo)
        {
            var instance = Instantiate(roomInfoPrefab, contentParent);
            instance.Display(roomInfo);
            instance.OnJoinClick += OnJoinClick;
        }

        private void OnJoinClick(RoomInfo obj)
        {
            var client = new RoomClient();
            client.Connect(obj);
            chatView.Initialise(client);
            chatView.gameObject.SetActive(true);
        }

        private void OnDestroy() => roomDiscoverController.OnRoomAdded -= AddRoom;
    }
}