
const button = document.getElementById("button")
button.onclick = () => {
    const request = new XMLHttpRequest()
    request.onload = evt => {
        console.log("Ein Ereignis", evt)
    }
    const path = 'get?path=c:\\windows\\system32&isVisible=false'
    const encodedPath = encodeURI(path)
    request.open('Get', `Commander/${encodedPath}`, true)
    request.send()
}

const buttonSse = document.getElementById("buttonSse")
buttonSse.onclick = () => {
    var source = new EventSource("events")
    source.onmessage = event => console.log("onMsg", event.data)
    source.addEventListener("Ereignis", event => console.log("onEreignis", event.data))
}

