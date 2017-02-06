#load @"paket-files\include-scripts\net452\include.main.group.fsx"
open SwaggerProvider
open HttpClient
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

module Auth =
    let [<Literal>] private ResponseSample = """{"token_type":"bearer","expires_in":3600,"scope":"wl.emails wl.basic wl.offline_access wl.signin","access_token":"access-token","refresh_token":"refresh-token","user_id":"cc2c4130f73448339235ed7862f1d4b5"}"""

    type private OAuthResponse = FSharp.Data.JsonProvider<ResponseSample>
    let private buildAccessTokenUrl clientId clientSecret authCode = sprintf "https://login.live.com/oauth20_token.srf?client_id=%s&client_secret=%s&redirect_uri=https://login.live.com/oauth20_desktop.srf&grant_type=authorization_code&code=%s" clientId clientSecret authCode
    let private buildRefreshTokenUrl clientId clientSecret refreshToken = sprintf "https://login.live.com/oauth20_token.srf?client_id=%s&client_secret=%s&redirect_uri=https://login.live.com/oauth20_desktop.srf&grant_type=refresh_token&refresh_token=%s" clientId clientSecret refreshToken
    /// Copy the access token from the browser after you have granted permissions.
    let getSignInUrl clientId =
        let scope = "wl.emails%20wl.basic%20wl.offline_access%20wl.signin"
        let url = sprintf "https://login.live.com/oauth20_authorize.srf?client_id=%s&redirect_uri=https://login.live.com/oauth20_desktop.srf&response_type=code&scope=%s" clientId scope
        System.Diagnostics.Process.Start url
    
    /// Try to get initial set of OAuthTokens
    let tryGetTokens clientId clientSecret authCode =
        let parseInitialAuth response =
            let response = OAuthResponse.Parse response
            response.AccessToken, response.RefreshToken

        let tryGetBody = function
            | { StatusCode = 200; EntityBody = Some body } -> Some body
            | _ -> None

        buildAccessTokenUrl clientId clientSecret authCode
        |> createRequest HttpMethod.Get
        |> getResponse
        |> tryGetBody
        |> Option.map parseInitialAuth

/// Get subscription id from MVP API site
let subscriptionKey = ""
/// Get client ID and Secret from  MS Live OAuth app registration site
let clientId, clientSecret = "", ""

// Copy the "code" key from the redirect url once you have granted permissions in the browser!
Auth.getSignInUrl clientId
let authCode = ""

// Now we can finally create the access token!
let accessToken, refreshToken = Auth.tryGetTokens clientId clientSecret authCode |> Option.get


/// OK - that's all the OAuth nonsense out of the way - on with the show...

type Mvp = SwaggerProvider< @"MvpProduction.swagger.json">

let client = Mvp(host = "https://mvpapi.azure-api.net", Headers = [| "Authorization", "Bearer " + accessToken |])

let profile = client.GetMvpProfile(null, subscriptionKey)
profile.YearsAsMvp

let contribs = client.GetContributions(0L, 10L, null, subscriptionKey)
contribs.Contributions.[4]
