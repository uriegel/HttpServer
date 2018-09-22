namespace WebServer

module Html =
    let get body = 
        """<!doctype html>
<html>
<head>
    <meta charset="utf-8" lang="de">
    <title>URiegel</title>
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <Style> 
    html {
        font-family: sans-serif;
    }
    h1 {
        font-weight: 100;
    }
    </Style>"
</head>
<body>""" + body + """</body>
</html>"""