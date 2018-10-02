
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