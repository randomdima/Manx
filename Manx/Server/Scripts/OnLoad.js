window.onload = function () {
    var socket = new WebSocket('ws://localhost:8181');
    var s = '';
    for (var x = 0; x < 1000000; x++) s += ' ' + x;
    window.onclick=function()
    {

        socket.send(s);
    }
    socket.onmessage = function (event) {
        alert(event.data);
    };
}
