using System.Net.Http.Headers;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Task = System.Threading.Tasks.Task;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie")
    .AddOpenIdConnect("meldrx", options =>
    {
        options.Authority = "https://app.meldrx.com";
        options.ClientId = "bbbbc257c99c4b06a5c11b09c2946233";
        options.ClientSecret = "9g35_0IsMx6P3S_-yzv3H7ErTS48-7";
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("patient/*.*");

        options.SaveTokens = true;
        options.SignInScheme = "cookie";
        options.CallbackPath = "/signin-oidc";

        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            if (ctx.Properties.Parameters.TryGetValue("aud", out var audUrlObj)
                && audUrlObj is string audUrlStr)
            {
                ctx.ProtocolMessage.Parameters["aud"] = audUrlStr;
            }

            return Task.CompletedTask;
        };
    });

var app = builder.Build();

app.MapGet("/login", () =>
    Results.Challenge(
        new AuthenticationProperties()
        {
            Parameters =
            {
                ["aud"] = "https://app.meldrx.com/api/fhir/2469093b-8329-47f7-9344-51e6629e9de4"
            },
            RedirectUri = "https://localhost:7245"
        },
        ["meldrx"]
    )
);

app.MapGet("/token", async (HttpContext ctx) =>
    {
        return await ctx.GetTokenAsync("access_token");
    }
);

app.MapGet("/patient", async (string location, HttpContext ctx, HttpClient http) =>
    {
        var token = await ctx.GetTokenAsync("access_token");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fhirClient = new FhirClient(
            "https://app.meldrx.com/api/fhir/2469093b-8329-47f7-9344-51e6629e9de4",
            http,
            new FhirClientSettings()
            {
                PreferredFormat = ResourceFormat.Json
            }
        );

        return await fhirClient.ReadAsync<Patient>(location);
    }
);

app.MapGet("/create-patient", async (HttpContext ctx, HttpClient http) =>
    {
        var token = await ctx.GetTokenAsync("access_token");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fhirClient = new FhirClient(
            "https://app.meldrx.com/api/fhir/2469093b-8329-47f7-9344-51e6629e9de4",
            http,
            new FhirClientSettings()
            {
                PreferredFormat = ResourceFormat.Json
            }
        );

        var patient = JsonSerializer.Deserialize<Patient>(
            Const.PatientJson,
            new JsonSerializerOptions().ForFhir()
            );

        return await fhirClient.CreateAsync<Patient>(patient);
    }
);

app.Run();

public class Const
{
    public const string PatientJson = """
                                      {
                                        "resourceType": "Patient",
                                        "id": "example",
                                        "text": {
                                          "status": "generated",
                                          "div": "<div xmlns=\"http://www.w3.org/1999/xhtml\">\n\t\t\t<table>\n\t\t\t\t<tbody>\n\t\t\t\t\t<tr>\n\t\t\t\t\t\t<td>Name</td>\n\t\t\t\t\t\t<td>Peter James \n              <b>Chalmers</b> (&quot;Jim&quot;)\n            </td>\n\t\t\t\t\t</tr>\n\t\t\t\t\t<tr>\n\t\t\t\t\t\t<td>Address</td>\n\t\t\t\t\t\t<td>534 Erewhon, Pleasantville, Vic, 3999</td>\n\t\t\t\t\t</tr>\n\t\t\t\t\t<tr>\n\t\t\t\t\t\t<td>Contacts</td>\n\t\t\t\t\t\t<td>Home: unknown. Work: (03) 5555 6473</td>\n\t\t\t\t\t</tr>\n\t\t\t\t\t<tr>\n\t\t\t\t\t\t<td>Id</td>\n\t\t\t\t\t\t<td>MRN: 12345 (Acme Healthcare)</td>\n\t\t\t\t\t</tr>\n\t\t\t\t</tbody>\n\t\t\t</table>\n\t\t</div>"
                                        },
                                        "identifier": [
                                          {
                                            "use": "usual",
                                            "type": {
                                              "coding": [
                                                {
                                                  "system": "http://terminology.hl7.org/CodeSystem/v2-0203",
                                                  "code": "MR"
                                                }
                                              ]
                                            },
                                            "system": "urn:oid:1.2.36.146.595.217.0.1",
                                            "value": "12345",
                                            "period": {
                                              "start": "2001-05-06"
                                            },
                                            "assigner": {
                                              "display": "Acme Healthcare"
                                            }
                                          }
                                        ],
                                        "active": true,
                                        "name": [
                                          {
                                            "use": "official",
                                            "family": "Chalmers",
                                            "given": [
                                              "Peter",
                                              "James"
                                            ]
                                          },
                                          {
                                            "use": "usual",
                                            "given": [
                                              "Jim"
                                            ]
                                          },
                                          {
                                            "use": "maiden",
                                            "family": "Windsor",
                                            "given": [
                                              "Peter",
                                              "James"
                                            ],
                                            "period": {
                                              "end": "2002"
                                            }
                                          }
                                        ],
                                        "telecom": [
                                          {
                                            "use": "home"
                                          },
                                          {
                                            "system": "phone",
                                            "value": "(03) 5555 6473",
                                            "use": "work",
                                            "rank": 1
                                          },
                                          {
                                            "system": "phone",
                                            "value": "(03) 3410 5613",
                                            "use": "mobile",
                                            "rank": 2
                                          },
                                          {
                                            "system": "phone",
                                            "value": "(03) 5555 8834",
                                            "use": "old",
                                            "period": {
                                              "end": "2014"
                                            }
                                          }
                                        ],
                                        "gender": "male",
                                        "birthDate": "1974-12-25",
                                        "_birthDate": {
                                          "extension": [
                                            {
                                              "url": "http://hl7.org/fhir/StructureDefinition/patient-birthTime",
                                              "valueDateTime": "1974-12-25T14:35:45-05:00"
                                            }
                                          ]
                                        },
                                        "deceasedBoolean": false,
                                        "address": [
                                          {
                                            "use": "home",
                                            "type": "both",
                                            "text": "534 Erewhon St PeasantVille, Rainbow, Vic  3999",
                                            "line": [
                                              "534 Erewhon St"
                                            ],
                                            "city": "PleasantVille",
                                            "district": "Rainbow",
                                            "state": "Vic",
                                            "postalCode": "3999",
                                            "period": {
                                              "start": "1974-12-25"
                                            }
                                          }
                                        ],
                                        "contact": [
                                          {
                                            "relationship": [
                                              {
                                                "coding": [
                                                  {
                                                    "system": "http://terminology.hl7.org/CodeSystem/v2-0131",
                                                    "code": "N"
                                                  }
                                                ]
                                              }
                                            ],
                                            "name": {
                                              "family": "du Marché",
                                              "_family": {
                                                "extension": [
                                                  {
                                                    "url": "http://hl7.org/fhir/StructureDefinition/humanname-own-prefix",
                                                    "valueString": "VV"
                                                  }
                                                ]
                                              },
                                              "given": [
                                                "Bénédicte"
                                              ]
                                            },
                                            "telecom": [
                                              {
                                                "system": "phone",
                                                "value": "+33 (237) 998327"
                                              }
                                            ],
                                            "address": {
                                              "use": "home",
                                              "type": "both",
                                              "line": [
                                                "534 Erewhon St"
                                              ],
                                              "city": "PleasantVille",
                                              "district": "Rainbow",
                                              "state": "Vic",
                                              "postalCode": "3999",
                                              "period": {
                                                "start": "1974-12-25"
                                              }
                                            },
                                            "gender": "female",
                                            "period": {
                                              "start": "2012"
                                            }
                                          }
                                        ],
                                        "managingOrganization": {
                                          "reference": "Organization/1"
                                        }
                                      }
                                      """;
}
