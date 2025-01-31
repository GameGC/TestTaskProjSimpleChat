using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RoomInfoView  : MonoBehaviour
    {
        public event Action<RoomInfo> OnJoinClick;

        public TextMeshProUGUI text;
        public Button join;

        private RoomInfo _roomInfo;

        private void OnEnable() => join.onClick.AddListener(OnJoinClicked);

        private void OnDisable() => join.onClick.RemoveListener(OnJoinClicked);

        private void OnJoinClicked() => OnJoinClick?.Invoke(_roomInfo);

        public void Display(RoomInfo roomInfo)
        {
            _roomInfo = roomInfo;
            text.text = roomInfo.Ip+':'+roomInfo.Port + ',' + roomInfo.Name;
        }
    }
}