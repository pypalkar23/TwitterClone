import { Component, OnInit } from '@angular/core';
import { mergeScan } from 'rxjs';
import { TwitterService } from 'src/app/service/twitter-service.service';

@Component({
  selector: 'app-search',
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss']
})
export class SearchComponent implements OnInit {
  querytext: string = '';
  searchResult: any = [];
  noTweetsFoundError: string = '';
  constructor(private twitterService: TwitterService) {
    this.searchResult = [];
  }

  ngOnInit(): void {

  }

  search() {
    this.twitterService.userSearch(this.querytext).subscribe((resp) => {
      this.searchResult = []
      this.noTweetsFoundError = '';
      let respObj = JSON.parse(resp);
      if (respObj && respObj.status == this.twitterService.successFlag) {
        let temp = respObj.message;
        if (temp.startsWith("No tweets")) {
          this.noTweetsFoundError = temp;
        }
        else {
          let tempArr = temp.split("-");
          tempArr.forEach((element: string) => {
            if (element && element.trim().length != 0) {
              let tempTweetArr = element.split("|");

              let obj = {
                user: tempTweetArr[1],
                time: tempTweetArr[2],
                text: tempTweetArr[3]
              }
              console.log(obj);
              this.searchResult.push(obj);
            }

          });
        }
      }
      else {
        this.noTweetsFoundError = respObj.message;
      }
    })
  }


}
