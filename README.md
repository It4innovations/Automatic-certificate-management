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

# Client part
It is used for modifying the record, validating already deployed certificates, and simplifying the deployment of new certificates. The application can be run via the included PowerShell script. 

The main menu is as follows:
  - Edit json file
  - Save changes
  - Upload changes back to git
  - Decode and validate new and deployed certificates


All options and settings are applied via menu selections.

## Description of main features
Modifying a JSON file involves adding, changing, or removing a record in the file. It also verifies the validity of the record such as validating DNS records or invalid settings. All changes take place in memory.

Saving the changes converts the data from memory into a json file.

The data is only stored locally but still needs to be uploaded to git.

The user has to enter a password for the private key and the number of days in the past he wants to authenticate services based on the issued certificates. Then the individual certificates are browsed and it is verified whether the certificate has already been deployed (supported protocols and services are HTTPS, LDAPS, MSSQL). If it is unable to verify the validity or the certificate has expired or is about to expire, it temporarily creates decrypted keys in a form suitable for deployment to servers (pem and pfx formats).



# Acknowledgement
This work was supported by the Ministry of Education, Youth and Sports of the Czech Republic through the e-INFRA CZ (ID:90140).

