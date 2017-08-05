function createDate() {
    var context = getContext();
    var request = context.getRequest();

    // document to be created in the current operation
    var documentToCreate = request.getBody();

    // validate properties
    //if (!("dateCreated" in documentToCreate)) {
        var date = new Date();
        documentToCreate.dateCreated = date.toUTCString();
    //}

    // update the document that will be created
    request.setBody(documentToCreate);
}