import { useState, useEffect } from "react";
import { Button } from "@heroui/button";
import { Input } from "@heroui/input";
import { Card, CardBody, CardHeader } from "@heroui/card";
import { Divider } from "@heroui/divider";
import Head from "next/head";
import DefaultLayout from "@/layouts/default";
import { confirmDonation, getDonationStats } from "@/utils/donationTracking";

interface DonationStats {
  totalClicks: number;
  totalDonations: number;
  totalDonationAmount: number;
  clicksToday: number;
  clicksThisWeek: number;
  clicksThisMonth: number;
  uniqueClickersToday: number;
  clicksByLocation: Array<{ location: string; count: number }>;
  recentClicks: Array<{
    clickedAt: string;
    clickLocation: string;
    confirmedDonation: boolean;
    donationAmount?: number;
  }>;
}

export default function DonationAdminPage() {
  const [stats, setStats] = useState<DonationStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  
  // Form state
  const [amount, setAmount] = useState("");
  const [transactionId, setTransactionId] = useState("");
  const [notes, setNotes] = useState("");
  const [message, setMessage] = useState("");

  useEffect(() => {
    fetchStats();
  }, []);

  const fetchStats = async () => {
    setLoading(true);
    try {
      const data = await getDonationStats();
      if (data) {
        setStats(data as DonationStats);
      }
    } catch (error) {
      console.error("Failed to fetch donation stats:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!amount || isNaN(parseFloat(amount))) {
      setMessage("Please enter a valid amount");
      return;
    }

    setSubmitting(true);
    setMessage("");

    try {
      const success = await confirmDonation(
        parseFloat(amount),
        transactionId || undefined,
        notes || undefined
      );

      if (success) {
        setMessage("Donation confirmed successfully! 🎉");
        setAmount("");
        setTransactionId("");
        setNotes("");
        // Refresh stats
        await fetchStats();
      } else {
        setMessage("Failed to confirm donation. Please try again.");
      }
    } catch (error) {
      setMessage("Error confirming donation. Please try again.");
      console.error("Error:", error);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Head>
        <title>Donation Admin - Unsecured API Keys</title>
        <meta name="robots" content="noindex, nofollow" />
      </Head>

      <DefaultLayout>
        <div className="container mx-auto px-4 py-8 max-w-6xl">
          <div className="text-center mb-8">
            <h1 className="text-4xl font-bold mb-4">💸 Donation Admin Panel</h1>
            <p className="text-default-600">
              Track and manage donations from generous developers who appreciate our chaos
            </p>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* Manual Donation Entry */}
            <Card className="h-fit">
              <CardHeader>
                <h2 className="text-2xl font-bold">Manual Donation Entry</h2>
              </CardHeader>
              <CardBody>
                <form onSubmit={handleSubmit} className="space-y-4">
                  <Input
                    label="Donation Amount ($)"
                    placeholder="5.00"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    type="number"
                    step="0.01"
                    min="0"
                    required
                    startContent={
                      <div className="pointer-events-none flex items-center">
                        <span className="text-default-400 text-small">$</span>
                      </div>
                    }
                  />
                  
                  <Input
                    label="PayPal Transaction ID (Optional)"
                    placeholder="1AB23456CD789012E"
                    value={transactionId}
                    onChange={(e) => setTransactionId(e.target.value)}
                  />
                  
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-default-700">
                      Notes (Optional)
                    </label>
                    <textarea
                      className="w-full p-3 border border-default-300 rounded-lg resize-none focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent"
                      placeholder="Any additional information about this donation..."
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      rows={3}
                    />
                  </div>
                  
                  <Button
                    type="submit"
                    color="primary"
                    size="lg"
                    isLoading={submitting}
                    className="w-full"
                  >
                    {submitting ? "Confirming..." : "Confirm Donation"}
                  </Button>
                  
                  {message && (
                    <div className={`p-3 rounded-lg text-center ${
                      message.includes("successfully") 
                        ? "bg-success/10 text-success border border-success/20" 
                        : "bg-danger/10 text-danger border border-danger/20"
                    }`}>
                      {message}
                    </div>
                  )}
                </form>
              </CardBody>
            </Card>

            {/* Statistics Overview */}
            <Card>
              <CardHeader>
                <h2 className="text-2xl font-bold">Donation Statistics</h2>
              </CardHeader>
              <CardBody>
                {loading ? (
                  <div className="space-y-4">
                    {[...Array(6)].map((_, i) => (
                      <div key={i} className="animate-pulse">
                        <div className="h-4 bg-default-200 rounded w-3/4"></div>
                      </div>
                    ))}
                  </div>
                ) : stats ? (
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="text-center p-3 bg-primary/10 rounded-lg">
                        <div className="text-2xl font-bold text-primary">
                          {stats.totalClicks.toLocaleString()}
                        </div>
                        <div className="text-sm text-default-600">Total Clicks</div>
                      </div>
                      
                      <div className="text-center p-3 bg-success/10 rounded-lg">
                        <div className="text-2xl font-bold text-success">
                          ${stats.totalDonationAmount.toFixed(2)}
                        </div>
                        <div className="text-sm text-default-600">Total Donated</div>
                      </div>
                      
                      <div className="text-center p-3 bg-warning/10 rounded-lg">
                        <div className="text-2xl font-bold text-warning">
                          {stats.totalDonations}
                        </div>
                        <div className="text-sm text-default-600">Confirmed Donations</div>
                      </div>
                      
                      <div className="text-center p-3 bg-secondary/10 rounded-lg">
                        <div className="text-2xl font-bold text-secondary">
                          {stats.clicksToday}
                        </div>
                        <div className="text-sm text-default-600">Clicks Today</div>
                      </div>
                    </div>
                    
                    <Divider />
                    
                    <div>
                      <h3 className="font-semibold mb-2">Clicks by Location</h3>
                      <div className="space-y-2">
                        {stats.clicksByLocation.map((location, index) => (
                          <div key={index} className="flex justify-between items-center">
                            <span className="px-2 py-1 bg-default-100 text-default-700 rounded-full text-sm">
                              {location.location}
                            </span>
                            <span className="text-sm font-medium">
                              {location.count} clicks
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                    
                    <Button
                      onClick={fetchStats}
                      variant="bordered"
                      size="sm"
                      className="w-full"
                    >
                      Refresh Stats
                    </Button>
                  </div>
                ) : (
                  <div className="text-center text-default-500">
                    Failed to load statistics
                  </div>
                )}
              </CardBody>
            </Card>
          </div>

          {/* Recent Activity */}
          {stats && stats.recentClicks.length > 0 && (
            <Card className="mt-8">
              <CardHeader>
                <h2 className="text-2xl font-bold">Recent Activity</h2>
              </CardHeader>
              <CardBody>
                <div className="space-y-3">
                  {stats.recentClicks.map((click, index) => (
                    <div
                      key={index}
                      className="flex justify-between items-center p-3 bg-default-50 rounded-lg"
                    >
                      <div>
                        <div className="font-medium">
                          Click at {click.clickLocation}
                        </div>
                        <div className="text-sm text-default-500">
                          {new Date(click.clickedAt).toLocaleString()}
                        </div>
                      </div>
                      <div className="text-right">
                        {click.confirmedDonation ? (
                          <span className="px-2 py-1 bg-success/10 text-success rounded-full text-sm border border-success/20">
                            ${click.donationAmount?.toFixed(2) || "0.00"} donated
                          </span>
                        ) : (
                          <span className="px-2 py-1 bg-default-100 text-default-600 rounded-full text-sm">
                            No donation
                          </span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </CardBody>
            </Card>
          )}
        </div>
      </DefaultLayout>
    </>
  );
}
