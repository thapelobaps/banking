'use client';

import Image from 'next/image';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import Footer from './Footer';
import { sidebarLinks } from '@/constants';
import type { User } from '@/types';

type SidebarProps = {
  user: User;
};

const Sidebar = ({ user }: SidebarProps) => {
  const pathname = usePathname();

  return (
    <aside className="sidebar">
      <div className="flex size-full flex-col">
        <Link href="/" className="flex items-center gap-3 px-3" aria-label="Kape App home">
          <div className="flex size-11 items-center justify-center rounded-2xl bg-[#4a2b20] shadow-sm">
            <Image src="/icons/logo.svg" alt="Kape App" width={28} height={28} className="brightness-0 invert" />
          </div>
          <div className="max-xl:hidden">
            <p className="text-xl font-bold tracking-tight text-[#2b1a14]">Kape</p>
            <p className="text-[11px] font-medium uppercase tracking-[0.18em] text-[#8a756b]">Money, simplified</p>
          </div>
        </Link>

        <div className="mx-3 mt-8 rounded-2xl border border-[#eadfd8] bg-[#fbf7f4] p-4 max-xl:hidden">
          <div className="flex items-center gap-2">
            <span className="size-2 rounded-full bg-emerald-500" />
            <p className="text-xs font-semibold text-[#4a2b20]">Demo workspace</p>
          </div>
          <p className="mt-2 text-xs leading-5 text-[#7b675e]">
            Secure SQL-backed South African banking simulation.
          </p>
        </div>

        <nav className="mt-6 flex-1 px-2">
          <ul className="space-y-2">
            {sidebarLinks.map((item) => {
              const isActive = pathname === item.route || pathname.startsWith(`${item.route}/`);
              return (
                <li key={item.route}>
                  <Link
                    className={`sidebar-link group ${
                      isActive
                        ? 'bg-[#4a2b20] text-white shadow-sm'
                        : 'text-[#6f5b52] hover:bg-[#f5eee9] hover:text-[#2b1a14]'
                    }`}
                    href={item.route}
                  >
                    <Image
                      src={item.imgURL}
                      alt=""
                      width={22}
                      height={22}
                      className={isActive ? 'brightness-0 invert' : 'opacity-70 group-hover:opacity-100'}
                    />
                    <span className="sidebar-label">{item.label}</span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>

        <div className="border-t border-[#eee5df] px-3 pt-4">
          <Footer user={user} />
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;
