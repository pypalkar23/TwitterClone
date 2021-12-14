import { Component, OnInit } from '@angular/core';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-tweet',
  templateUrl: './tweet.component.html',
  styleUrls: ['./tweet.component.scss']
})
export class TweetComponent implements OnInit {
  tweet:string = '';
  notificationMsg:string='';
  isError : boolean = false;

  constructor(private twitterService:TwitterService) { }

  ngOnInit(): void {
  }

  public tweetMsg(){
    this.notificationMsg ='';
    this.twitterService.userTweet(this.tweet).subscribe((resp)=>{
      let respObj = JSON.parse(resp);
      let status = respObj.code;
      let message = respObj.message;
      if(status == 'OK'){
        this.reset();
        this.notificationMsg = 'You have successfully tweeted a msg';
      }
      else{
        this.isError = true;
        this.notificationMsg = message;
      }
      
      
      //this.twitterService.storeUser(userId);
      //this.router.navigate(['/login']);
    })
}

public reset(){
  this.tweet='';
}

  

}
