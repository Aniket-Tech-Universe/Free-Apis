import Head from "next/head";
import { Button } from "@heroui/button";
import { Card, CardBody } from "@heroui/card";
import { Input, Textarea } from "@heroui/input";
import { Checkbox } from "@heroui/checkbox";
import { Modal, ModalContent, ModalHeader, ModalBody, ModalFooter } from "@heroui/modal";
import { HeartIcon, SparklesIcon, CheckCircleIcon } from "@heroicons/react/24/outline";
import NextLink from "next/link";
import { useState, useEffect } from "react";
import { useRouter } from "next/router";

import { title, subtitle } from "@/components/primitives";
import DefaultLayout from "@/layouts/default";
import { fetchDiscordUser } from "@/utils/discordAuth";

export default function DonatedPage() {
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [websiteUrl, setWebsiteUrl] = useState("");
  const [includeWebsite, setIncludeWebsite] = useState(false);
  const [discordUsername, setDiscordUsername] = useState("");
  const [showOnSupportersPage, setShowOnSupportersPage] = useState(true);
  const [notes, setNotes] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [discordUser, setDiscordUser] = useState<any>(null);
  const [showLeaveWarning, setShowLeaveWarning] = useState(false);
  const [hasStartedForm, setHasStartedForm] = useState(false);
  const [transactionId, setTransactionId] = useState("");
  const [trackingId, setTrackingId] = useState("");

  // Get transaction details from URL params
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const txnId = params.get('txn_id') || '';
    const trkId = params.get('tracking_id') || '';
    setTransactionId(txnId);
    setTrackingId(trkId);
  }, []);

  // Check if user is logged into Discord
  useEffect(() => {
    const checkDiscordUser = async () => {
      const user = await fetchDiscordUser();
      if (user) {
        setDiscordUser(user);
      }
    };
    checkDiscordUser();
  }, []);

  // Track if user has started filling the form
  useEffect(() => {
    if (displayName || websiteUrl || discordUsername || !showOnSupportersPage || notes) {
      setHasStartedForm(true);
    }
  }, [displayName, websiteUrl, discordUsername, showOnSupportersPage, notes]);

  // Warn before leaving if form started but not submitted
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasStartedForm && !isSubmitted) {
        e.preventDefault();
        e.returnValue = '';
      }
    };

    const handleRouteChange = (url: string) => {
      if (hasStartedForm && !isSubmitted && !window.confirm(
        "⚠️ If you leave this page, you won't be able to get back to submit your supporter information! " +
        "You'll miss out on being recognized for your generous donation. Are you sure you want to leave?"
      )) {
        router.events.emit('routeChangeError');
        throw 'Route change aborted';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    router.events.on('routeChangeStart', handleRouteChange);

    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
      router.events.off('routeChangeStart', handleRouteChange);
    };
  }, [hasStartedForm, isSubmitted, router]);

  const handleSubmit = async () => {
    if (!displayName.trim()) {
      alert("Please enter a display name!");
      return;
    }

    setIsSubmitting(true);
    try {
      const headers: any = {
        'Content-Type': 'application/json',
      };

      // Add Discord ID if logged in
      if (discordUser?.discordId) {
        headers['X-Discord-Id'] = discordUser.discordId;
      }

      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/API/SubmitSupporterInfo`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          trackingId,
          transactionId,
          displayName: displayName.trim(),
          websiteUrl: includeWebsite && websiteUrl.trim() ? websiteUrl.trim() : null,
          discordUsername: !discordUser && discordUsername.trim() ? discordUsername.trim() : null,
          showOnSupportersPage,
          notes: notes.trim() || null
        })
      });

      const result = await response.json();
      
      if (result.success) {
        setIsSubmitted(true);
      } else {
        alert(result.message || "Failed to save supporter information. Please try again.");
      }
    } catch (error) {
      console.error('Error submitting supporter info:', error);
      alert("An error occurred. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <Head>
        <title>Thank You! - Unsecured API Keys</title>
        <meta
          name="description"
          content="Thank you for your donation! Your generosity helps keep our API key security tools running and free for everyone."
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="keywords" content="thank you, donation, support, API key security, gratitude" />
        <link rel="canonical" href="https://unsecuredapikeys.com/donated/" />
        
        {/* Open Graph Meta Tags */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Unsecured API Keys" />
        <meta property="og:title" content="Thank You! - Unsecured API Keys" />
        <meta property="og:description" content="Thank you for your donation! Your generosity helps keep our security tools running." />
        <meta property="og:url" content="https://unsecuredapikeys.com/donated/" />
        <meta property="og:image" content="https://unsecuredapikeys.com/og-image.png" />
        
        {/* Twitter Card Meta Tags */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="Thank You! - Unsecured API Keys" />
        <meta name="twitter:description" content="Thank you for your donation! Your generosity helps keep our security tools running." />
        
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <DefaultLayout>
        <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
          <div className="inline-block max-w-4xl text-center justify-center">
            <div className="animate-bounce mb-8">
              <SparklesIcon className="h-20 w-20 mx-auto text-pink-500 mb-4" />
            </div>
            
            <h1 className={title({ color: "pink" })}>Holy Shit, You Actually Did It!</h1>
            <h2 className={subtitle({ class: "mt-4" })}>
              We&apos;re genuinely shocked and incredibly grateful 🥺
              <br />
              <span className="italic text-pink-500">
                (No, seriously, we didn&apos;t think anyone would actually donate)
              </span>
            </h2>
          </div>

          <div className="max-w-3xl mt-8 space-y-8">
            {!isSubmitted ? (
              <>
                <Card className="border-2 border-pink-200 bg-gradient-to-br from-pink-50 to-rose-50 dark:from-pink-950/20 dark:to-rose-950/20">
                  <CardBody className="p-8 text-center space-y-6">
                    <HeartIcon className="h-16 w-16 mx-auto text-pink-500 animate-pulse" />
                    
                    <h3 className="text-2xl font-bold text-pink-600 dark:text-pink-400">
                      Want to Be Recognized for Your Awesomeness? 🏆
                    </h3>
                    
                    <p className="text-default-700 dark:text-default-300">
                      Fill out this completely optional form to be added to our supporters page 
                      and get the recognition you deserve! Plus, if you&apos;re on Discord, 
                      we can hook you up with a special role!
                    </p>

                    <div className="space-y-4 text-left">
                      <Input
                        label="Display Name"
                        placeholder="How should we credit you?"
                        value={displayName}
                        onChange={(e) => setDisplayName(e.target.value)}
                        isRequired
                        description="This is how you'll appear on our supporters page"
                      />

                      <div className="space-y-2">
                        <Checkbox
                          isSelected={includeWebsite}
                          onValueChange={setIncludeWebsite}
                        >
                          I have a website I&apos;d like to link
                        </Checkbox>
                        
                        {includeWebsite && (
                          <Input
                            label="Website URL"
                            placeholder="https://yourawesome.site"
                            value={websiteUrl}
                            onChange={(e) => setWebsiteUrl(e.target.value)}
                            type="url"
                            description="We'll link to your site from our supporters page"
                          />
                        )}
                      </div>

                      {discordUser ? (
                        <div className="bg-green-100 dark:bg-green-900/20 p-4 rounded-lg">
                          <p className="text-green-700 dark:text-green-300 font-medium">
                            ✅ Discord Connected: {discordUser.username}
                          </p>
                          <p className="text-sm text-green-600 dark:text-green-400 mt-1">
                            We&apos;ll associate your donation with your Discord account for special perks!
                          </p>
                        </div>
                      ) : (
                        <Input
                          label="Discord Username (optional)"
                          placeholder="YourDiscordName#1234 or @username"
                          value={discordUsername}
                          onChange={(e) => setDiscordUsername(e.target.value)}
                          description="So we can give you credit on Discord (and maybe a special role 😉)"
                        />
                      )}

                      <Textarea
                        label="Any message for us? (optional)"
                        placeholder="Anything you'd like to say..."
                        value={notes}
                        onChange={(e) => setNotes(e.target.value)}
                        minRows={2}
                      />

                      <Checkbox
                        isSelected={showOnSupportersPage}
                        onValueChange={setShowOnSupportersPage}
                      >
                        Show me on the public supporters page
                      </Checkbox>
                    </div>

                    <div className="flex gap-4 justify-center">
                      <Button
                        color="primary"
                        variant="shadow"
                        size="lg"
                        onPress={handleSubmit}
                        isLoading={isSubmitting}
                        isDisabled={!displayName.trim()}
                      >
                        Submit My Info
                      </Button>
                      
                      <Button
                        color="default"
                        variant="bordered"
                        size="lg"
                        onPress={() => setShowLeaveWarning(true)}
                      >
                        Skip This
                      </Button>
                    </div>

                    <p className="text-sm text-warning-600 dark:text-warning-400 font-medium">
                      ⚠️ Note: If you leave this page without submitting, you won&apos;t be able to come back!
                    </p>
                  </CardBody>
                </Card>
              </>
            ) : (
              <Card className="border-2 border-green-200 bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950/20 dark:to-emerald-950/20">
                <CardBody className="p-8 text-center space-y-6">
                  <CheckCircleIcon className="h-20 w-20 mx-auto text-green-500" />
                  
                  <h3 className="text-2xl font-bold text-green-600 dark:text-green-400">
                    Perfect! You&apos;re All Set! 🎉
                  </h3>
                  
                  <p className="text-default-700 dark:text-default-300">
                    Your supporter information has been saved. You&apos;ll appear on our 
                    supporters page soon{discordUser ? ", and we'll get you that Discord role ASAP" : ""}!
                  </p>
                  
                  <NextLink href="/">
                    <Button
                      color="primary"
                      variant="shadow"
                      size="lg"
                      className="mt-4"
                    >
                      Back to Finding Keys
                    </Button>
                  </NextLink>
                </CardBody>
              </Card>
            )}

            {!isSubmitted && (
              <>
                <Card className="hover:shadow-lg transition-shadow">
                  <CardBody className="p-6 text-center">
                    <h4 className="text-xl font-bold mb-3 text-primary">What&apos;s Next?</h4>
                    <p className="text-default-600 mb-4">
                      Keep being awesome! Continue helping us identify and report 
                      exposed API keys to make the web safer for everyone.
                    </p>
                  </CardBody>
                </Card>

                <div className="text-center bg-gradient-to-r from-pink-100 to-rose-100 dark:from-pink-900/20 dark:to-rose-900/20 rounded-lg p-8">
                  <h3 className="text-2xl font-bold mb-4 text-pink-600 dark:text-pink-400">
                    From the Bottom of Our Caffeinated Hearts ☕❤️
                  </h3>
                  <p className="text-default-700 dark:text-default-300 mb-4">
                    We know you could have spent your money on literally anything else 
                    (like that subscription service you forgot you have), but you chose 
                    to support our weird little security project instead.
                  </p>
                  <p className="text-default-700 dark:text-default-300 font-medium">
                    That means the world to us, and we promise to use your donation 
                    responsibly* to keep fighting the good fight against exposed API keys.
                  </p>
                  <p className="text-xs text-default-400 mt-2 italic">
                    *Responsibly = 90% server costs, 10% celebrating with pizza
                  </p>
                </div>
              </>
            )}
          </div>
        </section>

        {/* Leave Warning Modal */}
        <Modal isOpen={showLeaveWarning} onClose={() => setShowLeaveWarning(false)}>
          <ModalContent>
            <ModalHeader className="text-warning">
              Are You Sure You Want to Skip? 😢
            </ModalHeader>
            <ModalBody>
              <p>
                If you skip this form, you won&apos;t be able to come back later to submit your 
                supporter information. You&apos;ll miss out on:
              </p>
              <ul className="list-disc list-inside mt-2 space-y-1">
                <li>Being featured on our supporters page</li>
                <li>Getting a special Discord role (if applicable)</li>
                <li>The eternal gratitude and recognition you deserve</li>
              </ul>
              <p className="mt-4 font-medium">
                This is your only chance - are you sure you want to skip?
              </p>
            </ModalBody>
            <ModalFooter>
              <Button color="default" variant="light" onPress={() => setShowLeaveWarning(false)}>
                I&apos;ll Fill It Out
              </Button>
              <Button 
                color="warning" 
                onPress={() => {
                  setIsSubmitted(true);
                  setShowLeaveWarning(false);
                  router.push('/');
                }}
              >
                Yes, Skip It
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </DefaultLayout>
    </>
  );
}
