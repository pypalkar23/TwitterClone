import { Injectable } from '@angular/core';
import { webSocket } from 'rxjs/webSocket';
import { HttpClient } from '@angular/common/http'
import { environment } from 'src/environments/environment';

const userField: string = 'twitter-user';
const websocketurl: string = `${environment.webSocketBase}/feed`
const TWEET_URL: string = `${environment.apiBase}/tweet`
const LOGIN_URL: string = `${environment.apiBase}/login`
const REGISTER_URL: string = `${environment.apiBase}/register`
const RETWEET_URL: string = `${environment.apiBase}/retweet`
const FOLLOW_URL: string = `${environment.apiBase}/follow`

@Injectable({
  providedIn: 'root'
})

export class TwitterService {
  subject: any;

  constructor(private http: HttpClient) {
    this.initializeWebSocket();
  }

  storeUser(userId: string) {
    localStorage.setItem(userField, userId)
  }

  getUser() {
    localStorage.getItem(userField);
  }

  initializeWebSocket() {
    console.log("initialized");
    console.log(websocketurl);
    this.subject = webSocket(websocketurl);
    this.subject.subscribe(
      (msg: string) => {
        console.log("Message Received",msg);
        this.processMessage(msg);
      },
      (err: any) => console.log("error in websocket",err),
      () => console.log("closing the connection")
    );
  }

  isLoggedIn() {
    return this.getUser() != undefined && this.getUser() != null;
  }

  processMessage(msg: string) {

  }

  sendDataToserver(msg: string) {
    console.log("Sending message to server", msg);
    this.subject.next(msg)
  }
  
  userLogin(userID:string, password:string){
    return this.http.post<any>(TWEET_URL,{userID:userID,value:password})
  }

  userRegister(userID:string, password:string){
    return this.http.post<any>(REGISTER_URL,{userID:userID,value:password})
  }

  userFollow(userID:string, val:string){
    return this.http.post<any>(FOLLOW_URL,{userID:userID,value:val})
  }

  userTweet(userID:string, val:string){
    return this.http.post<any>(TWEET_URL,{userID:userID,value:val})
  }

  userRetweet(userID:string, val:string){
    return this.http.post<any>(RETWEET_URL,{userID:userID,value:val})
  }
  
}
