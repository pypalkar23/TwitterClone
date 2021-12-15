open System
open Akka.FSharp
open FSharp.Json
open Akka.Actor
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Logging
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Constants
open Util


let system = ActorSystem.Create("TwitterCloneServer")
let mutable followersStore = Map.empty
let mutable hashTagsStore = Map.empty
let mutable mentionsStore = Map.empty
let mutable userStore = Map.empty
type FeedMessages = 
  | Register of (string)
  | SubscribeTo of (string*string)
  | MarkActive of (string*WebSocket)
  | MarkInactive of (string)
  | BroadcastToFeeds of (string*string*string)

let msgSender = MailboxProcessor<string*WebSocket>.Start(fun inbox ->
  let rec loop() = async {
    let! msg,webSocket = inbox.Receive()
    let byteRes = toByteSegment msg
    let! _ = webSocket.send Text byteRes true
    return! loop()
  }
  loop()
)

let FeedManager (mailbox:Actor<_>) = 
  let mutable followerMap = Map.empty
  let mutable activeUsersMap = Map.empty
  let mutable feedMap = Map.empty
  let rec loop () = actor {
      let! message = mailbox.Receive() 
      match message with
      | Register(userId) ->
        followerMap <- Map.add userId Set.empty followerMap
        feedMap <- Map.add userId List.empty feedMap
      | SubscribeTo(userId, followerId) ->
        if followerMap.ContainsKey followerId then
          let mutable followSet = Set.empty
          followSet <- followerMap.[followerId]
          followSet <- Set.add userId followSet
          followerMap <- Map.remove followerId followerMap 
          followerMap <- Map.add followerId followSet followerMap
          //let mutable jsonData: ResponseType = 
            //{userID = followerId; service= "Follow"; code = "OK"; message = sprintf "User %s started following you!" userId}
          let respData = prepareResponse (followerId, sprintf "follow|%s|%s|User %s has started following you!" userId (DateTime.Now.ToString()) userId , SERVICE_TYPE_FOLLOW, false)
          let mutable respJson = Json.serialize respData
          msgSender.Post (respJson,activeUsersMap.[followerId])
      | BroadcastToFeeds(userId,tweetMsg,respType) ->
        if followerMap.ContainsKey userId then
          let mutable partialUpdate = ""
          
          if respType = SERVICE_TYPE_TWEET then
            partialUpdate <- sprintf "tweet|%s|%s" userId (DateTime.Now.ToString())
          
          else 
            partialUpdate <- sprintf "retweet|%s|%s" userId (DateTime.Now.ToString())

          for followerId in followerMap.[userId] do 
            if followerMap.ContainsKey followerId then
              if activeUsersMap.ContainsKey followerId then
                let twt = sprintf "%s|%s" partialUpdate tweetMsg
                //let jsonData: ResponseType = {userID = followerId; service=respType; code="OK"; message = twt}
                let respData = prepareResponse(followerId, twt, respType, false)
                let respJson = Json.serialize respData
                msgSender.Post (respJson, activeUsersMap.[followerId])
              let mutable listy = []
              if feedMap.ContainsKey followerId then
                  listy <- feedMap.[followerId]
              listy  <- (sprintf "%s|%s" partialUpdate tweetMsg) :: listy
              feedMap <- Map.remove followerId feedMap
              feedMap <- Map.add followerId listy feedMap

      | MarkActive(userId,userWebSkt) ->
        if activeUsersMap.ContainsKey userId then  
          activeUsersMap <- Map.remove userId activeUsersMap
        activeUsersMap <- Map.add userId userWebSkt activeUsersMap
        let mutable userFeed = ""
        let mutable responseType = ""
        
        if feedMap.ContainsKey userId then
          let mutable feedHead = ""
          let mutable maxFeedSize = 10
          let feedList:List<string> = feedMap.[userId]
          
          if feedList.Length = 0 then
            responseType <- SERVICE_TYPE_FOLLOW
            userFeed <- sprintf "No feeds yet!!"
          
          else
            if feedList.Length < 10 then
                maxFeedSize <- feedList.Length
            
            for i in [0..(maxFeedSize-1)] do
              feedHead <- "-" + feedMap.[userId].[i] + feedHead
            userFeed<- feedHead
            responseType <- SERVICE_TYPE_LIVEFEED

          // let jsonData: ResponseType = {userID = userId; message = userFeed; code = "OK"; service=responseType}
          let respData = prepareResponse(userId, userFeed, responseType, false)
          let respJson = Json.serialize respData
          msgSender.Post (respJson,userWebSkt) 

      | MarkInactive(userId) ->
        if activeUsersMap.ContainsKey userId then  
          activeUsersMap <- Map.remove userId activeUsersMap
      return! loop()
  }
  loop()

let feedmanager = spawn system (sprintf "FeedManager") FeedManager

let webSocketHandler (webSocket : WebSocket) (context: HttpContext) =
  let rec loop() =
    let mutable user = ""
    socket { 
      let! msg = webSocket.read()
      match msg with
      | (Text, data, true) ->
        let reqMsg = UTF8.toString data
        let processedMsg = processedString reqMsg
        let jsonObj = Json.deserialize<TwitterRequest> processedMsg
        user <- jsonObj.userID
        feedmanager <! MarkActive(user, webSocket)
        return! loop()
      
      | (Close, _, _) ->
        printfn "Closing Websocket"
        feedmanager <! MarkInactive(user)
        let emptyResponse = [||] |> ByteSegment
        do! webSocket.send Close emptyResponse true
      
      | _ -> return! loop()
    }
  loop()

let registerFn reqData =
  let mutable respStr = ""
  let userId =  reqData.userID
  let value = reqData.value
  //printf "register %s" userId
  if userStore.ContainsKey userId then
    //let respObj: ResponseType = {userID = userId; message = sprintf "User %s has already registred. Please go to login window" userId; service = "Register"; code = "FAIL"}
    let respObj= prepareResponse (userId,"User with this name exists in the system. Please go to login window",SERVICE_TYPE_REGISTER,true) 
    respStr <- Json.serialize respObj
  
  else
    userStore <- Map.add userId reqData.value userStore
    followersStore <- Map.add userId Set.empty followersStore
    feedmanager <! Register(userId)
    let respObj= prepareResponse (userId,"Registeration Successful. Please go to login window",SERVICE_TYPE_REGISTER,false)
    //let respObj: ResponseType = {userID = userId; message = sprintf "User %s has registered successfully" userId; service = "Register"; code = "OK"}
    respStr <- Json.serialize respObj
  respStr

let loginFn reqData =
  let mutable respStr = ""
  let userId =  reqData.userID
  let password = reqData.value
  if userStore.ContainsKey userId then
    
    if userStore.[userId] = password then
      //let respObj: ResponseType = {userID = userId; message = sprintf "User %s logged in successfully" userId; service = "Login"; code = "OK"}
      let respObj= prepareResponse (userId,"Login Successful",SERVICE_TYPE_LOGIN,false)
      respStr <- Json.serialize respObj
    
    else 
      let respObj= prepareResponse (userId,"Invalid Username or Password",SERVICE_TYPE_LOGIN,true)
      //let respObj: ResponseType = {userID = userId; message = "Invalid userid / password"; service = "Login"; code = "FAIL"}
      respStr <- Json.serialize respObj
  
  else
    let respObj= prepareResponse (userId,"Invalid Username or Password",SERVICE_TYPE_LOGIN,true)
    //let respObj: ResponseType = {userID = userId; message = "Invalid userid / password"; service = "Login"; code = "FAIL"}
    respStr <- Json.serialize respObj
  respStr

let followFn reqData =
  let mutable respStr = ""
  let userId =  reqData.userID
  let otheruser = reqData.value
  if otheruser <> userId then
    
    if followersStore.ContainsKey otheruser then
      
      if not (followersStore.[otheruser].Contains userId) then
        let mutable tempset = followersStore.[otheruser]
        tempset <- Set.add userId tempset
        followersStore <- Map.remove otheruser followersStore
        followersStore <- Map.add otheruser tempset followersStore
        feedmanager <! SubscribeTo(userId,otheruser) 
        //let respObj: ResponseType = {userID = userId; service="Follow"; message = sprintf "You started following %s!" otheruser; code = "OK"}
        let respObj= prepareResponse (userId ,sprintf "You started following %s!" otheruser, SERVICE_TYPE_FOLLOW ,false)
        respStr <- Json.serialize respObj
      
      else 
        //let respObj: ResponseType = {userID = userId; service="Follow"; message = sprintf "You are already following %s!" otheruser; code = "FAIL"}
        let respObj= prepareResponse (userId ,sprintf "You are already following %s!" otheruser, SERVICE_TYPE_FOLLOW ,true)
        respStr <- Json.serialize respObj      
    
    else
        let respObj= prepareResponse (userId ,sprintf "Invalid request, No such user (%s)." otheruser, SERVICE_TYPE_FOLLOW ,true)  
        //let respObj: ResponseType = {userID = userId; service="Follow"; message = sprintf "Invalid request, No such user (%s)." otheruser; code = "FAIL"}
        respStr <- Json.serialize respObj
  
  else
     let respObj= prepareResponse (userId ,"You cannot follow yourself.", SERVICE_TYPE_FOLLOW ,true)
    //let respObj: ResponseType = {userID = userId; service="Follow"; message = sprintf "You cannot follow yourself."; code = "FAIL"}
     respStr <- Json.serialize respObj   
  respStr
  
let tweetFn reqData =
  let mutable resp = ""
  let userId = reqData.userID
  let tweetTxt = reqData.value
  
  if userStore.ContainsKey userId then
    let mutable hashTag = ""
    let mutable mentionedUser = ""
    let parsed = tweetTxt.Split ' '
    // printfn "parsed = %A" parsed
    
    for parse in parsed do
      if parse.Length > 0 then
        if parse.[0] = '#' then
          hashTag <- parse.[1..(parse.Length-1)]
        else if parse.[0] = '@' then
          mentionedUser <- parse.[1..(parse.Length-1)]

    if mentionedUser <> "" then
      if userStore.ContainsKey mentionedUser then
        if not (mentionsStore.ContainsKey mentionedUser) then
            mentionsStore <- Map.add mentionedUser List.empty mentionsStore
        let mutable mList = mentionsStore.[mentionedUser]
        mList <- (sprintf "tweet|%s|%s|%s" userId (DateTime.Now.ToString()) tweetTxt) :: mList
        mentionsStore <- Map.remove mentionedUser mentionsStore
        mentionsStore <- Map.add mentionedUser mList mentionsStore
        feedmanager <! BroadcastToFeeds(userId,tweetTxt,SERVICE_TYPE_TWEET)
        let respObj= prepareResponse (userId ,sprintf "%s tweeted: %s" userId tweetTxt, SERVICE_TYPE_TWEET ,false)
        // let respObj: ResponseType = {userID = userInput.userID; service="Tweet"; message = (sprintf "%s tweeted: %s" userInput.userID userInput.value); code = "OK"}
        resp <- Json.serialize respObj
      
      else
        let respObj= prepareResponse (userId ,sprintf "Invalid request, mentioned user '%s' is not registered" mentionedUser, SERVICE_TYPE_TWEET ,true)
        //let respObj: ResponseType = {userID = userId; service="Tweet"; message = sprintf "Invalid request, mentioned user (%s) is not registered" mentionedUser; code = "FAIL"}
        resp <- Json.serialize respObj
    
    else
      feedmanager <! BroadcastToFeeds(userId,tweetTxt,SERVICE_TYPE_TWEET)
      let respObj = prepareResponse (userId ,sprintf "tweet|%s|%s|%s" userId (DateTime.Now.ToString()) tweetTxt, SERVICE_TYPE_TWEET ,false)
      // let respObj: ResponseType = {userID = userId; service="Tweet"; message = (sprintf "%s tweeted: %s" userId tweetTxt); code = "OK"}
      resp <- Json.serialize respObj

    if hashTag <> "" then
      if not (hashTagsStore.ContainsKey hashTag) then
        hashTagsStore <- Map.add hashTag List.empty hashTagsStore
      let mutable tList = hashTagsStore.[hashTag]
      tList <- (sprintf "tweet|%s|%s|%s" userId (DateTime.Now.ToString()) tweetTxt) :: tList
      hashTagsStore <- Map.remove hashTag hashTagsStore
      hashTagsStore <- Map.add hashTag tList hashTagsStore
  
  else
    let respObj = prepareResponse (userId ,sprintf "Invalid request - user %s does not exist!" userId, SERVICE_TYPE_TWEET ,true)  
    //let respObj: ResponseType = {userID = userId; service="Tweet"; message = sprintf "Invalid request by user %s, Not registered yet!" userId; code = "FAIL"}
    resp <- Json.serialize respObj
  resp

let retweetFn userInput =
  let mutable resp = ""
  let userId = userInput.userID;
  let otherUser = userInput.value;
  
  if userStore.ContainsKey userInput.userID then
    feedmanager <! BroadcastToFeeds(userId, otherUser ,SERVICE_TYPE_RETWEET)
    //let respObj: ResponseType = {userID = userInput.userID; service="ReTweet"; message = (sprintf "%s re-tweeted: %s" userInput.userID userInput.value); code = "OK"}
    let respObj = prepareResponse (userId ,sprintf "%s has retweeted: %s" userId otherUser, SERVICE_TYPE_RETWEET ,false)
    resp <- Json.serialize respObj
  
  else  
    //let respObj: ResponseType = {userID = userInput.userID; service="ReTweet"; message = sprintf "Invalid request by user %s, Not registered yet!" userInput.userID; code = "FAIL"}
    let respObj = prepareResponse (userId ,sprintf "Invalid request - user %s does not exist!" userInput.userID, SERVICE_TYPE_RETWEET ,true)
    resp <- Json.serialize respObj
  resp

let query (userInput:string) = 
  let mutable tagsstring = ""
  let mutable mentionsString = ""
  let mutable resp = ""
  let mutable maxMentionsSize = 10
  let hashTagStart = '@'
  if userInput.Length > 0 then
    
    if userInput.[0] = hashTagStart then
      let searchKey = userInput.[1..(userInput.Length-1)]
      
      if mentionsStore.ContainsKey searchKey then
        let mentionsList:List<string> = mentionsStore.[searchKey]
        
        if (mentionsList.Length < 10) then
          maxMentionsSize <- mentionsList.Length
        
        for i in [0..(maxMentionsSize-1)] do
          mentionsString <- mentionsString + "-" + mentionsList.[i]
        // let respObj: ResponseType = {userID = ""; service="Query"; message = mentionsString; code = "OK"}
        let respObj = prepareResponse ("" ,mentionsString, SERVICE_TYPE_QUERY ,false)
        resp <- Json.serialize respObj
      
      else 
        //let respObj: ResponseType = {userID = ""; service="Query"; message = "-No tweets found for the mentioned user"; code = "OK"}
        let respObj = prepareResponse ("" ,"No tweets exist with this user mentioned", SERVICE_TYPE_QUERY ,false)
        resp <- Json.serialize respObj
    
    else
      let queryParam = userInput
      
      if hashTagsStore.ContainsKey queryParam then
        let mapData:List<string> = hashTagsStore.[queryParam]
        
        if (mapData.Length < 10) then
            maxMentionsSize <- mapData.Length
        
        for i in [0..(maxMentionsSize-1)] do
            tagsstring <- tagsstring + "-" + mapData.[i]
        // let respObj: ResponseType = {userID = ""; service="Query"; message = tagsstring; code = "OK"}
        let respObj = prepareResponse ("" ,tagsstring, SERVICE_TYPE_QUERY ,false)
        resp <- Json.serialize respObj
      
      else 
        let respObj = prepareResponse ("" ,"No tweets exist with this hashtag", SERVICE_TYPE_QUERY ,false)
        //let respObj: ResponseType = {userID = ""; service="Query"; message = "-No tweets found for the hashtag"; code = "OK"}
        resp <- Json.serialize respObj
  else
    let respObj = prepareResponse ("" ,"Empty String Received For Search Query", SERVICE_TYPE_QUERY ,true)
    //let respObj: ResponseType = {userID = ""; service="Query"; message = "Type something to search"; code = "FAIL"}
    resp <- Json.serialize respObj
  resp


let searchFn reqData = 
  let mutable tagsstring = ""
  let mutable mentionsString = ""
  let mutable resp = ""
  let mutable maxMentionsSize = 10
  let hashTagStart = '@'
  let searchQuery = reqData.value
  printfn "%s" searchQuery
  if searchQuery.Length > 0 then
    
    if searchQuery.[0] = hashTagStart then
      let searchKey = searchQuery.[1..(searchQuery.Length-1)]
      
      if mentionsStore.ContainsKey searchKey then
        let mentionsList:List<string> = mentionsStore.[searchKey]
        
        if (mentionsList.Length < 10) then
          maxMentionsSize <- mentionsList.Length
        
        for i in [0..(maxMentionsSize-1)] do
          mentionsString <- mentionsString + "-" + mentionsList.[i]
        // let respObj: ResponseType = {userID = ""; service="Query"; message = mentionsString; code = "OK"}
        let respObj = prepareResponse ("" ,mentionsString, SERVICE_TYPE_QUERY ,false)
        resp <- Json.serialize respObj
      
      else 
        //let respObj: ResponseType = {userID = ""; service="Query"; message = "-No tweets found for the mentioned user"; code = "OK"}
        let respObj = prepareResponse ("" ,"No tweets exist with this user mentioned", SERVICE_TYPE_QUERY ,false)
        resp <- Json.serialize respObj
    
    else
      let queryParam = searchQuery
      
      if hashTagsStore.ContainsKey queryParam then
        let mapData:List<string> = hashTagsStore.[queryParam]
        
        if (mapData.Length < 10) then
            maxMentionsSize <- mapData.Length
        
        for i in [0..(maxMentionsSize-1)] do
            tagsstring <- tagsstring + "-" + mapData.[i]
        // let respObj: ResponseType = {userID = ""; service="Query"; message = tagsstring; code = "OK"}
        let respObj = prepareResponse ("" ,tagsstring, SERVICE_TYPE_QUERY ,false)
        resp <- Json.serialize respObj
      
      else 
        let respObj = prepareResponse ("" ,"No tweets exist with this hashtag", SERVICE_TYPE_QUERY ,false)
        //let respObj: ResponseType = {userID = ""; service="Query"; message = "-No tweets found for the hashtag"; code = "OK"}
        resp <- Json.serialize respObj
  else
    let respObj = prepareResponse ("" ,"Empty String Received For Search Query", SERVICE_TYPE_QUERY ,true)
    //let respObj: ResponseType = {userID = ""; service="Query"; message = "Type something to search"; code = "FAIL"}
    resp <- Json.serialize respObj
  printf "%s" resp
  resp


let RegisterHandler = preparePostEntryFor REGISTER_ENDPOINT{
  Entry = registerFn
}

let LoginHandler = preparePostEntryFor LOGIN_ENDPOINT {
  Entry = loginFn
}

let FollowHandler = preparePostEntryFor FOLLOW_ENDPOINT {
  Entry = followFn
}

let TweetHandler = preparePostEntryFor TWEET_ENDPOINT {
  Entry = tweetFn
}

let ReTweetHandler = preparePostEntryFor RETWEET_ENDPOINT {
  Entry = retweetFn
}

let searchTwitterHandler = preparePostEntryFor QUERY_ENDPOINT{
    Entry= searchFn
}


//Handles the prefight requests
let allow_cors : WebPart =
    choose [
        OPTIONS >=>
            fun context ->
                context |> (
                    setCORSHeaders
                    >=> OK "CORS Request Allowed" )

    ]

let ws = 
  choose [
    allow_cors
    path "/feed" >=> handShake webSocketHandler
    RegisterHandler
    LoginHandler
    FollowHandler
    TweetHandler
    ReTweetHandler
    searchTwitterHandler
  ]

[<EntryPoint>]
let main _ =
  startWebServer { defaultConfig with logger = Targets.create Verbose [||] } ws
  0