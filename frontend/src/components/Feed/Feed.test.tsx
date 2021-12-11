import React from 'react';
import ReactDOM from 'react-dom';
import Feed from './Feed';

it('It should mount', () => {
  const div = document.createElement('div');
  ReactDOM.render(<Feed />, div);
  ReactDOM.unmountComponentAtNode(div);
});