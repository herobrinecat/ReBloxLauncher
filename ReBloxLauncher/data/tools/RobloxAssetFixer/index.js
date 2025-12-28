console.log("\x1b[32m","<INFO> Starting server...")
//If the assetdeilvery/thumbnail server of Roblox decides to fail, the asset part will fail also.
const express = require("express");
const https = require("https");
const http = require("http")
const filesystem = require("fs");
const zlib = require("zlib")
const crypto = require("crypto")
const readline = require("readline")
const jwt = require("jsonwebtoken")

//Create express server
const app = express();
app.use(express.json())
app.use(express.urlencoded({ extended: true }))

//User variables
var username = "Player" // can be set with -username
var userId = 1 // can be set with -userid
var accountOver13 = true // can be set with -accountUnder13
var avatarR15 = false // can be set with -r15
var avatarBodyColor = [] //can be set with -bodycolor=[]

//Server variables
var assetfolder = "./assets" //A directory path where the server will look if there's any assets to use locally
var verbose = true //Gives out more info like what the server is doing
var useAuth = false //ROBLOSECURITY is needed to be filled out to use this, can get other assets apart from Decals (can be set with -useAuth)
var ROBLOSECURITY = "" //This is required to tell the difference between Decal and Image due to assetdelivery update (if useAuth is enabled) (can be set with -ROBLOSECURITY [please put this before -useAuth])
var ignoreUrl = true //Ignores the check of url
var saveFile = false //saves the file that's not part of the file assets to the saved folder
var joining = false //Use to make several function contact the ip's server instead of simulating it...
var ip = "" // required for joining variable
var enableDataStore = true
var enableBadges = true
var enableFollow = true
var RBDFpath = "./default.rbdf" //A path to the ReBlox Datastore File
var treatImagesAsAssetForUseAuth = true //Treat the images as if they are not images when using useAuth
var clothids = [] // for clothing and assets for characters.
var clothidsstring = "" //ditto but as string
var disableParsingDecals = false //Disables parsing of decals if using treatImagesAsAssetForUseAuth.
var localClothes = true //enables -clothes as argument to support multiple user ids (enabled by default)
var assetsFromServer = false //makes the asset load from the server instead of emulating if joining (Asset packs for the server you're joining is no longer required!)
var useNewSignatureFormat = true //Uses the rbxsig format when signing scripts
var privateKey = "./../../private.pem"
var useNewSignatureAssetFormat = true
function isNumeric(str) {
  if (typeof str != "string") return false // we only process strings!  
  return !isNaN(str) && // use type coercion to parse the _entirety_ of the string (`parseFloat` alone does not do this)...
         !isNaN(parseFloat(str)) // ...and ensure strings of whitespace fail
}

process.on('uncaughtException', (err) => {
    console.log("\x1b[31m","<ERROR> Something went wrong while trying to process a request, this is usually due to the malformed request that the server can't handle or a bug, please check the error stack below!\n", err)
    console.log("\x1b[37m", "")
})

process.on('exit', (code) => {
    console.log("\x1b[37m", "")
})
process.argv.forEach(function (val) {
	if (val == "-useAuth") 
	{
		if (ROBLOSECURITY.startsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|")) {
			//wow, roblox still uses the old way of saying Robux.
			useAuth = true
		}
		else {
			console.log("\x1b[33m","<WARN> A valid ROBLOSECURITY is required, disabling useAuth...")
			useAuth=false
		}
	}
	else if (val.startsWith("-ROBLOSECURITY=")) 
	{
		ROBLOSECURITY = val.slice(15)
	}
    else if (val.startsWith("-username=")) 
	{
		if (val.slice(10).length >= 3 && val.slice(10).length <= 20) {
            username = val.slice(10)
        }
        else {
            console.log("\x1b[33m","<WARN> A valid username is required if you're using -username, using \"Player\" instead...")
        }
	}
    else if (val.startsWith("-userid=")) 
	{
		if (isNumeric(val.slice(8))) {
            userId = parseInt(val.slice(8))
        }
        else {
            console.log("\x1b[33m","<WARN> A valid UserId is required if you're using -userid, using 1 instead");
        }
	}
    else if (val == "-accountUnder13") {
        accountOver13 = false
    }
	else if (val == "-r15") { 
		avatarR15 = true
	}
	else if (val.startsWith("-bodycolor="))
	{
		avatarBodyColor = val.slice(12).slice(0,-1).split(',')
	}
    else if (val.startsWith("-clothes=")) {
        clothids = val.slice(10).slice(0,-1).split(',')
        clothidsstring = val.slice(10).slice(0,-1)
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
    else if (val.startsWith("-rbdf=")) {
        RBDFpath = val.slice(6).slice(0,-1)
    }
    else if (val == "-disableParseDecals") {
        disableParsingDecals = true
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
    else if (val == "-help") {
        console.log("\x1b[37m","\n<INFO> Usage for RobloxAssetFixer:\n\n-ROBLOSECURITY=\"roblosecurity\" - Set your ROBLOSECURITY (required for useAuth)\n-useAuth - Set the asset retrieval to use Roblox's servers that requires auth\n-username= - Set your player's username\n-userid= - Set your player's userid\n-accountUnder13 - Mark your account <13\n-r15 - Set your avatar to be R15\n-bodycolor=[0,0,0,0,0,0] - Set your body color of your avatar (works in 2016M and older)\n-clothes=[] - Set the asset ids of your avatar for customzation\n-ip= - Set an IP to the server if you're joining (required for -joining)\n-joining - Mark the server as joining and make several functions connect to the host's server instead of simulating it\n-disableDataStore - disable saving and loading of datastore with RBDF\n-disableBadges - disable saving badges with RBDF\n-disableFollowing - disable saving followers with RBDF\n-rbdf=\"path\" - A path to a ReBlox Datastore File\n-disableParseDecals - Disables parsing of decals if using treatImagesAsAssetForUseAuth")
        process.exit(0)
    }
})
console.log("\x1b[37m","<INFO> Username: " + username)
console.log("\x1b[37m","<INFO> User ID: " + userId)
console.log("\x1b[37m", "<INFO> Account Age: " + ((accountOver13) ? "13+" : "<13"))
console.log("\x1b[37m","<INFO> Avatar Type: " + ((avatarR15) ? "R15" : "R6"))

if (joining && ip != "") console.log("\x1b[37m","<INFO> Server IP: " + ip)
function calculateMD5(data) {
    const hash = crypto.createHash("md5")

    hash.update(data)

    return hash.digest("hex")
}

function getAsset(id, callback) {
    try {
        if (useAuth == false) {
        if (verbose == true) {
            console.log("\x1b[33m","<WARN> Not using auth, only limited to decals")
        }
         var options = {
            host: 'apis.roblox.com',
            port: 443,
            path: '/toolbox-service/v1/items/details?assetIds=' + id,
            method: "GET"

        }
            var inforesult = ""
        https.get(options, function(res1) {

            res1.setEncoding("utf8")
            res1.on("data", (chunk) => {
                inforesult += chunk
            })
            res1.on("end", () => {
                if (res1.statusCode == 200) {
                    var jsoninfo = JSON.parse(inforesult)
                if (jsoninfo["data"][0]["asset"]["typeId"] == 13) {
                     var options1 = {
            host: 'thumbnails.roblox.com',
            port: 443,
            path: '/v1/assets?assetIds=' + id + '&returnPolicy=PlaceHolder&size=700x700&format=png',
            method: "GET"

        }
            var infoiresult = ""
        https.get(options1, function(res2) {

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
        https.get(options2, function(res3) {

            var data = []
            if (res3.headers["content-encoding"]=='gzip') {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Image) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res3.pipe(gzip)
                            output=gzip
                        }
                        else if (res3.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Image) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res3.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Image)")
                            }
                            output = res3
                        }
            output.on("data", (chunk) => {
                data.push(chunk)
            })
            output.on("end", () => 
			{
				var buffer = Buffer.concat(data)
                if (saveFile == true) {
                    if (filesystem.existsSync("./saved")) 
					{
						filesystem.writeFileSync("./saved/" + id + ".png", buffer)
					}
					else {
						filesystem.mkdirSync("./saved")
                        filesystem.writeFileSync("./saved/" + id + ".png", buffer)
					}
                }
            return callback(buffer)
            })
        })
            })
        })
                }
                else {
                     return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
                }
                }
                else {
                    return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
                }
            })
        })
       }
       else {
         var options = {
            host: 'apis.roblox.com',
            port: 443,
            path: '/assets/user-auth/v1/assets/' + id + '?readMask=assetType',
            method: "GET",
            headers: {
                'Cookie': '.ROBLOSECURITY=' + ROBLOSECURITY
            }

        }
            var inforesult = ""
        https.get(options, function(res1) {

            res1.setEncoding("utf8")
            res1.on("data", (chunk) => {
                inforesult += chunk
            })
            res1.on("end", () => {
                if (res1.statusCode == 200) {
                    var jsoninfo = JSON.parse(inforesult)
                if (treatImagesAsAssetForUseAuth == false && jsoninfo["assetType"] == "Decal" || treatImagesAsAssetForUseAuth == false && jsoninfo["assetType"] == "Image") {
                     var options1 = {
            host: 'thumbnails.roblox.com',
            port: 443,
            path: '/v1/assets?assetIds=' + id + '&returnPolicy=PlaceHolder&size=700x700&format=png',
            method: "GET"
        }
            var infoiresult = ""
        var req2 = https.request(options1, function(res2) {

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
                "Accept-Encoding": "gzip, deflate"
            }
        }
        https.get(options2, function(res3) {

            var data = [],output
              if (res3.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Image) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res3.pipe(gzip)
                            output=gzip
                        }
                        else if (res3.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Image) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res3.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Image)")
                            }
                            output = res3
                        }
            output.on("data", (chunk) => {
                data.push(chunk)
            })
            output.on("end", () => {
                var buffer = Buffer.concat(data)
                if (saveFile == true) {
                    if (filesystem.existsSync("./saved")) 
					{
						filesystem.writeFileSync("./saved/" + id + ".png", buffer)
					}
					else {
						filesystem.mkdirSync("./saved")
                        filesystem.writeFileSync("./saved/" + id + ".png", buffer)
					}
                }
            return callback(buffer)
            })
        })
                }
                else {
                    return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
                }
            })
        })
        req2.end()
                }
                else {
                    
                    var options2 = {
            host: 'assetdelivery.roblox.com',
            port: 443,
            path: '/v2/asset/?id=' + id,
            method: "GET",
			headers: {
                'Cookie': '.ROBLOSECURITY=' + ROBLOSECURITY
            }
        }
        https.get(options2, function(res3) {

            var inforesult = ""
            res3.on("data", (chunk) => {
                inforesult += chunk
            })
            res3.on("end", () => {
                var jsonobject = JSON.parse(inforesult)
              if (jsonobject["locations"] != undefined) {
                  var options3 = {
                    host: jsonobject["locations"][0]["location"].slice(8).substring(0,14),
                    port: 443,
                    path: jsonobject["locations"][0]["location"].slice(22),
                    method: "GET",
                    headers: {
                        'User-Agent': 'totallychrome',
                        'Cookie': '.ROBLOSECURITY=' + ROBLOSECURITY,
                        'Accept-Encoding': 'gzip, deflate'
                    }
                }
                https.get(options3, function(res4) {
                    var data = [],output
                        if (res4.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res4.pipe(gzip)
                            output=gzip
                        }
                        else if (res4.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res4.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + id + " from Roblox server (Asset)")
                            }
                            output = res4
                        }
                    output.on("data", (chunk) => {
                        data.push(chunk)
                    })
                    output.on("end", () => {
                        var buffer = Buffer.concat(data)
                       if (saveFile == true) {
                         if (filesystem.existsSync("./saved")) 
					{
						if (jsonobject["assetTypeId"] == 3) {
                            if (data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67) {
                                filesystem.writeFileSync("./saved/" + id + ".ogg", buffer)
                            }
                            else {
                                filesystem.writeFileSync("./saved/" + id + ".mp3", buffer)
                            }
                        }
                        else if (jsonobject["assetTypeId"] == 40) {
                            filesystem.writeFileSync("./saved/" + id + ".mesh", buffer)
                        }
                        else if (jsonobject["assetTypeId"] == 1) {
                            filesystem.writeFileSync("./saved/" + id + ".png", buffer)
                        }
                        else if (jsonobject["assetTypeId"] == 13) {
                            filesystem.writeFileSync("./saved/" + id + ".rbxmx", buffer)
                        }
                        else if (jsonobject["assetTypeId"] == 10) {
                            filesystem.writeFileSync("./saved/" + id + ".rbxm", buffer)
                        }
                        else {
                            filesystem.writeFileSync("./saved/" + id + ".bin", buffer)
                        }
					}
					else {
						filesystem.mkdirSync("./saved")
                        if (jsonobject["assetTypeId"] == 3) {
                            if (data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67) {
                                filesystem.writeFileSync("./saved/" + id + ".ogg", buffer)
                            }
                            else {
                                filesystem.writeFileSync("./saved/" + id + ".mp3", buffer)
                            }
                        }
                        else if (jsonobject["assetTypeId"] == 40) {
                            filesystem.writeFileSync("./saved/" + id + ".mesh", buffer)
                        }
                        else if (jsonobject["assetTypeId"] == 1) {
                            filesystem.writeFileSync("./saved/" + id + ".png", buffer)
                        }
                        else if (jsonobject["assetTypeId"] == 13) {
                            filesystem.writeFileSync("./saved/" + id + ".rbxmx", buffer)
                        }
                        else if (jsonobject["assetTypeId"] == 10) {
                            filesystem.writeFileSync("./saved/" + id + ".rbxm", buffer)
                        }
                        else {
                            filesystem.writeFileSync("./saved/" + id + ".bin", buffer)
                        }
					}
                       }
                       if (jsonobject["assetTypeId"] == 13 && disableParsingDecals == false) {

                        var myRegExp= new RegExp("<url>http:\\/\\/www\\.roblox\\.com\\/asset\\/\\?id=(.*?)<\\/url>")
                        var match = myRegExp.exec(buffer.toString("utf8"))
                        if (match != null) {
                            var value = match[1]
                            getAsset(value, (result) => {
                                return callback(result)
                            })
                        }
                        else {
                            return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
                        }
                       }
                       else {
                            return callback(buffer)
                       }
                        
                    })
                })
              }
              else {
                console.log("\x1b[31m", "<ERROR> Something went wrong when trying to download asset " + id + " from the Roblox server!")
                return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
              }
			})
        })
                }
                }
                else {
                    return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
                }
            })
        })
       }
    }
    catch {
        console.log("\x1b[31m", "<ERROR> Something went wrong when trying to download asset " + id + " from the Roblox server!")
        return "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}"
    }
}
app.get("/", (req, res) => {
    res.send("OK")
})

app.get("/asset", (req,res) => {
    res.setHeader("Cache-Control", "no-cache")
    var assetfound = false
    if (isNumeric(req.query.id)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.id.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.id, (result) => {
        res.send(result)
      })
      }
    }
    }
    else if (isNumeric(req.query.assetversionid)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetversionid.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.assetversionid, (result) => {
        res.send(result)
      })
      }
    }
    }
    else {
        res.status(404).end()
    }
})
app.get("/asset/", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
   var assetfound = false
    if (isNumeric(req.query.id)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.id.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.id, (result) => {
        res.send(result)
      })
      }
    }
    }
    else if (isNumeric(req.query.assetversionid)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetversionid.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.assetversionid, (result) => {
        res.send(result)
      })
      }
    }
    }
    else {
        res.status(404).end()
    }
})


app.get("//asset/", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var assetfound = false
    if (isNumeric(req.query.id)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.id.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.id, (result) => {
        res.send(result)
      })
      }
    }
    }
    else if (isNumeric(req.query.assetversionid)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetversionid.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.assetversionid, (result) => {
        res.send(result)
      })
      }
    }
    }
    else {
        res.status(404).end()
    }
})

app.get("/v1/asset", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var assetfound = false
    if (isNumeric(req.query.id)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.id.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.id, (result) => {
        res.send(result)
      })
      }
    }
    }
    else if (isNumeric(req.query.assetversionid)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetversionid.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.assetversionid, (result) => {
        res.send(result)
      })
      }
    }
    }
    else {
        res.status(404).end()
    }
})

app.get("/v1/asset/", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var assetfound = false
    if (isNumeric(req.query.id)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.id.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%" + req.query.id + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.id + "%\r\n" : "%\r\n%" + req.query.id + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.id, (result) => {
        res.send(result)
      })
      }
    }
    }
    else if (isNumeric(req.query.assetversionid)) {
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetversionid.toString().trim()) {
            if (verbose) {
                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from asset folder (Asset)")
            }
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            if (file.endsWith(".lua")) {
                res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from((useNewSignatureAssetFormat ? "\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%" + req.query.assetversionid + "%\r\n"), "utf8") + filesystem.readFileSync(assetfolder + "/" + file), {key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + (useNewSignatureAssetFormat ? "%\r\n--rbxassetid%" + req.query.assetversionid + "%\r\n" : "%\r\n%" + req.query.assetversionid + "%\r\n") + filesystem.readFileSync(assetfolder + "/" + file, "utf8"))
            }
            else {
                res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            }
            assetfound = true
            return
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
                 var data = [],output
                        if (res1.headers["content-encoding"]=='gzip') {
                           if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res1.pipe(gzip)
                            output=gzip
                        }
                        else if (res1.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res1.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.assetversionid + " from local server (Asset)")
                            }
                            output = res1
                        }
                res1.on("data", (chunk) => {
                    data.push(chunk)
                })
                res1.on("end", () => {
                    var buffer = Buffer.concat(data)
                    if (buffer.length < 56) {
                        if (buffer.toString() == "{\"error\":[{\"code\":0,\"message\":\"Something went wrong\"}]}") {
                        getAsset(req.query.id, (result) => {
                            res.send(result)
                        })
                        return
                    }
                    }
                    res.status(res1.statusCode).send(buffer)
                })
            })

            } catch {
                res.status(500).end()
            }
      }
      else {
        getAsset(req.query.assetversionid, (result) => {
        res.send(result)
      })
      }
    }
    }
    else {
        res.status(404).end()
    }
})


app.get("/Thumbs/Avatar.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
     res.setHeader("Content-disposition", "attachment; filename=\"avatar.png\"")
            res.status(200).send(filesystem.readFileSync(assetfolder + "/avatar.png"))
})

app.get("/avatar-thumbnail/image", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"avatar.png\"")
            res.status(200).send(filesystem.readFileSync(assetfolder + "/avatar.png"))
})

app.get("/Thumbs/HeadShot.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"headshot.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/headshot.png"))
})
app.get("/Thumbs/Asset.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var assetfound = false
    if (isNumeric(req.query.assetId)) {
        if (verbose) {
            console.log("\x1b[34m", "<INFO> Getting " + req.query.assetId + " from Roblox server/file (Image)")
        }
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetId.toString().trim()) {
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            assetfound = true
            return
        }
    })
    if (assetfound == false ) {
        var options1 = {
            host: 'thumbnails.roblox.com',
            port: 443,
            path: '/v1/assets?assetIds=' + req.query.assetId + '&returnPolicy=PlaceHolder&size=700x700&format=png',
            method: "GET"
        }
            var infoiresult = ""
        https.get(options1, function(res2) {

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
        https.get(options2, function(res3) {

            var data = [],output
              if (res3.headers["content-encoding"]=='gzip') {
                      if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from Roblox server (Image) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res3.pipe(gzip)
                            output=gzip
                        }
                        else if (res3.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from Roblox server (Image) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res3.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from Roblox server (Image)")
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
            })
        })
    }
    else {
        res.status(404).end()
    }
    
}
})

app.get("/Game/Tools/ThumbnailAsset.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var assetfound = false
    if (isNumeric(req.query.assetId)) {
        if (verbose) {
            console.log("\x1b[34m", "<INFO> Getting " + req.query.assetId + " from Roblox server/file (Image)")
        }
        filesystem.readdirSync(assetfolder).forEach(file => {
        var splited = file.split('.')
        if (splited[0] == req.query.assetId.toString().trim()) {
            res.setHeader("Content-disposition", "attachment; filename=\"" + file + "\"")
            res.status(200).send(filesystem.readFileSync(assetfolder + "/" + file))
            assetfound = true
            return
        }
    })
    if (assetfound == false ) {
        var options1 = {
            host: 'thumbnails.roblox.com',
            port: 443,
            path: '/v1/assets?assetIds=' + req.query.aid + '&returnPolicy=PlaceHolder&size=' + req.query.wd + 'x' + req.query.ht +'&format=' + req.query.fmt,
            method: "GET"
        }
            var infoiresult = ""
        https.get(options1, function(res2) {

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
        https.get(options2, function(res3) {

            var data = [],output
              if (res3.headers["content-encoding"]=='gzip') {
                      if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from Roblox server (Image) [gzip compression]")
                            }
                            var gzip =zlib.createGunzip()
                            res3.pipe(gzip)
                            output=gzip
                        }
                        else if (res3.headers["content-encoding"]=='deflate'){
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from Roblox server (Image) [deflate compression]")
                            }
                            var deflate = zlib.createDeflate()
                            res3.pipe(deflate)
                            output=deflate
                        }
                        else {
                            if (verbose) {
                                console.log("\x1b[34m", "<INFO> Getting " + req.query.id + " from Roblox server (Image)")
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
            })
        })
    }
    else {
        res.status(404).end()
    }
    
}
})

app.get("/game/GetCurrentUser.ashx", (req, res) => {
    res.status(200).send(toString(userId))
})
const options = {
    key: filesystem.readFileSync("cert-key.pem"),
    cert: filesystem.readFileSync("cert.pem")
}
app.get("/my/settings/json", (req,res) => {
    res.status(200).send("{\"ChangeUsernameEnabled\":true,\"IsAdmin\":false,\"UserId\":" + userId + ",\"Name\":\"" + username + "\",\"DisplayName\": \"" + username + "\", \"UserAbove13\":" + accountOver13 + ", \"IsEmailOnFile\":true,\"IsEmailVerified\":true,\"UserEmail\":\"r*****@fakerebloxemail.com\",\"UserEmailMasked\":true,\"UserEmailVerified\":true,\"LocaleApiDomain\":\"http://locale.reblox.zip\",\"ApiProxyDomain\":\"http://api.reblox.zip\",\"AuthDomain\":\"http://auth.reblox.zip\",\"IsPremium\":false,\"AccountAgeInDays\":365,\"ClientIpAddress\":\"127.0.0.1\",\"IsDisplayNamesEnabled\": true,\"PremiumFeatureId\": null}")
})
app.get("/game/join.ashx", (req,res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
		if (verbose) console.log("\x1b[32m","<INFO> Mid-2017 or earlier detected, using join.ashx")
            if (filesystem.existsSync("./game/join.ashx")) {
                res.status(200).send(filesystem.readFileSync("./game/join.ashx"))
            }
            else {
                if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D,0x0A]) + filesystem.readFileSync("joinscript.txt"), filesystem.readFileSync(privateKey, "utf8")).toString("base64") + "%\r\n" + filesystem.readFileSync("joinscript.txt", "utf8"))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("joinscript.txt"))
				}
            }
	}
	else {
		res.status(404).end()
	}
})

app.get("/game/gameserver.ashx", (req,res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
		if (verbose) console.log("\x1b[32m","<INFO> Mid-2016* or earlier detected, using gameserver.ashx")
            if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + filesystem.readFileSync("./game/gameserver.ashx"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/gameserver.ashx","utf8"))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("./game/gameserver.ashx","utf8"))
				}
	res.status(200).send()
	}
	else {
		res.status(404).end()
	}
})

app.get("/game/visit.ashx", (req,res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
		if (verbose) console.log("\x1b[32m","<INFO> Mid-2016 or earlier detected, using visit.ashx")
	if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/visit.ashx","utf8").replaceAll("%userId%", userId.toString()), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/visit.ashx","utf8").replaceAll("%userId%", userId.toString()))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("./game/visit.ashx","utf8").replaceAll("%userId%", userId.toString()))
				}
	}
	else {
		res.status(404).end()
	}
})

app.get("/Asset/CharacterFetch.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
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
        console.log("\x1b[32m", "<INFO> Getting the avatar of " + req.query.userId)
        if (localClothes == true) {
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
             res.status(200).send("http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId)
        }
    } 
   
})
app.get("/Game/LoadPlaceInfo.ashx", (req,res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
		if (verbose) console.log("\x1b[32m","<INFO> Mid-2017 or earlier detected, using LoadPlaceInfo.ashx")
	if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/LoadPlaceInfo.ashx", "utf8").replaceAll("%userId%", userId), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/LoadPlaceInfo.ashx","utf8").replaceAll("%userId%", userId))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("./game/LoadPlaceInfo.ashx","utf8").replaceAll("%userId%", userId))
				}
	}
	else {
		res.status(404).end()
	}
})

app.get("/game/studio.ashx", (req,res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
		if (verbose) console.log("\x1b[32m","<INFO> Early-2015 or earlier detected, using Studio.ashx")
	if (filesystem.existsSync("./game/studio.ashx")) {
        if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/studio.ashx", "utf8").replaceAll("{id}", userId), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/studio.ashx","utf8").replaceAll("{id}", userId))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("./game/studio.ashx","utf8").replaceAll("{id}", userId))
				}
        res.status(200).send(filesystem.readFileSync("./game/studio.ashx","utf-8").replaceAll("{id}", userId))
    }
	}
	else {
		res.status(404).end()
	}
})

app.get("/Game/PlaceSpecificScript.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
		if (verbose) console.log("\x1b[32m","<INFO> Mid-2016 or earlier detected, using PlaceSpecificScript.ashx")
	if (filesystem.existsSync("./game/placespecificscript.ashx")) {
        if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + filesystem.readFileSync("./game/placespecificscript.ashx"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/placespecificscript.ashx","utf8"))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("./game/placespecificscript.ashx","utf8"))
				}
    }
	}
	else {
		res.status(404).end()
	}
})

app.get("//game/studio.ashx", (req,res) => {
	if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
        res.setHeader("Cache-Control", "no-cache")
		if (verbose) console.log("\x1b[32m","<INFO> Early-2015 or earlier detected, using Studio.ashx")
	if (filesystem.existsSync("./game/studio.ashx")) {
        if (filesystem.existsSync(privateKey)) 
				{
					res.status(200).send((useNewSignatureFormat ? "--rbxsig%" : "%") + crypto.sign("SHA1", Buffer.from([0x0D, 0x0A]) + Buffer.from(filesystem.readFileSync("./game/studio.ashx", "utf8").replaceAll("{id}", userId), "utf8"), { key: filesystem.readFileSync(privateKey, "utf8"), padding: crypto.constants.RSA_PKCS1_PADDING}).toString("base64") + "%\r\n" + filesystem.readFileSync("./game/studio.ashx","utf8").replaceAll("{id}", userId))
				}
				else
				{
					res.status(200).send(filesystem.readFileSync("./game/studio.ashx","utf8").replaceAll("{id}", userId))
				}   
    }
	}
	else {
		res.status(404).end()
	}
})

// idk what does GetScriptState do, can anyone explain to me what these do???
app.get("/asset/GetScriptState.ashx", (req, res) => {
    res.status(200).send("0 0 0 0")
})

app.get("//asset/GetScriptState.ashx", (req, res) => {
    res.status(200).send("0 0 0 0")
})

app.post("/Error/Dmp.ashx", (req, res) => {
    res.status(200).end() //STUB
})

app.get("/UploadMedia/UploadVideo.aspx", (req, res) => {
    res.status(200).send("Caught you recording ;)")
})
app.get("/game/ChatFilter.ashx", (req, res) => {
    res.send("True")
})
app.get("/v1.1/avatar-fetch", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "api.roblox.com" || ignoreUrl == true) 
	{
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
        console.log("\x1b[32m", "<INFO> Getting the avatar of " + req.query.userId + " via /v1.1/avatar-fetch")
        if (localClothes == true) {
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
                 res.status(200).send("{\"resolvedAvatarType\":\"" + json["bodyType"] + "\",\"accessoryVersionIds\":[" + tempclothes +"],\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"bodyColorsUrl\":\"http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId + "\",  \"bodyColors\":{\"HeadColor\":" + json["colors"]["headColor"] + ",\"LeftArmColor\":" + json["colors"]["leftArmColor"] + ",\"LeftLegColor\":" + json["colors"]["leftLegColor"] + ",\"RightArmColor\":" + json["colors"]["rightArmColor"] + ",\"RightLegColor\":" + json["colors"]["rightLegColor"] + ",\"TorsoColor\":" + json["colors"]["torsoColor"] + "},\"animations\":{},\"scales\":{\"Width\":1.0000,\"Height\":1.0000,\"Head\":1.0000,\"Depth\":1.00}}")
            }
            else {
                res.status(200).send("{\"resolvedAvatarType\":\"" + ((avatarR15) ? "R15" : "R6") + "\",\"accessoryVersionIds\":[],\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"bodyColorsUrl\":\"http://reblox.zip/Asset/BodyColors.ashx?userId=" + req.query.userId + "\",  \"bodyColors\":{\"HeadColor\":194,\"LeftArmColor\":194,\"LeftLegColor\":194,\"RightArmColor\":194,\"RightLegColor\":194,\"TorsoColor\":194},\"animations\":{},\"scales\":{\"Width\":1.0000,\"Height\":1.0000,\"Head\":1.0000,\"Depth\":1.00}}")
            }
        }
        else {
            res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + clothidsstring + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": " + avatarBodyColor[0] +", \"torsoColorId\": " + avatarBodyColor[5] + ", \"rightArmColorId\": " + avatarBodyColor[3] + ", \"leftArmColorId\": " + avatarBodyColor[1] + ", \"rightLegColorId\": " + avatarBodyColor[4] + ", \"leftLegColorId\": " + avatarBodyColor[2] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
        }
    } 
       
	}
	else {
		res.status(404).end()
	}
})
app.get("/v1/avatar-fetch", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
	if (req.get("host") == "avatar.roblox.com" || ignoreUrl == true) 
	{
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
        console.log("\x1b[32m", "<INFO> Getting the avatar of " + req.query.userId + " via /v1/avatar-fetch")
        if (localClothes == true) {
            var tempclothes = ""
            if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) {
                var json = JSON.parse(filesystem.readFileSync("./clothes/" + req.query.userId + ".json"))
                for (var i = 0; i < json["asset"].length; i++) {
                    if (i == json["asset"].length - 1) {
                        tempclothes = tempclothes + json["asset"][i]["id"]
                    }
                    else {
                        tempclothes = tempclothes + json["asset"][i]["id"] + ","
                    }
                }
                res.status(200).send("{\"resolvedAvatarType\": \"" + json["bodyType"] + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + tempclothes + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" +  json["bodyType"] + "\", \"bodyColors\": { \"headColorId\": " +  json["colors"]["headColor"] +", \"torsoColorId\": " + json["colors"]["torsoColor"] + ", \"rightArmColorId\": " + json["colors"]["rightArmColor"] + ", \"leftArmColorId\": " + json["colors"]["leftArmColor"] + ", \"rightLegColorId\": " + json["colors"]["rightLegColor"] + ", \"leftLegColorId\": " + json["colors"]["leftLegColor"] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
            }
        }
        else {
            res.status(200).send("{\"resolvedAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\",\"equippedGearVersionIds\":[],\"backpackGearVersionIds\":[],\"assetAndAssetTypeIds\":[" + clothidsstring + "],\"animationAssetIds\":{}, \"playerAvatarType\": \"" + ((avatarR15) ? "R15" : "R6") + "\", \"bodyColors\": { \"headColorId\": " + avatarBodyColor[0] +", \"torsoColorId\": " + avatarBodyColor[5] + ", \"rightArmColorId\": " + avatarBodyColor[3] + ", \"leftArmColorId\": " + avatarBodyColor[1] + ", \"rightLegColorId\": " + avatarBodyColor[4] + ", \"leftLegColorId\": " + avatarBodyColor[2] + "},\"scales\": { \"height\": 1.0000, \"width\": 1.0000, \"head\": 1.0000, \"depth\": 1.00, \"proportion\": 0.0000, \"bodyType\": 0.0000},\"emotes\":[]}")
        }
    } 
	}
	else {
		res.status(404).end()
	}
})
app.post("/device/initialize", (req, res) => {
    res.status(200).send("{ \"browserTrackerId\": 1, \"appDeviceIdentifier\": null }")
})
app.get("/Asset/BodyColors.ashx", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
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
                    res.setHeader("content-type","text/plain")
                    res.status(res1.statusCode).send(data)
                })
            })

            } catch {
                res.status(500).end()
            }
    }
    else {
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
})
app.get("/userblock/getblockedusers", (req, res) => {
	if (req.get("host") == "api.roblox.com" || ignoreUrl == true) 
	{
	    res.status(200).send("{\"data\":{\"blockedUserIds\":[],\"blockedUsers\":[],\"cursor\":null},\"error\":null}")
	}
	else {
		res.status(404).end()
	}
})

app.post("/v1/avatar/set-avatar", (req, res) => {
    //custom api for ReBlox

        if (joining) {
            try {
                var options = {
                host: ip,
                port: 80,
                path: "/v1/avatar/set-avatar?userId=" + req.query.userId,
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
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
                    console.log("\x1b[32m", "<INFO> Saving the avatar of " + req.query.userId + " to file...")
                    if (filesystem.existsSync("./clothes/" + req.query.userId + ".json")) filesystem.unlinkSync("./clothes/" + req.query.userId + ".json")
                    if (filesystem.existsSync("./clothes") == false) filesystem.mkdirSync("./clothes")
                        filesystem.writeFileSync("./clothes/" + req.query.userId + ".json", JSON.stringify(req.body))
                        res.status(200).end()
                    } catch (ex){
                        res.status(500).end()
                        console.error(ex)
                    }
        }
})

app.post("/universes/create", (req, res) => {
    res.status(200).send("{ \"universeId\": 2, \"rootPlaceId\": 1 }")
})

app.get("/users/get-by-username", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
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
    res.status(200).send("{ \"Id\": " + userId + ", \"Username\": \"" + username + "\",\"MembershipType\":3}")
})

app.get("/users/" + ":userid" + "/canmanage/" + ":id", (req, res) => {
    res.send("{ \"Success\": true, \"CanManage\": true }")
})

app.get("/users/:id", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
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
    res.status(200).send("[]") //STUB
})
app.get("/IDE/Toolbox/Items", (req, res) => {
    res.send("{\"TotalResults\":0,\"Results\":[]}")
})

app.get("/v1/search/universes", (req,res) => {
    res.send("{\"TotalResults\":0,\"Results\":[]}")
})
app.get("/v2/auth/metadata", (req,res) => {
	if (req.get("host") == "auth.roblox.com" || ignoreUrl == true) 
	{
		res.status(200).send("{\"cookieLawNoticeTimeout\": 0}")
	}
	else 
	{
		res.status(404).end()
	}
})
app.post("/v2/login", (req,res) => 
{
	if (req.get("host") == "auth.roblox.com" || ignoreUrl == true) 
	{
        res.setHeader("roblox-machine-id", crypto.randomUUID())
        res.setHeader("set-cookie", ".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_" + jwt.sign({"username": username}, "thisisarebloxprivatekeyforjwtchange",{algorithm: "HS256"}))
        res.status(200).send("{\"user\": { \"id\": " + userId + ", \"name\": \"" + username + "\", \"displayName\": \"" + username + "\"},\"accountBlob\":\"\",\"isBanned\": false }")
	}
	else 
	{
		res.status(404).end()
	}
})

app.post("/sign-out/v1", (req, res) => {
    res.status(200).send("{}")
})

app.post("/v2/logout", (req, res) => {
    res.status(200).send("{}")
})
app.get("/universes/get-info", (req, res) => {
	if (req.get("host") == "api.roblox.com" || ignoreUrl == true) 
	{
	res.status(200).send("{\"Name\":\"ReBlox Place\", \"Description\":\"A ReBlox place launched from the launcher\", \"RootPlace\":1, \"StudioAccessToApisAllowed\":true,\"CurrentUserHasEditPermissions\":true,\"UniverseAvatarType\":\"PlayerChoice\"}")
	}
	else {
		res.status(404).end()
	}
})

app.get("/Game/LuaWebService/HandleSocialRequest.ashx", (req, res) => {
    if (req.get("host") == "assetgame.roblox.com" || ignoreUrl == true) {
        if (req.query.method=="IsInGroup") {
            res.status(200).send("<Value Type=\"boolean\">false</Value>")
        }
        else if (req.query.method=="GetGroupRank") {
            res.status(200).send("<Value Type=\"integer\">0</Value>")
        }
    }
})

app.get("/universes/" + ":id" + "/cloudeditenabled", (req, res) => {
    res.status(200).send("{\"enabled\":false}") //required for creating place in 2018+ studio
})

app.get("/v1/users/" + ":userid" + "/groups/roles", (req, res) => {
    res.status(200).send("{\"data\":[]}")
})

app.get("/v1/universes/" + ":id" + "/permissions", (req, res) => {
    res.status(200).send("{\"canManage\":true,\"canCloudEdit\":false}")
})

app.get("/places/" + ":id" + "/settings", (req, res) => {
    res.status(200).send("{\"Creator\":{\"Name\":\"" + username + ",\"CreatorType\":1,\"CreatorTargetId\":" + userId + "}}") //STUB
})

app.get("/Game/Badge/HasBadge.ashx", async (req, res) => {
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
                else if (line.includes("userId=" + req.query.UserID) && line.includes("badgeId=" + req.query.BadgeID)) {
                    replacementext = line
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (replacementext != "") {
                    res.status(200).send("<Value Type=\"boolean\">True</Value>")
                }
                else {
                    res.status(200).send("<Value Type=\"boolean\">False</Value>")
                }

            }
            else {
                res.status(200).send("<Value Type=\"boolean\">False</Value>")
            }
        }
        else {
            res.status(200).send("<Value Type=\"boolean\">False</Value>")
        }
    }
    else {
            res.status(200).send("<Value Type=\"boolean\">False</Value>")
        }
    }
})
app.post("/assets/award-badge", async (req,res) => {
    if (req.get("host") == "api.roblox.com" || ignoreUrl == true) {
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
                else if (line.includes("userId=" + req.query.userId) && line.includes("badgeId=" + req.query.badgeId)) {
                    replacementtext = line
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (replacementtext != "") {
                    //do nothing
                }
                else {
                    filesystem.appendFileSync(RBDFpath, "<Badge userId=" + req.query.userId +" badgeId=" + req.query.badgeId + ">\n")
                }

            }
            else {
                //do nothing
            }
        }
        else {
            filesystem.writeFileSync(RBDFpath,"RBDF==\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges!\n\n")
            filesystem.appendFileSync(RBDFpath, "<Badge userId=" + req.query.userId +" badgeId=" + req.query.badgeId + ">\n")
        }
    }
         if (isNumeric(req.query.badgeId)) {
        var file = filesystem.readFileSync("./badges.json")
        var jsonresult = JSON.parse(file)
            if (jsonresult[req.query.badgeId.toString()] != undefined) {
				res.send(jsonresult[req.query.badgeId.toString()])
				sent = true
				return
            }
            if (sent == false) {
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
                if (jsoninfo1["name"] != undefined) {
					res.send(jsoninfo1["name"])
					sent = true
                }
				else 
				{
					 if (sent == false) res.send("Stub Badge Name")
				}
        })
        })
    }
    if (verbose) {
        console.log("\x1b[31m","<INFO> Awarding Badge: " + req.query.badgeId)
    }
   
    }
    else {
        res.status(400).end()
    }
        }
    }
})

app.get("/v1/gametemplates", (req, res) => {
    res.status(200).send("{\"data\":[{\"gameTemplateType\":\"Generic\",\"hasTutorials\":false,\"universe\":{\"id\":28220420,\"name\":\"Baseplate\",\"description\":\"\",\"isArchived\":false,\"rootPlaceId\":95206881,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2013-11-01T03:47:14.07\",\"updated\":\"2019-07-08T11:53:21.81\"}},{\"gameTemplateType\":\"Generic\",\"hasTutorials\":false,\"universe\":{\"id\":2464612126,\"name\":\"Classic Baseplate\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":6560363541,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2021-03-23T19:56:45.957\",\"updated\":\"2021-04-16T13:55:13.82\"}},{\"gameTemplateType\":\"Generic\",\"hasTutorials\":false,\"universe\":{\"id\":28223770,\"name\":\"Flat Terrain\",\"description\":\"\",\"isArchived\":false,\"rootPlaceId\":95206192,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2013-11-01T03:47:18.013\",\"updated\":\"2019-07-08T11:53:53.47\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":true,\"universe\":{\"id\":202770430,\"name\":\"Village\",\"description\":\"\",\"isArchived\":false,\"rootPlaceId\":520390648,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2016-10-10T16:32:42.78\",\"updated\":\"2019-11-19T14:07:36.98\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":true,\"universe\":{\"id\":93411794,\"name\":\"Castle\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":203810088,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-01-14T15:46:11.363-06:00\",\"updated\":\"2019-04-04T10:19:51.703-05:00\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":false,\"universe\":{\"id\":138962641,\"name\":\"Suburban\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":366130569,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2016-02-19T18:02:36.483\",\"updated\":\"2019-06-20T16:27:14.113\"}},{\"gameTemplateType\":\"Gameplay\",\"hasTutorials\":false,\"universe\":{\"id\":95830130,\"name\":\"Racing\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":215383192,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-02-12T18:36:49.8\",\"updated\":\"2019-04-04T11:15:46.39\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":true,\"universe\":{\"id\":107387509,\"name\":\"Pirate Island\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":264719325,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-07-01T17:54:38.927-05:00\",\"updated\":\"2019-04-05T09:54:25.077-05:00\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":false,\"universe\":{\"id\":138958533,\"name\":\"Western\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":366120910,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2016-02-19T17:47:34.103\",\"updated\":\"2019-04-05T04:32:47.2\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":false,\"universe\":{\"id\":93404984,\"name\":\"City\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":203783329,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-01-14T14:10:46.387-06:00\",\"updated\":\"2019-04-04T10:19:43.16-05:00\"}},{\"gameTemplateType\":\"Gameplay\",\"hasTutorials\":false,\"universe\":{\"id\":93412282,\"name\":\"Obby\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":203812057,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-01-14T15:51:25.83-06:00\",\"updated\":\"2019-04-05T04:27:08.9-05:00\"}},{\"gameTemplateType\":\"Theme\",\"hasTutorials\":true,\"universe\":{\"id\":142606178,\"name\":\"Starting Place\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":379736082,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2016-03-09T13:04:30.723-06:00\",\"updated\":\"2019-04-05T05:46:24.003-05:00\"}},{\"gameTemplateType\":\"Gameplay\",\"hasTutorials\":false,\"universe\":{\"id\":115791780,\"name\":\"Line Runner\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":301530843,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-09-28T17:16:52.42-05:00\",\"updated\":\"2019-04-04T19:28:06.833-05:00\"}},{\"gameTemplateType\":\"Gameplay\",\"hasTutorials\":false,\"universe\":{\"id\":37613887,\"name\":\"Capture The Flag\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":92721754,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2013-11-01T06:57:04.153-05:00\",\"updated\":\"2019-04-04T06:18:14.89-05:00\"}},{\"gameTemplateType\":\"Gameplay\",\"hasTutorials\":false,\"universe\":{\"id\":115791512,\"name\":\"Team/FFA Arena\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":301529772,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-09-28T17:14:08.107-05:00\",\"updated\":\"2019-11-19T14:09:27.037-06:00\"}},{\"gameTemplateType\":\"Gameplay\",\"hasTutorials\":false,\"universe\":{\"id\":93431584,\"name\":\"Combat\",\"description\":null,\"isArchived\":false,\"rootPlaceId\":203885589,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":998796,\"creatorName\":\"Templates\",\"created\":\"2015-01-14T19:21:33.01\",\"updated\":\"2019-11-19T14:09:18.557\"}}]}")
})

app.get("/IDE/Upload.aspx", (_, res) => {
    res.status(200).send("Uploading maps is not supported, since it's supposed to be local anyways")
})

app.get("/v1/user/groups/canmanage", (req, res) => {
    res.status(200).send("{\"data\":[]}")
})
app.get("//users/" + ":userid" + "/canmanage/" + ":uid", (req, res) => {
    res.status(200).send("{ \"Success\": true, \"CanManage\": true }")
})
app.get("/Marketplace/productinfo", (req, res) => {
    if (req.get("host") == "api.roblox.com" || ignoreUrl == true) {
        res.status(200).send("{\"TargetId\":" + req.query.assetid + ", \"ProductType\":\"User Product\", \"AssetId\":" + req.query.assetid + ", \"ProductId\":1,\"Name\":\"Stub Product Name\",\"Description\":\"\\\"nah nah, also about the icon, this yo god?\\\"\", \"AssetTypeId\":34, \"Creator\": {\"Id\":1, \"Name\":\"ROBLOX\", \"CreatorType\":\"User\",\"CreatorTargetId\":1}, \"IconImageAssetId\":12224170,\"Created\":\"2013-10-31T18:39:46.763Z\",\"Updated\":\"2016-10-02T01:06:11.017Z\",\"PriceInRobux\":100,\"PriceInTickets\":null, \"Sales\":2763, \"IsNew\":false,\"IsForSale\":true,\"IsPublicDomain\":false,\"IsLimited\":false,\"IsLimitedUnique\":false, \"Remaining\":null, \"MinimumMembershipLevel\":0, \"ContentRatingTypeId\":0}")
    }
})
app.get("/universes/get-universe-containing-place", (req, res) => {
    if (req.get("host") == "api.roblox.com" || ignoreUrl == true) {
        res.status(200).send("{\"UniverseId\":2}")
    }
})

app.get("/v1.1/game-start-info/", (req, res) => {
    if (req.get("host") == "api.roblox.com" || ignoreUrl == true) {
        res.status(200).send("{\"gameAvatarType\":\"PlayerChoice\",\"allowCustomAnimations\":\"True\",\"universeAvatarCollisionType\":\"OuterBox\",\"universeAvatarBodyType\":\"Standard\",\"jointPositioningType\":\"ArtistIntent\",\"message\":\"\",\"universeAvatarAssetOverrides\":[],\"moderationStatus\":null}")
    }
})
app.get("/studio/e.png", (req, res) => {
    res.status(200).send("")
})

app.get("/ide/welcome", (req, res) => {
    res.status(200).send("This studio experience is brought to you by [REDACTED]")
})
app.get("/Setting/QuietGet/ClientSharedSettings/",(req,res) => {
    res.status(200).send("{ \"StatsGatheringScriptUrl\": \"\", \"DFFlagLogAutoCamelCaseFixes\": \"True\" }")
})
app.get("/login/RequestAuth.ashx", (req, res) => {
    res.status(200).send("https://www.reblox.zip/Login/Negotiate.ashx")
})

app.get("/Login/Negotiate.ashx", (req, res) => {
    res.status(200).send("")
})

app.post("/Game/MachineConfiguration.ashx", (req, res) => {
    res.status(200).send("")
})

app.post("/game/validate-machine", (req, res) => {
    res.status(200).send("{ \"success\": true }")
})

app.get("/groups/can-manage-games", (req, res) => {
    res.status(200).send("[]")
})
app.get("/avatar-thumbnail/json", (req, res) => {
    res.status(200).send("{\"Url\":\"http://reblox.zip/Thumbs/Avatar.ashx\",\"Final\":true,\"SubstitutionType\":0}")
})
app.post("/login/Negotiate.ashx", (req, res) => {
    res.status(200).send("true")
})
app.get("/asset-thumbnail/json", (req, res) => {
    res.status(200).send("{\"Url\":\"http://assetgame.reblox.zip/gameicon.png\",\"Final\":false,\"SubstitutionType\":4}")
})
app.get("/UploadMedia/PostImage.aspx", (req, res) => {
    res.status(200).send("Caught you screenshotting ;)")
})
app.get("/gameicon.png", (req, res) => {
    res.setHeader("Content-disposition", "attachment; filename=\"gameicon.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/gameicon.png"))
})
app.get("/v1/user/studiodata", (req,res) => {
    //STUB
    res.status(200).send("{}")
})
app.get("/v1/universes/" + ":id", (req, res) => {
    res.status(200).send("{\"id\":" + req.params.id + ",\"name\":\"ReBlox Place\",\"description\"A ReBlox place launched from the launcher\",\"isArchived\":false,\"rootPlaceId\":1,\"isActive\":true,\"privacyType\":\"Public\",\"creatorType\":\"User\",\"creatorTargetId\":1,\"creatorName\":\"ROBLOX\",\"created\":\"2013-10-31T17:46:37.747Z\",\"updated\":\"2019-04-03T00:04:34.373Z\"}")
})

app.get("/My/Places.aspx", (req,res) => {
    //STUB
    res.status(200).send("This studio experience is brought to you by [REDACTED]")
})

app.get("/My/Places.aspx&version=" + ":version", (req,res) => {
    //STUB
    res.status(200).send("This studio experience is brought to you by [REDACTED]")
})

app.get("/IDE/ClientToolbox.aspx", (req, res) => {
    //STUB (if you wanna add actual toolbox, you can)
    res.status(200).send("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"></head><body><select><option value=\"images\">Decals</option><option value=\"default\" selected=\"selected\">Models</option><option value=\"setup\">ROBLOX Sets</option></select><input type=\"text\" id=\"searchtext\"><br><h2>*insert toolbox here*</h2></body></html>")
})

app.get("/Setting/QuietGet/ClientAppSettings/", (req, res) => {
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send(filesystem.readFileSync("./ClientAppSettings.json","utf-8").replaceAll("{id}", userId))
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/v2/settings/application/PCDesktopClient", (req, res) => {
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send("{\"applicationSettings\":" + filesystem.readFileSync("./ClientAppSettings.json","utf-8").replaceAll("{id}", userId) + "}")
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/v1/settings/application", (req, res) => {
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send("{\"applicationSettings\":" + filesystem.readFileSync("./ClientAppSettings.json","utf-8").replaceAll("{id}", userId) + "}")
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/v2/settings/application/PCStudioApp", (req, res) => {
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send("{\"applicationSettings\":" + filesystem.readFileSync("./ClientAppSettings.json","utf-8").replaceAll("{id}", userId) + "}")
    }
    else {
        res.status(200).send("{}")
    }
})

app.post("/v1/authentication-ticket/redeem", (req, res) => {
    console.log(req.body)
    res.status(200).send("")
})
app.get("/Setting/QuietGet/StudioAppSettings/", (req,res) => {
    if (filesystem.existsSync("./ClientAppSettings.json")) {
        res.status(200).send(filesystem.readFileSync("./ClientAppSettings.json", "utf-8").replaceAll("{id}", userId))
    }
    else {
        res.status(200).send("{}")
    }
})

app.get("/universes/" + ":id" + "/game-start-info", (req, res) => {
    res.status(200).send("{}")
})
app.get("/game/logout.aspx", (req, res) => {
    res.status(200).end()
})
app.get("/Game/JoinRate.ashx", (req, res) => {
    //STUB
    res.status(200).end()
})

app.get("/Game/ClientPresence.ashx", (req, res) => {
    //STUB
    res.status(200).end()
})

app.post("/Game/ClientPresence.ashx", (req, res) => {
    //STUB
    res.status(200).end()
})
app.post("/game/report-stats", (req, res) => {
    //STUB
    res.status(200).end()
})

app.post("/v1/user/studiodata", (req, res) => {
    //IDK how this work, so leaving it stub
    res.status(200).send("{}")
})
app.get("/asset-gameicon/multiget", (req, res) => {
    //STUB
    res.status(200).send("{}")
})

app.get("/v1/games/icons", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var options = {
        host: "thumbnails.roblox.com",
        port: 443,
        path: (req.get("host") + req.originalUrl).replaceAll("thumbnails.reblox.zip",""),
        method: "GET"
    }

    https.get(options, (res1) => {
        var jsondata = ""
        res1.setEncoding("utf8")
        res1.on("data", (chunk) => {
            jsondata += chunk
        })
        res1.on("end", () => {
            res.status(200).send(jsondata)
        })
    })
})
app.post("/persistence/set", async (req, res) => {
    console.log("\x1b[32m","<INFO> Adding a value to the datastore...")
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
                else if (line.includes("DataStoreName=\"" + req.query.key + "\"") && line.includes("Key=\"" + req.query.target + "\"")) {
                    replacementtext = line
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (replacementtext != "") {
                    filesystem.writeFileSync(RBDFpath, filesystem.readFileSync(RBDFpath, "utf-8").replace(replacementtext,"<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.query.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body.value + "\">"))
                }
                else {
                    filesystem.appendFileSync(RBDFpath, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.query.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body.value + "\">\n")
                }

            }
            else {
                res.status(500).end()
            }
        }
        else {
            filesystem.writeFileSync(RBDFpath,"RBDF==\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\n\n")
            filesystem.appendFileSync(RBDFpath, "<DataStore Key=\"" + req.query.target + "\" Scope=" + req.query.scope + " Type=" + req.query.type + " DataStoreName=\"" + req.query.key + "\" Value=\"" + req.body.value + "\">\n")
        }
        res.status(200).send("{\"data\":[{\"Key\": {\"Scope\":\"" + req.query.scope + "\", \"Target\": \"" + req.query.target + "\", \"Key\": \"" + req.query.key + "\"},\"Value\": \"" + req.body.value.slice(1).slice(0,-1).replaceAll("\\", "\\\\").replaceAll('"',"\\\"") + "\"}]}")
    }
    else {
        res.status(200).send("{\"data\":[]}")
    }
    }
})

app.post("/persistence/getV2", async (req, res) => {
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
                else if (line.includes("DataStoreName=\"" + req.body.qkeys[2] + "\"") && line.includes("Key=\"" + req.body.qkeys[1] + "\"")) {
                    replacementtext = line
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (replacementtext != "") {
                    var myRegExp= new RegExp('Value\=\".*\"')
                    var match = myRegExp.exec(replacementtext)
                    if (match != null) {
                        var value = match[0].slice(7).slice(0,-1)
                        res.status(200).send("{\"data\":[{\"Key\": {\"Scope\":\"" + req.query.scope + "\", \"Target\": \"" + req.body.qkeys[1] + "\", \"Key\": \"" + req.body.qkeys[2] + "\"},\"Value\": \"" + value.replaceAll("\\", "\\\\").replaceAll('"',"\\\"") + "\"}]}")
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
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (isFollowing == false) {
                    if (req.query.userId != undefined) {
                filesystem.appendFileSync(RBDFpath, "<Following userId=" + req.query.userId + " followerUserId=" + req.body.followedUserId +">\n")
            }
            else {
                filesystem.appendFileSync(RBDFpath, "<Following userId=" + userId + " followerUserId=" + req.body.followedUserId +">\n")
            }
        }
            }
            else {
                res.status(500).end()
                return
            }
        }
        else {
            filesystem.writeFileSync(RBDFpath,"RBDF==\n--This is a ReBlox Datastore File! This is important if you want to save your datastore/badges/followers!\n\n")
            if (req.query.userId != undefined) {
                filesystem.appendFileSync(RBDFpath, "<Following userId=" + req.query.userId + " followerUserId=" + req.body.followedUserId +">\n")
            }
            else {
                filesystem.appendFileSync(RBDFpath, "<Following userId=" + userId + " followerUserId=" + req.body.followedUserId +">\n")
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
                }
            }
            stream.destroy()
            rl.close()
            if (verified == true) {
                if (isFollowing == true) {
                    var filecontent = filesystem.readFileSync(RBDFpath, "utf8")
                    filesystem.unlinkSync(RBDFpath)
                    if (req.query.userId != undefined) {
                        filesystem.writeFileSync(RBDFpath, filecontent.replaceAll("<Following userId=" + req.query.userId + " followerUserId=" + req.body.followedUserId +">\n", ""))
                    }
                    else {
                        filesystem.writeFileSync(RBDFpath, filecontent.replaceAll("<Following userId=" + userId + " followerUserId=" + req.body.followedUserId +">\n", ""))
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
    res.status(200).send("[{\"Id\": " + req.params.id + ", \"AssetId\":0, \"VersionNumber\":1, \"RawContentId\":0, \"ParentAssetVersionId\": 0, \"CreatorType\":1, \"CreatorTargetId\": " + userId + ", \"CreatingUniverseId: null, \"Created\": \"2015-07-13T11:51:12.9073098-05:00\",\"Updated\": \"2015-07-13T11:51:12.9073098-05:00\"}]")
})
app.get("/user/get-friendship-count", (req,res) => {
    res.status(200).send("{\"success\": true, \"message\": \"Success\", \"count\": 0}")
})
app.post("/user/request-friendship", (req, res) => {
    res.status(200).send("{\"success\":true, \"message\":\"Success\"}")
})
app.post("/v1/batch", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    var unfinishedstring = "{ \"data\": ["
    for (var i = 0; i != Object.keys(req.body).length; i++) {
        if (req.body[i].type == "AvatarHeadShot") {
            if (i + 1 == Object.keys(req.body).length) {
                unfinishedstring = unfinishedstring + " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://reblox.zip/Thumbs/HeadShot.ashx\", \"version\": \"TN3\" }"
            }
            else {
                unfinishedstring = unfinishedstring + " {\"requestId\": \"" + req.body[i].requestId + "\", \"errorCode\": 0, \"errorMessage\": \"\", \"targetId\": " + req.body[i].targetId + ", \"state\": \"Completed\", \"imageUrl\": \"http://reblox.zip/Thumbs/HeadShot.ashx\", \"version\": \"TN3\" },"
            }
        }
    }
    res.status(200).send(unfinishedstring + " ] }")
})

app.get("/headshot-thumbnail/image", (req, res) => {
    res.setHeader("Cache-Control", "no-cache")
    res.setHeader("Content-disposition", "attachment; filename=\"headshot.png\"")
    res.status(200).send(filesystem.readFileSync(assetfolder + "/headshot.png"))
})

app.get("/v1/locales", (req,res) => {
    res.status(200).send("{\"data\": [{\"locale\":{\"id\":1, \"locale\":\"en_us\", \"name\": \"English (US)\", \"nativeName\":\"English\",\"language\":{\"id\":41,\"name\":\"English\",\"nativeName\":\"English\",\"languageCode\":\"en\"}},\"isEnabledForFullExperience\":true,\"IsEnabledForSignupAndLogin\":true,\"IsEnabledForInGameUgc\":true}]}")
})

app.get("/v2/users/" + ":userid" + "/groups/roles", (req, res) => {
    res.status(200).send("{\"data\":[]}")
})
app.use((req, res)=>{
  res.status(404).end();
  console.log("\x1b[33m", "<WARN> NOT IMPLEMENTED: \"" + req.protocol + "://" + req.get("host") + req.originalUrl + "\" (" + req.method + ")")
  if (req.method == "POST") {
    console.log("\x1b[33m", "<WARN> Request Body:\n" + toString(req.body))
  }
});

https.createServer(options, app).listen(443, function (req, res) {
    console.log("\x1b[32m","<INFO> Started a HTTPS server at port 443")
})

app.listen(80, function (req, res) {
    console.log("\x1b[32m","<INFO> Started a HTTP server at port 80")
})