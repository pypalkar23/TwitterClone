import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {

  username:string='';
  password:string='';
  notificationMsg:string='';
  isError : boolean = false;
  constructor(private twitterService: TwitterService,private router:Router) { 
      //this.register()
  }

  ngOnInit(): void {
    if(this.twitterService.isLoggedIn()){
      this.router.navigate(["/home"]);
    }
  }

  public register(){
      this.notificationMsg ='';
      this.twitterService.userRegister(this.username,this.password).subscribe((resp)=>{
        let respObj = JSON.parse(resp);
        let status = respObj.code;
        let message = respObj.message;
        if(status == 'OK'){
          this.reset();
        }
        else{
          this.isError = true;
        }
        this.notificationMsg = message;
        
        //this.twitterService.storeUser(userId);
        //this.router.navigate(['/login']);
      })
  }
  
  public reset(){
    this.username = '';
    this.password = '';
  }


}