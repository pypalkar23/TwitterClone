open System
open Akka.FSharp
open FSharp.Json
open Akka.Actor
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Writers
open Suave.Successful
open Suave.Logging
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Util

let system = ActorSystem.Create("TwitterServer")
let mutable hashTagsMap = Map.empty
let mutable mentionsMap = Map.empty
let mutable registeredUsers = Map.empty
let mutable globalfollowers = Map.empty

let initialCapacity = 101
let numProcs = Environment.ProcessorCount

type FeedMessages = 
  | Register of (string)
  | Subscribers of (string*string)
  | MarkActive of (string*WebSocket)
  | MarkInactive of (string)
  | UpdateFeeds of (string*string*string)

type TweetMessages = 
  | InitTweet of (IActorRef)
  | AddRegisteredUser of (string)
  | TweetRequest of (string*string*string)



let agent = MailboxProcessor<string*WebSocket>.Start(fun inbox ->
  let rec messageLoop() = async {
    let! msg,webSkt = inbox.Receive()
    let byteRes =
      msg
      |> System.Text.Encoding.ASCII.GetBytes
      |> ByteSegment
    let! _ = webSkt.send Text byteRes true
    return! messageLoop()
  }
  messageLoop()
)

let FeedActor (mailbox:Actor<_>) = 
  let mutable followers = Map.empty
  let mutable activeUsers = Map.empty
  let mutable feedtable = Map.empty
  let rec loop () = actor {
      let! message = mailbox.Receive() 
      match message with
      | Register(userId) ->
        followers <- Map.add userId Set.empty followers
        feedtable <- Map.add userId List.empty feedtable
        // printfn "Followers Map: %A" followers
      | Subscribers(userId, followerId) ->
        if followers.ContainsKey followerId then
          let mutable followSet = Set.empty
          followSet <- followers.[followerId]
          followSet <- Set.add userId followSet
          followers <- Map.remove followerId followers 
          followers <- Map.add followerId followSet followers
          // printfn "User %s started following you! for %s" userId followerId
          let mutable jsonData: ResponseType = 
            {userID = followerId; service= "Follow"; code = "OK"; message = sprintf "User %s started following you!" userId}
          let mutable consJson = Json.serialize jsonData
          // printfn "consjaosn = %A" consJson
          agent.Post (consJson,activeUsers.[followerId])
      | MarkActive(userId,userWebSkt) ->
        if activeUsers.ContainsKey userId then  
          activeUsers <- Map.remove userId activeUsers
        activeUsers <- Map.add userId userWebSkt activeUsers 
        let mutable feedsPub = ""
        let mutable sertype = ""
        if feedtable.ContainsKey userId then
          let mutable feedsTop = ""
          let mutable fSize = 10
          let feedList:List<string> = feedtable.[userId]
          if feedList.Length = 0 then
            sertype <- "Follow"
            feedsPub <- sprintf "No feeds yet!!"
          else
            if feedList.Length < 10 then
                fSize <- feedList.Length
            // printfn"fsize = %d" fSize
            for i in [0..(fSize-1)] do
              // printfn "feed %d = %s" i feedtable.[userId].[i]
              feedsTop <- "-" + feedtable.[userId].[i] + feedsTop

            feedsPub <- feedsTop
            sertype <- "LiveFeed"
          // printfn "feeds pub = %A" feedsPub
          let jsonData: ResponseType = {userID = userId; message = feedsPub; code = "OK"; service=sertype}
          let consJson = Json.serialize jsonData
          agent.Post (consJson,userWebSkt) 
      | MarkInactive(userId) ->
        if activeUsers.ContainsKey userId then  
          activeUsers <- Map.remove userId activeUsers
      | UpdateFeeds(userId,tweetMsg,sertype) ->
        if followers.ContainsKey userId then
          let mutable stype = ""
          if sertype = "Tweet" then
            stype <- sprintf "%s tweeted:" userId
          else 
            stype <- sprintf "%s re-tweeted:" userId
          for foll in followers.[userId] do 
            if followers.ContainsKey foll then
              if activeUsers.ContainsKey foll then
                let twt = sprintf "%s^%s" stype tweetMsg
                let jsonData: ResponseType = {userID = foll; service=sertype; code="OK"; message = twt}
                let consJson = Json.serialize jsonData
                agent.Post (consJson,activeUsers.[foll])
              let mutable listy = []
              if feedtable.ContainsKey foll then
                  listy <- feedtable.[foll]
              listy  <- (sprintf "%s^%s" stype tweetMsg) :: listy
              feedtable <- Map.remove foll feedtable
              feedtable <- Map.add foll listy feedtable
      return! loop()
  }
  loop()

let feedActor = spawn system (sprintf "FeedActor") FeedActor

let liveFeed (webSocket : WebSocket) (context: HttpContext) =
  let rec loop() =
    let mutable presentUser = ""
    socket { 
      let! msg = webSocket.read()
      match msg with
      | (Text, data, true) ->
        let reqMsg = UTF8.toString data
        printfn "%s" reqMsg
        let processedMsg = processedString reqMsg
        printfn "%s" processedMsg
        let parsed = Json.deserialize<RequestType> processedMsg
        printfn "%s" parsed.userID
        presentUser <- parsed.userID
        feedActor <! MarkActive(parsed.userID, webSocket)
        return! loop()
      | (Close, _, _) ->
        printfn "Closed WEBSOCKET"
        feedActor <! MarkInactive(presentUser)
        let emptyResponse = [||] |> ByteSegment
        do! webSocket.send Close emptyResponse true
      | _ -> return! loop()
    }
  loop()

let regNewUser userInput =
  let mutable resp = ""
  printf "register %s" userInput.userID
  if registeredUsers.ContainsKey userInput.userID then
    let rectype: ResponseType = {userID = userInput.userID; message = sprintf "User %s already registred" userInput.userID; service = "Register"; code = "FAIL"}
    resp <- Json.serialize rectype
  else
    registeredUsers <- Map.add userInput.userID userInput.value registeredUsers
    globalfollowers <- Map.add userInput.userID Set.empty globalfollowers
    feedActor <! Register(userInput.userID)
    let rectype: ResponseType = {userID = userInput.userID; message = sprintf "User %s registred successfully" userInput.userID; service = "Register"; code = "OK"}
    resp <- Json.serialize rectype
  resp

let loginUser userInput =
  let mutable resp = ""
  if registeredUsers.ContainsKey userInput.userID then
    if registeredUsers.[userInput.userID] = userInput.value then
      let rectype: ResponseType = {userID = userInput.userID; message = sprintf "User %s logged in successfully" userInput.userID; service = "Login"; code = "OK"}
      resp <- Json.serialize rectype
    else 
      let rectype: ResponseType = {userID = userInput.userID; message = "Invalid userid / password"; service = "Login"; code = "FAIL"}
      resp <- Json.serialize rectype
  else
    let rectype: ResponseType = {userID = userInput.userID; message = "Invalid userid / password"; service = "Login"; code = "FAIL"}
    resp <- Json.serialize rectype
  resp

let followUser userInput =
  let mutable resp = ""
  if userInput.value <> userInput.userID then
    if globalfollowers.ContainsKey userInput.value then
      if not (globalfollowers.[userInput.value].Contains userInput.userID) then
        let mutable tempset = globalfollowers.[userInput.value]
        tempset <- Set.add userInput.userID tempset
        globalfollowers <- Map.remove userInput.value globalfollowers
        globalfollowers <- Map.add userInput.value tempset globalfollowers
        feedActor <! Subscribers(userInput.userID,userInput.value) 
        let rectype: ResponseType = {userID = userInput.userID; service="Follow"; message = sprintf "You started following %s!" userInput.value; code = "OK"}
        resp <- Json.serialize rectype
      else 
        let rectype: ResponseType = {userID = userInput.userID; service="Follow"; message = sprintf "You are already following %s!" userInput.value; code = "FAIL"}
        resp <- Json.serialize rectype      
    else  
      let rectype: ResponseType = {userID = userInput.userID; service="Follow"; message = sprintf "Invalid request, No such user (%s)." userInput.value; code = "FAIL"}
      resp <- Json.serialize rectype
  else
    let rectype: ResponseType = {userID = userInput.userID; service="Follow"; message = sprintf "You cannot follow yourself."; code = "FAIL"}
    resp <- Json.serialize rectype   
  // printfn "follow response: %s" resp
  resp
  
let tweetUser userInput =
  let mutable resp = ""
  
  if registeredUsers.ContainsKey userInput.userID then
    let mutable hashTag = ""
    let mutable mentionedUser = ""
    let parsed = userInput.value.Split ' '
    // printfn "parsed = %A" parsed
    for parse in parsed do
      if parse.Length > 0 then
        if parse.[0] = '#' then
          hashTag <- parse.[1..(parse.Length-1)]
        else if parse.[0] = '@' then
          mentionedUser <- parse.[1..(parse.Length-1)]

    if mentionedUser <> "" then
      if registeredUsers.ContainsKey mentionedUser then
        if not (mentionsMap.ContainsKey mentionedUser) then
            mentionsMap <- Map.add mentionedUser List.empty mentionsMap
        let mutable mList = mentionsMap.[mentionedUser]
        mList <- (sprintf "%s tweeted:^%s" userInput.userID userInput.value) :: mList
        mentionsMap <- Map.remove mentionedUser mentionsMap
        mentionsMap <- Map.add mentionedUser mList mentionsMap
        feedActor <! UpdateFeeds(userInput.userID,userInput.value,"Tweet")
        let rectype: ResponseType = {userID = userInput.userID; service="Tweet"; message = (sprintf "%s tweeted:^%s" userInput.userID userInput.value); code = "OK"}
        resp <- Json.serialize rectype
      else
        let rectype: ResponseType = {userID = userInput.userID; service="Tweet"; message = sprintf "Invalid request, mentioned user (%s) is not registered" mentionedUser; code = "FAIL"}
        resp <- Json.serialize rectype
    else
      feedActor <! UpdateFeeds(userInput.userID,userInput.value,"Tweet")
      let rectype: ResponseType = {userID = userInput.userID; service="Tweet"; message = (sprintf "%s tweeted:^%s" userInput.userID userInput.value); code = "OK"}
      resp <- Json.serialize rectype

    if hashTag <> "" then
      if not (hashTagsMap.ContainsKey hashTag) then
        hashTagsMap <- Map.add hashTag List.empty hashTagsMap
      let mutable tList = hashTagsMap.[hashTag]
      tList <- (sprintf "%s tweeted:^%s" userInput.userID userInput.value) :: tList
      hashTagsMap <- Map.remove hashTag hashTagsMap
      hashTagsMap <- Map.add hashTag tList hashTagsMap
  else  
    let rectype: ResponseType = {userID = userInput.userID; service="Tweet"; message = sprintf "Invalid request by user %s, Not registered yet!" userInput.userID; code = "FAIL"}
    resp <- Json.serialize rectype
  resp

let retweetUser userInput =
  let mutable resp = ""
  if registeredUsers.ContainsKey userInput.userID then
    feedActor <! UpdateFeeds(userInput.userID,userInput.value,"ReTweet")
    let rectype: ResponseType = {userID = userInput.userID; service="ReTweet"; message = (sprintf "%s re-tweeted:^%s" userInput.userID userInput.value); code = "OK"}
    resp <- Json.serialize rectype
  else  
    let rectype: ResponseType = {userID = userInput.userID; service="ReTweet"; message = sprintf "Invalid request by user %s, Not registered yet!" userInput.userID; code = "FAIL"}
    resp <- Json.serialize rectype
  resp

let query (userInput:string) = 
  let mutable tagsstring = ""
  let mutable mentionsString = ""
  let mutable resp = ""
  let mutable size = 10
  if userInput.Length > 0 then
    if userInput.[0] = '@' then
      let searchKey = userInput.[1..(userInput.Length-1)]
      if mentionsMap.ContainsKey searchKey then
        let mapData:List<string> = mentionsMap.[searchKey]
        if (mapData.Length < 10) then
          size <- mapData.Length
        for i in [0..(size-1)] do
          mentionsString <- mentionsString + "-" + mapData.[i]
        let rectype: ResponseType = {userID = ""; service="Query"; message = mentionsString; code = "OK"}
        resp <- Json.serialize rectype
      else 
        let rectype: ResponseType = {userID = ""; service="Query"; message = "-No tweets found for the mentioned user"; code = "OK"}
        resp <- Json.serialize rectype
    else
      let searchKey = userInput
      if hashTagsMap.ContainsKey searchKey then
        let mapData:List<string> = hashTagsMap.[searchKey]
        if (mapData.Length < 10) then
            size <- mapData.Length
        for i in [0..(size-1)] do
            tagsstring <- tagsstring + "-" + mapData.[i]
        let rectype: ResponseType = {userID = ""; service="Query"; message = tagsstring; code = "OK"}
        resp <- Json.serialize rectype
      else 
        let rectype: ResponseType = {userID = ""; service="Query"; message = "-No tweets found for the hashtag"; code = "OK"}
        resp <- Json.serialize rectype
  else
    let rectype: ResponseType = {userID = ""; service="Query"; message = "Type something to search"; code = "FAIL"}
    resp <- Json.serialize rectype
  resp



let RegisterNewUserPoint = entryRequest "register" {
  Entry = regNewUser
}

let LoginUserPoint = entryRequest "login" {
  Entry = loginUser
}

let FollowUserPoint = entryRequest "follow" {
  Entry = followUser
}

let TweetUserPoint = entryRequest "tweet" {
  Entry = tweetUser
}

let ReTweetUserPoint = entryRequest "retweet" {
  Entry = retweetUser
}

let setCORSHeaders =
    setHeader  "Access-Control-Allow-Origin" "*"
    >=> setHeader "Access-Control-Allow-Headers" "Content-Type"
    >=> setHeader "Access-Control-Allow-Methods" "GET,POST"

let allow_cors : WebPart =
    choose [
        OPTIONS >=>
            fun context ->
                context |> (
                    setCORSHeaders
                    >=> OK "CORS approved" )
    ]

let ws = 
  choose [
    allow_cors
    path "/feed" >=> handShake liveFeed
    RegisterNewUserPoint
    LoginUserPoint
    FollowUserPoint
    TweetUserPoint
    ReTweetUserPoint
    pathScan "/searchTwitter/%s"
      (fun searchkey ->
        let keyval = (sprintf "%s" searchkey)
        let reply = query keyval
        OK reply) 
  ]

[<EntryPoint>]
let main _ =
  startWebServer { defaultConfig with logger = Targets.create Verbose [||] } ws
  0