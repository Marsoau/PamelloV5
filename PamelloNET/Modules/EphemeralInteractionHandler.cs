using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PamelloNET.Modules
{
    public class EphemeralInteractionToken
    {
        public int TokenID { get; private set; }
        public ulong UserID { get; set; }

        public bool IsTriggered { get; set; }

        public EphemeralInteractionToken(ulong userid) {
            Random rnd = new Random();

            UserID = userid;
            TokenID = rnd.Next();

            IsTriggered = false;
        }
    }

    public class EphemeralInteractionHandler
    {
        public Dictionary<EphemeralInteractionToken, string?> EphemeralInteractions { get; private set; }

        public EphemeralInteractionHandler() {
            EphemeralInteractions = new Dictionary<EphemeralInteractionToken, string?>();
        }

        public int Register(ulong userid) {
            EphemeralInteractionToken newtoken = new EphemeralInteractionToken(userid);


            foreach (EphemeralInteractionToken token in EphemeralInteractions.Keys) {
                if (token.UserID == newtoken.UserID) {
                    EphemeralInteractions.Remove(token);
                }
            }

            EphemeralInteractions.Add(newtoken, null);

            return newtoken.TokenID;
        }

        public bool IsActive(int tokenid) {
            foreach (EphemeralInteractionToken token in EphemeralInteractions.Keys) {
                if (tokenid == token.TokenID) {
                    return true;
                }
            }

            return false;
        }

        public void Trigger(string wild) {
            string[] args = wild.Split('&');
            if (args.Length != 2) throw new Exception("Wrong args count");

            if (args[0] == "all") {
                foreach (EphemeralInteractionToken token in EphemeralInteractions.Keys) {
                    EphemeralInteractions[token] = args[1];
                    token.IsTriggered = true;
                }
                return;
            }

            int tokenid = int.Parse(args[0]);
            foreach (EphemeralInteractionToken token in EphemeralInteractions.Keys) {
                if (tokenid == token.TokenID) {
                    EphemeralInteractions[token] = args[1];
                    token.IsTriggered = true;
                    return;
                }
            }

            throw new Exception("This queue sesion timed out");
        }

        public bool IsTriggered(int tokenid) {
            foreach (EphemeralInteractionToken token in EphemeralInteractions.Keys) {
                if (tokenid == token.TokenID) {
                    return token.IsTriggered;
                }
            }

            return false;
        }

        public string? Respond(int tokenid) {
            foreach (EphemeralInteractionToken token in EphemeralInteractions.Keys) {
                if (tokenid == token.TokenID) {
                    token.IsTriggered = false;
                    return EphemeralInteractions[token];
                }
            }

            return null;
        }
    }
}
