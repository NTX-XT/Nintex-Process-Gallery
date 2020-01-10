Version 1.0.0
Created by Dan Burke
Action to query cloud managed Azure AD groups. This Xtension will get member properties for a group, Add members to a group, and delete members from a group.

Notes:
-Add member to group action requires the User ID field to be formatted using url for directory object with odata format.
	Eg:

	 https://graph.microsoft.com/v1.0/directoryObjects/{id} 


-Get Group members result should be saved as text. Collections can be parsed using query json actions. Format jsonpath like below to get collection of data:

	User ID
	$.value[*].id

	User Principal Name
	$.value[*].userPrincipalName
