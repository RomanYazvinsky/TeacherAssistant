using System;
using System.Collections.Generic;

namespace Containers {
    public class DragData {
        private Action _dragSuccess;
        public DragData(string senderType, string senderId, List<object> data, Action dragSuccess) {
            this.Data = data;
            this.SenderId = senderId;
            this.SenderType = senderType;
            _dragSuccess = dragSuccess ?? (() => {});
        }

        public void Accept() {
            _dragSuccess();
        }
        
        public string SenderType { get; set; }
        
        public string SenderId { get; set; }
        public List<object> Data { get; set; }
    }
}