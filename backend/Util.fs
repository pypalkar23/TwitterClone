module Util
open System
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Sockets
open Suave.Writers
open Suave.WebSocket
open Constants

type TwitterResponse = {
  userID: string
  message: string
  resptype: string
  status: string
}

type TwitterRequest = {
  userID: string
  value: string
}

type ResourceType<'a> = {
    Entry : TwitterRequest -> string
}


let toByteSegment (x:string) =
   x |> System.Text.Encoding.ASCII.GetBytes |> ByteSegment




let prepareResponse (user:string, messageStr:string, serviceStr:string, error:bool ) =
   let resp:TwitterResponse={userID=user;message=messageStr;resptype=serviceStr;status= error |> (fun error -> if error then ERROR_CODE else SUCCESS_CODE)}
   resp


let setCORSHeaders =
    setHeader  ACCESS_CONTROL_ALLOW_ORIGIN_KEY ACCESS_CONTROL_ALLOW_ORIGIN_VALUE
    >=> setHeader ACCESS_CONTROL_ALLOW_HEADER_KEY ACCESS_CONTROL_ALLOW_HEADER_VALUE
    >=> setHeader ACCESS_CONTROL_ALLOW_METHODS_KEY ACCESS_CONTROL_ALLOW_METHODS_VALUE

let processedString (txt:String) =
    txt.Replace("\\","") |> fun x -> if x.StartsWith('"') then x.Substring(1,x.Length-2) else x


let prepareResponseGeneric v =     
    let jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(v, jsonSerializerSettings)
    |> OK 
    >=> setMimeType JSON_MIME_TYPE
    >=> setCORSHeaders

let respJson (v:string) =     
    let jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(v, jsonSerializerSettings)
    |> OK 
    >=> setMimeType JSON_MIME_TYPE
    >=> setCORSHeaders
    

let StringifyFromJsonGeneric<'a> json =
        JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

let getReqResource<'a> (requestInp : HttpRequest) = 
    let prepareString (rawForm:byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)
    let requestArrByte:byte[] = requestInp.rawForm
    requestArrByte |> prepareString |> StringifyFromJsonGeneric<TwitterRequest>

let preparePostEntryFor handler resource = 
  let endPoint = "/" + handler

  let getEntry reqData =
    let response = resource.Entry reqData
    response

  choose [
    path endPoint >=> choose [
      POST >=> request (getReqResource >> getEntry >> prepareResponseGeneric)
    ]
  ]
   