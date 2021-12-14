import { Component, OnInit } from '@angular/core';
import { TwitterService } from 'src/app/service/twitter-service.service';
@Component({
  selector: 'app-feed',
  templateUrl: './feed.component.html',
  styleUrls: ['./feed.component.scss']
})
export class FeedComponent implements OnInit {

  feedText: String[] = [];
  userId: any = '';
  constructor(private twitterService: TwitterService) {
    this.userId = twitterService.getUser();
    this.twitterService.tweetSubject.subscribe({ next: (feedentry) => { if(feedentry && feedentry.trim().length!=0) this.feedText.unshift(feedentry.replace("\^"," ")); } });
    //twitterService.sendDataToserver(JSON.stringify(data))
  }

  ngOnInit(): void {
    this.twitterService.sendDataToserver(JSON.stringify({ 'userID': this.userId, 'value': '' }))
  }


}
