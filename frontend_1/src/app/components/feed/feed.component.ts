import { Component, OnInit } from '@angular/core';
import { TwitterService } from 'src/app/service/twitter-service.service';
@Component({
  selector: 'app-feed',
  templateUrl: './feed.component.html',
  styleUrls: ['./feed.component.scss']
})
export class FeedComponent implements OnInit {
  
  
  constructor(twitterService: TwitterService) { 
    let data = {'userID':'mandar','value':''}
    twitterService.sendDataToserver(JSON.stringify(data))
  }

  ngOnInit(): void {
  }


}
