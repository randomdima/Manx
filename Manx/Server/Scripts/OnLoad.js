window.onload = function () {
    var cnv = document.createElement('canvas');
    cnv.style.width = '100%';
    cnv.style.height = '100%';
    document.body.appendChild(cnv);
    cnv.width = cnv.offsetWidth;
    cnv.height = cnv.offsetHeight;
    var context = cnv.getContext('2d');

    var socket = new WebSocket('ws://213.231.54.11:12397/ws');
    //var socket = new WebSocket('ws://192.168.0.118:12397/ws');
    var color = getRandomColor();
 
    socket.onmessage = function (e) {
        var pos = JSON.parse(e.data);
        context.beginPath();
        context.arc(pos.x, pos.y, 20, 0, 2 * Math.PI, false);
        context.fillStyle = pos.c;
        context.fill();
    };
    socket.onopen=function(){
        window.onmousedown=
        window.onmousemove=function(e)
        {
            if (e.buttons != 1) return;
            socket.send(JSON.stringify({ x: e.x, y: e.y, c: color }));
        }
    }
}

function getRandomColor() {
    var letters = '0123456789ABCDEF'.split('');
    var color = '#';
    for (var i = 0; i < 6; i++) {
        color += letters[Math.floor(Math.random() * 16)];
    }
    return color;
}