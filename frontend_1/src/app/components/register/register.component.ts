import { Component, OnInit } from '@angular/core';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {

  username:string = "mandar";
  password:string = "mandar";
  constructor(private twitterService: TwitterService) { 
      this.registerUser()
  }

  ngOnInit(): void {
  }

  public registerUser(){
      this.twitterService.userRegister(this.username,this.password).subscribe((resp)=>{
        console.log(JSON.parse(resp));
      })
  }



}
