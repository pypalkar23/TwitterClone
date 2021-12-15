import { Injectable } from '@angular/core';
import { webSocket } from 'rxjs/webSocket';
import { HttpClient } from '@angular/common/http'
import { environment } from 'src/environments/environment';
import { Subject } from 'rxjs';

const userField: string = 'twitter-user';
const websocketurl: string = `${environment.webSocketBase}/feed`
const TWEET_URL: string = `${environment.apiBase}/tweet`
const LOGIN_URL: string = `${environment.apiBase}/login`
const REGISTER_URL: string = `${environment.apiBase}/register`
const RETWEET_URL: string = `${environment.apiBase}/retweet`
const FOLLOW_URL: string = `${environment.apiBase}/follow`
const SEARCH_URL: string = `${environment.apiBase}/searchTwitter`

@Injectable({
  providedIn: 'root'
})

export class TwitterService {
  webSocket: any;
  tweetSubject = new Subject<string>();
  successFlag = "success";
  errorFlag ="error";

  constructor(private http: HttpClient) {
    //this.initializeWebSocket();
    
  }

  storeUser(userId: string) {
    localStorage.setItem(userField, userId)
  }

  getUser():string {
    return localStorage.getItem(userField) || "";
  }

  removeUser(){
    this.webSocket.complete();
    localStorage.removeItem(userField);
  }

  initializeWebSocket() {
    if (this.webSocket!= null)
      return;
    //console.log(websocketurl);
    this.webSocket = webSocket(websocketurl);
    this.webSocket.subscribe(
      (msg: string) => {
        console.log("Message Received",msg);
        this.processMessage(msg);
      },
      (err: any) => console.log("error in websocket",err),
      () => console.log("closing the connection")
    );
  }

  isLoggedIn() {
    //console.log(this.getUser())
    return this.getUser().length != 0;
  }

  closeWebSocket(){
    this.webSocket.complete();
  }

  processMessage(msg:any) {
      //console.log(msg);
      let msgstr;
      let service;
      if(msg && msg.status && msg.status == this.successFlag){
       if(msg.message && !msg.message.startsWith("No feeds")){
         msgstr = msg.message;
       }
       if(msg.resptype){
         service = msg.resptype
       }

       if(msgstr && service){
         if(service == 'LiveFeed'){
           let msgArr = msgstr.split("-");
           msgArr.forEach((element:string) => {
             this.tweetSubject.next(element);
           });
         }
         else{
          this.tweetSubject.next(msgstr);
         }
       }
      }      
  }

  sendDataToserver(msg: string) {
    console.log("Sending message to server", msg);
    this.webSocket.next(msg)
  }
  
  userLogin(userID:string, password:string){
    return this.http.post<any>(LOGIN_URL,{userID:userID,value:password})
  }

  userRegister(userID:string, password:string){
    return this.http.post<any>(REGISTER_URL,{userID:userID,value:password})
  }

  userFollow(val:string){
    return this.http.post<any>(FOLLOW_URL,{userID:this.getUser(),value:val})
  }

  userTweet(val:string){
    return this.http.post<any>(TWEET_URL,{userID:this.getUser(),value:val})
  }

  userRetweet(val:string){
    return this.http.post<any>(RETWEET_URL,{userID:this.getUser(),value:val})
  }

  userSearch(val:string){
    return this.http.post<any>(SEARCH_URL,{userID:this.getUser(),value:val})
  }
  
}
