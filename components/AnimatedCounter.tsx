'use client';

import CountUp from 'react-countup';

const AnimatedCounter = ({ amount }: { amount: number }) => {
  return (
    <span className="tabular-nums">
      <CountUp
        decimals={2}
        decimal=","
        separator=" "
        prefix="R "
        end={amount}
        duration={0.8}
      />
    </span>
  );
};

export default AnimatedCounter;
