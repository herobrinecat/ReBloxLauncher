var root = location.protocol + "//" + location.host + "/";

function insertContent(assetId) {
    try {
        window.external.Insert(root + "asset/?id=" + encodeURIComponent(assetId))
    } catch (e) {
        alert("Something went wrong while trying to insert this content!")
    }
}