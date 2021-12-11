import './Home.scss';
import Feed from '../Feed/Feed'
import Follow from '../Follow/Follow'
import Tweet from '../Tweet/Tweet'

const Home = () => (
  <div className="container">
    <div className="row">
      <div className="col-lg-7 col-mg-7">
        <Feed />
      </div>
      <div className="col-lg-5 col-mg-5">
      <Tweet />
      <Follow />  
      </div>
    </div>
  </div>
);

export default Home;
