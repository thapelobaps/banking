'use client';

import { logoutAccount } from '@/lib/actions/user.actions';
import { FooterProps } from '@/types';
import Image from 'next/image';
import { useRouter } from 'next/navigation';

const Footer = ({ user, type = 'desktop' }: FooterProps) => {
  const router = useRouter();

  const handleLogOut = async () => {
    const loggedOut = await logoutAccount();
    if (loggedOut) router.push('/sign-in');
  };

  const mobile = type === 'mobile';

  return (
    <footer className={mobile ? 'kape-user kape-user--mobile' : 'kape-user'}>
      <span className="kape-user__avatar" aria-hidden="true">
        {user.firstName[0]}{user.lastName?.[0] ?? ''}
      </span>
      <span className="kape-user__details">
        <strong>{user.firstName} {user.lastName}</strong>
        <small>{user.email}</small>
      </span>
      <button type="button" className="kape-user__logout" onClick={handleLogOut} aria-label="Sign out">
        <Image src="/icons/logout.svg" width={17} height={17} alt="" />
      </button>
    </footer>
  );
};

export default Footer;
