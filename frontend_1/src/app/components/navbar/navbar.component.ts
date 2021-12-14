import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {

  constructor(private twitterService:TwitterService, private router: Router) { }

  ngOnInit(): void {
  }

  isActive(){
    //console.log(this.twitterService.isLoggedIn());
    return this.twitterService.isLoggedIn();
  }

  logout(){
    this.twitterService.removeUser();
    this.router.navigate(["/login"]);
  }
}
