using Network;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    public class ChatView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI chatText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendBtn;
    
        private NetworkRoom _room;
        
        public void Initialise(NetworkRoom room) => _room = room;
    
        private void OnEnable()
        {
            _room.OnMessageReceived += OnMessageReceived;
            sendBtn.onClick.AddListener(SendMessage);
        }

        private void SendMessage()
        {
            _room.SendChatMessage(inputField.text);
           // OnMessageReceived(inputField.text);
            inputField.text = string.Empty;
        }

        private void OnMessageReceived(string obj) => chatText.text += $"\n{obj}";
    }
}