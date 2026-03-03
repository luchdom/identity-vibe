import type { Metadata } from "next";
import type { ReactNode } from "react";

import { Roboto } from "next/font/google";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";

import { AuthProvider } from "@/contexts/auth-context";
import { ThemeModeProvider } from "@/theme";

import "./globals.css";

const roboto = Roboto({
  weight: ["300", "400", "500", "700"],
  subsets: ["latin"],
  display: "swap",
  variable: "--font-roboto",
});

export const metadata: Metadata = {
  title: "Web App",
  description: "Material UI migration frontend for the identity admin experience.",
};

type RootLayoutProps = {
  children: ReactNode;
};

export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html lang="en" className={roboto.variable}>
      <body>
        <AppRouterCacheProvider options={{ key: "mui" }}>
          <ThemeModeProvider fontFamily="var(--font-roboto)">
            <AuthProvider>{children}</AuthProvider>
          </ThemeModeProvider>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
