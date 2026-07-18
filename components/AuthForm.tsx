'use client';

import Image from 'next/image';
import Link from 'next/link';
import { useState } from 'react';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Resolver, useForm } from 'react-hook-form';
import { Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';

import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  signInSchema,
  signUpSchema,
  SOUTH_AFRICAN_PROVINCES,
} from '@/lib/utils';
import { signIn, signUp } from '@/lib/actions/user.actions';
import CustomInput from './CustomInput';

type AuthFormValues = z.input<typeof signUpSchema>;

const AuthForm = ({ type }: { type: 'sign-in' | 'sign-up' }) => {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const formSchema = type === 'sign-up' ? signUpSchema : signInSchema;

  const form = useForm<AuthFormValues>({
    resolver: zodResolver(formSchema) as Resolver<AuthFormValues>,
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      mobileNumber: '',
      password: '',
      confirmPassword: '',
      address1: '',
      suburb: '',
      city: '',
      province: undefined,
      postalCode: '',
      dateOfBirth: '',
      country: 'South Africa',
      termsAccepted: false,
      privacyAccepted: false,
    },
  });

  const onSubmit = async (data: AuthFormValues) => {
    setIsLoading(true);
    form.clearErrors('root');

    try {
      if (type === 'sign-up') {
        const registrationData = signUpSchema.parse(data);
        await signUp(registrationData);
        router.replace('/');
        router.refresh();
        return;
      }

      const credentials = signInSchema.parse(data);
      const response = await signIn(credentials);
      if (response) {
        router.replace('/');
        router.refresh();
      }
    } catch (error) {
      form.setError('root', {
        message:
          error instanceof Error
            ? error.message
            : 'Something went wrong. Please try again.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <section className="auth-form">
      <header className="flex flex-col gap-5 md:gap-8">
        <Link href="/" className="flex cursor-pointer items-center gap-1">
          <Image src="/icons/logo.svg" width={34} height={34} alt="Kape App logo" />
          <h1 className="text-26 font-ibm-plex-serif font-bold text-black-1">
            Kape App
          </h1>
        </Link>
        <div className="flex flex-col gap-1 md:gap-3">
          <h1 className="text-24 font-semibold text-gray-900 lg:text-36">
            {type === 'sign-in' ? 'Sign in' : 'Create your account'}
          </h1>
          <p className="text-16 font-normal text-gray-600">
            {type === 'sign-in'
              ? 'Enter your account details.'
              : 'Register using your South African contact and address details.'}
          </p>
        </div>
      </header>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6" noValidate>
          {type === 'sign-up' && (
            <>
              <div className="grid gap-4 md:grid-cols-2">
                <CustomInput
                  control={form.control}
                  name="firstName"
                  label="First name"
                  placeholder="Enter your first name"
                  autoComplete="given-name"
                />
                <CustomInput
                  control={form.control}
                  name="lastName"
                  label="Surname"
                  placeholder="Enter your surname"
                  autoComplete="family-name"
                />
              </div>

              <CustomInput
                control={form.control}
                name="mobileNumber"
                label="South African mobile number"
                placeholder="e.g. 082 123 4567 or +27 82 123 4567"
                type="tel"
                inputMode="tel"
                autoComplete="tel"
              />

              <CustomInput
                control={form.control}
                name="address1"
                label="Address line 1"
                placeholder="Street number and street name"
                autoComplete="address-line1"
              />

              <div className="grid gap-4 md:grid-cols-2">
                <CustomInput
                  control={form.control}
                  name="suburb"
                  label="Suburb"
                  placeholder="Enter your suburb"
                  autoComplete="address-level3"
                />
                <CustomInput
                  control={form.control}
                  name="city"
                  label="City or town"
                  placeholder="Enter your city or town"
                  autoComplete="address-level2"
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <FormField
                  control={form.control}
                  name="province"
                  render={({ field }) => (
                    <FormItem className="form-item">
                      <FormLabel className="form-label">Province</FormLabel>
                      <div className="flex w-full flex-col">
                        <Select value={field.value} onValueChange={field.onChange}>
                          <FormControl>
                            <SelectTrigger className="input-class">
                              <SelectValue placeholder="Select your province" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {SOUTH_AFRICAN_PROVINCES.map((province) => (
                              <SelectItem key={province} value={province}>
                                {province}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage className="form-message mt-2" />
                      </div>
                    </FormItem>
                  )}
                />

                <CustomInput
                  control={form.control}
                  name="postalCode"
                  label="Postal code"
                  placeholder="e.g. 0002"
                  inputMode="numeric"
                  autoComplete="postal-code"
                  maxLength={4}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <CustomInput
                  control={form.control}
                  name="dateOfBirth"
                  label="Date of birth"
                  placeholder="DD/MM/YYYY"
                  inputMode="numeric"
                  autoComplete="bday"
                  maxLength={10}
                />

                <FormField
                  control={form.control}
                  name="country"
                  render={({ field }) => (
                    <FormItem className="form-item">
                      <FormLabel className="form-label">Country</FormLabel>
                      <div className="flex w-full flex-col">
                        <FormControl>
                          <Input
                            {...field}
                            value="South Africa"
                            readOnly
                            className="input-class bg-gray-50"
                          />
                        </FormControl>
                        <FormMessage className="form-message mt-2" />
                      </div>
                    </FormItem>
                  )}
                />
              </div>

              <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 text-sm text-gray-700">
                Basic registration does not request a South African ID number,
                passport number or banking credentials. Identity verification will
                be handled later through a secure KYC process.
              </div>
            </>
          )}

          <CustomInput
            control={form.control}
            name="email"
            label="Email address"
            placeholder="Enter your email address"
            type="email"
            autoComplete="email"
          />

          <CustomInput
            control={form.control}
            name="password"
            label="Password"
            placeholder="At least 8 characters"
            type="password"
            autoComplete={type === 'sign-in' ? 'current-password' : 'new-password'}
          />

          {type === 'sign-up' && (
            <>
              <CustomInput
                control={form.control}
                name="confirmPassword"
                label="Confirm password"
                placeholder="Enter your password again"
                type="password"
                autoComplete="new-password"
              />

              <FormField
                control={form.control}
                name="termsAccepted"
                render={({ field }) => (
                  <FormItem>
                    <div className="flex items-start gap-3">
                      <FormControl>
                        <input
                          type="checkbox"
                          checked={field.value}
                          onChange={(event) => field.onChange(event.target.checked)}
                          onBlur={field.onBlur}
                          ref={field.ref}
                          className="mt-1 size-4 rounded border-gray-300"
                        />
                      </FormControl>
                      <FormLabel className="text-sm font-normal text-gray-700">
                        I accept the Kape App terms of use.
                      </FormLabel>
                    </div>
                    <FormMessage className="form-message mt-2" />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="privacyAccepted"
                render={({ field }) => (
                  <FormItem>
                    <div className="flex items-start gap-3">
                      <FormControl>
                        <input
                          type="checkbox"
                          checked={field.value}
                          onChange={(event) => field.onChange(event.target.checked)}
                          onBlur={field.onBlur}
                          ref={field.ref}
                          className="mt-1 size-4 rounded border-gray-300"
                        />
                      </FormControl>
                      <FormLabel className="text-sm font-normal text-gray-700">
                        I acknowledge the privacy notice and consent to the processing
                        of my registration information.
                      </FormLabel>
                    </div>
                    <FormMessage className="form-message mt-2" />
                  </FormItem>
                )}
              />
            </>
          )}

          {form.formState.errors.root?.message && (
            <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">
              {form.formState.errors.root.message}
            </p>
          )}

          <Button type="submit" disabled={isLoading} className="form-btn w-full">
            {isLoading ? (
              <>
                <Loader2 size={20} className="animate-spin" /> &nbsp;
                {type === 'sign-in' ? 'Signing in...' : 'Creating account...'}
              </>
            ) : type === 'sign-in' ? (
              'Sign in'
            ) : (
              'Create account'
            )}
          </Button>
        </form>
      </Form>

      <footer className="flex justify-center gap-1">
        <p className="text-14 font-normal text-gray-600">
          {type === 'sign-in' ? "Don't have an account?" : 'Already have an account?'}
        </p>
        <Link href={type === 'sign-in' ? '/sign-up' : '/sign-in'} className="form-link">
          {type === 'sign-in' ? 'Sign up' : 'Sign in'}
        </Link>
      </footer>
    </section>
  );
};

export default AuthForm;
