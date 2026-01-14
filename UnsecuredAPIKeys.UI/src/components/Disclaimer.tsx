import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
} from "@heroui/modal";
import { Button } from "@heroui/button";

export default function Disclaimer({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      placement="center"
      size="2xl"
      isDismissable={false}
      hideCloseButton
      classNames={{
        backdrop: "bg-black/80 backdrop-blur-sm",
        wrapper: "z-[99999]",
      }}
    >
      <ModalContent>
        {() => (
          <>
            <ModalHeader className="flex flex-col gap-1 pb-4">
              <div className="flex items-center gap-3">
                <span className="text-2xl">🔐</span>
                <h2 className="text-xl font-bold">Important Security Notice</h2>
              </div>
            </ModalHeader>
            <ModalBody className="gap-4 pb-6">
              <div className="space-y-4">
                <div className="bg-warning/10 border border-warning/20 rounded-lg p-4">
                  <p className="text-sm font-medium text-warning">
                    ⚠️ Educational Purpose Only
                  </p>
                  <p className="text-sm text-default-600 mt-1">
                    This website is designed to help developers identify and secure exposed API keys in public repositories.
                  </p>
                </div>

                <div className="space-y-3">
                  <div className="flex gap-2 items-start">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-danger/10 text-danger">
                      Legal
                    </span>
                    <p className="text-sm text-default-600">
                      We are not responsible for any misuse of information found on this site. Users are expected to act ethically and legally.
                    </p>
                  </div>

                  <div className="flex gap-2 items-start">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-primary/10 text-primary">
                      Purpose
                    </span>
                    <p className="text-sm text-default-600">
                      Our goal is to improve security awareness by helping developers find and fix exposed credentials before malicious actors do.
                    </p>
                  </div>

                  <div className="flex gap-2 items-start">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-secondary/10 text-secondary">
                      Privacy
                    </span>
                    <p className="text-sm text-default-600">
                      When submitting issues, we may track basic analytics to improve our service. No personal data is stored or shared.
                    </p>
                  </div>
                </div>

                <div className="bg-default-100 rounded-lg p-4 mt-4">
                  <p className="text-xs text-default-500 italic">
                    By using this site, you agree to use the information responsibly and help make the internet more secure.
                  </p>
                </div>
              </div>
            </ModalBody>
            <ModalFooter className="pt-2">
              <Button
                color="primary"
                onPress={onClose}
                className="font-medium"
              >
                I Understand & Accept
              </Button>
            </ModalFooter>
          </>
        )}
      </ModalContent>
    </Modal>
  );
}
