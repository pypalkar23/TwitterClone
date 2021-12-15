import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {

  constructor(private twitterService:TwitterService, private router:Router) { 
    
  }

  ngOnInit(): void {
    if(!this.twitterService.isLoggedIn()){
      this.router.navigate(["/login"]);
      return;
    }
    this.twitterService.initializeWebSocket();
  }
}
