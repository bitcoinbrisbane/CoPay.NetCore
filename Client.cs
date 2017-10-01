using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CoPay
{
    public class Client
    {
        private readonly string BWS_INSTANCE_URL = "https://bws.bitpay.com/bws/api";

        public Client(String baseUrl = "https://bws.bitpay.com/bws/api")
        {
            BWS_INSTANCE_URL = baseUrl;
        }

        public async Task<string> createWallet(String walletName, String copayerName, Int16 m, Int16 n, opts opts)
        {
            Cred cred = new Cred()
            {
                copayerId = copayerName,
                network = "testnet",
                requestPrivKey = "tprv8dxkXXLevuHXR3tLvBkaDLyCnQxsQQVafnDMEQNds8r8tjSPfNTGD5ShtpP8QeTdtCoWGmrMC5gs9j7ap8ATdSsAD2KCv87BGdzPWwmdJt2",
                xPrivKey = "cNaQCDwmmh4dS9LzCgVtyy1e1xjCJ21GUDHe9K98nzb689JvinGV"
            };

            CreateWallet.Request request = new CreateWallet.Request()
            {
                m = m,
                n = n,
                name = walletName,
                pubKey = "02fcba7ecf41bc7e1be4ee122d9d22e3333671eb0a3a87b5cdf099d59874e1940f",
                network = "testnet"
            };

            String url = BWS_INSTANCE_URL + "/v2/wallets/";
            String reqSignature = Utils.signRequest("GET", url, request, cred.xPrivKey);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-identity", cred.copayerId);
            client.DefaultRequestHeaders.Add("x-signature", reqSignature);

            String json = JsonConvert.SerializeObject(request);
            StringContent requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpResponseMessage responseMessage = await client.PostAsync(url, requestContent))
            {
                if (responseMessage.IsSuccessStatusCode)
                {
                    String responseContent = await responseMessage.Content.ReadAsStringAsync();

                    CreateWallet.Response response = JsonConvert.DeserializeObject<CreateWallet.Response>(responseContent);
                    String share = buildSecret(response.walletId, cred.xPrivKey, "testnet");
                    return share;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public async Task<JoinWallet.Response> doJoinWallet(Guid walletId, String walletPrivKey, String xPubKey, String copayerName)
        {
            string reqPubKey = "03d222074bb076cf3b0b47e8a562d106cc833e39d95f1479600faeaed702613e93";
            var reqPrivKey = "L5iiNZ4PxhbLmiyHqtJHiYZzFYN7oW7ebmZmiYeoS1ifjQcGU4XT";

            var key = NBitcoin.Key.Parse(reqPrivKey, NBitcoin.Network.Main);

            var wkey = NBitcoin.Key.Parse(walletPrivKey, NBitcoin.Network.Main);

            String sharedEncryptingKey = Utils.PrivateKeyToAESKey(walletPrivKey);
            
            var encCopayerName = Utils.encryptMessage(copayerName, sharedEncryptingKey);

            var hash = Utils.getCopayerHash(copayerName, xPubKey, reqPubKey);

            // var copayerSignature = Utils.signMessage(hash, wkey);
            var copayerSignature = "3045022100c489117f1e2132dd827a0bc98af7f5a429ef57dce2dcb3cd6d362c42a9cd13ed0220124d2e7c103ee8179ad09e025e8f0cdd7954b28593ad6df8a4620952e7d31e9f";

            var request = new JoinWallet.Request {
                walletId = walletId.ToString(),
                name = copayerName,
                xPubKey = xPubKey,
                requestPubKey = reqPubKey,
                copayerSignature = copayerSignature
            };
            
            var url = BWS_INSTANCE_URL + String.Format("/v2/wallets/{0}/copayers", walletId);
            String reqSignature = Utils.signRequest("GET", url, request, reqPrivKey);
            var copayerId = Utils.XPubKeyToCopayerId(xPubKey);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-identity", copayerId);
            client.DefaultRequestHeaders.Add("x-signature", reqSignature);

            var json = JsonConvert.SerializeObject(request);
            Console.Out.WriteLine(json);
            StringContent requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpResponseMessage responseMessage = await client.PostAsync(url, requestContent))
            {
                if (responseMessage.IsSuccessStatusCode)
                {
                    String responseContent = await responseMessage.Content.ReadAsStringAsync();

                    Console.Out.WriteLine(responseContent);

                    // JoinWallet.Response response = JsonConvert.DeserializeObject<JoinWallet.Response>(responseContent);

                    return new JoinWallet.Response();
                }
                else
                {
                    Console.Out.WriteLine("Error");
                    Console.Out.WriteLine(responseMessage.StatusCode);
                    Console.Out.WriteLine(await responseMessage.Content.ReadAsStringAsync());
                    throw new InvalidOperationException();
                }
            }
        }

        private static string buildSecret(Guid walletId, String walletPrivKey, string network)
        {
            string widHx = walletId.ToString("N");

            string widBase58 = Encoders.Base58.EncodeData(Encoders.Hex.DecodeData(widHx));

            return widBase58 + walletPrivKey + "L";
        }
    }
}