import React from 'react';
import ReactDOM from 'react-dom';
import Tweet from './Tweet';

it('It should mount', () => {
  const div = document.createElement('div');
  ReactDOM.render(<Tweet />, div);
  ReactDOM.unmountComponentAtNode(div);
});