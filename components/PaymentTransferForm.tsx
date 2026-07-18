"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import * as z from "zod";

import { createMockTransfer } from "@/lib/actions/bank.actions";
import { PaymentTransferFormProps } from "@/types";

import { BankDropdown } from "./BankDropdown";
import { Button } from "./ui/button";
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import { Input } from "./ui/input";
import { Textarea } from "./ui/textarea";

const formSchema = z.object({
  name: z
    .string()
    .trim()
    .max(120, "Transfer note must be 120 characters or fewer")
    .optional()
    .default(""),
  amount: z
    .string()
    .trim()
    .refine(
      (value) => /^\d+(\.\d{1,2})?$/.test(value) && Number(value) > 0,
      "Enter a valid amount greater than R0.00"
    ),
  senderBank: z.string().uuid("Select a valid source account"),
  recipientReference: z.string().uuid("Enter a valid demo account reference"),
});

const PaymentTransferForm = ({ accounts }: PaymentTransferFormProps) => {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: "",
      amount: "",
      senderBank: accounts[0]?.id ?? "",
      recipientReference: "",
    },
  });

  const submit = async (data: z.infer<typeof formSchema>) => {
    setIsLoading(true);
    form.clearErrors("root");

    try {
      if (data.senderBank === data.recipientReference) {
        form.setError("recipientReference", {
          message: "Choose a different recipient account",
        });
        return;
      }

      await createMockTransfer({
        senderBankId: data.senderBank,
        receiverBankId: data.recipientReference,
        amount: Number(data.amount),
        name: data.name || "Demo transfer",
      });

      form.reset({
        name: "",
        amount: "",
        senderBank: accounts[0]?.id ?? "",
        recipientReference: "",
      });
      router.push("/");
      router.refresh();
    } catch (error) {
      form.setError("root", {
        message:
          error instanceof Error
            ? error.message
            : "The simulated transfer failed. No real money was moved.",
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(submit)} className="flex flex-col">
        <div className="mb-5 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
          <p className="font-semibold">Demo transfer only</p>
          <p className="mt-1">
            This flow updates SQL Server demo balances through the Kape API. It does not connect to a bank or move real money.
          </p>
        </div>

        <FormField
          control={form.control}
          name="senderBank"
          render={() => (
            <FormItem className="border-t border-gray-200">
              <div className="payment-transfer_form-item pb-6 pt-5">
                <div className="payment-transfer_form-content">
                  <FormLabel className="text-14 font-medium text-gray-700">
                    From account
                  </FormLabel>
                  <FormDescription className="text-12 font-normal text-gray-600">
                    Select the demo account to debit
                  </FormDescription>
                </div>
                <div className="flex w-full flex-col">
                  <FormControl>
                    <BankDropdown
                      accounts={accounts}
                      setValue={form.setValue}
                      otherStyles="!w-full"
                    />
                  </FormControl>
                  <FormMessage className="text-12 text-red-500" />
                </div>
              </div>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="recipientReference"
          render={({ field }) => (
            <FormItem className="border-t border-gray-200">
              <div className="payment-transfer_form-item py-5">
                <div className="payment-transfer_form-content">
                  <FormLabel className="text-14 font-medium text-gray-700">
                    Recipient demo account reference
                  </FormLabel>
                  <FormDescription className="text-12 font-normal text-gray-600">
                    Copy this reference from the recipient&apos;s demo account card
                  </FormDescription>
                </div>
                <div className="flex w-full flex-col">
                  <FormControl>
                    <Input
                      placeholder="Paste the demo account reference"
                      className="input-class"
                      autoComplete="off"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage className="text-12 text-red-500" />
                </div>
              </div>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="amount"
          render={({ field }) => (
            <FormItem className="border-t border-gray-200">
              <div className="payment-transfer_form-item py-5">
                <div className="payment-transfer_form-content">
                  <FormLabel className="text-14 font-medium text-gray-700">
                    Amount (ZAR)
                  </FormLabel>
                  <FormDescription className="text-12 font-normal text-gray-600">
                    Enter the mock amount to transfer
                  </FormDescription>
                </div>
                <div className="flex w-full flex-col">
                  <FormControl>
                    <Input
                      inputMode="decimal"
                      placeholder="e.g. 250.00"
                      className="input-class"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage className="text-12 text-red-500" />
                </div>
              </div>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem className="border-y border-gray-200">
              <div className="payment-transfer_form-item pb-6 pt-5">
                <div className="payment-transfer_form-content">
                  <FormLabel className="text-14 font-medium text-gray-700">
                    Payment reference
                  </FormLabel>
                  <FormDescription className="text-12 font-normal text-gray-600">
                    Add an optional reference for the simulated transfer
                  </FormDescription>
                </div>
                <div className="flex w-full flex-col">
                  <FormControl>
                    <Textarea
                      placeholder="e.g. Shared groceries"
                      className="input-class"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage className="text-12 text-red-500" />
                </div>
              </div>
            </FormItem>
          )}
        />

        {form.formState.errors.root?.message && (
          <p role="alert" className="mt-4 text-sm text-red-600">
            {form.formState.errors.root.message}
          </p>
        )}

        <div className="payment-transfer_btn-box">
          <Button
            type="submit"
            className="payment-transfer_btn"
            disabled={isLoading || accounts.length === 0}
          >
            {isLoading ? (
              <>
                <Loader2 size={20} className="animate-spin" /> &nbsp; Simulating...
              </>
            ) : (
              "Simulate transfer"
            )}
          </Button>
        </div>
      </form>
    </Form>
  );
};

export default PaymentTransferForm;
