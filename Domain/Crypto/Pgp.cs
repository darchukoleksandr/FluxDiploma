using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cinchoo.PGP;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;
using PgpKeyPair = Domain.Models.PgpKeyPair;

namespace Domain.Crypto
{
    public class Pgp : ChoPGPEncryptDecrypt
    {
        private static byte[] Compress(byte[] clearData, string fileName, CompressionAlgorithmTag algorithm)
        {
            using (var bOut = new MemoryStream())
            {
                PgpCompressedDataGenerator comData = new PgpCompressedDataGenerator(algorithm);
                Stream cos = comData.Open(bOut); // open it with the final destination
                PgpLiteralDataGenerator lData = new PgpLiteralDataGenerator();

                // we want to Generate compressed data. This might be a user option later,
                // in which case we would pass in bOut.
                Stream pOut = lData.Open(
                    cos,                    // the compressed output stream
                    PgpLiteralData.Binary,
                    fileName,               // "filename" to store
                    clearData.Length,       // length of clear data
                    DateTime.UtcNow         // current time
                );

                pOut.Write(clearData, 0, clearData.Length);
                pOut.Close();

                comData.Close();

                return bOut.ToArray();
            }
        }

        public string EncryptString(string text, IEnumerable<byte[]> recipientsPubKeys, bool withIntegrityCheck = true, bool armor = true)
        {
            byte[] processedData = Compress(Encoding.UTF8.GetBytes(text), PgpLiteralData.Console, CompressionAlgorithmTag.Uncompressed);

            using (var bOut = new MemoryStream())
            {
                using (var output = new ArmoredOutputStream(bOut))
                {
                    PgpEncryptedDataGenerator encGen = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, withIntegrityCheck, new SecureRandom());
                    foreach (var recipientsPubKey in recipientsPubKeys)
                    {
                        encGen.AddMethod(ReadPublicKey(recipientsPubKey));
                    }

                    using (var encOut = encGen.Open(output, processedData.Length))
                    {
                        encOut.Write(processedData, 0, processedData.Length);
                    }
                }

                return Encoding.UTF8.GetString(bOut.ToArray());
            }
        }

        public string DecryptString(string chiperText, byte[] privateKey, string passCode = null)
        {
            passCode = passCode ?? string.Empty;

            var result = Decrypt(Encoding.UTF8.GetBytes(chiperText), privateKey, passCode);

            return Encoding.UTF8.GetString(result);
        }

        private byte[] Decrypt(byte[] inputData, byte[] privateKey, string passCode = null)
        {
            passCode = passCode ?? string.Empty;

            using (var inputStream = new MemoryStream(inputData))
            {
                using (var decoderStream = PgpUtilities.GetDecoderStream(inputStream))
                {
                    using (var decoded = new MemoryStream())
                    {
                        try
                        {
                            PgpObjectFactory pgpF = new PgpObjectFactory(decoderStream);
                            PgpEncryptedDataList enc;
                            PgpObject o = pgpF.NextPgpObject();

                            //
                            // the first object might be a PGP marker packet.
                            //
                            if (o is PgpEncryptedDataList)
                                enc = (PgpEncryptedDataList)o;
                            else
                                enc = (PgpEncryptedDataList)pgpF.NextPgpObject();

                            //
                            // find the secret key
                            //
                            PgpPrivateKey sKey = null;
                            PgpPublicKeyEncryptedData pbe = null;
                            var pgpSec = new PgpSecretKeyRingBundle(privateKey);
                            //                var pgpSec = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(keyIn));
                            foreach (PgpPublicKeyEncryptedData pked in enc.GetEncryptedDataObjects())
                            {
                                PgpSecretKey pgpSecKey = pgpSec.GetSecretKey(pked.KeyId);
                                sKey = pgpSecKey?.ExtractPrivateKey(passCode.ToCharArray());

                                if (sKey != null)
                                {
                                    pbe = pked;
                                    break;
                                }
                            }
                            if (sKey == null)
                                throw new ArgumentException("secret key for message not found.");

                            Stream clear = pbe.GetDataStream(sKey);
                            PgpObjectFactory plainFact = new PgpObjectFactory(clear);
                            PgpObject message = plainFact.NextPgpObject();

                            if (message is PgpCompressedData)
                            {
                                PgpCompressedData cData = (PgpCompressedData)message;
                                PgpObjectFactory pgpFact = new PgpObjectFactory(cData.GetDataStream());
                                message = pgpFact.NextPgpObject();
                            }
                            if (message is PgpLiteralData)
                            {
                                PgpLiteralData ld = (PgpLiteralData)message;
                                Stream unc = ld.GetInputStream();
                                Streams.PipeAll(unc, decoded);
                            }
                            else if (message is PgpOnePassSignatureList)
                                throw new PgpException("encrypted message contains a signed message - not literal data.");
                            else
                                throw new PgpException("message is not a simple encrypted file - type unknown.");

                            if (pbe.IsIntegrityProtected())
                            {
                                //                    if (!pbe.Verify())
                                //                        MessageBox.Show(null, "Message failed integrity check.", "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                //                    else
                                //                        MessageBox.Show(null, "Message integrity check passed.", "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                //                    MessageBox.Show(null, "No message integrity check.", "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                            return decoded.ToArray();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Decryption went wrong: {e.Message}");
                            //                if (e.Message.StartsWith("Checksum mismatch"))
                            //                    MessageBox.Show(null, "Likely invalid passcode. Possible data corruption.", "Invalid Passcode", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //                else if (e.Message.StartsWith("Object reference not"))
                            //                    MessageBox.Show(null, "PGP data does not exist.", "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //                else if (e.Message.StartsWith("Premature end of stream"))
                            //                    MessageBox.Show(null, "Partial PGP data found.", "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //                else
                            //                    MessageBox.Show(null, e.Message, "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //                Exception underlyingException = e.InnerException;
                            //                if (underlyingException != null)
                            //                    MessageBox.Show(null, underlyingException.Message, "PGP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            throw e;
                        }
                    }
                }
            }
        }

        private PgpPublicKey ReadPublicKey(byte[] encodedPublicKey)
        {
            var pgpPub = new PgpPublicKeyRingBundle(encodedPublicKey);

            foreach (PgpPublicKeyRing kRing in pgpPub.GetKeyRings())
            {
                foreach (PgpPublicKey k in kRing.GetPublicKeys())
                {
                    if (k.IsEncryptionKey)
                        return k;
                }
            }

            throw new ArgumentException("Can't find encryption key in key ring.");
        }

        public PgpKeyPair GenerateKeyPair(SymmetricKeyAlgorithmTag symmetricKeyAlgorithmTag, string owner, int keyLength = 1024)
        {
            IAsymmetricCipherKeyPairGenerator kpg = new RsaKeyPairGenerator();
            kpg.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x13), new SecureRandom(), keyLength, 8));
            AsymmetricCipherKeyPair kp = kpg.GenerateKeyPair();

            var pgpSecretKey = new PgpSecretKey(
                PgpSignature.DefaultCertification,
                PublicKeyAlgorithmTag.RsaGeneral,
                kp.Public,
                kp.Private,
                DateTime.Now,
                owner,
                symmetricKeyAlgorithmTag,
                string.Empty.ToCharArray(),
                null,
                null,
                new SecureRandom()
            );

            byte[] privateKey;
            byte[] publicKey;

//            using (var memoryStream = new MemoryStream())
//            {
//                using (var armoredOutputStream = new ArmoredOutputStream(memoryStream))
//                {
//                    pgpSecretKey.Encode(armoredOutputStream);
//                }
//            
//                privateKey = memoryStream.ToArray();
//            }
            
//            using (var memoryStream = new MemoryStream())
//            {
//                using (var armoredOutputStream = new ArmoredOutputStream(memoryStream))
//                {
//                    pgpSecretKey.PublicKey.Encode(armoredOutputStream);
//                }
//                        
//                publicKey = memoryStream.ToArray();
//            }

            privateKey = pgpSecretKey.GetEncoded();
            publicKey = pgpSecretKey.PublicKey.GetEncoded();

            return new PgpKeyPair
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                Owner = owner
            };
//            return new PgpKeyPair(publicKey, privateKey, owner);
        }
    }
}