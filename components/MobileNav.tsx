'use client';

import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetTrigger,
} from '@/components/ui/sheet';
import { sidebarLinks } from '@/constants';
import Image from 'next/image';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import Footer from './Footer';
import { MobileNavProps } from '@/types';

const MobileNav = ({ user }: MobileNavProps) => {
  const pathname = usePathname();

  return (
    <section className="w-full max-w-[264px]">
      <Sheet>
        <SheetTrigger aria-label="Open navigation" className="flex size-10 items-center justify-center rounded-xl border border-[#eadfd8] bg-white">
          <Image src="/icons/hamburger.svg" width={24} height={24} alt="" className="cursor-pointer" />
        </SheetTrigger>
        <SheetContent side="left" className="border-r border-[#eadfd8] bg-white p-5">
          <Link href="/" className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-2xl bg-[#4a2b20]">
              <Image src="/icons/logo.svg" width={26} height={26} alt="Kape App logo" className="brightness-0 invert" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-[#2b1a14]">Kape</h1>
              <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-[#9a8378]">Money, simplified</p>
            </div>
          </Link>

          <div className="mobilenav-sheet mt-10">
            <nav className="flex h-full flex-col gap-2">
              {sidebarLinks.map((item) => {
                const isActive = pathname === item.route || pathname.startsWith(`${item.route}/`);
                return (
                  <SheetClose asChild key={item.route}>
                    <Link
                      href={item.route}
                      className={`flex w-full items-center gap-3 rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                        isActive
                          ? 'bg-[#4a2b20] text-white shadow-sm'
                          : 'text-[#6f5b52] hover:bg-[#f5eee9] hover:text-[#2b1a14]'
                      }`}
                    >
                      <Image
                        src={item.imgURL}
                        alt=""
                        width={20}
                        height={20}
                        className={isActive ? 'brightness-0 invert' : 'opacity-70'}
                      />
                      {item.label}
                    </Link>
                  </SheetClose>
                );
              })}
            </nav>

            <div className="border-t border-[#eee5df] pt-4">
              <Footer user={user} type="mobile" />
            </div>
          </div>
        </SheetContent>
      </Sheet>
    </section>
  );
};

export default MobileNav;
