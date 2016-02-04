window.onload = function () {
    var canvas = document.createElement('canvas');
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    document.body.appendChild(canvas);

    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
    var context = canvas.getContext('2d');


    window.client = new wsClient('ws://localhost:8181');

    window.onmousemove = function (e) {
         client.exec(0, { x: e.x,y:e.y });
    }

    client.on('PositionChanged', function (pos) {
        context.clearRect(0, 0, canvas.width, canvas.height);
        context.beginPath();
        context.arc(pos.x, pos.y, 50, 0, 2 * Math.PI, false);
        context.fillStyle = 'green';
        context.fill();
    });
}
