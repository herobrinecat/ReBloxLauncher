process.stdout.write(
    String.fromCharCode(27) + "]0;RobloxAssetFixer Server" + String.fromCharCode(7)
)
process.stdout.write("\x1Bc")
console.log("\x1b[32m%s\x1b[0m", "<INFO> Starting server...")
//If the assetdeilvery/thumbnail server of Roblox decides to fail, the asset part will fail also.
const express = require("express");
const https = require("https");
const http = require("http")
const filesystem = require("fs");
const zlib = require("zlib")
const crypto = require("crypto")
const readline = require("readline")
const path = require("path")
const jwt = require("jsonwebtoken");
const e = require("express");
const delay = ms => new Promise(resolve => setTimeout(resolve, ms))

//Create express server
const app = express();

app.use(express.json({ limit: "500mb" }))
app.use(express.urlencoded({ extended: true, limit: "500mb" }))
app.use(express.raw({ limit: "500mb", type: '*/*' }))

//User variables
var username = "Player" // can be set with -username
var userId = 1 // can be set with -userid
var accountOver13 = true // can be set with -accountUnder13
var avatarR15 = false // can be set with -r15
var avatarBodyColor = [] //can be set with -bodycolor=[]
var joining = false //Use to make several function contact the host's server instead of simulating it...
var ip = "" // required for joining variable
var enableDataStore = true //Enable DataStore support in RBDF
var enableBadges = true //Enable Badges support in RBDF
var enableFollow = true //Enable Following support in RBDF
var enableFriendships = true //Enable Friendships (Friends) support in RBDF (2016L and below)
var enableOwnedAssets = true //Enable owned assets support in RBDF
var enableDataPersistence = true //Enable Data Persistence support in RBDF (Separate from DataStore)
var AllowGetCurrentUser = false //Sends the GetCurrentUser with the User ID instead of null
var RBDFpath = "./default.rbdf" //A path to the ReBlox Datastore File
var clothidsstring = "" //for clothing and assets for characters.
var robux = 5000 //Total amount of ROBUX

//Server variables
var assetfolder = "./assets" //A directory path where the server will look if there's any assets to use locally
var verbose = true //Gives out more info like what the server is doing
var useAuth = false //ROBLOSECURITY is needed to be filled out to use this, can get other assets apart from Decals (can be set with -useAuth)
var ROBLOSECURITY = "" //This is required to tell the difference between Decal and Image due to assetdelivery update (if useAuth is enabled) (can be set with -ROBLOSECURITY [please put this before -useAuth])
var saveFile = false //Saves the file that's not part of the file assets to the saved folder
var localClothes = true //Enables -clothes as argument to support multiple user ids (enabled by default)
var assetsFromServer = false //Makes the asset load from the server instead of emulating if joining (Asset packs for the server you're joining is no longer required!)
var useNewSignatureFormat = true //Uses the rbxsig format when signing scripts
var privateKey = "./../../private.pem" //A path to the private key for signing joinscripts/gameserver/etc.
var publicKeyObj = null
var useNewSignatureAssetFormat = true //replaces %ID% with --rbxassetid%ID%
var cloth2021finished = false //A little hacky way to fix an issue in node.js
var notWorkingAssetIds = [] //A list for the asset fetcher to not attempt fetching if they don't work.
var duplicateAssetIds = [] //A list for multiple files with the same id (for optimization reason)
var memoryUserBadge = [] // A list of users that got the badge id (to prevent spam!)
var memoryUsers = [] // A list of users that has connected (estimated)
var duplicateBool = undefined // A check if the assets are duplicated
var lastProductId = 0 //A number for /marketplace/validatepurchase
var isInternetAvailable = false //A check variable for internet
var isRobloxAvailable = false //In case Roblox is banned by your country/ISP!
var RBDFTextFormat = false //Use an older format for RBDF (Not secured for sensitive information, the magic number determines the format...)
var allowTCPLauncher = true //Allows communication between the server and the launcher via TCP, if you're not using the launcher or don't have the launcher, use --disableTCP
var allowUploadFiles = false //Allows uploading of files to your computer (This could pose security risks to your computer, so it's disabled by default)
var forceCanManageTrue = false //Forces the canmanage value to be true (Pre-0.0.1320 behavior)
var checkROBLOSECURITY = false //A mark for checking ROBLOSECURITY from launcher
var enableHTTPS = true //Enable support for HTTPS (Recommended!)

if (filesystem.existsSync(privateKey)) {
    publicKeyObj = crypto.createPublicKey({
        key: filesystem.readFileSync(privateKey),
        format: "pem"
    })
}

function getByteFromDataType(datatype) {
    switch (datatype) {
        case "DataStore": return 0x00;
        case "Badge": return 0x01;
        case "Following": return 0x02;
        case "Friendship": return 0x03;
        case "OwnedAsset": return 0x04;
        case "DataPersistence": return 0x05;
        default: return 0x06;
    }
}

function readBinaryRBDF(path, index) {
    if (typeof (path) == "string" && typeof (index) == "number") {
        if (path.endsWith(".rbdf")) {
            if (filesystem.existsSync(path)) {
                if (index > -1) {
                    try {
                        var data = filesystem.readFileSync(path)
                        var headerverified = false
                        if (Buffer.from([data[0], data[1], data[2], data[3]]).toString("utf8") == "RBDF") {
                            if (Buffer.from([data[4], data[5], data[6], data[7]]).readInt32BE() == 293712399) {
                                headerverified = true
                                const itemcount = Buffer.from([data[13], data[14], data[15], data[16]]).readInt32LE()
                                if (itemcount > 0) {
                                    if (index < itemcount) {
                                        var itemindex = 17
                                        var realindex = 0
                                        for (var i = 0; i < itemcount; i++) {
                                            if (realindex != index) {
                                                itemindex = itemindex + 17
                                                const gzipsize = Buffer.from([data[itemindex], data[itemindex + 1], data[itemindex + 2], data[itemindex + 3]]).readInt32BE()
                                            }
                                            else {
                                                var md5hexcombined = []
                                                for (var x = 0; x < 16; x++) {
                                                    md5hexcombined.push(data[itemindex + x])
                                                }
                                            }
                                        }
                                    }
                                    else {

                                    }
                                }
                            }
                        }
                    }
                    catch {

                    }
                }
            }
        }
    }
}
function removeBinaryRBDF(path, index) {
    if (typeof (path) == "string" && typeof (index) == "number") {
        if (path.endsWith(".rbdf")) {
            if (filesystem.existsSync(path)) {
                if (index > -1) {
                    var data = filesystem.readFileSync(path)
                    var headerverified = false
                    if (Buffer.from([data[0], data[1], data[2], data[3]]).toString("utf8") == "RBDF") {
                        if (Buffer.from([data[4], data[5], data[6], data[7]]).readInt32BE() == 293712399) {
                            headerverified = true
                            if (Buffer.from([data[8], data[9], data[10], data[11], data[12]])) {

                            }
                        }
                    }
                }
            }
        }
    }
}
function writeBinaryRBDF(path, content) {
    if (path.endsWith(".rbdf")) {
        if (filesystem.existsSync(path)) {
            if (content != undefined && typeof (content) == "string") {
                checkRBDFFormat(path).then((result) => {
                    if (result == "binary") {
                        var data = filesystem.readFileSync(path)
                        var hash = crypto.createHash("md5").update(content).digest("hex")

                        var split = content.split(' ', 2)

                        var dataByte = getByteFromDataType(split[0].slice(1))

                    }
                    else {
                        console.log("\x1b[31m%s\x1b[0m", "<ERROR> This appears to be a text-based version of RBDF or an invalid file, please try a different RBDF!")
                    }
                })

            }
        }
        else {
            var hash = crypto.createHash("md5").update(content).digest("hex")

            var split = content.split(' ', 2)
            var contentcompressed = zlib.gzipSync(Buffer.from(content, "utf8"))
            const dataByte = getByteFromDataType(split[0].slice(1))
            var magicNumber = Buffer.alloc(4)
            magicNumber.writeInt32BE(293712399)
            var totalLength = Buffer.alloc(5)
            totalLength.writeInt32BE(contentcompressed.length + 21)
            var contentLength = Buffer.alloc(4)
            contentLength.writeInt32BE(contentcompressed.length)
            var headerBuffer = Buffer.concat([Buffer.from("RBDF", "utf8"), magicNumber, totalLength, Buffer.from([0x01, 0x00, 0x00, 0x00])])
            var item = Buffer.concat([Buffer.from(hash, "hex"), Buffer.from([dataByte]), contentLength, contentcompressed])

            var combined = Buffer.concat([headerBuffer, item])

            filesystem.writeFileSync(path, combined)

            contentcompressed = null
            totalLength = null
            contentLength = null
            headerBuffer = null
            item = null
            combined = null
        }
    }
}

//writeBinaryRBDF("C:\\Users\\NOAA\\Documents\\binarytest.rbdf", "<Badge userId=1038712 badgeId=58278772>")
//readBinaryRBDF("C:\\Users\\NOAA\\Documents\\binarytest.rbdf", 0)
function trueRaw(req, res, next) {
    if (req.headers["content-type"] == "*/*") {
        req.body = new Promise(resolve => {
            var buf = [];
            req.on('data', x => buf.push(x));
            req.on('end', () => {
                resolve(Buffer.concat(buf));
            });
        });
        next();
    }
    else {
        next();
    }
}
function isNumeric(str) {
    if (typeof str != "string") return false // we only process strings!  
    return !isNaN(str) && // use type coercion to parse the _entirety_ of the string (`parseFloat` alone does not do this)...
        !isNaN(parseFloat(str)) // ...and ensure strings of whitespace fail
}

function parseISOString(s) {
    var b = s.split(/\D+/);
    return new Date(Date.UTC(b[0], --b[1], b[2], b[3], b[4], b[5], b[6]))
}

process.on('uncaughtException', (err) => {
    console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong while trying to process a request, this is usually due to the malformed request that the server can't handle or a bug, please check the error stack below!\n", err)
})

process.on('exit', (code) => {
    console.log("\x1b[0m", "")
})

process.argv.forEach(function (val) {
    if (val == "-useAuth") {
        if (useAuth == false) {
            if (ROBLOSECURITY.startsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|")) {
                //wow, roblox still uses the old way of saying Robux.
                useAuth = true
            }
            else {
                console.log("\x1b[33m%s\x1b[0m", "<WARN> A valid ROBLOSECURITY is required, disabling useAuth...")
                useAuth = false
            }
        }
    }
    else if (val.startsWith("-ROBLOSECURITY=")) {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Setting your ROBLOSECURITY via command line is generally insecure! We strongly recommend using --syncROBLOSECURITYfromLauncher (ReBlox 0.0.1320+)")
        ROBLOSECURITY = val.slice(15)
    }
    else if (val.startsWith("-username=")) {
        if (val.slice(10).length >= 3 && val.slice(10).length <= 20) {
            username = val.slice(10)
        }
        else {
            console.log("\x1b[33m%s\x1b[0m", "<WARN> A valid username is required if you're using -username, using \"Player\" instead...")
        }
    }
    else if (val.startsWith("-userid=")) {
        if (isNumeric(val.slice(8))) {
            userId = parseInt(val.slice(8))
        }
        else {
            console.log("\x1b[33m%s\x1b[0m", "<WARN> A valid UserId is required if you're using -userid, using 1 instead");
        }
    }
    else if (val == "-accountUnder13") {
        accountOver13 = false
    }
    else if (val == "-r15") {
        avatarR15 = true
    }
    else if (val.startsWith("-bodycolor=")) {
        avatarBodyColor = val.slice(12).slice(0, -1).split(',')
    }
    else if (val.startsWith("-clothes=")) {
        clothidsstring = val.slice(10).slice(0, -1)
    }
    else if (val == "-clothes") {
        if (filesystem.existsSync("./clothes")) {
            localClothes = true
        }
        else {
            localClothes = false
        }
    }
    else if (val.startsWith("-ip=")) {
        ip = val.slice(4)
    }
    else if (val == "-joining") {
        if (ip != "") {
            joining = true
        }
    }
    else if (val == "-disableDataStore") {
        enableDataStore = false
    }
    else if (val == "-disableBadges") {
        enableBadges = false
    }
    else if (val == "-disableFollowing") {
        enableFollow = false
    }
    else if (val == "-disableOwnedAssets") {
        enableOwnedAssets = false
    }
    else if (val == "-disableFriendships") {
        enableFriendships = false
    }
    else if (val.startsWith("-rbdf=")) {
        RBDFpath = val.slice(6).slice(0, val.length - 6)
    }
    else if (val == "-assetFromServer") {
        assetsFromServer = true
    }
    else if (val == "-disableNewSignature") {
        useNewSignatureFormat = false
    }
    else if (val == "-disableNewSignatureAsset") {
        useNewSignatureAssetFormat = false
    }
    else if (val.trim().startsWith("-robux=")) {
        if (isNumeric(val.trim().slice(7))) {
            robux = parseInt(val.trim().slice(7))
        }
    }
    else if (val == "--allowGetCurrentUser") {
        AllowGetCurrentUser = true
    }
    else if (val == "--useRBDFTextFormat") {
        RBDFTextFormat = true
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Even though the text format will remain and continue to be supported in the server for compatibility reason, it is generally unsecure for sensitive information. (However, the text format will still be updated along with the binary file format, but if you're looking at the old RBDF files, it will be auto-detected as a text format, so most of the time you won't need --useRBDFTextFormat. This will also mark the binary format as invalid.)")
    }
    else if (val == "--disableTCP") {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Any changes that occurs that requires a change to the launcher will not be saved. Due to this, it will only be saved in this session only.")
        allowTCPLauncher = false
    }
    else if (val == "--allowUploadingFiles") {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Allowing clients to upload files to your computer can contains security risks (viruses, inappropriate files, etc). It is recommended that you turn this off if you're running a public server.")
        allowUploadFiles = true
    }
    else if (val == "--forceCanManageTrue") {
        forceCanManageTrue = true
    }
    else if (val == "--syncROBLOSECURITYfromLauncher") {
        if (allowTCPLauncher) {
            checkROBLOSECURITY = true
        }
        else {
            console.log("\x1b[31m%s\x1b[0m", "<ERROR> You can't contact the launcher as TCP communication is disabled by command line. Please remove --disableTCP from your command line and restart the server!")
        }
    }
    else if (val == "-disableDataPersistence") {
        enableDataPersistence = false
    }
    else if (val == "-help") {
        console.log("\r\n<INFO> Usage for RobloxAssetFixer:\r\n\r\n-ROBLOSECURITY=\"roblosecurity\" - Set your ROBLOSECURITY (required for useAuth)\r\n-useAuth - Set the asset retrieval to use Roblox's servers that requires auth\r\n-username= - Set your player's username\r\n-userid= - Set your player's userid\r\n-accountUnder13 - Mark your account <13\r\n-r15 - Set your avatar to be R15\r\n-bodycolor=[0,0,0,0,0,0] - Set your body color of your avatar (deprecated)\r\n-clothes=[] - Set the asset ids of your avatar for customzation (deprecated)\r\n-ip= - Set an IP to the server if you're joining (required for -joining)\r\n-joining - Mark the server as joining and make several functions connect to the host's server instead of simulating it (-ip required)\r\n-disableDataStore - Disable saving/loading of data via DataStore with RBDF\n-disableBadges - Disable saving badges with RBDF\r\n-disableFollowing - Disable saving followers with RBDF\r\n-rbdf=\"path\" - A path to a ReBlox Datastore File\r\n-assetFromServer - Makes the asset link attempt to contact the local server you're joining, use Roblox's server as fallback (requires -ip and -joining)\r\n-disableNewSignature - use %DATA% instead of --rbxsig%DATA% (required for 2013M and older)\r\n-disableNewSignatureAsset - makes the format for script signing %ID% instead of --rbxassetid%ID%\r\n-disableOwnedAssets - Disables saving owned assets with RBDF\r\n-robux=amount - Set the amount of ROBUX you have.\r\n--disableTCP - Disables communication between the server and the launcher.\r\n-disableDataPersistence - Disables saving/loading data via Data Persistence with RBDF")
        process.exit(0)
    }
})

checkInternet().then((result) => {
    isInternetAvailable = result
    if (result == false && useAuth) {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> It appears that you have no internet connection, disabling useAuth...")
        useAuth = false
    }
    else if (result && useAuth && isRobloxAvailable == false) {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> It appears that you have an internet connection, however we're unable to contact Roblox's servers (most likely because of your ISP or your country's ban), disabling useAuth...")
        useAuth = false
    }
    if (checkROBLOSECURITY == true) getROBLOSECURITYfromLauncher(1)
})

console.log("<INFO> Username: " + username)
console.log("<INFO> User ID: " + userId)
console.log("<INFO> Account Age: " + ((accountOver13) ? "13+" : "<13"))
console.log("<INFO> Avatar Type: " + ((avatarR15) ? "R15" : "R6"))
if (checkROBLOSECURITY == false) console.log("<INFO> Using Auth: " + useAuth.toString())
if (joining && ip != "") console.log("<INFO> Server IP: " + ip)

function getROBLOSECURITYfromLauncher(times) {
    if (checkROBLOSECURITY == true && isInternetAvailable == true && isRobloxAvailable == true) {
        if (times < 6) {
            const { Socket } = require("net")

            const client = new Socket()

            var options = {
                host: "127.0.0.1",
                port: 50355
            }
            client.connect(options, () => {
                if (verbose) { console.log("\x1b[34m%s\x1b[0m", "<INFO> Connected to the launcher via TCP!") }
                var roblosecurityfromlauncher = ""
                client.setEncoding("utf8")
                client.on("data", (chunk) => {
                    roblosecurityfromlauncher += chunk
                })

                client.on("end", () => {
                    if (Buffer.from(roblosecurityfromlauncher, "base64").toString("utf8").startsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|")) {
                        ROBLOSECURITY = Buffer.from(roblosecurityfromlauncher, "base64").toString("utf8")
                        if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Successfully synced ROBLOSECURITY with the launcher! Turning on useAuth...")
                        useAuth = true
                    }
                    else if (roblosecurityfromlauncher == "invalid") {
                        getROBLOSECURITYfromLauncher(times + 1)
                    }
                    else {
                        if (verbose) console.log("\x1b[31m%s\x1b[0m", "<ERROR> Received an invalid ROBLOSECURITY from the launcher!")
                    }
                })
            })

            client.write(Buffer.concat([Buffer.from((276312498).toString(16), "hex"), Buffer.from("GRS", "utf8"), Buffer.from(getTimestamp(), "utf8")]))
            client.end()
        }
        else {
            console.log("\x1b[31m%s\x1b[0m", "<ERROR> We're unable to sync the ROBLOSECURITY! Check your date/time settings.")
        }
    }
}
function getTimestamp() {
    var date = new Date(Date.now())

    return date.getUTCFullYear().toString().padEnd(4, "0") + (date.getUTCMonth() + 1).toString().padStart(2, "0") + date.getUTCDate().toString().padStart(2, 0) + date.getUTCHours().toString().padStart(2, "0") + date.getUTCMinutes().toString().padStart(2, "0") + date.getUTCSeconds().toString().padStart(2, "0")

}

if (verbose) { app.use((req, res, next) => { console.log("\x1b[32m%s\x1b[0m", "<INFO> " + req.ip + " requested: \"" + req.protocol + "://" + req.get("host") + req.originalUrl + "\" (" + req.method + ")"); next(); }) }

function randomUUID() {
    return crypto.randomBytes(4).toString("hex") + "-" + crypto.randomBytes(2).toString("hex") + "-" + crypto.randomBytes(2).toString("hex") + "-" + crypto.randomBytes(2).toString("hex") + "-" + crypto.randomBytes(6).toString("hex")
}

async function checkInternet() {
    //It's kinda hacky and weird, but hey! At least it works.
    try {
        var result = false
        await new Promise((resolve, reject) => {

            const options = {
                host: "gstatic.com",
                path: "/generate_204",
                port: 443,
                method: "GET",
                timeout: 2000
            }

            const req1 = https.request(options, (res) => {

                const options2 = {
                    host: "assetdelivery.roblox.com",
                    path: "/",
                    port: 443,
                    method: "GET",
                    timeout: 2000
                }

                const req2 = https.request(options2, (res1) => {
                    isRobloxAvailable = true
                    result = true
                    resolve()

                })
                req2.on("error", () => {
                    isRobloxAvailable = false
                    result = true
                    resolve()
                })
                req2.end()
            })
            req1.on("error", () => {
                result = false
                resolve()
            })

            req1.end()
        })

        return result
    } catch {
        return false;
    }
}

function getAsset(id, callback) {
    try {
        id = id.trim()
        if (id.includes("www.roblox.com")) id = id.toLowerCase().replace("http://www.roblox.com/asset/?id=", "");
        if (id.includes("www.reblox.zip")) id = id.toLowerCase().replace("http://www.reblox.zip/asset/?id=", "");
        if (notWorkingAssetIds.indexOf(id) > -1) {
            console.log("\x1b[33m%s\x1b[0m", "<WARN> Ignoring asset " + id + " assuming it's broken at this moment.")
            return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
        } else {
            if (useAuth == false && isRobloxAvailable == true) {
                const options = {
                    host: "assetdelivery.roblox.com",
                    port: 443,
                    path: "/v2/asset/?id=" + id,
                    method: "GET",
                    headers: {
                        "User-Agent": "RobloxStudio/WinInet"
                    }
                }

                https.get(options, function (res) {
                    res.setEncoding("utf8")

                    var result = ""
                    res.on("data", (chunk) => {
                        result += chunk
                    })

                    res.on("end", () => {
                        const jsonresult = JSON.parse(result)

                        if (jsonresult["locations"] != undefined) {
                            const splitted = jsonresult["locations"][0]["location"].split('/', 3)

                            const options2 = {
                                host: splitted[2],
                                port: 443,
                                path: jsonresult["locations"][0]["location"].replace("https://" + splitted[2], ""),
                                method: "GET"
                            }

                            https.get(options2, (res1) => {
                                var data = [], output

                                if (res1.headers["content-encoding"] == "gzip") {
                                    if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from AssetDelivery (gzip compression)"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                    const gzip = zlib.createGunzip()
                                    res1.pipe(gzip)
                                    output = gzip
                                }
                                else if (res.headers["content-encoding"] == "deflate") {
                                    if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from AssetDelivery (deflate compression)"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                    const deflate = zlib.createDeflate()
                                    res1.pipe(deflate)
                                    output = deflate
                                }
                                else {
                                    if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from AssetDelivery"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                    output = res1
                                }

                                output.on("data", (chunk) => {
                                    data.push(chunk)
                                })

                                output.on("end", () => {
                                    const buffer = Buffer.concat(data)

                                    if (jsonresult["assetTypeId"] == 13) {
                                        const buffertext = data.toString()
                                        const regex = new RegExp("<url>(.*)<\/url>")
                                        const match = regex.exec(buffertext)

                                        if (match != null && match.length > 0) {
                                            getAsset(match[1], function (data1) {
                                                return callback(data1)
                                            })
                                        }
                                    }
                                    else {
                                        return callback(buffer)
                                    }
                                })
                            }).on("error", () => {
                                console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong whlie trying to contact the Roblox server!")
                                return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
                            })
                        }
                        else {
                            if (verbose) console.log("\x1b[33m%s\x1b[0m", "<WARN> AssetDelivery attempt failed without auth, decals will only be supported.");
                            const options2 = {
                                host: "apis.roblox.com",
                                port: 443,
                                path: "/toolbox-service/v2/assets/" + id,
                                method: "GET"
                            }
                            https.get(options2, (res1) => {
                                var result = ""

                                res1.setEncoding("utf8")

                                res1.on("data", (chunk) => {
                                    result += chunk
                                })

                                res1.on("end", () => {
                                    var parsedJSON = JSON.parse(result)

                                    if (parsedJSON["asset"] != undefined) {
                                        if (parsedJSON["asset"]["assetTypeId"] == 13) {
                                            const options3 = {
                                                host: "thumbnails.roblox.com",
                                                port: 443,
                                                path: "/v1/assets?assetIds=" + id + "&returnPolicy=PlaceHolder&size=700x700&format=Png&isCircular=false",
                                                method: "GET"
                                            }

                                            https.get(options3, (res2) => {
                                                var result = ""

                                                res2.setEncoding("utf8")

                                                res2.on("data", (chunk) => {
                                                    result += chunk
                                                })

                                                res2.on("end", () => {
                                                    const parsedThumbnails = JSON.parse(result)

                                                    if (parsedThumbnails["data"].length > 0) {
                                                        const splitted = parsedThumbnails["data"][0]["imageUrl"].split('/', 3)
                                                        const options4 = {
                                                            host: splitted[2],
                                                            port: 443,
                                                            path: parsedThumbnails["data"][0]["imageUrl"].replace("https://" + splitted[2], ""),
                                                            method: "GET"
                                                        }

                                                        https.get(options4, (res3) => {
                                                            var data = [], output

                                                            if (res3.headers["content-encoding"] == "gzip") {
                                                                if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Thumbnails API (gzip compression)"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                                                const gzip = zlib.createGunzip()
                                                                res3.pipe(gzip)
                                                                output = gzip
                                                            }
                                                            else if (res3.headers["content-encoding"] == "deflate") {
                                                                if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Thumbnails API (deflate compression)"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                                                const deflate = zlib.createDeflate()
                                                                res3.pipe(deflate)
                                                                output = deflate
                                                            }
                                                            else {
                                                                if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Thumbnails API"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                                                output = res3
                                                            }

                                                            output.on("data", (chunk) => {
                                                                data.push(chunk)
                                                            })

                                                            output.on("end", () => {
                                                                const buffer = Buffer.concat(data)

                                                                return callback(buffer)
                                                            })
                                                        })
                                                    }
                                                    else {
                                                        return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
                                                    }
                                                })
                                            })
                                        }
                                        else {
                                            return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
                                        }
                                    }
                                })
                            })
                        }
                    })
                })
            }
            else if (isRobloxAvailable == false) {
                console.log("\x1b[31m%s\x1b[0m", "<ERROR> You can't download assets while the server is in offline mode! Please restart the server once you're connected to the internet.")
                return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
            }
            else if (useAuth == true && isRobloxAvailable == true) {
                const options = {
                    host: "assetdelivery.roblox.com",
                    port: 443,
                    path: "/v2/asset/?id=" + id,
                    method: "GET",
                    headers: {
                        "Cookie": ".ROBLOSECURITY=" + ROBLOSECURITY,
                        "User-Agent": "RobloxStudio/WinInet"
                    }
                }

                https.get(options, (res) => {
                    res.setEncoding("utf8")

                    var result = ""
                    res.on("data", (chunk) => {
                        result += chunk
                    })

                    res.on("end", () => {

                        if (res.headers["set-cookie"] != undefined) {
                            var regex1 = new RegExp("\.ROBLOSECURITY=(_\|WARNING:-DO-NOT-SHARE-THIS\.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items\.\|_)(.*)(;)")
                            var match = regex1.exec(res.headers["set-cookie"])
                            if (match.length > 0) {
                                ROBLOSECURITY = match[1].concat(match[2]).split(';')[0]
                                changeROBLOSECURITYOnLauncher(match[1].concat(match[2]).split(';')[0])
                            }
                        }

                        const jsonresult = JSON.parse(result)

                        if (jsonresult["locations"] != undefined) {
                            const splitted = jsonresult["locations"][0]["location"].split('/', 3)

                            const options2 = {
                                host: splitted[2],
                                port: 443,
                                path: jsonresult["locations"][0]["location"].replace("https://" + splitted[2], ""),
                                method: "GET"
                            }

                            https.get(options2, (res1) => {
                                var data = [], output

                                if (res1.headers["content-encoding"] == "gzip") {
                                    if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from AssetDelivery (gzip compression)"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                    const gzip = zlib.createGunzip()
                                    res1.pipe(gzip)
                                    output = gzip
                                }
                                else if (res.headers["content-encoding"] == "deflate") {
                                    if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from AssetDelivery (deflate compression)"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                    const deflate = zlib.createDeflate()
                                    res1.pipe(deflate)
                                    output = deflate
                                }
                                else {
                                    if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from AssetDelivery"); else console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + id + " from Roblox server");
                                    output = res1
                                }

                                output.on("data", (chunk) => {
                                    data.push(chunk)
                                })

                                output.on("end", () => {
                                    const buffer = Buffer.concat(data)

                                    if (jsonresult["assetTypeId"] == 13) {
                                        const buffertext = data.toString()
                                        const regex = new RegExp('<url>(.*)<\/url>')
                                        const match = regex.exec(buffertext)

                                        if (match != null && match.length > 0) {
                                            getAsset(match[1], function (data1) {
                                                return callback(data1)
                                            })
                                        }
                                        else {
                                            return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
                                        }
                                    }
                                    else {
                                        return callback(buffer)
                                    }
                                })
                            }).on("error", () => {
                                console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong whlie trying to contact the Roblox server!")
                                return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
                            })
                        }
                        else {
                            notWorkingAssetIds.push(id)
                            return callback(result)
                        }
                    })
                })
            }
        }
    }
    catch {
        console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong when trying to download asset " + id + " from the Roblox server!")
        return callback("{\"errors\":[{\"code\":0,\"message\":\"Something went wrong\"}]}")
    }
}
app.get("/", (req, res) => {
    res.send("OK")
})

app.get("/games", (req, res) => {
    res.send("OK")
})
app.get("/favicon.ico", (req, res) => {
    if (filesystem.existsSync("./favicon.ico")) res.status(200).send(filesystem.readFileSync("./favicon.ico")); else res.status(404).end();
})

function calculateDuplicateFiles(name, dirName) {
    if (typeof (name) == "string" && typeof (dirName) == "string" && duplicateBool != false) {
        if (duplicateAssetIds.length > 0) {
            if (duplicateAssetIds.includes(name)) {
                return 2
            }
            else {
                return 1
            }
        }
        else {
            if (filesystem.existsSync(dirName)) {
                var count = 0
                var lastfilename = ""
                filesystem.readdirSync(dirName).forEach((file) => {
                    var splitted = file.split('.')
                    if (splitted[0] == name) {
                        count++
                    }
                    if (lastfilename == splitted[0] && duplicateAssetIds.includes(splitted[0]) == false) duplicateAssetIds.push(splitted[0])
                    lastfilename = splitted[0]
                })
                if (duplicateAssetIds.length > 0) duplicateBool = true; else duplicateBool = false;
                return count
            }
            else {
                return 0
            }
        }
    }
    else {
        return 0
    }
}


async function checkRBDFFormat(path) {
    if (path != undefined) {
        if (filesystem.existsSync(path)) {
            var data = filesystem.readFileSync(path)
            if (data[0] == 0x52 && data[1] == 0x42 && data[2] == 0x44 && data[3] == 0x46) {
                if (parseInt(data[4].toString(16).padStart(2, 0) + data[5].toString(16).padStart(2, 0) + data[6].toString(16).padStart(2, 0) + data[7].toString(16).padStart(2, 0), 16) == 293712399) {
                    return "binary"
                }
                else {
                    return "text"
                }
            }
            else {
                return "invalid"
            }
            data = null
        }
    }
    else {
        if (filesystem.existsSync(RBDFpath)) {
            var data = filesystem.readFileSync(RBDFpath)
            if (data[0] == 0x52 && data[1] == 0x42 && data[2] == 0x44 && data[3] == 0x46) {
                if (parseInt(data[4].toString(16).padStart(2, 0) + data[5].toString(16).padStart(2, 0) + data[6].toString(16).padStart(2, 0) + data[7].toString(16).padStart(2, 0), 16) == 293712399) {
                    return "binary"
                }
                else {
                    return "text"
                }
            }
            else {
                return "invalid"
            }
            data = null
        }
    }
}

app.get("/asset", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(req.query.id)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.id.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.id.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.id + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.id,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.length < 60) {
                                    if (buffer.toString().startsWith("{\"errors\"")) {
                                        getAsset(req.query.id, (result) => {
                                            if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                            res.send(result)
                                        })
                                        return
                                    }
                                    else {
                                        if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.status(res1.statusCode).send(buffer)
                                        return
                                    }
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.id, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.ID)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.ID.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%" + req.query.ID + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%\r\n%" + req.query.ID + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.ID.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%" + req.query.ID + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%\r\n%" + req.query.ID + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%" + req.query.ID + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%\r\n%" + req.query.ID + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.ID + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.ID,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.ID, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.ID, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.assetversionid)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.assetversionid.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from uploads folder (Asset)")
                }
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetversionid.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
                    }
                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }
                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetversionid + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.assetversionid,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.ID, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.assetversionid, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }

    }
    else {
        res.status(404).end()
    }
})

app.get("/asset/", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(req.query.id)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.id.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.id.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.id + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.id,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.ID, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.id, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.ID)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.ID.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%" + req.query.ID + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%\r\n%" + req.query.ID + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.ID.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%" + req.query.ID + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%\r\n%" + req.query.ID + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%" + req.query.ID + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.ID + "%\r\n" : "%\r\n%" + req.query.ID + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.ID + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.ID,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.ID + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.ID, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.ID, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.assetversionid)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.assetversionid.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from uploads folder (Asset)")
                }
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetversionid.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
                    }
                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }
                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetversionid + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.assetversionid,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.id, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.assetversionid, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }

    }
    else {
        res.status(404).end()
    }
})


app.get("//asset/", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(req.query.id)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.id.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.id.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.id + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.id,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.id, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.id, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.assetversionid)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.assetversionid.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from uploads folder (Asset)")
                }
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetversionid.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
                    }
                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }
                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetversionid + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.assetversionid,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)

                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.id, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.assetversionid, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }

    }
    else {
        res.status(404).end()
    }
})

app.get("/v1/asset", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(req.query.id)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.id.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.id.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.id + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.id,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.id, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.id, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.assetversionid)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.assetversionid.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from uploads folder (Asset)")
                }
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetversionid.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
                    }
                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }
                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetversionid + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.assetversionid,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)

                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.id, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.assetversionid, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }

    }
    else {
        res.status(404).end()
    }
})

app.get("/v1/asset/", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(req.query.id)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.id.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from uploads folder (Asset)")
                }

                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.id.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
                    }

                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }

                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.id + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.id,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.length < 60) {
                                    if (buffer.toString().startsWith("{\"errors\":")) {
                                        getAsset(req.query.id, (result) => {
                                            if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; statusCode = 400; }
                                            res.send(result)
                                        })
                                        return
                                    }
                                    else {
                                        if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.status(res1.statusCode).send(buffer)
                                        return
                                    }
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.id, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }
    }
    else if (isNumeric(req.query.assetversionid)) {
        var duplicatecount = 0
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.assetversionid.toString().trim()) {
                if (verbose) {
                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from uploads folder (Asset)")
                }
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                if (file.endsWith(".lua")) {
                    res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync("./uploads/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync("./uploads/" + file, "utf8"))
                }
                else {
                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                }
                assetfound1 = true
                return
            }
        })
        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetversionid.toString().trim()) {
                    if (verbose) {
                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
                    }
                    if (calculateDuplicateFiles(splitted[0], assetfolder) > 1) {
                        if (duplicatecount == 0 && (splitted[1] == "png" || splitted[1] == "jpg" || splitted[1] == "jpeg" || splitted[1] == "bmp")) {
                            duplicatecount++
                            //do nothing for christ sake
                        }
                        else {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            if (file.endsWith(".lua")) {
                                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                            }
                            else {
                                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            }
                            assetfound = true
                            return
                        }
                    }
                    else {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        if (file.endsWith(".lua")) {
                            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                        }
                        assetfound = true
                        return
                    }
                }
            })
            if (assetfound == false) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetversionid + "\"")
                if (assetsFromServer && joining) {
                    try {
                        var options = {
                            host: ip,
                            port: 80,
                            path: "/asset?id=" + req.query.assetversionid,
                            method: "GET"
                        }

                        http.get(options, (res1) => {
                            var data = [], output
                            if (res1.headers["content-encoding"] == 'gzip') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                                }
                                var gzip = zlib.createGunzip()
                                res1.pipe(gzip)
                                output = gzip
                            }
                            else if (res1.headers["content-encoding"] == 'deflate') {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                                }
                                var deflate = zlib.createDeflate()
                                res1.pipe(deflate)
                                output = deflate
                            }
                            else {
                                if (verbose) {
                                    console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                                }
                                output = res1
                            }
                            output.on("data", (chunk) => {
                                data.push(chunk)
                            })
                            output.on("end", () => {
                                var buffer = Buffer.concat(data)
                                if (buffer.toString("utf8").startsWith("{\"errors\":")) {
                                    getAsset(req.query.id, (result) => {
                                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                        res.send(result)
                                    })
                                    return
                                }
                                else {
                                    if (buffer.toString("utf8").startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                                    res.status(res1.statusCode).send(buffer)
                                    return
                                }
                            })
                        })

                    } catch {
                        res.status(500).end()
                        return
                    }
                }
                else {
                    getAsset(req.query.assetversionid, (result) => {
                        if (typeof (result) == "string" && result.startsWith("{\"errors\":")) { res.removeHeader("Content-disposition"); res.setHeader("content-type", "application/json; charset=utf-8"); res.statusCode = 400; }
                        res.send(result)
                        return
                    })
                }
            }
        }

    }
    else {
        res.status(404).end()
    }
})

function translateAssetTypeToAssetTypeId(assetType) {
    switch (assetType) {
        case "Image": return 1;
        case "TShirt": return 2;
        case "Mesh": return 3;
        case "Lua": return 5;
        case "Hat": return 8;
        case "Place": return 9;
        case "Decal": return 13;
        case "Animation": return 24;
        case "Shirt": return 11;
        case "Pants": return 12;
        case "Model": return 10;
        case "Video": return 62;
        case "Audio": return 3;
        case "Face": return 18;
        case "Head": return 17;
        case "MeshPart": return 40;
        case "Badge": return 21;
        case "GamePass": return 34;
        case "Package": return 32;
        case "EmoteAnimation": return 61;
        case "Plugin": return 38;
        case "HairAccessory": return 41;
        case "FaceAccessory": return 42;
        case "NeckAccessory": return 43;
        case "ShoulderAccesory": return 44;
        case "FrontAccessory": return 45;
        case "BackAccessory": return 46;
        case "WaistAccessory": return 47;
        default: return 10;
    }
}

function translateAssetTypeIdToAssetType(assetTypeId) {
    switch (assetTypeId) {
        case 1: return "Image";
        case 2: return "TShirt";
        case 3: return "Mesh";
        case 5: return "Lua";
        case 8: return "Hat";
        case 9: return "Place";
        case 13: return "Decal";
        case 24: return "Animation";
        case 11: return "Shirt";
        case 12: return "Pants";
        case 10: return "Model";
        case 62: return "Video";
        case 3: return "Audio";
        case 18: return "Face";
        case 17: return "Head";
        case 40: return "MeshPart";
        case 21: return "Badge";
        case 34: return "GamePass";
        case 32: return "Package";
        case 61: return "EmoteAnimation";
        case 38: return "Plugin";
        case 41: return "HairAccessory";
        case 42: return "FaceAccessory";
        case 43: return "NeckAccessory";
        case 44: return "ShoulderAccesory";
        case 45: return "FrontAccessory";
        case 46: return "BackAccessory";
        case 47: return "WaistAccessory";
        default: return "Model";
    }
}

async function createBatchResponse(request) {
    var edit = ""
    if (filesystem.existsSync("./assettype.json")) assetTypeJSON = JSON.parse(filesystem.readFileSync("./assettype.json", "utf8"))
    request.forEach((requestAsset) => {
        if (filesystem.existsSync("./assettype.json") || requestAsset["assetType"] != undefined) {
            if (requestAsset["assetType"] != undefined) {
                if (requestAsset != request[request.length - 1]) {
                    edit = edit + "{\"location\": \"" + ((requestAsset["assetType"] == "Image") ? "http://reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + requestAsset["assetId"] + "&wd=700&ht=700&fmt=png" : "http://assetdelivery.reblox.zip/v1/asset/?id=" + requestAsset["assetId"]) + "\", \"requestId\": \"" + requestAsset["requestId"] + "\", \"isArchived\":false, \"assetTypeId\": " + translateAssetTypeToAssetTypeId(requestAsset["assetType"]).toString() + ", \"isRecordable\": true }, "
                }
                else {
                    edit += "{\"location\": \"" + ((requestAsset["assetType"] == "Image") ? "http://reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + requestAsset["assetId"] + "&wd=700&ht=700&fmt=png" : "http://assetdelivery.reblox.zip/v1/asset/?id=" + requestAsset["assetId"]) + "\", \"requestId\": \"" + requestAsset["requestId"] + "\", \"isArchived\":false, \"assetTypeId\": " + translateAssetTypeToAssetTypeId(requestAsset["assetType"]).toString() + ", \"isRecordable\": true }"
                }
            }
            else {
                if (requestAsset != request[request.length - 1]) {
                    edit += "{\"location\": \"" + ((getAssetType(requestAsset["assetId"]) == 1) ? "http://reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + requestAsset["assetId"] + "&wd=700&ht=700&fmt=png" : "http://assetdelivery.reblox.zip/v1/asset/?id=" + requestAsset["assetId"]) + "\", \"requestId\": \"" + requestAsset["requestId"] + "\", \"isArchived\":false, \"assetTypeId\": " + getAssetType(requestAsset["assetId"]).toString() + ", \"isRecordable\": true }, "
                }
                else {
                    edit += "{\"location\": \"" + ((getAssetType(requestAsset["assetId"]) == 1) ? "http://reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + requestAsset["assetId"] + "&wd=700&ht=700&fmt=png" : "http://assetdelivery.reblox.zip/v1/asset/?id=" + requestAsset["assetId"]) + "\", \"requestId\": \"" + requestAsset["requestId"] + "\", \"isArchived\":false, \"assetTypeId\": " + getAssetType(requestAsset["assetId"]).toString() + ", \"isRecordable\": true }"
                }
            }
        }
        else {
            if (requestAsset != request[request.length - 1]) {
                edit += "{\"location\": \"" + ((getAssetType(requestAsset["assetId"]) == 1) ? "http://reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + requestAsset["assetId"] + "&wd=700&ht=700&fmt=png" : "http://assetdelivery.reblox.zip/v1/asset/?id=" + requestAsset["assetId"]) + "\", \"requestId\": \"" + requestAsset["requestId"] + "\", \"isArchived\":false, \"assetTypeId\": " + getAssetType(requestAsset["assetId"]).toString() + ", \"isRecordable\": true }, "
            }
            else {
                edit += "{\"location\": \"" + ((getAssetType(requestAsset["assetId"]) == 1) ? "http://reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + requestAsset["assetId"] + "&wd=700&ht=700&fmt=png" : "http://assetdelivery.reblox.zip/v1/asset/?id=" + requestAsset["assetId"]) + "\", \"requestId\": \"" + requestAsset["requestId"] + "\", \"isArchived\":false, \"assetTypeId\": " + getAssetType(requestAsset["assetId"]).toString() + ", \"isRecordable\": true }"
            }
        }
    })

    return "[" + edit + "]"
}

app.post("/v1/assets/batch", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")

    createBatchResponse(req.body).then((result) => {
        res.status(200).send(result)
    })
})

app.get("/Thumbs/Avatar.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"avatar.png\"")
    if (req.query.userId != undefined && isNumeric(req.query.userId)) {
        if (filesystem.existsSync("./renders/" + req.query.userId + ".png")) {
            res.status(200).send(filesystem.readFileSync("./renders/" + req.query.userId + ".png"))
        }
        else {
            res.status(200).send(filesystem.readFileSync(assetfolder + "/avatar.png"))
        }
    }
    else {
        res.status(200).send(filesystem.readFileSync(assetfolder + "/avatar.png"))
    }
})

app.get("/avatar-thumbnail/image", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"avatar.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/avatar.png"))
})


app.get("/Thumbs/GameIcon.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(req.query.assetId)) {
        if (verbose) {
            console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server/file (Image)")
        }
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == req.query.assetId.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                assetfound1 = true
                return
            }
        })

        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetId.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                    res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                    res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                    assetfound = true
                    return
                }
            })
            if (assetfound == false) {
                var options1 = {
                    host: 'thumbnails.roblox.com',
                    port: 443,
                    path: '/v1/assets?assetIds=' + req.query.assetId + '&returnPolicy=PlaceHolder&size=' + req.query.width + 'x' + req.query.height + '&format=png',
                    method: "GET"
                }
                var infoiresult = ""
                https.get(options1, (res2) => {

                    res2.setEncoding("utf8")
                    res2.on("data", (chunk) => {
                        infoiresult += chunk
                    })
                    res2.on("end", () => {
                        var jsoninfo1 = JSON.parse(infoiresult)
                        if (jsoninfo1["data"] != undefined && jsoninfo1["data"].length > 0) {
                            var options2 = {
                                host: 'tr.rbxcdn.com',
                                port: 443,
                                path: jsoninfo1["data"][0]["imageUrl"].toString().slice(21),
                                method: "GET",
                                headers: {
                                    "User-Agent": "totallychrome",
                                    "Accept-Encoding": "gzip,deflate"
                                }

                            }
                            https.get(options2, (res3) => {

                                var data = [], output
                                if (res3.headers["content-encoding"] == 'gzip') {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server (Image) [gzip compression]")
                                    }
                                    var gzip = zlib.createGunzip()
                                    res3.pipe(gzip)
                                    output = gzip
                                }
                                else if (res3.headers["content-encoding"] == 'deflate') {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server (Image) [deflate compression]")
                                    }
                                    var deflate = zlib.createDeflate()
                                    res3.pipe(deflate)
                                    output = deflate
                                }
                                else {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server (Image)")
                                    }
                                    output = res3
                                }
                                output.on("data", (chunk) => {
                                    data.push(chunk)
                                })
                                output.on("end", () => {
                                    var buffer = Buffer.concat(data)
                                    res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetId + ".png\"")
                                    res.status(200).send(buffer)
                                    assetfound = true
                                })
                            })
                        }
                        else {
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/gameicon.png"))
                        }
                    })
                })
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/gameicon.png"))
            }
        }
    }
})

app.get("/Thumbs/HeadShot.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"headshot.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/headshot.png"))
})
app.get("/Thumbs/Asset.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var assetfound = false
    var assetfound1 = false
    if (isNumeric(((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId))) {
        if (verbose) {
            console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + " from Roblox server/file (Image)")
        }
        filesystem.readdirSync("./uploads").forEach(file => {
            var splitted = file.split('.')
            if (splitted[0] == ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId).toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                assetfound1 = true
                return
            }
        })

        if (assetfound1 == false) {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId).toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                    res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                    res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                    assetfound = true
                    return
                }
            })
            if (assetfound == false && isInternetAvailable) {
                var options1 = {
                    host: 'thumbnails.roblox.com',
                    port: 443,
                    path: '/v1/assets?assetIds=' + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + '&returnPolicy=PlaceHolder&size=700x700&format=' + req.query.format,
                    method: "GET"
                }
                var infoiresult = ""
                https.get(options1, (res2) => {

                    res2.setEncoding("utf8")
                    res2.on("data", (chunk) => {
                        infoiresult += chunk
                    })
                    res2.on("end", () => {
                        var jsoninfo1 = JSON.parse(infoiresult)
                        if (jsoninfo1["data"] != undefined) {
                            var options2 = {
                                host: 'tr.rbxcdn.com',
                                port: 443,
                                path: jsoninfo1["data"][0]["imageUrl"].toString().slice(21),
                                method: "GET",
                                headers: {
                                    "User-Agent": "totallychrome",
                                    "Accept-Encoding": "gzip,deflate"
                                }
                            }
                            https.get(options2, (res3) => {

                                var data = [], output
                                if (res3.headers["content-encoding"] == 'gzip') {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + " from Roblox server (Image) [gzip compression]")
                                    }
                                    var gzip = zlib.createGunzip()
                                    res3.pipe(gzip)
                                    output = gzip
                                }
                                else if (res3.headers["content-encoding"] == 'deflate') {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + " from Roblox server (Image) [deflate compression]")
                                    }
                                    var deflate = zlib.createDeflate()
                                    res3.pipe(deflate)
                                    output = deflate
                                }
                                else {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + " from Roblox server (Image)")
                                    }
                                    output = res3
                                }
                                output.on("data", (chunk) => {
                                    data.push(chunk)
                                })
                                output.on("end", () => {
                                    var buffer = Buffer.concat(data)
                                    res.setHeader("Content-disposition", "attachment; filename=\"" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + "." + req.query.format + "\"")
                                    res.status(200).send(buffer)
                                    if (saveFile) filesystem.writeFileSync("./saved/" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + "." + req.query.format)
                                    assetfound = true
                                })
                            })
                        } else {
                            console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong when trying to download image " + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + " from the Roblox server!")
                            res.status(400).send("{\"errors\":[{\"code\": 0, \"message\":\"BadRequest\"}]}")
                        }

                    })
                })
            }
            else {
                res.status(404).end()
            }
        }
    }
    else {
        res.status(400).end()
    }
})

app.get("/Game/Tools/ThumbnailAsset.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")

    if (joining) {
        var options = {
            host: ip,
            port: 80,
            path: "/Game/Tools/ThumbnailAsset.ashx?aid=" + req.query.aid + "&wd=" + (req.query.wd != undefined ? req.query.wd : 700) + "&ht=" + (req.query.ht != undefined ? req.query.ht : 700) + "&fmt=" + (req.query.fmt != undefined ? req.query.fmt : "png"),
            method: "GET"
        }

        http.get(options, (res1) => {
            var data = []

            res1.on("data", (chunk) => {
                data.push(chunk)
            })

            res1.on("end", () => {
                var buffer = Buffer.concat(data)
                if (res1.statusCode == 200) {
                    res.status(res1.statusCode).send(buffer)
                }
                else {
                    var assetfound = false
                    var assetfound1 = false
                    var assetfound2 = false
                    if (isNumeric(req.query.aid)) {
                        if (verbose) {
                            console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server/file (Image)")
                        }
                        filesystem.readdirSync("./icons").forEach(file => {
                            var splitted = file.split('.')
                            if (splitted[0] == req.query.aid.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                                res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                                res.status(200).send(filesystem.readFileSync("./icons/" + file))
                                assetfound2 = true
                                return
                            }
                        })
                        if (assetfound2 == false) {
                            filesystem.readdirSync("./uploads").forEach(file => {
                                var splitted = file.split('.')
                                if (splitted[0] == req.query.aid.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                                    res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                                    res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                                    assetfound1 = true
                                    return
                                }
                            })
                            if (assetfound1 == false) {
                                filesystem.readdirSync(assetfolder).forEach(file => {
                                    var splitted = file.split('.')
                                    if (splitted[0] == req.query.aid.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                                        res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                                        assetfound = true
                                        return
                                    }
                                })
                                if (assetfound == false && isInternetAvailable) {
                                    var options1 = {
                                        host: 'thumbnails.roblox.com',
                                        port: 443,
                                        path: '/v1/assets?assetIds=' + req.query.aid + '&returnPolicy=PlaceHolder&size=' + req.query.wd + 'x' + req.query.ht + '&format=' + req.query.fmt,
                                        method: "GET"
                                    }
                                    var infoiresult = ""

                                    https.get(options1, (res2) => {

                                        res2.setEncoding("utf8")
                                        res2.on("data", (chunk) => {
                                            infoiresult += chunk
                                        })
                                        res2.on("end", () => {
                                            var jsoninfo1 = JSON.parse(infoiresult)
                                            if (jsoninfo1["data"] == undefined) {
                                                console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong when trying to download image " + req.query.aid + " from the Roblox server!")
                                                res.status(400).send("{\"errors\":[{\"code\": 0, \"message\":\"BadRequest\"}]}")
                                            }
                                            else {
                                                var options2 = {
                                                    host: 'tr.rbxcdn.com',
                                                    port: 443,
                                                    path: jsoninfo1["data"][0]["imageUrl"].toString().slice(21),
                                                    method: "GET",
                                                    headers: {
                                                        "User-Agent": "totallychrome",
                                                        "Accept-Encoding": "gzip,deflate"
                                                    }

                                                }
                                                https.get(options2, (res3) => {

                                                    var data = [], output
                                                    if (res3.headers["content-encoding"] == 'gzip') {
                                                        if (verbose) {
                                                            console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server (Image) [gzip compression]")
                                                        }
                                                        var gzip = zlib.createGunzip()
                                                        res3.pipe(gzip)
                                                        output = gzip
                                                    }
                                                    else if (res3.headers["content-encoding"] == 'deflate') {
                                                        if (verbose) {
                                                            console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server (Image) [deflate compression]")
                                                        }
                                                        var deflate = zlib.createDeflate()
                                                        res3.pipe(deflate)
                                                        output = deflate
                                                    }
                                                    else {
                                                        if (verbose) {
                                                            console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server (Image)")
                                                        }
                                                        output = res3
                                                    }
                                                    output.on("data", (chunk) => {
                                                        data.push(chunk)
                                                    })
                                                    output.on("end", () => {
                                                        var buffer = Buffer.concat(data)
                                                        res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.aid + "." + req.query.fmt + "\"")
                                                        res.status(200).send(buffer)
                                                        assetfound = true
                                                    })
                                                })
                                            }
                                        })
                                    })
                                }
                                else {
                                    res.status(404).end()
                                }
                            }
                        }
                    }
                }
            })
        })
    }
    else {
        var assetfound = false
        var assetfound1 = false
        var assetfound2 = false
        if (isNumeric(req.query.aid)) {
            if (verbose) {
                console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server/file (Image)")
            }
            filesystem.readdirSync("./icons").forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.aid.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                    res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                    res.status(200).send(filesystem.readFileSync("./icons/" + file))
                    assetfound2 = true
                    return
                }
            })
            if (assetfound2 == false) {
                filesystem.readdirSync("./uploads").forEach(file => {
                    var splitted = file.split('.')
                    if (splitted[0] == req.query.aid.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                        res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                        res.status(200).send(filesystem.readFileSync("./uploads/" + file))
                        assetfound1 = true
                        return
                    }
                })
                if (assetfound1 == false) {
                    filesystem.readdirSync(assetfolder).forEach(file => {
                        var splitted = file.split('.')
                        if (splitted[0] == req.query.aid.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                            assetfound = true
                            return
                        }
                    })
                    if (assetfound == false && isInternetAvailable) {
                        var options1 = {
                            host: 'thumbnails.roblox.com',
                            port: 443,
                            path: '/v1/assets?assetIds=' + req.query.aid + '&returnPolicy=PlaceHolder&size=' + req.query.wd + 'x' + req.query.ht + '&format=' + req.query.fmt,
                            method: "GET"
                        }
                        var infoiresult = ""

                        https.get(options1, (res2) => {

                            res2.setEncoding("utf8")
                            res2.on("data", (chunk) => {
                                infoiresult += chunk
                            })
                            res2.on("end", () => {
                                var jsoninfo1 = JSON.parse(infoiresult)
                                if (jsoninfo1["data"] == undefined) {
                                    console.log("\x1b[31m%s\x1b[0m", "<ERROR> Something went wrong when trying to download image " + req.query.aid + " from the Roblox server!")
                                    res.status(400).send("{\"errors\":[{\"code\": 0, \"message\":\"BadRequest\"}]}")
                                }
                                else {
                                    var options2 = {
                                        host: 'tr.rbxcdn.com',
                                        port: 443,
                                        path: jsoninfo1["data"][0]["imageUrl"].toString().slice(21),
                                        method: "GET",
                                        headers: {
                                            "User-Agent": "totallychrome",
                                            "Accept-Encoding": "gzip,deflate"
                                        }

                                    }
                                    https.get(options2, (res3) => {

                                        var data = [], output
                                        if (res3.headers["content-encoding"] == 'gzip') {
                                            if (verbose) {
                                                console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server (Image) [gzip compression]")
                                            }
                                            var gzip = zlib.createGunzip()
                                            res3.pipe(gzip)
                                            output = gzip
                                        }
                                        else if (res3.headers["content-encoding"] == 'deflate') {
                                            if (verbose) {
                                                console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server (Image) [deflate compression]")
                                            }
                                            var deflate = zlib.createDeflate()
                                            res3.pipe(deflate)
                                            output = deflate
                                        }
                                        else {
                                            if (verbose) {
                                                console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.aid + " from Roblox server (Image)")
                                            }
                                            output = res3
                                        }
                                        output.on("data", (chunk) => {
                                            data.push(chunk)
                                        })
                                        output.on("end", () => {
                                            var buffer = Buffer.concat(data)
                                            res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.aid + "." + req.query.fmt + "\"")
                                            res.status(200).send(buffer)
                                            assetfound = true
                                        })
                                    })
                                }
                            })
                        })
                    }
                    else {
                        res.status(404).end()
                    }
                }
            }
        }
    }
})

app.get("/game/GetCurrentUser.ashx", (req, res) => {
    if (AllowGetCurrentUser) {
        res.status(200).send(userId)
    }
    else {
        res.status(200).send("null")
    }
})

app.get("/my/settings/json", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"ChangeUsernameEnabled\":true,\"IsAdmin\":false,\"UserId\":" + userId + ",\"Name\":\"" + username + "\",\"DisplayName\": \"" + username + "\", \"UserAbove13\":" + accountOver13 + ", \"IsEmailOnFile\":true,\"IsEmailVerified\":true,\"UserEmail\":\"r*****@fakerebloxemail.com\",\"UserEmailMasked\":true,\"UserEmailVerified\":true,\"LocaleApiDomain\":\"http://locale.reblox.zip\",\"ApiProxyDomain\":\"http://api.reblox.zip\",\"AuthDomain\":\"http://auth.reblox.zip\", \"IsOBC\": false, \"IsTBC\": false, \"IsAnyBC\": false, \"IsPremium\":false,\"AccountAgeInDays\":365,\"ClientIpAddress\":\"127.0.0.1\",\"IsDisplayNamesEnabled\": true,\"PremiumFeatureId\": null,\"HasValidPasswordSet\": true, \"AgeBracket\": 0, \"IsUiBootstrapModalV2Enabled\": true, \"InApp\": false, \"HasFreeNameChange\": false, \"IsAgeDownEnabled\": true, \"Facebook\": null, \"Twitter\": null, \"YouTube\": null, \"Twitch\": null}")
})

app.post("/AbuseReport/InGameChatHandler.ashx", (_, res) => {
    res.status(200).end() //STUB
})

app.post("/game/join.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2017 or earlier (or Player) detected, using join.ashx")
    if (filesystem.existsSync("./game/join.ashx")) {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.concat([Buffer.from([0x0D, 0x0A]), filesystem.readFileSync("./game/join.ashx")]), filesystem.readFileSync(privateKey, "utf8")).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/join.ashx", "utf8"))
        }
        else {
            res.status(200).send(filesystem.readFileSync("./game/join.ashx"))
        }
    }
    else {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.concat([Buffer.from([0x0D, 0x0A]), filesystem.readFileSync("joinscript.txt")]), filesystem.readFileSync(privateKey, "utf8")).toString("base64") + "%\r\n" + filesystem.readFileSync("joinscript.txt", "utf8"))
        }
        else {
            res.status(200).send(filesystem.readFileSync("joinscript.txt"))
        }
    }
})

app.post("/game/placelauncher.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> 2018 or later (Player) detected, using placelauncher.ashx")
    res.status(200).send("{\"jobId\": \"Test\", \"status\":2, \"joinScriptUrl\":\"http://reblox.zip/game/join.ashx\",\"authenticationUrl\":\"http://reblox.zip/Login/Negotiate.ashx\", \"authenticationTicket\": \"SomeTicketThatDoesntCrash\", \"message\": \"\"}")
})

app.get("/game/placelauncher.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> 2016 or later (Player) detected, using placelauncher.ashx")
    res.status(200).send("{\"jobId\": \"Test\", \"status\":2, \"joinScriptUrl\":\"http://reblox.zip/game/join.ashx\",\"authenticationUrl\":\"http://reblox.zip/Login/Negotiate.ashx\", \"authenticationTicket\": \"SomeTicketThatDoesntCrash\", \"message\": \"\"}")
})

app.get("/game/join.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2017 or earlier (or Player) detected, using join.ashx")
    if (filesystem.existsSync("./game/join.ashx")) {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.concat([Buffer.from([0x0D, 0x0A]), filesystem.readFileSync("./game/join.ashx")]), filesystem.readFileSync(privateKey, "utf8")).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/join.ashx", "utf8"))
        }
        else {
            res.status(200).send(filesystem.readFileSync("./game/join.ashx"))
        }
    }
    else {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.concat([Buffer.from([0x0D, 0x0A]), filesystem.readFileSync("joinscript.txt")]), filesystem.readFileSync(privateKey, "utf8")).toString("base64") + "%\r\n" + filesystem.readFileSync("joinscript.txt", "utf8"))
        }
        else {
            res.status(200).send(filesystem.readFileSync("joinscript.txt"))
        }
    }

})

app.get("/Data/AutoSave.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2012* or earlier detected, using autosave.ashx")
    if (filesystem.existsSync(privateKey)) {
        res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + filesystem.readFileSync("./game/autosave.ashx"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/autosave.ashx", "utf8"))
    }
    else {
        res.status(200).send(filesystem.readFileSync("./game/autosave.ashx", "utf8"))
    }
})

app.get("/v1/places/:id/symbolic-links", (req, res) => {
    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": []}")
})

app.post("/Data/Upload.ashx", trueRaw, async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (isNumeric(req.query.assetid)) {
        if (allowUploadFiles) {
            if (req.body != undefined) {
                if (req.headers["content-type"] == "*/*") {
                    var data = Buffer.from([0x00])
                    if (req.headers["content-encoding"] == "gzip") {
                        data = zlib.gunzipSync(await req.body)
                    }
                    else if (req.headers["content-encoding"] == "inflate") {
                        data = zlib.deflateSync(await req.body)
                    }
                    else {
                        data = await req.body
                    }
                    if (filesystem.existsSync("./uploads")) {
                        if (filesystem.existsSync("./uploads/" + req.query.assetid)) {
                            filesystem.unlinkSync("./uploads/" + req.query.assetid)
                            filesystem.writeFileSync("./uploads/" + req.query.assetid, data)
                        }
                        else {
                            filesystem.writeFileSync("./uploads/" + req.query.assetid, data)
                        }
                    }
                    else {
                        filesystem.mkdirSync("./uploads")
                        filesystem.writeFileSync("./uploads/" + req.query.assetid, data)
                    }
                }
                else {
                    if (filesystem.existsSync("./uploads")) {
                        if (filesystem.existsSync("./uploads/" + req.query.assetid)) {
                            filesystem.unlinkSync("./uploads/" + req.query.assetid)
                            filesystem.writeFileSync("./uploads/" + req.query.assetid, req.body)
                        }
                        else {
                            filesystem.writeFileSync("./uploads/" + req.query.assetid, req.body)
                        }
                    }
                    else {
                        filesystem.mkdirSync("./uploads")
                        filesystem.writeFileSync("./uploads/" + req.query.assetid, req.body)
                    }
                }
            }
            else {
                res.status(400).end()
                return
            }
        }
        else {
            if (verbose) console.log("\x1b[34m%s\x1b[0m", "<INFO> Blocked an upload from " + req.ip)
        }
        res.status(200).send(req.query.assetid + " 1")
    }
    else {
        res.status(400).end()
    }
})

app.post("/Analytics/Measurement.ashx", (_, res) => {
    res.status(200).end() //STUB
})

app.get("/Analytics/ContentProvider.ashx", (_, res) => {
    res.status(200).end() //STUB
})

app.get("/game/gameserver.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2016* or earlier detected, using gameserver.ashx")
    if (filesystem.existsSync(privateKey)) {
        res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.concat([Buffer.from([0x0D, 0x0A]), filesystem.readFileSync("./game/gameserver.ashx")]), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/gameserver.ashx", "utf8"))
    }
    else {
        res.status(200).send(filesystem.readFileSync("./game/gameserver.ashx", "utf8"))
    }
})

app.get("//UploadMedia/PostImage.aspx", (req, res) => {
    res.status(200).send("Caught you screenshotting ;)") //STUB
})

app.get("/game/visit.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2016 or earlier detected, using visit.ashx")
    if (filesystem.existsSync(privateKey)) {
        res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.concat([Buffer.from([0x0D, 0x0A]), Buffer.from(filesystem.readFileSync("./game/visit.ashx", "utf8").replace(new RegExp("%userId%", "g"), userId.toString()), "utf8")]), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/visit.ashx", "utf8").replace(new RegExp("%userId%", "g"), userId.toString()))
    }
    else {
        res.status(200).send(filesystem.readFileSync("./game/visit.ashx", "utf8").replace(new RegExp("%userId%", "g"), userId.toString()))
    }
})

app.get("/Asset/CharacterFetch.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Asset/CharacterFetch.ashx?userId=" + req.query.userId,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })

        } catch {
            res.status(500).end()
        }
    }
    else {
        console.log("\x1b[32m%s\x1b[0m", "<INFO> Getting the avatar of " + req.query.userId + " via /Asset/CharacterFetch.ashx")
        if (localClothes == true) {
            if (isNumeric(req.query.userId)) {
                var tempclothes = ";"
                if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) {
                    var json = JSON.parse(filesystem.readFileSync("./clothes/" + req.query.userId + ".json"))
                    if (json["asset"] != undefined) {
                        for (var i = 0; i < json["asset"].length; i++) {
                            if (i == json["asset"].length - 1) {
                                tempclothes = tempclothes + "http://reblox.zip/asset/?id=" + json["asset"][i]["id"]
                            }
                            else {
                                tempclothes = tempclothes + "http://reblox.zip/asset/?id=" + json["asset"][i]["id"] + ";"
                            }
                        }
                    }
                    if (tempclothes == ";") tempclothes = ""
                    res.status(200).send("http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId + tempclothes)
                }
                else {
                    res.status(200).send("http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId)
                }
            }
            else {
                res.status(400).end()
            }
        }
        else {
            res.status(200).send("http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId)
        }
    }

})

app.post("/Game/PlaceVisit.ashx", (_, res) => {
    res.status(200).end()
})

app.get("/Game/LoadPlaceInfo.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2017 or earlier detected, using LoadPlaceInfo.ashx")
    if (filesystem.existsSync(privateKey)) {
        res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/LoadPlaceInfo.ashx", "utf8").replace(new RegExp("%userId%", "g"), userId), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/LoadPlaceInfo.ashx", "utf8").replace(new RegExp("%userId%", "g"), userId))
    }
    else {
        res.status(200).send(filesystem.readFileSync("./game/LoadPlaceInfo.ashx", "utf8").replace(new RegExp("%userId%", "g"), userId))
    }
})

app.get("/game/studio.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Early-2015 or earlier detected, using Studio.ashx")
    if (filesystem.existsSync("./game/studio.ashx")) {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/studio.ashx", "utf8").replace(new RegExp("{id}", "g"), userId), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/studio.ashx", "utf8").replace(new RegExp("{id}", "g"), userId))
        }
        else {
            res.status(200).send(filesystem.readFileSync("./game/studio.ashx", "utf8").replace(new RegExp("{id}", "g"), userId))
        }
        res.status(200).send(filesystem.readFileSync("./game/studio.ashx", "utf-8").replace(new RegExp("{id}", "g"), userId))
    }
})

app.get("/Game/PlaceSpecificScript.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Mid-2016 or earlier detected, using PlaceSpecificScript.ashx")
    if (filesystem.existsSync("./game/placespecificscript.ashx")) {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + filesystem.readFileSync("./game/placespecificscript.ashx"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/placespecificscript.ashx", "utf8"))
        }
        else {
            res.status(200).send(filesystem.readFileSync("./game/placespecificscript.ashx", "utf8"))
        }
    }
})

app.get("//game/studio.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (verbose) console.log("\x1b[32m%s\x1b[0m", "<INFO> Early-2015 or earlier detected, using Studio.ashx")
    if (filesystem.existsSync("./game/studio.ashx")) {
        if (filesystem.existsSync(privateKey)) {
            res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/studio.ashx", "utf8").replace(new RegExp("{id}", "g"), userId), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING }).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/studio.ashx", "utf8").replace(new RegExp("{id}", "g"), userId))
        }
        else {
            res.status(200).send(filesystem.readFileSync("./game/studio.ashx", "utf8").replace(new RegExp("{id}", "g"), userId))
        }
    }
})

// idk what does GetScriptState do, can anyone explain to me what these do???
app.get("/asset/GetScriptState.ashx", (_, res) => {
    res.status(200).send("0 0 0 0")
})

app.get("//asset/GetScriptState.ashx", (_, res) => {
    res.status(200).send("0 0 0 0")
})

app.get("/gametransactions/getpendingtransactions/", (_, res) => {
    res.status(200).send("[]") //STUB
})

app.get("/Game/Tools/InsertAsset.ashx", (req, res) => {
    res.setHeader("content-type", "text/plain")
    res.setHeader("cache-control", "no-cache")
    var result = "<List></List>"
    var setpath = ""
    if (filesystem.existsSync("./sets")) {
        if (req.query.userid != undefined) {
            setpath = "./sets/u" + req.query.userid + ".xml"
        }
        else if (req.query.sid != undefined) {
            setpath = "./sets/s" + req.query.sid + ".xml"
        }
        else if (req.query.type == "base") {
            setpath = "./sets/base.xml"
        }
    }

    if (setpath != "" && filesystem.existsSync(setpath)) {
        result = filesystem.readFileSync(setpath)
    } else {
        if (isInternetAvailable) {
            var options = {
                host: "sets.pizzaboxer.xyz",
                port: 443,
                path: "/Game/Tools/InsertAsset.ashx" + req.originalUrl.replace(new RegExp("(.*)\/Game\/InsertAsset\.ashx", "g"), ""),
                method: "GET"
            }

            https.get(options, (res) => {
                res.setEncoding("utf8")

                var httpresult = ""

                res.on("data", (chunk) => {
                    httpresult += chunk
                })

                res.on("end", () => {
                    result = httpresult
                })
            })
        }
    }

    res.status(200).send(result)
})

app.post("/Error/Dmp.ashx", (_, res) => {
    res.status(200).end() //STUB
})

app.post("/Error/Lua.ashx", (_, res) => {
    res.status(200).end() //STUB
})

app.post("/Error/Grid.ashx", (_, res) => {
    res.status(200).end() //STUB
})

app.get("/UploadMedia/UploadVideo.aspx", (_, res) => {
    res.status(200).send("Caught you recording ;)")
})

app.get("//UploadMedia/UploadVideo.aspx", (_, res) => {
    res.status(200).send("Caught you recording ;)")
})

app.get("/game/ChatFilter.ashx", (_, res) => {
    res.send("True")
})

app.post("/Game/ChatFilter.ashx", (_, res) => {
    res.send("True")
})

app.get("/points/get-awardable/points", (_, res) => {
    res.status(200).send("{\"points\":\"0\"}") //STUB
})

app.get("/Game/Badge/IsBadgeDisabled.ashx", (_, res) => {
    res.status(200).send(0) //STUB
})

app.get("/v1.1/avatar-fetch", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1.1/avatar-fetch?userId=" + req.query.userId,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })

        } catch {
            res.status(500).end()
        }
    }
    else {
        if (localClothes == true) {
            if (isNumeric(req.query.userId)) {
                console.log("\x1b[32m%s\x1b[0m", "<INFO> Getting the avatar of " + req.query.userId + " via /v1.1/avatar-fetch")
                var tempclothes = ""
                if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) {
                    var json = JSON.parse(filesystem.readFileSync("./clothes/" + req.query.userId + ".json"))
                    if (json["asset"] != undefined) {
                        for (var i = 0; i < json["asset"].length; i++) {
                            if (i == json["asset"].length - 1) {
                                tempclothes = tempclothes + json["asset"][i]["id"]
                            }
                            else {
                                tempclothes = tempclothes + json["asset"][i]["id"] + ","
                            }
                        }
                    }
                    res.status(200).send("{\"resolvedAvatarType\":\"" + json["bodyType"] + "\",\"accessoryVersionIds\":[" + tempclothes + "],\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"bodyColorsUrl\":\"http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId + "\",  \"bodyColors\":{\"HeadColor\":" + json["colors"]["headColor"] + ",\"LeftArmColor\":" + json["colors"]["leftArmColor"] + ",\"LeftLegColor\":" + json["colors"]["leftLegColor"] + ",\"RightArmColor\":" + json["colors"]["rightArmColor"] + ",\"RightLegColor\":" + json["colors"]["rightLegColor"] + ",\"TorsoColor\":" + json["colors"]["torsoColor"] + "},\"animations\":{},\"scales\":{\"Width\":1.0000,\"Height\":1.0000,\"Head\":1.0000,\"Depth\":1.00}}")
                }
                else {
                    res.status(200).send("{\"resolvedAvatarType\":\"" + ((avatarR15) ? "R15" : "R6") + "\",\"accessoryVersionIds\":[],\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"bodyColorsUrl\":\"http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId + "\",  \"bodyColors\":{\"HeadColor\":194,\"LeftArmColor\":194,\"LeftLegColor\":194,\"RightArmColor\":194,\"RightLegColor\":194,\"TorsoColor\":194},\"animations\":{},\"scales\":{\"Width\":1.0000,\"Height\":1.0000,\"Head\":1.0000,\"Depth\":1.00}}")
                }
            }
            else {
                res.status(400).end()
            }
        }
        else {
            res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + clothidsstring + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": " + avatarBodyColor[0] + ", \"torsoColorId\": " + avatarBodyColor[5] + ", \"rightArmColorId\": " + avatarBodyColor[3] + ", \"leftArmColorId\": " + avatarBodyColor[1] + ", \"rightLegColorId\": " + avatarBodyColor[4] + ", \"leftLegColorId\": " + avatarBodyColor[2] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
        }
    }
})

async function getAssetType(assetid, callback) {
    var typefound = false
    if (filesystem.existsSync("./assettype.json")) {
        try {
            var jsonresult = JSON.parse(filesystem.readFileSync("./assettype.json", "utf8"))
            if (jsonresult[assetid.toString()] != undefined) {
                typefound = true
                return callback(jsonresult[assetid.toString()])
            }
            else {
                if (isInternetAvailable) {
                    var options = {
                        host: "catalog.roblox.com",
                        port: 443,
                        path: "/v1/catalog/items/" + assetid + "/details?itemType=asset",
                        method: "GET"
                    }
                    https.get(options, (res1) => {
                        var jsonresult = ""
                        res1.setEncoding("utf8")
                        res1.on("data", (chunk) => {
                            jsonresult += chunk
                        })
                        res1.on("end", () => {
                            if (jsonresult["assetType"] != undefined) {
                                return callback(jsonresult["assetType"])
                            }
                            else {
                                if (useAuth) {
                                    var options1 = {
                                        host: "assetdelivery.roblox.com",
                                        port: 443,
                                        path: "/v2/asset/?id=" + assetid,
                                        method: "GET",
                                        headers: {
                                            "Cookie": ".ROBLOSECURITY=" + ROBLOSECURITY,
                                            "User-Agent": "totallychrome"
                                        }
                                    }

                                    https.get(options1, (res) => {
                                        var result = ""
                                        res.setEncoding("utf8")

                                        res.on("data", (chunk) => {
                                            result += chunk
                                        })

                                        res.on("end", () => {
                                            if (res.headers["set-cookie"] != undefined) {
                                                var regex1 = new RegExp("\.ROBLOSECURITY=(_\|WARNING:-DO-NOT-SHARE-THIS\.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items\.\|_)(.*)(;)")
                                                var match = regex1.exec(res.headers["set-cookie"])
                                                if (match.length > 0) {
                                                    ROBLOSECURITY = match[1].concat(match[2]).split(';')[0]
                                                    changeROBLOSECURITYOnLauncher(match[1].concat(match[2]).split(';')[0])
                                                }
                                            }
                                            var jsonresult1 = JSON.parse(result)

                                            if (jsonresult1["assetTypeId"] != undefined) {
                                                return callback(jsonresult1["assetTypeId"])
                                            }
                                            else {
                                                return callback(0)
                                            }
                                        })
                                    })
                                } else {
                                    return callback(0)
                                }
                            }
                        })
                    })
                } else {
                    return callback(0)
                }
            }
        }
        catch {
            return callback(0)
        }
    }
    else {
        if (isInternetAvailable) {
            var options = {
                host: "catalog.roblox.com",
                port: 443,
                path: "/v1/catalog/items/" + assetid + "/details?itemType=asset",
                method: "GET"
            }
            https.get(options, (res1) => {
                var jsonresult = ""
                res1.setEncoding("utf8")
                res1.on("data", (chunk) => {
                    jsonresult += chunk
                })
                res1.on("end", () => {
                    if (jsonresult["assetType"] != undefined) {
                        return callback(jsonresult["assetType"])
                    }
                    else {
                        if (useAuth) {
                            var options1 = {
                                host: "assetdelivery.roblox.com",
                                port: 443,
                                path: "/v2/asset/?id=" + assetid,
                                method: "GET",
                                headers: {
                                    "Cookie": ".ROBLOSECURITY=" + ROBLOSECURITY,
                                    "User-Agent": "totallychrome"
                                }
                            }

                            https.get(options1, (res) => {
                                var result = ""
                                res.setEncoding("utf8")

                                res.on("data", (chunk) => {
                                    result += chunk
                                })

                                res.on("end", () => {
                                    if (res.headers["set-cookie"] != undefined) {
                                        var regex1 = new RegExp("\.ROBLOSECURITY=(_\|WARNING:-DO-NOT-SHARE-THIS\.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items\.\|_)(.*)(;)")
                                        var match = regex1.exec(res.headers["set-cookie"])
                                        if (match.length > 0) {
                                            ROBLOSECURITY = match[1].concat(match[2]).split(';')[0]
                                            changeROBLOSECURITYOnLauncher(match[1].concat(match[2]).split(';')[0])
                                        }
                                    }
                                    var jsonresult1 = JSON.parse(result)

                                    if (jsonresult1["assetTypeId"] != undefined) {
                                        return callback(jsonresult1["assetTypeId"])
                                    }
                                    else {
                                        return callback(0)
                                    }
                                })
                            })
                        } else {
                            return callback(0)
                        }
                    }
                })
            })
        } else {
            return callback(0)
        }
    }
}

app.get("/v1/avatar-rules", (req, res) => {
    if (filesystem.existsSync("./avatarrule.json")) {
        res.status(200).send(filesystem.readFileSync("./avatarrule.json", "utf8"))
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/v1/avatar-fetch", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/avatar-fetch?userId=" + req.query.userId,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })

        } catch {
            res.status(500).end()
        }
    }
    else {
        if (localClothes == true) {
            if (isNumeric(req.query.userId)) {
                var tempclothes = "{"
                if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) {
                    console.log("\x1b[32m%s\x1b[0m", "<INFO> Getting the avatar of " + req.query.userId + " via /v1/avatar-fetch")
                    cloth2021finished = false
                    var json = JSON.parse(filesystem.readFileSync("./clothes/" + req.query.userId + ".json", "utf8"))
                    var tempi = 0
                    if (json["asset"] != undefined) {
                        for (var i = 0; i < json["asset"].length; i++) {
                            await getAssetType(json["asset"][i]["id"], (typenumber) => {
                                if (tempi == json["asset"].length - 1) {
                                    tempclothes = tempclothes + "\"assetId\":" + json["asset"][tempi]["id"] + ",\"assetTypeId\":" + typenumber + "}"
                                    cloth2021finished = true
                                    sendWhenFinished(cloth2021finished, res, "{\"resolvedAvatarType\": \"" + json["bodyType"] + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + tempclothes + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + json["bodyType"] + "\", \"bodyColors\": { \"headColorId\": " + json["colors"]["headColor"] + ", \"torsoColorId\": " + json["colors"]["torsoColor"] + ", \"rightArmColorId\": " + json["colors"]["rightArmColor"] + ", \"leftArmColorId\": " + json["colors"]["leftArmColor"] + ", \"rightLegColorId\": " + json["colors"]["rightLegColor"] + ", \"leftLegColorId\": " + json["colors"]["leftLegColor"] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
                                }
                                else {
                                    if (tempi < json["asset"].length) {
                                        tempclothes = tempclothes + "\"assetId\":" + json["asset"][tempi]["id"] + ",\"assetTypeId\":" + typenumber + "}, {"
                                    }
                                }
                                tempi = tempi + 1
                            })
                        }
                    } else {
                        cloth2021finished = true
                        sendWhenFinished(cloth2021finished, res, "{\"resolvedAvatarType\": \"" + json["bodyType"] + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + json["bodyType"] + "\", \"bodyColors\": { \"headColorId\": " + json["colors"]["headColor"] + ", \"torsoColorId\": " + json["colors"]["torsoColor"] + ", \"rightArmColorId\": " + json["colors"]["rightArmColor"] + ", \"leftArmColorId\": " + json["colors"]["leftArmColor"] + ", \"rightLegColorId\": " + json["colors"]["rightLegColor"] + ", \"leftLegColorId\": " + json["colors"]["leftLegColor"] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
                    }

                }
                else {
                    res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": 144, \"torsoColorId\": 144, \"rightArmColorId\": 144, \"leftArmColorId\": 144, \"rightLegColorId\": 144, \"leftLegColorId\": 144},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
                }
            }
            else {
                res.status(400).end()
            }
        }
        else {
            res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + clothidsstring + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": " + avatarBodyColor[0] + ", \"torsoColorId\": " + avatarBodyColor[5] + ", \"rightArmColorId\": " + avatarBodyColor[3] + ", \"leftArmColorId\": " + avatarBodyColor[1] + ", \"rightLegColorId\": " + avatarBodyColor[4] + ", \"leftLegColorId\": " + avatarBodyColor[2] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
        }
    }
})

app.post("/users/filter-friends", (_, res) => {
    res.status(200).send("[]")
})

app.get("/users/inventory/list-json", (req, res) => {
    res.status(200).send("{\"IsValid\": true, \"Data\":{\"TotalItems\":0,\"nextPageCursor\":null,\"previousPageCursor\":null,\"PageType\":\"inventory\",\"Items\":[]}}") //STUB
})

app.get("/v1/avatar", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")

    console.log("\x1b[32m%s\x1b[0m", "<INFO> Getting the avatar of " + userId + " via /v1/avatar")
    if (localClothes == true) {
        var tempclothes = "{"

        if (filesystem.existsSync("./clothes/" + userId + ".json")) {
            cloth2021finished = false
            var json = JSON.parse(filesystem.readFileSync("./clothes/" + userId + ".json", "utf8"))
            var tempi = 0
            if (json["asset"] != undefined) {
                for (var i = 0; i < json["asset"].length; i++) {
                    await getAssetType(json["asset"][i]["id"], (typenumber) => {
                        if (tempi == json["asset"].length - 1) {
                            tempclothes = tempclothes + "\"assetId\":" + json["asset"][tempi]["id"] + ",\"assetTypeId\":" + typenumber + "}"
                            cloth2021finished = true
                            sendWhenFinished(cloth2021finished, res, "{\"resolvedAvatarType\": \"" + json["bodyType"] + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + tempclothes + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + json["bodyType"] + "\", \"bodyColors\": { \"headColorId\": " + json["colors"]["headColor"] + ", \"torsoColorId\": " + json["colors"]["torsoColor"] + ", \"rightArmColorId\": " + json["colors"]["rightArmColor"] + ", \"leftArmColorId\": " + json["colors"]["leftArmColor"] + ", \"rightLegColorId\": " + json["colors"]["rightLegColor"] + ", \"leftLegColorId\": " + json["colors"]["leftLegColor"] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
                        }
                        else {
                            if (tempi < json["asset"].length) {
                                tempclothes = tempclothes + "\"assetId\":" + json["asset"][tempi]["id"] + ",\"assetTypeId\":" + typenumber + "}, {"
                            }
                        }
                        tempi = tempi + 1
                    })
                }
            } else {
                cloth2021finished = true
                sendWhenFinished(cloth2021finished, res, "{\"resolvedAvatarType\": \"" + json["bodyType"] + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + json["bodyType"] + "\", \"bodyColors\": { \"headColorId\": " + json["colors"]["headColor"] + ", \"torsoColorId\": " + json["colors"]["torsoColor"] + ", \"rightArmColorId\": " + json["colors"]["rightArmColor"] + ", \"leftArmColorId\": " + json["colors"]["leftArmColor"] + ", \"rightLegColorId\": " + json["colors"]["rightLegColor"] + ", \"leftLegColorId\": " + json["colors"]["leftLegColor"] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
            }

        }
        else {
            res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": 144, \"torsoColorId\": 144, \"rightArmColorId\": 144, \"leftArmColorId\": 144, \"rightLegColorId\": 144, \"leftLegColorId\": 144},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
        }
    }
    else {
        res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + clothidsstring + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": " + avatarBodyColor[0] + ", \"torsoColorId\": " + avatarBodyColor[5] + ", \"rightArmColorId\": " + avatarBodyColor[3] + ", \"leftArmColorId\": " + avatarBodyColor[1] + ", \"rightLegColorId\": " + avatarBodyColor[4] + ", \"leftLegColorId\": " + avatarBodyColor[2] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
    }
})

app.post("/device/initialize", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("set-cookie", "RBXEventTrackerV2=21398")
    res.status(200).send("{ \"browserTrackerId\": 1, \"appDeviceIdentifier\": null }")
})

app.post("/v1/get-enrollments", (_, res) => {
    res.status(200).send("{\"data\":[]}") //STUB
})

app.post("/v1/enrollments", (_, res) => {
    res.status(200).send("{\"data\":[]}") //STUB
})

async function sendWhenFinished(bool, res, data) {
    if (bool == false) {
        await delay(100)
        sendWhenFinished(cloth2021finished, res, data)
    }
    else {
        res.status(200).send(data)
    }
}

app.post("/v2/universes/:id/shutdown", (req, res) => {
    res.status(200).send("{}")
})

app.get("/v1/locales/supported-locales", (_, res) => {
    res.status(200).send("{\"supportedLocales\": [{\"id\": 1, \"locale\": \"en_us\", \"name\": \"English (United States)\", \"nativeName\": \"English (United States)\", \"language\": {\"id\": 41, \"name\": \"English\", \"nativeName\": \"English\", \"languageCode\": \"en\", \"isRightToLeft\": false}}]}")
})

app.get("/v1/universes/:id/configuration", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")

    if (filesystem.existsSync("./games.json")) {
        var gamesjson = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (gamesjson.length > 0) {
            var found = false
            gamesjson.forEach((game) => {
                if (game["id"] == req.params.id) {
                    found = true
                    res.status(200).send("{\"id\": " + req.params.id + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"universeAvatarType\": \"PlayerChoice\", \"universeScaleType\": \"AllScales\",\"universeAnimationType\": \"PlayerChoice\", \"universeCollisionType\": \"OuterBox\", \"universeBodyType\": \"Standard\", \"universeJointPositioningType\": \"ArtistIntent\", \"isArchived\": " + game["isArchived"] + ", \"isFriendsOnly\": " + game["isFriendsOnly"] + ", \"genre\": \"" + game["genre"] + "\", \"playableDevices\": " + JSON.stringify(game["playableDevices"]) + ", \"isForSale\": false, \"price\": 0, \"studioAccessToApisAllowed\": " + game["studioAccessToApisAllowed"] + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"privacyType\": \"" + game["privacyType"] + "\", \"fiatModerationStatus\": \"NotModerated\"}")
                    return
                }
            })
            if (found == false) res.status(200).send("{\"id\": " + req.params.id + ", \"name\": \"ReBlox Place\", \"universeAvatarType\": \"PlayerChoice\", \"universeScaleType\": \"AllScales\",\"universeAnimationType\": \"PlayerChoice\", \"universeCollisionType\": \"OuterBox\", \"universeBodyType\": \"Standard\", \"universeJointPositioningType\": \"ArtistIntent\", \"isArchived\": false, \"isFriendsOnly\": false, \"genre\": \"Tutorial\", \"playableDevices\": [\"Computer\", \"Phone\", \"Tablet\"], \"isForSale\": false, \"studioAccessToApisAllowed\": true, \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"privacyType\": \"Public\", \"fiatModerationStatus\": \"NotModerated\"}")
        }
    }
    else {
        res.status(200).send("{\"id\": " + req.params.id + ", \"name\": \"ReBlox Place\", \"universeAvatarType\": \"PlayerChoice\", \"universeScaleType\": \"AllScales\",\"universeAnimationType\": \"PlayerChoice\", \"universeCollisionType\": \"OuterBox\", \"universeBodyType\": \"Standard\", \"universeJointPositioningType\": \"ArtistIntent\", \"isArchived\": false, \"isFriendsOnly\": false, \"genre\": \"Tutorial\", \"playableDevices\": [\"Computer\", \"Phone\", \"Tablet\"], \"isForSale\": false, \"studioAccessToApisAllowed\": true, \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"privacyType\": \"Public\", \"fiatModerationStatus\": \"NotModerated\"}")
    }
})

app.get("/v2/universes/:id/configuration", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")

    if (filesystem.existsSync("./games.json")) {
        var gamesjson = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (gamesjson.length > 0) {
            var found = false
            gamesjson.forEach((game) => {
                if (game["id"] == req.params.id) {
                    found = true
                    res.status(200).send("{\"id\": " + req.params.id + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"universeAvatarType\": \"PlayerChoice\", \"universeScaleType\": \"AllScales\",\"universeAnimationType\": \"PlayerChoice\", \"universeCollisionType\": \"OuterBox\", \"universeBodyType\": \"Standard\", \"universeJointPositioningType\": \"ArtistIntent\", \"isArchived\": " + game["isArchived"] + ", \"isFriendsOnly\": " + game["isFriendsOnly"] + ", \"genre\": \"" + game["genre"] + "\", \"playableDevices\": " + JSON.stringify(game["playableDevices"]) + ", \"isForSale\": false, \"price\": 0, \"studioAccessToApisAllowed\": " + game["studioAccessToApisAllowed"] + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"privacyType\": \"" + game["privacyType"] + "\", \"fiatModerationStatus\": \"NotModerated\"}")
                    return
                }
            })
            if (found == false) res.status(200).send("{\"id\": " + req.params.id + ", \"name\": \"ReBlox Place\", \"universeAvatarType\": \"PlayerChoice\", \"universeScaleType\": \"AllScales\",\"universeAnimationType\": \"PlayerChoice\", \"universeCollisionType\": \"OuterBox\", \"universeBodyType\": \"Standard\", \"universeJointPositioningType\": \"ArtistIntent\", \"isArchived\": false, \"isFriendsOnly\": false, \"genre\": \"Tutorial\", \"playableDevices\": [\"Computer\", \"Phone\", \"Tablet\"], \"isForSale\": false, \"studioAccessToApisAllowed\": true, \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"privacyType\": \"Public\", \"fiatModerationStatus\": \"NotModerated\"}")
        }
    }
    else {
        res.status(200).send("{\"id\": " + req.params.id + ", \"name\": \"ReBlox Place\", \"universeAvatarType\": \"PlayerChoice\", \"universeScaleType\": \"AllScales\",\"universeAnimationType\": \"PlayerChoice\", \"universeCollisionType\": \"OuterBox\", \"universeBodyType\": \"Standard\", \"universeJointPositioningType\": \"ArtistIntent\", \"isArchived\": false, \"isFriendsOnly\": false, \"genre\": \"Tutorial\", \"playableDevices\": [\"Computer\", \"Phone\", \"Tablet\"], \"isForSale\": false, \"studioAccessToApisAllowed\": true, \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"privacyType\": \"Public\", \"fiatModerationStatus\": \"NotModerated\"}")
    }
})

app.get("/v1/universes/:id/icon", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var found1 = false
    var imageId = 133293265
    if (filesystem.existsSync("./gametemplates.json")) {
        var jsondata = JSON.parse("{\"data\": [" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}")

        if (jsondata["data"].length > 0) {
            jsondata["data"].forEach((template) => {
                if (template["universe"]["id"] == req.params.id) {
                    found1 = true
                    imageId = template["universe"]["rootPlaceId"]
                    return
                }
            })
        }
    }

    if (found1 == false) {
        if (filesystem.existsSync("./games.json")) {
            var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

            if (jsondata.length > 0) {
                jsondata.forEach((game) => {
                    if (game["id"] == req.params.id) {
                        imageId = game["rootPlaceId"]
                        return
                    }
                })
            }
        }
    }

    if (filesystem.existsSync("./icons/" + imageId + ".png") == false) imageId = 133293265
    res.status(200).send("{\"imageId\":" + imageId + ", \"isApproved\":true}")
})

app.get("/Asset/BodyColors.ashx", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Asset/BodyColors.ashx?userId=" + req.query.userId,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "text/plain")
                    res.status(res1.statusCode).send(data)
                })
            })

        } catch {
            res.status(500).end()
        }
    }
    else {
        if (isNumeric(req.query.userId)) {
            if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) {
                var json = JSON.parse(filesystem.readFileSync("./clothes/" + req.query.userId + ".json", "utf8"))
                res.setHeader("content-type", "text/plain")
                res.status(200).send("<roblox xmlns:xmime=\"http://www.w3.org/2005/05/xmlmime\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://www.roblox.com/roblox.xsd\" version=\"4\">\r\n <External>null</External>\r\n <External>nil</External>\r\n <Item class=\"BodyColors\">\r\n  <Properties>\r\n   <int name=\"HeadColor\">" + json["colors"]["headColor"] + "</int>\r\n   <int name=\"LeftArmColor\">" + json["colors"]["leftArmColor"] + "</int>\r\n   <int name=\"LeftLegColor\">" + json["colors"]["leftLegColor"] + "</int>\r\n   <string name=\"Name\">Body Colors</string>\r\n   <int name=\"RightArmColor\">" + json["colors"]["rightArmColor"] + "</int>\r\n   <int name=\"RightLegColor\">" + json["colors"]["rightLegColor"] + "</int>\r\n   <int name=\"TorsoColor\">" + json["colors"]["torsoColor"] + "</int>\r\n   <bool name=\"archivable\">true</bool>\r\n  </Properties>\r\n </Item>\r\n</roblox>")
            }
            else {
                res.setHeader("content-type", "text/plain")
                res.status(200).send("<roblox xmlns:\"http://www.w3.org/2005/05/xmlmime\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://www.roblox.com/roblox.xsd\" version=\"4\"> <External>null</External> <External>nil</External> <Item class=\"BodyColors\"> <Properties> <int name=\"HeadColor\">194</int> <int name=\"LeftArmColor\">194</int> <int name=\"LeftLegColor\">194</int> <string name=\"Name\">Body Colors</string> <int name=\"RightArmColor\">194</int> <int name=\"RightLegColor\">194</int> <int name=\"TorsoColor\">194</int> <bool name=\"archivable\">true</bool> </Properties> </Item> </roblox>")
            }
        }
        else {
            res.status(400).end()
        }
    }
})
app.get("/userblock/getblockedusers", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\":{\"blockedUserIds\":[],\"blockedUsers\":[],\"cursor\":null},\"error\":null}") //STUB
})

app.get("/universal-app-configuration/v1/behaviors/studio/content", (_, res) => {
    res.status(200).send("{}")
})
app.post("/v1/avatar/set-avatar", (req, res) => {
    //custom api for ReBlox

    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/avatar/set-avatar?userId=" + req.query.userId + (req.query.username != undefined ? "&username=" + req.query.username : ""),
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-Token": req.headers["x-token"] != undefined ? req.headers["x-token"] : 0
                }
            }

            var request1 = http.request(options, (res1) => {
                res1.on("end", () => {
                    res.status(res1.statusCode).end()
                })
            })
            request1.write(JSON.stringify(req.body))
            request1.end()
        } catch {
            res.status(500).end()
        }
    }
    else {
        try {
            if (req.headers["x-token"] != undefined) {
                if (crypto.verify("SHA1", JSON.stringify(req.body), publicKeyObj, Buffer.from(req.headers["x-token"], "base64"))) {
                    if (isNumeric(req.query.userId)) {
                        if (req.query.username != undefined) memoryUsers.push({ "userId": req.query.userId, "username": req.query.username })
                        console.log("\x1b[32m%s\x1b[0m", "<INFO> Saving the avatar of " + req.query.userId + " to file...")
                        if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) filesystem.unlinkSync("./clothes/" + req.query.userId + ".json")
                        if (filesystem.existsSync("./clothes") == false) filesystem.mkdirSync("./clothes")
                        filesystem.writeFileSync("./clothes/" + req.query.userId + ".json", JSON.stringify(req.body))
                        res.status(200).end()
                    }
                    else {
                        res.status(400).end()
                    }
                }
                else {
                    res.status(500).send("{\"errors\": [{\"code\": 0, \"message\": \"Invalid verification (Not matching)\"}]}")
                }
            }
            else {
                res.status(500).send("{\"errors\": [{\"code\": 0, \"message\": \"Invalid verification\"}]}")
            }

        } catch (ex) {
            res.status(500).end()
            console.error(ex)
        }
    }
})

app.post("/universes/create", async (req, res) => {
    var universeId = 1
    var placeId = 1
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./games.json")) {
        var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        json.forEach((game) => {
            if (game["id"] == universeId) {
                universeId++
            }
        })
    }
    if (filesystem.existsSync("./gametemplates.json")) {
        var json = JSON.parse("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]")

        json.forEach((game) => {
            if (game["universe"]["id"] == universeId) {
                universeId++
            }
        })
    }
    if (filesystem.existsSync("./assets")) {
        var list = await filesystem.readdirSync("./assets")

        for (var i = 0; i < list.length; i++) {
            list[i] = list[i].replace(new RegExp("\.[^/.]+$"), "")
        }
        list.sort(function (a, b) { return a - b })
        for (var i = 0; i < list.length; i++) {
            if (list[i] == placeId) {
                placeId++
            }
        }
    }
    if (filesystem.existsSync("./uploads")) {
        var list = filesystem.readdirSync("./uploads")

        list.sort(function (a, b) { return a - b })

        for (var i = 0; i < list.length; i++) {
            if (list[i] == placeId) {
                placeId++
            }
        }
    }
    if (allowUploadFiles) {
        if (typeof (req.body["templatePlaceIdToUse"]) == "number" && isNaN(req.body["templatePlaceIdToUse"]) == false) {
            var marketplace = filesystem.readFileSync("./marketplace.json", "utf8")
            var games = filesystem.readFileSync("./games.json", "utf8")
            var marketplacejson = JSON.parse(marketplace)
            var gamesjson = JSON.parse(games)

            gamesjson.push({ id: universeId, name: "Baseplate", description: "", "isArchived": false, "rootPlaceId": placeId, isActive: false, genre: "All", isFriendsOnly: false, playableDevices: ["Computer", "Phone", "Tablet"], studioAccessToApisAllowed: false, privacyType: "Private", creatorType: "User", creatorTargetId: userId, creatorName: username, created: new Date(Date.now()).toISOString(), updated: new Date(Date.now()).toISOString() })
            marketplacejson.push({ id: placeId, name: "Baseplate", description: "", assetType: 9, imageId: placeId, robux: 0, creatorTargetId: userId, creatorName: username })

            var gamesrecompiled = JSON.stringify(gamesjson, null, 4)
            var marketplacerecompiled = JSON.stringify(marketplacejson, null, 4)

            if (filesystem.existsSync("./marketplace.json")) filesystem.unlinkSync("./marketplace.json");
            filesystem.writeFileSync("./marketplace.json", marketplacerecompiled);
            if (filesystem.existsSync("./games.json")) filesystem.unlinkSync("./games.json");
            filesystem.writeFileSync("./games.json", gamesrecompiled);
            if (filesystem.existsSync("./defaulticons")) {
                var icons = filesystem.readdirSync("./defaulticons")

                if (icons.length > 0) {
                    if (filesystem.existsSync("./icons") == false) filesystem.mkdirSync("./icons");
                    filesystem.writeFileSync("./icons/" + placeId + ".png", filesystem.readFileSync("./defaulticons/" + icons[Math.floor(Math.random() * ((icons.length - 1) - 0))]))
                }
            }
            if (filesystem.existsSync("./uploads/" + placeId)) {
                filesystem.unlinkSync("./uploads/" + placeId)
                if (filesystem.existsSync("./assets/" + req.body["templatePlaceIdToUse"] + ".rbxl")) {
                    filesystem.writeFileSync("./uploads/" + placeId, filesystem.readFileSync("./assets/" + req.body["templatePlaceIdToUse"] + ".rbxl"))
                }
                else {
                    getAsset(req.body["templatePlaceIdToUse"], (result) => {
                        filesystem.writeFileSync("./uploads/" + placeId, result)
                    })
                }
            }
            else {
                if (filesystem.existsSync("./assets/" + req.body["templatePlaceIdToUse"] + ".rbxl")) {
                    filesystem.writeFileSync("./uploads/" + placeId, filesystem.readFileSync("./assets/" + req.body["templatePlaceIdToUse"] + ".rbxl"))
                }
                else {
                    getAsset(req.body["templatePlaceIdToUse"], (result) => {
                        filesystem.writeFileSync("./uploads/" + placeId, result)
                    })
                }
            }
        }
    }
    if (forceCanManageTrue) {
        res.status(200).send("{ \"UniverseId\": 2, \"RootPlaceId\": 1818 }")
    }
    else {
        res.status(200).send("{ \"UniverseId\": " + universeId + ", \"RootPlaceId\": " + placeId + " }")
    }
})

app.post("/v1/universes/:id/deactivate", (req, res) => {
    if (filesystem.existsSync("./games.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (jsondata.length > 0) {
            for (var i = 0; i < jsondata.length; i++) {
                if (jsondata[i]["id"] == req.params.id) {
                    jsondata[i]["isActive"] = false
                    jsondata[i]["privacyType"] = "Private"
                    break
                }
            }

            var jsondatarecompiled = JSON.stringify(jsondata, null, 4)

            filesystem.unlinkSync("./games.json")

            filesystem.writeFileSync("./games.json", jsondatarecompiled)
        }
    }

    res.status(200).send("{}")
})

app.post("/v1/universes/:id/activate", (req, res) => {
    if (filesystem.existsSync("./games.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (jsondata.length > 0) {
            for (var i = 0; i < jsondata.length; i++) {
                if (jsondata[i]["id"] == req.params.id) {
                    jsondata[i]["isActive"] = true
                    jsondata[i]["privacyType"] = "Public"
                    break
                }
            }

            var jsondatarecompiled = JSON.stringify(jsondata, null, 4)

            filesystem.unlinkSync("./games.json")

            filesystem.writeFileSync("./games.json", jsondatarecompiled)
        }
    }

    res.status(200).send("{}")
})

app.get("/places/:id/update", (_, res) => {
    res.send("<pre>*insert edit place here*</pre>") //STUB
})

app.get("/universes/configure", (_, res) => {
    res.send("<pre>*insert edit universe here*</pre>") //STUB
})

app.get("/games/:id", (_, res) => {
    res.send("<pre>*insert game page here*</pre>") //STUB
})
app.get("/v1/places/:id/teamcreate/active_session/members", (req, res) => {
    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
})
app.post("/ide/places/createV2", async (req, res) => {
    var placeId = 1
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")

    if (filesystem.existsSync("./assets")) {
        var list = await filesystem.readdirSync("./assets")

        for (var i = 0; i < list.length; i++) {
            list[i] = list[i].replace(new RegExp("\.[^/.]+$"), "")
        }
        list.sort(function (a, b) { return a - b })
        for (var i = 0; i < list.length; i++) {
            if (list[i] == placeId) {
                placeId++
            }
        }
    }
    if (filesystem.existsSync("./uploads")) {
        var list = filesystem.readdirSync("./uploads")

        list.sort(function (a, b) { return a - b })

        for (var i = 0; i < list.length; i++) {
            if (list[i] == placeId) {
                placeId++
            }
        }
    }
    if (allowUploadFiles) {
        if (isNumeric(req.query.templatePlaceIdToUse)) {
            var marketplace = filesystem.readFileSync("./marketplace.json", "utf8")
            var games = filesystem.readFileSync("./games.json", "utf8")
            var marketplacejson = JSON.parse(marketplace)
            var gamesjson = JSON.parse(games)

            gamesjson.push({ id: req.query.universeId, name: "Baseplate", description: "", "isArchived": false, "rootPlaceId": placeId, isActive: false, genre: "All", isFriendsOnly: false, playableDevices: ["Computer", "Phone", "Tablet"], studioAccessToApisAllowed: false, privacyType: "Private", creatorType: "User", creatorTargetId: userId, creatorName: username, created: (new Date(Date.now()).toISOString()), updated: (new Date(Date.now()).toISOString()) })
            marketplacejson.push({ id: placeId, name: "Baseplate", description: "", assetType: 9, imageId: placeId, robux: 0, creatorTargetId: userId, creatorName: username })

            var gamesrecompiled = JSON.stringify(gamesjson, null, 4)
            var marketplacerecompiled = JSON.stringify(marketplacejson, null, 4)

            if (filesystem.existsSync("./marketplace.json")) filesystem.unlinkSync("./marketplace.json")
            filesystem.writeFileSync("./marketplace.json", marketplacerecompiled)
            if (filesystem.existsSync("./games.json")) filesystem.unlinkSync("./games.json")
            filesystem.writeFileSync("./games.json", gamesrecompiled)

            if (filesystem.existsSync("./defaulticons")) {
                var icons = filesystem.readdirSync("./defaulticons")

                if (icons.length > 0) {
                    if (filesystem.existsSync("./icons") == false) filesystem.mkdirSync("./icons");
                    filesystem.writeFileSync("./icons/" + placeId + ".png", filesystem.readFileSync("./defaulticons/" + icons[Math.floor(Math.random() * ((icons.length - 1) - 0))]))
                }
            }

            if (filesystem.existsSync("./uploads/" + placeId)) {
                filesystem.unlinkSync("./uploads/" + placeId)
                if (filesystem.existsSync("./assets/" + req.query.templatePlaceIdToUse + ".rbxl")) {
                    filesystem.writeFileSync("./uploads/" + placeId, filesystem.readFileSync("./assets/" + req.query.templatePlaceIdToUse + ".rbxl"))
                }
                else {
                    getAsset(req.query.templatePlaceIdToUse, (result) => {
                        filesystem.writeFileSync("./uploads/" + placeId, result)
                    })
                }
            }
            else {
                if (filesystem.existsSync("./assets/" + req.query.templatePlaceIdToUse + ".rbxl")) {
                    filesystem.writeFileSync("./uploads/" + placeId, filesystem.readFileSync("./assets/" + req.query.templatePlaceIdToUse + ".rbxl"))
                }
                else {
                    getAsset(req.query.templatePlaceIdToUse, (result) => {
                        filesystem.writeFileSync("./uploads/" + placeId, result)
                    })
                }
            }
        }
    }
    if (forceCanManageTrue) {
        res.status(200).send("{\"Success\":true, \"PlaceId\":1818}")
    }
    else {
        res.status(200).send("{\"Success\":true, \"PlaceId\":" + placeId + "}")
    }
})

app.post("/v1/usernames/users", (req, res) => {
    var edit = ""
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (req.body["usernames"] != undefined) {
        req.body["usernames"].forEach((username1) => {
            if (typeof (username1) == "string") {
                if (username1 == username) {

                    if (username1 == req.body["usernames"][req.body["usernames"].length - 1]) {
                        edit += "{\"requestedUsername\": \"" + username1 + "\", \"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"" + username + "\", \"displayName\": \"" + username + "\" }"
                    }
                    else {
                        edit += "{\"requestedUsername\": \"" + username1 + "\", \"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"" + username + "\", \"displayName\": \"" + username + "\" }, "
                    }
                }
                else {
                    if (joining) {
                        try {
                            var options = {
                                host: ip,
                                port: 80,
                                path: "/users/" + username1,
                                method: "GET"
                            }

                            http.get(options, (res1) => {
                                res1.setEncoding("utf-8")
                                var data = ""
                                res1.on("data", (chunk) => {
                                    data += chunk
                                })
                                res1.on("end", () => {
                                    if (data != "{\"data\":[]}") {
                                        var json = JSON.stringify(JSON.parse(data)["data"]).slice(0, 1).slice(0, -1)
                                        edit += json
                                    }
                                })
                            })

                        } catch {

                        }
                    }
                    else {
                        if (filesystem.existsSync("./users.json")) {
                            try {
                                var userjson = JSON.parse(filesystem.readFileSync("./users.json", "utf8"))
                                var objectKeys = Object.keys(userjson)

                                for (var i = 0; i < objectKeys.length; i++) {
                                    if (userjson[objectKeys[i].toString()] == username1) {
                                        if (username1 == req.body["usernames"][req.body["usernames"].length - 1]) {
                                            edit += "{\"requestedUsername\": \"" + username1 + "\", \"hasVerifiedBadge\": false, \"id\": " + objectKeys[i] + ", \"name\":\"" + username1 + "\", \"displayName\": \"" + username1 + "\" }"
                                        }
                                        else {
                                            edit += "{\"requestedUsername\": \"" + username1 + "\", \"hasVerifiedBadge\": false, \"id\": " + objectKeys[i] + ", \"name\":\"" + username1 + "\", \"displayName\": \"" + username1 + "\" }, "
                                        }
                                    }
                                }

                                userjson = null
                            } catch {

                            }
                        }
                        else {

                        }
                    }
                }
            }
        })
    }
    res.status(200).send("{\"data\":[" + edit + "]}")
})
app.post("/v1/users", (req, res) => {
    var edit = ""
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (req.body["userIds"] != undefined) {
        req.body["userIds"].forEach((id) => {
            if (typeof (id) == "number") {
                if (id == userId) {

                    if (id == req.body["userIds"][req.body["userIds"].length - 1]) {
                        edit += "{\"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"" + username + "\", \"displayName\": \"" + username + "\" }"
                    }
                    else {
                        edit += "{\"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"" + username + "\", \"displayName\": \"" + username + "\" }, "
                    }
                }
                else {
                    if (joining) {
                        try {
                            var options = {
                                host: ip,
                                port: 80,
                                path: "/users/" + id,
                                method: "GET"
                            }

                            http.get(options, (res1) => {
                                res1.setEncoding("utf-8")
                                var data = ""
                                res1.on("data", (chunk) => {
                                    data += chunk
                                })
                                res1.on("end", () => {
                                    if (data != "{\"data\":[]}") {
                                        var json = JSON.stringify(JSON.parse(data)["data"]).slice(0, 1).slice(0, -1)
                                        edit += json
                                    }
                                })
                            })

                        } catch {

                        }
                    }
                    else {
                        if (id < 0) {
                            if (id == req.body["userIds"][req.body["userIds"].length - 1]) {
                                edit += "{\"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"Player" + Math.abs(id) + "\", \"displayName\": \"Player" + Math.abs(id) + "\" }"
                            }
                            else {
                                edit += "{\"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"Player" + Math.abs(id) + "\", \"displayName\": \"Player" + Math.abs(id) + "\" }, "
                            }
                        }
                        else {
                            if (filesystem.existsSync("./users.json")) {
                                try {
                                    var userjson = JSON.parse(filesystem.readFileSync("./users.json", "utf8"))

                                    if (userjson[id.toString()] != undefined) {
                                        if (id == req.body["userIds"][req.body["userIds"].length - 1]) {
                                            edit += "{\"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"" + userjson[id] + "\", \"displayName\": \"" + userjson[id] + "\" }"
                                        }
                                        else {
                                            edit += "{\"hasVerifiedBadge\": false, \"id\": " + id + ", \"name\":\"" + userjson[id] + "\", \"displayName\": \"" + userjson[id] + "\" }, "
                                        }
                                    }
                                    else {

                                    }
                                    userjson = null
                                } catch {

                                }
                            }
                            else {

                            }
                        }
                    }
                }
            }
        })
    }
    res.status(200).send("{\"data\":[" + edit + "]}")
})

app.get("/users/get-by-username", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (req.query.username == username) {
        res.status(200).send("{ \"Id\": " + userId + ", \"Username\":\"" + username + "\",\"AvatarUri\":null, \"AvatarFinal\":false, \"IsOnline\":true }")
    }
    else {
        if (joining) {
            try {
                var options = {
                    host: ip,
                    port: 80,
                    path: "/users/get-by-username?username=" + req.query.username,
                    method: "GET"
                }

                http.get(options, (res1) => {
                    res1.setEncoding("utf-8")
                    var data = ""
                    res1.on("data", (chunk) => {
                        data += chunk
                    })
                    res1.on("end", () => {
                        res.status(res1.statusCode).send(data)
                    })
                })

            } catch {
                res.status(500).end()
            }
        }
        else {
            if (filesystem.existsSync("./users.json")) {
                try {
                    var userjson = JSON.parse(filesystem.readFileSync("./users.json", "utf8"))
                    var objectKeys = Object.keys(userjson)

                    for (var i = 0; i < objectKeys.length; i++) {
                        if (userjson[objectKeys[i].toString()] == req.query.username) {
                            res.status(200).send("{ \"Id\": " + objectKeys[i] + ", \"Username\":\"" + req.query.username + "\",\"AvatarUri\":null, \"AvatarFinal\":false, \"IsOnline\":true }")
                            return
                        }
                    }
                    res.status(200).send("{}")
                    userjson = null
                } catch {
                    res.status(200).send("{}")
                }
            }
            else {
                res.status(200).send("{}")
            }
        }
    }
})

app.get("/users/account-info", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{ \"UserId\": " + userId + ", \"Username\": \"" + username + "\",\"HasPasswordSet\": true, \"Email\": {\"Value\": \"r*****@fakerebloxemail.com\", \"IsVerified\": true }, \"AgeBracket\": 0, \"Roles\": [], \"MembershipType\": 0, \"RobuxBalance\": " + robux + ", \"CountryCode\": \"US\"}")
})

app.get("/users/:userid/canmanage/:placeid", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./games.json")) {
        var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (json.length > 0) {
            var canManage = false
            for (var i = 0; i < json.length; i++) {
                if (json[i]["rootPlaceId"] == req.params.placeid) {
                    canManage = true
                    break
                }
            }
            res.send("{ \"Success\": true, \"CanManage\": " + canManage + " }")
        }
        else {
            res.send("{ \"Success\": true, \"CanManage\": false }")
        }
    } else {
        res.send("{ \"Success\": true, \"CanManage\": true }")
    }
})

app.post("//moderation/filtertext/", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")

    res.status(200).send("{\"success\": true, \"data\": {\"white\": \"" + req.body["text"] + "\", \"black\": \"\"}}")
})

app.post("/moderation/filtertext/", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")

    res.status(200).send("{\"success\": true, \"data\": {\"white\": \"" + req.body["text"] + "\", \"black\": \"\"}}")
})

app.get("//game/players/:id", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")

    res.status(200).send("{ \"ChatFilter\": \"whitelist\" }")
})

app.get("/users/:id", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (isNumeric(req.params.id)) {
        if (req.params.id == userId) {
            res.status(200).send("{ \"Id\": " + userId + ", \"Username\":\"" + username + "\",\"AvatarUri\":null, \"AvatarFinal\":false, \"IsOnline\":true }")
        }
        else {
            if (joining) {
                try {
                    var options = {
                        host: ip,
                        port: 80,
                        path: "/users/" + req.params.id,
                        method: "GET"
                    }

                    http.get(options, (res1) => {
                        res1.setEncoding("utf-8")
                        var data = ""
                        res1.on("data", (chunk) => {
                            data += chunk
                        })
                        res1.on("end", () => {
                            res.status(res1.statusCode).send(data)
                        })
                    })

                } catch {
                    res.status(500).end()
                }
            }
            else {
                if (filesystem.existsSync("./users.json")) {
                    try {
                        var userjson = JSON.parse(filesystem.readFileSync("./users.json", "utf8"))

                        if (userjson[req.params.id.toString()] != undefined) {
                            res.status(200).send("{ \"Id\": " + req.params.id + ", \"Username\":\"" + userjson[req.params.id] + "\",\"AvatarUri\":null, \"AvatarFinal\":false, \"IsOnline\":true }")
                        }
                        else {
                            res.status(200).send("{}")
                        }
                        userjson = null
                    } catch {
                        res.status(200).send("{}")
                    }
                }
                else {
                    res.status(200).send("{}")
                }
            }
        }
    }
    else {
        res.status(400).end()
    }
})

app.get("/users/:id/friends", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("[]") //STUB
})
app.get("/IDE/Toolbox/Items", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.send("{\"TotalResults\":0,\"Results\":[]}")
})

function updatedDateSort(a, b) {
    return parseISOString(b.updated).getTime() - parseISOString(a.updated).getTime();
}

function dateSort(a, b) {
    return parseISOString(a.created).getTime() - parseISOString(b.created).getTime();
}

function reverseDateSort(a, b) {
    return parseISOString(b.created).getTime() - parseISOString(a.created).getTime();
}

function compareStrings(a, b) {
    return (a < b) ? -1 : (a > b) ? 1 : 0;
}
app.get("/v1/search/universes", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (req.query.q.includes(' ')) {
        var splitted = req.query.q.split(' ')
        var ogwithoutcreator = req.query.q.slice(0, req.query.q.length - splitted[splitted.length - 1].length).trim()
        if (splitted[splitted.length - 1] == "creator:Team" || (splitted[0] == "creator:Team" && splitted[1].startsWith("archived:"))) {
            res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
        }
        else {
            if (filesystem.existsSync("./games.json")) {
                var data = []

                var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

                if (req.query.sort == "GameCreated") {
                    json.sort(dateSort)
                }
                else if (req.query.sort == "-GameCreated") {
                    json.sort(reverseDateSort)
                }
                else if (req.query.sort == "-LastUpdated") {
                    json.sort(updatedDateSort)
                }
                else if (req.query.sort == "GameName") {
                    json.sort(function (a, b) {
                        return compareStrings(a.name, b.name)
                    })
                }

                if (json.length > 0) {
                    json.forEach((game) => {
                        if ((game["name"].toLowerCase().includes(ogwithoutcreator.toLowerCase()) || ((splitted[1].split(':')[1] != undefined ? splitted[1].split(':')[1].toLowerCase() : "") == game["isArchived"].toString() && splitted.length == 2)) && game["creatorTargetId"] == userId) {
                            data.push(JSON.parse("{\"id\": " + game["id"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isArchived\": " + game["isArchived"] + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"isActive\": " + game["isActive"] + ", \"privacyType\": \"" + game["privacyType"] + "\", \"creatorType\": \"" + game["creatorType"] + "\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatorName\": \"" + game["creatorName"] + "\", \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\"}"))
                        }
                    })
                    res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":" + JSON.stringify(data) + "}")
                }
                else {
                    res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
                }
            }
            else {
                res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
            }
        }
    }
    else {
        if (req.query.q == "creator:Team") {
            res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
        }
        else {
            if (filesystem.existsSync("./games.json")) {
                var data = []

                var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

                if (req.query.sort == "GameCreated") {
                    json.sort(dateSort)
                }
                else if (req.query.sort == "-GameCreated") {
                    json.sort(reverseDateSort)
                }
                else if (req.query.sort == "-LastUpdated") {
                    json.sort(updatedDateSort)
                }
                else if (req.query.sort == "GameName") {
                    json.sort(function (a, b) {
                        return compareStrings(a.name, b.name)
                    })
                }

                if (json.length > 0) {
                    json.forEach((game) => {
                        if (game["creatorTargetId"] == userId) {
                            data.push(JSON.parse("{\"id\": " + game["id"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isArchived\": " + game["isArchived"] + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"isActive\": " + game["isActive"] + ", \"privacyType\": \"" + game["privacyType"] + "\", \"creatorType\": \"" + game["creatorType"] + "\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatorName\": \"" + game["creatorName"] + "\", \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\"}"))
                        }
                    })
                    res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":" + JSON.stringify(data) + "}")
                }
                else {
                    res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
                }
            }
            else {
                res.send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
            }
        }
    }
})
app.get("/v2/auth/metadata", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"cookieLawNoticeTimeout\": 20000}")
})

app.get("/universes/get-universe-places", (req, res) => {
    var templates = filesystem.existsSync("./gametemplates.json") ? ("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]") : "[]"
    var templatesjson = JSON.parse(templates)
    var gamesjson = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesparsed = JSON.parse(gamesjson)

    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (gamesparsed.length > 0) {
        for (var i = 0; i < gamesparsed.length; i++) {
            if (gamesparsed[i]["id"] == req.query.universeId) {
                res.status(200).send("{\"FinalPage\": true, \"RootPlace\": " + gamesparsed[i]["rootPlaceId"] + ", \"Places\":[{\"PlaceId\":" + gamesparsed[i]["rootPlaceId"] + ", \"Name\":\"" + gamesparsed[i]["name"] + "\"}], \"PageSize\":50}")
                return
            }
        }
    }
    if (templatesjson.length > 0) {
        for (var i = 0; i < templatesjson.length; i++) {
            if (templatesjson[i]["universe"]["id"] == req.query.universeId) {
                res.status(200).send("{\"FinalPage\": true, \"RootPlace\": " + templatesjson[i]["universe"]["rootPlaceId"] + ", \"Places\":[{\"PlaceId\":" + templatesjson[i]["universe"]["rootPlaceId"] + ", \"Name\":\"" + templatesjson[i]["universe"]["name"] + "\"}], \"PageSize\":50}")
                return
            }
        }
    }
    res.status(200).send("{\"FinalPage\": true, \"RootPlace\": 1818, \"Places\":[{\"PlaceId\":1818, \"Name\":\"ReBlox Place\"}], \"PageSize\":50}")
})

app.get("/v1/universes/:id/places", (req, res) => {
    var templates = filesystem.existsSync("./gametemplates.json") ? ("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]") : "[]"
    var templatesjson = JSON.parse(templates)
    var gamesjson = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesparsed = JSON.parse(gamesjson)

    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (gamesparsed.length > 0) {
        for (var i = 0; i < gamesparsed.length; i++) {
            if (gamesparsed[i]["id"] == req.params.id) {
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[{\"id\": " + gamesparsed[i]["rootPlaceId"] + ", \"universeId\": " + req.params.id + ", \"name\": \"" + gamesparsed[i]["name"] + "\", \"description\": \"A place that's on ReBlox.\"}]}")
                return
            }
        }
    }
    if (templatesjson.length > 0) {
        for (var i = 0; i < templatesjson.length; i++) {
            if (templatesjson[i]["universe"]["id"] == req.params.id) {
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[{\"id\": " + templatesjson[i]["universe"]["rootPlaceId"] + ", \"universeId\": " + req.params.id + ", \"name\": \"" + templatesjson[i]["universe"]["name"] + "\", \"description\": \"" + templatesjson[i]["universe"]["description"] + "\"}]}")
                return
            }
        }
    }
    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[{}]}")
})

app.get("/places/icons/json", (req, res) => {
    res.status(200).send("{\"ImageId\":" + req.query.placeId + "}") //STUB
})

app.get("/v1/games/multiget-place-details", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var splitted = req.query.placeIds.split(',')
    var edited = ""

    var templatesjson = []
    var gamesjson = []
    if (filesystem.existsSync("./gametemplates.json")) templatesjson = JSON.parse("{\"data\":[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}");
    if (filesystem.existsSync("./games.json")) gamesjson = JSON.parse(filesystem.readFileSync("./games.json", "utf8"));
    if (typeof (req.query.placeIds) == "string") {
        splitted.forEach((id) => {
            var found = false
            var found1 = false
            if (templatesjson.length > 0) {
                if (templatesjson["data"].length > 0) {
                    templatesjson.forEach((template) => {
                        if (template["universe"]["rootPlaceId"] == id) {
                            found = true
                            if (id == splitted[splitted.length - 1]) {
                                edited += "{\"placeId\": " + id + ",\"name\": \"" + template["universe"]["name"] + "\", \"description\": \"" + template["universe"]["description"] + "\", \"sourceName\": \"" + template["universe"]["name"] + "\", \"sourceDescription\": \"" + template["universe"]["description"] + "\", \"url\": \"http://www.reblox.zip/games/" + id + "/" + template["universe"]["name"].replace(new RegExp(" ", "g"), "-") + "\", \"builder\": \"" + template["universe"]["creatorName"] + "\", \"builderId\": " + template["universe"]["creatorTargetId"] + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": " + template["universe"]["id"] + ", \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"}"
                            }
                            else {
                                edited += "{\"placeId\": " + id + ",\"name\": \"" + template["universe"]["name"] + "\", \"description\": \"" + template["universe"]["description"] + "\", \"sourceName\": \"" + template["universe"]["name"] + "\", \"sourceDescription\": \"" + template["universe"]["description"] + "\", \"url\": \"http://www.reblox.zip/games/" + id + "/" + template["universe"]["name"].replace(new RegExp(" ", "g"), "-") + "\", \"builder\": \"" + template["universe"]["creatorName"] + "\", \"builderId\": " + template["universe"]["creatorTargetId"] + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": " + template["universe"]["id"] + ", \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"},"
                            }
                            return
                        }
                    })
                    if (found == false) {
                        if (gamesjson.length > 0) {
                            gamesjson.forEach((template) => {
                                if (template["rootPlaceId"] == id) {
                                    found1 = true
                                    if (id == splitted[splitted.length - 1]) {
                                        edited += "{\"placeId\": " + id + ",\"name\": \"" + template["name"] + "\", \"description\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": \"" + template["name"] + "\", \"sourceDescription\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\\\"", "g"), "\\\\\\\"") + "\", \"url\": \"http://www.reblox.zip/games/" + id + "/" + template["name"].replace(new RegExp(" ", "g"), "-") + "\", \"builder\": \"" + template["creatorName"] + "\", \"builderId\": " + template["creatorTargetId"] + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": " + template["id"] + ", \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"}"
                                    }
                                    else {
                                        edited += "{\"placeId\": " + id + ",\"name\": \"" + template["name"] + "\", \"description\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": \"" + template["name"] + "\", \"sourceDescription\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"url\": \"http://www.reblox.zip/games/" + id + "/" + template["name"].replace(new RegExp(" ", "g"), "-") + "\", \"builder\": \"" + template["creatorName"] + "\", \"builderId\": " + template["creatorTargetId"] + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": " + template["id"] + ", \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"},"
                                    }
                                    return
                                }
                            })
                            if (found1 == false) {
                                if (id == splitted[splitted.length - 1]) {
                                    edited += "{\"placeId\": " + id + ",\"name\": \"ReBlox Place\", \"description\": \"A place that's on ReBlox.\", \"sourceName\": \"ReBlox Place\", \"sourceDescription\": \"A place that's on ReBlox\", \"url\": \"http://www.reblox.zip/games/" + id + "/ReBlox-Place\", \"builder\": \"" + username + "\", \"builderId\": " + userId + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": 2, \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"}"
                                }
                                else {
                                    edited += "{\"placeId\": " + id + ",\"name\": \"ReBlox Place\", \"description\": \"A place that's on ReBlox.\", \"sourceName\": \"ReBlox Place\", \"sourceDescription\": \"A place that's on ReBlox\", \"url\": \"http://www.reblox.zip/games/" + id + "/ReBlox-Place\", \"builder\": \"" + username + "\", \"builderId\": " + userId + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": 2, \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"},"
                                }
                            }
                        }
                    }
                }
            }
            else {
                if (gamesjson.length > 0) {
                    gamesjson.forEach((template) => {
                        if (template["rootPlaceId"] == id) {
                            found1 = true
                            if (id == splitted[splitted.length - 1]) {
                                edited += "{\"placeId\": " + id + ",\"name\": \"" + template["name"] + "\", \"description\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": \"" + template["name"] + "\", \"sourceDescription\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"url\": \"http://www.reblox.zip/games/" + id + "/" + template["name"].replace(new RegExp(" ", "g"), "-") + "\", \"builder\": \"" + template["creatorName"] + "\", \"builderId\": " + template["creatorTargetId"] + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": " + template["id"] + ", \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"}"
                            }
                            else {
                                edited += "{\"placeId\": " + id + ",\"name\": \"" + template["name"] + "\", \"description\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": \"" + template["name"] + "\", \"sourceDescription\": \"" + template["description"].replace(new RegExp("\r\n"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"url\": \"http://www.reblox.zip/games/" + id + "/" + template["name"].replace(new RegExp(" ", "g"), "-") + "\", \"builder\": \"" + template["creatorName"] + "\", \"builderId\": " + template["creatorTargetId"] + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": " + template["id"] + ", \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"},"
                            }
                            return
                        }
                    })
                    if (found1 == false) {
                        if (id == splitted[splitted.length - 1]) {
                            edited += "{\"placeId\": " + id + ",\"name\": \"ReBlox Place\", \"description\": \"A place that's on ReBlox.\", \"sourceName\": \"ReBlox Place\", \"sourceDescription\": \"A place that's on ReBlox\", \"url\": \"http://www.reblox.zip/games/" + id + "/ReBlox-Place\", \"builder\": \"" + username + "\", \"builderId\": " + userId + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": 2, \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"}"
                        }
                        else {
                            edited += "{\"placeId\": " + id + ",\"name\": \"ReBlox Place\", \"description\": \"A place that's on ReBlox.\", \"sourceName\": \"ReBlox Place\", \"sourceDescription\": \"A place that's on ReBlox\", \"url\": \"http://www.reblox.zip/games/" + id + "/ReBlox-Place\", \"builder\": \"" + username + "\", \"builderId\": " + userId + ", \"hasVerifiedBadge\": false, \"isPlayable\": true, \"reasonProhibited\": \"None\", \"universeId\": 2, \"universeRootPlaceId\": " + id + ", \"price\": 0, \"imageToken\": \"T_" + id + "_fb22\"},"
                        }
                    }
                }
            }
        })
    }

    res.status(200).send("[" + edited + "]")
})

app.get("/v2/universes/:id/permissions", (req, res) => {
    res.status(200).send("{\"data\":[]}")
})

app.get("/v1/users/:id/friends/online", (req, res) => {
    res.status(200).send("{\"data\": []}")
})

app.get("/v2/chat-settings", (req, res) => {
    res.status(200).send("{\"chatEnabled\": true, \"isActiveChatUser\": true, \"isConnectTabEnabled\": true}")
})

app.get("/v1/packages/assets/:id/highest-permission", (req, res) => {
    res.status(200).send("{\"assetId\": " + req.params.id + ", \"hasPermission\": true, \"action\": \"Own\", \"upToVersion\": 1}")
})

app.post("/ide/publish/uploadnewasset", (req, res) => {
    res.status(200).end()
})

app.post("/data/upload/json", async (req, res) => {
    var assetId = 1
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./assets")) {
        var list = await filesystem.readdirSync("./assets")

        for (var i = 0; i < list.length; i++) {
            list[i] = list[i].replace(new RegExp("\.[^/.]+$"), "")
        }
        list.sort(function (a, b) { return a - b })
        for (var i = 0; i < list.length; i++) {
            if (list[i] == assetId) {
                assetId++
            }
        }
    }
    if (filesystem.existsSync("./uploads")) {
        var list = filesystem.readdirSync("./uploads")

        list.sort(function (a, b) { return a - b })

        for (var i = 0; i < list.length; i++) {
            if (list[i] == assetId) {
                assetId++
            }
        }
    }
    res.status(200).send("{\"Success\": true, \"BackingAssetId\": " + assetId + "}")
})

app.post("/universes/create-alias-v2", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{}")
})
app.get("/v1/products", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./premiumsubscriptions.json")) {
        res.status(200).send(filesystem.readFileSync("./premiumsubscriptions.json"))
    }
    else {
        res.status(200).send("{\"products\":[]}")
    }
})

app.get("/v1/users/:id/friends", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\": []}")
})
app.get("/v1/universes/:id/configuration/vip-servers", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"isEnabled\":true,\"price\":0,\"activeServersCount\":0,\"activeSubscriptionsCount\":0}")
})
app.get("/developerproducts/list", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"FinalPage\":true, \"DeveloperProducts\":[], \"PageSize\":0}")
})
app.get("/v1/resale-tax-rate", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"taxRate\":0.3, \"minimumFee\":1}")
})
app.get("/v2/universes/:id/places", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")

    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["id"] == req.params.id) {
                found = true
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"maxPlayerCount\": 50, \"socialSlotType\": \"Automatic\", \"customSocialSlotsCount\": 15, \"allowCopying\": false, \"currentSavedVersion\":1,\"isAllGenresAllowed\": null, \"allowedGearTypes\": null, \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\", \"id\": " + game["rootPlaceId"] + ", \"universeId\": " + req.params.id + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isRootPlace\": true}]}")
                return
            }
        })
        if (found == false) res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"maxPlayerCount\": 50, \"socialSlotType\": \"Automatic\", \"customSocialSlotsCount\": 15, \"allowCopying\": false, \"currentSavedVersion\":1,\"isAllGenresAllowed\": null, \"allowedGearTypes\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"updated\": \"2021-08-26T22:01:58.237Z\", \"id\": 1, \"universeId\": " + req.params.id + ", \"name\": \"ReBlox Place\", \"description\": \"A place on ReBlox\", \"isRootPlace\": true}]}")
    }
    else {
        res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"maxPlayerCount\": 50, \"socialSlotType\": \"Automatic\", \"customSocialSlotsCount\": 15, \"allowCopying\": false, \"currentSavedVersion\":1,\"isAllGenresAllowed\": null, \"allowedGearTypes\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"updated\": \"2021-08-26T22:01:58.237Z\", \"id\": 1, \"universeId\": " + req.params.id + ", \"name\": \"ReBlox Place\", \"description\": \"A place on ReBlox\", \"isRootPlace\": true}]}")
    }
})
app.get("/v1/assets/:id/saved-versions", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")

    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["id"] == req.params.id) {
                found = true
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatingUniverseId\": null, \"created\": \"" + game["created"] + "\", \"isPublished\": true}]}")
                return
            }
        })
        if (found == false) res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + userId + ", \"creatingUniverseId\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"isPublished\": true}]}")
    }
    else {
        res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + userId + ", \"creatingUniverseId\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"isPublished\": true}]}")
    }
})

app.get("/v1/assets/:id/published-versions", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")

    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["id"] == req.params.id) {
                found = true
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatingUniverseId\": null, \"created\": \"" + game["created"] + "\", \"isPublished\": true, \"isEqualToCurrentPublishedVersion\": true}]}")
                return
            }
        })
        if (found == false) res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + userId + ", \"creatingUniverseId\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"isPublished\": true, \"isEqualToCurrentPublishedVersion\": true}]}")
    }
    else {
        res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + userId + ", \"creatingUniverseId\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"isPublished\": true, \"isEqualToCurrentPublishedVersion\": true}]}")
    }
})

app.get("/v2/assets/:id/versions", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["id"] == req.params.id) {
                found = true
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatingUniverseId\": null, \"created\": \"" + game["created"] + "\", \"isPublished\": true}]}")
                return
            }
        })
        if (found == false) res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + userId + ", \"creatingUniverseId\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"isPublished\": true}]}")
    }
    else {
        res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": [{\"Id\": 0, \"assetId\": " + req.params.id + ", \"assetVersionNumber\":1, \"creatorType\": \"User\", \"creatorTargetId\": " + userId + ", \"creatingUniverseId\": null, \"created\": \"2021-08-26T22:01:58.237Z\", \"isPublished\": true}]}")
    }
})

app.get("/v1/universes/:id/badges", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": []}")
})

app.get("/v1/users/:id/currency", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"robux\": " + robux + "}")
})

app.get("/v1/universes/:id/symbolic-links", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
})

app.post("/v1/places/:id/symbolic-links", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
})

app.get("/universes/get-aliases", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"FinalPage\": true, \"Aliases\": [], \"PageSize\": 50}")
})

app.post("/product-experimentation-platform/v1/projects/:isthisid/values", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"projectId\": " + req.params.isthisid + ", \"version\": -1, \"publishedAt\": -1, \"layers\": { \"ClientTestBtidLayer\": {\"experimentName\": null, \"isAudienceSpecified\": false, \"isAudienceMember\": null, \"segment\": -1, \"experimentVariant\": \"da39a3e\", \"parameters\": {}, \"primaryUnit\": null, \"primaryUnitValue\": null, \"holdoutGroupExperimentName\": null }} }, \"userAgent\": \"" + req.headers["user-agent"] + "\", \"platformType\": \"Unknown\", \"platformTypeId\": 8 }")
})

app.get("/toolbox-service/v1/:type", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"totalResults\": 0, \"data\": []}")
})
app.post("/v2/login", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("roblox-machine-id", randomUUID())
    res.setHeader("set-cookie", ".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_" + jwt.sign({ "username": username }, "thisisarebloxprivatekeyforjwtchange", { algorithm: "HS256" }) + "; domain=.reblox.zip; path=/; expires=Tue, 10 Mar 2889 07:28:00 GMT; samesite=lax")
    res.setHeader("strict-transport-security", "max-age=31536000")
    res.status(200).send("{\"user\": { \"id\": " + userId + ", \"name\": \"" + username + "\", \"displayName\": \"" + username + "\"},\"accountBlob\":\"\",\"isBanned\": false, \"recoveryEmail\": null, \"shouldAutoLoginFromRecovery\": null}")
})

app.get("/studio-user-settings/plugin-permissions", (req, res) => {
    res.status(200).send("[]") //STUB
})
app.post("/sign-out/v1", (req, res) => {
    res.status(200).send("{}")
})

app.post("/v2/logout", (req, res) => {
    res.status(200).send("{}")
})
app.get("/universes/get-info", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")

    if (filesystem.existsSync("./gametemplates.json")) {
        var json = JSON.parse("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]")

        if (json.length > 0) {
            for (var i = 0; i < json.length; i++) {
                if (json[i]["universe"]["rootPlaceId"] == req.query.placeId) {
                    res.status(200).send("{\"Name\":\"" + json[i]["universe"]["name"] + "\", \"Description\":\"" + json[i]["universe"]["description"] + "\", \"RootPlace\":" + json[i]["universe"]["rootPlaceId"] + ", \"StudioAccessToApisAllowed\": false, \"CurrentUserHasEditPermissions\":false,\"UniverseAvatarType\":\"PlayerChoice\"}")
                    return
                }
            }
        }
    }

    if (filesystem.existsSync("./games.json")) {
        var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (json.length > 0) {
            for (var i = 0; i < json.length; i++) {
                if (json[i]["rootPlaceId"] == req.query.placeId) {
                    res.status(200).send("{\"Name\":\"" + json[i]["name"] + "\", \"Description\":\"" + json[i]["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"RootPlace\":" + json[i]["rootPlaceId"] + ", \"StudioAccessToApisAllowed\":" + json[i]["studioAccessToApisAllowed"] + ", \"CurrentUserHasEditPermissions\":true,\"UniverseAvatarType\":\"PlayerChoice\"}")
                    return
                }
            }
        }
        else {
            res.status(200).send("{\"Name\":\"ReBlox Place\", \"Description\":\"A ReBlox place launched from the launcher\", \"RootPlace\":1, \"StudioAccessToApisAllowed\":true,\"CurrentUserHasEditPermissions\":true,\"UniverseAvatarType\":\"PlayerChoice\"}")
            return
        }
    }
    res.status(200).send("{\"Name\":\"ReBlox Place\", \"Description\":\"A ReBlox place launched from the launcher\", \"RootPlace\":1, \"StudioAccessToApisAllowed\":true,\"CurrentUserHasEditPermissions\":true,\"UniverseAvatarType\":\"PlayerChoice\"}")
})

app.get("/Game/LuaWebService/HandleSocialRequest.ashx", async (req, res) => {
    if (req.query.method == "IsInGroup") {
        res.status(200).send("<Value Type=\"boolean\">false</Value>")
    }
    else if (req.query.method == "GetGroupRank") {
        res.status(200).send("<Value Type=\"integer\">0</Value>")
    }
    else if (req.query.method == "IsFriendsWith") {
        if (isNumeric(req.query.playerid) && isNumeric(req.query.userid)) {
            var verified = false
            if (enableFriendships == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
                if (filesystem.existsSync(RBDFpath)) {
                    var areFriends = false
                    const stream = filesystem.createReadStream(RBDFpath)

                    const rl = readline.createInterface({
                        input: stream,
                        crlfDelay: Infinity
                    })

                    for await (const line of rl) {
                        if (line == "RBDF==") {
                            verified = true
                        }
                        else {
                            if (line.startsWith("<Friendship")) {
                                if (line == "<Friendship Receiver=" + req.query.playerid + " Sender=" + req.query.userid + ">" || line == "<Friendship Receiver=" + req.query.userid + " Sender=" + req.query.playerid + ">") {
                                    areFriends = true
                                }
                            }
                        }
                    }
                    stream.destroy()
                    rl.close()
                    if (verified == true) {
                        res.status(200).send("<Value type=\"boolean\">" + areFriends + "</Value>")
                    }
                    else {
                        res.status(200).send("<Value type=\"boolean\">false</Value>")
                    }
                }
                else {
                    res.status(200).send("<Value type=\"boolean\">false</Value>")
                }
            }
            else {
                res.status(200).send("<Value type=\"boolean\">false</Value>")
            }
        }
        else {
            res.status(400).end()
        }
    }
    else {
        if (verbose) console.log("\x1b[33m%s\x1b[0m", "<STUB> Unknown SocialRequest method: " + req.query.method)
        res.status(200).end()
    }
})

app.get("/universes/:id/cloudeditenabled", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"enabled\":false}") //Required for creating place in 2018+ studio
})

app.get("/v1/user/is-verified-creator", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"isVerifiedCreator\": true}")
})
app.get("/v1/users/:userid/groups/roles", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\":[]}") //STUB
})

app.get("/localization/games/:id/configure", (_, res) => {
    res.status(200).get("<pre>*insert localization configuration page here*</pre>")
})

app.get("/v1/universes/:id/permissions", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./games.json")) {
        var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (json.length > 0) {
            var canManage = false
            for (var i = 0; i < json.length; i++) {
                if (json[i]["id"] == req.params.id) {
                    canManage = true
                    break
                }
            }
            res.status(200).send("{\"canManage\":" + canManage + ",\"canCloudEdit\":false}")
        }
        else {
            res.status(200).send("{\"canManage\":false,\"canCloudEdit\":false}")
        }
    } else {
        res.status(200).send("{\"canManage\":true,\"canCloudEdit\":false}")
    }
})

app.get("/places/:id/settings", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["rootPlaceId"] == req.params.id) {
                found = true
                res.status(200).send("{\"Creator\":{\"Name\":\"" + game["creatorName"] + ",\"CreatorType\":1,\"CreatorTargetId\":" + game["creatorTargetId"] + "}}")
                return
            }
        })
        if (found == false) res.status(200).send("{\"Creator\":{\"Name\":\"" + username + ",\"CreatorType\":1,\"CreatorTargetId\":" + userId + "}}")
    }
    else {
        res.status(200).send("{\"Creator\":{\"Name\":\"" + username + ",\"CreatorType\":1,\"CreatorTargetId\":" + userId + "}}")
    }
})

app.get("/Game/Badge/HasBadge.ashx", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Game/Badge/HasBadge.ashx?UserID=" + req.query.UserID + "&BadgeID=" + req.query.BadgeID,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementext = ""
        if (enableBadges == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.startsWith("<Badge userId=" + req.query.UserID) && line.includes("badgeId=" + req.query.BadgeID)) {
                        replacementext = line
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementext != "") {
                        res.status(200).send("Success")
                    }
                    else {
                        res.status(200).send("Failure")
                    }

                }
                else {
                    res.status(200).send("Failure")
                }
            }
            else {
                res.status(200).send("Failure")
            }
        }
        else {
            res.status(200).send("Failure")
        }
    }
})

app.get("/v1/users/:uid/badges/awarded-dates", async (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/users/:uid/badges/awarded-dates?badgeIds=" + (typeof (req.query.badgeIds) == "object" ? req.query.badgeIds.join(',') : req.query.badgeIds),
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementext = ""
        if (enableBadges == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                var badges = []
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.startsWith("<Badge userId=" + req.params.uid) && line.endsWith(">")) {
                        badges.push(line)
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (badges.length > 0) {
                        var jsondata = ""

                        if (typeof (req.query.badgeIds) == "string") {
                            var splitted = []
                            if (req.query.badgeIds.includes(',')) splitted = req.query.badgeIds.split(',');

                            if (splitted.length > 0) {
                                for (var i = 0; i < splitted.length; i++) {
                                    badges.forEach((badgedata) => {
                                        if (badgedata.includes("badgeId=" + splitted[i])) {
                                            if (badgedata.includes("awardDate=\"")) {
                                                const regex = new RegExp("awardDate=\"(.*)\"")
                                                var match = regex.exec(badgedata)
                                                if (match != null && match.length > 0) {
                                                    jsondata += "{\"badgeId\": " + splitted[i] + ", \"awardedDate\": \"" + match[1] + "\"}, "
                                                }
                                            }
                                        }
                                    })
                                }
                            }
                            else {
                                badges.forEach((badgedata) => {
                                    if (badgedata.includes("badgeId=" + req.query.badgeIds)) {
                                        if (badgedata.includes("awardDate=\"")) {
                                            const regex = new RegExp("awardDate=\"(.*)\"")
                                            var match = regex.exec(badgedata)
                                            if (match != null && match.length > 0) {
                                                jsondata += "{\"badgeId\": " + req.query.badgeIds + ", \"awardedDate\": \"" + match[1] + "\"}, "
                                            }
                                        }
                                    }
                                })
                            }
                        }
                        else if (typeof (req.query.badgeIds) == "object") {
                            if (req.query.badgeIds != undefined) {
                                for (var i = 0; i < req.query.badgeIds.length; i++) {
                                    badges.forEach((badgedata) => {
                                        if (badgedata.includes("badgeId=" + req.query.badgeIds[i])) {
                                            if (badgedata.includes("awardDate=\"")) {
                                                const regex = new RegExp("awardDate=\"(.*)\"")
                                                var match = regex.exec(badgedata)
                                                if (match != null && match.length > 0) {
                                                    jsondata += "{\"badgeId\": " + req.query.badgeIds[i] + ", \"awardedDate\": \"" + match[1] + "\"}, "
                                                }
                                            }
                                        }
                                    })
                                }
                            }
                        }
                        res.status(200).send("{\"data\": [" + jsondata.slice(0, jsondata.length - 2) + "]}")
                    }
                    else {
                        res.status(200).send("{\"data\": []}")
                    }

                }
                else {
                    res.status(200).send("{\"data\": []}")
                }
            }
            else {
                res.status(200).send("{\"data\": []}")
            }
        }
        else {
            res.status(200).send("{\"data\": []}")
        }
    }
})
async function getUsernameFromMemory(userid) {
    var value = "unknown"

    memoryUsers.forEach((user) => {
        if (user["userId"] == userid) {
            if (user["username"] != undefined) {
                value = user["username"]
                return
            }
        }
    })
    return value
}

app.get("/badges/list-badges-for-place/json", (_, res) => {
    res.status(200).send("{\"Badges\": []. \"FinalPage\": true, \"PageSize\": 50}")
})
app.post("/Game/Badge/AwardBadge.ashx", async (req, res) => {
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Game/Badge/AwardBadge.ashx?UserID=" + req.query.UserID + "&BadgeID=" + req.query.BadgeID,
                method: "POST"
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var sent = false
        if (enableBadges == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            memoryUserBadge.forEach((array) => {
                if (array["userId"] == req.query.UserID && array["badgeId"] == req.query.BadgeID) {
                    res.status(200).send(0)
                    sent = true
                    return
                }
            })
            if (sent == true) return
            memoryUserBadge.push({ "userId": req.query.UserID, "badgeId": req.query.BadgeID });
            var verified = false
            var replacementtext = ""
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.startsWith("<Badge userId=" + req.query.UserID + " badgeId=" + req.query.BadgeID) && line.endsWith(">")) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        res.status(200).send(0)
                        return
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<Badge userId=" + req.query.UserID + " badgeId=" + req.query.BadgeID + " awardDate=\"" + new Date(Date.UTC()).toISOString() + "\">\r\n")
                    }

                }
                else {
                    //do nothing
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<Badge userId=" + req.query.UserID + " badgeId=" + req.query.BadgeID + " awardDate=\"" + new Date(Date.UTC()).toISOString() + "\">\r\n")
            }
        }
        if (isNumeric(req.query.BadgeID)) {
            var file = filesystem.readFileSync("./badges.json")
            var jsonresult = JSON.parse(file)
            if (jsonresult[req.query.BadgeID.toString()] != undefined) {
                getUsernameFromMemory(req.query.UserID).then((usernameresult) => {
                    res.send(usernameresult + " won " + username + "'s \"" + jsonresult[req.query.BadgeID.toString()] + "\" award!")
                })
                sent = true
                return
            }
            if (sent == false) {
                if (isRobloxAvailable) {
                    var options1 = {
                        host: 'badges.roblox.com',
                        port: 443,
                        path: '/v1/badges/' + req.query.BadgeID,
                        method: "GET"
                    }
                    https.get(options1, (res2) => {
                        var infoiresult = ""
                        res2.setEncoding("utf8")
                        res2.on("data", (chunk) => {
                            infoiresult += chunk
                        })
                        res2.on("end", () => {
                            var jsoninfo1 = JSON.parse(infoiresult)
                            getUsernameFromMemory(req.query.UserID).then((usernameresult) => {
                                if (jsoninfo1["name"] != undefined) {
                                    res.send(usernameresult + " won " + username + "'s " + jsoninfo1["name"])
                                    sent = true
                                }
                                else {
                                    if (sent == false) res.send(usernameresult + " won " + username + "'s \"Stub Badge Name\" award!")
                                }
                            })
                        })
                    })
                }
                else {
                    getUsernameFromMemory(req.query.userId).then((usernameresult) => {
                        if (sent == false) res.send(usernameresult + " won " + username + "'s \"Stub Badge Name\" award!")
                        sent = true
                    })
                }
            }
            if (verbose) {
                console.log("\x1b[32m%s\x1b[0m", "<INFO> Awarding Badge: " + req.query.BadgeID)
            }

        }
        else {
            res.status(400).end()
        }
    }
})

app.post("/assets/award-badge", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/assets/award-badge?userId=" + req.query.userId + "&badgeId=" + req.query.badgeId,
                method: "POST"
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write("")
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var sent = false
        if (enableBadges == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            memoryUserBadge.forEach((array) => {
                if (array["userId"] == req.query.userId && array["badgeId"] == req.query.badgeId) {
                    res.status(200).send(0)
                    sent = true
                    return
                }
            })
            if (sent == true) return
            var verified = false
            var replacementtext = ""
            memoryUserBadge.push({ "userId": req.query.userId, "badgeId": req.query.badgeId });
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.startsWith("<Badge userId=" + req.query.userId + " badgeId=" + req.query.badgeId) && line.endsWith(">")) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        res.status(200).send(0)
                        return
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<Badge userId=" + req.query.userId + " badgeId=" + req.query.badgeId + " awardDate=\"" + new Date(Date.now()).toISOString() + "\">\r\n")
                    }

                }
                else {
                    //do nothing
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<Badge userId=" + req.query.userId + " badgeId=" + req.query.badgeId + " awardDate=\"" + new Date(Date.now()).toISOString() + "\">\r\n")
            }
        }
        if (isNumeric(req.query.badgeId)) {
            var file = filesystem.readFileSync("./badges.json")
            var jsonresult = JSON.parse(file)
            if (jsonresult[req.query.badgeId.toString()] != undefined) {
                getUsernameFromMemory(req.query.userId).then((usernameresult) => {
                    res.send(usernameresult + " won " + username + "'s " + jsonresult[req.query.badgeId.toString()])
                })
                sent = true
                return
            }
            if (sent == false) {
                if (isRobloxAvailable) {
                    var options1 = {
                        host: 'badges.roblox.com',
                        port: 443,
                        path: '/v1/badges/' + req.query.badgeId,
                        method: "GET"
                    }
                    https.get(options1, (res2) => {
                        var infoiresult = ""
                        res2.setEncoding("utf8")
                        res2.on("data", (chunk) => {
                            infoiresult += chunk
                        })
                        res2.on("end", () => {
                            var jsoninfo1 = JSON.parse(infoiresult)
                            getUsernameFromMemory(req.query.userId).then((usernameresult) => {
                                if (jsoninfo1["name"] != undefined) {
                                    res.send(usernameresult + " won " + username + "'s \"" + jsoninfo1["name"] + "\" award!")
                                    sent = true
                                }
                                else {
                                    if (sent == false) res.send(usernameresult + " won " + username + "'s \"Stub Badge Name\" award!")
                                }
                            })
                        })
                    })
                }
            }
            else {
                getUsernameFromMemory(req.query.userId).then((usernameresult) => {
                    if (sent == false) res.send(usernameresult + " won " + username + "'s \"Stub Badge Name\" award!")
                    sent = true
                })
            }
            if (verbose) {
                console.log("\x1b[32m%s\x1b[0m", "<INFO> Awarding Badge: " + req.query.badgeId)
            }

        }
        else {
            res.status(400).end()
        }
    }
})

app.get("/v1/gametemplates", (_, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\":[" + (filesystem.existsSync("./gametemplates.json") ? filesystem.readFileSync("./gametemplates.json", "utf8") : "") + "]}")
})

app.get("/IDE/Upload.aspx", (_, res) => {
    res.status(200).send("Uploading maps is not supported in 2020E and below, since it's supposed to be local anyways. HOWEVER! You can upload your game locally if you allow uploading files and use 2020M+")
})

app.get("/v1/user/groups/canmanage", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\":[]}")
})
app.get("//users/:userid/canmanage/:placeid", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./games.json")) {
        var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        if (json.length > 0) {
            var canManage = false
            for (var i = 0; i < json.length; i++) {
                if (json[i]["rootPlaceId"] == req.params.placeid) {
                    canManage = true
                    break
                }
            }
            res.send("{ \"Success\": true, \"CanManage\": " + canManage + " }")
        }
        else {
            res.send("{ \"Success\": true, \"CanManage\": false }")
        }
    } else {
        res.send("{ \"Success\": true, \"CanManage\": true }")
    }
})

function getMD5FileHash(path) {
    return new Promise((resolve, reject) => {
        const hash = crypto.createHash("md5")
        const stream = filesystem.createReadStream(path)
        stream.on("error", err => reject(err))
        stream.on("data", chunk => hash.update(chunk))
        stream.on("end", () => resolve(hash.digest('hex')))
    })
}
app.get("/GetAllowedMD5Hashes/", async (_, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        var options = {
            host: ip,
            port: 80,
            path: "/GetAllowedMD5Hashes/?apiKey=",
            method: "GET"
        }

        http.get(options, (res1) => {
            res1.setEncoding("utf8")

            var result = ""
            res1.on("data", (chunk) => {
                result += chunk
            })

            res1.on("end", () => {
                res.status(200).send(result)
            })
        })
    }
    else {
        if (filesystem.existsSync("./RobloxPlayerBeta.exe")) {
            const hash = await getMD5FileHash("./RobloxPlayerBeta.exe")
            res.status(200).send("{\"data\":[\"" + hash + "\"]}")
        }
        else {
            res.status(200).send("{\"data\":[\"\"]}")
        }
    }
})

app.get("/GetAllowedSecurityVersions/", (_, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        var options = {
            host: ip,
            port: 80,
            path: "/GetAllowedSecurityVersions/?apiKey=",
            method: "GET"
        }

        http.get(options, (res1) => {
            res1.setEncoding("utf8")

            var result = ""
            res1.on("data", (chunk) => {
                result += chunk
            })

            res1.on("end", () => {
                res.status(200).send(result)
            })
        })
    }
    else {
        if (filesystem.existsSync("./version.txt")) {
            var stringcheck = filesystem.readFileSync("./version.txt", "utf8")
            if (stringcheck.includes('[') || stringcheck.includes(']') || stringcheck.includes('{') || stringcheck.includes('}')) {
                console.log("\x1b[31m%s\x1b[0m", "<ERROR> Special characters are not allowed in version.txt")
                res.status(200).send("{\"data\":[]}")
                return
            }
            res.status(200).send("{\"data\":[" + filesystem.readFileSync("./version.txt") + "]}")
        }
        else {
            res.status(200).send("{\"data\":[\"0.0.0pcplayer\"]}")
        }
    }
})

app.post("/v1/places/:placeId", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var found = false
    var found1 = false
    if (filesystem.existsSync("./gametemplates.json")) {
        var jsondata = JSON.parse("{\"data\": [" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}")

        if (jsondata["data"].length > 0) {
            jsondata["data"].forEach((template) => {
                if (template["universe"]["rootPlaceId"] == req.query.placeId) {
                    found1 = true
                    res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": " + template["universe"]["id"] + ", \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
                    return
                }
            })
        }
        else {
            res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": 2, \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
        }
    }

    if (found1 == false) {
        if (filesystem.existsSync("./games.json")) {
            var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

            if (jsondata.length > 0) {
                jsondata.forEach((game) => {
                    if (game["rootPlaceId"] == req.query.placeId) {
                        found = true
                        res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": " + game["id"] + ", \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
                        return
                    }
                })
            }
            else {
                res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": 2, \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
            }
        }
        if (found == false) res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": 2, \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
    }
})

app.patch("/v1/places/:placeId", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var found = false
    var found1 = false
    var oldname = ""
    var olddescription = ""
    var oldApiAccess = false


    if (filesystem.existsSync("./gametemplates.json")) {
        var jsondata = JSON.parse("{\"data\": [" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}")

        if (jsondata["data"].length > 0) {
            jsondata["data"].forEach((template) => {
                if (template["universe"]["rootPlaceId"] == req.query.placeId) {
                    found1 = true
                    res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": " + template["universe"]["id"] + ", \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
                    return
                }
            })
        }
        else {
            res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": 2, \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
        }
    }

    if (found1 == false) {
        if (filesystem.existsSync("./games.json")) {
            var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))
            var marketplacejson = JSON.parse(filesystem.readFileSync("./marketplace.json", "utf8"))

            if (jsondata.length > 0) {
                for (var i = 0; i < jsondata.length; i++) {
                    if (jsondata[i]["rootPlaceId"] == req.params.placeId) {
                        found = true
                        oldname = jsondata[i]["name"]
                        olddescription = jsondata[i]["description"]
                        oldApiAccess = jsondata[i]["studioAccessToApisAllowed"]
                        jsondata[i]["updated"] = new Date(Date.now()).toISOString()
                        var keys = Object.keys(req.body)

                        for (var x = 0; x < keys.length; x++) {
                            if (jsondata[i][keys[x]] != undefined) {
                                jsondata[i][keys[x]] = req.body[keys[x]]

                            }
                        }
                        break
                    }
                }
                for (var i = 0; i < marketplacejson.length; i++) {
                    if (marketplacejson[i]["id"] == req.params.placeId) {
                        if (req.body.name != undefined) marketplacejson[i]["name"] = (req.body.name != undefined ? req.body.name : oldname)
                        if (req.body.description != undefined) marketplacejson[i]["description"] = (req.body.description != undefined ? req.body.description : olddescription)
                        break
                    }
                }

                var jsondatarecompiled = JSON.stringify(jsondata, null, 4)
                var marketplacerecompiled = JSON.stringify(marketplacejson, null, 4)

                filesystem.unlinkSync("./games.json")
                filesystem.unlinkSync("./marketplace.json")

                filesystem.writeFileSync("./games.json", jsondatarecompiled)
                filesystem.writeFileSync("./marketplace.json", marketplacerecompiled)

                jsondata.forEach((game) => {
                    if (game["rootPlaceId"] == req.params.placeId) {
                        found = true
                        res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": " + game["id"] + ", \"name\": \"" + (req.body.name != undefined ? req.body.name : oldname) + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description : olddescription).replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\"}")
                        return
                    }
                })
            }
            else {
                res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": 2, \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
            }
        }
        if (found == false) res.status(200).send("{\"id\": " + req.params.placeId + ", \"universeId\": 2, \"name\": \"" + req.body.name + "\", \"description\": \"" + req.body.description + "\"}")
    }
})

app.patch("/v1/universes/:id/configuration", (req, res) => {
    var found = false
    if (filesystem.existsSync("./games.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))
        var marketplacejson = JSON.parse(filesystem.readFileSync("./marketplace.json", "utf8"))
        var placeid = 1818
        var oldname = ""
        var olddescription = ""
        var oldApiAccess = false

        if (jsondata.length > 0) {
            for (var i = 0; i < jsondata.length; i++) {
                if (jsondata[i]["id"] == req.params.id) {
                    found = true
                    placeid = jsondata[i]["rootPlaceId"]
                    oldname = jsondata[i]["name"]
                    olddescription = jsondata[i]["description"]
                    oldApiAccess = jsondata[i]["studioAccessToApisAllowed"]
                    jsondata[i]["updated"] = new Date(Date.now()).toISOString()
                    var keys = Object.keys(req.body)

                    for (var x = 0; x < keys.length; x++) {
                        if (jsondata[i][keys[x]] != undefined) {
                            jsondata[i][keys[x]] = req.body[keys[x]]

                        }
                    }
                    break
                }
            }
            for (var i = 0; i < marketplacejson.length; i++) {
                if (marketplacejson[i]["id"] == placeid) {
                    if (req.body.name != undefined) marketplacejson[i]["name"] = (req.body.name != undefined ? req.body.name : oldname)
                    if (req.body.description != undefined) marketplacejson[i]["description"] = (req.body.description != undefined ? req.body.description : olddescription)
                    break
                }
            }

            var jsondatarecompiled = JSON.stringify(jsondata, null, 4)
            var marketplacerecompiled = JSON.stringify(marketplacejson, null, 4)

            filesystem.unlinkSync("./games.json")
            filesystem.unlinkSync("./marketplace.json")

            filesystem.writeFileSync("./games.json", jsondatarecompiled)
            filesystem.writeFileSync("./marketplace.json", marketplacerecompiled)
            res.status(200).send("{\"allowPrivateServers\": true, \"privateServerPrice\": 0, \"id\": " + placeid + ", \"name\": \"" + (req.body.name != undefined ? req.body.name : oldname) + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description : olddescription) + "\", \"studioAccessToApisAllowed\": " + (req.body.studioAccessToApisAllowed != undefined ? req.body.studioAccessToApisAllowed : oldApiAccess) + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"isArchived\": false}")
        }
        else {
            res.status(200).send("{\"allowPrivateServers\": true, \"privateServerPrice\": 0, \"id\": 1818, \"name\": \"" + (req.body.name != undefined ? req.body.name : "") + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description : "") + "\", \"studioAccessToApisAllowed\": " + (req.body.studioAccessToApisAllowed != undefined ? req.body.studioAccessToApisAllowed : "true") + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"isArchived\": false}")
        }
    }
    if (found == false) res.status(200).send("{\"allowPrivateServers\": true, \"privateServerPrice\": 0, \"id\": 1818, \"name\": \"" + (req.body.name != undefined ? req.body.name : "") + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description : "") + "\", \"studioAccessToApisAllowed\": " + (req.body.studioAccessToApisAllowed != undefined ? req.body.studioAccessToApisAllowed : "true") + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"isArchived\": false}")
})

app.patch("/v2/universes/:id/configuration", (req, res) => {
    var found = false
    if (filesystem.existsSync("./games.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))
        var marketplacejson = JSON.parse(filesystem.readFileSync("./marketplace.json", "utf8"))
        var placeid = 1818
        var oldname = ""
        var olddescription = ""
        var oldApiAccess = false

        if (jsondata.length > 0) {
            for (var i = 0; i < jsondata.length; i++) {
                if (jsondata[i]["id"] == req.params.id) {
                    found = true
                    placeid = jsondata[i]["rootPlaceId"]
                    oldname = jsondata[i]["name"]
                    olddescription = jsondata[i]["description"]
                    oldApiAccess = jsondata[i]["studioAccessToApisAllowed"]
                    jsondata[i]["updated"] = new Date(Date.now()).toISOString()
                    var keys = Object.keys(req.body)

                    for (var x = 0; x < keys.length; x++) {
                        if (jsondata[i][keys[x]] != undefined) {
                            jsondata[i][keys[x]] = req.body[keys[x]]

                        }
                    }
                    break
                }
            }
            for (var i = 0; i < marketplacejson.length; i++) {
                if (marketplacejson[i]["id"] == placeid) {
                    if (req.body.name != undefined) marketplacejson[i]["name"] = (req.body.name != undefined ? req.body.name : oldname)
                    if (req.body.description != undefined) marketplacejson[i]["description"] = (req.body.description != undefined ? req.body.description : olddescription)
                    break
                }
            }

            var jsondatarecompiled = JSON.stringify(jsondata, null, 4)
            var marketplacerecompiled = JSON.stringify(marketplacejson, null, 4)

            filesystem.unlinkSync("./games.json")
            filesystem.unlinkSync("./marketplace.json")

            filesystem.writeFileSync("./games.json", jsondatarecompiled)
            filesystem.writeFileSync("./marketplace.json", marketplacerecompiled)
            res.status(200).send("{\"allowPrivateServers\": true, \"privateServerPrice\": 0, \"id\": " + placeid + ", \"name\": \"" + (req.body.name != undefined ? req.body.name : oldname) + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description.replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") : olddescription.replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"")) + "\", \"studioAccessToApisAllowed\": " + (req.body.studioAccessToApisAllowed != undefined ? req.body.studioAccessToApisAllowed : oldApiAccess) + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"isArchived\": false}")
        }
        else {
            res.status(200).send("{\"allowPrivateServers\": true, \"privateServerPrice\": 0, \"id\": 1818, \"name\": \"" + (req.body.name != undefined ? req.body.name : "") + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description.replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") : "") + "\", \"studioAccessToApisAllowed\": " + (req.body.studioAccessToApisAllowed != undefined ? req.body.studioAccessToApisAllowed : "true") + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"isArchived\": false}")
        }
    }
    if (found == false) res.status(200).send("{\"allowPrivateServers\": true, \"privateServerPrice\": 0, \"id\": 1818, \"name\": \"" + (req.body.name != undefined ? req.body.name : "") + "\", \"description\": \"" + (req.body.description != undefined ? req.body.description.replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") : "") + "\", \"studioAccessToApisAllowed\": " + (req.body.studioAccessToApisAllowed != undefined ? req.body.studioAccessToApisAllowed : "true") + ", \"permissions\": { \"IsThirdPartyTeleportAllowed\": true, \"IsThirdPartyAssetAllowed\": true, \"IsThirdPartyPurchaseAllowed\": true, \"IsClientTeleportAllowed\": true }, \"universeAvatarMinScales\": {\"height\": 0.9, \"width\": 0.7, \"head\": 0.95, \"depth\": 0, \"proportion\": 0, \"bodyType\": 0}, \"universeAvatarMaxScales\": {\"height\": 1.05, \"width\": 1, \"head\": 1, \"depth\": 1, \"proportion\": 1, \"bodyType\": 1}, \"isArchived\": false}")
})

app.post("/v1/autolocalization/games/:id/autolocalizationtable", (req, res) => {
    if (req.params.id > 0) {
        res.status(200).send("{\"isAutolocalizationEnabled\": false, \"shouldUseLocalizationTable\": false, \"autoLocalizationTableId\": \"" + randomUUID() + "\", \"assetId\": 0}") //STUB
    }
    else {
        res.status(400).send("{\"errors\": [{ \"code\": 14, \"message\": \"Invalid game id\"}]}")
    }
})

app.get("/v1/autolocalization/games/:id", (_, res) => {
    res.status(200).send("{}")
})
app.get("/v1/source-language/games/:id", (req, res) => {
    if (req.params.id > 0) {
        res.status(200).send("{\"name\": \"English\", \"nativeName\": \"English\", \"languageCode\": \"en\"}")
    }
    else {
        res.status(400).send("{\"errors\": [{\"code\":14, \"message\": \"Invalid game id\"}]}")
    }
})

app.get("/v1/automatic-translation/games/:id/feature-status", (req, res) => {
    if (req.params.id > 0) {
        res.status(200).send("{\"gameId\": " + req.params.id + ", \"isAutomaticTranslationAllowed\": false, \"isAutomaticTranslationSwitchesUIEnabled\": false}")
    }
    else {
        res.status(500).send("{\"errors\":[{\"code\": 0, \"message\": \"InternalServerError\"}]}")
    }
})

app.get("/v1/supported-languages/games/:id/automatic-translation-status", (req, res) => {
    if (req.params.id > 0) {
        res.status(200).send("{\"data\":[{\"languageCodeType\": \"Language\", \"languageCode\": \"en\", \"isAutomaticTranslationEnabled\": false}]}")
    }
    else {
        res.status(400).send("{\"errors\": [{\"code\": 14, \"message\": \"Invalid game id\"}]}")
    }
})

app.get("/v1/automatic-translation/languages/:lang/target-languages", (req, res) => {
    res.status(200).send("{\"sourceLanguage\": \"" + req.params.lang + "\", \"targetLanguages\": []}")
})
app.get("/v1/users/:userid/items/gamepass/:passid", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/users/" + req.params.userid + "/itmes/gamepass/" + req.params.passid,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
        } catch {
            res.status(500).end()
        }
    }
    else {
        if (typeof (req.params.userid) == "number" && typeof (req.params.passid) == "number") {
            var verified = false
            var hasOwnedAsset = false
            if (enableOwnedAssets == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
                if (filesystem.existsSync(RBDFpath)) {
                    const stream = filesystem.createReadStream(RBDFpath)

                    const rl = readline.createInterface({
                        input: stream,
                        crlfDelay: Infinity
                    })

                    for await (const line of rl) {
                        if (line == "RBDF==") {
                            verified = true
                        }
                        else if (line.trim() == "<OwnedAsset userId=" + req.params.userid + " AssetId=" + req.params.passid + ">") {
                            hasOwnedAsset = true
                        }
                    }
                    stream.destroy()
                    rl.close()
                    if (verified == true) {
                        if (hasOwnedAsset == true) {
                            var assetname = await getAssetName(req.params.passid)
                            res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[{\"id\":" + req.params.passid + ", \"name\": \"" + assetname + "\", \"type\": \"GamePass\", \"instanceId\": null}]}")
                        }
                        else {
                            res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
                        }
                    }
                    else {
                        res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
                    }
                }
                else {
                    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
                }
            }
            else {
                res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
            }
        }
        else {
            res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\":[]}")
        }
    }
})

async function getAssetName(id) {
    var result = "Stub Place/Product Name"
    if (filesystem.existsSync("./marketplace.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./marketplace.json"))
        jsondata.forEach((asset) => {
            if (asset["id"] != undefined) {
                if (asset["id"] == id) {
                    if (isNumeric(id)) {
                        result = ((asset["name"] != undefined) ? asset["name"] : "Stub Place/Product Name")
                        return
                    }
                }
            }
        })
    }
    return result
}

app.get("/Marketplace/productinfo", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var sent = false
    if (filesystem.existsSync("./marketplace.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./marketplace.json"))

        jsondata.forEach((asset) => {
            if (asset["id"] != undefined) {
                if (asset["id"] == ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId)) {
                    if (isNumeric(((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId))) {
                        res.status(200).send("{\"TargetId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ", \"ProductType\":\"User Product\", \"AssetId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ", \"ProductId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ",\"Name\":\"" + ((asset["name"] != undefined) ? asset["name"] : "Stub Place/Product Name") + "\",\"Description\":\"" + ((asset["description"] != undefined) ? asset["description"].replace(new RegExp("\"", "g"), "\\\"").replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n") : "") + "\", \"AssetTypeId\":" + ((asset["assetType"] != undefined && typeof (asset["assetType"]) == "number") ? asset["assetType"] : "34") + ", \"Creator\": {\"Id\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + ", \"Name\":\"" + (asset["creatorName"] != undefined ? asset["creatorName"] : username) + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + "}, \"IconImageAssetId\":" + ((asset["imageId"] != undefined && typeof (asset["imageId"]) == "number") ? asset["imageId"] : "133293265") + ",\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":" + ((asset["robux"] != undefined && asset["robux"] > -1) ? asset["robux"] : "0") + ",\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
                        sent = true
                    }
                    else {
                        res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
                    }
                }
            }
        })
        if (sent) return
        if (isNumeric(((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId))) {
            res.status(200).send("{\"TargetId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ", \"ProductType\":\"User Product\", \"AssetId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ", \"ProductId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ",\"Name\":\"Stub Place/Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":" + userId + ", \"Name\":\"" + username + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + userId + "}, \"IconImageAssetId\":133293265,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
    else {
        if (isNumeric(((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId))) {
            res.status(200).send("{\"TargetId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ", \"ProductType\":\"User Product\", \"AssetId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ", \"ProductId\":" + ((req.query.assetid != undefined) ? req.query.assetid : req.query.assetId) + ",\"Name\":\"Stub Place/Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":" + userId + ", \"Name\":\"" + username + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + userId + "}, \"IconImageAssetId\":133293265,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
})

app.get("/v1/favorites/bundles/:id/count", (req, res) => {
    res.status(200).send(0)
})

app.get("/v1/favorites/users/:userid/bundles/:id/favorite", (req, res) => {
    res.status(200).send("null")
})

app.get("/v1/users/authenticated", (_, res) => {
    res.setHeader("cache-control", "no-cache")
    res.status(200).send("{\"id\": " + userId + ", \"name\": \"" + username + "\", \"displayName\": \"" + username + "\"}")
})

app.get("/v1/users/authenticated/app-launch-info", (_, res) => {
    res.status(200).send("{\"ageBracket\": 0, \"countryCode\": \"US\", \"isPremium\": false, \"id\": " + userId + ", \"name\": \"" + username + "\", \"displayName\": \"" + username + "\"}")
})

app.get("/v1/users/authenticated/roles", (_, res) => {
    res.status(200).send("{ \"roles\": [] }")
})

app.get("/v1/products/:id", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./marketplace.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./marketplace.json", "utf8"))
        var sent = false

        jsondata.forEach((product) => {
            if (req.params.id == product["id"]) {
                sent = true
                res.status(200).send("{\"assetName\":\"" + product["name"] + "\", \"price\": " + (product["robux"] != undefined ? product["robux"] : 0) + ", \"reason\": \"Success\", \"productId\": " + req.params.id + ", \"currency\": 1, \"assetId\":" + req.params.id + ", \"assetType\": \"" + translateAssetTypeIdToAssetType(product["assetType"]) + "\", \"assetTypeDisplayName\": \"" + translateAssetTypeIdToAssetType(product["assetType"]) + "\", \"assetIsWearable\": true, \"sellerName\": \"" + (product["creatorName"] != undefined ? product["creatorName"] : "ROBLOX") + "\", \"isMultiPrivateSale\": false}")
            }
        })
        if (sent == false) res.status(200).send("{\"assetName\":\"Stub Place/Product Name\", \"price\": 0, \"reason\": \"Success\", \"productId\": " + req.params.id + ", \"currency\": 1, \"assetId\":133293265, \"assetType\": \"TShirt\", \"assetTypeDisplayName\": \"T-Shirt\", \"assetIsWearable\": true, \"sellerName\": \"ROBLOX\", \"isMultiPrivateSale\": false}")
    }
    else {
        res.status(200).send("{\"assetName\":\"Stub Place/Product Name\", \"price\": 0, \"reason\": \"Success\", \"productId\": " + req.params.id + ", \"currency\": 1, \"assetId\":133293265, \"assetType\": \"TShirt\", \"assetTypeDisplayName\": \"T-Shirt\", \"assetIsWearable\": true, \"sellerName\": \"ROBLOX\", \"isMultiPrivateSale\": false}")
    }
})

app.get("/marketplace/productdetails", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./marketplace.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./marketplace.json"))
        var sent = false
        jsondata.forEach((asset) => {
            if (asset["id"] != undefined) {
                if (asset["id"] == req.query.productId) {
                    if (isNumeric(req.query.productId)) {
                        res.status(200).send("{\"TargetId\":" + req.query.productId + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.query.productId + ", \"ProductId\":" + req.query.productId + ",\"Name\":\"" + ((asset["name"] != undefined) ? asset["name"] : "Stub Place/Product Name") + "\",\"Description\":\"" + ((asset["description"] != undefined) ? asset["description"].replace(new RegExp("\"", "g"), "\\\"").replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n") : "") + "\", \"AssetTypeId\":" + ((asset["assetType"] != undefined && typeof (asset["assetType"]) == "number") ? asset["assetType"] : "34") + ", \"Creator\": {\"Id\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + ", \"Name\":\"" + (asset["creatorName"] != undefined ? asset["creatorName"] : username) + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + "}, \"IconImageAssetId\":" + ((asset["imageId"] != undefined && typeof (asset["imageId"]) == "number") ? asset["imageId"] : "133293265") + ",\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":" + ((asset["robux"] != undefined && asset["robux"] > -1) ? asset["robux"] : "0") + ",\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
                        sent = true
                        return
                    }
                    else {
                        res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
                        return
                    }
                }
            }
        })
        if (sent) return
        if (isNumeric(req.query.productId)) {
            res.status(200).send("{\"TargetId\":" + req.query.productId + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.query.productId + ", \"ProductId\":" + req.query.productId + ",\"Name\":\"Stub Place/Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":" + userId + ", \"Name\":\"" + username + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + userId + "}, \"IconImageAssetId\":133293265,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
    else {
        if (isNumeric(req.query.productId)) {
            res.status(200).send("{\"TargetId\":" + req.query.productId + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.query.productId + ", \"ProductId\":" + req.query.productId + ",\"Name\":\"Stub Place/Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":" + userId + ", \"Name\":\"" + username + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + userId + "}, \"IconImageAssetId\":133293265,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
})

app.get("/v2/assets/:id/details", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (filesystem.existsSync("./marketplace.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./marketplace.json"))
        var sent = false
        jsondata.forEach((asset) => {
            if (asset["id"] != undefined) {
                if (asset["id"] == req.params.id) {
                    if (isNumeric(req.params.id)) {
                        res.status(200).send("{\"TargetId\":" + req.params.id + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.params.id + ", \"ProductId\":" + req.params.id + ",\"Name\":\"" + ((asset["name"] != undefined) ? asset["name"] : "Stub Place/Product Name") + "\",\"Description\":\"" + ((asset["description"] != undefined) ? asset["description"].replace(new RegExp("\"", "g"), "\\\"").replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n") : "") + "\", \"AssetTypeId\":" + ((asset["assetType"] != undefined && typeof (asset["assetType"]) == "number") ? asset["assetType"] : "34") + ", \"Creator\": {\"Id\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + ", \"Name\":\"" + (asset["creatorName"] != undefined ? asset["creatorName"] : username) + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + "}, \"IconImageAssetId\":" + ((asset["imageId"] != undefined && typeof (asset["imageId"]) == "number") ? asset["imageId"] : "133293265") + ",\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":" + ((asset["robux"] != undefined && asset["robux"] > -1) ? asset["robux"] : "0") + ",\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
                        sent = true
                        return
                    }
                    else {
                        res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
                        return
                    }
                }
            }
        })
        if (sent) return
        if (isNumeric(req.params.id)) {
            res.status(200).send("{\"TargetId\":" + req.params.id + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.params.id + ", \"ProductId\":" + req.params.id + ",\"Name\":\"Stub Place/Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":" + userId + ", \"Name\":\"" + username + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + userId + "}, \"IconImageAssetId\":133293265,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
    else {
        if (isNumeric(req.params.id)) {
            res.status(200).send("{\"TargetId\":" + req.params.id + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.params.id + ", \"ProductId\":" + req.params.id + ",\"Name\":\"Stub Place/Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":" + userId + ", \"Name\":\"" + username + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + userId + "}, \"IconImageAssetId\":133293265,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
})

app.get("/latency-measurements/get-servers-to-ping", (_, res) => {
    res.status(200).end()
})
app.get("/ownership/hasasset", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/ownership/hasasset?userId=" + req.query.userId + "&assetId=" + req.query.assetId,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var hasOwnedAsset = false
        if (enableOwnedAssets == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.trim() == "<OwnedAsset userId=" + req.query.userId + " AssetId=" + req.query.assetId + ">") {
                        hasOwnedAsset = true
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (hasOwnedAsset == true) {
                        res.status(200).send(true)
                    }
                    else {
                        res.status(200).send(false)
                    }
                }
                else {
                    res.status(200).send(false)
                }
            }
            else {
                res.status(200).send(false)
            }
        }
        else {
            res.status(200).send(false)
        }
    }
})

app.post("/marketplace/submitpurchase", (req, res) => {
    if (robux >= req.body["expectedUnitPrice"]) {
        lastProductId = req.body.productId
        robux = robux - req.body["expectedUnitPrice"]
        res.status(200).send("{ \"success\": true, \"status\": \"Bought\", \"receipt\": \"" + randomUUID() + "\" }")
    }
})

app.post("/marketplace/purchase", async (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    if (joining) {
        var options = {
            "host": ip,
            "port": 80,
            "path": "/marketplace/purchase?userId=" + userId,
            "method": "POST"
        }
        var req1 = http.request(options, (res1) => {
            res1.setEncoding("utf-8")

            var result = ""
            res1.on("data", (chunk) => {
                result += chunk
            })
            res1.on("end", () => {
                if (result == "{\"success\": true, \"status\": \"Purchased\"}") {
                    robux = robux - req.body["purchasePrice"]
                }
                res.status(200).send(result)
            })
        })
        req1.write(JSON.stringify(req.body))
        req1.end()
    } else {
        var verified = false
        var replacementtext = ""

        if (enableOwnedAssets == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line == "<OwnedAsset userId=" + ((req.query.userId != undefined) ? req.query.userId : userId) + " AssetId=" + req.body.productId + ">") {
                        replacementtext = line
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        res.status(500).send("{\"status\": \"error\",\"error\": \"Unable to purchase\"}")
                        if (req.query.userId == undefined) robux = robux - req.body["purchasePrice"]
                        return
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<OwnedAsset userId=" + ((req.query.userId != undefined) ? req.query.userId : userId) + " AssetId=" + req.body.productId + ">\r\n")
                    }

                }
                else {
                    res.status(500).end()
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<OwnedAsset userId=" + ((req.query.userId != undefined) ? req.query.userId : userId) + " AssetId=" + req.body.productId + ">\r\n")
            }
            res.status(200).send("{\"success\": true, \"status\": \"Purchased\"}")
            if (req.query.userId == undefined) robux = robux - req.body["purchasePrice"]
        }
        else {
            res.status(200).send("{\"success\": true, \"status\": \"Purchased\"}")
            if (req.query.userId == undefined) robux = robux - req.body["purchasePrice"]
        }
    }
})

app.post("/marketplace/validatepurchase", (_, res) => {
    res.status(200).send("{\"playerId\": " + userId + ", \"placeId\": 1818, \"isValid\": true, \"productId\": " + lastProductId + "}")
})
app.get("/currency/balance", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"robux\": " + robux + ", \"tickets\": 255}")
})

app.get("/v1/name-description/games/:id", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")

    if (req.params.id > 0) {
        var found = false
        var found1 = false
        if (filesystem.existsSync("./gametemplates.json")) {
            var jsondata = JSON.parse("{\"data\": [" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}")

            if (jsondata["data"].length > 0) {
                jsondata["data"].forEach((template) => {
                    if (template["universe"]["id"] == req.params.id) {
                        found1 = true
                        res.status(200).send("{\"data\": [{\"name\":\"" + template["universe"]["name"] + "\", \"description\":\"" + template["universe"]["description"] + "\", \"updateType\": null, \"languageCode\": \"en\"}]}")
                        return
                    }
                })
            }
            else {
                res.status(200).send("{\"data\": [{\"name\":\"ReBlox Place\", \"description\":\"\", \"updateType\": null, \"languageCode\": \"en\"}]}")
            }
        }

        if (found1 == false) {
            if (filesystem.existsSync("./games.json")) {
                var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

                if (jsondata.length > 0) {
                    jsondata.forEach((game) => {
                        if (game["id"] == req.params.id) {
                            found = true
                            res.status(200).send("{\"data\": [{\"name\":\"" + game["name"] + "\", \"description\":\"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"updateType\": null, \"languageCode\": \"en\"}]}")
                            return
                        }
                    })
                }
                else {
                    res.status(200).send("{\"data\": [{\"name\":\"ReBlox Place\", \"description\":\"\", \"updateType\": null, \"languageCode\": \"en\"}]}")
                }
            }
            if (found == false) res.status(200).send("{\"data\": [{\"name\":\"ReBlox Place\", \"description\":\"\", \"updateType\": null, \"languageCode\": \"en\"}]}")
        }
    }
    else {
        res.status(400).send("{\"errors\": [{ \"code\": 14, \"message\": \"Invalid game id\"}]}")
    }
})

app.get("/universes/get-universe-containing-place", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var found = false
    var found1 = false
    if (filesystem.existsSync("./gametemplates.json")) {
        var jsondata = JSON.parse("{\"data\": [" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}")

        if (jsondata["data"].length > 0) {
            jsondata["data"].forEach((template) => {
                if (template["universe"]["rootPlaceId"] == req.query.placeId) {
                    found1 = true
                    res.status(200).send("{\"UniverseId\":" + template["universe"]["id"] + "}")
                    return
                }
            })
        }
        else {
            res.status(200).send("{\"UniverseId\":2}")
        }
    }

    if (found1 == false) {
        if (filesystem.existsSync("./games.json")) {
            var jsondata = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

            if (jsondata.length > 0) {
                jsondata.forEach((game) => {
                    if (game["rootPlaceId"] == req.query.placeId) {
                        found = true
                        res.status(200).send("{\"UniverseId\":" + game["id"] + "}")
                        return
                    }
                })
            }
            else {
                res.status(200).send("{\"UniverseId\":2}")
            }
        }
        if (found == false) res.status(200).send("{\"UniverseId\":2}")
    }
})

app.get("/v1.1/game-start-info/", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"gameAvatarType\":\"PlayerChoice\",\"allowCustomAnimations\":\"True\",\"universeAvatarCollisionType\":\"OuterBox\",\"universeAvatarBodyType\":\"Standard\",\"jointPositioningType\":\"ArtistIntent\",\"message\":\"\",\"universeAvatarAssetOverrides\":[],\"moderationStatus\":null}")
})
app.get("/studio/e.png", (_, res) => {
    res.status(200).end()
})

app.get("/ide/welcome", (_, res) => {
    res.status(200).send("<!DOCTYPE html><body><pre>This studio experience is brought to you by [REDACTED]</pre></body>")
})
app.get("/Setting/QuietGet/ClientSharedSettings/", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{ \"StatsGatheringScriptUrl\": \"\", \"DFFlagLogAutoCamelCaseFixes\": \"True\" }")
})
app.get("/login/RequestAuth.ashx", (_, res) => {
    res.status(200).send("https://www.reblox.zip/Login/Negotiate.ashx?suggest=0")
})

app.get("/Login/Negotiate.ashx", (_, res) => {
    res.setHeader("set-cookie", ".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_" + jwt.sign({ "username": username }, "thisisarebloxprivatekeyforjwtchange", { algorithm: "HS256" }) + "; domain=.reblox.zip; path=/; expires=Tue, 10 Mar 2889 07:28:00 GMT; samesite=lax")
    res.setHeader("strict-transport-security", "max-age=31536000")
    res.status(200).end()
})

app.post("/Login/Negotiate.ashx", (_, res) => {
    res.setHeader("set-cookie", ".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_" + jwt.sign({ "username": username }, "thisisarebloxprivatekeyforjwtchange", { algorithm: "HS256" }) + "; domain=.reblox.zip; path=/; expires=Tue, 10 Mar 2889 07:28:00 GMT; samesite=lax")
    res.setHeader("strict-transport-security", "max-age=31536000")
    res.status(200).end()
})

app.get("/auth/negotiate", (_, res) => {
    res.setHeader("set-cookie", ".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_" + jwt.sign({ "username": username }, "thisisarebloxprivatekeyforjwtchange", { algorithm: "HS256" }) + "; domain=.reblox.zip; path=/; expires=Tue, 10 Mar 2889 07:28:00 GMT; samesite=lax")
    res.setHeader("strict-transport-security", "max-age=31536000")
    res.status(200).end()
})

app.post("/auth/negotiate", (_, res) => {
    res.setHeader("set-cookie", ".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_" + jwt.sign({ "username": username }, "thisisarebloxprivatekeyforjwtchange", { algorithm: "HS256" }) + "; domain=.reblox.zip; path=/; samesite=lax")
    res.setHeader("strict-transport-security", "max-age=31536000")
    res.status(200).end()
})

app.post("/auth/invalidate", (_, res) => {
    res.status(200).end()
})

app.post("/auth/renew", (_, res) => {
    res.status(200).end()
})

app.post("/Game/MachineConfiguration.ashx", (_, res) => {
    res.status(200).end()
})

app.post("/game/validate-machine", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{ \"success\": true }")
})

app.get("/universes/validate-place-join", (_, res) => {
    res.status(200).send(true)
})
app.get("/groups/can-manage-games", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("[]")
})

app.post("//Analytics/Measurement.ashx", (_, res) => {
    res.status(200).end()
})

app.get("/v1/player-policies-client", (_, res) => {
    res.status(200).send("{\"isSubjectToChinaPolicies\": false, \"arePaidRandomItemsRestricted\": false, \"isPaidItemTradingAllowed\": true, \"areAdsAllowed\": false}")
})
app.get("/v1/packages/:id/assets", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(404).send("{\"errors\":[{\"code\": 1, \"message\": \"Package assets are disabled.\"}]}") //STUB
})
app.get("/avatar-thumbnail/json", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"Url\":\"http://reblox.zip/Thumbs/Avatar.ashx\",\"Final\":true,\"SubstitutionType\":0}")
})

app.get("/asset-thumbnail/json", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    res.status(200).send("{\"Url\":\"http://www.reblox.zip/Thumbs/GameIcon.ashx\",\"Final\":true,\"SubstitutionType\":0}")
})

app.get("/universal-app-configuration/v1/behaviors/app-policy/content", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"ChatHeaderSearch\": true, \"ChatPlayTogether\": true, \"GamePlayerCounts\": true, \"Notifications\": true, \"SearchBar\": true, \"GameReportingDisabled\": false, \"FriendFinder\": true, \"UseBottomBar\": true, \"ShowYoutubeAgeAlert\": false, \"ShowDisplayName\": true, \"CatalogReportingDisabled\": false, \"ShowVideoThumbnails\": true, \"EnableInGameHomeIcon\": false, \"EnableVoiceReportAbuseMenu\": true, \"EnablePremiumUserFeatures\": true, \"PlatformGroup\": \"Unknown\", \"RealNamesInDisplayNamesEnabled\": false, \"SystemBarPlacement\": \"Bottom\"}")
})

app.get("/v1/assets/:id/bundles", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    if (useAuth == true) {
        try {
            var options = {
                host: "catalog.roblox.com",
                port: 443,
                path: "/v1/assets/" + req.params.id + "/bundles",
                method: "GET"
            }

            https.get(options, (res1) => {
                var result = ""
                res1.setEncoding("utf8")
                res1.on("data", (chunk) => {
                    result += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(result)
                })
            })
        } catch {
            res.status(500).end()
        }
    }
    else {
        res.setHeader("content-type", "application/json; charset=utf-8")
        res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": []}")
    }
})

app.get("/v1/favorites/users/:userid/assets/:assetid/favorite", (req, res) => {
    res.setHeader("content-type", "application/json")
    res.status(200).send("null") //STUB
})

app.get("/v1/favorites/assets/:assetid/count", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json")
    res.status(200).send(0)
})

app.get("/marketplace/game-pass-product-info", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./marketplace.json")) {
        var jsondata = JSON.parse(filesystem.readFileSync("./marketplace.json"))
        var sent = false
        jsondata.forEach((asset) => {
            if (asset["id"] != undefined) {
                if (asset["id"] == req.query.gamePassId) {
                    if (isNumeric(req.query.gamePassId)) {
                        res.status(200).send("{\"TargetId\":" + req.query.gamePassId + ", \"ProductType\":\"Game Pass\", \"AssetId\":" + req.query.gamePassId + ", \"ProductId\":" + req.query.gamePassId + ",\"Name\":\"" + ((asset["name"] != undefined) ? asset["name"] : "Stub Place/Product Name") + "\",\"Description\":\"" + ((asset["description"] != undefined) ? asset["description"].replace(new RegExp("\"", "g").replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\""), "\\\"") : "") + "\", \"AssetTypeId\":" + ((asset["assetType"] != undefined && typeof (asset["assetType"]) == "number") ? asset["assetType"] : "34") + ", \"Creator\": {\"Id\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + ", \"Name\":\"" + (asset["creatorName"] != undefined ? asset["creatorName"] : username) + "\", \"CreatorType\":\"User\",\"CreatorTargetId\":" + (asset["creatorTargetId"] != undefined ? asset["creatorTargetId"] : userId) + "}, \"IconImageAssetId\":" + ((asset["imageId"] != undefined && typeof (asset["imageId"]) == "number") ? asset["imageId"] : "133293265") + ",\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":" + ((asset["robux"] != undefined && asset["robux"] > -1) ? asset["robux"] : "0") + ",\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
                        sent = true
                    }
                    else {
                        res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
                    }
                }
            }
        })
        if (sent) return
        if (isNumeric(req.query.gamePassId)) {
            res.status(200).send("{\"TargetId\":" + req.query.gamePassId + ",\"ProductType\":\"Game Pass\", \"AssetId\":" + req.query.gamePassId + ", \"ProductId\": " + req.query.gamePassId + ", \"Name\":\"ReBlox Stub Gamepass\", \"Description\":\"Get this for free blud\", \"AssetTypeId\":0,\"Creator\":{\"Id\":" + userId + ",\"Name\":\"" + username + "\",\"CreatorType\":\"User\"},\"IconImageAssetId\":133293265,\"Created\":\"2020-11-15T01:40:49.473Z\",\"Updated\":\"2020-11-15T18:51:09.11Z\", \"PriceInRobux\":0,\"PriceInTickets\":null,\"Sales\":0,\"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false,\"Remaining\":null,\"MinimumMembershipLevel\":0,\"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
    else {
        if (isNumeric(req.query.gamePassId)) {
            res.status(200).send("{\"TargetId\":" + req.query.gamePassId + ",\"ProductType\":\"Game Pass\", \"AssetId\":" + req.query.gamePassId + ", \"ProductId\": " + req.query.gamePassId + ", \"Name\":\"ReBlox Stub Gamepass\", \"Description\":\"Get this for free blud\", \"AssetTypeId\":0,\"Creator\":{\"Id\":" + userId + ",\"Name\":\"" + username + "\",\"CreatorType\":\"User\"},\"IconImageAssetId\":133293265,\"Created\":\"2020-11-15T01:40:49.473Z\",\"Updated\":\"2020-11-15T18:51:09.11Z\", \"PriceInRobux\":0,\"PriceInTickets\":null,\"Sales\":0,\"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false,\"Remaining\":null,\"MinimumMembershipLevel\":0,\"ContentRatingTypeId\":0}")
        }
        else {
            res.status(404).send("{\"errors\":[{\"code\":404,\"Message\":\"NotFound\"}]")
        }
    }
})

app.get("/users/friends/list-json", (req, res) => {
    res.status(200).send("{\"UserId\":" + req.query.userId + ", \"TotalFriends\": 0, \"CurrentPage\":" + req.query.currentPage + ", \"PageSize\": 0, \"TotalPages\": 0, \"FriendsType\": \"" + req.query.friendsType + "\", \"Friends\": []}")
})

app.post("/v1/favorites/users/:uid/assets/:assetid/favorite", (_, res) => {
    res.setHeader("cache-control", "no-cache")

    res.status(200).send("{}") //STUB
})

app.delete("/v1/favorites/users/:uid/assets/:assetid/favorite", (_, res) => {
    res.setHeader("cache-control", "no-cache")

    res.status(200).send("{}") //STUB
})

app.get("/v2/get-user-conversations", (_, res) => {
    res.status(200).send("[]")
})

app.get("/asset-thumbnail/image", (req, res) => {
    try {
        res.setHeader("cache-control", "no-cache")
        var assetfound = false
        if (isNumeric(req.query.assetId)) {
            if (verbose) {
                console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server/file (Image)")
            }
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')
                if (splitted[0] == req.query.assetId.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                    res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
                    res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
                    assetfound = true
                    return
                }
            })
            if (assetfound == false) {
                if (isInternetAvailable == true) {
                    var options1 = {
                        host: 'thumbnails.roblox.com',
                        port: 443,
                        path: '/v1/assets?assetIds=' + req.query.assetId + '&returnPolicy=PlaceHolder&size=' + req.query.width + 'x' + req.query.height + '&format=' + req.query.format,
                        method: "GET"
                    }
                    var infoiresult = ""
                    https.get(options1, (res2) => {

                        res2.setEncoding("utf8")
                        res2.on("data", (chunk) => {
                            infoiresult += chunk
                        })
                        res2.on("end", () => {
                            var jsoninfo1 = JSON.parse(infoiresult)
                            var options2 = {
                                host: 'tr.rbxcdn.com',
                                port: 443,
                                path: jsoninfo1["data"][0]["imageUrl"].toString().slice(21),
                                method: "GET",
                                headers: {
                                    "User-Agent": "totallychrome",
                                    "Accept-Encoding": "gzip,deflate"
                                }
                            }
                            https.get(options2, (res3) => {

                                var data = [], output
                                if (res3.headers["content-encoding"] == 'gzip') {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server (Image) [gzip compression]")
                                    }
                                    var gzip = zlib.createGunzip()
                                    res3.pipe(gzip)
                                    output = gzip
                                }
                                else if (res3.headers["content-encoding"] == 'deflate') {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server (Image) [deflate compression]")
                                    }
                                    var deflate = zlib.createDeflate()
                                    res3.pipe(deflate)
                                    output = deflate
                                }
                                else {
                                    if (verbose) {
                                        console.log("\x1b[34m%s\x1b[0m", "<INFO> Getting " + req.query.assetId + " from Roblox server (Image)")
                                    }
                                    output = res3
                                }
                                output.on("data", (chunk) => {
                                    data.push(chunk)
                                })
                                output.on("end", () => {
                                    var buffer = Buffer.concat(data)
                                    res.setHeader("Content-disposition", "attachment; filename=\"" + req.query.assetId + "." + req.query.format + "\"")
                                    res.status(200).send(buffer)
                                    assetfound = true
                                })
                            })
                        })
                    })
                } else {
                    res.status(404).end()
                }
            }
            else {
                res.status(500).end()
            }

        }
    } catch {
        res.status(404).end()
    }
})
app.get("/UploadMedia/PostImage.aspx", (_, res) => {
    res.status(200).send("Caught you screenshotting ;)")
})
app.get("/gameicon.png", (_, res) => {
    res.setHeader("Content-disposition", "attachment; filename=\"gameicon.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/gameicon.png"))
})
app.get("/v1/user/studiodata", (req, res) => {
    //STUB
    res.status(200).send("{}")
})

app.get("/v1/universes/multiget", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    var games = (filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json") : "[]")
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        var ids = req.query.ids
        var result = ""
        gamesjson.forEach((game) => {
            if (ids.includes(game["id"].toString())) {
                result += "{\"id\": " + game["id"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isArchived\": " + game["isArchived"] + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"isActive\": " + game["isActive"] + ", \"privacyType\": \"" + game["privacyType"] + "\", \"creatorType\": \"" + game["creatorType"] + "\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatorName\": \"" + game["creatorName"] + "\", \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\"},"
                found = true
            }
        })
        if (found == false) res.status(200).send("{\"data\": []}"); else res.status(200).send("{\"data\": [" + result.slice(0, result.length - 1) + "]}")
    }
    else {
        res.status(200).send("{\"data\": []}")
    }
})

app.get("/v1/universes/:id", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    var games = (filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json") : "[]")
    var gamesjson = JSON.parse(games)
    var templates = (filesystem.existsSync("./gametemplates.json") ? filesystem.readFileSync("./gametemplates.json") : "[]")
    var templatesjson = JSON.parse("[" + templates + "]")
    var found = false
    var found1 = false
    if (gamesjson.length > 0) {

        gamesjson.forEach((game) => {
            if (req.params.id == game["id"]) {
                res.status(200).send("{\"id\": " + game["id"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isArchived\": " + game["isArchived"] + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"isActive\": " + game["isActive"] + ", \"privacyType\": \"" + game["privacyType"] + "\", \"creatorType\": \"" + game["creatorType"] + "\", \"creatorTargetId\": " + game["creatorTargetId"] + ", \"creatorName\": \"" + game["creatorName"] + "\", \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\"}")
                found = true
                return
            }
        })
        if (found == false) {
            if (templatesjson.length > 0) {
                templatesjson.forEach((game) => {
                    if (req.params.id == game["universe"]["id"]) {
                        res.status(200).send("{\"id\": " + game["universe"]["id"] + ", \"name\": \"" + game["universe"]["name"] + "\", \"description\": \"" + game["universe"]["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isArchived\": " + game["universe"]["isArchived"] + ", \"rootPlaceId\": " + game["universe"]["rootPlaceId"] + ", \"isActive\": " + game["universe"]["isActive"] + ", \"privacyType\": \"" + game["universe"]["privacyType"] + "\", \"creatorType\": \"" + game["universe"]["creatorType"] + "\", \"creatorTargetId\": " + game["universe"]["creatorTargetId"] + ", \"creatorName\": \"" + game["universe"]["creatorName"] + "\", \"created\": \"" + game["universe"]["created"] + "\", \"updated\": \"" + game["universe"]["updated"] + "\"}")
                        found1 = true
                        return
                    }
                })
                if (found1 == false) res.status(400).send("{\"errors\": [{\"code\": 1, \"message\": \"The universe does not exist.\", \"field\": \"universeId\"}]}");
            }
            else {
                res.status(400).send("{\"errors\": [{\"code\": 1, \"message\": \"The universe does not exist.\", \"field\": \"universeId\"}]}")
            }
        }
    }
    else {
        if (templatesjson.length > 0) {
            templatesjson.forEach((game) => {
                if (req.params.id == game["universe"]["id"]) {
                    res.status(200).send("{\"id\": " + game["universe"]["id"] + ", \"name\": \"" + game["universe"]["name"] + "\", \"description\": \"" + game["universe"]["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"isArchived\": " + game["universe"]["isArchived"] + ", \"rootPlaceId\": " + game["universe"]["rootPlaceId"] + ", \"isActive\": " + game["universe"]["isActive"] + ", \"privacyType\": \"" + game["universe"]["privacyType"] + "\", \"creatorType\": \"" + game["universe"]["creatorType"] + "\", \"creatorTargetId\": " + game["universe"]["creatorTargetId"] + ", \"creatorName\": \"" + game["universe"]["creatorName"] + "\", \"created\": \"" + game["universe"]["created"] + "\", \"updated\": \"" + game["universe"]["updated"] + "\"}")
                    found1 = true
                    return
                }
            })
            if (found1 == false) res.status(400).send("{\"errors\": [{\"code\": 1, \"message\": \"The universe does not exist.\", \"field\": \"universeId\"}]}");
        }
        else {
            res.status(400).send("{\"errors\": [{\"code\": 1, \"message\": \"The universe does not exist.\", \"field\": \"universeId\"}]}")
        }
    }

})

app.get("/My/Places.aspx", (_, res) => {
    //STUB
    res.status(200).send("<!DOCTYPE html><body><pre>This studio experience is brought to you by [REDACTED]</pre></body>")
})

app.get("/My/Places.aspx&version=:version", (_, res) => {
    //STUB
    res.status(200).send("<!DOCTYPE html><body><pre>This studio experience is brought to you by [REDACTED]</pre></body>")
})

app.get("/IDE/ClientToolbox.aspx", (_, res) => {
    //STUB (if you wanna add actual toolbox, you can)
    res.status(200).send("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"></head><body><select><option value=\"images\">Decals</option><option value=\"default\" selected=\"selected\">Models</option><option value=\"setup\">ROBLOX Sets</option></select><input type=\"text\" id=\"searchtext\"><br><h2>*insert toolbox here*</h2></body></html>")
})

app.get("/Setting/QuietGet/ClientAppSettings/", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send(filesystem.readFileSync("./ClientAppSettings.json", "utf-8").replace(new RegExp("{id}", "g"), userId))
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/v2/settings/application/PCDesktopClient", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send("{\"applicationSettings\":" + filesystem.readFileSync("./ClientAppSettings.json", "utf-8").replace(new RegExp("{id}", "g"), userId) + "}")
    }
    else {
        res.status(200).send("{\"applicationSettings\": {}}")
    }
})

app.get("/v1/settings/application", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send("{\"applicationSettings\":" + filesystem.readFileSync("./ClientAppSettings.json", "utf-8").replace(new RegExp("{id}", "g"), userId) + "}")
    }
    else {
        res.status(200).send("{\"applicationSettings\": {}}")
    }
})

app.get("/v1/settings/universe/:id", (_, res) => {
    res.status(200).send("{\"isUniverseEnabledForVoice\": false, \"isUniverseEnabledForAvatarVideo\": false, \"isChatGroupsApiEnabled\": false}") //STUB
})

app.get("/v2/settings/application/PCStudioApp", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send("{\"applicationSettings\":" + filesystem.readFileSync("./ClientAppSettings.json", "utf-8").replace(new RegExp("{id}", "g"), userId) + "}")
    }
    else {
        res.status(200).send("{\"applicationSettings\": {}}")
    }
})

app.post("/v1/authentication-ticket/redeem", (_, res) => {
    res.status(200).end()
})

app.get("/Setting/QuietGet/StudioAppSettings/", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send(filesystem.readFileSync("./ClientAppSettings.json", "utf-8").replace(new RegExp("{id}", "g"), userId))
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/universes/:id/game-start-info", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"r15Morphing\": false}")
})
app.get("/game/logout.aspx", (_, res) => {
    res.status(200).end()
})
app.get("/Game/JoinRate.ashx", (_, res) => {
    //STUB
    res.status(200).end()
})

app.get("/Game/ClientPresence.ashx", (_, res) => {
    //STUB
    res.status(200).end()
})

app.post("/Game/ClientPresence.ashx", (_, res) => {
    //STUB
    res.status(200).end()
})
app.post("/game/report-stats", (_, res) => {
    //STUB
    res.status(200).end()
})

app.post("/v1/user/studiodata", (req, res) => {
    //IDK how this work, so leaving it stub
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{}")
})

app.get("/asset-gameicon/multiget", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")

    if (typeof (req.query.universeId) == "string") {
        if (filesystem.existsSync("./games.json")) {
            var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

            var found = false
            json.forEach((game) => {
                if (game["id"] == req.query.universeId) {
                    found = true
                    if (filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".png")) {
                        res.status(200).send("[{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}]")
                    }
                    else {
                        res.status(200).send("[{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}]")
                    }
                    return
                }
            })

            if (found == false) {
                res.status(200).send("[{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}]")
            }
        }
        else {
            res.status(200).send("[{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}]")
        }
    }
    else {
        var complete = ""
        var json = JSON.parse(filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]")
        if (req.query.universeId != undefined) {
            for (var i = 0; i < req.query.universeId.length; i++) {
                if (i == req.query.universeId.length - 1) {
                    if (filesystem.existsSync("./games.json")) {
                        var found = false
                        json.forEach((game) => {
                            if (game["id"] == req.query.universeId[i]) {
                                if (filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".png")) {
                                    found = true
                                    complete += "{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}"
                                }
                                return
                            }
                        })

                        if (found == false) {
                            if (filesystem.existsSync("./gametemplates.json")) {
                                var json1 = JSON.parse("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]")
                                var found1 = false
                                json1.forEach((game) => {
                                    if (game["universe"]["id"] == req.query.universeId[i]) {
                                        found1 = true
                                        complete += "{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["universe"]["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}"
                                        return
                                    }
                                })

                                if (found1 == false) {
                                    complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}"
                                }
                            }
                            else {
                                complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}"
                            }
                        }
                    }
                    else {
                        if (filesystem.existsSync("./gametemplates.json")) {
                            var json1 = JSON.parse("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]")
                            var found1 = false
                            json1.forEach((game) => {
                                if (game["universe"]["id"] == req.query.universeId[i]) {
                                    found1 = true
                                    complete += "{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["universe"]["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}"
                                    return
                                }
                            })

                            if (found1 == false) {
                                complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}"
                            }
                        }
                        else {
                            complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}"
                        }
                    }
                }
                else {
                    if (filesystem.existsSync("./games.json")) {
                        var found = false
                        json.forEach((game) => {
                            if (game["id"] == req.query.universeId[i]) {
                                if (filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".png")) {
                                    found = true
                                    complete += "{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}, "
                                }
                                return
                            }
                        })

                        if (found == false) {
                            if (filesystem.existsSync("./gametemplates.json")) {
                                var json1 = JSON.parse("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]")
                                var found1 = false
                                json1.forEach((game) => {
                                    if (game["universe"]["id"] == req.query.universeId[i]) {
                                        found1 = true
                                        complete += "{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["universe"]["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}, "
                                        return
                                    }
                                })

                                if (found1 == false) {
                                    complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}, "
                                }
                            }
                            else {
                                complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}, "
                            }
                        }
                    }
                    else {
                        if (filesystem.existsSync("./gametemplates.json")) {
                            var json1 = JSON.parse("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]")
                            var found1 = false
                            json1.forEach((game) => {
                                if (game["universe"]["id"] == req.query.universeId[i]) {
                                    found1 = true
                                    complete += "{\"Url\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["universe"]["rootPlaceId"] + "&wd=700&ht=700&fmt=png\"}, "
                                    return
                                }
                            })

                            if (found1 == false) {
                                complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}, "
                            }
                        }
                        else {
                            complete += "{\"Url\": \"http://reblox.zip/v1/asset/?id=133293265\"}, "
                        }
                    }
                }
            }
        }
        res.status(200).send("[" + complete + "]")
    }
})

app.post("/Analytics/LogFile.ashx", (_, res) => {
    res.status(200).end()
})
app.get("/v1/games/icons", (req, res) => {
    res.setHeader("cache-control", "no-cache")

    if (isRobloxAvailable) {
        var idsplitted = req.query.universeIds.split(",")
        var edit = ""
        var idneeded = []
        var placeidneeded = []
        var gamesjson = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
        var templatesjson = filesystem.existsSync("./gametemplates.json") ? ("[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]") : "[]"
        var templatesjsonparsed = JSON.parse(templatesjson)

        idsplitted.forEach((id) => {
            filesystem.readdirSync(assetfolder).forEach(file => {
                var splitted = file.split('.')

                if (templatesjsonparsed.length > 0) {
                    templatesjsonparsed.forEach((template) => {
                        if (template["universe"]["id"] == id) {
                            if (splitted[0] == id.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                                idneeded.push(id)
                                placeidneeded.push(template["universe"]["rootPlaceId"])
                            }
                        }
                    })
                }
            })
            filesystem.readdirSync("./icons").forEach(file => {
                var splitted = file.split('.')

                if (templatesjsonparsed.length > 0) {
                    templatesjsonparsed.forEach((template) => {
                        if (template["universe"]["id"] == id) {
                            if (splitted[0] == id.toString().trim() && (file.endsWith(".png") || file.endsWith(".jpg") || file.endsWith(".jpeg") || file.endsWith(".bmp"))) {
                                idneeded.push(id)
                                placeidneeded.push(template["universe"]["rootPlaceId"])
                            }
                        }
                    })
                }

            })
        })
        var options = {
            host: "thumbnails.roblox.com",
            port: 443,
            path: (req.get("host") + req.originalUrl).replace(new RegExp("thumbnails.reblox.zip", "g"), "").replace("AutoGenerated", "PlaceHolder"),
            method: "GET"
        }

        https.get(options, (res1) => {
            var jsondata = ""
            res1.setEncoding("utf8")
            res1.on("data", (chunk) => {
                jsondata += chunk
            })
            res1.on("end", () => {

                var jsonparsed = JSON.parse(jsondata)
                var gamesjsonparsed = JSON.parse(gamesjson)
                if (jsonparsed["data"].length > 0) {
                    jsonparsed["data"].forEach((imageData) => {
                        for (var i = 0; i < idneeded.length; i++) {
                            if (imageData["targetId"] == idneeded[i]) {
                                imageData["imageUrl"] = "http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + placeidneeded[i]
                            }
                        }
                        if (gamesjsonparsed.length > 0) {
                            gamesjsonparsed.forEach((game) => {
                                if (imageData["targetId"] == game["id"]) {
                                    if (filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".png") || filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".jpg") || filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".jpeg")) imageData["imageUrl"] = "http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["rootPlaceId"]
                                }
                            })
                        }
                    })

                    var recompiledJSON = JSON.stringify(jsonparsed)
                    res.status(200).send(recompiledJSON)

                }
                else {
                    res.status(200).send(jsondata)
                }
            })
        })
    }
    else {
        var idsplitted = req.query.universeIds.split(',')
        var edit = ""

        if (filesystem.existsSync("./gametemplates.json")) {
            var json = "{\"data\":[" + filesystem.readFileSync("./gametemplates.json", "utf8") + "]}"

            var jsonparsed = JSON.parse(json)

            idsplitted.forEach((id) => {
                var inserted = false
                if (jsonparsed["data"].length > 0) {
                    jsonparsed["data"].forEach((template) => {
                        if (template["universe"]["id"] == id) {
                            inserted = true
                            if (idsplitted[idsplitted.length - 1] == id) {
                                edit += "{\"targetId\":" + id + ",\"State\":\"Completed\",\"imageUrl\":\"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + template["universe"]["rootPlaceId"] + "&wd=700&ht=700&fmt=png\", \"version\":\"TN3\"}"
                            }
                            else {
                                edit += "{\"targetId\":" + id + ",\"State\":\"Completed\",\"imageUrl\":\"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + template["universe"]["rootPlaceId"] + "&wd=700&ht=700&fmt=png\", \"version\":\"TN3\"}, "
                            }
                        }
                    })
                }
                if (inserted == false) {
                    if (idsplitted[idsplitted.length - 1] == id) {
                        edit += "{\"targetId\":" + id + ",\"State\":\"Completed\",\"imageUrl\":\"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + id + "&wd=700&ht=700&fmt=png\", \"version\":\"TN3\"}"
                    }
                    else {
                        edit += "{\"targetId\":" + id + ",\"State\":\"Completed\",\"imageUrl\":\"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + id + "&wd=700&ht=700&fmt=png\", \"version\":\"TN3\"}, "
                    }
                }
            })

        }
        else {
            if (idsplitted[idsplitted.length - 1] == id) {
                edit += "{\"targetId\":" + id + ",\"State\":\"Completed\",\"imageUrl\":\"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + id + "&wd=700&ht=700&fmt=png\", \"version\":\"TN3\"}"
            }
            else {
                edit += "{\"targetId\":" + id + ",\"State\":\"Completed\",\"imageUrl\":\"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + id + "&wd=700&ht=700&fmt=png\", \"version\":\"TN3\"}, "
            }
        }

        res.status(200).send("{\"data\":[" + edit + "]}")
    }
})

app.post("/v1/games/:id/icon", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)
    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["id"] == req.params.id) {
                found = true
                var rawtext = Buffer.from(req.body).toString("hex")
                var splittedraw = rawtext.split(Buffer.from('name="request.files"\r\n\r\n', "utf8").toString("hex"))

                if (filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".png")) filesystem.unlinkSync("./icons/" + game["rootPlaceId"] + ".png");
                filesystem.writeFileSync("./icons/" + game["rootPlaceId"] + ".png", Buffer.from(splittedraw[1], "hex"))
                res.status(200).send("{\"targetId\": " + game["rootPlaceId"] + "}")
                return
            }
        })
        if (found == false) res.status(200).send("{\"targetId\": 1818}")
    }
    else {
        res.status(200).send("{\"targetId\": 1818}")
    }
})

app.get("/v1/games/:id/media", (req, res) => {
    res.status(200).send("{\"data\":[]}") //STUB
})

app.get("/v1/games", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var result = ""
    var valid = true
    const games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    const gamesjson = JSON.parse(games)

    if (typeof (req.query.universeIds) == "string") {
        const splitorstring = (req.query.universeIds.includes(",") ? req.query.universeIds.split(',') : req.query.universeIds)
        if (typeof (splitorstring) == "string") {
            if (parseInt(splitorstring) > 0) {
                if (isNumeric(splitorstring) == false) {
                    valid = false
                }

                if (gamesjson.length > 0 && valid == true) {
                    gamesjson.forEach((game) => {
                        if (game["id"] == splitorstring) {
                            result += "{\"id\": " + splitorstring + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": null, \"sourceDescription\": null, \"creator\": {\"id\": " + game["creatorTargetId"] + ", \"name\": \"" + game["creatorName"] + "\", \"Type\": \"User\", \"isRNVAccount\": false, \"hasVerifiedBadge\": false}, \"price\": null, \"allowedGearGenres\": [], \"allowedGearCategories\": [], \"isGenreEnforced\": false, \"copyingAllowed\": false, \"playing\": 0, \"visits\": 0, \"maxPlayers\": 29, \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\", \"studioAccessToApisAllowed\": " + game["studioAccessToApisAllowed"] + ", \"createVipServerAllowed\": true, \"universeAvatarType\": \"PlayerChoice\", \"genre\": \"\", \"isAllGenre\": true, \"isFavoritedByUser\": false, \"canonicalUrlPath\": \"/games/" + game["rootPlaceId"] + "/" + game["name"].replace(new RegExp(" ", "g"), "-") + "\"},"
                            return
                        }
                    })
                }
            }
            else {
                valid = false
            }
        }
        else {
            splitorstring.forEach((id) => {
                if (isNumeric(id) == false) {
                    valid = false
                    return
                }

                if (gamesjson.length > 0 && valid == true) {
                    gamesjson.forEach((game) => {
                        if (game["id"] == id) {
                            result += "{\"id\": " + id + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": null, \"sourceDescription\": null, \"creator\": {\"id\": " + game["creatorTargetId"] + ", \"name\": \"" + game["creatorName"] + "\", \"Type\": \"User\", \"isRNVAccount\": false, \"hasVerifiedBadge\": false}, \"price\": null, \"allowedGearGenres\": [], \"allowedGearCategories\": [], \"isGenreEnforced\": false, \"copyingAllowed\": false, \"playing\": 0, \"visits\": 0, \"maxPlayers\": 29, \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\", \"studioAccessToApisAllowed\": " + game["studioAccessToApisAllowed"] + ", \"createVipServerAllowed\": true, \"universeAvatarType\": \"PlayerChoice\", \"genre\": \"\", \"isAllGenre\": true, \"isFavoritedByUser\": false, \"canonicalUrlPath\": \"/games/" + game["rootPlaceId"] + "/" + game["name"].replace(new RegExp(" ", "g"), "-") + "\"},"
                            return
                        }
                    })
                }
            })
        }


        if (valid == false) {
            res.status(400).send("{\"errors\": [{\"code\": 8, \"message\": \"The universe IDs specified are invalid.\"}]}")
            return
        }

        if (result.length > 0) {
            res.status(200).send("{\"data\": [" + result.slice(0, result.length - 1) + "]}")
        }
        else {
            res.status(200).send("{\"data\": []}")
        }
    }
    else {
        if (req.query.universeIds != undefined) {
            req.query.universeIds.forEach((id) => {
                if (isNumeric(id) == false) {
                    valid = false
                    return
                }

                if (gamesjson.length > 0 && valid == true) {
                    gamesjson.forEach((game) => {
                        if (game["id"] == id) {
                            result += "{\"id\": " + id + ", \"rootPlaceId\": " + game["rootPlaceId"] + ", \"name\": \"" + game["name"] + "\", \"description\": \"" + game["description"].replace(new RegExp("\r\n", "g"), "\\r\\n").replace(new RegExp("\n", "g"), "\\n").replace(new RegExp("\"", "g"), "\\\"") + "\", \"sourceName\": null, \"sourceDescription\": null, \"creator\": {\"id\": " + game["creatorTargetId"] + ", \"name\": \"" + game["creatorName"] + "\", \"Type\": \"User\", \"isRNVAccount\": false, \"hasVerifiedBadge\": false}, \"price\": null, \"allowedGearGenres\": [], \"allowedGearCategories\": [], \"isGenreEnforced\": false, \"copyingAllowed\": false, \"playing\": 0, \"visits\": 0, \"maxPlayers\": 29, \"created\": \"" + game["created"] + "\", \"updated\": \"" + game["updated"] + "\", \"studioAccessToApisAllowed\": " + game["studioAccessToApisAllowed"] + ", \"createVipServerAllowed\": true, \"universeAvatarType\": \"PlayerChoice\", \"genre\": \"\", \"isAllGenre\": true, \"isFavoritedByUser\": false, \"canonicalUrlPath\": \"/games/" + game["rootPlaceId"] + "/" + game["name"].replace(new RegExp(" ", "g"), "-") + "\"},"
                            return
                        }
                    })
                }
            })
            if (valid == false) {
                res.status(400).send("{\"errors\": [{\"code\": 8, \"message\": \"The universe IDs specified are invalid.\"}]}")
                return
            }

            if (result.length > 0) {
                res.status(200).send("{\"data\": [" + result.slice(0, result.length - 1) + "]}")
            }
            else {
                res.status(200).send("{\"data\": []}")
            }
        }
        else {
            res.status(400).send("{\"errors\": [{\"code\": 8, \"message\": \"The universe IDs specified are invalid.\"}]}")
        }
    }

})

app.get("/Game/AreFriends", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "text/plain")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Game/AreFriends?userId=" + req.query.userId + "&otherUserIds=" + req.query.otherUserIds,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var friendsarray = req.query.otherUserIds.includes(",") ? req.query.otherUserIds.split(",") : req.query.otherUserIds
        var friendsExistsList = []
        if (enableFriendships == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else {
                        if (line.startsWith("<Friendship")) {
                            //I know this is not optimized, but whatever, too lazy to rewrite the entire thing.
                            if (typeof (friendsarray) == "string" || typeof (friendsarray) == "number") {
                                if (line == "<Friendship Receiver=" + req.query.userId + " Sender=" + friendsarray + ">" || line == "<Friendship Receiver=" + friendsarray + " Sender=" + req.query.userId + ">") {
                                    if (friendsExistsList.includes(friendsarray) == false) {
                                        friendsExistsList.push(friendsarray)
                                    }
                                }
                            }
                            else {
                                for (var i = 0; i < friendsarray.length; i++) {
                                    if (line == "<Friendship Receiver=" + req.query.userId + " Sender=" + friendsarray[i] + ">" || line == "<Friendship Receiver=" + friendsarray[i] + " Sender=" + req.query.userId + ">\r\n") {
                                        if (friendsExistsList.includes(friendsarray[i]) == false) {
                                            friendsExistsList.push(friendsarray[i])
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    res.status(200).send((friendsExistsList.length > 0) ? "," + friendsExistsList.toString() + "," : "")
                }
                else {
                    res.status(500).end()
                }
            }
            else {
                res.status(200).send("")
            }
        }
        else {
            res.status(200).send("")
        }
    }
})

app.get("/Friend/AreFriends", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Friend/AreFriends?userId=" + req.query.userId + "&otherUserIds=" + req.query.otherUserIds,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var friendsarray = (req.query.otherUserIds.includes(",")) ? req.query.otherUserIds.split(",") : req.query.otherUserIds
        var friendsExistsList = []
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else {
                        //I know this is not optimized, but whatever, too lazy to rewrite the entire thing.
                        if (line.startsWith("<Friendship")) {
                            if (typeof (friendsarray) == "string" || typeof (friendsarray) == "number") {
                                if (line == "<Friendship Receiver=" + req.query.userId + " Sender=" + friendsarray + ">" || line == "<Friendship Receiver=" + friendsarray + " Sender=" + req.query.userId + ">") {
                                    if (friendsExistsList.includes(friendsarray) == false) {
                                        friendsExistsList.push(friendsarray)
                                    }
                                }
                            }
                            else {
                                for (var i = 0; i < friendsarray.length; i++) {
                                    if (line == "<Friendship Receiver=" + req.query.userId + " Sender=" + friendsarray[i] + ">" || line == "<Friendship Receiver=" + friendsarray[i] + " Sender=" + req.query.userId + ">") {
                                        if (friendsExistsList.includes(friendsarray[i]) == false) {
                                            friendsExistsList.push(friendsarray[i])
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    res.status(200).send((friendsExistsList.length > 0) ? "," + friendsExistsList.toString() + "," : "")
                }
                else {
                    res.status(500).end()
                }
            }
            else {
                res.status(200).send("")
            }
        }
        else {
            res.status(200).send("")
        }
    }
})

app.post("/Game/CreateFriend", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Creating a friendship for " + req.query.firstUserId + " and " + req.query.secondUserId + "...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Game/CreateFriend?firstUserId=" + req.query.firstUserId + "&secondUserId=" + req.query.secondUserId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableFriendships == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line == "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">" || line == "<Friendship Receiver=" + req.query.secondUserId + " Sender=" + req.query.firstUserId + ">") {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        //do nothing
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">\r\n")
                    }

                }
                else {
                    res.status(500).end()
                    return
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">\r\n")
            }
            res.status(200).end()
        }
        else {
            res.status(200).end()
        }
    }
})

app.post("/Game/BreakFriend", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Breaking a friendship between " + req.query.firstUserId + " and " + req.query.secondUserId + "...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Game/BreakFriend?firstUserId=" + req.query.firstUserId + "&secondUserId=" + req.query.secondUserId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableFriendships == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })
                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line == "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">" || line == "<Friendship Receiver=" + req.query.secondUserId + " Sender=" + req.query.firstUserId + ">") {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(new RegExp(replacementtext + "\r\n", "g"), ""))
                    }
                    else {
                        res.status(200).end()
                    }

                }
                else {
                    res.status(500).end()
                }
            }
            res.status(200).end()
        }
        else {
            res.status(200).end()
        }
    }
})

app.post("/Friend/BreakFriend", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Breaking a friendship between " + req.query.firstUserId + " and " + req.query.secondUserId + "...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Friend/BreakFriend?firstUserId=" + req.query.firstUserId + "&secondUserId=" + req.query.secondUserId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableFriendships == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })
                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line == "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">" || line == "<Friendship Receiver=" + req.query.secondUserId + " Sender=" + req.query.firstUserId + ">") {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(new RegExp(replacementtext + "\r\n", "g"), ""))
                    }
                    else {
                        res.status(200).end()
                    }

                }
                else {
                    res.status(500).end()
                }
            }
            res.status(200).end()
        }
        else {
            res.status(200).end()
        }
    }
})

app.post("/Friend/CreateFriend", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Creating a friendship for " + req.query.firstUserid + " and " + req.query.secondUserId + "...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/Friend/CreateFriend?firstUserId=" + req.query.firstUserId + "&secondUserId=" + req.query.secondUserId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableFriendships == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line == "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">" || line == "<Friendship Receiver=" + req.query.secondUserId + " Sender=" + req.query.firstUserId + ">") {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        //do nothing
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">\r\n")
                    }

                }
                else {
                    res.status(500).end()
                    return
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<Friendship Receiver=" + req.query.firstUserId + " Sender=" + req.query.secondUserId + ">\r\n")
            }
            res.status(200).end()
        }
        else {
            res.status(200).end()
        }
    }
})

app.post("/persistence/getsortedvalues", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\":{\"Entries\":[],\"ExclusiveStartKey\":null}}") //STUB
})

function changeROBLOSECURITYOnLauncher(roblosecurityedit) {
    if (allowTCPLauncher) {
        const { Socket } = require("net")

        const client = new Socket()

        var options = {
            host: "127.0.0.1",
            port: 50355
        }
        client.connect(options, () => {
            if (verbose) { console.log("\x1b[34m%s\x1b[0m", "<INFO> Connected to the launcher via TCP!") }

            client.on("data", message => {
                if (message.toString("utf8") == "200") {
                    if (verbose) { console.log("\x1b[34m%s\x1b[0m", "<INFO> Successfully updated ROBLOSECURITY on the launcher! Disconnecting...") }
                    client.end()
                }
                else {
                    if (verbose) { console.log("\x1b[33m%s\x1b[0m", "<WARN> Something went wrong while trying to update the ROBLOSECURITY, the new ROBLOSECURITY will only be used this session! Disconnecting...") }
                    client.end()
                }
            })
        })

        client.write(Buffer.concat([Buffer.from((276312498).toString(16), "hex"), Buffer.from("URS", "utf8"), Buffer.from(roblosecurityedit, "utf8")]))
    }
}

app.post("/v1/persistence/:type/multi-get", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/persistence/" + req.params.type + "/multi-get",
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        var result = ""
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                req.body.keys.forEach(async (dataBody) => {
                    const stream = filesystem.createReadStream(RBDFpath)

                    const rl = readline.createInterface({
                        input: stream,
                        crlfDelay: Infinity
                    })

                    for await (const line of rl) {
                        if (line == "RBDF==") {
                            verified = true
                        }
                        else if (line.includes("DataStoreName=\"" + dataBody.key + "\"") && line.includes("Key=\"" + dataBody.target + "\"") && line.includes("Scope=" + dataBody.scope)) {
                            replacementtext = line
                            break
                        }
                    }
                    stream.destroy()
                    rl.close()
                    if (verified == true) {
                        if (replacementtext != "") {
                            var myRegExp = new RegExp('Value\=\"(.*)\"')
                            var match = myRegExp.exec(replacementtext)
                            if (match.length > 0) {
                                if (dataBody == req.body.keys[req.body.keys.length - 1]) {
                                    result = "{\"entries\": [" + result + " {\"key\": \"" + dataBody.key + "\", \"scope\": \"" + dataBody.scope + "\", \"target\": \"" + dataBody.target + "\", \"usn\": \"0.0.0.01\", \"value\": \"" + (match[1] != undefined ? match[1] : "").replace(new RegExp("\\\"", "g"), "\\\"") + "\"}]}"
                                    res.status(200).send(result)
                                } else {
                                    result += "{\"key\": \"" + dataBody.key + "\", \"scope\": \"" + dataBody.scope + "\", \"target\": \"" + dataBody.target + "\", \"usn\": \"0.0.0.01\", \"value\": \"" + (match[1] != undefined ? match[1] : "") + "\"},"

                                }
                            }
                        }
                        else {
                            if (dataBody == req.body.keys[req.body.keys.length - 1]) {
                                result = "{\"entries\": []}"
                                res.status(200).send(result)
                            }
                        }
                    }
                    else {
                        res.status(500).end()
                        return
                    }
                })

            }
            else {
                res.status(200).send("{\"entries\": []}")
            }
        }
        else {
            res.status(200).send("{\"entries\": []}")
        }
    }

})

app.get("/v1/persistence/:type/list", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"entries\":[], \"lastEvaluatedKey\":null}") //STUB
})

app.get("/v1/persistence/:type", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/persistence/" + req.params.type + "?scope=" + req.query.scope + "&key=" + req.query.key + "&target=" + req.query.target,
                method: "GET"
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.includes("DataStoreName=\"" + req.query.key + "\"") && line.includes("Key=\"" + req.query.target + "\"") && line.includes("Scope=" + req.query.scope)) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        var myRegExp = new RegExp('Value\=\"(.*)\"')
                        var match = myRegExp.exec(replacementtext)
                        if (match.length > 0) {
                            res.status(200).send(match[1] != undefined ? match[1] : "")
                        }
                        else {
                            res.status(200).end()
                        }
                    }
                    else {
                        res.status(200).end()

                    }
                }
                else {
                    res.status(500).end()
                }
            }
            else {
                res.status(200).end()
            }
        }
        else {
            res.status(200).end()
        }
    }
})

app.post("/v1/persistence/:type/remove", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Removing a value from the datastore...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/persistence/" + req.params.type + "/remove?scope=" + req.query.scope + "&target=" + req.query.target + "&key=" + req.query.key,
                method: "POST",
                headers: {
                    "Content-Type": "application/octet-stream"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(req.body)
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.includes("DataStoreName=\"" + req.query.key + "\"") && line.includes("Key=\"" + req.query.target + "\"") && line.includes("Scope=" + req.query.scope)) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        var myRegExp = new RegExp('Value\=\"(.*)\"')
                        var match = myRegExp.exec(replacementtext)
                        if (match.length > 0) {
                            filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(replacementtext, ""))
                            res.status(200).send(match[1] != undefined ? match[1] : "")
                            return
                        }
                    }
                }
                else {
                    res.status(500).end()
                    return
                }
            }
            res.status(200).end() //STUB
        }
        else {
            res.status(200).end() //STUB
        }
    }
})

app.post("/v1/persistence/:type", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Adding a value to the datastore...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/v1/persistence/" + req.params.type + "?scope=" + req.query.scope + "&target=" + req.query.target + "&key=" + req.query.key,
                method: "POST",
                headers: {
                    "Content-Type": "application/octet-stream"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(req.body)
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.includes("DataStoreName=\"" + req.query.key + "\"") && line.includes("Key=\"" + req.query.target + "\"") && line.includes("Scope=" + req.query.scope)) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(replacementtext, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.params.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body + "\">"))
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.params.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body + "\">\r\n")
                    }

                }
                else {
                    res.status(500).end()
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.params.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body + "\">\r\n")
            }
            res.status(200).send("{\"usn\":\"0.0.0.01\"}") //STUB
        }
        else {
            res.status(200).send("{\"usn\":\"0.0.0.01\"}") //STUB
        }
    }
})

app.post("/user/decline-friend-request", (req, res) => {
    res.status(200).send("{\"success\": true, \"message\": \"Success\"}")
})

app.post("/persistence/set", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Adding a value to the datastore...")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/persistence/set?scope=" + req.query.scope + "&target=" + req.query.target + "&type=" + req.query.type + "&key=" + req.query.key,
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-decoded"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.setHeader("content-type", "application/json; charset=utf-8")
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(req.body)
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        res.setHeader("content-type", "application/json; charset=utf-8")
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.includes("DataStoreName=\"" + req.query.key + "\"") && line.includes("Key=\"" + req.query.target + "\"") && line.includes("Scope=" + req.query.scope)) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(replacementtext, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.query.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body.value + "\">"))
                    }
                    else {
                        filesystem.appendFileSync(RBDFpath, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.query.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body.value + "\">\r\n")
                    }

                }
                else {
                    res.status(500).end()
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                filesystem.appendFileSync(RBDFpath, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.query.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body.value + "\">\r\n")
            }
            res.status(200).send("{\"data\":{\"value\":\"" + req.body.value.slice(1).slice(0, -1).replace(new RegExp("\\\\", "g"), "\\\\").replace(new RegExp("\"", "g"), "\\\"") + "\"}}")
        }
        else {
            res.status(200).send("{\"data\":[]}")
        }
    }
})

app.post("/persistence/setblob.ashx", trueRaw, async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    var data = undefined
    var verified = false
    var replacementtext = ""
    if (req.headers["content-encoding"] == "gzip") {
        data = zlib.unzipSync(await req.body)
    }
    else if (req.headers["content-encoding"] == "deflate") {
        data = zlib.inflateSync(await req.body)
    }
    else {
        data = await req.body
    }

    if (enableDataPersistence == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
        console.log("\x1b[32m%s\x1b[0m", "<INFO> Adding value to datastore (Data Persistence)")
        var hash = ""
        var ready = false
        if (filesystem.existsSync(assetfolder + "/" + req.query.placeid + ".rbxl")) {
            getMD5FileHash(assetfolder + "/" + req.query.placeid + ".rbxl").then((result) => {
                hash = result
                ready = true
            })
        }
        else if (filesystem.existsSync(assetfolder + "/" + req.query.placeid + ".rbxlx")) {
            getMD5FileHash(assetfolder + "/" + req.query.placeid + ".rbxlx").then((result) => {
                hash = result
                ready = true
            })
        }
        else if (filesystem.existsSync("./uploads/" + req.query.placeid)) {
            getMD5FileHash("./uploads/" + req.query.placeid).then((result) => {
                hash = result
                ready = true
            })
        }
        else {
            ready = true
        }
        while (ready == false) {
            //do nothing
            await delay(50)
        }
        if (filesystem.existsSync(RBDFpath)) {
            const stream = filesystem.createReadStream(RBDFpath)

            const rl = readline.createInterface({
                input: stream,
                crlfDelay: Infinity
            })

            for await (const line of rl) {
                if (line == "RBDF==") {
                    verified = true
                }
                else if (line.startsWith("<DataPersistence userId=" + req.query.userid + " placeId=" + req.query.placeid + hash + " table=\"")) {
                    replacementtext = line
                    break
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (replacementtext != "") {
                    filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(replacementtext, "<DataPersistence userId=" + req.query.userid + " placeId=" + req.query.placeid + hash + " table=\"" + zlib.gzipSync(Buffer.from(data, "utf8")).toString("base64") + "\">"))
                }
                else {
                    filesystem.appendFileSync(RBDFpath, "<DataPersistence userId=" + req.query.userid + " placeId=" + req.query.placeid + hash + " table=\"" + zlib.gzipSync(Buffer.from(data, "utf8")).toString("base64") + "\">\r\n")
                }

            }
            else {
                res.status(500).end()
            }
        }
        else {
            filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
            filesystem.appendFileSync(RBDFpath, "<DataPersistence userId=" + req.query.userid + " placeId=" + req.query.placeid + hash + " table=\"" + zlib.gzipSync(Buffer.from(data, "utf8")).toString("base64") + "\">\r\n")
        }
        res.status(200).end()
    }
    else {
        res.status(200).end()
    }
})

app.get("/persistence/getbloburl.ashx", async (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Getting value from datastore (Data Persistence)")
    res.setHeader("cache-control", "no-cache")
    var verified = false
    var replacementtext = ""
    var hash = ""
    var ready = false
    if (filesystem.existsSync(assetfolder + "/" + req.query.placeid + ".rbxl")) {
        getMD5FileHash(assetfolder + "/" + req.query.placeid + ".rbxl").then((result) => {
            hash = result
            ready = true
        })
    }
    else if (filesystem.existsSync(assetfolder + "/" + req.query.placeid + ".rbxlx")) {
        getMD5FileHash(assetfolder + "/" + req.query.placeid + ".rbxlx").then((result) => {
            hash = result
            ready = true
        })
    }
    else if (filesystem.existsSync("./uploads/" + req.query.placeid)) {
        getMD5FileHash("./uploads/" + req.query.placeid).then((result) => {
            hash = result
            ready = true
        })
    }
    else {
        ready = true
    }

    while (ready == false) {
        await delay(50)
    }
    if (enableDataPersistence == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
        if (filesystem.existsSync(RBDFpath)) {
            const stream = filesystem.createReadStream(RBDFpath)

            const rl = readline.createInterface({
                input: stream,
                crlfDelay: Infinity
            })

            for await (const line of rl) {
                if (line == "RBDF==") {
                    verified = true
                }
                else if (line.startsWith("<DataPersistence userId=" + req.query.userid + " placeId=" + req.query.placeid + hash + " table=\"")) {
                    replacementtext = line
                    break
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (replacementtext != "") {
                    var myRegExp = new RegExp('table="(.*)">$')
                    var match = myRegExp.exec(replacementtext)
                    if (match.length > 0) {
                        var value = match[1] != undefined ? zlib.unzipSync(Buffer.from(match[1], "base64")).toString("utf8") : "<Table></Table>"
                        res.status(200).send(value)
                    }
                    else {
                        res.status(200).send("<Table></Table>")
                    }
                }
                else {
                    res.status(200).send("<Table></Table>")

                }
            }
            else {
                res.status(500).end()
            }
        }
        else {
            res.status(200).send("<Table></Table>")
        }
    }
    else {
        res.status(200).send("<Table></Table>")
    }
})

app.post("/persistence/getV2", async (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/persistence/getV2?scope=" + req.query.scope,
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-decoded"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(req.body)
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var replacementtext = ""
        if (enableDataStore == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.includes("DataStoreName=\"" + req.body.qkeys[2] + "\"") && line.includes("Key=\"" + req.body.qkeys[1] + "\"") && line.includes("Scope=" + req.query.scope)) {
                        replacementtext = line
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (replacementtext != "") {
                        var myRegExp = new RegExp('Value\=\"(.*)\"')
                        var match = myRegExp.exec(replacementtext)
                        if (match.length > 0) {
                            var value = match[1] != undefined ? match[1] : ""
                            res.status(200).send("{\"data\":[{\"Key\": {\"Scope\":\"" + req.query.scope + "\", \"Target\": \"" + req.body.qkeys[1] + "\", \"Key\": \"" + req.body.qkeys[2] + "\"},\"Value\": \"" + value.replace(new RegExp("\\\\", "g"), "\\\\").replace(new RegExp("\"", "g"), "\\\"") + "\"}]}")
                        }
                        else {
                            res.status(200).send("{\"data\":[]}")
                        }
                    }
                    else {
                        res.status(200).send("{\"data\":[]}")

                    }
                }
                else {
                    res.status(500).end()
                }
            }
            else {
                res.status(200).send("{\"data\":[]}")
            }
        }
        else {
            res.status(200).send("{\"data\":[]}")
        }
    }
})

app.get("/user/following-exists", async (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/user/following-exists?userId=" + req.query.userId + "&followerUserId=" + req.query.followerUserId,
                method: "GET"
            }

            http.get(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var isFollowing = false
        if (enableFollow == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.trim() == "<Following userId=" + req.query.followerUserId + " followerUserId=" + req.query.userId + ">") {
                        isFollowing = true
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (isFollowing == true) {
                        res.status(200).send("{\"success\":true,\"isFollowing\":true}")
                    }
                    else {
                        res.status(200).send("{\"success\":true,\"isFollowing\":false}")
                    }
                }
                else {
                    res.status(500).end()
                }
            }
            else {
                res.status(200).send("{\"success\":true,\"isFollowing\":false}")
            }
        }
        else {
            res.status(200).send("{\"success\":true,\"isFollowing\":false}")
        }
    }
})

app.post("/user/follow", async (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/user/follow?userId=" + userId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var isFollowing = false
        if (enableFollow == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.trim() == "<Following userId=" + ((req.query.userId != undefined) ? req.query.userId : userId) + " followerUserId=" + req.body.followedUserId + ">") {
                        isFollowing = true
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (isFollowing == false) {
                        if (req.query.userId != undefined) {
                            filesystem.appendFileSync(RBDFpath, "<Following userId=" + req.query.userId + " followerUserId=" + req.body.followedUserId + ">\r\n")
                        }
                        else {
                            filesystem.appendFileSync(RBDFpath, "<Following userId=" + userId + " followerUserId=" + req.body.followedUserId + ">\r\n")
                        }
                    }
                }
                else {
                    res.status(500).end()
                    return
                }
            }
            else {
                filesystem.writeFileSync(RBDFpath, "RBDF==\r\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\r\n\r\n")
                if (req.query.userId != undefined) {
                    filesystem.appendFileSync(RBDFpath, "<Following userId=" + req.query.userId + " followerUserId=" + req.body.followedUserId + ">\r\n")
                }
                else {
                    filesystem.appendFileSync(RBDFpath, "<Following userId=" + userId + " followerUserId=" + req.body.followedUserId + ">\r\n")
                }
            }
            res.status(200).send("{\"success\":true, \"message\": \"Success\"}")
        }
        else {
            res.status(200).send("{\"success\":true, \"message\": \"Success\"}")
        }
    }
})

app.post("/user/unfollow", async (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    if (joining) {
        try {
            var options = {
                host: ip,
                port: 80,
                path: "/user/unfollow?userId=" + userId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                }
            }

            var req1 = http.request(options, (res1) => {
                res1.setEncoding("utf-8")
                var data = ""
                res1.on("data", (chunk) => {
                    data += chunk
                })
                res1.on("end", () => {
                    res.status(res1.statusCode).send(data)
                })
            })
            req1.write(JSON.stringify(req.body))
            req1.end()

        } catch {
            res.status(500).end()
        }
    }
    else {
        var verified = false
        var isFollowing = false
        if (enableFollow == true && RBDFpath != "" && RBDFpath.endsWith(".rbdf")) {
            if (filesystem.existsSync(RBDFpath)) {
                const stream = filesystem.createReadStream(RBDFpath)

                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                })

                for await (const line of rl) {
                    if (line == "RBDF==") {
                        verified = true
                    }
                    else if (line.trim() == "<Following userId=" + ((req.query.userId != undefined) ? req.query.userId : userId) + " followerUserId=" + req.body.followedUserId + ">") {
                        isFollowing = true
                        break
                    }
                }
                stream.destroy()
                rl.close()
                if (verified == true) {
                    if (isFollowing == true) {
                        var filecontent = filesystem.readFileSync(RBDFpath, "utf8")
                        filesystem.unlinkSync(RBDFpath)
                        if (req.query.userId != undefined) {
                            filesystem.writeFileSync(RBDFpath, filecontent.replace("<Following userId=" + req.query.userId + " followerUserId=" + req.body.followedUserId + ">\r\n", ""))
                        }
                        else {
                            filesystem.writeFileSync(RBDFpath, filecontent.replace("<Following userId=" + userId + " followerUserId=" + req.body.followedUserId + ">\r\n", ""))
                        }
                    }
                }
                else {
                    res.status(500).end()
                    return
                }
            }
            else {
                res.status(200).send("{\"success\":false, \"message\": \"Server Error\"}")
                return
            }
            res.status(200).send("{\"success\":true, \"message\": \"Success\"}")
        }
        else {
            res.status(200).send("{\"success\":true, \"message\": \"Success\"}")
        }
    }
})

app.get("/assets/:id/versions", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var games = filesystem.existsSync("./games.json") ? filesystem.readFileSync("./games.json", "utf8") : "[]"
    var gamesjson = JSON.parse(games)

    if (gamesjson.length > 0) {
        var found = false
        gamesjson.forEach((game) => {
            if (game["rootPlaceId"] == req.params.id) {
                found = true
                res.status(200).send("[{\"Id\": " + game["id"] + ", \"AssetId\":" + req.params.id + ", \"VersionNumber\":1, \"RawContentId\":" + req.params.id + ", \"ParentAssetVersionId\": " + req.params.id + ", \"CreatorType\":1, \"CreatorTargetId\": " + game["creatorTargetId"] + ", \"CreatingUniverseId\": null, \"Created\": \"" + game["created"] + "\",\"Updated\": \"" + game["updated"] + "\"}]")
                return
            }
        })
        if (found == false) res.status(200).send("[{\"Id\": " + req.params.id + ", \"AssetId\":0, \"VersionNumber\":1, \"RawContentId\":0, \"ParentAssetVersionId\": 0, \"CreatorType\":1, \"CreatorTargetId\": " + userId + ", \"CreatingUniverseId\": null, \"Created\": \"2015-07-13T11:51:12.9073098-05:00\",\"Updated\": \"2015-07-13T11:51:12.9073098-05:00\"}]")
    }
    else {
        res.status(200).send("[{\"Id\": " + req.params.id + ", \"AssetId\":0, \"VersionNumber\":1, \"RawContentId\":0, \"ParentAssetVersionId\": 0, \"CreatorType\":1, \"CreatorTargetId\": " + userId + ", \"CreatingUniverseId\": null, \"Created\": \"2015-07-13T11:51:12.9073098-05:00\",\"Updated\": \"2015-07-13T11:51:12.9073098-05:00\"}]")
    }

})
app.get("/user/get-friendship-count", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"success\": true, \"message\": \"Success\", \"count\": 0}")
})
app.post("/user/request-friendship", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"success\":true, \"message\":\"Success\"}")
})
app.post("/v1/batch", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.setHeader("cache-control", "no-cache")
    var unfinishedstring = "{ \"data\": ["
    for (var i = 0; i != Object.keys(req.body).length; i++) {
        if (req.body[i]["type"] == "AvatarHeadShot") {
            if (i + 1 == Object.keys(req.body).length) {
                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://reblox.zip/Thumbs/HeadShot.ashx\", \"version\": \"TN3\" }"
            }
            else {
                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://reblox.zip/Thumbs/HeadShot.ashx\", \"version\": \"TN3\" },"
            }
        }
        else if (req.body[i]["type"] == "GameIcon") {
            if (filesystem.existsSync("./games.json")) {
                var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

                var found = false
                json.forEach((game) => {
                    if (game["id"] == req.body[i]["targetId"]) {
                        found = true
                        if (filesystem.existsSync("./icons/" + game["rootPlaceId"] + ".png")) {
                            if (i + 1 == Object.keys(req.body).length) {
                                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["rootPlaceId"] + "&wd=700&ht=700&fmt=png\", \"version\": \"TN3\" }"
                            }
                            else {
                                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://www.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + game["rootPlaceId"] + "&wd=700&ht=700&fmt=png\", \"version\": \"TN3\" },"
                            }
                        }
                        return
                    }
                })

                if (found == false) {
                    if (i + 1 == Object.keys(req.body).length) {
                        unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/gameicon.png\", \"version\": \"TN3\" }"
                    }
                    else {
                        unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/gameicon.png\", \"version\": \"TN3\" },"
                    }
                }
            }
            else {
                if (i + 1 == Object.keys(req.body).length) {
                    unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/gameicon.png\", \"version\": \"TN3\" }"
                }
                else {
                    unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/gameicon.png\", \"version\": \"TN3\" },"
                }
            }
        }
        else if (req.body[i]["type"] == "Asset") {
            if (i + 1 == Object.keys(req.body).length) {
                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + req.body[i]["targetId"] + "&wd=" + req.body[i]["size"].split("x")[0] + "&ht=" + req.body[i]["size"].split("x")[1] + "&fmt=png\", \"version\": \"TN3\" }"
            }
            else {
                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + req.body[i]["targetId"] + "&wd=" + req.body[i]["size"].split("x")[0] + "&ht=" + req.body[i]["size"].split("x")[1] + "&fmt=png\", \"version\": \"TN3\" },"
            }
        }
        else if (req.body[i]["type"] == "AutoGeneratedAsset") {
            if (i + 1 == Object.keys(req.body).length) {
                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + req.body[i]["targetId"] + "&wd=" + req.body[i]["size"].split("x")[0] + "&ht=" + req.body[i]["size"].split("x")[1] + "&fmt=png\", \"version\": \"TN3\" }"
            }
            else {
                unfinishedstring += " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://assetgame.reblox.zip/Game/Tools/ThumbnailAsset.ashx?aid=" + req.body[i]["targetId"] + "&wd=" + req.body[i]["size"].split("x")[0] + "&ht=" + req.body[i]["size"].split("x")[1] + "&fmt=png\", \"version\": \"TN3\" },"
            }
        }
        else {
            console.log("\x1b[34m%s\x1b[0m", "<STUB> Unknown type: " + req.body[i]["type"])
        }
    }
    res.status(200).send(unfinishedstring + " ] }")
})

app.get("/headshot-thumbnail/image", (req, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"headshot.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/headshot.png"))
})

app.get("/headshot-thumbnail/json", (_, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"headshot.png\"")
    res.status(200).send("{\"Url\":\"http://www.reblox.zip/headshot-thumbnail/image\", \"Final\": true}")
})

app.get("/v1/locales", (req, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\": [{\"locale\":{\"id\":1, \"locale\":\"en_us\", \"name\": \"English (United States)\", \"nativeName\":\"English (United States)\",\"language\":{\"id\":41,\"name\":\"English\",\"nativeName\":\"English\",\"languageCode\":\"en\", \"isRightToLeft\": false}},\"isEnabledForFullExperience\":true,\"IsEnabledForSignupAndLogin\":true,\"IsEnabledForInGameUgc\":true}]}")
})

app.get("/v1/game-localization-roles/games/:id/current-user/roles", (req, res) => {
    res.setHeader("cache-control", "no-cache")

    res.setHeader("content-type", "application/json; charset=utf-8")
    if (filesystem.existsSync("./games.json")) {
        var json = JSON.parse(filesystem.readFileSync("./games.json", "utf8"))

        var found = false
        json.forEach((game) => {
            if (game["id"] == req.params.id && game["creatorTargetId"] == userId) {
                found = true
                res.status(200).send("{\"data\": [ \"owner\" ]}")
            }
        })

        if (found == false) res.status(200).send("{\"data\": []}")
    }
    else {
        res.status(200).send("{\"data\": []}")
    }
})
app.get("/v2/users/:userid/groups/roles", (_, res) => {
    res.setHeader("content-type", "application/json; charset=utf-8")
    res.status(200).send("{\"data\":[]}")
})

app.get("/v2/users/:id/inventory/:typeid", (_, res) => {
    res.setHeader("cache-control", "no-cache")
    res.setHeader("content-type", "application/json; charset=utf-8")

    res.status(200).send("{\"previousPageCursor\": null, \"nextPageCursor\": null, \"data\": []}") //STUB
})

app.use((req, res) => {
    // res.setHeader("cache-control", "no-cache")
    res.status(404).end();
    console.log("\x1b[33m%s\x1b[0m", "<WARN> NOT IMPLEMENTED: \"" + req.protocol + "://" + req.get("host") + req.originalUrl + "\" (" + req.method + ")")
    if (req.method == "POST") {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Request Body:\n" + toString(req.body))
    }
    else if (req.method == "PUT") {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Request Body:\n" + toString(req.body))
    }
    else if (req.method == "PATCH") {
        console.log("\x1b[33m%s\x1b[0m", "<WARN> Request Body:\n" + toString(req.body))
    }
});

app.listen(80, (req, res) => {
    console.log("\x1b[32m%s\x1b[0m", "<INFO> Started a HTTP server at port 80")
})

if (enableHTTPS) {
    const options = {
        ca: filesystem.readFileSync("./../../../ca.pem"), //CHANGE THIS WHEN YOU USE THIS IN A DIFFERENT DIRECTORY!
        key: filesystem.readFileSync("cert-key.pem"),
        cert: filesystem.readFileSync("cert.pem")
    }

    https.createServer(options, app).listen(443, (req, res) => {
        console.log("\x1b[32m%s\x1b[0m", "<INFO> Started a HTTPS server at port 443")
    })

}
