import './Follow.scss';

const Follow = () => (
  <div className="follow-wrapper container-fluid">
  <div className="heading-follow">Follow User</div>
  <div className="shadow p-3">
    <div className="mb-3">
      <label htmlFor="exampleInputEmail1" className="form-label">Username</label>
      <input type="text" className="form-control" id="exampleInputEmail1" aria-describedby="emailHelp" />
      <div id="emailHelp" className="form-text">We'll never share your email with anyone else.</div>
    </div>
    <button type="submit" className="btn btn-primary">Submit</button>
  </div>
</div>
);

export default Follow;
