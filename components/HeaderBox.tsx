import { HeaderBoxProps } from '@/types';

const HeaderBox = ({ type = 'title', title, subtext, user }: HeaderBoxProps) => {
  return (
    <div className="header-box">
      <h1 className="header-box-title">
        <span>{title}</span>
        {type === 'greeting' && user ? <span className="text-[#7a4a37]"> {user}</span> : null}
      </h1>
      <p className="header-box-subtext">{subtext}</p>
    </div>
  );
};

export default HeaderBox;
