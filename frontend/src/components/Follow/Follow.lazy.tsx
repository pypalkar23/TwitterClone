import React, { lazy, Suspense } from 'react';

const LazyFollow = lazy(() => import('./Follow'));

const Follow = (props: JSX.IntrinsicAttributes & { children?: React.ReactNode; }) => (
  <Suspense fallback={null}>
    <LazyFollow {...props} />
  </Suspense>
);

export default Follow;
