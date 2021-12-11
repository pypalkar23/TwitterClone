import React from 'react';
import './Feed.scss';

const username="pypalkar23"

const Feed = () => (
  <div className="col-lg-11 col-md-11">
    <div className="heading">Tweet Feed</div>
    <div className="tweet-feed">
      <div className="tweet-wrapper d-flex justify-content-center">
        <div className= "tweet shadow-sm p-3">
        <div className="tweet-by">{"@"+ username}</div>
        <div className="tweet-txt">Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum</div>
        </div>
      </div>
      <div className="tweet-wrapper d-flex justify-content-center">
        <div className= "tweet shadow-sm p-3">
        <div className="tweet-by">{"@"+ username}</div>
        <div className="tweet-txt">Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum</div>
        </div>
      </div>
    </div>
  </div>
);

export default Feed;
