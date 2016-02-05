window.onload = function () {
    var socket = new WebSocket('ws://localhost:8181');
    socket.onopen = function () {
        var x = 100000;
        var arr = new Array(100000);
        while (x--) arr[x] = x;        
        socket.send(arr.join(' '));
    };

    socket.onmessage = function (event) {
        alert(event.data);
    };
}
