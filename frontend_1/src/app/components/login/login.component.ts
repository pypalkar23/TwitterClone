import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  username:string='';
  password:string='';
  notificationMsg:string='';
  isError : boolean = false;
  constructor(private twitterService:TwitterService, private router:Router) { }

  ngOnInit(): void {
    if(this.twitterService.isLoggedIn()){
      this.router.navigate(["/home"]);
    }
  }

  login(){
    this.twitterService.userLogin(this.username,this.password).subscribe((resp)=>{
      console.log(resp);
      let respObj = JSON.parse(resp);
        let status = respObj.status;
        let message = respObj.message;
        let userID = respObj.userID;
        if(status == this.twitterService.successFlag){
          this.reset();
          this.twitterService.storeUser(userID);
          this.router.navigate(["/home"]);
        }
        else{
          this.isError = true;
          this.notificationMsg = message;
        }
    })
  }

  public reset(){
    this.username = '';
    this.password = '';
  }

}
