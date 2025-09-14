'use client'

import * as React from "react"
import { GalleryVerticalEnd, Minus, Plus, ShoppingCart, Home, Users, Settings, LogOut, User, Shield } from "lucide-react"

import { SearchForm } from "@/components/search-form"
import { useAuth } from "@/contexts/auth-context"
import { isAdmin } from "@/components/auth/AuthGuard"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from "@/components/ui/sidebar"
import { NavUser } from "./nav-user"

// Identity System Admin Navigation
const data = {
  navMain: [
    {
      title: "Dashboard",
      url: "/dashboard",
      icon: Home,
      items: [
        {
          title: "Overview",
          url: "/dashboard",
        },
      ],
    },
    {
      title: "Orders Management",
      url: "#",
      icon: ShoppingCart,
      items: [
        {
          title: "All Orders",
          url: "/orders",
          isActive: true,
        },
        {
          title: "Pending Orders",
          url: "/orders?status=0",
        },
        {
          title: "Shipped Orders",
          url: "/orders?status=3",
        },
        {
          title: "Cancelled Orders",
          url: "/orders?status=5",
        },
      ],
    },
    {
      title: "User Management",
      url: "#",
      icon: Users,
      items: [
        {
          title: "All Users",
          url: "/users",
        },
        {
          title: "Roles & Permissions",
          url: "/users/roles",
        },
        {
          title: "User Activity",
          url: "/users/activity",
        },
      ],
    },
    {
      title: "System Settings",
      url: "#",
      icon: Settings,
      items: [
        {
          title: "General Settings",
          url: "/settings",
        },
        {
          title: "OAuth Clients",
          url: "/settings/oauth",
        },
        {
          title: "API Keys",
          url: "/settings/api-keys",
        },
        {
          title: "Audit Logs",
          url: "/settings/audit",
        },
      ],
    },
    {
      title: "Admin Panel",
      url: "#",
      icon: Shield,
      adminOnly: true,
      items: [
        {
          title: "System Overview",
          url: "/admin",
        },
        {
          title: "Advanced Settings",
          url: "/admin/settings",
        },
        {
          title: "System Diagnostics",
          url: "/admin/diagnostics",
        },
      ],
    },
  ],
}

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { user, logout } = useAuth();

  const handleLogout = async () => {
    try {
      await logout();
      // Redirect to login page after logout
      window.location.href = '/login';
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  return (
    <Sidebar {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild>
              <a href="#">
                <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
                  <GalleryVerticalEnd className="size-4" />
                </div>
                <div className="flex flex-col gap-0.5 leading-none">
                  <span className="font-medium">Identity Admin</span>
                  <span className="">v1.0.0</span>
                </div>
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
        <SearchForm />
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {data.navMain.filter(item => !item.adminOnly || isAdmin(user)).map((item, index) => (
              <Collapsible
                key={item.title}
                defaultOpen={index === 1}
                className="group/collapsible"
              >
                <SidebarMenuItem>
                  <CollapsibleTrigger asChild>
                    <SidebarMenuButton>
                      {item.icon && <item.icon className="mr-2 h-4 w-4" />}
                      {item.title}{" "}
                      <Plus className="ml-auto group-data-[state=open]/collapsible:hidden" />
                      <Minus className="ml-auto group-data-[state=closed]/collapsible:hidden" />
                    </SidebarMenuButton>
                  </CollapsibleTrigger>
                  {item.items?.length ? (
                    <CollapsibleContent>
                      <SidebarMenuSub>
                        {item.items.map((item) => (
                          <SidebarMenuSubItem key={item.title}>
                            <SidebarMenuSubButton
                              asChild
                              isActive={'isActive' in item ? item.isActive : false}
                            >
                              <a href={item.url}>{item.title}</a>
                            </SidebarMenuSubButton>
                          </SidebarMenuSubItem>
                        ))}
                      </SidebarMenuSub>
                    </CollapsibleContent>
                  ) : null}
                </SidebarMenuItem>
              </Collapsible>
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        {user && (
          <NavUser 
            user={{
              name: `${user.firstName} ${user.lastName}`,
              email: user.email,
              avatar: `https://api.dicebear.com/7.x/initials/svg?seed=${encodeURIComponent(`${user.firstName} ${user.lastName}`)}`
            }}
            onLogout={handleLogout}
          />
        )}
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
