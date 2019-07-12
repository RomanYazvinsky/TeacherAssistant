using System;
using System.Collections.Generic;

namespace Containers {
    public class DragData {
        private Action _dragSuccess;
        public DragData(object sender, List<object> data, Action dragSuccess) {
            this.Data = data;
            this.Sender = sender;
            _dragSuccess = dragSuccess ?? (() => {});
        }

        public void Accept() {
            _dragSuccess();
        }
        
        public object Sender { get; set; }
        public List<object> Data { get; set; }
    }
}