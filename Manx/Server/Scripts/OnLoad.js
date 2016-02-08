var pos;
window.onload = function () {
    document.body.style.overflow = 'hidden';
    document.body.style.padding = 0;
    document.body.style.margin = 0;
    var cnv = document.createElement('canvas');
    cnv.style.overflow = 'hidden';
    cnv.style.width = '100%';
    cnv.style.height = '100%';
    document.body.appendChild(cnv);
    cnv.width = cnv.offsetWidth;
    cnv.height = cnv.offsetHeight;
    var context = cnv.getContext('2d');

    var socket = new WebSocket('ws://'+window.location.host+'/ws');
   // var socket = new WebSocket('ws://localhost:8181');
    var color = getRandomColor();
 
    socket.onmessage = function (e) {
        //var now = new Date(); now = now.getMinutes()*60*1000+now.getSeconds()*1000 + now.getMilliseconds();
        //var parts = e.data.split(' ', 5);
        //var p1 = parseInt(parts[0]);
        //var p2 = parseInt(parts[1]);
        //var p3 = parseInt(parts[2]);
        //var p4 = parseInt(parts[3]);
        //console.log('       '+(p3 - p4) + '       ' + (p2 - p3) + '     ' + (p1 - p2) + '     ' + (now - p1));

        //var pos = JSON.parse(parts[4]);
        var pos = JSON.parse(e.data);
        context.beginPath();
        context.lineWidth = 10;
        context.lineCap = "round";
        context.strokeStyle = pos.c;
        context.moveTo(pos.lx, pos.ly);
        context.lineTo(pos.x, pos.y);
        context.stroke();
    };

    var lastp;
    socket.onopen=function(){
        document.body.addEventListener('mousedown', function (e) {
            lastp = e;
            //var now = new Date(); now = now.getMinutes() * 60 * 1000 + now.getSeconds() * 1000 + now.getMilliseconds();
            socket.send(JSON.stringify({ x: e.x, y: e.y, lx: e.x - 1, ly: e.y - 1, c: color }));
        });
        document.body.addEventListener('mousemove', function (e) {
            if (!e.buttons) return;
            //  var now = new Date(); now = now.getMinutes() * 60 * 1000 + now.getSeconds() * 1000 + now.getMilliseconds();
            socket.send(JSON.stringify({ x: e.x, y: e.y, lx: lastp.x, ly: lastp.y, c: color }));
            lastp = e;
        });
    }

    window.onresize = function () {
        cnv.width = cnv.offsetWidth;
        cnv.height = cnv.offsetHeight;
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