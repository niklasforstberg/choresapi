Could you please create a new file with Chorelog endpoints? These endpoints manages the entries in the ChoreLog table. I would like for you to create endpoints for:
- Add logentry
- Update logentry
- Delete logentry
- Get all logentries for a user
- Get all logentries for a family
- Get all logentries for a chore
- Delete list of logentries
- get all logentries for a family and a specific week. A week is defined by its week number and year.

Please follow the style of familyendpoints. Please create the endpoints in the ChoreLogEndpoints.cs file.
Please create DTOs for the logentries. Please make sure to return a 200 status code for all endpoints if there is no actual error. Please return a 400 status code if there is an error. It is ok to return an empty list.