﻿using System;
using System.Security.Cryptography;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Math.EC.Multiplier;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using ECCurve = Org.BouncyCastle.Math.EC.ECCurve;

namespace OpenVASP.CSharpClient.Cryptography
{
    public class ECDH_Key
    {
        private static SecureRandom _secureRandom = new SecureRandom();
        private static X9ECParameters _curve = SecNamedCurves.GetByName("secp256k1");
        private static ECDomainParameters _domain = new ECDomainParameters(_curve.Curve, _curve.G, _curve.N, _curve.H);

        private readonly AsymmetricCipherKeyPair _key;

        private ECDH_Key(AsymmetricCipherKeyPair key)
        {
            _key = key;

            PrivateKey = GetPrivateKey(_key);
            PublicKey = GetPublicKey(_key);
        }

        public string PrivateKey { get; }

        public string PublicKey { get; }

        public string GenerateSharedSecretHex(string pubKeyHex)
        {
            var pubKeyBytes = pubKeyHex.HexToByteArray();
            var q = _curve.Curve.DecodePoint(pubKeyBytes);
            var remotePublicKey = new ECPublicKeyParameters("ECDH", q, _domain);
            var agreement = new ECDHBasicAgreement();
            agreement.Init(_key.Private);

            var basicAgreementResult = agreement.CalculateAgreement(remotePublicKey);

            return BigIntegers.AsUnsignedByteArray(agreement.GetFieldSize(), basicAgreementResult).ToHex(true);
        }

        public static ECDH_Key GenerateKey()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDH");
            var derObjectIdentifier = SecNamedCurves.GetOid("secp256k1");
            generator.Init(new ECKeyGenerationParameters(derObjectIdentifier, _secureRandom));
            var generatedKey = generator.GenerateKeyPair();
            var key = new ECDH_Key(generatedKey);

            return key;
        }

        public static ECDH_Key ImportKey(string privateKeyHex)
        {
            var d = new BigInteger(1, privateKeyHex.HexToByteArray());
            ECPrivateKeyParameters privateKey = new ECPrivateKeyParameters(d, _domain);
            var q = (new FixedPointCombMultiplier()).Multiply(_domain.G, d);
            var pubKey = new ECPublicKeyParameters("ECDH", q, _domain);
            var importedKey = new AsymmetricCipherKeyPair(pubKey, privateKey);
            var key = new ECDH_Key(importedKey);

            return key;
        }

        private static string GetPublicKey(AsymmetricCipherKeyPair keyPair)
        {
            if (keyPair.Public is ECPublicKeyParameters dhPublicKeyParameters)
            {
                return dhPublicKeyParameters.Q.GetEncoded().ToHex(true);
            }
            throw new NullReferenceException("The key pair provided is not a valid ECDH keypair.");
        }

        // This returns a
        private static string GetPrivateKey(AsymmetricCipherKeyPair keyPair)
        {
            if (keyPair.Private is ECPrivateKeyParameters dhPrivateKeyParameters)
            {
                var bytes = dhPrivateKeyParameters.D.ToByteArrayUnsigned();
                return bytes.ToHex(prefix: true);
            }
            throw new NullReferenceException("The key pair provided is not a valid ECDH keypair.");
        }
    }
}