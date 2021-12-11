import React, { lazy, Suspense } from 'react';

const LazyTweet = lazy(() => import('./Tweet'));

const Tweet = (props: JSX.IntrinsicAttributes & { children?: React.ReactNode; }) => (
  <Suspense fallback={null}>
    <LazyTweet {...props} />
  </Suspense>
);

export default Tweet;
