
const button = document.getElementById("button")
button.onclick = () => {
    const request = new XMLHttpRequest()
    request.onload = evt => {
        console.log("Ein Ereignis", evt)
    }
    const path = 'c:\\windows\\system32'
    const encodedPath = encodeURI(path)
    request.open('Get', `Commander?get=${encodedPath}`, true)
    request.send()
}