module Util
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open System
open Suave.Writers

type ResponseType = {
  userID: string
  message: string
  service: string
  code: string
}

type RequestType = {
  userID: string
  value: string
}

type RestResource<'a> = {
    Entry : RequestType -> string
}

let processedString (txt:String) =
    txt.Replace("\\","") |> fun x -> if x.StartsWith('"') then x.Substring(1,x.Length-2) else x


let respInJson v =     
    let jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(v, jsonSerializerSettings)
    |> OK 
    >=> setMimeType "application/json; charset=utf-8"
    >=> setHeader  "Access-Control-Allow-Origin" "*"
    >=> setHeader "Access-Control-Allow-Headers" "Content-Type"
    >=> setHeader "Access-Control-Allow-Methods" "GET,POST"

let respJson (v:string) =     
    let jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(v, jsonSerializerSettings)
    |> OK 
    >=> setMimeType "application/json; charset=utf-8"
    >=> setHeader  "Access-Control-Allow-Origin" "*"
    >=> setHeader "Access-Control-Allow-Headers" "Content-Type"
    >=> setHeader "Access-Control-Allow-Methods" "GET,POST"

let fromJson<'a> json =
        JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

let getReqResource<'a> (requestInp : HttpRequest) = 
    let getInString (rawForm:byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)
    let requestArr:byte[] = requestInp.rawForm
    requestArr |> getInString |> fromJson<RequestType>

let entryRequest resourceName resource = 
  let resourcePath = "/" + resourceName

  let entryDone userInput =
    let userRegResp = resource.Entry userInput
    userRegResp

  choose [
    path resourcePath >=> choose [
      POST >=> request (getReqResource >> entryDone >> respInJson)
    ]
  ]
   