using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using RabbitMq_integration.CommunicationParty;

namespace RabbitMq_integration.Ar;

public class CommunicationPartyServiceAccessor
{
    private readonly ICommunicationPartyService _communicationPartyService;

        public CommunicationPartyServiceAccessor(ICommunicationPartyService communicationPartyService)
        {
            _communicationPartyService = communicationPartyService;
        }

        /// <summary>
        /// Calls <see cref="ICommunicationPartyService.GetCommunicationPartyDetailsAsync"/>, and throws the appropriate 
        /// <see cref="InvalidParameterException"/> when a CommunicationParty with the given HerId is not found, 
        /// or when the found CommunicationParty is not valid.
        /// </summary>
        /// <exception cref="InvalidParameterException">Thrown with Issue.Gf2 when a CommunicationParty with the given HerId is not found,
        /// or when a CommunicationParty with IsValidCommunicationParty == false is found.</exception>
        public async Task<CommunicationParty.CommunicationParty> GetValidCommunicationPartyAsync(int herId)
        {
            CommunicationParty.CommunicationParty communicationParty = null;
            try
            {
                communicationParty = await _communicationPartyService.GetCommunicationPartyDetailsAsync(herId);
            }
            catch (FaultException<GenericFault> ex) when (ex.Detail.ErrorCode == "InvalidHerIdSupplied")
            {
                Console.WriteLine("Did not find a CommunicationParty with the provided HerId in AR");
            }

            if (!communicationParty.IsValidCommunicationParty)
            {
                Console.WriteLine("A CommunicationParty with the given HerId was found but the CommunicationParty needs be of type Service or Person in order to have a Collaboration Protocol Profile or Agreement.");
            }

            return communicationParty;
        }

        /// <summary>
        /// Calls ICommunicationPartyService.GetCertificateForValidatingSignatureAsync, and throws the appropriate InvalidParameterException when
        /// a certificate is not found for the given HerId.
        /// </summary>
        /// <returns>The requested certificate as a B64 encoded string</returns>
        public async Task<string> GetCertificateForValidatingSignatureAsync(int herId)
        {
            return await DownloadCertificateOrThrow(_communicationPartyService.GetCertificateForValidatingSignatureAsync(herId), "signature validation");
        }

        /// <summary>
        /// Calls ICommunicationPartyService.GetCertificateForEncryptionAsync, and throws the appropriate InvalidParameterException when
        /// a certificate is not found for the given HerId.
        /// </summary>
        /// <returns>The requested certificate as a B64 encoded string</returns>
        public async Task<string> GetCertificateForEncryptionAsync(int herId)
        {
            return await DownloadCertificateOrThrow(_communicationPartyService.GetCertificateForEncryptionAsync(herId), "encryption");
        }

        /// <summary>
        /// The methods in CommunicationPartyService called above, intend to throw GenericFault with errorcode "CertificateNotFound" 
        /// when no certificate can be found, but in reality they also sometimes let null through. This method handles both cases by throwing a GF-6.
        /// </summary>
        private static async Task<string> DownloadCertificateOrThrow(Task<byte[]> getCertificateTask, string certificateType)
        {
            byte[] certificateBytes = new byte[] { };
            try
            {
                certificateBytes = await getCertificateTask;
            }
            catch (FaultException<GenericFault> e) when (e.Code.Name == "CertificateNotFound")
            {
                Console.WriteLine(e.Message);
            }

            if (certificateBytes == null)
            {
                Console.WriteLine($"Certificate of type '{certificateType}' was not found for the communication party. Please provide a certificate in AR.");
            }

            return Convert.ToBase64String(certificateBytes);
        }
}