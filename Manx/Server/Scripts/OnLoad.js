window.onload = function () {
    document.body.style.overflow = 'hidden';
    document.body.style.padding = 0;
    document.body.style.margin = 0;
    window.canvas = document.createElement('canvas');
    canvas.style.overflow = 'hidden';
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    document.body.appendChild(canvas);
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
    window.context = canvas.getContext('2d');

    var socket = new RPCClient('ws://'+window.location.host+'/ws');
    socket.onError(function (e) { alert(e); });
    socket.onStart(start);
    socket.start();
    window.onresize = function () {
        canvas.width = canvas.offsetWidth;
        canvas.height = canvas.offsetHeight;
    }
}

function start(world) {
    window.world = world;
  //  world.Items[2].MoveTo(3, 4);
    draw(world);

    world.onItemAdded(function (item) {
        world.Items.push(item);
        draw(world);
    });
    world.onItemRemoved(function (item) {
        var i = world.Items.indexOf(item);
        world.Items.splice(i, 1);
        draw(world);
    });
    window.onclick = function (e) {
        for (var q = 0; q < world.Items.length; q++) {
            var i = world.Items[q];
            if (((e.x - i.X) * (e.x - i.X) + (e.y - i.Y) * (e.y - i.Y)) < i.Size * i.Size) {
                world.Remove(i);
                return;
            }
        }
        world.Add(e.x,e.y,Math.round(Math.random()*50)+20,getRandomColor());
    }
}
function draw(world) {
    context.clearRect(0, 0, canvas.width, canvas.height);
    for (var q = 0; q < world.Items.length; q++)
        drawItem(world.Items[q]);
}

function drawItem(item) {
    context.beginPath();
    context.arc(item.X, item.Y, item.Size, 0, 2 * Math.PI, false);
    context.fillStyle = item.Color;
    context.fill();
}

function getRandomColor() {
    var letters = '0123456789ABCDEF'.split('');
    var color = '#';
    for (var i = 0; i < 6; i++) {
        color += letters[Math.floor(Math.random() * 16)];
    }
    return color;
}