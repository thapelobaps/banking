// lib/appwrite.ts
import { Client, Account, Databases } from 'node-appwrite';
import { cookies } from 'next/headers';

export const createAdminClient = async () => {
  const client = new Client()
    .setEndpoint(process.env.NEXT_PUBLIC_APPWRITE_ENDPOINT!)
    .setProject(process.env.NEXT_PUBLIC_APPWRITE_PROJECT!)
    .setKey(process.env.APPWRITE_API_KEY!);

  return {
    account: new Account(client),
    database: new Databases(client),
  };
};

export const createSessionClient = async () => {
  const client = new Client()
    .setEndpoint(process.env.NEXT_PUBLIC_APPWRITE_ENDPOINT!)
    .setProject(process.env.NEXT_PUBLIC_APPWRITE_PROJECT!);

  const session = cookies().get('appwrite-session')?.value;
  if (!session) {
    return null;
  }

  client.setSession(session);

  return {
    account: new Account(client),
    database: new Databases(client),
  };
};