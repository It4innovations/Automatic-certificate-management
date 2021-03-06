# Automatic certificate management
This project aims to manage existing and new certificates issued by CESNET. Certificates already issued are checked at each start-up and if it is the last month before expiry, a new certificate is requested.
The process is fully automated and consists of the following steps:
- Creation of the request.
- Submitting the request using the robotic certificate
- Test the status of the application
- Download and generate the certificate in the form of pem and pkcs12

A robotic certificate is required for proper functionality. (https://pki.cesnet.cz/cs/tcs-robot.html)


The application has three functions depending on the number of parameters.
* 1 - Generates a basic json file that is used as a list of certificates
  * path to the file
* 3 - Testing the robotic certificate
  * path to the robotic certificate
  * path to the robotic certificate key
  * request number
* 6 - Requesting and checking certificates
  * Path to json file
  * Folder where the results are located
  * Template to generate the certificate using openssl
  * path to the robotic certificate
  * Path to the robotic certificate key
  * Path to the file where the intermediate certificates are located 

## Enviroment
The generated key is encrypted and the password is placed in the PRIVATEPASS variable

## Example of json file
```
 {
   "MainDNS": "Root",
   "AlternativeDNS": null,
   "Childs": [
     {
       "MainDNS": "cz",
       "AlternativeDNS": null,
       "Childs": [
         {
           "MainDNS": "vsb",
           "AlternativeDNS": null,
           "Childs": [
             {
               "MainDNS": "test",
             },
             {
               "MainDNS": "test2",
               "AlternativeDNS": [
                 "test3.it4i.cz"
               ],
             }
           ],
         }
       ],
     },
     {
       "MainDNS": "tech",
       "AlternativeDNS": null,
       "Childs": [
         {
           "MainDNS": "lexis",
           "AlternativeDNS": null,
           "Childs": [
             {
               "MainDNS": "it4i"
               ],
            }
            ],
         }
       ],
     }
   ],
 }
```
## Extension
The application itself handles the work with certificates, but it is advisable to place it e.g. in a gitlab, where it will be run regularly and the resulting certificates will be safely located.
