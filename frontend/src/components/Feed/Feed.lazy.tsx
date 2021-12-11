import React, { lazy, Suspense } from 'react';

const LazyFeed = lazy(() => import('./Feed'));

const Feed = (props: JSX.IntrinsicAttributes & { children?: React.ReactNode; }) => (
  <Suspense fallback={null}>
    <LazyFeed {...props} />
  </Suspense>
);

export default Feed;
