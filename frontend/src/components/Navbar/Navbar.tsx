import './Navbar.scss';

const Navbar = () => (
  <div className="navbar-wrapper">
        <div className="container-fluid">
          <nav className={"navbar navbar-expand-lg"}>
            <div className="container">
              <a className="navbar-brand" href="#">Twitter Clone</a>
              <button className="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                <span className={"navbar-toggler-icon"}></span>
              </button>
              <div className={"collapse navbar-collapse"} id="navbarSupportedContent">
                <ul className="navbar-nav me-auto mb-2 mb-lg-0">
                  <li className="nav-item">
                    <a className={"nav-link active"} aria-current="page" href="/home">Home</a>
                  </li>
                </ul>
                <li className={"nav-item d-flex"}>
                  <a className="nav-link" href="#">Login</a>
                </li>
                <li className={"nav-item d-flex"}>
                  <a className="nav-link" href="#">Logout</a>
                </li>
              </div>
            </div>
          </nav>
        </div>
      </div>
);

export default Navbar;
