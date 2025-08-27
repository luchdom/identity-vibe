import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { 
  LogOut,
  Bell,
  Search,
  Users,
  Database,
  Shield,
  TrendingUp,
  Activity,
  Server,
  User
} from 'lucide-react';

import { Input } from '@/components/ui/input';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuSeparator, 
  DropdownMenuTrigger 
} from '@/components/ui/dropdown-menu';
import { 
  SidebarInset, 
  SidebarProvider, 
  SidebarTrigger 
} from '@/components/ui/sidebar';
import { AppSidebar } from '@/components/AppSidebar';
import { ModeToggle } from '@/components/ModeToggle';
import { useAuth } from '@/contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

const DashboardPage = () => {
  const [isLoadingData] = useState(false);
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logout();
      toast.success('Logged out successfully');
      navigate('/login', { replace: true });
    } catch (error) {
      toast.error('Failed to logout');
    }
  };

  const stats = [
    {
      title: 'Total Users',
      value: '2,345',
      icon: Users,
      description: '+20.1% from last month',
      trend: 'up',
    },
    {
      title: 'Active Sessions',
      value: '145',
      icon: Activity,
      description: '+10% from last hour',
      trend: 'up',
    },
    {
      title: 'System Status',
      value: 'Healthy',
      icon: Shield,
      description: 'All services operational',
      trend: 'stable',
    },
    {
      title: 'Database',
      value: '99.9%',
      icon: Database,
      description: 'Uptime this month',
      trend: 'stable',
    },
  ];

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        {/* Header */}
        <header className="flex h-16 shrink-0 items-center gap-2 transition-[width,height] ease-linear group-has-[[data-collapsible=icon]]/sidebar-wrapper:h-12">
          <div className="flex items-center gap-2 px-4">
            <SidebarTrigger className="-ml-1" />
            {/* Search */}
            <div className="relative hidden sm:flex">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search..."
                className="w-64 pl-10"
              />
            </div>
          </div>

          <div className="ml-auto flex items-center gap-4 px-4">
            {/* Theme toggle */}
            <ModeToggle />
            
            {/* Logout button */}
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={handleLogout}
              className="flex items-center gap-2"
            >
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Logout</span>
            </Button>

            {/* Notifications */}
            <Button variant="ghost" size="icon">
              <Bell className="h-5 w-5" />
            </Button>

            {/* User menu */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="relative h-8 w-8 rounded-full">
                  <Avatar className="h-8 w-8">
                    <AvatarFallback>
                      {user?.email?.[0]?.toUpperCase() || 'U'}
                    </AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <div className="flex items-center justify-start gap-2 p-2">
                  <div className="flex flex-col space-y-1 leading-none">
                    {user?.name && (
                      <p className="font-medium">{user.name}</p>
                    )}
                    <p className="w-[200px] truncate text-sm text-muted-foreground">
                      {user?.email}
                    </p>
                  </div>
                </div>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  <a href="/profile" className="flex items-center w-full">
                    <span>Profile</span>
                  </a>
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <span>Settings</span>
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleLogout}>
                  <LogOut className="mr-2 h-4 w-4" />
                  <span>Log out</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto p-4 lg:p-8">
          <div className="max-w-7xl mx-auto space-y-8">
            {/* Welcome Section */}
            <div>
              <h1 className="text-3xl font-bold tracking-tight">
                Welcome back, {user?.name || 'User'}!
              </h1>
              <p className="text-muted-foreground">
                Here's what's happening with your system today.
              </p>
            </div>

            {/* Stats Grid */}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 xl:gap-6">
              {stats.map((stat) => (
                <Card key={stat.title}>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">
                      {stat.title}
                    </CardTitle>
                    <stat.icon className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{stat.value}</div>
                    <p className="text-xs text-muted-foreground flex items-center">
                      {stat.trend === 'up' && (
                        <TrendingUp className="h-3 w-3 mr-1 text-green-500" />
                      )}
                      {stat.description}
                    </p>
                  </CardContent>
                </Card>
              ))}
            </div>

            <div className="grid gap-6 lg:grid-cols-2 xl:gap-8">
              {/* User Information */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <User className="h-5 w-5" />
                    User Information
                  </CardTitle>
                  <CardDescription>
                    Your account details and authentication status
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid gap-2">
                    <div className="flex justify-between">
                      <span className="font-medium">Email:</span>
                      <span className="text-muted-foreground">{user?.email}</span>
                    </div>
                    {user?.name && (
                      <div className="flex justify-between">
                        <span className="font-medium">Name:</span>
                        <span className="text-muted-foreground">{user.name}</span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span className="font-medium">User ID:</span>
                      <span className="text-muted-foreground font-mono text-sm">
                        {user?.id}
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* API Data */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Server className="h-5 w-5" />
                    API Connection
                  </CardTitle>
                  <CardDescription>
                    Data from your backend services
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {isLoadingData ? (
                    <div className="space-y-2">
                      <Skeleton className="h-4 w-full" />
                      <Skeleton className="h-4 w-3/4" />
                      <Skeleton className="h-4 w-1/2" />
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <div>
                        <span className="font-medium">Status: </span>
                        <span className="text-muted-foreground">Connected</span>
                      </div>
                      <div>
                        <span className="font-medium">Last Updated: </span>
                        <span className="text-muted-foreground font-mono text-sm">
                          {new Date().toLocaleString()}
                        </span>
                      </div>
                    </div>
                  )}
                  
                  <Button 
                    variant="outline"
                    size="sm"
                    className="w-full"
                  >
                    Refresh Data
                  </Button>
                </CardContent>
              </Card>
            </div>
          </div>
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
};

export default DashboardPage;