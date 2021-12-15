import { Component, OnInit } from '@angular/core';
import { resetFakeAsyncZone } from '@angular/core/testing';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-follow',
  templateUrl: './follow.component.html',
  styleUrls: ['./follow.component.scss']
})
export class FollowComponent implements OnInit {
  username: string = "";
  notificationMsg: string = '';
  isError: boolean = false;

  constructor(private twitterService: TwitterService) { }

  ngOnInit(): void {
  }

  follow() {
    this.twitterService.userFollow(this.username).subscribe((resp) => {
      console.log(resp);
      let respObj = JSON.parse(resp);
      let status = respObj.status;
      let message = respObj.message;
      if (status == this.twitterService.successFlag) {
        this.reset();
        this.isError = false;
      }
      else {
        this.isError = true;
      }
      this.notificationMsg = message;
    })
  }


  reset() {
    this.username = '';
  }

}
