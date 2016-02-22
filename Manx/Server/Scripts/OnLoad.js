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

    var socket = new RPCClient('ws://' + window.location.host + '/ws');
    //socket.onError(function (e) { alert(e); });
    socket.start(start);
    window.onresize = function () {
        canvas.width = canvas.offsetWidth;
        canvas.height = canvas.offsetHeight;
    }
}

function start(world) {
    var socket = this;
    var selected;
    var xoff,yoff;
    window.world = world;
    //for (var q in world.Items)
    var item= world.Items[0];
    item.add_OnMove(function (x, y) {
        item.X = x;
        item.Y = y;
        draw(world, selected)
    });
    draw(world, selected);

    //world.onItemAdded(function (item) {
    //    world.Items.push(item);
    //    draw(world);
    //});
    //world.onItemRemoved(function (item) {
    //    var i = world.Items.indexOf(item);
    //    world.Items.splice(i, 1);
    //    draw(world);
    //});
    window.onmousemove = function (e) {
        if (!selected) return;
        selected.MoveTo(e.x - xoff, e.y - yoff);
    }
    window.onmouseup = function (e) { selected = null; }
    window.onmousedown = function (e) {
        selected = null;
        for (var q = world.Items.length; q--;) {
            var i = world.Items[q];
            xoff=e.x - i.X;
            yoff=e.y - i.Y;
            if ((xoff*xoff + yoff*yoff) < i.Size * i.Size) {
                selected = i;
                return;
            }
        }
    }
}
function draw(world,selected) {
    context.clearRect(0, 0, canvas.width, canvas.height);
    for (var q = 0; q < world.Items.length; q++)
        drawItem(world.Items[q], world.Items[q]==selected);
}

function drawItem(item, selected) {
    context.beginPath();
    context.arc(item.X, item.Y, item.Size, 0, 2 * Math.PI, false);
    context.fillStyle = item.Color;
    context.fill();
    context.lineWidth = 3;
    context.strokeStyle = 'black';
    if(selected)
    context.stroke();
}