This is an LDTTeam project designed for Donator reward fulfillment and other similar authentication purposes.
The overall design is to have a user link multiple OAuth accounts together and then provide an API for clients to check whether a user does or doesn't have a specific reward.

One of the major goals of this project was to maintain the privacy of those using the service, as such there is no way to publicly mass query rewards / etc, every user and reward must be checked individually.

# Structure

This project is designed to be modular such that any specific authentication source can easily be removed / added.
Besides a few exceptions (e.g. a form for minecraft usernames in the ExternalLogins page) this has been achieved.

Directories:
* Modules: this is where all modules of the project are located
* OAuth: this is where custom made oauth providers are located
* LDTTeam.Authentication.Server is the main server implementation
* LDTTeam.Authentication.Modules.Api is the main location for module interfaces and abstractions.

# Hosting Requirements

Currently the only hosting requirement is a postgres database.